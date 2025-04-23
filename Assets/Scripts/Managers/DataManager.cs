using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using Sirenix.OdinInspector;
using Commons.Helpers;
using I_PurifiedParamsData = ControllerBase.I_PurifiedParamsData;

public sealed partial class DataManager : MonoBehaviour
{
    [Header("===DataManager_TemporyWork_Reference===")]
    [SerializeField, PropertyOrder(int.MinValue)] private bool test_isAppliedTemporyAll = false;  //체크할시 정적으로 있던 모든데이터들을 파싱해서 
    [SerializeField, ShowIf(nameof(test_isAppliedTemporyAll))] private GameObject[] pp_RequiredPrefabByTemporyObjs; //다른 방식을 생각해야될 수 도 있다

    [Header("======MainDataHeader======")]
    [Header("Parsing Reference")]
    [SerializeField] private bool m_isSyncParsingOnlyThisData = false;
    [Header("Compare Scene Data Reference")]
    [SerializeField] private SetSceneChangeControllerInfo[] m_AllGetChangeSceneInfo;
        
    [Tooltip("Data Required Values")]
    private bool m_isFirstTime = false;
    private SetSceneChangeControllerInfo m_AllGetChangeSceneInfoNow;
    private MyInfo m_InitMyinfo = null;
    private DelEventManager m_DelEventManager;
    private SettingManager m_SettingManager;
    private AssetBundleManager m_AssetBundleManager;
    private ScriptableObjectManager m_ScriptableObjectManager;
    private LoadingManager m_LoadingManager;

    [Header("Data Support Required Value")]
    [field: SerializeField] public MyInfo.ConTestModeChildTypeName[] pp_MyInfoCheckTestModeByChild { get; private set; }

    [Tooltip("Property")]
    public bool pp_isFirstTime { get => m_isFirstTime; }

    #region Bundle Parsing Reference (**Test 부분은 전부 서버측에서 가져와야함**)

    private readonly long test_BundleLastUpdateTickTime = 638689329008008127;

    #endregion

    #region Resources Path Reference
    [Tooltip("Resources Paths")]
    //AnimController\DefultObject\MainClips
    private Dictionary<string, string> m_AllResourcesLoadPath = new()
    {
        { nameof(AudioClip), $"Objects/DefultObject/MainClips/{nameof(AudioClip)}/" },
        { nameof(UnityEngine.Video.VideoClip), $"Objects/DefultObject/MainClips/{nameof(UnityEngine.Video.VideoClip)}/" },
    };

    public string ResourcesParsingPath<T>() where T : UnityEngine.Object => m_AllResourcesLoadPath[typeof(T).Name];

    #endregion

    //이후 partial을 통해 Datamanager.Ratio<클래스>를 생성하여 모두 이전할 수 있도록

    [Header("DataManager => SetRatio")]
    [SerializeField] private bool m_isFreeRatio = false; 
    [SerializeField, ShowIf(nameof(ISOdinToggleFalse))] private Vector2 m_StaticRatio = Vector2.zero;
    public Vector2 pp_RatioApply { get => m_isFreeRatio ? new Vector2(Screen.width, Screen.height) : m_StaticRatio; }
    #region OdinShowOrHide => m_isFreeRatio
    public bool ISOdinToggleFalse() => !m_isFreeRatio;
    #endregion

    #region Property
    public MapInfo pp_ParsingMapInfoNow { get; private set; }
    public LocalMapDataModule pp_ParsingPrograssMapNow { get; private set; }
    public SetSceneType pp_SceneType { get; private set;}
    public SceneStarterAction[] pp_AllGetChangeSceneInfoNow { get => m_AllGetChangeSceneInfoNow.s_AllSceneStarterAction; }

    #endregion

