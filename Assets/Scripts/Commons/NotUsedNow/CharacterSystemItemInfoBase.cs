using System.Collections.Generic;
using System.Collections;
using System.Linq;
using UnityEngine;
using Commons;
using Commons.Helpers;
using Sirenix.OdinInspector;


/// <summary>
/// 현재 사용안함 (앞으로 사용하지 않을 가능성이 높음 => 그럴경우 삭제 요망....)
/// 대부분이 MyInfo<클래스>를 통해 내정보를 대체할 수 있기 때문에....
/// </summary>
public class UserPlayerSystemItemInfoBase : InstanceAnim
{
    #region UserPlayerSystemItemInfoBase에 관한 설명 [현재는 구현 보류]
    //만일 해당 추상 부모 클래스를 사용하게 된다면
    //1.오직 사용자가 플레이 할 수 있는 캐릭터에 관한 정보들로 사용해야한다
    //2.예를들어 멀티일 경우 나를 제외한 다른 클라이언트의 정보나
    //3.구현사항에 따라 플레이어가 다른 캐릭터를 조작할때 파생해서 사용하기 위한 로직에
    //해당되겠다
    //[보류 더나아가 사용하지 않는 이유]
    //하지만 대부분 메인 클라이언트 (즉 내가 플레이 하는 캐릭터)에만 거의 사용할 로직에
    //해당될 확률이 높기 때문이다 (**다음 아래에서는 왜 해당 로직들은 MyInfo에서만 정의해야 하는지에 대한 이유이다**)
    //*(몹 => 비슷하더라도 전부 나눠서 데이터를 관리) +
    //*(if 멀티 타 유저 => 데이터가 필요할때 (ex => isMine : false가 죽어서 인벤 데이터를 뿌리기 or 상태 무기 정보)
    //등은 필요할때 그때마다 송신하여 isMine 캐릭터가 확인하는 구조로 간다 <**각 클라이언트의 데이터 과부화 현상 최적화 방지를 위해**>
    #endregion

    [Header("Required Values")]
    [SerializeField, ReadOnly] private PlatformManager.PlatformType s_PlatformType;
    [SerializeField, ReadOnly] private NewPlayerCharacterController s_CreateMyPlayer;
    [SerializeField] MySavedInfo s_MySavedInfo;
    [SerializeField] MyInGameOutPutInfo s_MyInGameOutPutInfo;

    [Tooltip("Sub Values")]
    private bool s_WaitInvenInitDone = false;
    private Camera s_ListenOGMainCam;
    private ScriptableObjectManager m_GetSDataManager;
    private readonly System.UInt16 s_MyInfoCharacterOutPutShaderIDX = 700;

    public string spp_DBSavedIDX { get; private set; }
    public bool spp_isOutPutMyCharacter { get => s_CreateMyPlayer != null; }

    public UserPlayerSystemItemInfoBase(Appinstance _GetInstance, string _InitSaveIDX)
    {
        m_GetSDataManager = _GetInstance.ms_ScriptableObjectManager;
        ResetAllSaved();
    }

    #region Main System Functions (Parsing DB)

    //해당 구역에서 로드 이후에 아이템값을 찾는 과정을 코루틴으로 제어할 수 있도록 한다(아이템양이 많을 때를 대비한 조치)
    public void AllMyInfoLoadAndApplyAnyncParsing(System.Action _EndCallBack, out IEnumerator[] _GetCourtines)
    {
        LocalSettingJsonData(SettingJsonType.Load);
        _GetCourtines = new IEnumerator[2];
        _GetCourtines[0] = ChangeByInfoToItemInfo(true, s_MySavedInfo.s_SavedInvenItemKeys, s_MyInGameOutPutInfo.s_InvenInfos);
        ItemInfo[] ApplyInfos = s_MyInGameOutPutInfo.s_AmountInfos.OfType<ItemInfo>().ToArray();
        _GetCourtines[1] = ChangeByInfoToItemInfo(false, s_MySavedInfo.s_SavedAmountItemKeys, ApplyInfos, _EndCallBack);
    }

    private void LocalSettingJsonData(SettingJsonType _currState)
    {
        string GetFileNames = $"{nameof(MyInfo)}_{s_PlatformType}_{spp_DBSavedIDX}";
        if (_currState == SettingJsonType.Save || _currState == SettingJsonType.AllReset)
        {
            if (_currState == SettingJsonType.AllReset) ResetAllSaved();
            string SavedJson = JsonUtility.ToJson(s_MySavedInfo, true);
            PlatformManager.SetPlatformSaveData(PlatformManager.ParsingPathType.LocalJson, GetFileNames, SavedJson);
        }
        else if (_currState == SettingJsonType.Load)
        {
            if (PlatformManager.GetPlatformLoadData(
                PlatformManager.ParsingPathType.LocalJson, GetFileNames, true, out object _OutReference))
                s_MySavedInfo = JsonUtility.FromJson<MySavedInfo>(_OutReference.ToString());
        }
    }

