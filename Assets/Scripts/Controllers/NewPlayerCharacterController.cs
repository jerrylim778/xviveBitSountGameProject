using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Commons;
using Commons.Helpers;
using Commons.Helpers.Attribute;

[RequireComponent(typeof(AnimationController), typeof(Animator))] 
[RequireComponent(typeof(CapsuleCollider), typeof(Rigidbody))] 
public class NewPlayerCharacterController : MonoBehaviour
{
    #region 정적형태의 캐릭터에 대한 설명
    //1.먼저 정적으로 밖혀있는 구간을 사용하기에 AnimatiorController<클래스>는 Power나 그외에 
    //필 수적으로 처리할 영역만 남긴뒤 삭제 처리한다
    //2.플레이어 컨트롤러의 역활은 정적으로 박혀있는 구간을 실행하고 
    //각 구간 묶여있는 클래스들의 집합 오버로딩 역활
    //내 정보에 따른 캐릭터의 아이템 팟츠역활을 진행한다
    //3.나눌 구간은 플레이어 카메라 + 애니메이션 + 인풋 + 물리적 계산 로직이다
    #endregion
    [Header("MainControll")]
    [SerializeField] private bool isTest = false;
    [Header("MainHeader(Actions)")]
    [SerializeField] private ActionInfo m_ActionInfoNow;
    [Tooltip("MainHeader(Decoration)")]
    private NewShaderInfo m_AllNShaderInfoNow;
    private ShaderByObjRendererSystem m_MainShaderSystem;

    //Info Property
    public ActionInfo? pp_ActionInfoListen { get; private set; }
    public NewShaderInfo pp_AllNShaderInfoListen { get; private set; }

    [Header("===RequiredCostomComponent===")]
    [Lim_InspectorReadOnly, SerializeField] private CameraController m_CameraController;
    [Lim_InspectorReadOnly, SerializeField] private AnimationController m_AnimationController;
    [Lim_InspectorReadOnly, SerializeField] private BInputController m_InputController;
    [Lim_InspectorReadOnly, SerializeField] private SubModuleControllerBase[] m_GetAllSubModuleController;

    //[Tooltip("ItemDataReference")] //필요하면 사용하여 주석해제
    //private Dictionary<CollectionPlayerSavedType, CompositeCollectionBase> m_AllCompositeBaseDic;

    #region Property
    public bool pp_isMine { get; private set;}
    public Collider pp_MainColl { get; private set;}
    public Rigidbody pp_MainRB { get; private set;}
    public CameraController pp_CameraController { get { return m_CameraController; } }
    public AnimationController pp_AnimationController { get { return m_AnimationController; } }
    public BInputController pp_InputController { get { return m_InputController; } }
    #endregion

    #region Init Reference

    public void Initialization(MyInfo _ApplyMyInfo = null,
    PowerType _ApplyPower = PowerType.NotNeedAnyPower, NewShaderInfo _GetNInfo = null)
    {
        if (_ApplyPower != PowerType.NotNeedAnyPower)
        m_ActionInfoNow.s_PowerType = _ApplyPower;
        m_AllNShaderInfoNow = _GetNInfo;
        pp_isMine = _ApplyMyInfo != null;
        pp_MainColl = GetComponent<Collider>();
        pp_MainRB = GetComponent<Rigidbody>();
        m_AnimationController = GetComponent<AnimationController>();
        //if (pp_isMine) InitCharacterItemCollections(AppInstance.Instance.ms_MyInfo.s_MyInGameOutPutInfo.s_InvenInfos, null);
        pp_MainRB.constraints = RigidbodyConstraints.FreezeRotation;
        //Renderer를 찾는 과정 역시 다른곳으로 이전하여 관리하는 형태로 적용해야한다 (수정요망***)
        m_MainShaderSystem = new ShaderByObjRendererSystem(this.transform.ChildLinearStuctureSearch<Renderer>()[0].gameObject);
        
        BuilderByActionBoundary(m_ActionInfoNow);
    }

