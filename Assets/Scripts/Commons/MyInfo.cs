using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Commons;
using Commons.Helpers;
//using Commons.Helpers.Attribute;
using Sirenix.OdinInspector;

[System.Serializable]
public class MyInfo //: CharacterItemInfo //이건 다른걸로 교체되거나 상속이 아닌 종속성 형태로 격하될 수 있다
{
    #region MyInfo의 구조에 관한 설명(스크립터블 데이터 Reference)
    //CharacterInfo를 상속이 아닌 컴포지션으로 SManager<클래스>나 DataManager<클래스>를 통해 가져올 수 있도록 한다
    //(또한 MyInfo를 CharacterController<클래스>에 삽입하고
    //해당 되는 ItemInfo<스크립터블 데이터 클래스> 또한 해당 구역에서 파싱하여 Controller 정적으로 구성해야하는(ex => 초기화 or 생성자 함수)
    //부분에 전부 데이터를 뿌릴 수 있도록 한다
    #endregion
    [Header("======ItemInfo => MyInfo Reference======")]
    [Header("Most Required Values")]
    [SerializeField, ReadOnly] protected MySavedInfo s_MySavedInfo; //해당 구조체가 저장 로드하는 최종 데이터임 (리팩가능성 존재) ****************************************
    [Header("Required Values")] //ReadOnly 부분 DropDown으로 다른곳에서 확인만 가능하되 동적인 상황에서만 의미가 있다는것을 명시 요망...
    [SerializeField, ReadOnly] private PlatformManager.PlatformType s_PlatformType; //시작시 Appinstance에서 플렛폼 타입을 구분하고 MyInfo초기화시 가져오는것으로 변경하기
    [SerializeField, ReadOnly] private string s_MyinfoLoadResourcesPath = string.Empty; //(**삭제요망**) GamePlayManager or GamePlaySystem에서 받아오는걸로 진행한다
    [SerializeField, ReadOnly] private NewPlayerCharacterController s_CreateMyPlayer;
    [SerializeField] MyInGameOutPutInfo s_MyInGameOutPutInfo;
    [Header("TestMode Reference Values")]
    [SerializeField, ReadOnly] private ConTestModeChildTypeName[] s_ControlChildTestMode;

    [Header("Required Values(**ApplyNew**)")]// 아래 변수를 잘 확인하여 위의 사항을 전부 변경할 수 있도록 수정요망
    [SerializeField, ReadOnly] private ControllerBase s_CreateMainController;

    [Tooltip("Sub Values")]
    private bool s_WaitInvenInitDone = false;
    protected DataManager m_DataManager;
    protected ScriptableObjectManager m_GetSDataManager;
    private Camera s_ListenOGMainCam;
    //private readonly System.UInt16 test_MyInfoCharacterOutPutShaderIDX = 400; //다른 방식으로 적용해야함

    public string spp_DBSavedIDX { get; private set; }
    public bool spp_isOutPutMyCharacter { get => s_CreateMyPlayer != null; }
    public bool spp_isMyMainController { get => s_CreateMainController != null; }
    public MySavedInfo spp_MySavedInfo { get => s_MySavedInfo; set => s_MySavedInfo = value; }//테스트용임 원래는 다른 루트를 통해 제이슨에 저장후 변경해야함

