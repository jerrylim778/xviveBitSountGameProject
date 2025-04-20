using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Commons.Helpers;
using Sirenix.OdinInspector;

public class TestManager : MonoBehaviour//, I_StartControllerBase
{
    #region [카테고리별 정리 모음]
    //1.GamePlaySystem<클래스> 처럼 PopUp 사용이 가능할 수 있도록 한다
    //(정확히는 팝업 시스템을 빌려오거나 팝업에 해당되는 로직만 가져올 수 있도록
    //2.TestManager<클래스>의 카테고리 사용은 TestManager<클래스>씬을 자동으로
    //불러와서 사용하는것을 기본으로 하고 카테고리 변로 관련된 프리팹을 전부 가져와 바로 실행하는
    //과정을 기본 Test의 로직으로 설정하여 진행한다(GaemPlaySystem<클래스>에서 Loading 구간을 사용할 수 있도록)
    //3.Editor에서 해당 기능을 사용할때 자동으로 런타임을 하여 실시간 Editor창에서 아이템을 적용할 수 있도록 한다
    //4.개발자 모드 빌드인지를 확인하여 없앨지 추가할지를 정해야한다
    //5.TestMode에 진입시 인게임에서는 활용할 수 없도록 수정(인게임 진행 검사(QA)등에도 TestMode일경우를 정확히 명시한다
    //(예를들어서 계속 하다가 멈추면 다시 TestScene으로 와서 관련된 카테고리를 다시 검사할 수 있도록 수정해야된다
    #endregion

    #region [에디터 시작 조건 모음]
    //1.해당 구간은 EditorScript와 어느정도 연관이 있기 때문에 전처리기로 실행할 수 있도록 한다
    //(참고로 타입으로 구분이 안되고 오르지 전처리기를 사용해야됨)
    //2.만일 시작조건이 만족치 않을시 에러를 표출할 수 있도록 한다
    #endregion

    #region [Log 관리]
    //1.카테고리 OutPutGUI에 함께 정의할 수 있도록 한다
    //2.TestManager<클래스>에서 배출되는 OutPut이 존재할시 함께 출력하거나
    //string에 가지고 있다가 한번에 출력하기 (물론 Clear시 전부 없애거나 중복 같이 출력같은 기능도 추가하기
    //(Asset에 의존해도 되겠다)
    //3.
    #endregion
    #region Support By TestManager
    public struct LiverOutPutInfo
    {
        public string s_LiverName;
        public System.Action s_GetCallback;
        public LiverOutPutInfo(string _GetName, System.Action _GetCallBack)
        {
            s_LiverName = _GetName;
            s_GetCallback = _GetCallBack;
        }
    }

    [System.Serializable]
    public struct OutPutLogInfo
    {
        public Helper.HDType s_OutPUtHDType;
        public string s_OutPutStr;
    }

    public enum TestModeType
    {
        None = 0,
        EditorScriptsController, //에디터에서 컨트롤에 의한 테스트인지(Only Editor Platform)
        GameTestByGUIController, //GUI 시스템에 의해 각 오브젝트를 불러와 사용할지
        InGameTestMode, //실제 인게임에서의 테스트
    }
    #endregion

    [Header("ControllTestMode")]
    [SerializeField] private bool m_ForceTestMode = false;

    [Header("TestManager Main Values")]
    [SerializeField, ReadOnly] private TestModeType m_OnUseTMType = TestModeType.None;
    [SerializeField, ReadOnly] private DataManager m_DataManager;
    [SerializeField, ReadOnly] private Canvas m_UseTestCanvas;
    [SerializeField, ReadOnly] private TestModeGUISystem m_OutPutGUIModule;
    [SerializeField] private EventTrigger m_MainPanelTrigger;
    [SerializeField] private GameObject[] m_RequiredSystemProfabs;
    