    public void Initlization()
    {
        m_DelEventManager = Appinstance.Instance.ms_DelEventManager;
        m_LoadingManager = Appinstance.Instance.ms_LoadingManager;
        m_SettingManager = Appinstance.Instance.ms_SettingManager;
        m_AssetBundleManager = Appinstance.Instance.ms_AssetBundleManager;
        m_ScriptableObjectManager = Appinstance.Instance.ms_ScriptableObjectManager;
        //Parsing Current Scene Init
        pp_SceneType = SearchSceneTypeNow();
        //MyInfo Data Init
        ApplyMyInfoFirstInit(Appinstance.Instance.ms_MyInfo);
        m_DelEventManager.DEL_UpdateMyInfo_Firstinit += ApplyMyInfoFirstInit;
        //Map Data Init
        pp_ParsingPrograssMapNow = FindAnyObjectByType<LocalMapDataModule>();
        pp_ParsingMapInfoNow = pp_ParsingPrograssMapNow?.pp_StaticMapInfo;
    }

    private void ApplyMyInfoFirstInit(MyInfo _GetInfo)
    {
        m_InitMyinfo = _GetInfo;
        m_DelEventManager.DEL_UpdateMyInfo_Firstinit -= ApplyMyInfoFirstInit; //한번만 해야되기에 바로 없앤다
    }

    #region [Custom] Sub System All Functions
    //해당 전처리기 구간은 Test구간이기 때문에 다른 방식으로 변경해야된다는것을 인지 하고 정의해야됨
    public SceneStarterAction[] GetStarterActionBySceneType(SetSceneType _currScene)
    => m_AllGetChangeSceneInfo.ToList().Find(x => x.s_SetSceneType == _currScene).s_AllSceneStarterAction;

    private System.Action TestOnlyOutPutDebugLoading(string _OutPutDebugStr) => () =>
    {
        Debug.LogWarning(_OutPutDebugStr);
        m_LoadingManager.DELManagerUpdateGCCollect(true, 1);    
    };

    private SetSceneType SearchSceneTypeNow()
    {
        string GetSNameNow = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        SetSceneType ReturnType = GetSNameNow.Contains("_") && GetSNameNow.Split('_')[1] != SetSceneType.IntroScene.ToString() ?
        SetSceneType.IntroScene : Helper.StringToEnum<SetSceneType>(GetSNameNow.IsFindStringToEnum<SetSceneType>());
        (ReturnType == SetSceneType.None).HDebug("현재 씬이 정상적이지 않습니다.!", Helper.HDType.Warning);
        return ReturnType;
    }

    public string SearchCurrentSceneName()
    => UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;

    #endregion

    #region All Parsing And Beach Reference
    //파싱 순서 : 내정보 및 셋팅값 (로컬 + 서버)  => 스크립터블 데이터 (로컬 + 서버) => 배치
    public Dictionary<int, System.Action> AllParsingInfosActions()
    {
        if(m_isFirstTime)
        {
            Debug.LogError("해당 함수는 오직 한번만 실행 가능합니다..!!");
            return null;
        }
        m_isFirstTime = true;
        string SendClientValue = string.Empty;
        return new Dictionary<int, System.Action>()
        {
            //서버로 부터 정보 파싱 (우선 로컬로 저장해둔 MyInfo정보만 가져오는걸로 한다
            //{ -1, () => StartCoroutine(m_LoadingManager.CO_GCCollectByLoadAfter(1.5f, true, () => ApplyReciveDatas(0, ref SendClientValue))) }, //웹서버 정보 파싱
            //{ -2, () => StartCoroutine(m_LoadingManager.CO_GCCollectByLoadAfter(1.5f, true, () => ApplyReciveDatas(1, ref SendClientValue, SendClientValue))) }, //번들 파싱
            //{ -3, () => StartCoroutine(m_LoadingManager.CO_GCCollectByLoadAfter(1.5f, true, () => ApplyReciveDatas(2, ref SendClientValue, SendClientValue))) }, //스크립터블 파싱
            { -4, () => StartCoroutine(m_LoadingManager.CO_GCCollectByLoadAfter(1.5f, true, () => ApplyReciveDatas(3, ref SendClientValue, SendClientValue))) }, //내정보 적용
            { -5, () => StartCoroutine(m_LoadingManager.CO_GCCollectByLoadAfter(1.5f, true, () => ApplyReciveDatas(4, ref SendClientValue, SendClientValue))) }, //세팅값 적용
            { -6, () => StartCoroutine(m_LoadingManager.CO_GCCollectByLoadAfter(1.5f, true, () => ApplyReciveDatas(5, ref SendClientValue, SendClientValue))) }, //데이터에 따른 배치 적용
            //내정보와 관련된것 전부 배치 (실제 배치해야될 WorldPos에 배치에서의 의미 => 세팅값 적용
        }; 
    }

