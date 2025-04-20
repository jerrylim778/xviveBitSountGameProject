using System.Collections.Generic;
using System.Collections;
using System.Linq;
using UnityEngine;
using Sirenix.OdinInspector;
using Commons.Helpers;
using I_PurifiedParamsData = ControllerBase.I_PurifiedParamsData;

public class NewCharacterController : ControllerBase, I_SubModulesCollection, I_SubModulesDivide
{
    #region ******(이후 다른 프로젝트에서 반드시 유사점을 발견하고 대부분 일치할시 상속하여 사용할것)*******
    //하지만 대부분 CharacterController<클래스>와의 유사점이 대부분 존재하기에 
    //다른 MainController들이 해당 NewCharacterController<클래스>를 참고하여 적용할 수 있도록 수정할것
    #endregion
    #region CharacterController에 관한 최소한의 개발 규칙에 관한 설명
    //CharacterController의 Medel(Data)를 받아들이는 시점 =>
    //(데이터 => 각 구조체 의 활용 객체로 변환) 하는것은 비슷하지만
    //계산하는 Controller를 분리할지 아님 그 조차도 따로 관리할지 
    //결정하여 해당 클래스에서 작업할 수 있도록 한다 
    #endregion
    #region OutPut할 수 있는 Controller의 경우의 수 에 대한 설명
    //각 게임의 장르에 따라
    //캐릭터가 나올 수 있고 
    //각 요소끼리 가져와야 하는 요소끼리의 Controller(GamePlaySystem 의 서브 격이 되는 Controller)
    //가 게임의 주 요소가 될 수 있기 때문에 기존 캐릭터를 중심으로 맞췄던 
    //템플릿 로직을 더 모듈화 (스크립터블 데이터화 + 각 객체끼리로 묶어서 파싱할 수 있는 방법)
    //하여 템플릿 로직에서 각 Controller의 OutPut에 따라 분할 적용으로 리팩토링을 해야한다 (일일업무 메모장 참고)
    //*(경우의 수 에 따라 캐릭터 + 요소 Controller 모두 배치되어야 할 수 도 있다)
    #endregion

    [Header("===CharacterController Reference===")]
    [Header("===CharacterDatas(Model)===")]
    [SerializeField] UserInfo m_UserInfoNow; //모델에서 가져온뒤 ReadOnly로 전환
    [SerializeField, ReadOnly] private NewActionInfo m_NewActionInfoNow; //모델에서 가져온뒤 ReadOnly로 전환해야됨
    
    [Header("===RequiredCostomComponent===")]
    [SerializeField, ReadOnly] private CharacterItemInfo m_ApplyGameItemInfo;
    [SerializeField, ReadOnly] private CharacterICNRequiredPopUp m_ConnectRequiredPopUp;
    [SerializeField, ReadOnly] protected SubModuleControllerBase[] m_RequiredSubModuleController;
    [SerializeField, ReadOnly] protected SubModuleControllerBase[] m_FlammabilitySubModuleController;
    [Header("Control Value => TestMode")]
    [SerializeField, ShowIf(nameof(m_isTestMode))] private int m_TempApplyIDX = -1;
    [Tooltip("Required Values")]
    private System.Action<GamePlayManager.PlayConditionType> m_MainConditionCallBack;
    private int m_isSubModuleInitCount = 0; //가연성 다시 플레이할때 0으로 바뀐다 (연출 대기시간)
    
    //Property
    public NewActionInfo? pp_ActionInfoNow { get => m_NewActionInfoNow; }
    public NewActionInfo? pp_ListenActionInfo { get; private set; }
    public FlammabilityOrderInfo pp_FlammabilityOrderInfo { get; private set; }

    public bool pp_isSubModulesAllComplate { get => /*m_FlammabilitySubModuleController == null ? false :*/
    m_isSubModuleInitCount >= m_FlammabilitySubModuleController.Length + 1 /*1은 자기자신*/; }

    #region Only Used Test Mode Reference
    private void StartTestMode()
    {
        CharacterItemInfo GetCInfo = Appinstance.Instance.ms_ScriptableObjectManager.
        SDataParsingByItemIDX<SDataCharacterInfo, CharacterItemInfo>(m_TempApplyIDX);
        Initlization(GetCInfo);
        Appinstance.Instance.ms_DelEventManager.DEL_RequiredParsing_Complate -= StartTestMode;
    }

