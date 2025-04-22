using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Commons.Helpers;
using Sirenix.OdinInspector;
using PausePopUpType = PausePopUp.PausePopUpBtnType;

[DisallowMultipleComponent]
public class GamePlayManager : MonoBehaviour, I_StartControllerBase, I_CheckInit
{
    public enum GamePlayType { None = 0, StartingSet, Play, RePlay, Pause, EndReady, End}
    //StartingProudctionDone 같은 경우를 ProductionManager<클래스>에서 그 여부를 결정해야됨 (아직 연출 관리자 구현안됨)
    public enum PlayConditionType { None = 0, StartProcessDone, PlayerSettingDone, StartingProudctionDone }

    [Header("Required Values")]
    [field: SerializeField, ReadOnly] public GamePlayType pp_GamePlayTypeNow { get; private set; }
    [field: SerializeField, ReadOnly] public SetSceneType pp_RuningSceneTypeNow { get; private set; }
    
    [Tooltip("Reference Helper")]
    private MyInfo m_MyInfo;
    private DataManager m_DataManager;
    private AudioManager m_AudioManager;
    private LoadingManager m_LoadingManager;
    private DelEventManager m_DelEventManager;
    private ScriptableObjectManager m_ScriptableObjectManager;
    //private ControllerBase m_MainController; 
    private ControllerBase[] m_GetAllMainControllers; //메인컨트롤러지만 최종 제어를 서브형식으로 하는 나머지 Controller들
    private NewPlayerCharacterController m_MyPlayerController; //삭제후 관련된걸 모두 위의 ControllerBase기준으로 재정의할것

    [Tooltip("Required Values")]
    private SetSceneType m_RunningSceneTypeBefore = SetSceneType.None;
    private AllGameStartProcessInfo[] m_AllGameStartProcessInfo;
    private Dictionary<PlayConditionType, bool> m_DicConditionByPlay;
    private int m_InitCount = 0;
    private bool m_isFirstInit = false, m_isReadyEnd = false;
    public bool pp_isActionToPlay { get; private set;}

    public void Initlization()
    {
        if (m_isFirstInit.HDebug($"한번 초기화된것에 대하여 ${nameof(SC_Start)}" +
        $"를 호출할 수 없습니다.!", Helper.HDType.Error)) return;
        m_isFirstInit = true;
        #region Referecne Init
        m_DataManager = Appinstance.Instance.ms_DataManager;
        m_AudioManager = Appinstance.Instance.ms_AudioManager;
        m_DelEventManager = Appinstance.Instance.ms_DelEventManager;
        m_LoadingManager = Appinstance.Instance.ms_LoadingManager;
        m_ScriptableObjectManager = Appinstance.Instance.ms_ScriptableObjectManager;
        pp_RuningSceneTypeNow = m_DataManager.pp_SceneType;
        m_RunningSceneTypeBefore = pp_RuningSceneTypeNow;
        ApplyMyInfoFirstInit(Appinstance.Instance.ms_MyInfo);
        #endregion
        #region StartProcess Init
        m_AllGameStartProcessInfo = new AllGameStartProcessInfo[3];
        string[] GetTypeArray = Helper.GetEnumByStringArray<AllGameStartProcessType>(true);
        for (int i = 0; i < m_AllGameStartProcessInfo.Length; i++)
        m_AllGameStartProcessInfo[i].s_AllGameProcessType = GetTypeArray[i].StringToEnum<AllGameStartProcessType>();
        #endregion
        #region Play Condition Init
        m_DicConditionByPlay = new Dictionary<PlayConditionType, bool>();
        Helper.GetEnumByStringArray<PlayConditionType>(true).ToList().ForEach(x =>
        m_DicConditionByPlay.Add(x.StringToEnum<PlayConditionType>(), false));
        #endregion
        m_DelEventManager.DEL_UpdateMyInfo_Firstinit += ApplyMyInfoFirstInit;
        m_DelEventManager.DEL_MemoryAllResetDELEvntBy_ChangeScene += MemoryResetByGM;
        StartControllerInit(m_DataManager.GetStarterActionBySceneType(pp_RuningSceneTypeNow));
        ExecuteChangeAllGameControll(GamePlayType.StartingSet);
        m_InitCount++;
    }

