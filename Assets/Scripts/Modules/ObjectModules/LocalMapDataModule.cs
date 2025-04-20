using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Commons.Helpers;
//using Commons.Helpers.Attribute;
using Sirenix.OdinInspector;

[DisallowMultipleComponent]
public class LocalMapDataModule : MonoBehaviour
{
    #region LocalMapDataModule에 관한 설명
    //각 MapInfo의 오브젝트 최상위에 부착되어 있으며
    //인스턴싱 하는 모듈에서 연출 및 진행도에 따른 보상 지점을 
    //가져올 수 있도록 설계된 구간
    //또한 해당 구역에서 연출되는 모듈 구조체 역시 관리될 수 있도록 수정한다
    #endregion
    [Header("Required Values")]
    //갔다와서 타입에 따라 정적맵데이터를 가지고 있다가 GamePlayManager<클래스>에 뿌릴지를 정해서
    //DataManager => GamePlayManager<클래스>의 데이터값을 가져올 수 있도록 수정한다
    //그이후에 Do it Again 버튼을 클릭시 다시 돌아올 수 있도록 수정한다 ************************************************************
    [SerializeField] private bool m_isStaticModule = false;
    #region OdinShowOrHide => isStaticModule
    public bool ISOdinToggleFalse() => !m_isStaticModule;
    #endregion
    [SerializeField, HideIf(nameof(ISOdinToggleFalse))] private System.UInt16 m_MapIDX;
    [SerializeField, HideIf(nameof(ISOdinToggleFalse))] private System.UInt16 m_SavedPrograssIDX;
    [SerializeField, HideIf(nameof(ISOdinToggleFalse))] private ProgressUpdateInfo[] m_ProgressByMapInfo; //이후에 ItemInfo로 스크립터블 데이터화를 유도해도됨
    [SerializeField, ShowIf(nameof(ISOdinToggleFalse))] private MapInfo m_StaticMapInfo;

    [Tooltip("Required Values")]
    private bool InitLMapResources = false;
    public bool pp_isNewEnterMap { get; private set; }
    public MapInfo pp_StaticMapInfo { get { return m_isStaticModule? m_StaticMapInfo : null; } }

    public LocalMapDataModule Initlization(MapInfo _GetMapInfo, MySavedMapReference? GetMapSavedInfo)
    {
        if ((m_MapIDX != _GetMapInfo.s_ItemIDX).HDebug("적용하고자 하는 맵 인덱스가 일치 하지 않습니다..!! " +
        $"{_GetMapInfo.s_ItemName} {m_MapIDX}", Helper.HDType.Error)) return null;

        bool isNullValue = GetMapSavedInfo == null || !GetMapSavedInfo.HasValue;
        m_SavedPrograssIDX = isNullValue ? (System.UInt16)0 : (System.UInt16)GetMapSavedInfo.Value.s_PrograssIDX;
        pp_isNewEnterMap = isNullValue;
        if (!_GetMapInfo.s_isOnlyInit && GetMapSavedInfo != null && GetMapSavedInfo.HasValue) UpdatePrograssEvent(true);
        if(!_GetMapInfo.s_isOnlyInit) 
        Appinstance.Instance.ms_DelEventManager.DEL_MemoryAllResetDELEvntBy_ChangeScene += FinalSaveByPrograss;
        InitLMapResources = true;
        return this;
    }

    private void FinalSaveByPrograss()
    {
        Appinstance.Instance.ms_MyInfo.SaveMyInfoByMapReference(pp_isNewEnterMap, new MySavedMapReference()
        {
            s_SavedItemIDX = m_MapIDX,
            s_PrograssIDX = m_SavedPrograssIDX,
            s_HP = 0, s_MP = 0
        });
        Appinstance.Instance.ms_DelEventManager.DEL_MemoryAllResetDELEvntBy_ChangeScene -= FinalSaveByPrograss;
    }

    #region Main Prograss System Public Functions

    public void RespowonPlayerPosByPrograss(NewPlayerCharacterController _GetPlayer, System.Action _EndCallBack)
    {
        if ((!InitLMapResources || !_GetPlayer.pp_isMine).
        HDebug("아직 초기화가 진행이 안됬거나 캐릭터가 내정보에서 파생된 캐릭터가 아닙니다.!", Helper.HDType.Error)) return;
        #region 연출이 완성되고 시작부분에 리스폰이 존재할때 해당 스폰로직을 연출쪽으로 넘길 수 있도록 수정 요망**********
        if (m_SavedPrograssIDX == 0) _EndCallBack?.Invoke();
        //if (m_SavedPrograssIDX == 0 && m_ProductionManager.GetFindProductionByType(ProductionType.StartType))
        //{
        //    m_ProductionManager.RespowonPlayerPosByPrograss(_GetPlayer);
        //    return;
        //}
            #endregion
        _GetPlayer.transform.SyncObjPosRot(m_ProgressByMapInfo[m_SavedPrograssIDX].s_RespownTr);
    }

    public void UpdatePrograssEvent(bool _InitValue = false)
    {
        if(_InitValue) for (int i = 0; i < m_SavedPrograssIDX + 1; i++)
        {
            m_ProgressByMapInfo[i].s_ActiveObjs.ToList().ForEach(x => x.gameObject.SetActive(true));
            m_ProgressByMapInfo[i].s_UnActiveObjs.ToList().ForEach(x => x.gameObject.SetActive(false));
        }
        else
        {
            m_SavedPrograssIDX++;
            m_ProgressByMapInfo[m_SavedPrograssIDX].s_ActiveObjs.ToList().ForEach(x => x.gameObject.SetActive(true));
            m_ProgressByMapInfo[m_SavedPrograssIDX].s_UnActiveObjs.ToList().ForEach(x => x.gameObject.SetActive(false));
        }

        ProgressUpdateInfo? PrograssNow = m_ProgressByMapInfo[m_SavedPrograssIDX];
        if (PrograssNow != null && PrograssNow.Value.s_isSetProgressModuleBase)
        {
            FindObjectsOfType<ProgressModuleBase>().ToList().FindAll(x =>
            x.pp_PrograssEventIDX == PrograssNow.Value.s_ProgressModuleBaseIDX).ForEach(y =>
            {
                if (!y.gameObject.activeSelf) y.gameObject.SetActive(true);
                y.Initlization(PrograssNow.Value.s_progressModuleValueStr); //문자열로 수정하는것 변경해야될 수 있음
            });
        }
    }
    #endregion
}

//진행도 다른것으로 바꿔야 할 수 도 있다
[System.Serializable]
public struct ProgressUpdateInfo
{
    public bool s_isSetProgressModuleBase;
    public int s_ProgressModuleBaseIDX;
    public string s_progressModuleValueStr;
    public Transform s_RespownTr;
    public GameObject[] s_ActiveObjs;
    public GameObject[] s_UnActiveObjs;
}


public abstract class ProgressModuleBase : MonoBehaviour
{
    [SerializeField] private int m_PrograssEventIDX = -1;
    public int pp_PrograssEventIDX {get => m_PrograssEventIDX;}

    public abstract void Initlization(string _ActionOrderStr);
}