    #endregion

    #region All Parsing Apply Sub Data Reference

    private void ApplyReciveDatas(System.UInt16 _ParsingIDX, ref string _SendClientValue, string _ReciveServerValue = "")
    {
        switch (_ParsingIDX)
        {
            //갔다와서 에셋하나를 전체 가져와 번들 빌드해보고 (에셋 번들 규칙에 맞게 번들 빌드해야됨) 그걸 본프로젝트에서 파싱할 수 있도록 테스트 QA 진행하기
            case 0:
                //WebNetworkManager<클래스>로 부터 정상적인 접속에 필요한 모듈과 (유저의 유일키) 및 유일키에 포함되어있는 모든 데이터를 가져올 수 있도록 한다
                m_LoadingManager.DELManagerUpdateGCCollect(true, 1);
                break;
            case 1:
                long ApplyLongTime = string.IsNullOrEmpty(_ReciveServerValue) ? test_BundleLastUpdateTickTime : long.Parse(_ReciveServerValue);
                m_AssetBundleManager.LoadStartBundleParsing(pp_SceneType, ApplyLongTime, () => //테스트 후 실사용할 수 있도록 수정한다
                m_LoadingManager.DELManagerUpdateGCCollect(true, 1));
                break;
            case 2: //스크립터블 데이터 파싱 (로컬 DB 파싱 + (내정보에 관한 제이슨 정보 파싱 + 캐시 로컬로 부터 번들 파싱))
                m_ScriptableObjectManager.SetApplyScriptableObject(() => 
                m_LoadingManager.DELManagerUpdateGCCollect(true, 1));
                break;
            case 3: //내정보(MyInfo)에 서버로부터 가져온 DB 데이터 적용 (예정) (**리팩요망 모듈에 따른 템플릿화 유도하기**)
                if (string.IsNullOrEmpty(_ReciveServerValue)) _ReciveServerValue = "TestLoginKey";
                #region 각 프로젝트마다 생성자를 인식할 수 있는 방안 간구 요망 **
                //클래스이름으로 찾는것이 아니라면 생성자고정값을로 생성하기 때문에 다른 방식으로 로직적용을 변경해야한다
                //(단 해당 클래스가 프로젝트내에 있는지를 확인하는 Helper 클래스의 함수 SearchClassInProject는 정의해야한다)
                //m_InitMyinfo = Helper.SearchClassInProject(nameof(CharacterMyInfo)) ? 
                //new CharacterMyInfo(Appinstance.Instance, _ReciveServerValue) : new MyInfo(Appinstance.Instance, _ReciveServerValue);
                #endregion
                int CurrCount = 0, MaxCount = 0;
                m_InitMyinfo = new MyInfo(Appinstance.Instance, _ReciveServerValue);
                //m_InitMyinfo = new TyconMyinfo(Appinstance.Instance, _ReciveServerValue);
                //m_InitMyinfo = new CharacterMyInfo(Appinstance.Instance, _ReciveServerValue);
                m_InitMyinfo.AllMyInfoLoadAndApplyAnyncParsing(() =>
                {
                    CurrCount++;
                    if (CurrCount >= MaxCount) m_LoadingManager.DELManagerUpdateGCCollect(true, 1);
                }, out IEnumerator[] _GetCourtines);
                MaxCount = _GetCourtines.Length;
                _GetCourtines.ToList().ForEach(x => StartCoroutine(x));
                break;
            case 4: //서버로부터 받아온 데이터 => Setting값에 Load And Apply
                m_SettingManager.AllSettingLoadAndApplyAnyncParsing(() => 
                m_LoadingManager.DELManagerUpdateGCCollect(true, 1));
                break;
            case 5: //필수 파싱 데이터 이후 관찰자를 통한 버퍼 관리자를 통한 배치 및 각 클래스에 할당된 작업 개시
                m_DelEventManager.DELRequiredParsingComplate(() =>
                m_LoadingManager.DELManagerUpdateGCCollect(true, 1));
                break;
        }
    }