    #endregion

    public override object[] Initlization(params object[] _ParsingParams)
    {
        pp_FlammabilityOrderInfo = new FlammabilityOrderInfo(this);
        //Other Init Or Test
        bool isLocalApplyData = CheckAndApplyByParams<CharacterItemInfo>(_ParsingParams, _ApplyCallBack => m_ApplyGameItemInfo = _ApplyCallBack);
        //Mine Init Reference
        if (!isLocalApplyData && !m_isTestMode) 
        {
            CheckAndApplyByParams<UserInfo>(_ParsingParams, _ApplyCallBack => m_UserInfoNow = _ApplyCallBack);
            if (CheckAndApplyByParams<MyInfo>(_ParsingParams, _ApplyCallBack => m_ReciveMyInfo = _ApplyCallBack)) //내정보일경우 
            {
                CheckAndApplyByParams<System.Action<GamePlayManager.PlayConditionType>>(_ParsingParams, _ApplyCallBack => m_MainConditionCallBack = _ApplyCallBack);
                m_UserInfoNow = (m_ReciveMyInfo as CharacterMyInfo).spp_SavedUserInfo;
            }
            pp_isTempMine = true;
        }
        m_NewActionInfoNow = isLocalApplyData ? InitCInfoDataToActionInfo(m_ApplyGameItemInfo) : m_UserInfoNow.s_NewActionInfo.Value; //Apply Model To Controller
        //SubModules Init
        I_InitRequiredSubModules(m_NewActionInfoNow.s_SubModuleBase.ToList().FindAll(x =>
        x.pp_isRequiredSubModule).ToArray(), m_NewActionInfoNow.s_RequiredSubModuleParams);

        if (pp_isMine) //스택에 미리 존재하는지의 여부를 GamePlaySystem<클래스>에 정의하여 중복된 팝업이 나올 수 없도록 수정할것
        {
            m_ConnectRequiredPopUp = GamePlaySystem.Instance.ExecutePopUpStack<CharacterICNRequiredPopUp>();
            m_ConnectRequiredPopUp.InitCharacterConnectPopUp(this);
            GamePlaySystem.Instance.MainPopUpPush<CharacterICNRequiredPopUp>();
        }

        #region 참고할 수 있는 여타 로직
        //타이쿤 필수팝업 만든뒤 보낼 수 있도록 한다 (혹은 다른 지점에서 선 필 수 팝업이 나타날 수 있도록 설계 및 구현할것)
        //GamePlaySystem.Instance.ExecutePopUpStack<TestMFLGameRequiredPopUp>().InitTestMFLRPopUp(this, m_MFLActionInfoNow, GetCardItemInfo);
        //GamePlaySystem.Instance.MainPopUpPush<TestMFLGameRequiredPopUp>();

        //다시 시작하는 메카니즘만 참고해서 사용하고 삭제 처리할것
        //I_MainEventManager().Notify( //테스트 목적이 크기 때문에 다른곳으로 이전될 가능성이 존재 이전 요망...
        //EventListenType.TestMFLGameRequiredPopUp_TestReStartGameBtn, new Event_TestMFLGame_ReStartBtn(GetCardItemInfo, () =>
        //{
        //    if (!m_isActionProcess) return true;
        //    m_isActionProcess = false; m_isSubModuleInitCount = 0;
        //    GamePlaySystem.Instance.ExecuteOutPutSystemBase(SystemInfoType.WPMB_PlayMoterMove);
        //    m_AllInstanceObjs.ForEach(x => Destroy(x)); m_AllInstanceObjs.Clear();
        //    I_BuilderByActionBoundary(m_MFLActionInfoNow as I_PurifiedCData);
        //    return false;
        //}));
        #endregion

        //Init Event Manager 
        //m_StaticEventTypes.HForEach(x => I_DivisionTypes().Subscribe(x, I_OrderEventDefine));

        I_BuilderByActionBoundary(m_NewActionInfoNow as I_PurifiedCData);
        return null;
    }