    public MyInfo(Appinstance _GetInstance, string _InitSaveIDX)
    {
        s_PlatformType = _GetInstance.ms_MainPlatform; //PlatformManager.InitPlatform();
        //로그인을 실패하는 역활이 될 수도 있기 때문에 Notice를 실행하고 다시 로그인창으로 이동해야된다 재설계 요망.....*******
        if ((s_PlatformType != PlatformManager.PlatformType.Editor && string.IsNullOrEmpty(_InitSaveIDX)).
        HDebug("키값을 서버로부터 받아오지 못했습니다 에디터 이외에 구간에서는 키값이 존재해야합니다..!!", Helper.HDType.Error)) return;
        _GetInstance.ApplyMyInfo(this, out s_MyinfoLoadResourcesPath);
        _GetInstance.ms_DelEventManager.DELUpdateMyInfoFirstinit(this);
        spp_DBSavedIDX = string.IsNullOrEmpty(_InitSaveIDX) ? "TestLoginKey" : _InitSaveIDX;
        m_DataManager = _GetInstance.ms_DataManager;
        m_GetSDataManager = _GetInstance.ms_ScriptableObjectManager;
        s_ControlChildTestMode = m_DataManager.pp_MyInfoCheckTestModeByChild;
        ResetAllSaved();
    }

    #region My Character OutPut Reference (인게임에서만 사용 요망) + [**리팩후 삭제 요망**] (현재는 사용하지 않음)
    //[전체 설명] 해당 로직들 또한 초반 초기화 연출(캐릭터 동작 or 시네마틱중 동작)과 관련된 사항이기 때문에 
    //후에 스크립터블로 구조화하여 ProductionInfo<스크립터블 데이터 클래스>에서 맵 인덱스에 따라 실행될 수 있도록 로직을 옮길 수 있도록 수정 요망....
    //public IEnumerator MyPlayerSetting(bool _isInitAction,
    //CameraController.CamControllerInitInfo? _GetInitInfo, System.Action<NewPlayerCharacterController> AppliedOutPost, System.Action _EndCallBack)
    //{
    //    NewPlayerCharacterController GetPlayer = CheckAndOutPutByTemp<NewPlayerCharacterController>("MyPlayerController");
    //    CameraController GetCameraController = CheckAndOutPutByTemp<CameraController>("MainPlayerCamController");
    //    //InputController<클래스>의 경우 해당 구역에서 MyInfo의 타입을 가져와 JoyStack인지 구분할 수 있도록 한다
    //    bool isApplyPlayerPower = 
    //    s_PlatformType == PlatformManager.PlatformType.PC_Window || s_PlatformType == PlatformManager.PlatformType.Editor;
    //    #region InGame MyInfo Character Required Init Reference
    //    //Character Init
    //    GetPlayer.Initialization(this, isApplyPlayerPower ? Commons.PowerType.SelfInputPower : Commons.PowerType.SelfJoyStickPower);
    //    GetPlayer.ChangeState(false, false);
    //    //Character Shader Init
    //    NewShaderInfo GetShaderInfo =
    //    m_GetSDataManager.SDataParsingByItemIDX<SDataShaderInfo, NewShaderInfo>(test_MyInfoCharacterOutPutShaderIDX);
    //    //Camera Init
    //    UnityEngine.Rendering.Volume GetVC =
    //    Camera.main.transform.ChildLinearStuctureSearch<UnityEngine.Rendering.Volume>()[0];
    //    s_ListenOGMainCam = Camera.main;
    //    Transform ApplyPlayerTr = Helper.ChildLinearStuctureSearch(GetPlayer.transform).ToList().Find(x => x.name == "CamTarget");
    //    Object.FindObjectsOfType<Camera>().ToList().FindAll(x => x.gameObject.tag == "MainCamera" &&
    //    !x.Equals(GetCameraController.GetComponent<Camera>())).ForEach(x => x.gameObject.SetActive(false));
    //    GetVC.transform.SetParent(GetCameraController.transform);
    //    s_CreateMyPlayer = GetPlayer;
    //    AppliedOutPost?.Invoke(GetPlayer);
    //    #endregion

    //    if (!_isInitAction) 
    //    {
    //        GetPlayer.ChangeState(true, false);
    //        GetPlayer.ApplyShaderInfo(GetShaderInfo, true, true);
    //        GetCameraController.Initlization(ApplyPlayerTr, _GetInitInfo);
    //        _EndCallBack?.Invoke();
    //        return null;
    //        //return new System.Tuple<NewPlayerCharacterController, IEnumerator>(s_CreateMyPlayer, null);
    //    }

