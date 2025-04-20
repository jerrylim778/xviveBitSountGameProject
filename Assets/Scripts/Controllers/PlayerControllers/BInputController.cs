using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using Commons;
using Commons.Helpers;
using UnityEngine.AI;
using Unity.AI.Navigation;

public class BInputController : SubModuleControllerBase
{
    [Header("MainHeader")]
    [SerializeField] private PowerType m_PowerType;
    [SerializeField] private InputCalculateBase m_CalculateInputNow;
    
    public InputValueInfo mspp_InputValues { get { return m_CalculateInputNow == null ? null : m_CalculateInputNow.s_InputValues; } }

    [Tooltip("System Value Refernce")]
    [HideInInspector] public bool m_isActionInputPower = false;
    public PowerType pp_PowerType { get => m_PowerType; }

    #region Old Verstion (**향후 삭제할 수 있도록 리팩 요망**)

    public void Initialization(PowerType _getType)
    {
        m_isActionInputPower = false;
        m_PowerType = _getType;
        if (m_PowerType == PowerType.NotNeedAnyPower) { Destroy(this); return; }

        switch (_getType)
        {
            case PowerType.SelfInputPower or PowerType.SelfJoyStickPower:
                m_CalculateInputNow = new InputSelf(this.gameObject);
                break;
            case PowerType.TargetPower or PowerType.MousePointTargetPower or PowerType.AITargetPower:
                //m_CalculateInputNow = new InputTarget(this.gameObject);
                break;
            default:
                Debug.LogError("현재 정의되어있지 않은 힘의 타입입니다..!!");
                return;
        }
        m_isActionInputPower = true;
    }
    #endregion

    public override object[] Initlization(params object[] _ParsingParams)
    {
        m_isActionInputPower = false;
        var ApplyParams = base.Initlization(_ParsingParams);
        NewActionInfo GetActionInfo = (NewActionInfo)ApplyParams[0];
        InputDataInfo GetInputData = ApplyControllerInfoData<InputDataInfo>(GetActionInfo.s_RequiredSubModuleParams);
        this.transform.SetParent(m_MainController.transform);
        this.transform.position = Vector3.zero; this.transform.rotation = Quaternion.identity;

        m_PowerType = GetInputData.s_PowerType;
        if (m_PowerType == PowerType.NotNeedAnyPower) { Destroy(this); return null; }

        m_CalculateInputNow = m_PowerType switch
        {
            PowerType.SelfInputPower or PowerType.SelfJoyStickPower => 
            new InputSelf(this.gameObject),
            PowerType.TargetPower or PowerType.AITargetPower or PowerType.MousePointTargetPower => 
            new InputTarget(this.gameObject, GetInputData.s_InputTargetInfo),
            PowerType.MouseAndCamBaoundaryPower => 
            new InputMouseAndCamBoundary(this.gameObject, (MouseAndCamPowerInfo)GetInputData.s_SubApplyParams),
            _ => null
        };
        if ((m_CalculateInputNow == null).HDebug("현재 정의되어있지 않은 힘의 타입입니다..!!", Helper.HDType.Error)) return null;
        //m_isActionProcess로 전부 변경해야 한다 <**리팩 요망 ************************************************************>
        if (m_MainController is I_SubModulesCollection GetComplateSign) 
            GetComplateSign.I_CheckSubModuleIsAllCanAction(this);
        else 
        {
            m_isActionProcess = true; pp_isSelfProcessUpdate = true;
            m_isActionInputPower = true;  //삭제 요망 **
        }
        //m_isActionProcess로 전부 변경해야 한다 <**리팩 요망 ************************************************************>
        return null;
    }

    #region Sub System Functions (**상속에 의해 필요없는것들은 삭제 및 수정 요망**)
    public void OrderEventPower<T>(T _Items)
    {
        //한번 정지후 변경 해야한다
        m_CalculateInputNow.s_InputValues.s_MoveValue = Vector2.zero;
        m_CalculateInputNow.s_InputValues.s_BreakPoint = false;
        if (_Items is bool)
        {
            m_CalculateInputNow.s_InputValues.s_BreakPoint = bool.Parse(_Items.ToString());
            m_CalculateInputNow.s_InputValues.s_MoveValue = Vector2.zero;
        }
        if (_Items is GetActionOutOrder?)
        {
            GetActionOutOrder? GetOrder = _Items as GetActionOutOrder?;
            m_CalculateInputNow.s_InputValues.s_LookPos = GetOrder.Value.ApplyLookPosValue;
            m_CalculateInputNow.s_InputValues.s_BreakPoint = GetOrder.Value.LayerIDX != 0 && GetOrder.Value.ApplyIDX != 0;
            //m_CalculateInputNow.s_InputValues.s_isLayer2EventIDX = GetOrder.Value.ApplyIDX;
        }
    }