    public void SC_Start()
    {
        if (m_RunningSceneTypeBefore == pp_RuningSceneTypeNow && m_InitCount < 2) //이후에 다른 로직으로 변경 및 리팩 ****요망****
        { m_InitCount++; return; }
        MemoryResetByGM();
        pp_RuningSceneTypeNow = m_DataManager.pp_SceneType;
        ExecuteChangeAllGameControll(GamePlayType.StartingSet);
    }

    #region Apply Info Private Functions

    public bool ISInitlization() => m_isFirstInit;

    private void ApplyMyInfoFirstInit(MyInfo _GetInfo)
    {
        m_MyInfo = _GetInfo;
        m_DelEventManager.DEL_UpdateMyInfo_Firstinit -= ApplyMyInfoFirstInit; //한번만 해야되기에 바로 없앤다
    }

    private void MemoryResetByGM()
    {
        Helper.GetEnumByStringArray<PlayConditionType>(true).HForEach(x => m_DicConditionByPlay[x.StringToEnum<PlayConditionType>()] = false);
        m_isReadyEnd = false; pp_isActionToPlay = false;
    }

    #endregion

    #region Main System Private Functions

    #region Division All Game Controll Reference

    private void TItleController(GamePlayType _CurrType, bool _SetActionSubSystems = true)
    {
        switch (_CurrType)
        {
            case GamePlayType.StartingSet:
                //처음 타이틀이 아닌 동적 타이틀에서 배치해야되는 부분을 배치한 이후에 Play가 이루어질 수 있도록
                CheckUserPlayCount(PlayConditionType.PlayerSettingDone);
                CheckUserPlayCount(PlayConditionType.StartingProudctionDone);
                //EnterGUI에서 실행할 부분이 존재한다면 끝나는 부분에서 실행해도 무방함
                break;
            case GamePlayType.Play:
                //이후에 TimeManager<클래스>를 만들어서 오브젝트나 GUI가 따로 TimeScale의 영향을 받을 수 있도록 재설계 요망...
                Time.timeScale = 1f;
                break;
            case GamePlayType.Pause:
                Time.timeScale = 0f;
                break;
        }
    }