    //    GetCameraController.Initlization(s_ListenOGMainCam.transform, null);
    //    ShaderOutPutBase GetBase = GetPlayer.BuilderShaderOutPut(true, GetShaderInfo, null, _EndCallBack);
    //    return CO_CharacterWaitInitSetting(GetPlayer, GetCameraController, ApplyPlayerTr, _GetInitInfo, GetBase);
    //    //해당 코루틴 내부에서 또한 쉐이더 작동 여부를 결정하여 넘겨주면 되겠다
    //    //return new System.Tuple<NewPlayerCharacterController, IEnumerator>(
    //    //s_CreateMyPlayer, CO_CharacterWaitInitSetting(GetPlayer, GetCameraController, ApplyPlayerTr, _GetInitInfo, GetBase));
    //}

    #region My Character OutPut Sub System Functions

    private T CheckAndOutPutByTemp<T>(string _GetName) where T : MonoBehaviour
    {
        MonoBehaviour[] GetObjArray = MonoBehaviour.FindObjectsOfType<T>();

        if (GetObjArray.Length > 1 || spp_isOutPutMyCharacter)
        {
            Debug.LogErrorFormat("가져오려는 내정보중 {0}는 중복하여 생성할 수 없습니다..!!", typeof(T).Name);
            return null;
        }
        return GetObjArray.Length == 1 ? GetObjArray[0] as T :
        Object.Instantiate(Resources.Load<T>(s_MyinfoLoadResourcesPath + _GetName));
    }

    public void RemoveBySettingMyCharacter(NewPlayerCharacterController _GetPlayer)
    {
        if ((!s_CreateMyPlayer.Equals(_GetPlayer) || !spp_isOutPutMyCharacter || !_GetPlayer.pp_isMine).
        HDebug($"내캐릭터가 생성된적이 없거나 매개변수에 해당되는 변수 {_GetPlayer.gameObject.name} 이 내 캐릭터가 아닙니다.! "))
        return;
        Object.Destroy(s_CreateMyPlayer.gameObject);
        Object.Destroy(Object.FindObjectOfType<CameraController>().gameObject);
        s_ListenOGMainCam.gameObject.SetActive(true);
        s_CreateMyPlayer = null;
    }

    //게임에 따라 필요하거나 필요없을 수 있기 때문에 이후 CameraControllerHeader를 만들어서 컨트롤할 수 있는 구간을 따로 만든다
    IEnumerator CO_CharacterWaitInitSetting(
    NewPlayerCharacterController _GetPlayer, CameraController _GetController, Transform _FinalTr = null,
    CameraController.CamControllerInitInfo? _GetInitInfo = null, ShaderOutPutBase _GetShaderBase = null)
    {
        if (_FinalTr == null) yield return null;
        yield return new WaitForSeconds(0.3f);
        _GetController.Initlization(_FinalTr, _GetInitInfo);
        #region 제약 이후 풀기위한 조건
        Vector3 OGPos = _GetController.transform.position;
        yield return new WaitUntil(() => OGPos != _GetController.transform.position);
        yield return new WaitForSeconds(1.5f);
        OGPos = _GetController.transform.position;
        yield return new WaitUntil(() => OGPos == _GetController.transform.position);
        #endregion
        _GetShaderBase?.OutPutShader(() =>
        {
            CameraController.CamControllerInitInfo SetApplyNewInfo = _GetInitInfo != null ?
            _GetInitInfo.Value : new CameraController.CamControllerInitInfo();
            SetApplyNewInfo.s_isNotUseRot = false;
            SetApplyNewInfo.s_isFollowSmooth = false;
            _GetController.Initlization(_FinalTr, SetApplyNewInfo);
            _GetPlayer.ChangeState(true, false);
        });
    }

    #endregion

    #endregion

    #region Main System Functions (Load & Save Reference)