    #endregion

    #region Map Beach Data Reference

    public Dictionary<int, System.Action> ChangeSceneInit
    (SetSceneType _currType, SetSceneType _BeforeType, MainLoadingSystem _GetMainLoadingSystem, MapInfo _GetMapInfo, 
    out System.Action _EndOutPutCallBack)
    {
        SetSceneType BeforeSceneType = pp_SceneType;
        pp_ParsingMapInfoNow = _GetMapInfo;
        pp_SceneType = _currType;
        
        m_AllGetChangeSceneInfoNow =
        m_AllGetChangeSceneInfo.ToList().Find(x => x.s_SetSceneType == _currType);
        _EndOutPutCallBack = null; //DataManager측에서 리턴할 EndCallBack가 없다면 삭제 조치 요망...
        return ChangeSceneByActions(_currType, _BeforeType, _GetMainLoadingSystem, out _EndOutPutCallBack);
    }

    private Dictionary<int, System.Action> ChangeSceneByActions(
    SetSceneType _GetSceneType, SetSceneType _BeforeType, MainLoadingSystem _GetSystem, out System.Action _EndOutPutCallBack)
    {
        _EndOutPutCallBack = null;
        if (_GetSceneType == SetSceneType.TitleScene)
        return new Dictionary<int, System.Action>(2)
        {
            { 2004, () =>
            StartCoroutine(Appinstance.Instance.ms_LoadingManager.CO_GCCollectByLoadAfter(3f, true, () =>
            m_DelEventManager.DELMemoryAllResetDELEvntByChangeScene(true)))}, //전에 존재한 메모리 리셋

            { 2003, () =>
            StartCoroutine(Appinstance.Instance.ms_LoadingManager.CO_GCCollectByLoadAfter(3f, true, () =>
            StartCoroutine(_GetSystem.CO_StepNextSceneAsync(_GetSceneType))))} //씬 변경
        };
        else if (_GetSceneType == SetSceneType.InGameScene)
        return new Dictionary<int, System.Action>(4)
        {
            { -1, () => //이후에 TableIDX로 따로 할당할 수 있도록 하자
            StartCoroutine(Appinstance.Instance.ms_LoadingManager.CO_GCCollectByLoadAfter(1.5f, true ,() =>
            StartCoroutine(CO_BeforeBachMapAction(_GetSceneType, _BeforeType)))) },//씬 변경전 같은 씬인지 테스트

            { 2004, () =>
            StartCoroutine(Appinstance.Instance.ms_LoadingManager.CO_GCCollectByLoadAfter(3f, true, () =>
            m_DelEventManager.DELMemoryAllResetDELEvntByChangeScene(true)))}, //전에 존재한 메모리 리셋

            { 2003, () =>
            StartCoroutine(Appinstance.Instance.ms_LoadingManager.CO_GCCollectByLoadAfter(3f, true, () =>
            StartCoroutine(_GetSystem.CO_StepNextSceneAsync(_GetSceneType))))}, //씬 변경

            { 2005, () =>
            StartCoroutine(Appinstance.Instance.ms_LoadingManager.CO_GCCollectByLoadAfter(2f, true, () =>
            InstanceMap(_GetSceneType, pp_ParsingMapInfoNow)))}, //맵배치
            #region 일전에 존재한 정적 데이터 로직 (참고용 이후 삭제요망..)
            //    //() => StartCoroutine(Appinstance.Instance.ms_LoadingManager.CO_GCCollectByLoadAfter(1.5f, true ,() =>
            //    //BeforeBachMapAction(_GetSceneType))), //씬 변경전 같은 씬인지 테스트

            //    () => StartCoroutine(Appinstance.Instance.ms_LoadingManager.CO_GCCollectByLoadAfter(1.5f, true, () =>
            //    m_DelEventManager.DELMemoryAllResetDELEvntByChangeScene(true))), //전에 존재한 메모리 리셋

            //    () => StartCoroutine(Appinstance.Instance.ms_LoadingManager.CO_GCCollectByLoadAfter(1.5f, true ,() =>
            //    StartCoroutine(_GetSystem.CO_StepNextSceneAsync(_GetSceneType)))), //씬 변경

            //    () => StartCoroutine(Appinstance.Instance.ms_LoadingManager.CO_GCCollectByLoadAfter(2f, true, () =>
            //    InstanceMap(_GetSceneType, pp_ParsingMapInfoNow))), //맵배치

            //    //() => StartCoroutine(NewLoadingManager.instance.CO_GCCollecByLoadAfter(1.5f, () =>
            //    //BachMapSave(m_GetMapInfo, InSaveMapBachType.BachProduction))), //연출 과정 로드 (Leaf형 구조체 데이터 리스트만 로드)

            //    //() => StartCoroutine(NewLoadingManager.instance.CO_GCCollecByLoadAfter(3f, () =>
            //    //BachMapSave(m_GetMapInfo, InSaveMapBachType.BachCharacter))), //캐릭터 정보 로드 (TradeModdulSystem전환)

            //    //() => StartCoroutine(NewLoadingManager.instance.CO_GCCollecByLoadAfter(3f, () =>
            //    //BachMapSave(m_GetMapInfo, InSaveMapBachType.BachObject))), //정/동적 오브젝트 정보 로드 (TradeModdulSystem전환)

            //    //() => StartCoroutine(NewLoadingManager.instance.CO_GCCollecByLoadAfter(3f, () =>
            //    //StartCoroutine(CO_ProcessTradeBachByInfo()))), //TradeModdul => 정보에 따라 씬에 배치 (내정보 제외)

            //    //() => StartCoroutine(NewLoadingManager.instance.CO_GCCollecByLoadAfter(3f, () => //내정보 전체 Trade
            //    //GameManager.instance.TestBachLoadScene(_SendUpdateCount =>
            //    //{
            //    //    Appinstance.instance.ap_DelEventManager.DELUpdateGCCollectByAction(false, () =>
            //    //    Appinstance.instance.ap_DelEventManager.DELUpdateProcess(_SendUpdateCount));
            //    //})))
            #endregion
        };

        return null;
        #region Before Logic By Load Actions (오직 참고용 필요없다면 삭제요망..)
        //System.Action[] ReturnAction = null;
        //#region InitTitleMainScene
        ////if (_GetSceneType == SetSceneType.IntroScene)
        ////ReturnAction = new System.Action[2]
        ////{
        ////() => StartCoroutine(NewLoadingManager.instance.CO_GCCollecByLoadAfter(2f, () =>
        ////Appinstance.instance.ap_DelEventManager.DELUpdateGCCollectByAction(false, () =>
        ////Appinstance.instance.ap_DelEventManager.DELUpdateProcess(1)))),

        ////() => StartCoroutine(NewLoadingManager.instance.CO_GCCollecByLoadAfter(1.5f, () =>
        ////StartCoroutine(_GetSystem.CO_StepNextSceneAsync(_GetSceneType == SetSceneType.InitTitleMainScene ?
        ////SetSceneType.TitleMainScene.ToString() : _GetSceneType.ToString()))))
        ////};
        //#endregion
        //if (_GetSceneType == SetSceneType.TitleScene)
        //#region TitleMainScene
        //ReturnAction = new System.Action[2]
        //{
        //    () => StartCoroutine(Appinstance.Instance.ms_LoadingManager.CO_GCCollectByLoadAfter(3f, true,() =>
        //    m_DelEventManager.DELMemoryAllResetDELEvntByChangeScene(true))), // (전 씬의) 메모리 해제

        //    () => StartCoroutine(Appinstance.Instance.ms_LoadingManager.CO_GCCollectByLoadAfter(3f, true, () =>
        //    StartCoroutine(_GetSystem.CO_StepNextSceneAsync(_GetSceneType)))), // 씬전환

        ////() => StartCoroutine(NewLoadingManager.instance.CO_GCCollecByLoadAfter(3f, () =>
        ////StartCoroutine(CO_ProcessTradeBachByInfo()))), //모든 (정/동적 + 캐릭터) 정보 배치 (TradeModdulSystem<클래스>는 정적으로 이미 배치됨)

        ////() => StartCoroutine(AppInstance.Instance.ms_Loadingmanager.CO_GCCollectByLoadAfter(3f, true,() => //모든 내정보 배치
        ////GameManager.instance.TestBachLoadScene(_SendUpdateCount =>
        ////{
        ////    Appinstance.instance.ap_DelEventManager.DELUpdateGCCollectByAction(false, () =>
        ////    Appinstance.instance.ap_DelEventManager.DELUpdateProcess(_SendUpdateCount));
        ////})))
        //};
        //#endregion
        //else if (_GetSceneType == SetSceneType.InGameScene)
        //#region InGameScene
        //ReturnAction = new System.Action[3]
        //{
        //    //() => StartCoroutine(Appinstance.Instance.ms_LoadingManager.CO_GCCollectByLoadAfter(1.5f, true ,() =>
        //    //BeforeBachMapAction(_GetSceneType))), //씬 변경전 같은 씬인지 테스트

        //    () => StartCoroutine(Appinstance.Instance.ms_LoadingManager.CO_GCCollectByLoadAfter(1.5f, true, () =>
        //    m_DelEventManager.DELMemoryAllResetDELEvntByChangeScene(true))), //전에 존재한 메모리 리셋

        //    () => StartCoroutine(Appinstance.Instance.ms_LoadingManager.CO_GCCollectByLoadAfter(1.5f, true ,() =>
        //    StartCoroutine(_GetSystem.CO_StepNextSceneAsync(_GetSceneType)))), //씬 변경

        //    () => StartCoroutine(Appinstance.Instance.ms_LoadingManager.CO_GCCollectByLoadAfter(2f, true, () =>
        //    InstanceMap(_GetSceneType, pp_ParsingMapInfoNow))), //맵배치

        //    //() => StartCoroutine(NewLoadingManager.instance.CO_GCCollecByLoadAfter(1.5f, () =>
        //    //BachMapSave(m_GetMapInfo, InSaveMapBachType.BachProduction))), //연출 과정 로드 (Leaf형 구조체 데이터 리스트만 로드)

        //    //() => StartCoroutine(NewLoadingManager.instance.CO_GCCollecByLoadAfter(3f, () =>
        //    //BachMapSave(m_GetMapInfo, InSaveMapBachType.BachCharacter))), //캐릭터 정보 로드 (TradeModdulSystem전환)

        //    //() => StartCoroutine(NewLoadingManager.instance.CO_GCCollecByLoadAfter(3f, () =>
        //    //BachMapSave(m_GetMapInfo, InSaveMapBachType.BachObject))), //정/동적 오브젝트 정보 로드 (TradeModdulSystem전환)

        //    //() => StartCoroutine(NewLoadingManager.instance.CO_GCCollecByLoadAfter(3f, () =>
        //    //StartCoroutine(CO_ProcessTradeBachByInfo()))), //TradeModdul => 정보에 따라 씬에 배치 (내정보 제외)

        //    //() => StartCoroutine(NewLoadingManager.instance.CO_GCCollecByLoadAfter(3f, () => //내정보 전체 Trade
        //    //GameManager.instance.TestBachLoadScene(_SendUpdateCount =>
        //    //{
        //    //    Appinstance.instance.ap_DelEventManager.DELUpdateGCCollectByAction(false, () =>
        //    //    Appinstance.instance.ap_DelEventManager.DELUpdateProcess(_SendUpdateCount));
        //    //})))
        //};
        //#endregion
        //return ReturnAction;
        #endregion
    }