    private void InGameController(GamePlayType _CurrType, bool _SetActionSubSystems = false, I_Data _SetData = null)
    {
        if ((_CurrType != GamePlayType.StartingSet && !pp_isActionToPlay).HDebug( //상위 조건에 적용할것 (리팩 요망) ************
        $"잘못된 함수 {nameof(InGameController)} 호출을 시도합니다 사전 조건 {GamePlayType.StartingSet}을 먼저 확인하세요.!", Helper.HDType.Error)) return;

        switch (_CurrType)
        {
            case GamePlayType.StartingSet:
                #region Check StartingSet Type Condition
                if (!_SetActionSubSystems && _SetData == null) return;
                System.Action<PlayConditionType> EndCallBack = _ParsingType => CheckUserPlayCount(_ParsingType);
                // 게임전체 관리자에 의해 실행하려면 반드시 MyInfo가 생성받은것을 적용해야한다
                if ((_SetData is not MyItemInfoBuffer).HDebug(
                $"{nameof(MyItemInfoBuffer)}이 필연적으로 존재해야합니다,!", Helper.HDType.Error)) return;
                #endregion
                ItemInfo[] GetInfos = ((MyItemInfoBuffer)_SetData).buffer_MyItemInfos;
                if (GetInfos.CheckArrayNull(0))
                m_GetAllMainControllers = m_MyInfo == null ? new ControllerBase[GetInfos.Length] : m_MyInfo.ApplyModulesByInfo(GetInfos);
                else m_GetAllMainControllers = FindObjectsOfType<ControllerBase>().ToList().FindAll(x => x is not SubModuleControllerBase).ToArray();
                m_GetAllMainControllers.HForEach(x => x.Initlization(m_MyInfo, EndCallBack)); //전부 초기 셋팅이 끝났는지 확인해야한다
                #region 인위적인 데이터 셋에 의해 적용한 버전 (보류 => 이후 삭제 요망)
                //bool isHaveElem = GetInfos.CheckArrayNull(GetInfos.Length - 1);
                //m_GetAllMainControllers = FindObjectsOfType<ControllerBase>().ToList().FindAll(x => x is not SubModuleControllerBase).ToArray();
                //Helper.HCountForEach(0, m_GetAllMainControllers.Length - 1, _CountIDX =>
                //m_GetAllMainControllers[_CountIDX].Initlization(m_MyInfo, isHaveElem ? GetInfos[_CountIDX] : null, EndCallBack));
                #endregion
                CheckUserPlayCount(PlayConditionType.PlayerSettingDone);
                break;
            case GamePlayType.Play:
                Time.timeScale = 1f;
                m_GetAllMainControllers.HForEach(x => x.ForcePlayOrStopOrder(true));
                break;
            case GamePlayType.RePlay:
                #region 재시작 구성 요건에 관한 설명 (**리팩요망**)
                //StartChangeSceneProcess(MainLoadingSystem.SetMainLoadingType.OutPutNothing, (**반드시 이로직을 사용할것**)
                //SetSceneType.MFLGameScene, m_DataManager.pp_ParsingMapInfoNow.s_ItemIDX); //각 MapInfo를 등록했을때 사용
                //삭제 or TestMode일때만 사용 (원래 처럼 위에 있는 로직으로 활용할 수 있도록 리팩토링해야한다 ******************리팩요망******************
                //if (m_MainController is I_UseEventData GetEventData) //MFL 임시 적용사항 (원래는 씬을 필히 다시 시작해야한다)
                //    GetEventData.I_OrderEventDefine(EventListenType.MFLController_ReStart);
                #endregion
                break;
            case GamePlayType.Pause:
                m_GetAllMainControllers.HForEach(x => x.ForcePlayOrStopOrder(false)); 
                m_AudioManager.SoundPlayOrPause(false, AudioType.BGM, new CustomReverveVersion_1());
                System.Nullable<PauseSettingInfo> GetPauseInfo = _SetData as System.Nullable<PauseSettingInfo>;
                bool isTweening = GamePlaySystem.Instance.ExecutePopUpStack<PausePopUp>().
                InitPausePopUp(GetPauseInfo.Value.s_TweeningSped, GetPauseInfo.Value.s_MFLGameReowrd,
                new System.Tuple<PausePopUpType, System.Action>(PausePopUpType.ContinueBtnType, () =>
                {
                    m_AudioManager.SoundPlayOrPause(true, AudioType.BGM, new CustomReverveVersion_1());
                    m_isReadyEnd = false;
                }));
                GamePlaySystem.Instance.MainPopUpPush<PausePopUp>();
                if (!isTweening) Time.timeScale = 0f;
                m_isReadyEnd = true;
                break;
            case GamePlayType.EndReady:
                m_AudioManager.StopAllSound();
                FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None).ToList().FindAll(x => x is I_MustEndBeforeSet).
                ToList().ForEach(y => (y as I_MustEndBeforeSet).I_EndBeforeSet());
                m_GetAllMainControllers.HForEach(x => x.CallEndController(_SetActionSubSystems, _SetData));
                m_isReadyEnd = true;
                break;
            case GamePlayType.End:
                if ((!m_isReadyEnd).HDebug("End 구간을 실행하려면 먼저 EndReady 구간을 실행해야 합니다.!", Helper.HDType.Error)) return;
                PlatformManager.Exit(); //삭제요망
                break;
        }
    }

    #region InGame Controller (Old 2) (**(Only Reference 현재 사용 안함) 단일 MainController 사용시 다시 리팩토링을 고려할것**)
    //이후에 씬 이동이 발생한다면 InGameController에 통합 => ControllerBase로 적용) 통합 이후 리팩토링할 수 있도록 한다
    //private void InGameController(GamePlayType _CurrType, bool _SetActionSubSystems = false, I_Data _SetData = null)
    //{
    //    if ((_CurrType != GamePlayType.StartingSet && !pp_isActionToPlay).HDebug( //상위 조건에 적용할것 (리팩 요망) ************
    //    $"잘못된 함수 {nameof(InGameController)} 호출을 시도합니다 사전 조건 {GamePlayType.StartingSet}을 먼저 확인하세요.!", Helper.HDType.Error)) return;

    //    switch (_CurrType)
    //    {
    //        case GamePlayType.StartingSet:
    //            if (!_SetActionSubSystems && _SetData == null) return;
    //            System.Action<PlayConditionType> EndCallBack = _ParsingType => CheckUserPlayCount(_ParsingType);
    //            if (m_MyInfo != null)
    //            {
    //                m_MainController = m_MyInfo.ApplyModulesByInfo(_SetData);
    //                m_MainController.Initlization(m_MyInfo, EndCallBack);
    //                CheckUserPlayCount(PlayConditionType.PlayerSettingDone);
    //                return;
    //            }
    //            m_MainController?.Initlization(); //MFLUserInfo를 삽입해야함
    //            EndCallBack?.Invoke(PlayConditionType.StartingProudctionDone); //그 이후에 모든 과정을 끝낼 수 있도록 수정 요망..
    //            break;
    //        case GamePlayType.Play: 
    //            Time.timeScale = 1f;
    //            m_MainController.ForcePlayOrStopOrder(true);
    //            break;
    //        case GamePlayType.RePlay:
    //            #region 재시작 구성 요건에 관한 설명 (**리팩요망**)
    //            //StartChangeSceneProcess(MainLoadingSystem.SetMainLoadingType.OutPutNothing, (**반드시 이로직을 사용할것**)
    //            //SetSceneType.MFLGameScene, m_DataManager.pp_ParsingMapInfoNow.s_ItemIDX); //각 MapInfo를 등록했을때 사용
    //            //삭제 or TestMode일때만 사용 (원래 처럼 위에 있는 로직으로 활용할 수 있도록 리팩토링해야한다 ******************리팩요망******************
    //            #endregion
    //            if (m_MainController is I_UseEventData GetEventData)
    //            GetEventData.I_OrderEventDefine(EventListenType.MFLController_ReStart);
    //            break;
    //        case GamePlayType.Pause:
    //            m_MainController.ForcePlayOrStopOrder(false); //Time.timeScale = 0f;
    //            System.Nullable<PauseSettingInfo> GetPauseInfo = _SetData as System.Nullable<PauseSettingInfo>;
    //            m_AudioManager.SoundPlayOrPause(false, AudioType.BGM, new CustomReverveVersion_1());
    //            GamePlaySystem.Instance.ExecutePopUpStack<PausePopUp>().InitPausePopUp(GetPauseInfo.Value.s_TweeningSped, GetPauseInfo.Value.s_MFLGameReowrd, 
    //            new System.Tuple<PausePopUpType, System.Action>(PausePopUpType.ContinueBtnType, () => 
    //            {
    //                m_AudioManager.SoundPlayOrPause(true, AudioType.BGM, new CustomReverveVersion_1());
    //                m_isReadyEnd = false;
    //            })); 
    //            GamePlaySystem.Instance.MainPopUpPush<PausePopUp>();
    //            m_isReadyEnd = true;
    //            break;
    //        case GamePlayType.EndReady:
    //            m_AudioManager.StopAllSound();
    //            FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None).ToList().FindAll(x => x is I_MustEndBeforeSet).
    //            ToList().ForEach(y => (y as I_MustEndBeforeSet).I_EndBeforeSet());
    //            m_MainController.CallEndController(_SetActionSubSystems, _SetData);
    //            m_isReadyEnd = true;
    //            break;
    //        case GamePlayType.End:
    //            if ((!m_isReadyEnd).HDebug("End 구간을 실행하려면 먼저 EndReady 구간을 실행해야 합니다.!", Helper.HDType.Error)) return;
    //            PlatformManager.Exit(); //삭제요망
    //            break;
    //    }
    //}
    #endregion

    #region InGameController State (Old)

    //private void InGameController(GamePlayType _CurrType, bool _SetActionSubSystems = true)
    //{
    //    switch (_CurrType)
    //    {
    //        case GamePlayType.StartingSet: //처음 시작되기 전 셋팅에 대한것
    //            #region Starting Set 1. MyInfo Character OutPut
    //            LocalMapDataModule GetModuleNow = m_DataManager.pp_ParsingPrograssMapNow;
    //            IEnumerator GetMyCharacterInfo =
    //            m_MyInfo.MyPlayerSetting(GetModuleNow.pp_isNewEnterMap, GetModuleNow.pp_isNewEnterMap?
    //            new CameraController.CamControllerInitInfo(true, true, false, CameraController.ViewType.TPS) :
    //            new CameraController.CamControllerInitInfo(false, false, false, CameraController.ViewType.TPS),
    //            _OutPutPlayer => m_MyPlayerController = _OutPutPlayer, () => CheckUserPlayCount(PlayConditionType.PlayerSettingDone));
    //            #endregion
    //            #region Starting Set 2. MyInfo Character Creative In Scene
    //            GetModuleNow.RespowonPlayerPosByPrograss(m_MyPlayerController,
    //            () => CheckUserPlayCount(PlayConditionType.StartingProudctionDone)); //다른곳에 실행될 수 있음
    //            if (GetModuleNow.pp_isNewEnterMap) StartCoroutine(GetMyCharacterInfo);
    //            #endregion
    //            break;
    //        case GamePlayType.Play: //종료되었던것을 다시 Play하거나 그에 준하는 상황
    //            m_MyPlayerController.ChangeState(true, false); //메인 플레이어에 카메라가 존재한다고 가정하고 해당 카메라를 제어시킨다
    //            Time.timeScale = 1f;
    //            //이후에 TimeManager<클래스>를 만들어서 오브젝트나 GUI가 따로 TimeScale의 영향을 받을 수 있도록 재설계 요망...
    //            break;
    //        case GamePlayType.RePlay:
    //            StartChangeSceneProcess(MainLoadingSystem.SetMainLoadingType.OutPutOnlyImgs,
    //            SetSceneType.InGameScene, m_DataManager.pp_ParsingMapInfoNow.s_ItemIDX);
    //            break;
    //        case GamePlayType.Pause:
    //            m_MyPlayerController.ChangeState(false, false);
    //            Time.timeScale = 0f;
    //            //사용할 GUI 요소들을 제외하고 월드에 올려진 오브젝트의 퍼즈상태(TimeScale사용 등등)을 풀 수 있도록 
    //            break;
    //        case GamePlayType.End:
    //            //월드에 띄워져있는것 모두 종료후 원하는 클래스 구간만 정상작동
    //            //씬에 올려진 것중 관련된 인터페이스를 사용하는것 즉 끄기전에 반드시 멈춰야하는 것을에 대한 진행후 End가 되어야함
    //            FindObjectsOfType<MonoBehaviour>().ToList().FindAll(x => x is I_MustEndBeforeSet).
    //            ToList().ForEach(y => (y as I_MustEndBeforeSet).I_EndBeforeSet());
    //            //m_PlayByMyPlayerController.ChangeState(new Commons.ActionInfo()); //MyInfo => DeleteMyPlayer를 한뒤에 정리할 수 있도록
    //            break;
    //            #region End로 통합되기 전 EndGame과 Replay관련 정의
    //            //case GamePlayType.EndGame or GamePlayType.EndGameReplay:
    //            //    if (m_GamePlayTypeNow != GamePlayType.EndStart)
    //            //    {
    //            //        Debug.LogErrorFormat("잘못된 상태 사용입니다 게임을 종료하고 싶다면 먼저 " +
    //            //        "EndStart를 진행해야합니다.! 현재 상태 [{0}]", m_GamePlayTypeNow);
    //            //        return;
    //            //    }

    //            //    bool isEndGameType = _CurrType == GamePlayType.EndGame;
    //            //    if (_SetActionSubSystems) SetCusorOutPoint(true);
    //            //    StartChangeSceneProcess(SetMainLoadingType.OutPutNothing,
    //            //    isEndGameType ? SetSceneType.TitleScene :  SetSceneType.InGameScene,
    //            //    isEndGameType ? -1 : m_RuningSceneType != SetSceneType.TestModeScene ?
    //            //    AppInstance.Instance.ms_DataManager.pp_ParsingMapInfoNow.s_ItemIDX :
    //            //    AppInstance.Instance.ms_DataManager.FindMapReference(200).s_ItemIDX);
    //            //    break;
    //            #region 가져간 이후에 or 키워드를 사용하여 한 타입으로 묶되 가운데서 타입을 나뉠 수 있도록 한다
    //            //case GamePlayType.EndGameReplay:
    //            //    //(임시로 진입) 현재 씬을 다시 진입한다
    //            //    //UnityEngine.SceneManagement.SceneManager.LoadScene(m_SceneKeyIDX);
    //            //    if (_SetActionSubSystems)
    //            //    {
    //            //        m_PlayByMyPlayerController.ChangeState(new Commons.ActionInfo());
    //            //        SetCusorOutPoint(true);
    //            //    }
    //            //    StartChangeSceneProcess(SetMainLoadingType.OutPutNothing, SetSceneType.InGameScene,
    //            //    m_RuningSceneType == SetSceneType.TestModeScene ? AppInstance.Instance.ms_DataManager.FindMapReference(200).s_ItemIDX :
    //            //    AppInstance.Instance.ms_DataManager.pp_ParsingMapInfoNow.s_ItemIDX);
    //            //    break;
    //            //case GamePlayType.EndGame:
    //            //    if (_SetActionSubSystems)
    //            //    {
    //            //        m_PlayByMyPlayerController.ChangeState(new Commons.ActionInfo());
    //            //        SetCusorOutPoint(true);
    //            //    }
    //            //    StartChangeSceneProcess(SetMainLoadingType.OutPutEffect, SetSceneType.TitleScene);
    //            //    break;
    //            #endregion

    //    }
    //    //if (_CurrType == GamePlayType.EndGame || _CurrType == GamePlayType.EndGameReplay)
    //    //{
    //    //    if (m_GamePlayTypeNow != GamePlayType.EndStart)
    //    //    {
    //    //        Debug.LogErrorFormat("잘못된 상태 사용입니다 게임을 종료하고 싶다면 먼저 " +
    //    //        "EndStart를 진행해야합니다.! 현재 상태 [{0}]", m_GamePlayTypeNow);
    //    //        return;
    //    //    }

    //    //    bool isEndGameType = _CurrType == GamePlayType.EndGame;
    //    //    if (_SetActionSubSystems) SetCusorOutPoint(true);
    //    //    StartChangeSceneProcess(SetMainLoadingType.OutPutNothing,
    //    //    isEndGameType ? SetSceneType.TitleScene : SetSceneType.InGameScene,
    //    //    isEndGameType ? -1 : m_RuningSceneType != SetSceneType.TestModeScene ?
    //    //    AppInstance.Instance.ms_DataManager.pp_ParsingMapInfoNow.s_ItemIDX :
    //    //    AppInstance.Instance.ms_DataManager.FindMapReference(200).s_ItemIDX);
    //    //}
    //    #endregion
    //}

    #endregion

    #endregion

    #endregion

    #region Main System Public Reference

    public bool HasCurrentState(GamePlayType _CompareType) => pp_GamePlayTypeNow == _CompareType;

    public T GetFindControllerBaseByTemp<T>() where T : ControllerBase => m_GetAllMainControllers.HFind(x => x is T) as T;

    public void ExecuteChangeAllGameControll(GamePlayType _CurrType, bool _SetActionSubSystems = false, I_Data _SetData = null)
    {
        #region 해당 조건역시 타이틀씬에서 변경조건에 맞게 변경될 수 있기에 예시로 조건을 달았지만 향후 달라지 가능성이 있음
        //if(UnityEngine.SceneManagement.SceneManager.GetSceneAt(m_SceneKeyIDX).name == "TitleScene" &&
        //_CurrType != GamePlayType.DefultPlay)
        //{ Debug.LogErrorFormat("TitleScene에서는 오르지 {0} 타입만 허용합니다.", GamePlayType.DefultPlay); return; }
        #endregion
        if ((_CurrType != GamePlayType.StartingSet && !pp_isActionToPlay).HDebug(
        "아직 유저가 실행하는 모든 상황이 끝나질 않았습니다.!", Helper.HDType.Error)) return;
        switch (pp_RuningSceneTypeNow)
        {
            case SetSceneType.IntroScene or SetSceneType.TitleScene or SetSceneType.TestModeScene:
                TItleController(_CurrType, _SetActionSubSystems); break;
            case SetSceneType.InGameScene or SetSceneType.SoundGameScene:
                InGameController(_CurrType, _SetActionSubSystems, _SetData); break;
        }
        pp_GamePlayTypeNow = _CurrType; //그전값을 비교하는 타입이 존재하기에 뒤에서 전체 상태를 변경함
    }

    public void StartChangeSceneProcess(
    MainLoadingSystem.SetMainLoadingType _MainLoadType, SetSceneType _SetSceneType, int _MapItemIDX = -1, 
    System.Action _EndCallBack = null)
    {
        if (_SetSceneType == SetSceneType.InGameScene && _MapItemIDX == -1)
        { Debug.LogError("MapInfo 데이터 없이 인게임 씬을 진입시도는 할 수 없습니다..!!"); return; }

        /*SetSceneType BeforeSceneType*/
        m_RunningSceneTypeBefore = pp_RuningSceneTypeNow;
        pp_RuningSceneTypeNow = _SetSceneType;

        //리팩요망 
        _MapItemIDX = _SetSceneType != SetSceneType.InGameScene && _MapItemIDX == -1 ? 900 : _MapItemIDX; 
        MapInfo GetMapInfo = m_ScriptableObjectManager.SDataParsingByItemIDX<SDataMapInfo, MapInfo>(_MapItemIDX);
        MainLoadingSystem GetSystem = m_LoadingManager.SetLoadingPrograss();

        Dictionary<int, System.Action> GetLoadingActions = 
        m_DataManager.ChangeSceneInit(_SetSceneType, m_RunningSceneTypeBefore, GetSystem, GetMapInfo, out System.Action _DataBachEndCallBack);
        GetSystem.MainLoadingInit(_MainLoadType, _SetSceneType, m_RunningSceneTypeBefore, GetMapInfo, () =>
        m_LoadingManager.StartLoadProcessDefult(3f, GetLoadingActions),
        () =>
        {
            StartControllerInit(m_DataManager.pp_AllGetChangeSceneInfoNow);
            _DataBachEndCallBack?.Invoke();
            _EndCallBack?.Invoke();
        });
    }

    #endregion

    #region Starting Set Reference
    private I_StartControllerBase[] ChangeSceneActionInfo(AllGameStartProcessType _GetType, string[] _ClassName)
    {
        List<I_StartControllerBase> GetBases = FindObjectsOfType<MonoBehaviour>().ToList().FindAll(y =>
        y is I_StartControllerBase).OfType<I_StartControllerBase>().ToList();
        List<I_StartControllerBase> ReturnBases = new List<I_StartControllerBase>();
        _ClassName.ToList().ForEach(x =>
        {
            int FIDX = -1; FIDX = GetBases.ToList().FindIndex(y => y.GetType().Name == x);
            if (FIDX != -1) ReturnBases.Add(GetBases[FIDX]);
        });
        GameObject.FindGameObjectsWithTag(string.Format("StartProcess_{0}", _GetType.ToString())).ToList().ForEach(x =>
        {
            MonoBehaviour[] GetMonos = x.GetComponents<MonoBehaviour>();
            int FIDX = -1; FIDX = GetMonos.ToList().FindIndex(y => y is I_StartControllerBase);
            if (FIDX != -1) ReturnBases.Add(GetMonos[FIDX] as I_StartControllerBase);
        });
        return ReturnBases.ToArray();
    }

    private void StartControllerInit(SceneStarterAction[] _GetInfo)
    {
        for (int i = 0; i < m_AllGameStartProcessInfo.Length; i++)
        {
            m_AllGameStartProcessInfo[i].s_StartControllerBase = null;
            if(_GetInfo != null) _GetInfo.ToList().ForEach(y =>
            {
                if (m_AllGameStartProcessInfo[i].s_AllGameProcessType == y.s_GetProcessType)
                    m_AllGameStartProcessInfo[i].s_StartControllerBase = ChangeSceneActionInfo(y.s_GetProcessType, y.s_ClassNames);
            });
        }
        AllGameChangeState(AllGameStartProcessType.StaticFirst);
    }

    private void AllGameChangeState(AllGameStartProcessType _RunActionType)
    {
        //메니져 측에서 계속해서 씬이 바뀔때마다 존재하는 구간에 관찰자 형태로 넣을 수 있도록 구분한 조건
        I_StartControllerBase[] GetBase =
        m_AllGameStartProcessInfo.ToList().Find(x => x.s_AllGameProcessType == _RunActionType).s_StartControllerBase;
        if (GetBase != null) GetBase.ToList().ForEach(x => x.SC_Start());
        switch (_RunActionType)
        {
            case AllGameStartProcessType.StaticFirst:
                AllGameChangeState(AllGameStartProcessType.StaticAfter);
                break;
            case AllGameStartProcessType.StaticAfter:
                AllGameChangeState(AllGameStartProcessType.Dynimic);
                break;
            case AllGameStartProcessType.Dynimic:
                Debug.Log("AllStarterProcessComplate");
                CheckUserPlayCount(PlayConditionType.StartProcessDone);
                break;
        }
    }
    #endregion

    #region UserPlayAction Set Reference

    private void CheckUserPlayCount(PlayConditionType _GetType)
    {
        m_DicConditionByPlay[_GetType] = true;
        pp_isActionToPlay = m_DicConditionByPlay.ToList().TrueForAll(x => x.Value);
        if(pp_isActionToPlay) ExecuteChangeAllGameControll(GamePlayType.Play);
    }

    #endregion
}

public interface I_MustEndBeforeSet
{
    public void I_EndBeforeSet();
}

#region Main Send Recive Data Reference

public struct PauseSettingInfo : I_Data
{
    public float s_TweeningSped;
    public MFLGameReowrd s_MFLGameReowrd;

    public PauseSettingInfo(float _TweeningSped, MFLGameReowrd _MFLGameReowrd)
    {
        s_TweeningSped = _TweeningSped;
        s_MFLGameReowrd = _MFLGameReowrd;
    }
}

#endregion

#region Init StartProcess Referecne
//각 맵마다 존재해야하는 호출 목록과 관련된 정리사항
[System.Serializable]
public struct AllGameStartProcessInfo
{
    public AllGameStartProcessType s_AllGameProcessType;
    public I_StartControllerBase[] s_StartControllerBase;
}

public enum AllGameStartProcessType
{
    None = 0,
    StaticFirst, //정적 처음 실행 
    StaticAfter, //처음실행 이후에 실행되어야 할것 
    Dynimic //정적에서 동적으로 게임이 전환되었다는걸 알림
}

public interface I_StartControllerBase
{
    public void SC_Start();
}
#endregion


