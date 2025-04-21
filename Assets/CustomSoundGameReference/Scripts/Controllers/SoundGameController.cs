using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using PlayConditionType = GamePlayManager.PlayConditionType;
using Commons.Helpers;
using Sirenix.OdinInspector;
using DG.Tweening;

public class SoundGameController : ControllerBase, I_SubModulesCollection, I_OwnerShip
{
    [Header("Test Control Values")]
    [SerializeField, ShowIf(nameof(m_isTestMode))] private bool m_isUsedStartEvent;

    [Header("Temp Game Rule Values")] //방대해질경우 전용 DataManager로 이전요망
    private int m_CurrentLevel;

    [Header("Control Values")]
    [SerializeField] private bool m_isOutPutOldVer;
    [SerializeField] private float m_AverageTweeningSped;

    [Header("Required Values")]
    [SerializeField] private Transform m_MapModuleHeaderTr;
    [SerializeField] private OutPutAudioEqulizeModule m_WPSticksEqulizer;
    [SerializeField, ReadOnly] private RequiredSoundGamePopUp m_RequiredSoundGamePopUp;
    [SerializeField, ReadOnly] List<SubModuleControllerBase> m_GetMainSubControllerBases = new();
    [SerializeField, ReadOnly] List<ElemSinngerSubController> m_GetCurrSinngerSubControllers = new();

    [Header("Sound Level Values")]
    [SerializeField, ReadOnly] private AudioASInfo m_CurrAudioASinfo;
    [SerializeField, ReadOnly] private ElemASBuffer m_CurrElemASBuffer;
    [SerializeField] private SDataSoundGameInfo m_SDataSoundGameInfo;
    private AlbumInfo m_CurrAlbumInfo; //전용 MyInfo를 만들어 이전 요망 **

    [Tooltip("Required Values")]
    //private bool m_isFirst;
    private int m_SubModuleInitCount;
    private float m_CurrDSPRunTime, m_DirBeforeCycleCurrTime;
    private double m_DSPTimeNow, m_PauseStartTime, m_TotalPausedDuration;
    private System.Action<PlayConditionType> m_MainConditionCallBack;

    //Property
    public bool pp_isOutPutOldVer => m_isOutPutOldVer;
    public bool pp_isMusicOn { get; private set; }
    public float pp_CycleCurrTime { get; private set; }
    public float pp_CycleMaxTime { get; private set; } 
    public RequiredSoundGamePopUp pp_RequiredSoundGamePopUp => m_RequiredSoundGamePopUp;
    public SpriteRenderer pp_MainMapModuleBase => m_MapModuleHeaderTr.GetChild(0).GetComponent<SpriteRenderer>();
    public bool pp_isSubModulesAllComplate
    { get => m_SubModuleInitCount >= m_GetMainSubControllerBases.Count + 1 /*1은 자기자신*/ + 1 /*1은 필수 팝업*/; }
    public Transform pp_MainMapModuleByVersion => Helper.ChildLinearStuctureSearch(m_MapModuleHeaderTr).
    HFind(x => x.gameObject.name.Contains(m_isOutPutOldVer? "Old" : "New"));

    public override object[] Initlization(params object[] _ParsingParams)
    {
        TemporyApplySubControllerInfos TempASCInfo = default(TemporyApplySubControllerInfos);
        
        CheckAndApplyByParams<MyInfo>(_ParsingParams, x => m_ReciveMyInfo = x); //이후 SoundGameController전용 MyInfo를 만들어서 해당구역에서 ItemInfo를 추출할것
        CheckAndApplyByParams<AlbumInfo>(_ParsingParams, x => m_CurrAlbumInfo = x);
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

            if(!TempASCInfo.s_PairParamDatas.CheckArrayNull(0) || !TempASCInfo.s_PairParamDatas.HExist(x => x.Item1 is
             AudioListenrtRecordSubController) || FindAnyObjectByType<AudioListenrtRecordSubController>() != null)
            {
                TempASCInfo.s_PairParamDatas = new();
                TempASCInfo.s_PairParamDatas.Add(new(
                FindAnyObjectByType<SimpleCameraController>().GetComponent<AudioListenrtRecordSubController>(), null));
                TempASCInfo.s_PairParamDatas.Add(new(
                FindAnyObjectByType<SimpleCameraController>().GetComponent<CameraProductionSubController>(), null));
            }
        }
        #endregion
        #region Map Module Parsing Init 
        //m_SDataSoundGameInfo.SubDScriptableInit();
        var ApplyAudioMixer = this.transform.ChildLinearStuctureSearch<AudioMixVisualizeSubController>()[0];
        TempASCInfo.s_PairParamDatas.Add(new(ApplyAudioMixer, null));