    #endregion

    #region Map Sub Beach Data Reference

    IEnumerator CO_BeforeBachMapAction(SetSceneType _CurrType, SetSceneType _BeforeType)
    {
        if ((_CurrType != SetSceneType.InGameScene).
        HDebug("맵 배치 및 적용은 오르지 인게임씬 진입때만 이루어집니다.!", Helper.HDType.Error) ||
        _BeforeType != SetSceneType.InGameScene) 
        {
            m_LoadingManager.DELManagerUpdateGCCollect(true, 1); 
            yield return null; 
        }

        //만일 GamePlaySystem<클래스>에서 해당 구역으로 끝날때 적용되는 함수를 대리자 나 인터페이스를 통해 실행할 수 있다
        //이거 GamePlayManager<클래스>에서 End지점에 존재하는데 GamePlaySystem으로 변경하면 되겠다
        Destroy(GamePlaySystem.Instance.gameObject);
        foreach (NewPlayerCharacterController x in FindObjectsOfType<NewPlayerCharacterController>().ToList())
        {
            if (x.pp_isMine) m_InitMyinfo.RemoveBySettingMyCharacter(x);
            else Destroy(x.gameObject);
            yield return new WaitForSeconds(0.1f);
        }
        m_LoadingManager.DELManagerUpdateGCCollect(true, 1);
    }