    #region Main System Private Functions

    //반드시 인터페이스 다중 상속을 통한 재정의 형태로 되야 된다 (리팩요망..)
    private void I_BuilderByActionBoundary(I_PurifiedCData _SetInfo)
    {
        if ((!(_SetInfo is NewActionInfo)).HDebug(
        $"{_SetInfo}타입은 {nameof(NewActionInfo)}이 아닙니다.!", Helper.HDType.Error)) return;

        if (_SetInfo is NewActionInfo AllNewActionInfo &&
        m_NewActionInfoNow.s_ActionInfoLocalIDX != AllNewActionInfo.s_ActionInfoLocalIDX)
            m_NewActionInfoNow = AllNewActionInfo;

        #region SubModuleControllers 초기화 및 적용
        if (m_FlammabilitySubModuleController != null)
        {
            m_FlammabilitySubModuleController.HForEach(x =>
            {
                x.ExecuteChangeSubController(); 
                if(!x.pp_isInstanceOnlyOnce) Destroy(x.gameObject); 
            });
            m_FlammabilitySubModuleController = null;
        }

        if (m_NewActionInfoNow.s_SubModuleBase != null && m_NewActionInfoNow.s_SubModuleBase.Length > 0)
        {
            var ApplyFAB = m_NewActionInfoNow.s_SubModuleBase.ToList().FindAll(x => !x.pp_isRequiredSubModule);
            m_FlammabilitySubModuleController = new SubModuleControllerBase[ApplyFAB.Count];
            int CountIDX = 0; ApplyFAB.HForEach(x =>
            {
                if (x != null)
                {
                    SubModuleControllerBase GetBase = x.pp_isInstanceOnlyOnce && x.gameObject.activeInHierarchy?
                    x : Instantiate(x, default, default, x.pp_isInstanceOnlyOnce ? null : this.transform); //위치와 회전이 각각 마다 변경될 수도 있음
                    m_FlammabilitySubModuleController[CountIDX] = GetBase;
                    GetBase.Initlization(this, m_NewActionInfoNow);
                }
                CountIDX++;
            });
        }
        #endregion

        I_CheckSubModuleIsAllCanAction(this);
    }

    #endregion

    #region Main System Public Function (**State Reference**)

    public NewActionInfo? ChangeState(NewActionInfo _ActionInfo, bool _isBeforeTypeRemember = false)
    {
        pp_ListenActionInfo = _isBeforeTypeRemember ? m_NewActionInfoNow : null;
        m_NewActionInfoNow = _ActionInfo; m_isSubModuleInitCount = 0;
        I_BuilderByActionBoundary(m_NewActionInfoNow);
        return pp_ListenActionInfo;
    }

    #endregion

    #region Main System Private Functions (**Parsing Generator By Info Init Reference**)

    //내정보에서 파생된 데이터가 아닌 경우 직접적인 NewActionInfo를 자체적으로 뽑아 사용할 수 있도록 수정한다
    //또는 NewActionInfo를 직접적으로 집어넣어서 다른결과를 도출하는방법도 존재한다 (GeneratorSubModuleController<클래스> 참고)
    private NewActionInfo InitCInfoDataToActionInfo(CharacterItemInfo _GetCInfo)
    {
        if ((_GetCInfo.s_CharacterType == CharacterType.MainUserCharacter).HDebug(
        $"사용자 전용 캐릭터는 {nameof(CharacterMyInfo)}에서만 생성 가능합니다.!", Helper.HDType.Error))
        throw new System.NotImplementedException();

        if(_GetCInfo.s_CharacterType == CharacterType.NPC)
            this.gameObject.layer = LayerMask.NameToLayer("NPC");

        NewActionInfo? GetNAInfo = null;
        var GetDataManager = Appinstance.Instance.ms_DataManager;
        GetNAInfo = (NewActionInfo)GetDataManager.CompareInfoType<NewActionInfo>(_GetCInfo.s_NewActionInfoIDX.ToString());
        if (GetNAInfo != null)
        {
            var GstInfo = GetNAInfo.Value;
            GstInfo = GeneratorRequiredByCInfo(GetDataManager, _GetCInfo, GstInfo);
            GstInfo.s_ApplyGameCharacterInfo = _GetCInfo;
            GetNAInfo = GstInfo;
        }
        return GetNAInfo != null && GetNAInfo.HasValue ? GetNAInfo.Value : throw new System.NotImplementedException();
    }