    #region 서브파티 비선형 F3패턴 아이템 부분 초기화 (현재는 사용 안함)
    //private void InitCharacterItemCollections(ItemInfo[] _InvenInfos, ItemInfo[] _AmountInfos)
    //{
    //    m_AllCompositeBaseDic = new Dictionary<CollectionPlayerSavedType, CompositeCollectionBase>();
    //    m_AllCompositeBaseDic.Add(CollectionPlayerSavedType.Coll_Inventory, this.gameObject.CheckComnectComponent<CompositeColl_Inventory>());
    //    //m_AllCompositeBaseDic.Add(CollectionPlayerSavedType.Coll_Amount, 
    //    //this.gameObject.CheckComnectComponent<CompositeColl_Amount>());
    //    m_AllCompositeBaseDic.ToList().ForEach(x =>
    //    {
    //        if (x.Value is CompositeColl_Inventory) (x.Value as CompositeColl_Inventory).
    //        Initialization<ItemInventoryInfo>(new ItemInventoryInfo(_InvenInfos)
    //        {
    //            s_DBSavedIDX = AppInstance.Instance.ms_MyInfo.pps_DBSavedIDX,
    //            s_GetCharacterController = this
    //        });
    //        //else if (x.Value is CompositeColl_Amount) (x.Value as CompositeColl_Amount).
    //        //Initialization<ItemAmountInfo>(new ItemAmountInfo(_AmountInfos)
    //        //{
    //        //    s_SaveIDX = m_MyInfo.ppm_SaveIDX,
    //        //    s_GetCharacterController = this,
    //        //    s_CharacterAnim = GetComponent<Animator>(),
    //        //});
    //    });
    //}
    #endregion

    #endregion

    #region Main System Public Function

    public void ChangeState(ActionInfo _ActionInfo, bool _isBeforeTypeRemember = false, NewShaderInfo _RunShader = null)
    {
        if (_isBeforeTypeRemember)
        {
            pp_ActionInfoListen = m_ActionInfoNow;
            pp_AllNShaderInfoListen = m_AllNShaderInfoNow;
        }
        else pp_ActionInfoListen = null;
        m_ActionInfoNow = _ActionInfo;
        m_AllNShaderInfoNow = _RunShader;
        BuilderByActionBoundary(m_ActionInfoNow);
    }

    public void ChangeState(PowerType _PowerType)
    {
        m_ActionInfoNow.s_PowerType = _PowerType;
        BuilderByActionBoundary(m_ActionInfoNow);
    }

    public void ChangeState(bool _isActivate, bool _isCamChild = true, bool _isNotBreakAnim = false) //재설계 요망****
    {
        if(m_GetAllSubModuleController != null) m_GetAllSubModuleController.ToList().ForEach(x => 
        x?.BreakPoint(_isActivate, SubModuleControllerBase.SubModuleBreakType.PartBreak));
        if (m_InputController != null) m_InputController.OrderEventPower<bool>(!_isActivate);
        if (_isNotBreakAnim && m_AnimationController != null) m_AnimationController.BreakPoint(_isActivate);

        #region 카메라 부분 이후에 카메라의 오버로딩 함수 하나로 해결할 수 있도록 수정 요망****
        if (m_CameraController != null && _isCamChild) 
        m_CameraController.SetChangeUpdateState(_isActivate ? CameraController.UpdateMode.LateUpdate : 
        CameraController.UpdateMode.NoUpdate, _isActivate ? null : GetComponent<Animator>().GetBoneTransform(HumanBodyBones.Head));
        else if (m_CameraController != null && !_isCamChild) m_CameraController.pp_UpdateMode = 
        _isActivate? CameraController.UpdateMode.LateUpdate : CameraController.UpdateMode.Update;
        #endregion

        if (_isActivate && pp_ActionInfoListen != null) 
        {
            m_AnimationController = this.gameObject.CheckComnectComponent<AnimationController>(); 
            ChangeState(pp_ActionInfoListen.Value); pp_ActionInfoListen = null; 
        }
    }

    #endregion

    #region Sub System Public Functions 
    //public T FindItemCompositeByType<T>() where T : CompositeBase
    //=> (m_AllCompositeBaseDic.ToList().Find(x => x.Value is T).Value as T);