    private void InstanceMap(SetSceneType _CurrType, MapInfo _GetMapInfo)
    {
        if (_CurrType != SetSceneType.InGameScene)
        { Debug.LogError("맵 배치 및 적용은 오르지 인게임씬 진입때만 이루어집니다.!!"); return; }

        GameObject ParsingObj = _GetMapInfo.s_isUseStaticScene ?
        GameObject.FindGameObjectWithTag("MapModule") : Instantiate(_GetMapInfo.s_PrefabObjs);
        MySavedMapReference? GetMapSavedInfo = m_InitMyinfo.GetFindSavedMyInfoMapReferByIDX(_GetMapInfo.s_ItemIDX);
        pp_ParsingPrograssMapNow = ParsingObj.GetComponent<LocalMapDataModule>().Initlization(_GetMapInfo, GetMapSavedInfo);
        Destroy(FindObjectsOfType<Light>().ToList().Find(x => x.type == LightType.Directional).gameObject);
        if (!_GetMapInfo.s_isUseStaticScene)
        {
            RenderSettings.skybox = Instantiate(_GetMapInfo.s_SkyBox);
            RenderSettings.sun = Instantiate(_GetMapInfo.s_MainDirectionalLight);
            RenderSettings.fogColor = _GetMapInfo.s_FogColor;
            RenderSettings.fogDensity = _GetMapInfo.s_FogDensity;
            RenderSettings.fog = _GetMapInfo.s_FogDensity > 0f;
        }
        UnityEngine.Rendering.VolumeProfile GetVP = _GetMapInfo.s_MainDefultVolumeProfile;
        #region Map Volume Apply VP Reference
        Camera GetMainCam = Camera.main;
        UniversalAdditionalCameraData GetURPCamData = GetMainCam.GetUniversalAdditionalCameraData();
        GetURPCamData.renderPostProcessing = true;
        GameObject VolumeObj = new GameObject(GetVP.name);
        VolumeObj.transform.SyncObjPosRot(GetMainCam.transform, true);
        VolumeObj.CheckComnectComponent<UnityEngine.Rendering.Volume>().profile = GetVP;
        #endregion
        m_LoadingManager.DELManagerUpdateGCCollect(true, 1);
    }