    private NewActionInfo GeneratorRequiredByCInfo(DataManager _GetDManager, CharacterItemInfo _GetCInfo, NewActionInfo _ApplyActionInfo)
    {
        //사실상 처음 시작시 내 캐릭터에만 카메라가 타겟팅 되어있다고 가정하고 로직을 산정한다(**아닐시 변경해얗됨**)
        //GetBase.Add(_GetDManager.GetCompareComponent<CameraController>(true, CameraController.ViewType.TPS.ToString(), ref RefPData));
        var ApplyControllerBase = new List<SubModuleControllerBase>();
        var ApplyParamsData = new List<I_PurifiedParamsData>();
        if (_GetCInfo.s_CharacterUsePowerType != Commons.PowerType.NotNeedAnyPower)
        {
            var ElemSetInfo = _GetDManager.GetCompareComponentOutParams<BInputController>(false, _GetCInfo.s_CharacterUsePowerType.ToString());
            ApplyControllerBase.Add(ElemSetInfo.Item1); ApplyParamsData.Add(ElemSetInfo.Item2);
        }
        if (_GetCInfo.s_CalculateInfoIDX != -1)
        {
            var ElemSetInfo = _GetDManager.GetCompareComponentOutParams<AnimationController>(false, _GetCInfo.s_CalculateInfoIDX.ToString());
            ApplyControllerBase.Add(ElemSetInfo.Item1); ApplyParamsData.Add(ElemSetInfo.Item2);
        }
        if (_ApplyActionInfo.s_SubModuleBase != null && _ApplyActionInfo.s_SubModuleBase.Length > 0)
        {
            var GetOGList = _ApplyActionInfo.s_SubModuleBase.ToList();
            GetOGList.AddRange(ApplyControllerBase.ToArray());
            _ApplyActionInfo.s_SubModuleBase = GetOGList.ToArray();
        }
        else _ApplyActionInfo.s_SubModuleBase = ApplyControllerBase.ToArray();
        _ApplyActionInfo.s_RequiredSubModuleParams = ApplyParamsData.ToArray();
        return _ApplyActionInfo;
    }
    #endregion

    #region (**MainProcess => MainModule**) 

    public override void CallEndController(bool _isSubCheck, I_Data AppliedDataSet = null)
    {
        base.CallEndController(_isSubCheck, AppliedDataSet);
        I_GetInstanceAllSubController().HForEach(x => x.CallEndController(_isSubCheck, AppliedDataSet));
        GamePlaySystem.Instance.ExecutePopUpStack<EndGamePopUp>().InitEndGamePopUp(_isSubCheck, 0.32f, //이후 타이쿤종류의 DataManager<클래스>에서 가져올 수 있도록 수정한다
        new System.Tuple<EndPopUpClickType, System.Action>(_isSubCheck ? EndPopUpClickType.DefultReStart : EndPopUpClickType.UseItemContinue, null),
        new System.Tuple<EndPopUpClickType, System.Action>(_isSubCheck ? EndPopUpClickType.GoNextLevel : EndPopUpClickType.UseItemReStart, null));
        GamePlaySystem.Instance.MainPopUpPush<EndGamePopUp>();
    }

    public override void ForcePlayOrStopOrder(bool _isPlay)
    {
        base.ForcePlayOrStopOrder(_isPlay);
        I_GetInstanceAllSubController().HForEach(x => x.BreakPoint(_isPlay, SubModuleControllerBase.SubModuleBreakType.BreakAll));
    }

    #endregion

    #region (**MainProcess => SubModules**) Init Reference System Functions