    [Tooltip("Support Values")]
    private int m_TOCCount = 0;
    private float m_TOCTime = 0f;
    private bool m_isEditor = false;
    private GamePlayManager m_GamePlayManager;
    //Property
    public bool pp_isOnGUI { get => m_OutPutGUIModule != null; }
    public bool pp_isOnUseTestMode { get => m_OnUseTMType != TestModeType.None; } 
    public Stack<OutPutLogInfo> pp_OutPutLogInfoStack { get; private set; }
    public Stack<LiverOutPutInfo> pp_OutPutLiverInfoStack { get; private set; }
    //해당 조건이 True 일때 하면 안되는것을 정의하면 되겠다

    public void Initlization()
    {
        m_isEditor = Application.isEditor;
        m_DataManager = Appinstance.Instance.ms_DataManager;
        if (!Debug.isDebugBuild && !m_isEditor)
        {
            Destroy(m_MainPanelTrigger.gameObject);
            Destroy(this.gameObject);
            return;
        }
        m_UseTestCanvas = this.transform.ChildLinearStuctureSearch<Canvas>()[0];
        m_UseTestCanvas.GetComponent<CanvasScaler>().referenceResolution = m_DataManager.pp_RatioApply;
        (m_MainPanelTrigger.transform.parent as RectTransform).sizeDelta = m_DataManager.pp_RatioApply;
        m_GamePlayManager = Appinstance.Instance.ms_GamePlayManager;
        pp_OutPutLogInfoStack = new Stack<OutPutLogInfo>();
        pp_OutPutLiverInfoStack = new Stack<LiverOutPutInfo>();
        StartCoroutine("CO_WaitByIntroSetting");
    }

    //public void SC_Start() => InitLiverTestMode();

    #region Main System Private Functions

    //해당 구간을 사용하는건 어느구간에서 Test를 진행할지를 말한다
    public void OrderTest(System.Type _CompareType, TestModeType _GetType)
    {
        if (((_GetType == TestModeType.GameTestByGUIController || _GetType == TestModeType.InGameTestMode) && 
        _CompareType.Name != nameof(TestModeGUISystem)) || (_GetType == TestModeType.EditorScriptsController && 
        _CompareType.Name == nameof(TestModeGUISystem)).HDebug(
        "가져올 타입의 클래스가 다른 형식입니다 이는 호출 불가 형태입니다.!", Helper.HDType.Error)) return;

        if (_GetType == TestModeType.GameTestByGUIController || _GetType == TestModeType.EditorScriptsController)
        Appinstance.Instance.ms_GamePlayManager.StartChangeSceneProcess(
        MainLoadingSystem.SetMainLoadingType.OutPutOnlyImgs, SetSceneType.TestModeScene, -1, () =>
        {
            if(_GetType == TestModeType.GameTestByGUIController) 
            {
                //그리고 해당 구역에 LoadingPopUp 이후의 모든 리소스를 긁어 모은다 
                //<**DataManager<클래스>AllParsingInfosActions()**>지점을 통과해야된다는 뜻
                m_OutPutGUIModule.ReturnInitOutBG(TestModeGUISystem.GUIBGType.GUISelectType);
                m_OnUseTMType = _GetType;
            }
            else if (_GetType == TestModeType.EditorScriptsController)
            {
                //EditorTestManager<클래스>를 사용하여 TestScene을 진입하고 Play이후에 해당 함수를 호출하여 Editor의
                //리소스로 활용할 수 있도록 해야한다 (즉 서버의 통신으로 가져온 모든 리소스만 가지고 테스트 진행)
                m_OutPutGUIModule.ReturnInitOutBG(TestModeGUISystem.GUIBGType.GUISelectStaticType);
                m_OnUseTMType = _GetType;
            }
        });
        if (_GetType == TestModeType.InGameTestMode)
        {
            m_OutPutGUIModule.ReturnInitOutBG(TestModeGUISystem.GUIBGType.SpeedSelectType);
            m_OnUseTMType = _GetType;
        }
    }

