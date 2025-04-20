using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class PhycisOutPutBase : MonoBehaviour
{
    public enum MoveMode
    {
        Directional,
        Strafe
    }

    [Header("MainAniControllHeader")]
    public bool m_isOutPutDebugRay = false;
    public bool m_isOutPutDebugLog = false;
    //public BCharacterController m_CharacterController;
    public NewPlayerCharacterController m_CharacterController;
    public NewCharacterController m_NewCharacterController;
    protected Animator m_RuntimeAnimNow;
    public AnimValuesInfo m_AnimValuesInfo;
    public InputValueInfo m_InputInfo;
    public List<PhycisHelper> CalculationPhycisList = new List<PhycisHelper>();
    

    [Header("Init(초기 상태) Reference")]
    [SerializeField] public CapsuleCollider CapsuleColl;
    [HideInInspector] public float originalHeight = 0f;
    [HideInInspector] public Vector3 originalCenter = Vector3.zero;

    [Header("PhycisTime")]
    public float FixedTime = 0f;
    public Vector3 m_FixedUpdatePosition = Vector3.zero;
    public Quaternion m_FixedUpdateRotation = Quaternion.identity;

    [Header("Direction Reference")]
    public MoveMode m_MoveMode = MoveMode.Directional;
    public Vector3 moveDirection = Vector3.zero; //
    public Vector3 moveDirectionVelocity = Vector3.zero;
    public Vector3 platformVelocity = Vector3.zero;
    [Range(0f, 5f)] public float SmoothAccelerationTime = 0.2f;//캐릭터 의 법선과 카메라의 전방백터에 대한 보간 변수 
    [Range(0f, 10f)] public float LinearAccelerationSpeed = 3f; //캐릭터 의 법선과 카메라의 전방백터에 대한 선형 가속 변수

    [Header("Rotate Reference")]
    public bool m_LookInCameraDirection = false; // 캐릭터와 카메라가 같은 전방백터(방향)을 유지할지의 여부
    [HideInInspector] public Vector3 lastForward = Vector3.zero;
    [HideInInspector] public float deltaAngle = 0f;

    [Header("Ground Reference")]
    public bool isGround = false;
    public float SpherecastRadius = 0.1f;
    public float AirborneThreshold = 0.6f; //공중으로 떠있을때의 지면에서의 높이
    public float platformFriction = 7f;
    public float groundStickyEffect = 4f;
    public float VelocityToGroundTangentWeight = 0f;
    public float MaxVerticalVelocityOnGround = 3f;
    public LayerMask GroundLayers;
    [HideInInspector] public Vector3 Gravity = Vector3.zero;
    [HideInInspector] public Vector3 WhenGroundUpNormal = Vector3.zero;
    [HideInInspector] public Vector3 platformAngularVelocity = Vector3.zero;
    [HideInInspector] public RaycastHit hit;

    [Header("Jump Reference")]
    public bool isSmoothJump = false;
    public float airSpeed = 6f; // 공중에 떠 있는 동안 캐릭터의 최대 속도를 결정함
    public float airControl = 3f; // 공중에 떠 있는 동안 캐릭터의 속도를 제어함
    public float jumpPower = 5f; // 공중에 뜰 힘을 나타냄
    [HideInInspector] public float jumpLeg, jumpEndTime, forwardMlp, groundDistance, lastAirTime, stickyForce;
    public float VelocityY = 0f;
    [HideInInspector] public Vector3 VerticalVelocity = Vector3.zero;

    protected bool m_isActionPhycisBase = false;

    protected virtual void SubInitlization(GameObject _getController)
    {
        if (_getController.GetComponent<Collider>() != null)
        {
            CapsuleColl = (_getController.GetComponent<Collider>() as CapsuleCollider);
            originalHeight = CapsuleColl.height;
            originalCenter = CapsuleColl.center;
        }
    }

    public void BreakPoint(bool _isBreak)
    {
        m_isActionPhycisBase = _isBreak;
        if (!_isBreak) m_NewCharacterController.GetComponent<Rigidbody>().linearVelocity = Vector3.zero;
    }

    #region System Function
    public virtual void Initlization(NewPlayerCharacterController _getMainController) { /*DONOTHING...*/ } //삭제후 아래 지점으로 로직 변경할것
    public abstract void Initlization(NewCharacterController _getMainController);
    public abstract void ActionPhycis();
    protected virtual void ProcessOnDestroy() { } //DONotiong
    protected virtual void ProcessAnimationMove() { } //DONothing
    protected virtual void ProcessUpdate() { } //DONothing
    protected virtual void ProcessLateUpdate() { } //DONothing
    protected virtual void ProcessFixedUpdate() { } //DONothing
    protected virtual void ProcessCollisionEnterEvent(GameObject _GetObj) { } //DONothing

    //오직 테스트 용도*(코루틴 방식은 전부 삭제 요망)
    protected Coroutine GetCoroutine = null;
    public virtual void ProcessUseCoroutine(bool _isUse, IEnumerator _GetIter)
    {
        if (_isUse )
        {
            if (GetCoroutine != null && GetCoroutine.ToString() == _GetIter.ToString()) 
            StopCoroutine(GetCoroutine);
            GetCoroutine = null;
            GetCoroutine = StartCoroutine(_GetIter);
            return;
        }
        if (GetCoroutine != null && GetCoroutine.ToString() == _GetIter.ToString())
        {
            StopCoroutine(GetCoroutine);
            GetCoroutine = null;
            return;
        }
        StopCoroutine(_GetIter);
    }

    #endregion

    #region 유니티 이벤트 함수
    public void OnDestroy()
    {
        ProcessOnDestroy();
    }

    //CharacterController에서 작용한 콜리전에 대한 이벤트 처리 함수
    public void OnCollistionEnterEvent(GameObject _getObj)
    {
        if (!m_isActionPhycisBase)
            return;

        ProcessCollisionEnterEvent(_getObj);
    }

    //애니메이션이 달려있는곳에 놔야 이벤트가 작동한다
    //AnimationController초기화에 이전조치함
    public void OnAniActionMoveEvent()
    {
        if (!m_isActionPhycisBase)
            return;
        ProcessAnimationMove();
    }

    void Update()
    {
        if (!m_isActionPhycisBase)
            return;
        ProcessUpdate();
    }

    void LateUpdate()
    {
        if (!m_isActionPhycisBase)
            return;
        ProcessLateUpdate();
    }

    void FixedUpdate()
    {
        if (!m_isActionPhycisBase)
            return;
        ProcessFixedUpdate();
    }
    #endregion
}