    public bool FindInputCalCulBase<T>(out T _GetBaseValues) where T : InputCalculateBase
    {
        if (m_CalculateInputNow is T) 
        {
            _GetBaseValues = (m_CalculateInputNow as T);
            return true; 
        }
        _GetBaseValues = null;
        return false;
    }

    //상태 변형때 전 InputController<클래스>에서 처리해야될 예외사항이 존재할경우 호출
    public void ExceptionBeforeController()
    {
        m_isActionInputPower = false;

        if (m_CalculateInputNow != null) m_CalculateInputNow.ExceptionBeforeInput();
    }

    #endregion

    #region Sub System Public Functions (**New Main Process**)

    public override void BreakPoint(bool _isBreak, SubModuleBreakType _BrackType = SubModuleBreakType.BreakAll, System.Type _GetType = null)
    {
        base.BreakPoint(_isBreak, _BrackType, _GetType);
        if (FindParentsObjTypeByTemp<NewCharacterController>().pp_isMine && 
        m_CalculateInputNow != null) m_CalculateInputNow.ExceptionBeforeInput();
        m_isActionInputPower = true;
    }

    public override void ExecuteChangeSubController()
    {
        base.ExecuteChangeSubController();

        if (m_CalculateInputNow != null) 
            m_CalculateInputNow.ExceptionBeforeInput();
    }

    #endregion

    #region 이벤트 함수

    //protected override void Update() //ProcessUpdate로 변경 상위객체에서 컨트롤할 수 있도록 수정한다
    public override void ProcessUpdate()
    {
        if (!m_isActionInputPower) //상속에 의한 힘으로 변경할것
            return;

        m_CalculateInputNow.CalculateInput();
    }
    #endregion
}

#region Calculate Base
public abstract class InputCalculateBase
{
    protected BInputController s_InputController;
    public InputValueInfo s_InputValues;

    public InputCalculateBase(GameObject _getController)
    {
        s_InputController = _getController.GetComponent<BInputController>();
        s_InputValues = new InputValueInfo();
    }

    public virtual void ExceptionBeforeInput() { } //DONOTIONG.....

    public abstract void CalculateInput();
}


#region Self Power
public class InputSelf : InputCalculateBase
{
    private Camera s_GetControllCam;
    private bool s_isJoyStickTOC = false;
    private bool s_isSwichTOC = false;
    private ForcePopUpJoyStickSubController s_JoyController;

    public InputSelf(GameObject _getControllerObj) : base(_getControllerObj)
    {
        s_GetControllCam = Object.FindAnyObjectByType<CameraController>() != null ?
        Object.FindAnyObjectByType<CameraController>().GetComponent<Camera>() : Camera.main;
        s_isJoyStickTOC = s_InputController.pp_PowerType == PowerType.SelfJoyStickPower;
        if (s_isJoyStickTOC)
        {
            ForceSubOrderPopUp GetFSPopUp = GamePlaySystem.Instance.ExecutePopUpStack<ForceSubOrderPopUp>(
            new SubOrderAddPopUpInfo(typeof(CharacterICNRequiredPopUp), "JoyStick_SubOrderPopUp"));
            s_JoyController = GetFSPopUp.pp_SupportSubControllerBase as ForcePopUpJoyStickSubController;
            s_JoyController.BreakPoint(true);
        }
    }

    public override void ExceptionBeforeInput()
    {
        base.ExceptionBeforeInput();
        s_isSwichTOC = !s_isSwichTOC;
        if (s_JoyController != null) s_JoyController.BreakPoint(s_isSwichTOC);
    }