    //이후에 GamePlayManager<클래스>와 함께 CheckInitMain<> 템플릿형 인터페이스를 구현하여
    //각 템플릿에 적용하는 구간데로 최종 플레이 승인을 할 수 있도록 리팩토링 요망**********************
    public void I_CheckSubModuleIsAllCanAction(ControllerBase _GetSubBase)
    {
        //내 자신만 해당됨
        if (!pp_isMine || (!this.Equals(_GetSubBase) && !m_FlammabilitySubModuleController.Contains(_GetSubBase)).HDebug(
        $"{_GetSubBase}는 현재 {this.gameObject}에 가지고 있지 않는 {nameof(SubModuleControllerBase)} 입니다.!")) return;

        m_isSubModuleInitCount++;
        if (pp_isSubModulesAllComplate)
        {
            //Appinstance.Instance.ms_AudioManager.PlaySound("videoplayback");//GamePlayManager<클래스>에서 브금을 실행하는게 맞음
            if (m_MainConditionCallBack == null)
            {
                Appinstance.Instance.ms_GamePlayManager.ExecuteChangeAllGameControll(GamePlayManager.GamePlayType.Play);
                return;
            }
            m_MainConditionCallBack(GamePlayManager.PlayConditionType.StartingProudctionDone);
            m_MainConditionCallBack = null;
        }
    }

    public void I_InitRequiredSubModules(SubModuleControllerBase[] _GetBase, I_PurifiedParamsData[] _CompareParamsDatas)
    {
        if (_GetBase == null || _GetBase.Length <= 0 ||
        _CompareParamsDatas == null || _CompareParamsDatas.Length <= 0) return;

        m_RequiredSubModuleController = new SubModuleControllerBase[_GetBase.Length];
        int CountIDX = 0; _GetBase.HForEach(x =>
        {
            if ((!x.pp_isRequiredSubModule).HDebug(
            $"{x.name}은 필수 SubModule이 아닙니다.!", Helper.HDType.Error)) return;
            int FIDX = -1; FIDX = _CompareParamsDatas.HFindIndex(y => y.I_ISGetOutParams(x));
            if (x != null && !x.pp_isInitComplate && FIDX != 1 && (!x.pp_isInstanceOnlyOnce || !x.gameObject.activeInHierarchy))
            {
                x.Initlization(this, _CompareParamsDatas[FIDX]);
                m_RequiredSubModuleController[CountIDX] = x;
            }
            CountIDX++;
        });
    }

    public T I_GetSubModule<T>() where T : SubModuleControllerBase
    => ISCheckSubModule<T>(out T _OutPutBase) ? _OutPutBase : null;

    public bool ISCheckSubModule<T>(out T _OutPutBase) where T : SubModuleControllerBase
    {
        _OutPutBase = null; int FIDX = -1; FIDX =
        m_FlammabilitySubModuleController.HFindIndex(x => x.GetType().Name == typeof(T).Name);
        if (FIDX != -1)
        {
            _OutPutBase = m_FlammabilitySubModuleController[FIDX] as T;
            return true;
        }
        return false;
    }

    public SubModuleControllerBase[] I_GetInstanceAllSubController()
    {
        var GetAllSubControlList = new List<SubModuleControllerBase>();
        if(m_RequiredSubModuleController != null) GetAllSubControlList.AddRange(m_RequiredSubModuleController);
        if (m_FlammabilitySubModuleController != null) GetAllSubControlList.AddRange(m_FlammabilitySubModuleController);
        return GetAllSubControlList.ToArray();
    }

    #endregion

    #region (**MainProcess => EventManager**) (현재는 사용하지 않음)

    //public EventManager I_MainEventManager() => Appinstance.Instance.ms_EventManager;

    //public EventListenType[] I_DivisionTypes() => I_MainEventManager().CheckAllEventTypeByTemp<MFLController>();

    //public void I_OrderEventDefine(EventListenType _GetType, I_EventData _GetInter = null)
    //{
    //    if ((!m_StaticEventTypes.Contains(_GetType)).HDebug(
    //    $"[{nameof(EventManager)}이벤트 잘못된 등록] {_GetType}은 {nameof(MFLController)}에", Helper.HDType.Error)) return;

