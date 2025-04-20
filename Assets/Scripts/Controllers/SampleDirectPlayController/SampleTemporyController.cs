using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using PlayConditionType = GamePlayManager.PlayConditionType;
using Commons.Helpers;
using Sirenix.OdinInspector;

public class SampleTemporyController : ControllerBase, I_SubModulesCollection
{
    [Header("Temp Game Rule Values")] //방대해질경우 전용 DataManager로 이전요망
    private int m_CurrentLevel;

    [SerializeField, ReadOnly] List<SubModuleControllerBase> m_GetMainSubControllerBases = new();

    [Tooltip("Required Values")]
    private int m_SubModuleInitCount;
    private System.Action<PlayConditionType> m_MainConditionCallBack;

    //Property
    public bool pp_isSubModulesAllComplate
    { get => m_SubModuleInitCount >= m_GetMainSubControllerBases.Count + 1 /*1은 자기자신*/; }

    public Transform pp_StartPointTr
    {
        get => GamePlaySystem.Instance != null ?
        GamePlaySystem.Instance.pp_StartCharacterTr : Helper.ChildLinearStuctureSearch(this.transform).HFind(x => x.name.Contains("PlayerStartPoint"));
    }

    public override object[] Initlization(params object[] _ParsingParams)
    {
        TemporyApplySubControllerInfos TempASCInfo = default(TemporyApplySubControllerInfos);
        CheckAndApplyByParams<MyInfo>(_ParsingParams, x => m_ReciveMyInfo = x);
        CheckAndApplyByParams<System.Action<PlayConditionType>>(_ParsingParams, x => m_MainConditionCallBack = x);
        CheckAndApplyByParams<TemporyApplySubControllerInfos>(_ParsingParams, x => TempASCInfo = x);
        #region Only TestMode Init
        if (Appinstance.Instance != null) //전부 MyInfo에서 처리해서 가져와야함
        {
            if (m_ReciveMyInfo == null)
            {
                m_ReciveMyInfo = new MyInfo(Appinstance.Instance, "TestLoginKey");
                m_CurrentLevel = 1;
            }

            if (!TempASCInfo.s_PairParamDatas.CheckArrayNull(0) || !TempASCInfo.s_PairParamDatas.HExist(x => x.Item1 is
             RenewalCinemachineCameraController) || FindAnyObjectByType<RenewalCinemachineCameraController>() != null)
            {
                TempASCInfo.s_PairParamDatas = new();
                I_PurifiedParamsData GetParamsModel = null;
                var GetRCamController = Appinstance.Instance.ms_DataManager.
                GetCompareComponent<RenewalCinemachineCameraController>(true, CameraController.ViewType.TPS.ToString(), ref GetParamsModel);
                TempASCInfo.s_PairParamDatas.Add(new(GetRCamController, GetParamsModel));
            }
        }
        #endregion
        #region Data Apply to Controller Init
        //TempASCInfo.s_PairParamDatas.Add(new(m_FlyKnifeModule, null));
        //SubModuleControllerBase Init 
        var GetCallBacks = new System.Action[TempASCInfo.s_PairParamDatas.Count];
        int CountIDX = 0; TempASCInfo.s_PairParamDatas.ForEach(x =>
        {
            var GetSubModuleController =
            x.Item1.pp_isInstanceOnlyOnce ? x.Item1 : Instantiate(x.Item1, this.transform);
            //if (GetSubModuleController is I_FlyKnifeSliceModule) pp_InstanceFlyKnifeModule = GetSubModuleController;
            m_GetMainSubControllerBases.Add(GetSubModuleController);
            GetCallBacks[CountIDX] = () => GetSubModuleController.Initlization(this, x.Item2);
            CountIDX++;
        });
        GetCallBacks.HForEach(x => x());
        #endregion
        #region GUI init(**TestOnly**)//TestOnly (**GUI로 이전 요망**)
        //if (!m_isTestMode)
        //{
        //    m_SinalDownArrowCG.gameObject.SetActive(false);
        //    test_RequiredSlicePopUpBG.gameObject.SetActive(true);
        //}
        //m_ReStartBtn.onClick.AddListener(ResetStartControl);
        #endregion
        ////들어오는 레벨에 따라서 달라질 수 있음
        //ParsingAndBachMapModule(m_CurrentLevel);

        I_CheckSubModuleIsAllCanAction(this);
        return null;
    }

    #region Sub System Public Functions (**Interface Process**)

    public void I_CheckSubModuleIsAllCanAction(ControllerBase _GetSubBase)
    {
        //�� �ڽŸ� �ش��
        if (!pp_isMine || (!this.Equals(_GetSubBase) &&
        (_GetSubBase is SubModuleControllerBase _GetCastSubBase && !m_GetMainSubControllerBases.Contains(_GetCastSubBase))).HDebug(
        $"{_GetSubBase}�� ���� {this.gameObject}�� ������ ���� �ʴ� {nameof(SubModuleControllerBase)} �Դϴ�.!")) return;

        m_SubModuleInitCount++;
        if (pp_isSubModulesAllComplate)
        {

            if (m_isTestMode) { ForcePlayOrStopOrder(true); return; }
            if (m_MainConditionCallBack == null)
            {
                Appinstance.Instance.ms_GamePlayManager.ExecuteChangeAllGameControll(GamePlayManager.GamePlayType.Play);
                return;
            }
            m_MainConditionCallBack(GamePlayManager.PlayConditionType.StartingProudctionDone);
            m_MainConditionCallBack = null;
        }
    }

    public T I_GetSubModule<T>() where T : SubModuleControllerBase
    => ISCheckSubModule<T>(out T _OutPutBase) ? _OutPutBase : null;

    public bool ISCheckSubModule<T>(out T _OutPutBase) where T : SubModuleControllerBase
    {
        _OutPutBase = null; int FIDX = -1; FIDX =
        m_GetMainSubControllerBases.HFindIndex(x => x.GetType().Name == typeof(T).Name);
        if (FIDX != -1)
        {
            _OutPutBase = m_GetMainSubControllerBases[FIDX] as T;
            return true;
        }
        return false;
    }
    #endregion

    #region 유니티 이벤트 함수
    void Start()
    {
        if (!m_isTestMode) return;
        StartCoroutine("TestCO_WaitAppInstanceStart");
    }

    //정석으로 했을때 전부 삭제할것
    IEnumerator TestCO_WaitAppInstanceStart()
    {
        yield return new WaitUntil(() => Appinstance.Instance.didStart);
        Initlization();
    }

    protected override void Update()
    {
        if (!m_isActionProcess) return;

        m_GetMainSubControllerBases.HForEach(x => x.ProcessUpdate());
        m_GetMainSubControllerBases.HForEach(x => x.ProcessUpdate(Time.deltaTime));
    }
    #endregion  
}

[System.Serializable]
public struct TemporyApplySubControllerInfos : ControllerBase.I_PurifiedCData
{
    public int m_PlayerLevel; //판이 달라질수록 초기화 되며 저장시 MyInfo에 이전될 수 있도록 수정한다
    //또한 레벨에 따라 업그데이드 한 지점을 추상화를 통해 들고 있을 수 있도록 수정한다
    public List<System.Tuple<SubModuleControllerBase,
    ControllerBase.I_PurifiedParamsData>> s_PairParamDatas;
}