    IEnumerator ChangeByInfoToItemInfo(bool _isInven,
    MySavedItemInfo[] _GetSavedParsingInfos, ItemInfo[] _ApplyItemInfo, System.Action _EndCallBack = null)
    {
        if (!_isInven) yield return new WaitUntil(() => s_WaitInvenInitDone);
        int CountIDX = 0; _ApplyItemInfo = new PartsItemInfo[_GetSavedParsingInfos.Length];
        foreach (MySavedItemInfo x in _GetSavedParsingInfos)
        {
            _ApplyItemInfo[CountIDX] = m_GetSDataManager.
            SDataParsingByItemIDX(x.s_SavedItemIDX) as PartsItemInfo; CountIDX++;
            yield return new WaitForSeconds(0.1f);
        }
        if (_isInven) { s_MyInGameOutPutInfo.s_InvenInfos = _ApplyItemInfo; s_WaitInvenInitDone = true; }
        else s_MyInGameOutPutInfo.s_AmountInfos = _ApplyItemInfo.ToList().OfType<PartsItemInfo>().ToArray();
        _EndCallBack?.Invoke();
    }

    #endregion

    #region Sub System Public Functions

    //초기화 및 Reset 로직
    public void ResetAllSaved()
    {
        s_MySavedInfo = new MySavedInfo();
        s_MySavedInfo.s_SavedInvenItemKeys = new MySavedItemInfo[0];
        s_MySavedInfo.s_SavedAmountItemKeys = new MySavedItemInfo[0];
        s_MySavedInfo.s_SavedMapItemKeys = new MySavedMapReference[0];
        s_MyInGameOutPutInfo = new MyInGameOutPutInfo();
        s_MyInGameOutPutInfo.s_InvenInfos = new ItemInfo[0];
        s_MyInGameOutPutInfo.s_AmountInfos = new PartsItemInfo[0];
    }

    //중복 검사 관련한 로직
    public enum OverlappingType { None = 0, InsertType, RemoveType }

    public bool FindDuplicateItem(int _CompareItemIDX, OverlappingType _SetType = OverlappingType.None) //아이템의 중복검사
    {
        //인벤토리만 중복검사를 진행하기 때문에 Amount는 필요없다 기존 Push And Pop을 진행할 수 있도록 하자
        int FIDX = -1; FIDX =
         s_MySavedInfo.s_SavedInvenItemKeys.ToList().FindIndex(x => x.s_SavedItemIDX == _CompareItemIDX);
        if (FIDX != -1)
        {
            if (_SetType == OverlappingType.InsertType) s_MySavedInfo.s_SavedInvenItemKeys[FIDX].s_SavedItemCount++;
            else if (_SetType == OverlappingType.RemoveType)
            {
                int CountNow = s_MySavedInfo.s_SavedInvenItemKeys[FIDX].s_SavedItemCount;
                if (CountNow <= 1) return false;
                s_MySavedInfo.s_SavedInvenItemKeys[FIDX].s_SavedItemCount--;
            }
            return true;
        }
        return false;
    }

    #endregion

    #region 내정보의 MapReference Functions

    public void SaveMyInfoByMapReference(bool _isNew, MySavedMapReference _GetMapReference)
    {
        if (_isNew)
        {
            s_MySavedInfo.s_SavedMapItemKeys = s_MySavedInfo.s_SavedMapItemKeys.Concat(new[] { _GetMapReference }).ToArray();
            LocalSettingJsonData(SettingJsonType.Save);
            return;
        }
        int FIDX = -1; FIDX =
        s_MySavedInfo.s_SavedMapItemKeys.ToList().FindIndex(x => x.Equals(_GetMapReference));
        if ((FIDX == -1).HDebug("내정보에 저장된 맵 정보가 존재하지 않습니다..!!", Helper.HDType.Error)) return;
        s_MySavedInfo.s_SavedMapItemKeys[FIDX] = _GetMapReference;
        LocalSettingJsonData(SettingJsonType.Save);
    }

    public MySavedMapReference? GetFindSavedMyInfoMapReferByIDX(int _FIDX)
    {
        int FIDX = -1; FIDX =
        s_MySavedInfo.s_SavedMapItemKeys.ToList().FindIndex(x => x.s_SavedItemIDX == _FIDX);
        if (FIDX == -1) return null;
        return s_MySavedInfo.s_SavedMapItemKeys[FIDX];
    }
    #endregion