    public override void CalculateInput()
    {
        if (s_InputValues.s_BreakPoint) return;

        float GetH = Input.GetAxisRaw("Horizontal");
        float GetV = Input.GetAxisRaw("Vertical");
        if (s_JoyController != null && s_JoyController.pp_GetMainPower.magnitude != 0)
        {
            GetH = s_JoyController.pp_GetMainPower.x; 
            GetV = s_JoyController.pp_GetMainPower.y;
        }

        Vector3 ConvertVec3 = new Vector3(GetH, 0f, GetV);
        //Debug.Log(ConvertVec3.normalized);
        Vector3 Move = ConvertVec3;//s_GetControllCam.transform.rotation * ConvertVec3;
        s_InputValues.s_isRunLeftShift = Input.GetKey(KeyCode.LeftShift);
        Vector3 CompareMove = Vector3.zero;
        if (Move != Vector3.zero)
        {
            float OGmagnitude = Move.magnitude;
            Vector3 Normal = s_InputController.transform.up;
            Vector3.OrthoNormalize(ref Normal, ref Move);
            //if(s_isJoyStickTOC) Move *= OGmagnitude; //보간 자연스러운 이동시 사용할것
            CompareMove = new Vector2(Move.x, Move.z);
        }
        else CompareMove = Vector2.zero;
        s_InputValues.s_MoveValue = CompareMove * 1.3f;
        if(s_JoyController == null || s_JoyController.pp_GetMainPower.magnitude == 0)
        /*if (!s_isJoyStickTOC) */s_InputValues.s_MoveValue *= s_InputValues.s_isRunLeftShift ? 1.5f : 1f;

        s_InputValues.s_LookCamForword = s_GetControllCam.transform.forward;
        s_InputValues.s_LookCamRight = s_GetControllCam.transform.right;
        s_InputValues.s_LookPos = s_InputController.pp_MainController.transform.position + s_GetControllCam.transform.forward * 100f;
    }
}
#endregion

#region Target Power (단순 목표 지점까지 이동하기 위한 힘)

public class InputTarget : InputCalculateBase
{
    [Tooltip("Type By Value")]
    private Rigidbody CharacterRB;
    private GameObject TargetObj;
    private Vector3 s_ReturnTarget, s_BeforeTarget;
    private System.Action s_TargetDirCallBack;
    [Tooltip("Target Power Support Values")]
    InputTargetInfo? s_InputTargetInfo;
    [Tooltip("Use Live Nav Mesh Reference")]
    private NavMeshSurface s_NavSelfSurface;
    private int s_CurrMovePathCount = 0, s_BeforeCount = 0;
    public GameObject pp_TargetObj { get => TargetObj; }

    #region Property
    private bool spp_isUseSupportTPower { get => s_InputTargetInfo != null && s_InputTargetInfo.HasValue; }
    private bool spp_isUsedNavMeshPath { get => spp_isUseSupportTPower && 
    (s_InputTargetInfo.Value.s_isUseNavPathFinder || s_InputTargetInfo.Value.s_isUseSelfLiveNavSurface); }
    private bool spp_isUsedSelfLiveNavSurface { get => spp_isUseSupportTPower && s_InputTargetInfo.Value.s_isUseSelfLiveNavSurface; }
    #endregion

    public InputTarget(GameObject _getControllerObj, InputTargetInfo _InputTargetInfo) : base(_getControllerObj)
    {
        s_InputTargetInfo = _InputTargetInfo;
        var MainCharacterObj = s_InputController.pp_MainController.gameObject;
        CharacterRB = MainCharacterObj.CheckComnectComponent<Rigidbody>();
        if (spp_isUsedSelfLiveNavSurface) UpdateNavBoundary(true, MainCharacterObj);
        switch (s_InputController.pp_PowerType)
        {
            case PowerType.TargetPower:
                TargetObj = new GameObject(_getControllerObj.name);
                TargetObj.transform.SetParent(Helper.FindOrCreateByTag("Pool").transform);
                TargetObj.transform.position = s_InputController.pp_MainController.transform.position;
                TargetObj.gameObject.name += "_" + s_InputController.pp_MainController.name;
                break;
        }
        s_TargetDirCallBack = () =>
        {
            if (spp_isUsedNavMeshPath)
            { s_CurrMovePathCount = 0; s_BeforeCount = 0; }
        };
    }

    //다른방식으로 없앨것
    public override void ExceptionBeforeInput()
    {
        base.ExceptionBeforeInput();
        if (s_InputController.pp_PowerType == PowerType.TargetPower)
            Object.Destroy(TargetObj);
        if (s_InputTargetInfo.Value.s_isUseSelfLiveNavSurface)
            Object.Destroy(s_NavSelfSurface.gameObject);
    }