    //public void AnimAnyCallBack()
    //=>AppInstance.Instance.ms_DelEventManager.DELMainCharacterAnimCallBack();

    public void AnimAnyCallBack(object _GetValues = null)
    {
        if (pp_AnimationController.m_AnimValuesInfo == null)
            return;
        int ChangeValue = (int)_GetValues;
        if (_GetValues == null) ChangeValue = 0;
        pp_AnimationController.m_AnimValuesInfo.s_ChangeStateIDX = ChangeValue;
    }
    

    public bool ISCheckSubModule<T>(out SubModuleControllerBase _OutPutBase) where T : SubModuleControllerBase
    {
        _OutPutBase = null; int FIDX = -1; FIDX =
        m_GetAllSubModuleController.ToList().FindIndex(x => x.GetType().Name == typeof(T).Name);
        if (FIDX != -1)
        {
            _OutPutBase = m_GetAllSubModuleController[FIDX];
            return true;
        }
        return false;
    }
    

    public void CheckAddSubModule(SubModuleControllerBase _GetSubBase)
    {
        if(!_GetSubBase.pp_isInitComplate || !pp_isMine)
        {
            Debug.LogError("서브 모듈 중간에 추가하려면 초기화가 되어있는 " +
            "상태가 되어야되고 사용하는 주체는 사용자 캐릭터여야만 합니다..!!");
            return;
        }

        m_GetAllSubModuleController = m_GetAllSubModuleController.Concat(new[] { _GetSubBase }).ToArray();
    }

    #endregion

    #region Action Logic Reference
    //후에 로 옮겨야 함

    private void BuilderByActionBoundary(ActionInfo _SetInfo)
    {
        m_ActionInfoNow = _SetInfo;

        #region SubModuleControllers 초기화 및 적용
        if (m_GetAllSubModuleController != null)
        {
            m_GetAllSubModuleController.ToList().ForEach(x =>
            { x.ExecuteChangeSubController(); Destroy(x.gameObject); });
            m_GetAllSubModuleController = null;
        }
        if (m_ActionInfoNow.s_SubModuleBase != null && m_ActionInfoNow.s_SubModuleBase.Length > 0)
        {
            List<SubModuleControllerBase> GetNewBaseList = new List<SubModuleControllerBase>();
            m_ActionInfoNow.s_SubModuleBase.ToList().ForEach(x =>
            {
                if(x != null)
                {
                    SubModuleControllerBase GetBases = Instantiate(x, this.transform);
                    GetBases.Initlization(this);
                    GetNewBaseList.Add(GetBases);
                }
            });
            m_GetAllSubModuleController = GetNewBaseList.ToArray();
        }
        #endregion

        #region Input => Animation => Phycis순으로 초기화
        //InputInit
        if (m_InputController != null) 
        {
            m_InputController.ExceptionBeforeController();
            BInputController GetController = m_InputController; m_InputController = null;
            Destroy(GetController);
        }
        if (m_ActionInfoNow.s_PowerType != PowerType.NotNeedAnyPower && m_ActionInfoNow.s_PowerType != PowerType.ListenSelfPower)
        {
            m_InputController = this.GetComponent<BInputController>() != null ?
            gameObject.GetComponent<BInputController>() : gameObject.AddComponent<BInputController>();
            m_InputController.Initialization(m_ActionInfoNow.s_PowerType);
        }
        

        //CameraController
        //해당 구간은 CaemraControllerHeader<클래스>작명으로 CameraController<클래스>를 제어하거나 원래 위치로 옮기는
        //카메라 관리자 형태의 클래스로 옮겨서 작업해야한다
        if (_SetInfo.s_PowerType == PowerType.SelfInputPower || _SetInfo.s_PowerType == PowerType.SelfJoyStickPower ||
            _SetInfo.s_PowerType == PowerType.ListenSelfPower)
        {
            bool isComplete = _SetInfo.s_PowerType == PowerType.SelfInputPower || _SetInfo.s_PowerType == PowerType.SelfJoyStickPower;
            m_CameraController = FindObjectOfType<CameraController>();
            m_CameraController.enabled = true;
            #region 캐릭터의 머리 부분을 제어하기 위한 로직 (후에 필요할 수 있기 때문(혹은 로직을 이동조치 가능)에 삭제 금지)
            //m_CameraController.Initlization(
            //Helper.ChildLinearStuctureSearch(this.transform).ToList().Find(x => x.name == "CamTarget")); //원래 로직
            //Transform GetHead = GetComponent<Animator>().GetBoneTransform(HumanBodyBones.Head);
            //m_CameraController.SetAngles(Quaternion.LookRotation(this.transform.forward));
            //m_CameraController.Initlization(GetHead, !isComplete);
            //m_CameraController.pp_UpdateMode = isComplete ? CameraController.UpdateMode.LateUpdate :
            //CameraController.UpdateMode.Update;
            #endregion
        }
        else
        {
            if (m_CameraController != null)
            {
                m_CameraController.enabled = false;
                m_CameraController.Initlization(); //다시 Null상태로 만들어줌
            }
            m_CameraController = null;
        }

        //AnimInit
        m_AnimationController.Initlization(_SetInfo.s_CalculateInfo);
        #endregion

        if(m_AllNShaderInfoNow != null)
        BuilderShaderOutPut(true, m_AllNShaderInfoNow).OutPutShader();
    }

