using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Commons.Helpers;
using Sirenix.OdinInspector;

[CreateAssetMenu(fileName = nameof(SDataMapInfo), 
menuName = "ScriptableObject/" + nameof(SDataMapInfo), order = int.MaxValue)]
public class SDataMapInfo : ScriptableCommon
{
    //전부 Helper로부터 끌어올 수 있도록 수정 (이미 Helper.AssetPackages 에 정의함 사용하는 다른걸 참고하여 변경 요망,,) ***********************************
    [SerializeField, InlineProperty, HideLabel, ValueDropdown("GetClassListAsDropdown")] private List<MapInfo> m_SItemMapInfo = new List<MapInfo>();
    
    private IEnumerable<ValueDropdownItem<MapInfo>> GetClassListAsDropdown()// 드롭다운에 표시할 리스트를 반환하는 함수
    {
        foreach (var item in m_SItemMapInfo)
            yield return new ValueDropdownItem<MapInfo>($"{item.s_ItemIDX} (Value: {item.s_ItemName})", item);
    }

    public override ItemType ReciverGetType(ItemType _CompareType = ItemType.None) => ItemType.MapType;

    public override void Initialization(ScriptableObjectManager _GetManager)
    {
        m_ScriptableObjectManager = _GetManager;
        pp_CheckErrorStack = new List<string>();
        pp_DefultItemInfos = new List<ItemInfo>();
        pp_DefultItemInfos.AddRange(m_SItemMapInfo);
    }

    #region Sub System Public Functions

    public override void DestroyElementByDB()
    {
        List<MapInfo> ReAddArtificialType =
        m_SItemMapInfo.FindAll(x => x.s_ScriptableDataType == ScriptableDataType.Artificial);
        m_SItemMapInfo?.Clear(); pp_CheckErrorStack?.Clear(); pp_DefultItemInfos?.Clear();
        m_SItemMapInfo = ReAddArtificialType;
        pp_DefultItemInfos?.AddRange(m_SItemMapInfo);
    }