    protected void LocalSettingJsonData(SettingJsonType _currState)
    {
        string GetFileNames = $"{this.GetType().Name}_{s_PlatformType}_{spp_DBSavedIDX}";
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

    public virtual void SavedMyInfoData<T>(T _GetData, string _GetFieldName)
    {
        var GetApplyValue = s_MySavedInfo.SetCompareFieldInfo<T>(_GetData, _GetFieldName);
        s_MySavedInfo = (MySavedInfo)GetApplyValue; //인벤과 장착 아이템 로직 또한 CharacterMyInfo로 옮길 수 있도록 수정한다
        LocalSettingJsonData(SettingJsonType.Save);
    }
    #endregion

    #region Sub System Functions (Parsing DB)

    #region ApplyModulesByInfo에 대한 설명
    /// <summary>
    ///프로젝트마다 상속하여 최종 GamePlaymanager<클래스>에 MainController배열을 배출하는 구조이며
    ///말그대로 MainController만 전달가능하다 또한 배열의 제일 앞에 위치한 요소가 GamePlayManager<크래스에서>
    ///관장하는 중점적으로 제어하는 Controller가 되며 나머지는 기존 SubModuleController처럼 GamePlayManager<클래스>에서
    ///통제하게 된다 마지막으로 각 매개변수는 상속된 MyInfo와 관련된 ItemInfo(스크립터블 데이터)만 들어갈 수 있도록 제한된다)
    /// </summary>
    /// <param name="_ApplyParams"></param>
    /// <returns></returns>
    #endregion
    public virtual ControllerBase[] ApplyModulesByInfo(params ItemInfo[] _ApplyParams) => new ControllerBase[_ApplyParams.Length];

    public virtual void ApplyNewSubModule(object _ApplyElem, bool _isSpecApply = false) { } //DoNothing.. 혹은 관련된 기본 MyInfo 데이터 넘길것

    //해당 구역에서 로드 이후에 아이템값을 찾는 과정을 코루틴으로 제어할 수 있도록 한다(아이템양이 많을 때를 대비한 조치)
    public virtual void AllMyInfoLoadAndApplyAnyncParsing(System.Action _EndCallBack, out IEnumerator[] _GetCourtines)
    {
        //bool isExecuteEndCallBack = this.GetType().Name == nameof(MyInfo);
        LocalSettingJsonData(SettingJsonType.Load);
        _GetCourtines = new IEnumerator[2];
        _GetCourtines[0] = ChangeByInfoToItemInfo(true, s_MySavedInfo.s_SavedInvenItemKeys, s_MyInGameOutPutInfo.s_InvenInfos, _EndCallBack);
        ItemInfo[] ApplyInfos = s_MyInGameOutPutInfo.s_AmountInfos.OfType<ItemInfo>().ToArray();
        _GetCourtines[1] = ChangeByInfoToItemInfo(false, s_MySavedInfo.s_SavedAmountItemKeys, ApplyInfos, _EndCallBack);
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
        if (_isInven) { s_MyInGameOutPutInfo.s_InvenInfos = _ApplyItemInfo; s_WaitInvenInitDone = true;}
        else s_MyInGameOutPutInfo.s_AmountInfos = _ApplyItemInfo.ToList().OfType<PartsItemInfo>().ToArray();
        _EndCallBack?.Invoke();
    }

    #endregion

    #region Sub System Functions (** Info Reference **)

    //초기화 및 Reset 로직
    protected virtual void ResetAllSaved()
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

    public bool IsPlatForm(PlatformManager.PlatformType _GetType)
    => s_PlatformType == _GetType;

    #endregion

    #region Sub System Private Functions (**Init Reference**)

    protected bool CheckBaseInfoTemp<T>(ControllerBase[] _GetBases, ItemInfo[] _GetParams, out System.Tuple<T, int> _ApplyPair) where T : ItemInfo
    {
        bool isFindTemp = _GetParams.ISArrayFindCondition<ItemInfo>(x => x is T, out ItemInfo _GetTemp);
        bool isFindBases = _GetBases.ISArrayFindIDXCondition<ControllerBase>(x => x == null, out int _ApplyIDX);
        _ApplyPair = isFindTemp && isFindBases ? new(_GetTemp as T, _ApplyIDX) : null;
        return isFindTemp && isFindBases;
    }

    protected bool TestMode_CheckByChildSubObject<T>() where T : MyInfo
    {
        if(s_ControlChildTestMode.ISArrayFindCondition(x => 
        this is T GetTemp && typeof(T).Name == x.s_ChildObjectName, out ConTestModeChildTypeName _GetInfo))
        return _GetInfo.s_isTestMode;
        return false;
    }

    protected virtual bool CheckCompareDBIDX(object _GetDBKey)
    {
        bool isComplate = false;
        if(_GetDBKey is string _GetStrKey)
        isComplate = spp_DBSavedIDX == _GetStrKey;
        (!isComplate).HDebug("잘못된 데이터가 들어왔습니다.!", Helper.HDType.Error);
        return isComplate;
    }

    #endregion

    #region 내정보의 MapReference Functions

    //삭제처리할것 위의 상혹 함수인 SavedMyInfoData으로 전체 변환할 수 있도록 수정할것
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

    #region My Exclucive Info Reference

    [System.Serializable]
    public struct ConTestModeChildTypeName
    {
        public string s_ChildObjectName;
        public bool s_isTestMode;
    }

    #endregion
}

#region 25/01/02 Update MyInfo Apply Redux
//Buffer 작명은 데이터 전달 목적의 모든 구조체에 부여되는 호칭으로 앞으로사용된다
//기타 Buffer를 상위객체로 묶어야 되는 케이스가 발생하면 전체를 참조형태로 변경해야한다
//(**ProjectSingleNoteFolder => TyconGames_참고사항정리 메모장 참고**)
public struct MyItemInfoBuffer : I_Data
{
    public ItemInfo[] buffer_MyItemInfos;