    #endregion

    #region Decoration Logic Reference 

    #region 데코레이션에 관한 설명
    //기존 사용한 꾸미기용 서브파티 빌더 패턴 가져오도록한다
    //각 패턴에 따라 쉐이더가 캐릭터나 캐릭터의 주변 내지는 참조되는 영역까지 확장할 수 있도록 설계해야한다
    #endregion
    public void ApplyShaderInfo(NewShaderInfo _RunSShaderInfo, params object[] _InitSettingOutPutBase)
    {
        if (m_AllNShaderInfoNow != null && m_AllNShaderInfoNow.s_ItemIDX == _RunSShaderInfo.s_ItemIDX) return;
        if (_InitSettingOutPutBase != null && !_RunSShaderInfo.s_SPType.ToString().Contains(ShaderPlayerType.EventEffect.ToString()))
        m_MainShaderSystem.ApplyListenNextSettingInit(_RunSShaderInfo.s_ItemIDX, _InitSettingOutPutBase);
        m_AllNShaderInfoNow = _RunSShaderInfo;
    }

    //오직 테스트 과정 이후에 변경할지 말지를 결정해야됨
    public ShaderOutPutBase BuilderShaderOutPut(bool _isApplyMain, NewShaderInfo _RunSShaderInfo, 
    Renderer _AddRenderer = null, System.Action _EndCallBack = null)
    {
        if (((m_MainShaderSystem == null).HDebug("랜더러를 관리할 구간이 초기화 되지 않았습니다.!", Helper.HDType.Error) ||
        _RunSShaderInfo == null) && _RunSShaderInfo.s_SPType == ShaderPlayerType.PlayerMaterial && _AddRenderer != null) return null;
        if (_isApplyMain && (m_AllNShaderInfoNow == null || (m_AllNShaderInfoNow != null && m_AllNShaderInfoNow.s_ItemIDX !=
        _RunSShaderInfo.s_ItemIDX))) m_AllNShaderInfoNow = _RunSShaderInfo;
        return m_MainShaderSystem.AddActionByType(_RunSShaderInfo, _AddRenderer, _EndCallBack);
    }

    #endregion

    #region 유니티 이벤트 함수
    #region 캐릭터에서 충돌하여 아이템을 확인할시에 전개도에 설계 먼저 진행하고 구현할것
    //void OnTriggerEnter(Collider other)
    //{
    //    //후에 어떤 Tag가 비면 해당 Tag로 로직을 구성할 수 있도록
    //    if (other.GetComponent<CompositeElem_Item>() != null)
    //    {
    //        //처음에는 Amount순서대로(즉 바로 장착하는 상태로)아이템을 저장할 수 있도록 한다
    //        if (other.GetComponent<CompositeElem_Item>().pp_OutPutItemElementItem is PartsItemInfo)
    //        {
    //            m_AllCompositeBaseDic.ToList().Find(x => x.Key == CollectionPlayerSavedType.Coll_Amount).Value.AddCollectionList(
    //            other.GetComponent<CompositeElem_Item>().pp_OutPutItemElementItem as PartsItemInfo);
    //        }
    //    }
    //}
    #endregion
    #region 오직 테스트 후에 삭제 요망*****
    //캐릭터가 목표에 앉을때 발동
    //private void OnTriggerEnter(Collider other)
    //{
    //    if (other.gameObject.layer != LayerMask.NameToLayer("Ground"))
    //        return;