    //    switch (_GetType)
    //    {
    //        //씬 재진입 구현이후 GUI 포함 삭제 요망..
    //        case EventListenType.MFLController_ReStart: //원래 재시작은 씬을 다시 와야되기 때훈 해당 과정이 사실상 필요치 않다 **(즉 이후 삭제후 리팩토링을 실시한다)**
    //            MFLGameItemInfo[] GetCardItemInfo = m_MFLActionInfoNow.s_ApplyGameLevelInfo.oro_ApplyOutCardItems;
    //            //UIActions
    //            I_MainEventManager().Notify(EventListenType.
    //            TestMFLGameRequiredPopUp_ReStartGame, new Event_TestMFLGame_ReStartBtn(GetCardItemInfo, null));
    //            //ControllerActions
    //            GamePlaySystem.Instance.ExecuteOutPutSystemBase(SystemInfoType.WPMB_PlayMoterMove);
    //            m_isActionProcess = false; m_isSubModuleInitCount = 0;
    //            m_AllInstanceObjs.ForEach(x => Destroy(x)); m_AllInstanceObjs.Clear();
    //            I_BuilderByActionBoundary(m_MFLActionInfoNow as I_PurifiedCData);
    //            break;
    //        case EventListenType.MFLController_Continue: //각 인게임 상황에 따라 다른것을 적용한뒤 GameManager 상태를 Play
    //            break;
    //    }
    //}

    #endregion

    #region 유니티 이벤트 함수

    private void Start()
    {
        if (m_isTestMode && this.gameObject.scene.name != null) //런타임 이전에 미리 씬에 배치되어있는지 확인하는 로직
        Appinstance.Instance.ms_DelEventManager.DEL_RequiredParsing_Complate += StartTestMode;
    }

    protected override void Update()
    {
        if (!m_isActionProcess) return;

        //if (UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject()) //UI 이벤트 클릭과 관련있음
        //    return;

        m_FlammabilitySubModuleController.HForEach(x => x.ProcessUpdate());
        
    }
    #endregion
}

#region Model(Data) Reference (이후 Common이나 그에 준하는 곳으로 이전 요망)

[System.Serializable]
public struct UserInfo : I_Data
{
    //MyInfo에서 유일키 인덱스를 할당 //해당 인덱스에 따라 파싱하는 형태나 Compare로 구분하기 위함
    [ReadOnly] public string s_DBSavedIDX;
    public Vector3 s_SavedPos, s_SavedRot;
    [SerializeField, ShowIf("@s_NewActionInfo != null")] private NewActionInfo s_OutPutNewActionInfo;
    public NewActionInfo? s_NewActionInfo { get; private set; }

    //기본형 향후 생성자를 추가해야됨
    public UserInfo(string _UserDBMainIDX, NewActionInfo? _NewActionInfo = null)
    {
        s_DBSavedIDX = _UserDBMainIDX;
        s_NewActionInfo = _NewActionInfo;
        s_SavedPos = default; s_SavedRot = default;
         s_OutPutNewActionInfo = default(NewActionInfo);
        if (s_NewActionInfo != null && s_NewActionInfo.HasValue) 
        s_OutPutNewActionInfo = s_NewActionInfo.Value;
    }
}

[System.Serializable]
public struct NewActionInfo : ControllerBase.I_PurifiedCData, ControllerBase.I_PurifiedAddKey
{
    public int s_ActionInfoLocalIDX; //정적형태인 스크립터블 데이터을 저장한 형태를 가져오기 위한 변수
    public bool s_isShowProduction; //해당 인덱스에 따라 파싱하는 형태나 Compare로 구분하기 위함

    [BoxGroup("Force ApplyData"), SerializeField] private bool odin_isForceApplyData;
    [BoxGroup("Force ApplyData"), SerializeField, ShowIf(nameof(odin_isForceApplyData))] private bool odin_isFindCamIDX;
    [BoxGroup("Force ApplyData"), ShowIf("@odin_isForceApplyData && odin_isFindCamIDX")] public int s_ReciveCamDataByIDX;
    [BoxGroup("Force ApplyData"), ShowIf("@odin_isForceApplyData && !odin_isFindCamIDX")] public CameraController.ViewType s_ReciveCamData;
    [BoxGroup("Force ApplyData"), ShowIf(nameof(odin_isForceApplyData))] public Commons.PowerType s_CharacterUsePowerType;
    [BoxGroup("Force ApplyData"), ShowIf(nameof(odin_isForceApplyData))] public int s_CalculateIDX;

