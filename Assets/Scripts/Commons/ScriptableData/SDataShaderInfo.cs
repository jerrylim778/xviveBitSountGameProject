using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Commons;
using Commons.Helpers;
using Sirenix.OdinInspector;


[CreateAssetMenu(fileName = nameof(SDataShaderInfo),
menuName = "ScriptableObject/" + nameof(SDataShaderInfo), order = int.MaxValue)]
public class SDataShaderInfo : ScriptableCommon
{
    [SerializeField] private RenewalShaderInfo test_RShaderInfo;
    [SerializeField, InlineProperty, HideLabel, ValueDropdown("GetClassListAsDropdown")] private List<NewShaderInfo> m_SItemShaderInfo = new List<NewShaderInfo>();

    private IEnumerable<ValueDropdownItem<NewShaderInfo>> GetClassListAsDropdown()// 드롭다운에 표시할 리스트를 반환하는 함수
    {
        foreach (var item in m_SItemShaderInfo)
            yield return new ValueDropdownItem<NewShaderInfo>($"{item.s_ItemIDX} (Value: {item.s_ItemName})", item);
    }

    public override ItemType ReciverGetType(ItemType _CompareType = ItemType.None) => ItemType.ShaderType;

    public override void Initialization(ScriptableObjectManager _GetManager)
    {
        m_ScriptableObjectManager = _GetManager;
        pp_CheckErrorStack = new List<string>();
        pp_DefultItemInfos = new List<ItemInfo>();
        pp_DefultItemInfos.AddRange(m_SItemShaderInfo);
    }

    #region Sub System Public Functions

    public override void DestroyElementByDB()
    {
        List<NewShaderInfo> ReAddArtificialType =
        m_SItemShaderInfo.FindAll(x => x.s_ScriptableDataType == ScriptableDataType.Artificial);
        m_SItemShaderInfo?.Clear(); pp_CheckErrorStack?.Clear(); pp_DefultItemInfos?.Clear();
        m_SItemShaderInfo = ReAddArtificialType;
        pp_DefultItemInfos?.AddRange(m_SItemShaderInfo);
    }

    public override void UpdateProcess()
    {
        if (pp_DefultItemInfos.Count > m_SItemShaderInfo.Count)
        {
            Debug.LogError("각 데이터의 맵 리스트보다 추가할 맵 리스트의 크기가 클 수 없습니다..!!");
            return;
        }

        if (pp_DefultItemInfos.Count < m_SItemShaderInfo.Count)
        {
            pp_DefultItemInfos.ForEach(x =>
            {
                int FIDX = -1; FIDX = m_SItemShaderInfo.FindIndex(y => y.s_ItemIDX == x.s_ItemIDX);
                if (FIDX != -1)
                {
                    NewShaderInfo GetInfo = m_SItemShaderInfo[FIDX];
                    Debug.LogWarningFormat("중간에 추가되지 않는 아이템을 발견하여 적용합니다..!! {0}::{1}",
                    GetInfo.s_ItemIDX, GetInfo.s_ItemName);
                    pp_DefultItemInfos.Add(GetInfo);
                    m_ScriptableObjectManager.CheckScriptableKeyDepuil();
                }
            });
        }
    }

    #endregion

    #region Sub System Protected Functions (By Shaderinfos)

    protected override string ApplyErrorMessageByParsingItem(ItemInfo _GetInfo, bool _isAddParents)
    {
        string SetErrorMge = string.Empty;
        NewShaderInfo GetInfos = (_GetInfo as NewShaderInfo);
        SetErrorMge = base.ApplyErrorMessageByParsingItem(_GetInfo, _isAddParents);
        if (GetInfos.s_SPType == ShaderPlayerType.None) SetErrorMge += "비교할 ShaderPlayerType 값, ";
        if (!string.IsNullOrEmpty(SetErrorMge)) pp_CheckErrorStack.Add(
        string.Format("아이템 키 {0} 에서 {1} 가 없습니다...!!", _GetInfo.s_ItemIDX, SetErrorMge));
        return SetErrorMge;
    }

    #endregion

    public override void InPutDataByItem(string _TableElement, out bool _isComplateReciverInData)
    {
        NewShaderInfo ApplyShaderInfo = new NewShaderInfo();
        string[] SplitTables = _TableElement.Split(',');
        ApplyShaderInfo.s_ItemIDX = int.Parse(SplitTables[0]);
        ApplyShaderInfo.s_ItemName = SplitTables[1];
        ApplyShaderInfo.s_ItemType = Helper.StringToEnum<ItemType>(SplitTables[2]);
        ApplyShaderInfo.s_ScriptableDataType = Helper.StringToEnum<ScriptableDataType>(SplitTables[3]);
        ApplyShaderInfo.s_LoadObjectParsingType = Helper.StringToEnum<SDataLoadParsingType>(SplitTables[4]);

        ApplyShaderInfo.s_Descirption = SplitTables[5];
        string[] GetSprArray = Helper.SplitStrValue(SplitTables[6], (object)'?');
        int CountIDX = 0; ApplyShaderInfo.s_SpriteArray = new Sprite[GetSprArray.Length];
        GetSprArray.ToList().ForEach(x =>
        {
            ApplyShaderInfo.s_SpriteArray[CountIDX] = base.LoadType<Sprite>(ApplyShaderInfo.s_LoadObjectParsingType, x);
            CountIDX++;
        });

        ApplyShaderInfo.s_PrefabObjs = base.LoadType<GameObject>(ApplyShaderInfo.s_LoadObjectParsingType, SplitTables[7]);

        //해당 부분에 SShaderInfo의 정보를 Apply할 수 있도록 수정한다

        //2. DataManager<클래스>나 그밖에 해당 클래스에서 MapInfo를 저장했다면 즉시 전부 관련된 사항 일일메모장 확인해서 수정하기
        //3. TableManager<클래스> 리팩토링 실시할것
        ApplyErrorMessageByParsingItem(ApplyShaderInfo, false);
        m_SItemShaderInfo.Add(ApplyShaderInfo);
        pp_DefultItemInfos.Add(m_SItemShaderInfo[m_SItemShaderInfo.Count - 1]);

        _isComplateReciverInData = true;
    }
}