    //    ChangeState<GetActionOutOrder>(PlayerState.StaticAction, new GetActionOutOrder()
    //    {
    //        LayerIDX = 2,
    //        ApplyIDX = 1
    //    });
    //}
    #endregion

    private void Start()    
    {
        //매개변수는 비어있어야 한다
        if(isTest) Initialization(new MyInfo(Appinstance.Instance, ""));
    }
    #endregion
}

//ItemInfo있는쪽으로 이전요망
[System.Serializable]
public class NewShaderInfo : InstanceObjectInfo
{
    [Header("======SData Shader======")]
    public bool s_RememberShaderBase;
    public float s_StartValue, s_EndValue, s_TweeningSpeed;
    public ShaderPlayerType s_SPType;

    public ShaderOutPutBase SDataLogic_GetInstancePrefabObj(Transform _ApplyPar = null)
    {
        GameObject PrefabsInstancObj = Object.Instantiate(s_PrefabObjs, _ApplyPar);
        if (PrefabsInstancObj.GetComponent<MeshFilter>() != null)
        Object.Destroy(PrefabsInstancObj.GetComponent<MeshFilter>());
        if (PrefabsInstancObj.GetComponent<Renderer>() != null)
        Object.Destroy(PrefabsInstancObj.GetComponent<Renderer>());
        return PrefabsInstancObj.GetComponent<ShaderOutPutBase>();
    }
}

#region Apply Shader By Obj Reference (Common으로 이전 혹은 다른곳으로 이전요망)
[System.Serializable]
public class ShaderByObjRendererSystem
{
    #region Shader System 의 규칙에 관한 설명
    //1.모든 쉐이더 터리는 무조건 Renderer이 있는 기준으로 해당되는 클래스를
    //걷힌뒤에 NewShaderInfo의 타입에 의해 처리될 수 있도록 한다
    //2.자식에 Renderer가 존재할경우 발생하는 비선형구조는 각 Renderer를 찾고 거기에 System을 생성하는
    //구조로 처리할 수 있도록 설계한다 (즉 해당 클래스는 NewShaderInfo의 타입에 따라 쉐이더를 보여주는 로직만
    //생각하면 되겠다
    #endregion
    public enum TextrueType { None = 0, BaseMap, NormalMap, MetallicMap }
    [Header("Required When ShaderOutPut")]
    //[Lim_InspectorReadOnly, SerializeField] bool s_isRunningShader = false;
    [Lim_InspectorReadOnly, SerializeField] Material s_RunningShdaderMat;
    [Lim_InspectorReadOnly, SerializeField] Renderer s_AddRenderer;
    [SerializeField] private List<System.Tuple<NewShaderInfo, ShaderOutPutBase>> s_RememberShaderBaseList;
    [Tooltip("Sub SInfo Values")]
    private System.Tuple<NewShaderInfo, ShaderOutPutBase> s_ShaderRunningNow;
    private System.Action<int, ShaderOutPutBase> ListenInitSettingCallBack = null;

    public bool spp_isChangeShader { get => s_RunningShdaderMat != null && s_ShaderRunningNow != null; }
    public RendererMeshMaterialInfo? spp_RenderMeshInfo { get; private set; }


    public ShaderByObjRendererSystem(GameObject _GetMainObj)
    {
        spp_RenderMeshInfo = ParsingInitRendererInfo(_GetMainObj.GetComponent<Renderer>());
        s_RememberShaderBaseList = new List<System.Tuple<NewShaderInfo, ShaderOutPutBase>>();
    }