    //성격이 살짝 다를 수 있기 때문에 다른곳으로 이전할 가능성 존재
    [HideInInspector, System.NonSerialized] public CharacterItemInfo s_ApplyGameCharacterInfo; //정적 스크립터블로 가져올 수 있도록 한다
    [HideInInspector, System.NonSerialized] public SubModuleControllerBase[] s_SubModuleBase;
    [HideInInspector, System.NonSerialized] public I_PurifiedParamsData[] s_RequiredSubModuleParams;

    public string I_SubInfoIDX() => s_ActionInfoLocalIDX.ToString();

    public T FindRequiredInfo<T>() where T : SubModuleControllerBase
    => s_SubModuleBase.HFind(x => x is T) as T;

    public T FindRequiredParamsInfo<T>(ref int _GetIDX) where T : ControllerBase.I_PurifiedParamsData
    {
        int FIDX = -1; FIDX = s_RequiredSubModuleParams.ToList().
        FindIndex(x => typeof(T).Name == x.GetType().Name);
        _GetIDX = FIDX;
        return FIDX != -1 ? (T)s_RequiredSubModuleParams[FIDX] : default(T);
    }

    public System.Tuple<SubModuleControllerBase, I_PurifiedParamsData> ApplyParsingData(bool _isInstance, SubModuleControllerBase _ApplySubCB, I_PurifiedParamsData _ApplyParamsData)
    {
        var ApplySubCB = _isInstance ? UnityEngine.Object.Instantiate(_ApplySubCB) : _ApplySubCB;
        s_SubModuleBase = s_SubModuleBase == null ?
        new SubModuleControllerBase[1] { ApplySubCB } : s_SubModuleBase.ToList().Concat(new[] { ApplySubCB }).ToArray();
        s_RequiredSubModuleParams = s_RequiredSubModuleParams == null ?
        new I_PurifiedParamsData[1] { _ApplyParamsData } : s_RequiredSubModuleParams.ToList().Concat(new[] { _ApplyParamsData }).ToArray();
        return new(ApplySubCB, _ApplyParamsData);
    }
}

#endregion

//여러 객체에서 일시적인 Order를 받을때 사용에 중점을 둔 정보로 활용한다
//Character뿐 아니라 다른곳에서 필요시 사용될 수도 있기 때문에 Common등으로 이전요망 **
//현재는 사용성이 CharacterController내부에서 진행되는게 아니라면 쓸 이유가 없다 (**리팩 요망**)
public enum ConjugationOrderType
{
    None = 0,
    Quest
}

public class FlammabilityOrderInfo : I_Buffer
{
    [SerializeField] private ControllerBase s_ConjugationController;
    public Dictionary<ConjugationOrderType, I_Buffer> s_DicSavedOrder;

    public FlammabilityOrderInfo(ControllerBase _GetConBase)
    {
        s_ConjugationController = _GetConBase;
        s_DicSavedOrder = new();
    }

    public bool CheckSavedOrderByType(ConjugationOrderType _GetType) =>
    s_DicSavedOrder.ContainsKey(_GetType);

    public bool GetSavedOrderByType<T>(ConjugationOrderType _GetType, System.Action<I_Buffer> _GetCallBack) where T : I_Buffer
    {
        if ((!CheckSavedOrderByType(_GetType)).
        HDebug($"{_GetType}에 대한 정보 없음", Helper.HDType.Warning)) return false;

        _GetCallBack?.Invoke(s_DicSavedOrder[_GetType]);
        return true;
    }

    public void SetSavedOrderByType(ConjugationOrderType _GetType, I_Buffer _GetBuffer)
    {
        if (!CheckSavedOrderByType(_GetType))
        {
            s_DicSavedOrder.Add(_GetType, _GetBuffer);
            return;
        }
        //if(_GetBuffer is I_TypeBuffer _GetTypeBuffer && _GetTypeBuffer.I_GetTypeBuffer<System.Action>(
        //ReciveBufferType.EndCallBack, out System.Action _GetValue))
        //_GetValue?.Invoke();
        s_DicSavedOrder.Remove(_GetType);
        s_DicSavedOrder.Add(_GetType, _GetBuffer);
    }
}