    #endregion

    #region Sub System Public Functions  (**Apply ControllerBase Data Set Reference**)

    public T FindComponentByTemp<T>() where T : Component
    {
        var GetComponent =
        pp_RequiredPrefabByTemporyObjs.HFind(x => x.GetComponent<T>() != null);
        return GetComponent.GetComponent<T>();
    }

    public System.Tuple<T, I_PurifiedParamsData> GetCompareComponentOutParams<T>(bool _isFindInstance, string _ParsingTmepKey) where T : ControllerBase
    {
        I_PurifiedParamsData ReturnValue = null;
        if (_isFindInstance && FindAnyObjectByType<T>() != null)
        {
            ReturnValue = CompareClassType<I_PurifiedParamsData>(FindAnyObjectByType<T>(), _ParsingTmepKey);
            return new (FindAnyObjectByType<T>(), ReturnValue);
        }

        var ApplyGameObj = pp_RequiredPrefabByTemporyObjs.HFind(x => x.GetComponent<T>() != null);
        if (ApplyGameObj == null) return null;
        ReturnValue = CompareClassType<I_PurifiedParamsData>(ApplyGameObj.GetComponent<T>(), _ParsingTmepKey);
        return new(ApplyGameObj.GetComponent<T>(), ReturnValue);

    }

