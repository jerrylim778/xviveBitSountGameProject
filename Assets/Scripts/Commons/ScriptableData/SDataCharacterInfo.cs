using System.Collections.Generic;
using UnityEngine;
using Commons;
using Commons.Helpers;
using Sirenix.OdinInspector;


[CreateAssetMenu(fileName = nameof(SDataCharacterInfo),
menuName = "ScriptableObject/" + nameof(SDataCharacterInfo), order = int.MaxValue)]
public class SDataCharacterInfo : ScriptableCommon
{
    #region ODIN_OutPut Item List
    private IEnumerable<ValueDropdownItem<CharacterItemInfo>> GetClassListAsDropdown()// 드롭다운에 표시할 리스트를 반환하는 함수
    {
        foreach (var item in m_SItemCharacterInfo)
            yield return new ValueDropdownItem<CharacterItemInfo>($"{item.s_ItemIDX} (Value: {item.s_ItemName})", item);
    }

    [Button("ODIN_AddDefultCharacterInfo")]
    private void ODIN_AddDefultCharacterInfo()
    => m_SItemCharacterInfo.HODIN_AddItemByList<CharacterItemInfo>();
    #endregion
    [SerializeField, InlineProperty, HideLabel, ValueDropdown("GetClassListAsDropdown")] private List<CharacterItemInfo> m_SItemCharacterInfo = new List<CharacterItemInfo>();

    public override ItemType ReciverGetType(ItemType _CompareType = ItemType.None) => ItemType.CharacterType;

    public override void Initialization(ScriptableObjectManager _GetManager)
    {
        m_ScriptableObjectManager = _GetManager;
        pp_CheckErrorStack = new List<string>();
        pp_DefultItemInfos = new List<ItemInfo>();
        pp_DefultItemInfos.AddRange(m_SItemCharacterInfo);
    }

    #region Sub System Public Functions

    public override void DestroyElementByDB()
    {
        List<CharacterItemInfo> ReAddArtificialType =
        m_SItemCharacterInfo.FindAll(x => x.s_ScriptableDataType == ScriptableDataType.Artificial);
        m_SItemCharacterInfo?.Clear(); pp_CheckErrorStack?.Clear(); pp_DefultItemInfos?.Clear();
        m_SItemCharacterInfo = ReAddArtificialType;
        pp_DefultItemInfos?.AddRange(m_SItemCharacterInfo);
    }

    public override void UpdateProcess()
    {
        if (pp_DefultItemInfos.Count > m_SItemCharacterInfo.Count)
        {
            Debug.LogError("각 데이터의 맵 리스트보다 추가할 맵 리스트의 크기가 클 수 없습니다..!!");
            return;
        }

        if (pp_DefultItemInfos.Count < m_SItemCharacterInfo.Count)
        {
            pp_DefultItemInfos.ForEach(x =>
            {
                int FIDX = -1; FIDX = m_SItemCharacterInfo.FindIndex(y => y.s_ItemIDX == x.s_ItemIDX);
                if (FIDX != -1)
                {
                    CharacterItemInfo GetCInfo = m_SItemCharacterInfo[FIDX];
                    Debug.LogWarningFormat("중간에 추가되지 않는 아이템을 발견하여 적용합니다..!! {0}::{1}",
                    GetCInfo.s_ItemIDX, GetCInfo.s_ItemName);
                    pp_DefultItemInfos.Add(GetCInfo);
                    m_ScriptableObjectManager.CheckScriptableKeyDepuil();
                }
            });
        }
    }

    #endregion

    #region Sub System Protected Functions (By Mapinfos)

    protected override string ApplyErrorMessageByParsingItem(ItemInfo _GetInfo, bool _isAddParents)
    {
        string SetErrorMge = string.Empty;
        CharacterItemInfo GetInfos = (_GetInfo as CharacterItemInfo);
        SetErrorMge = base.ApplyErrorMessageByParsingItem(_GetInfo, _isAddParents);
        if (GetInfos.s_CharacterType == CharacterType.None) SetErrorMge += $"인스턴싱할 {nameof(CharacterType)} 값, ";
        if (string.IsNullOrEmpty(GetInfos.s_NewActionInfoIDX.ToString())) SetErrorMge += $"인스턴싱할 캐릭터 {nameof(NewActionInfo)}의 키값, ";
        if (string.IsNullOrEmpty(GetInfos.s_CalculateInfoIDX.ToString())) SetErrorMge += $"인스턴싱할 캐릭터 {nameof(CalculateInfo)}의 키값, ";
        //if (string.IsNullOrEmpty(GetInfos.s_InputPowerIDX.ToString())) SetErrorMge += $"인스턴싱할 캐릭터 {nameof(InputDataInfo)}의 키값, ";
        if (!string.IsNullOrEmpty(SetErrorMge)) pp_CheckErrorStack.Add(
        string.Format("아이템 키 {0} 에서 {1} 가 없습니다...!!", _GetInfo.s_ItemIDX, SetErrorMge));
        return SetErrorMge;
    }

    #endregion

    public override void InPutDataByItem(string _TableElement, out bool _isComplateReciverInData)
    {
        CharacterItemInfo ApplyCItemInfo = new CharacterItemInfo();
        base.BaseInPutDataByItem<CharacterItemInfo>(ApplyCItemInfo, _TableElement, out string[] SplitTables);

        ApplyCItemInfo.s_CharacterType = Helper.StringToEnum<CharacterType>(SplitTables[0]);
        ApplyCItemInfo.s_CharacterUsePowerType = Helper.StringToEnum<Commons.PowerType>(SplitTables[1]);
        ApplyCItemInfo.s_NewActionInfoIDX = int.Parse(SplitTables[1]); 
        ApplyCItemInfo.s_CalculateInfoIDX = int.Parse(SplitTables[2]); 
        //ApplyCItemInfo.s_InputPowerIDX = int.Parse(SplitTables[3]); 

        //2. DataManager<클래스>나 그밖에 해당 클래스에서 MapInfo를 저장했다면 즉시 전부 관련된 사항 일일메모장 확인해서 수정하기
        //3. TableManager<클래스> 리팩토링 실시할것
        ApplyErrorMessageByParsingItem(ApplyCItemInfo, false);
        m_SItemCharacterInfo.Add(ApplyCItemInfo);
        pp_DefultItemInfos.Add(m_SItemCharacterInfo[m_SItemCharacterInfo.Count - 1]);

        _isComplateReciverInData = true;
    }
}