    public override void UpdateProcess()
    {
        if(pp_DefultItemInfos.Count > m_SItemMapInfo.Count)
        {
            Debug.LogError("각 데이터의 맵 리스트보다 추가할 맵 리스트의 크기가 클 수 없습니다..!!");
            return;
        }

        if (pp_DefultItemInfos.Count < m_SItemMapInfo.Count)
        {
            pp_DefultItemInfos.ForEach(x =>
            {
                int FIDX = -1; FIDX = m_SItemMapInfo.FindIndex(y => y.s_ItemIDX == x.s_ItemIDX);
                if (FIDX != -1)
                {
                    MapInfo GetMapInfo = m_SItemMapInfo[FIDX];
                    Debug.LogWarningFormat("중간에 추가되지 않는 아이템을 발견하여 적용합니다..!! {0}::{1}",
                    GetMapInfo.s_ItemIDX, GetMapInfo.s_ItemName);
                    pp_DefultItemInfos.Add(GetMapInfo);
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
        MapInfo GetInfos = (_GetInfo as MapInfo);
        SetErrorMge = base.ApplyErrorMessageByParsingItem(_GetInfo, _isAddParents);
        if (GetInfos.s_MainDefultVolumeProfile == null) SetErrorMge += "인스턴싱할 URPVolume 값, ";
        if (GetInfos.s_MainDirectionalLight == null) SetErrorMge += "인스턴싱할 라이트 값, ";
        if (GetInfos.s_SkyBox == null) SetErrorMge += "인스턴싱할 스카이 박스 값, ";
        if (!string.IsNullOrEmpty(SetErrorMge)) pp_CheckErrorStack.Add(
        string.Format("아이템 키 {0} 에서 {1} 가 없습니다...!!", _GetInfo.s_ItemIDX, SetErrorMge));
        return SetErrorMge;
    }

    #endregion

    public override void InPutDataByItem(string _TableElement, out bool _isComplateReciverInData)
    {
        #region 이전 전부 파싱 로직
        //MapInfo ApplyMapInfo = new MapInfo();
        //string[] SplitTables = _TableElement.Split(',');
        //ApplyMapInfo.s_ItemIDX = int.Parse(SplitTables[0]);
        //ApplyMapInfo.s_ItemName = SplitTables[1];
        //ApplyMapInfo.s_ItemType = Helper.StringToEnum<ItemType>(SplitTables[2]);
        //ApplyMapInfo.s_ScriptableDataType = Helper.StringToEnum<ScriptableDataType>(SplitTables[3]);
        //ApplyMapInfo.s_LoadObjectParsingType = Helper.StringToEnum<SDataLoadParsingType>(SplitTables[4]);

        //ApplyMapInfo.s_Descirption = SplitTables[5];
        //string[] GetSprArray = Helper.SplitStrValue(SplitTables[6], (object)'?');
        //int CountIDX = 0; ApplyMapInfo.s_SpriteArray = new Sprite[GetSprArray.Length];
        //GetSprArray.ToList().ForEach(x =>
        //{
        //    ApplyMapInfo.s_SpriteArray[CountIDX] =  base.LoadType<Sprite>(ApplyMapInfo.s_LoadObjectParsingType, x);
        //    CountIDX++;
        //});

        //ApplyMapInfo.s_PrefabObjs = base.LoadType<GameObject>(ApplyMapInfo.s_LoadObjectParsingType, SplitTables[7]);

        //================================================

        //ApplyMapInfo.s_isOnlyInit = bool.Parse(SplitTables[8]);
        //ApplyMapInfo.s_isUseStaticScene = bool.Parse(SplitTables[9]);
        //ApplyMapInfo.s_MainDefultVolumeProfile =
        //base.LoadType<UnityEngine.Rendering.VolumeProfile>(ApplyMapInfo.s_LoadObjectParsingType, SplitTables[10]);
        //ApplyMapInfo.s_MainDirectionalLight = base.LoadType<Light>(ApplyMapInfo.s_LoadObjectParsingType, SplitTables[11]);
        //ApplyMapInfo.s_SkyBox = base.LoadType<Material>(ApplyMapInfo.s_LoadObjectParsingType, SplitTables[12]);
        //Vector3 GetColorInfo = Helper.GetStringToVectorByTable(SplitTables[13]);
        //ApplyMapInfo.s_FogColor = new Color(GetColorInfo.x, GetColorInfo.y, GetColorInfo.z, 1);
        //ApplyMapInfo.s_FogDensity = string.IsNullOrEmpty(SplitTables[14]) ? 0f : float.Parse(SplitTables[14]);
        #endregion

        MapInfo ApplyMapInfo = new MapInfo(); 
        base.BaseInPutDataByItem<MapInfo>(ApplyMapInfo, _TableElement, out string[] SplitTables);

        ApplyMapInfo.s_isOnlyInit = bool.Parse(SplitTables[0]);
        ApplyMapInfo.s_isUseStaticScene = bool.Parse(SplitTables[1]);
        ApplyMapInfo.s_MainDefultVolumeProfile =
        base.LoadType<UnityEngine.Rendering.VolumeProfile>(ApplyMapInfo.s_LoadObjectParsingType, SplitTables[2]);
        ApplyMapInfo.s_MainDirectionalLight = base.LoadType<Light>(ApplyMapInfo.s_LoadObjectParsingType, SplitTables[3]);
        ApplyMapInfo.s_SkyBox = base.LoadType<Material>(ApplyMapInfo.s_LoadObjectParsingType, SplitTables[4]);
        Vector3 GetColorInfo = Helper.GetStringToVectorByTable(SplitTables[5]);
        ApplyMapInfo.s_FogColor = new Color(GetColorInfo.x, GetColorInfo.y, GetColorInfo.z, 1);
        ApplyMapInfo.s_FogDensity = string.IsNullOrEmpty(SplitTables[6]) ? 0f : float.Parse(SplitTables[6]);

        //2. DataManager<클래스>나 그밖에 해당 클래스에서 MapInfo를 저장했다면 즉시 전부 관련된 사항 일일메모장 확인해서 수정하기
        //3. TableManager<클래스> 리팩토링 실시할것
        ApplyErrorMessageByParsingItem(ApplyMapInfo, false); 
        m_SItemMapInfo.Add(ApplyMapInfo);
        pp_DefultItemInfos.Add(m_SItemMapInfo[m_SItemMapInfo.Count - 1]);
        
        _isComplateReciverInData = true;
    }
}