    //CharacterMyInfo전용 함수이기 때문에 위의 함수와 오버로딩을 할 수 있는 방안을 간구하여 코드 간소화 요망 ****
    public System.Tuple<bool, ControllerBase, I_PurifiedParamsData>[] GetCompareComponents(
    params System.Tuple<System.Type, bool, string>[] _GetParams)
    {
        List<System.Tuple<bool, ControllerBase, I_PurifiedParamsData>> ReturnValue = new();
        _GetParams.HForEach(x =>
        {
            ControllerBase ReciveBase = null; I_PurifiedParamsData OutPutPD = null;
            if (x.Item2 && FindAnyObjectByType(x.Item1) != null)
            {
                OutPutPD = CompareClassType<I_PurifiedParamsData>(FindAnyObjectByType(x.Item1), x.Item3);
                ReciveBase = (ControllerBase)FindAnyObjectByType(x.Item1);
            }
            else
            {
                var ApplyGameObj = pp_RequiredPrefabByTemporyObjs.HFind(y => y.GetComponent(x.Item1) != null);
                if (ApplyGameObj != null)
                {
                    OutPutPD = CompareClassType<I_PurifiedParamsData>(ApplyGameObj.GetComponent(x.Item1), x.Item3);
                    ReciveBase = (ControllerBase)ApplyGameObj.GetComponent(x.Item1);
                }
            }
            ReturnValue.Add(new(!x.Item2, ReciveBase, OutPutPD));
        });
        return ReturnValue.ToArray();
    }

    public T GetCompareComponent<T>(bool _isFindInstance, string _ParsingTmepKey, ref I_PurifiedParamsData _OutPutPD) where T : Component
    {
        if (_isFindInstance && FindAnyObjectByType<T>() != null)
        {
            _OutPutPD = CompareClassType<I_PurifiedParamsData>(FindAnyObjectByType<T>(), _ParsingTmepKey);
            return FindAnyObjectByType<T>();
        }

        var ApplyGameObj = pp_RequiredPrefabByTemporyObjs.HFind(x => x.GetComponent<T>() != null);
        if (ApplyGameObj == null) return null;
        //var ApplyGameObj = Instantiate(ReturnValue, _Pos, _Rot, _Parent); //다른곳에서 인스턴싱된다
        _OutPutPD = CompareClassType<I_PurifiedParamsData>(ApplyGameObj.GetComponent<T>(), _ParsingTmepKey);
        return ApplyGameObj.GetComponent<T>();
    }

    public ControllerBase.I_PurifiedCData CompareInfoType<T>(string _ParsingTmepKey) where T : ControllerBase.I_PurifiedCData => typeof(T).Name switch
    {
        nameof(NewActionInfo) => LoadCompareTempCInfo<T>(_ParsingTmepKey),
        _ => null
    };

    private T CompareClassType<T>(UnityEngine.Object _CompareCompoent, string _KeyIDX) where T : class => _CompareCompoent switch
    {
        RenewalCinemachineCameraController or CameraControllerBase or BInputController or AnimationController or TPSPackageSubModuleController
        => LoadCompareTempCParamInfo(_CompareCompoent as ControllerBase, _KeyIDX) as T,
        //이후에 등장하는 DataManager<클래스>역시 해당 방식을 이용하여 파싱 후 참조할 수 있도록 수정
        _ => null
    };

    #endregion

    #region 유니티 이벤트 함수
    void Start()
    {
        if (!m_isSyncParsingOnlyThisData) return;

        Initlization();
    }

    #endregion
}

public interface I_Data
{
    //Do Not Define....
}

//상속한 인터페이스는 DB에 포함되지 않는 ItemInfo 와 같은
//데이터에 대한 표식을 위해 선언 
//스크립터블데이터화 지만 DB에 포스팅 되지 않을것만
public interface I_NoneDBData 
{
    //Do Not Define....
}

public interface I_SharedKeyData
{
    public int I_GetKeyData();
}

//스크립터블데이터화가 아니고 단일 데이터 송수신에 필요할때만
public interface I_Buffer
{

}


[System.Serializable]
public struct SetSceneChangeControllerInfo
{
    public SetSceneType s_SetSceneType;
    public SceneStarterAction[] s_AllSceneStarterAction;
}


[System.Serializable]
public struct SceneStarterAction
{
    public AllGameStartProcessType s_GetProcessType;
    public string[] s_ClassNames;
}