    #region Main System Public Functions
    //기존에 돌아가고 있던것을 그대로 출력 (물론 아직 그대로 존재했을때를 가정한다)
    //돌아가고 있던것을 변수에 해당되는 구조만 변경하여 출력하는것이기 때문에
    //리스트와는 관련이 없을을 분명히 숙지하고 넘어갈 수 있도록 한다

    public virtual ShaderOutPutBase AddActionByType(NewShaderInfo _GetSShdaerInfo, Renderer _AddRenderer = null,
    System.Action _EndCallBack = null)
    {
        bool isAddSubRenderer = _GetSShdaerInfo.s_SPType != ShaderPlayerType.PlayerMaterial;
        if ((_GetSShdaerInfo.s_SPType != ShaderPlayerType.PlayerMaterial && _AddRenderer == null).HDebug(
        $"직접적인 쉐이더의 변경이 아닌 {ShaderPlayerType.PlayerMaterial}이 아닌 경우 무조건 _AddRenderer가 필요합니다.! ",
        Helper.HDType.Error)) return null;

        bool isRunningShader = spp_isChangeShader || /*s_isRunningShader ||*/ s_AddRenderer != null;

        if (isRunningShader && s_ShaderRunningNow != null && _GetSShdaerInfo.s_ItemIDX == s_ShaderRunningNow.Item1.s_ItemIDX)
            return s_ShaderRunningNow.Item2;

        //비교하여 최종 ShaderOutPut 즉 배출할 쉐이더를 결정하기
        bool isReUseShaderSavedByList = GetFindRememberList(_GetSShdaerInfo, _GetSShdaerInfo.s_PrefabObjs.GetComponent<
        ShaderOutPutBase>(), out System.Tuple<NewShaderInfo, ShaderOutPutBase> _GetOutTuple);
        ShaderOutPutBase _GetOutPutBase = isReUseShaderSavedByList ?
        _GetOutTuple.Item2 : _GetSShdaerInfo.SDataLogic_GetInstancePrefabObj();

        //만약 돌아가있을 경우 OutPutBase의 성질에 따라 없애고 추가할 수 있거나 없다면 리턴 처리로 변경해야한다
        if (isRunningShader) RemoveWhenShaderOff(true, _GetSShdaerInfo, _GetOutPutBase);

        //진입을 알리는것을 System에 적용
        //s_isRunningShader = true;
        if (isAddSubRenderer) s_AddRenderer = _AddRenderer;
        s_ShaderRunningNow = new System.Tuple<NewShaderInfo, ShaderOutPutBase>(_GetSShdaerInfo, _GetOutPutBase);
        GameObject GetMainObj = isAddSubRenderer ? _AddRenderer.gameObject : spp_RenderMeshInfo.Value.s_GetRender.gameObject;

        #region Setting After Run Shader Reference

        //만일 전용 리스트에 있을경우 재활용하여 사용하기
        if (isReUseShaderSavedByList)
            return RuningByShaderInfos(isAddSubRenderer, GetMainObj, _GetOutTuple.Item1, _GetOutPutBase, _EndCallBack);

        //Info에서 저장하는 경우 (쉐이더를 재활용의 가능성 때문에) 전용 리스트에 저장할 수 있도록
        if (_GetSShdaerInfo.s_RememberShaderBase)
        s_RememberShaderBaseList.Add(new System.Tuple<NewShaderInfo, ShaderOutPutBase>(_GetSShdaerInfo, _GetOutPutBase));

        //만일 쉐이더를 발동하는 기준으로 쫓아오는 경우면 하위객체로 이동할 수 있도록
        if(_GetSShdaerInfo.s_SPType.ToString().Contains("Follow"))
        _GetOutPutBase.transform.transform.SetParent(GetMainObj.transform);

        return RuningByShaderInfos(isAddSubRenderer, GetMainObj, _GetSShdaerInfo, _GetOutPutBase, _EndCallBack);
        #endregion
    }