    public override void CalculateInput()
    {
        if (s_InputValues.s_BreakPoint) return;
        if (spp_isUsedSelfLiveNavSurface) UpdateNavBoundary(false);

        CalcaulateTargetArrow();
        Vector3 TargetMove = s_ReturnTarget - CharacterRB.position;
        Vector3 ReTargetingByState = spp_isUsedNavMeshPath ? NavAgentPathMove(TargetMove) : TargetMove;  
        Vector3 Normal = CharacterRB.transform.up;
        float OGTargetPower = ReTargetingByState.magnitude;
        Vector3.OrthoNormalize(ref Normal, ref ReTargetingByState);
        s_InputValues.s_MoveValue = OGTargetPower > 1.2f ? new Vector2(ReTargetingByState.x, ReTargetingByState.z) : Vector2.zero;
    }

    #region Sub System Private Functions (**Support Targeting**)

    private void CalcaulateTargetArrow()
    {
        switch (s_InputController.pp_PowerType)
        {
            case PowerType.TargetPower:
                s_ReturnTarget = TargetObj.transform.position;
                break;
            case PowerType.MousePointTargetPower:
                if (Input.GetMouseButtonDown(1))
                {
                    RaycastHit[] GetRayHits = Physics.RaycastAll(Camera.main.ScreenPointToRay(Input.mousePosition));
                    bool isComplate = Helper.ISArrayFindCondition(GetRayHits, x => x.collider.gameObject.layer ==
                    LayerMask.NameToLayer("DefultLevelGround"), out RaycastHit _RayHit);
                    if (isComplate) s_ReturnTarget = _RayHit.point;
                }
                break;
        }

        if (Vector3.Distance(s_BeforeTarget, s_ReturnTarget) > 0.1f) 
        s_TargetDirCallBack?.Invoke();
        s_BeforeTarget = s_ReturnTarget;
    }

    #endregion

    #region Sub System Private Functions (**NavMesh Reference**)

    //이후에 이러한 거리 계산들을 InputDataInfo I_Param인 InputTargetInfo 새로히 navmesh버전을 만들어서
    //각 움직일 단계들의 계산을 해당 구역이 아닌 NavMesh구간으로 이전하여 해당 구간
    private Vector3 NavAgentPathMove(Vector3 directionToTarget)
    {
        NavMeshPath CurrNPath = new NavMeshPath();
        bool isCanSearch = NavMesh.CalculatePath(CharacterRB.position, s_ReturnTarget, NavMesh.AllAreas, CurrNPath)
        && CurrNPath.status == NavMeshPathStatus.PathComplete;

        //만일 전 탐색 배열과 다르다면 다시 0부터 시작할것 (해당 현상은 전에 탐색했던 스택을 Pop하기 때문에 갈수록 길이가 감소하게 되는것)
        if (s_BeforeCount > 0 && s_BeforeCount != CurrNPath.corners.Length) s_CurrMovePathCount = 0;
        s_BeforeCount = CurrNPath.corners.Length;

        if (isCanSearch && CurrNPath.corners.Length > 1 && s_CurrMovePathCount < CurrNPath.corners.Length)
        {
            Vector3 NextCorner = CurrNPath.corners[s_CurrMovePathCount]; // 현재 위치에서 다음 경로의 코너
            Vector3 MoveDirection = (NextCorner - CharacterRB.position).normalized * directionToTarget.magnitude;
            if (Vector3.Distance(CharacterRB.transform.position, CurrNPath.corners[s_CurrMovePathCount]) < 0.3f)
                s_CurrMovePathCount++;
            return MoveDirection; 
        }

        return Vector3.zero;
    }

    private void UpdateNavBoundary(bool _isInit, GameObject _MainObj = null)
    {
        if (_isInit)
        {
            s_NavSelfSurface = _MainObj.CreateApplyComponent<NavMeshSurface>(Helper.FindOrCreateByTag("Pool").transform);
            s_NavSelfSurface.collectObjects = CollectObjects.Volume;
            s_NavSelfSurface.size *= 5f;
            s_NavSelfSurface.BuildNavMesh();
            return;
        }
        if (!spp_isUsedSelfLiveNavSurface && s_NavSelfSurface == null) return;
        var GetCharacter = s_InputController.pp_MainController;
        if (Vector3.Distance(GetCharacter.transform.position, s_NavSelfSurface.transform.position) >= 3f)
        {
            s_NavSelfSurface.transform.position = GetCharacter.transform.position;
            s_NavSelfSurface.BuildNavMesh();
        }
    }