    public void ExecuteOutTestMode(TestModeType _GetType)
    {
        #region TestMode 역 초기화시에 관한 설명
        //그다음 각 타입에 따라 초기화 해야될것이 어떤것인지 체크하여 초기화 시킬 수 있도록
        //예를들어 Editor의 경우는 런타임 플레이 false
        //인게임 테스트전부는 다시 MainScene으로 돌아가 플레이
        //아닌경우 마지막에 테스트했던 지점을 저장하여 그것을 불러와 다시 해당 부분 부터 플레이 할 수 있도록 수정하기
        #endregion
        if ((m_OnUseTMType != _GetType).HDebug(
        "처음 가져온 TMType과는 다릅니다 이는 있을 수 없습니다.! ", Helper.HDType.Error)) return;
        Destroy(m_OutPutGUIModule.gameObject);
        m_OnUseTMType = TestModeType.None;
    }
    #endregion

    #region Sub System Private Functions
    
    public T TestOutPutRequiredObj<T>(bool _isJustFindOnlyTMPrefabs, bool _isMoveTestModeCanvas = false) where T : MonoBehaviour
    {
        if (_isJustFindOnlyTMPrefabs)
        return m_RequiredSystemProfabs.ToList().Find(x => x.GetComponent<T>() != null).GetComponent<T>();

        bool isOGUsed = FindObjectOfType<T>() != null;
        T ReturnValue = isOGUsed ? FindObjectOfType<T>() :
        m_RequiredSystemProfabs.ToList().Find(x => x.GetComponent<T>() != null).GetComponent<T>();
        T GetValue = isOGUsed ? ReturnValue : ReturnValue != null ? Instantiate(ReturnValue) : null;
        //해당 구간부터 GUI 싱크 맞지 않는 문제 해결하기
        if (_isMoveTestModeCanvas && GetValue != null)
        {
            GetValue.transform.SetParent(m_UseTestCanvas.transform);
            (GetValue.transform as RectTransform).anchoredPosition = Vector2.zero;
        }
        
        return GetValue;
    }

    IEnumerator CO_WaitByIntroSetting()
    {
        yield return new WaitUntil(() => m_GamePlayManager.pp_isActionToPlay || m_ForceTestMode);
        EventTrigger.Entry NewEntry = new EventTrigger.Entry();
        NewEntry.eventID = EventTriggerType.PointerClick;
        NewEntry.callback.AddListener(_data =>
        {
            m_TOCCount++;
            m_TOCTime = 5f;
            if (m_TOCCount <= 5 || (pp_isOnGUI).HDebug("현재 TestGUI가 켜져 있습니다.!", Helper.HDType.Warning)) return;
            m_OutPutGUIModule = TestOutPutRequiredObj<TestModeGUISystem>(false, true);
            InitLiverTestMode();
            m_OutPutGUIModule.Initlization(this); 
        });
        m_MainPanelTrigger.triggers.Add(NewEntry);
    }
    #endregion