    //쉐이더를 완전히 없애거나 해당 구역에서 다른것으로 변경할때 사용함
    public virtual void RemoveWhenShaderOff(bool _isRemoveList, NewShaderInfo _GetInfo, ShaderOutPutBase _GetBase)
    {
        if ((s_ShaderRunningNow == null || s_ShaderRunningNow.Item1.s_ItemIDX != _GetInfo.s_ItemIDX || 
        !_GetBase.CheckCompareMaterialName(s_RunningShdaderMat, out string _CS)).
        HDebug("현재 돌리는 Rednerer의 쉐이더와는 다릅니다 이는 규칙 위반입니다.!", Helper.HDType.Error)) return;

        if (s_AddRenderer == null && _GetInfo.s_SPType == ShaderPlayerType.PlayerMaterial)
        SetChangedTexToApplyMat(spp_RenderMeshInfo.Value.s_GetRender, spp_RenderMeshInfo);

        s_ShaderRunningNow = null; /*s_isRunningShader = false; */ s_RunningShdaderMat = null; s_AddRenderer = null;

        if (!_isRemoveList) return;

        if (GetFindRememberList(_GetInfo, _GetBase, out System.Tuple<NewShaderInfo, ShaderOutPutBase> _GetOutTuple))
        {
            s_RememberShaderBaseList.Remove(_GetOutTuple);
            Object.Destroy(_GetOutTuple.Item2.gameObject);
            return;
        }
        Object.Destroy(_GetBase.gameObject);
    }

    #endregion

    #region Run Or Action Sub System Private Functions

    private ShaderOutPutBase RuningByShaderInfos(bool _isAddSubRenderer, GameObject _RuningObj, NewShaderInfo _GetInfo, 
    ShaderOutPutBase _GetBase, System.Action _EndCallBack = null)
    {
        //타입 EventEffect일경우는 다른 방식으로 리팩토링 수정 요망....
        if (_GetInfo.s_SPType == ShaderPlayerType.EventEffect)
        {
            if (_GetInfo.s_RememberShaderBase) RemoveWhenShaderOff(true, _GetInfo, _GetBase);
            _RuningObj.GetComponent<ParticleSystem>().Play();
            return null;
        }
        //스크립트로 제어하는 쉐이더일 경우
        s_RunningShdaderMat = _GetBase.pp_GetMainOutPutMaterial;
        Renderer ChangedRenderer = _RuningObj.GetComponent<Renderer>();
        RendererMeshMaterialInfo? GetRendererInfos = _isAddSubRenderer ? ParsingInitRendererInfo(ChangedRenderer) : spp_RenderMeshInfo;
        SetChangedTexToApplyMat(ChangedRenderer, GetRendererInfos, s_RunningShdaderMat);
        ListenInitSettingCallBack?.Invoke(_GetInfo.s_ItemIDX, _GetBase);
        _GetBase.Initialization(this, _GetInfo, GetRendererInfos.Value.s_GetRender, _EndCallBack);
        return _GetBase;
    }

    public void ApplyListenNextSettingInit(int _GetInfoValue, params  object[] _InitSettingOutPutBase)
    {
        ListenInitSettingCallBack = (_intValue, _GetBase) =>
        {
            if (_intValue != _GetInfoValue)
                return;

            _GetBase.ListenInitSetting(_InitSettingOutPutBase);
            ListenInitSettingCallBack = null;
        };
    }

    #endregion

    #region Sub System Private Functions

    protected virtual bool GetFindRememberList(
    NewShaderInfo _GetInfo, ShaderOutPutBase _GetBase, out System.Tuple<NewShaderInfo, ShaderOutPutBase> _GetOutTuple)
    {
        _GetOutTuple = null;
        if (!_GetInfo.s_RememberShaderBase) return false;
        int FIDX = -1; FIDX =
        s_RememberShaderBaseList.FindIndex(x => x.Item1.s_ItemIDX == _GetInfo.s_ItemIDX && x.Item2.Equals(_GetBase));
        _GetOutTuple = FIDX != -1? s_RememberShaderBaseList[FIDX] : null; return FIDX != -1;
    }