    #endregion
}
#endregion

#region MouseAndCamBaoundaryPower (마우스의 위치와 해당 위치가 화면상의 위치에 따라 정해지는 힘)
public class InputMouseAndCamBoundary : InputCalculateBase
{
    private Camera m_GetMainCam;
    private MouseAndCamPowerInfo s_GetMCInfo;

    public InputMouseAndCamBoundary(GameObject _getControllerObj, MouseAndCamPowerInfo _GetMousePowerInfo) : base(_getControllerObj)
    {
        m_GetMainCam = _GetMousePowerInfo.s_GetCamController.pp_ControllCam;
        s_GetMCInfo = _GetMousePowerInfo;
    }

    public override void CalculateInput()
    {
        if (s_InputValues.s_BreakPoint || m_GetMainCam == null) return;

        Vector3 GetMousePos = Input.mousePosition;
        //Vector3 GetCameraPos = m_GetMainCam.transform.position;

        Vector3 MoveDir = Vector3.zero;

        MoveDir.x = GetMousePos.x <= s_GetMCInfo.s_EdgeSize ? -1 : GetMousePos.x >= Screen.width - s_GetMCInfo.s_EdgeSize ? 1 : 0; //왼 / 오른 쪽으로
        MoveDir.z = GetMousePos.y <= s_GetMCInfo.s_EdgeSize ? -1 : GetMousePos.y >= Screen.height - s_GetMCInfo.s_EdgeSize ? 1 : 0;  //위 / 아래 쪽으로
        
        if (MoveDir.magnitude != 0)
        {
            //범위의 적용 개념을 확인했다면 적용할것
            //GetCameraPos = MoveDir.magnitude != 0 ? MoveDir.normalized * s_GetMCInfo.s_MoveSpeed * Time.deltaTime : Vector3.zero;

            //GetCameraPos.x = Mathf.Clamp(GetCameraPos.x, s_GetMCInfo.s_MinBounds.x, s_GetMCInfo.s_MaxBounds.x);
            //GetCameraPos.z = Mathf.Clamp(GetCameraPos.z, s_GetMCInfo.s_MinBounds.y, s_GetMCInfo.s_MaxBounds.y);
        }
        var ApplyVec3 = MoveDir.magnitude != 0 ? MoveDir.normalized * s_GetMCInfo.s_MoveSpeed * Time.deltaTime : Vector3.zero;
        s_InputValues.s_MoveValue = new Vector2(ApplyVec3.x, ApplyVec3.z);
    }
}
#endregion

#endregion

[System.Serializable]
public class InputValueInfo
{
    public Vector2 s_MoveValue;
    public Vector3 s_LookPos;
    public bool s_isRunLeftShift;
    public bool s_isCrouch;
    public bool s_isJump;
    public bool s_BreakPoint;

    //250210추가한것 (카메라의 데이터를 가져오기 위함)
    public Vector3 s_LookCamForword;
    public Vector3 s_LookCamRight;
    //3인칭 Starter에 필요한것
    public bool s_isAnalogMovement;

    #region 테스트 용도 (**사용후 삭제요망**)
    ////테스트 용도 리팩토링후에 삭제를 결정 (아래 인덱스로 변경해야함
    //public bool s_isLayer1EventAction;
    ////public int s_isLayer1EventIDX; //이벤트 인덱스 관련 (레이어 1 Add Event)
    //public int s_isLayer2EventIDX; // 이벤트 인덱스관련 (레이어 2 Static)
    #endregion
}


//Commons로 이전 조치 요망******
[System.Serializable]
public struct GetActionOutOrder
{
    //public PlayerState ChangeState;
    public int LayerIDX, ApplyIDX;
    public Vector3 ApplyLookPosValue;
}

//25/01/09 .. 추가 
[System.Serializable]
public struct MouseAndCamPowerInfo : ControllerBase.I_PurifiedParamsData
{
    public float s_EdgeSize;
    public float s_MoveSpeed;
    public Vector2 s_MinBounds, s_MaxBounds;
    [HideInInspector] public CameraController s_GetCamController;

    public bool I_ISGetOutParams(ControllerBase _GetPPData) => _GetPPData is BInputController;
}