    #region Liver TM Reference
    public void InitLiverTestMode()
    {
        if (pp_OutPutLogInfoStack != null && pp_OutPutLogInfoStack.Count > 0) return;

        LiverOutPutInfo[] LiverOutPutInfoArray = new LiverOutPutInfo[]
        {
            #region ~241120 까지의 테스트 관련 적용들
            //            new LiverOutPutInfo("전체 데이터(서버 + 로컬)테이블 => 스크립터블 오브젝트 변환", () =>
            //            Appinstance.Instance.ms_ScriptableObjectManager.SetApplyScriptableObject(() =>
            //            Appinstance.Instance.ms_LoadingManager.DELManagerUpdateGCCollect(true, 1))),

            //            new LiverOutPutInfo("받은 데이터 내정보 강제 초기화", () =>
            //            {
            //                MyInfo MyInfoHeader = Appinstance.Instance.ms_MyInfo;
            //                MyInfoHeader = new MyInfo(Appinstance.Instance, "");
            //                MyInfoHeader.AllMyInfoLoadAndApplyAnyncParsing(() =>
            //                Appinstance.Instance.ms_LoadingManager.DELManagerUpdateGCCollect(true, 1), out IEnumerator[] _GetCourtines);
            //                _GetCourtines.ToList().ForEach(x => StartCoroutine(x));
            //            }),
            //#if UNITY_EDITOR
            //            new LiverOutPutInfo("파일 Resources => CharacterSystems 폴더내 파일 갯수 ", () =>
            //            {
            //                if (!m_isEditor) return;
            //                string GetPath = $"{Application.dataPath}/Resources/Objects/DefultObject/Systems/CharacterSystems";
            //                string[] GetFiles = System.IO.Directory.GetFiles(GetPath);
            //                GetFiles.HForEach(x => Debug.Log(x));
            //            }),
            //#endif
            //            new LiverOutPutInfo("인게임 씬 전환.! (씬 로드 이미지 타입)", () =>
            //            {
            //                Appinstance.Instance.ms_GamePlayManager.StartChangeSceneProcess(
            //                MainLoadingSystem.SetMainLoadingType.OutPutEffect, SetSceneType.InGameScene, 902);
            //                ExecuteOutTestMode(m_OnUseTMType);
            //            }),

            //            new LiverOutPutInfo("타이틀 씬 전환.! (씬 로드 이미지 타입)", () =>
            //            {
            //                Appinstance.Instance.ms_GamePlayManager.StartChangeSceneProcess(
            //                MainLoadingSystem.SetMainLoadingType.OutPutEffect, SetSceneType.TitleScene, -1);
            //                ExecuteOutTestMode(m_OnUseTMType);
            //            }),

            //            new LiverOutPutInfo("MFLController => 회전 회오리! (DoTween Version)", () =>
            //            {
            //                FindAnyObjectByType<MFLTablesSubModuleController>().TestCalculate(true);
            //                ExecuteOutTestMode(m_OnUseTMType);
            //            }),

            //            new LiverOutPutInfo("MFLController => 회전 회오리 복구!", () =>
            //            {
            //                FindAnyObjectByType<MFLTablesSubModuleController>().InitCalculate();
            //            })
            #endregion

            #region MFL 테스트 관련 적용들
            //new LiverOutPutInfo("EndPopUp (Fail 관련) OutPut!", () =>
            //{
            //    GamePlaySystem.Instance.MainPopUpPop<EndGamePopUp>(null, true);
            //    GamePlaySystem.Instance.ExecutePopUpStack<EndGamePopUp>().InitEndGamePopUp(false, 0.32f);
            //    GamePlaySystem.Instance.MainPopUpPush<EndGamePopUp>();
            //    ExecuteOutTestMode(m_OnUseTMType);
            //}),

            //new LiverOutPutInfo("EndPopUp (Victory 관련) OutPut!", () =>
            //{
            //    GamePlaySystem.Instance.MainPopUpPop<EndGamePopUp>(null, true);
            //    GamePlaySystem.Instance.ExecutePopUpStack<EndGamePopUp>().InitEndGamePopUp(true, 0.32f);
            //    GamePlaySystem.Instance.MainPopUpPush<EndGamePopUp>();
            //    ExecuteOutTestMode(m_OnUseTMType);
            //}),

            //new LiverOutPutInfo("PausePopUp OutPut!", () =>
            //{
            //    GamePlaySystem.Instance.MainPopUpPop<PausePopUp>(null, true);
            //    GamePlaySystem.Instance.ExecutePopUpStack<PausePopUp>().InitPausePopUp(0.32f, 
            //    Appinstance.Instance.ms_DataManager.pp_MFLGameData.spp_MFLGameReword);
            //    GamePlaySystem.Instance.MainPopUpPush<PausePopUp>();
            //    ExecuteOutTestMode(m_OnUseTMType);
            //})
            #endregion

            #region ~250305 까지의 테스트 관련 적용들
            //new LiverOutPutInfo("BundleParsing.!", () =>
            //Appinstance.Instance.ms_AssetBundleManager.LoadStartBundleParsing(
            //Helper.StringToEnum<SetSceneType>(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name), 638689329008008127)),

            //new LiverOutPutInfo("TYCRequiredPopUp OutPut", () =>
            //{
            //    //GamePlaySystem.Instance.MainPopUpPop<TYCGameRequiredPopUp>(null, true);
            //    GamePlaySystem.Instance.ExecutePopUpStack<TYCGameRequiredPopUp>().InitTYCGameRequiredPopUp(null);
            //    GamePlaySystem.Instance.MainPopUpPush<TYCGameRequiredPopUp>();
            //    ExecuteOutTestMode(m_OnUseTMType);
            //}),

            //new LiverOutPutInfo("NoticePopUp OutPut", () =>
            //{
            //    GamePlaySystem.Instance.MainPopUpPop<NoticePopUp>(null, true);
            //    GamePlaySystem.Instance.ExecutePopUpStack<NoticePopUp>().InitNotice("Notice Test \n Notice Test \n Notice Test \n");
            //    GamePlaySystem.Instance.MainPopUpPush<NoticePopUp>();
            //    ExecuteOutTestMode(m_OnUseTMType);
            //}),

            //new LiverOutPutInfo("Test User Control Character", () =>
            //{
            //    var GetManager = Appinstance.Instance.ms_DataManager;
            //    var GetSManager = Appinstance.Instance.ms_ScriptableObjectManager;
            //    GamePlaySystem GetGameSystem = GamePlaySystem.Instance;
            //    System.Action _ApplyActions = () =>
            //    #region 로딩 이후에 작업들
            //    {
            //        CharacterItemInfo CInfo = GetSManager.SDataParsingByItemIDX<SDataCharacterInfo , CharacterItemInfo>(199);
            //        var MainController = Object.Instantiate(CInfo.s_PrefabObjs).GetComponent<NewCharacterController>();
            //        var GetBase = new List<ControllerBase>(); var _OutPutPDs = new ControllerBase.I_PurifiedParamsData[3];
            //        #region 각 묶은 작업을 GenerateInfo나 기타 다른작업에 데이터화하여 코드 간소화 요망**)
            //        NewActionInfo GetNAInfo = (NewActionInfo)m_DataManager.CompareInfoType<NewActionInfo>(CInfo.s_NewActionInfoIDX.ToString());
            //        GetBase.Add(m_DataManager.GetCompareComponent<CameraController>(false, CameraController.ViewType.TPS.ToString(), ref _OutPutPDs[0]));
            //        GetBase.Add(m_DataManager.GetCompareComponent<BInputController>(false, CInfo.s_CharacterUsePowerType.ToString(), ref _OutPutPDs[1]));
            //        GetBase.Add(m_DataManager.GetCompareComponent<AnimationController>(false, CInfo.s_CalculateInfoIDX.ToString(), ref _OutPutPDs[2]));
            //        //var ChangeInfoValue = (CameraController.CamControllerInitInfo)_OutPutPDs[0];
            //        //ChangeInfoValue.s_isNotUseRot = false;
            //        //_OutPutPDs[0] = ChangeInfoValue;
            //        GetNAInfo.s_SubModuleBase = GetBase.OfType<SubModuleControllerBase>().ToArray();
            //        GetNAInfo.s_RequiredSubModuleParams = _OutPutPDs;
            //        #endregion
            //        UserInfo MakeTempUInfo = new(string.Empty, GetNAInfo);
            //        MainController.Initlization(MakeTempUInfo);
            //        MainController.ForcePlayOrStopOrder(true);
            //    };
            //    #endregion
            //    if (GetManager.pp_isFirstTime)
            //    {
            //        _ApplyActions();
            //        ExecuteOutTestMode(m_OnUseTMType);
            //        return;
            //    }
            //    GetGameSystem.ExecutePopUpStack<LoadingPopUp>().InitLoadingPopUp(GetManager.AllParsingInfosActions(), _ApplyActions);
            //    GetGameSystem.MainPopUpPush<LoadingPopUp>();
            //    ExecuteOutTestMode(m_OnUseTMType);
            //}),

            //new LiverOutPutInfo("Test New Control Character", () =>
            //{
            //    var GetManager = Appinstance.Instance.ms_DataManager;
            //    var GetSManager = Appinstance.Instance.ms_ScriptableObjectManager;
            //    GamePlaySystem GetGameSystem = GamePlaySystem.Instance;
            //    System.Action _ApplyActions = () =>
            //    #region 로딩 이후에 작업들
            //    {
            //        CharacterItemInfo CInfo = GetSManager.SDataParsingByItemIDX<SDataCharacterInfo , CharacterItemInfo>(199);
            //        var MainController = Object.Instantiate(CInfo.s_PrefabObjs).GetComponent<NewCharacterController>();
            //        var GetBase = new List<ControllerBase>(); var _OutPutPDs = new ControllerBase.I_PurifiedParamsData[3];
            //        #region 각 묶은 작업을 GenerateInfo나 기타 다른작업에 데이터화하여 코드 간소화 요망**)
            //        NewActionInfo GetNAInfo = (NewActionInfo)m_DataManager.CompareInfoType<NewActionInfo>(CInfo.s_NewActionInfoIDX.ToString());
            //        GetBase.Add(m_DataManager.GetCompareComponent<CinemachineCameraController>(true, CameraController.ViewType.TPS.ToString(), ref _OutPutPDs[0]));
            //        GetBase.Add(m_DataManager.GetCompareComponent<BInputController>(false, CInfo.s_CharacterUsePowerType.ToString(), ref _OutPutPDs[1]));
            //        GetBase.Add(m_DataManager.GetCompareComponent<TPSPackageSubModuleController>(false, CInfo.s_CalculateInfoIDX.ToString(), ref _OutPutPDs[2]));
            //        GetNAInfo.s_SubModuleBase = GetBase.OfType<SubModuleControllerBase>().ToArray();
            //        GetNAInfo.s_RequiredSubModuleParams = _OutPutPDs;
            //        #endregion
            //        UserInfo MakeTempUInfo = new(string.Empty, GetNAInfo);
            //        MainController.Initlization(MakeTempUInfo);
            //        MainController.ForcePlayOrStopOrder(true);
            //    };
            //    #endregion
            //    if (GetManager.pp_isFirstTime)
            //    {
            //        _ApplyActions();
            //        ExecuteOutTestMode(m_OnUseTMType);
            //        return;
            //    }
            //    GetGameSystem.ExecutePopUpStack<LoadingPopUp>().InitLoadingPopUp(GetManager.AllParsingInfosActions(), _ApplyActions);
            //    GetGameSystem.MainPopUpPush<LoadingPopUp>();
            //    ExecuteOutTestMode(m_OnUseTMType);
            //}),

            //new LiverOutPutInfo("Force Add Money", () =>
            //{
            //    Appinstance.Instance.ms_DelEventManager.DELApplyGUIUpdateMoney(true, 800);
            //    ExecuteOutTestMode(m_OnUseTMType);
            //})
            #endregion

            #region Runner 테스트 관련 로직들

            //new LiverOutPutInfo("Runner EndGamePopUp", () =>
            //{
            //    GamePlaySystem.Instance.ExecutePopUpStack<EndGamePopUpUnion>().InitEndGamePopUp(true, 0.4f, null);
            //    GamePlaySystem.Instance.MainPopUpPush<EndGamePopUpUnion>();
            //    ExecuteOutTestMode(m_OnUseTMType);
            //}),

            //new LiverOutPutInfo("Show NoticePopup", () =>
            //{
            //    GamePlaySystem.Instance.ExecutePopUpStack<NoticePopUp>().InitNotice("Show Test NoticePopUp");
            //    GamePlaySystem.Instance.MainPopUpPush<NoticePopUp>();
            //    ExecuteOutTestMode(m_OnUseTMType);
            //}),

            //new LiverOutPutInfo("Defult EndGamePopUp", () =>
            //{
            //    GamePlaySystem.Instance.ExecutePopUpStack<EndGamePopUp>().InitEndGamePopUp(true, 0.32f);
            //    GamePlaySystem.Instance.MainPopUpPush<EndGamePopUp>();
            //    ExecuteOutTestMode(m_OnUseTMType);
            //}),

            //new LiverOutPutInfo("Show Runner Required PopUp", () =>
            //{
            //    GamePlaySystem.Instance.MainPopUpPop<RunnerGameRequiredPopUp>(_isTestMode: true);
            //    GamePlaySystem.Instance.ExecutePopUpStack<RunnerGameRequiredPopUp>().InitRunnerGameRequiredPopUp(null);
            //    GamePlaySystem.Instance.MainPopUpPush<RunnerGameRequiredPopUp>();
            //    ExecuteOutTestMode(m_OnUseTMType);
            //}),

            #endregion

            #region Dragon Bow Reference
            //new LiverOutPutInfo("DragonBow EndGamePopUp", () =>
            //{
            //    GamePlaySystem.Instance.MainPopUpPop<EndGamePopUpUnion>(_isTestMode : true);
            //    GamePlaySystem.Instance.ExecutePopUpStack<EndGamePopUpUnion>().InitEndGamePopUp(true, 0.4f, null);
            //    GamePlaySystem.Instance.MainPopUpPush<EndGamePopUpUnion>();
            //    ExecuteOutTestMode(m_OnUseTMType);
            //}),

            //new LiverOutPutInfo("Bow Game LevelUp PopUp", () =>
            //{
            //    GamePlaySystem.Instance.MainPopUpPop<LevelUpPopUp>(_isTestMode: true);
            //    GamePlaySystem.Instance.ExecutePopUpStack<LevelUpPopUp>().LevelUpPopUpInit(null, null);
            //    GamePlaySystem.Instance.MainPopUpPush<LevelUpPopUp>();
            //    ExecuteOutTestMode(m_OnUseTMType);
            //})
            #endregion

            new LiverOutPutInfo("FlipGame EndGamePopUp", () =>
            {
                GamePlaySystem.Instance.MainPopUpPop<EndGamePopUpUnion>(_isTestMode : true);
                GamePlaySystem.Instance.ExecutePopUpStack<EndGamePopUpUnion>().InitEndGamePopUp(false, 0.3f, null);
                GamePlaySystem.Instance.MainPopUpPush<EndGamePopUpUnion>();
                ExecuteOutTestMode(m_OnUseTMType);
            }),
        };
        LiverOutPutInfoArray.ToList().ForEach(x => pp_OutPutLiverInfoStack.Push(x));
    }