        pp_MainMapModuleByVersion.ChildLinearStuctureSearch<ElemSinngerSubController>().HForEach(x =>
        {
            m_GetCurrSinngerSubControllers.Add(x);
            TempASCInfo.s_PairParamDatas.Add(new(x, null));
        });

        #endregion
        #region Data Apply to Controller Init
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
        #region Support Part Of Module Before Init
        pp_isMusicOn = true;

        //Init Required PopUp
        m_RequiredSoundGamePopUp = GamePlaySystem.Instance.ExecutePopUpStack<RequiredSoundGamePopUp>();
        m_RequiredSoundGamePopUp.InitRequiredSoundGamePopUp(this, m_SDataSoundGameInfo.pp_DSSoundgameClipInfos.ToArray());

        //들어오는 레벨에 따라서 달라질 수 있음
        //ParsingAndBachMapModule(m_CurrentLevel)

        //Init DSPTime
        var GetClipInfos = m_SDataSoundGameInfo.pp_DSSoundgameClipInfos.
        Find(x => x.s_AudioClipsInfo.CheckArrayNull(0));
        m_DSPTimeNow = AudioSettings.dspTime;
        pp_CycleMaxTime = GetClipInfos.s_AudioClipsInfo[0].length * 60f;

        #endregion

        StartCoroutine(CO_WaitProduction(0.05f, () => 
        {
            if (m_isOutPutOldVer) OldProductionSoundGameController();
            else StartCoroutine(NewProductionSoundGameController());
        }));
        return null;
    }

    public override void ForcePlayOrStopOrder(bool _isPlay)
    {
        base.ForcePlayOrStopOrder(_isPlay);
        m_WPSticksEqulizer.SemiBreakPoint(true);
        m_RequiredSoundGamePopUp.BreakPoint(_isPlay);
        m_GetMainSubControllerBases.ForEach(x => x.BreakPoint(_isPlay));
    }

    private void ResetStartControl()
    {
        ResetMainAudioClipTime();
    }

    private void ResetMainAudioClipTime()
    {
        m_GetCurrSinngerSubControllers.ForEach(x => x.AllRestart());
        m_DSPTimeNow = AudioSettings.dspTime;
        m_TotalPausedDuration = 0; m_PauseStartTime = 0;
        m_CurrDSPRunTime = 0; pp_CycleCurrTime = 0f;
        m_DirBeforeCycleCurrTime = 0f;
    }

    #region Production This Controller Reference

    IEnumerator CO_WaitProduction(float _WaitTime, System.Action _EndCallBack)
    {
        yield return new WaitForSeconds(_WaitTime);
        _EndCallBack();
    }
    //카메라 전환부터 진행할것 카메라 전환은 모두 ProductionCam에서 진행할것
    IEnumerator NewProductionSoundGameController()
    {
        #region Tweening Before Init 
        var MainSR = pp_MainMapModuleByVersion.GetChild(0).GetComponent<SpriteRenderer>();
        var MainParticle = pp_MainMapModuleByVersion.GetChild(1).GetComponent<ParticleSystem>();
        var ApplyMainICONPR = pp_MainMapModuleByVersion.GetChild(0).GetChild(0);
        //var ReturnMainOGScale = MainSR.transform.localScale;
        //MainSR.transform.localScale = ReturnMainOGScale * 0.2f;

        MainSR.color = Helper.SetChangeColorAlpha(MainSR.color, 0f);
        MainParticle.Stop();
        MainParticle.gameObject.SetActive(false);
        ApplyMainICONPR.gameObject.SetActive(false);
        m_WPSticksEqulizer.gameObject.SetActive(false);

        ApplyMainICONPR.GetChild(0).GetComponent<SpriteRenderer>().sprite = 
        m_CurrAlbumInfo.s_SpriteArray[0];
        //m_WPSticksEqulizer.transform.localScale = new Vector3(0f, 1f, 1f);

        MainParticle.gameObject.SetActive(true);
        if (MainParticle.isPlaying) MainParticle.Stop(); MainParticle.Play();

        #endregion

        yield return new WaitForSeconds(0.8f);

        MainSR.gameObject.SetActive(true);
        //MainSR.transform.DOScale(ReturnMainOGScale, 0.65f).SetEase(Ease.InOutSine);
        MainSR.DOColor(Helper.SetChangeColorAlpha(MainSR.color, 1f), 0.85f).SetEase(Ease.InOutSine).OnComplete(() =>
        {
            m_WPSticksEqulizer.gameObject.SetActive(true);
            InitAudioBaouns(true, m_CurrAlbumInfo, () => GamePlaySystem.Instance.MainPopUpPush<RequiredSoundGamePopUp>());
            ApplyMainICONPR.gameObject.SetActive(true);

            var GetLists = SplitListMiddleCount(m_GetCurrSinngerSubControllers);
            GetLists.HForEach(x => Helper.HCountForEach(0, x.Count - 1, _CountIDX =>
            {
                var MainSR = x[_CountIDX].GetComponent<SpriteRenderer>();
                var ApplySubDecorSR = x[_CountIDX].transform.GetChild(0).GetComponent<SpriteRenderer>();
                MainSR.color = Helper.SetChangeColorAlpha(MainSR.color, 0f);
                ApplySubDecorSR.color = Helper.SetChangeColorAlpha(ApplySubDecorSR.color, 0f);
                x[_CountIDX].gameObject.SetActive(true);
                x[_CountIDX].ElemSinngerOutPutInit(_CountIDX);
            }));
            I_CheckSubModuleIsAllCanAction(this);
        });
    }

    public void OldProductionSoundGameController()
    {
        //if (m_isFirst) return; m_isFirst = true;
        pp_MainMapModuleByVersion.gameObject.SetActive(true);
        var MainBG = pp_MainMapModuleByVersion.GetChild(0).GetComponent<SpriteRenderer>();
        var OGMLS = MainBG.transform.localScale;
        MainBG.transform.localScale = Vector3.right * 0.1f;
        MainBG.transform.DOScaleY(OGMLS.y, m_AverageTweeningSped).SetEase(Ease.InCirc).OnComplete(() =>
        MainBG.transform.DOScaleX(OGMLS.x, m_AverageTweeningSped - 0.1f).SetEase(Ease.InCirc).OnComplete(() =>
        {
            Helper.HCountForEach(0, m_GetCurrSinngerSubControllers.Count - 1, _CountIDX =>
            {
                m_GetCurrSinngerSubControllers[_CountIDX].gameObject.SetActive(true);
                m_GetCurrSinngerSubControllers[_CountIDX].ElemSinngerOutPutInit(_CountIDX);
            });
            GamePlaySystem.Instance.MainPopUpPush<RequiredSoundGamePopUp>();
            I_CheckSubModuleIsAllCanAction(this);
        }));
    }
    #endregion

    #region Main System Private Functions 

    //Sound Refereence
    public bool PlayingNow() //현재 전체 재생 일시 정지인지도 확인할것
    => m_GetCurrSinngerSubControllers.Exists(x => x.pp_isOn);

    public bool MusicPlayOrPause()
    {
        pp_isMusicOn = !pp_isMusicOn;

        if (pp_isMusicOn)
        {
            double pausedDuration = AudioSettings.dspTime - m_PauseStartTime;
            m_TotalPausedDuration += pausedDuration;
        }
        else m_PauseStartTime = AudioSettings.dspTime;

        m_GetCurrSinngerSubControllers.ForEach(x => { if (x.pp_isOn) x.BreakPoint(pp_isMusicOn); });

        return pp_isMusicOn;
    }

    private void InitAudioBaouns(bool _isAdd, AlbumInfo _CurrAlbumInfo = null, System.Action _EndCallBack = null)
    {
        if (_isAdd) m_CurrAudioASinfo = Appinstance.Instance.ms_AudioManager.PlaySound(true,
        _CurrAlbumInfo.m_MainClipBGM.s_isLoop, _CurrAlbumInfo.m_MainClipBGM.s_ItemName, 
        AudioType.BGM, _CurrAlbumInfo.m_MainClipBGM.s_AudioClipsInfo[0], this);

        var ApplyMixerCon = I_GetSubModule<AudioMixVisualizeSubController>();
        if (_isAdd)
        {
            m_CurrElemASBuffer = new ElemASBuffer(false, m_CurrAudioASinfo.s_AudioSource, _CurrAlbumInfo.m_MainClipBGM);
            ApplyMixerCon.AddAudioSource(m_CurrElemASBuffer);
        }
        else ApplyMixerCon.RemoveAudioSource(m_CurrElemASBuffer);

        
        if (_isAdd)
        {
            ModuleMonoBase ApplyMonoBase = null;
            m_WPSticksEqulizer.Initlization(ApplyMonoBase, ApplyMixerCon, _EndCallBack);
            //m_WPSticksEqulizer.SemiBreakPoint(true);
        }
        else
        {
            m_CurrAudioASinfo.StopOrComplate();
            ApplyMixerCon.InitOutPutAudioSpectrum(false, m_WPSticksEqulizer);
            m_CurrAudioASinfo = null;
            m_CurrElemASBuffer = null;
        }
    }

    #endregion

    #region Main System Private Functions (**Update Reference**)

    private void UpdateCycleTime()
    {
        if (!pp_isMusicOn) return;

        if (!PlayingNow())
        {
            ResetMainAudioClipTime();
            return;
        }

        if (pp_CycleCurrTime >= 1f) ResetMainAudioClipTime();

        if (m_CurrDSPRunTime <= 0.5f) ResetMainAudioClipTime();

        if(pp_CycleCurrTime - m_DirBeforeCycleCurrTime < 0) ResetMainAudioClipTime();

        System.TimeSpan GetTS = System.TimeSpan.FromSeconds(AudioSettings.dspTime - m_DSPTimeNow - m_TotalPausedDuration);
        float CalculateTime = (float)GetTS.TotalSeconds * 60;
        m_CurrDSPRunTime = Mathf.Abs(pp_CycleMaxTime - CalculateTime);
        pp_CycleCurrTime = Mathf.Abs(1 - (m_CurrDSPRunTime / pp_CycleMaxTime));
        //Debug.Log($"{pp_CycleCurrTime} :: {m_CurrDSPRunTime} :: {m_DSPTimeNow} :: {m_TotalPausedDuration} ");
        m_DirBeforeCycleCurrTime = pp_CycleCurrTime;
    }

    private void Update2DRayCast()
    {
        ElemSinngerSubController _GetOutController = null;

        #region 포인트 다운시 조건에 맞는 이벤트
        if (Input.GetMouseButtonDown(0) && UpdateMobileRayCast(ref _GetOutController) &&
        m_RequiredSoundGamePopUp.pp_SelectCurrModule == null)
        {
            m_GetCurrSinngerSubControllers.HForEach(x => x.ShowMuteModule(false));
            if(_GetOutController.pp_isOn) _GetOutController.ShowMuteModule(true);
        }
        #endregion

        #region 드래그 시 조건에 맞는 이벤트
        if (UpdateMobileRayCast(ref _GetOutController) &&
        m_RequiredSoundGamePopUp.pp_SelectCurrModule != null && !_GetOutController.pp_isOn)
        {
            _GetOutController.OffCharacterDragEvent(true);
            m_RequiredSoundGamePopUp.pp_SelectCurrModule.DragEvent(true);
        }
        else
        {
            if(m_RequiredSoundGamePopUp.pp_SelectCurrModule != null) 
            m_RequiredSoundGamePopUp.pp_SelectCurrModule.DragEvent(false);
            m_GetCurrSinngerSubControllers.ForEach(x => x.OffCharacterDragEvent(false));
        }
        #endregion

        #region 포인트 업시 조건에 맞는 이벤트
        if (Input.GetMouseButtonUp(0) && UpdateMobileRayCast(ref _GetOutController)) 
        {
            if (!m_RequiredSoundGamePopUp.ISGrapICONModule(out DataMonoMusicICONModule _GetModule)) return;

            if(!PlayingNow()) pp_CycleCurrTime = 0f;
            _GetOutController.ApplyAudioInfos(_GetModule);
        }
        #endregion

        #region 이전 실시간 탐색 로직
        //var GetWorldMousePoint = Camera.main.ScreenToWorldPoint(Input.mousePosition);

        //var GetHit2D = Physics2D.Raycast(GetWorldMousePoint, Vector2.zero);

        //if (GetHit2D.collider == null)
        //{
        //    m_GetCurrSinngerSubControllers.HForEach(x => x.ShowMuteModule(false));
        //    return;
        //}

        //if (GetHit2D.collider.gameObject.layer != LayerMask.NameToLayer("Wall") ||
        //!GetHit2D.collider.TryGetComponent<ElemSinngerSubController>(out ElemSinngerSubController _GetController))
        //{
        //    m_GetCurrSinngerSubControllers.HForEach(x => x.ShowMuteModule(false));
        //    return;
        //}


        //if (m_RequiredSoundGamePopUp.pp_SelectCurrModule == null && _GetController.pp_isOn)
        //    _GetController.ShowMuteModule(true);
        #endregion
    }

    private bool UpdateMobileRayCast(ref ElemSinngerSubController _GetOutController)
    {
        var GetWorldMousePoint = Camera.main.ScreenToWorldPoint(Input.mousePosition);

        var GetHit2D = Physics2D.Raycast(GetWorldMousePoint, Vector2.zero);

        if (GetHit2D.collider == null)
        {
            m_GetCurrSinngerSubControllers.HForEach(x => x.ShowMuteModule(false));
            return false;
        }

        if (GetHit2D.collider.gameObject.layer != LayerMask.NameToLayer("Wall") ||
        !GetHit2D.collider.TryGetComponent<ElemSinngerSubController>(out _GetOutController))
        {
            m_GetCurrSinngerSubControllers.HForEach(x => x.ShowMuteModule(false));
            return false;
        }

        if (_GetOutController == null) return false;

        return true;
    }

    #endregion

    #region Sub System Public Functions (**Interface Process**)

    public void I_CheckSubModuleIsAllCanAction(ControllerBase _GetSubBase)
    {
        if (!pp_isMine || (!this.Equals(_GetSubBase) &&
        (_GetSubBase is SubModuleControllerBase _GetCastSubBase && !m_GetMainSubControllerBases.Contains(_GetCastSubBase))).HDebug(
        $"불러들인 {_GetSubBase}은 {this.gameObject}객체내의 {nameof(SubModuleControllerBase)}가 아닙니다.!")) return;

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

    #region Sub System Private Functions 

    //Helpe 로 이전할것
    private List<List<T>> SplitListMiddleCount<T>(IList<T> _GetList) where T : class
    {
        bool _isCol = _GetList.Count % 2 == 0; //짝 홀
        int GetIDX = _isCol ?
        _GetList.Count / 2 : Mathf.RoundToInt(_GetList.Count / 2);
        var LHalfList = new List<T>(); var RHalfList = new List<T>();
        Helper.HCountForEach(0, GetIDX - 1, _CountIDX => LHalfList.Add(_GetList[_CountIDX]));
        Helper.HCountForEach(GetIDX, _isCol ? GetIDX * 2 - 1 : GetIDX * 2, _CountIDX => RHalfList.Add(_GetList[_CountIDX]));
        LHalfList.Reverse();
        return new List<List<T>>(2){ LHalfList, RHalfList };
    }

    #endregion

    #region 유니티 이벤트 함수
    void Start()
    {
        //m_MapModuleHeaderTr.gameObject.SetActive(false);
        Helper.ChildLinearStuctureSearch(pp_MainMapModuleByVersion).HForEach(x => x.gameObject.SetActive(false));
        if (!m_isTestMode || !m_isUsedStartEvent) return;
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

        Update2DRayCast();
        UpdateCycleTime();
        m_GetMainSubControllerBases.HForEach(x => x.ProcessUpdate());
        m_GetMainSubControllerBases.HForEach(x => x.ProcessUpdate(Time.deltaTime));
    }
    #endregion  
}

public abstract class ModuleMonoBase : MonoBehaviour
{
    [SerializeField] protected bool m_isTestMode;

    public bool pp_isTestMode => m_isTestMode;

    protected bool m_isProcessAction;

    public virtual void Initlization(I_PopUpInfo _MainPopUpBase, params object[] _OtherParams) { }

    public virtual void Initlization(ControllerBase _MainBase, params object[] _OtherParams) { }

    public virtual void Initlization(ModuleMonoBase _MainModuleBase, params object[] _OtherParams) { }

    public abstract void SemiBreakPoint(bool _isBreakPoint);

    protected virtual bool CheckAndApplyByParams<T>(object[] _ParsingParams, System.Action<T> _ComplateCallBack)
    {
        int FIDX = -1; FIDX =
        _ParsingParams.HFindIndex(x => x != null && (x.GetType().Name == typeof(T).Name || x is T));
        if (FIDX != -1) _ComplateCallBack?.Invoke((T)_ParsingParams[FIDX]);
        return FIDX != -1;
    }
}