    #region 내정보의 아이템 생성 삭제 관한것 (차후 장착 또한 진행할것)
    //중복값이 포함됨
    private bool IsCheckCompareSaveIDX(string _CompareSaveIDX)
    {
        string SetCompare = _CompareSaveIDX.Contains('_') ? _CompareSaveIDX.Split('_')[1] : _CompareSaveIDX;
        if (spp_DBSavedIDX != SetCompare)
        {
            Debug.LogErrorFormat("저장키값이 일치하지 않습니다 {0} {1} \n " +
            "캐릭터에서 사용하는 아이템 컬렉션은 저장값이 일치되어야 합니다.", spp_DBSavedIDX, SetCompare);
            return true;
        }
        return false;
    }

    public void AddCheckUpdateInfo(string _CompareSaveIDX, ItemInfo _AddItemInfo)
    {
        string SetCompare = _CompareSaveIDX.Contains('_') ? _CompareSaveIDX.Split('_')[0] : _CompareSaveIDX;
        if (IsCheckCompareSaveIDX(_CompareSaveIDX)) return;

        //1.중복된 값을 찾아서 있다면 Amount가 아닌 오르지 Inventory에서만 존재할 수 있도록 수정해야 된다

        switch (SetCompare)
        {
            case "Amount"://=>Amount에서는 타입(PartsType)으로 해당 타입을 없애고 추가하는 형식으로 진행하고
                //int FIDX = -1;
                //FIDX = s_MySavedInfo.s_SavedAmountItemKeys.ToList().FindIndex(x => x.s_SavedItemIDX == _AddItemInfo.s_ItemIDX);
                //if (FIDX != -1) RemoveCheckUpdateInfo(_CompareSaveIDX, _AddItemInfo);
                break;
            case "Inventory":
                //=>Inventory는 같은 값이 존재한다면 중복 ItemCount를 덫붙힐 수 있도록 한다 (이건 차후에 컴펌이 필요할 수도 있다)
                s_MySavedInfo.s_SavedInvenItemKeys = s_MySavedInfo.s_SavedInvenItemKeys.Concat(new[] {
                new MySavedItemInfo { s_SavedItemIDX = _AddItemInfo.s_ItemIDX, s_SavedItemCount = 1,
                s_SavedItemType = _AddItemInfo.s_ItemType} }).ToArray();
                s_MyInGameOutPutInfo.s_InvenInfos =
                Helper.ArrayAddRemoveStruct(s_MyInGameOutPutInfo.s_InvenInfos, _AddItemInfo, true);
                break;
        }

        //2.업데이트된 데이터를 제이슨 형태로 서버나 로컬에 저장....
    }

    public void RemoveCheckUpdateInfo(string _CompareSaveIDX, ItemInfo _RemoveItemInfo)
    {
        string SetCompare = _CompareSaveIDX.Contains("_") ? _CompareSaveIDX.Split('_')[0] : _CompareSaveIDX;
        if (IsCheckCompareSaveIDX(_CompareSaveIDX)) return;
        //1.생성도 포함이지만 아예 지우고 다시 생성할 수 있도록 수정한다

        switch (SetCompare)
        {
            case "Amount":
                //s_MySavedInfo.s_SavedAmountItemKeys = s_MySavedInfo.s_SavedAmountItemKeys.Where
                //(item => item.s_SavedItemIDX != _RemoveItemInfo.s_ItemIDX).ToArray();
                //s_MyInGameOutPutInfo.s_AmountInfos =
                //Helper.ArrayAddRemoveStruct(s_MyInGameOutPutInfo.s_AmountInfos, _RemoveItemInfo as PartsItemInfo, false);
                break;
            case "Inventory":
                s_MySavedInfo.s_SavedInvenItemKeys = s_MySavedInfo.s_SavedInvenItemKeys.Where
                (item => item.s_SavedItemIDX != _RemoveItemInfo.s_ItemIDX).ToArray();
                s_MyInGameOutPutInfo.s_InvenInfos =
                Helper.ArrayAddRemoveStruct(s_MyInGameOutPutInfo.s_InvenInfos, _RemoveItemInfo, false);
                break;
        }
    }
    #endregion

}

//혹시라도 해당 클래스를 사용하여 데이터 System 상속구조를 사용할경우
//비슷한것들을 빼서 작명을 "Character"로 바꾼다음 컴포지션 패턴을 고수할 수 있도록 리팩토링 요망.... *****
//파생되는 구간은 
//[System.Serializable]
//public struct CharacterSavedInfo
//{
//    public MySavedItemInfo[] s_SavedInvenItemKeys;
//    public MySavedItemInfo[] s_SavedAmountItemKeys;
//}