    #endregion

    #region 유니티 이벤트 함수
    void Update()
    {
        if (m_MainPanelTrigger.triggers.Count <= 0) return;
        if(m_TOCTime > 0f) m_TOCTime -= Time.unscaledDeltaTime * 1.5f;
        if (m_MainPanelTrigger.triggers.Count > 0 && m_TOCTime <= 0f) m_TOCCount = 0;
    }
    #endregion
}

#region 해당 구간은 Editor환경에서 Project부분에서 파일이 변경되었는지 감지하는 로직 (Editor 스크립트에서 만족하는것을 찾아 정의요망)
//따라서 해당 구간은 Eidtor일 경우를 살펴서 맞다면 Edior의 로직을 불러와 Playing될 수 있는지의 조건을
//만족할 수 있도록 한다(그 이외의 경우는 무조건 Editor => TestManager<클래스>의 조건을 불러와 사용해야됨을 명심하자)
//using UnityEditor;

//public class MyAssetPostprocessor : AssetPostprocessor
//{
//    static void OnPostprocessAllAssets(
//        string[] importedAssets,
//        string[] deletedAssets,
//        string[] movedAssets,
//        string[] movedFromAssetPaths)
//    {
//        foreach (string asset in importedAssets)
//        {
//            // 파일이 변경되었을 때 실행할 코드
//            Debug.Log("Reimported Asset: " + asset);
//        }

//        foreach (string asset in deletedAssets)
//        {
//            // 파일이 삭제되었을 때 실행할 코드
//            Debug.Log("Deleted Asset: " + asset);
//        }

//        foreach (string asset in movedAssets)
//        {
//            // 파일이 이동되었을 때 실행할 코드
//            Debug.Log("Moved Asset: " + asset);
//        }
//    }
//}
#endregion