    public MyItemInfoBuffer(ItemInfo[] _GetInfos)
    {
        buffer_MyItemInfos = _GetInfos;
    }
}

#endregion

#region MyInfo Reference 이후 Commmons에 이전요망

[System.Serializable]
public struct MySavedInfo
{
    public int s_UserDBMainIDX;
    public int s_NickName; //저장한 닉네임
    public MySavedItemInfo[] s_SavedInvenItemKeys;
    public MySavedItemInfo[] s_SavedAmountItemKeys;
    public MySavedMapReference[] s_SavedMapItemKeys;
    //25-01 새로적용한것들 (변경가능성 존재한다)
    public int stest_SavedTotalScore; //각 MyInfo를 정의하여 해당 구간에 적용하는게 원래 운영 원칙이다
    public UserInfo s_SavedUserDatas;
    public SoundGameSavedData s_SavedSoundGameInfo;
    //public RunnerPlayInfo s_SavedRunnerPlayInfo; //Runner Project Reference
    //public TYCComprehensiveInfo s_SavedTyconDatas; //Tycon Project Reference

}

[System.Serializable]
public struct MySavedItemInfo : I_Data
{
    public int s_SavedItemIDX;
    public int s_SavedItemCount;
    public ItemType s_SavedItemType;
}

[System.Serializable]
public struct MySavedMapReference : I_Data
{
    public int s_SavedItemIDX; 
    public int s_PrograssIDX; //저장한 진행도
    public int s_HP, s_MP; //저장한 체력 마나
}

[System.Serializable]
public struct MyInGameOutPutInfo //여긴 중복되지 않는 아이템값만 포함이라는걸 명심해야된다
{
    public ItemInfo[] s_InvenInfos;
    public PartsItemInfo[] s_AmountInfos;
}
#endregion