    private RendererMeshMaterialInfo? ParsingInitRendererInfo(Renderer _GetRenderer)
    {
        if ((_GetRenderer == null).HDebug("쉐이더를 관리할 Renderer가 해당 오브젝트에 존재하지 않습니다.!",
        Helper.HDType.Warning)) return null;
        List<MaterialInfo> MaterialInfos = new List<MaterialInfo>();
        _GetRenderer.sharedMaterials.ToList().ForEach(x =>
        {
            Dictionary<TextrueType, Texture> GetMaterialTexs = GetMaterialTexInfo(x);
            MaterialInfos.Add(new MaterialInfo(
              x,
              GetMaterialTexs[TextrueType.BaseMap],
              GetMaterialTexs[TextrueType.NormalMap],
              GetMaterialTexs[TextrueType.MetallicMap]
            ));
        });

        return new RendererMeshMaterialInfo()
        {
            s_GetRender = _GetRenderer,
            s_MaterialsArray = MaterialInfos.ToArray()
        };
    }

    protected virtual string GetStrByAboutTex(TextrueType _GetType, Material _GetMat)
    {
        string CompareStr = string.Empty;
        bool isDefultMat = _GetMat.shader.name == "Universal Render Pipeline/Lit";
        switch (_GetType)
        {
            case TextrueType.BaseMap:
                CompareStr = isDefultMat ? "_BaseMap" : "_MainTex"; break;
            case TextrueType.NormalMap:
                CompareStr = isDefultMat ? "_BumpMap" : "_NormalMap"; break;
            case TextrueType.MetallicMap:
                CompareStr = isDefultMat ? "_MetallicGlossMap" : "_MetallicMap"; break;
        }
        return CompareStr;
    }

    protected virtual void SetSubMatTexBySName(TextrueType _GetType, Material _GetMat, Texture _ApplyTex)
    {
        string CompareStr = GetStrByAboutTex(_GetType, _GetMat);
        if (!_GetMat.HasProperty(CompareStr)) return;
        _GetMat.SetTexture(CompareStr, _ApplyTex);
    }

    protected virtual void SetChangedTexToApplyMat(Renderer _ApplyedRender, 
    RendererMeshMaterialInfo? _GetOGInfos, Material _ChangeMat = null)
    {
        int CountIDX = 0;
        List<Material> ApplyMatList = new List<Material>();
        _ApplyedRender.materials.ToList().ForEach(x => { ApplyMatList.Add(
        _ChangeMat == null ? _GetOGInfos.Value.s_MaterialsArray[CountIDX].spp_GetMat : Object.Instantiate(_ChangeMat)); CountIDX++; });
        _ApplyedRender.materials = ApplyMatList.ToArray();
        CountIDX = 0;
        if (_ChangeMat != null) _ApplyedRender.materials.ToList().ForEach(x => 
        {
            MaterialInfo ApplySetByIDX = _GetOGInfos.Value.s_MaterialsArray[CountIDX];
            SetSubMatTexBySName(TextrueType.BaseMap, x, ApplySetByIDX.spp_BaseMap);
            SetSubMatTexBySName(TextrueType.NormalMap, x, ApplySetByIDX.spp_NomalMap);
            SetSubMatTexBySName(TextrueType.MetallicMap, x, ApplySetByIDX.spp_MatallicMap);
            CountIDX++;
        });
    }


    protected virtual Texture GetSubMatTexBySName(TextrueType _GetType, Material _GetMat)
    {
        Texture ReturnValue = null;
        string CompareStr = GetStrByAboutTex(_GetType, _GetMat);
        if (_GetMat.HasProperty(CompareStr)) 
            ReturnValue = _GetMat.GetTexture(CompareStr);
        return ReturnValue;
    }

    protected virtual Dictionary<TextrueType, Texture> GetMaterialTexInfo(Material _GetMat)
    {
        Dictionary<TextrueType, Texture> ReturnValue =
        new Dictionary<TextrueType, Texture>();
        Helper.GetEnumByStringArray<TextrueType>(true).ToList().ForEach(x =>
        {
            TextrueType GetType = x.StringToEnum<TextrueType>();
            Texture GetApplyValue = GetSubMatTexBySName(GetType, _GetMat);
            ReturnValue.Add(GetType, GetApplyValue);
        });
        return ReturnValue;
    }
    #endregion
}
#endregion


