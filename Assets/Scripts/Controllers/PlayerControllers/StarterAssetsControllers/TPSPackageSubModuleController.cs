using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using Commons.Helpers;

public class TPSPackageSubModuleController : SubModuleControllerBase, I_SubModulesCollection
{
    [Header("New Custom Value")]
    [SerializeField] private bool m_isRunOGValue;
    [SerializeField, ReadOnly] private AnimationControllerMoveModule m_ACMoveModule;
    [field: SerializeField, ReadOnly] public List<SubModuleControllerBase> pp_AddSubControllers { get; private set; }
    public Animator pp_MainAnimator { get => _animator; }

    [Header("Player")]
    [Tooltip("Move speed of the character in m/s")]
    public float MoveSpeed = 2.0f;

    [Tooltip("Sprint speed of the character in m/s")]
    public float SprintSpeed = 5.335f;

    [Tooltip("How fast the character turns to face movement direction")]
    [Range(0.0f, 0.3f)]
    public float RotationSmoothTime = 0.12f;

    [Tooltip("Acceleration and deceleration")]
    public float SpeedChangeRate = 10.0f;

    public AudioClip LandingAudioClip;
    public AudioClip[] FootstepAudioClips;
    [Range(0, 1)] public float FootstepAudioVolume = 0.5f;

    [Space(10)]
    [Tooltip("The height the player can jump")]
    public float JumpHeight = 1.2f;

    [Tooltip("The character uses its own gravity value. The engine default is -9.81f")]
    public float Gravity = -15.0f;

    [Space(10)]
    [Tooltip("Time required to pass before being able to jump again. Set to 0f to instantly jump again")]
    public float JumpTimeout = 0.50f;

    [Tooltip("Time required to pass before entering the fall state. Useful for walking down stairs")]
    public float FallTimeout = 0.15f;

    [Header("Player Grounded")]
    [Tooltip("If the character is grounded or not. Not part of the CharacterController built in grounded check")]
    public bool Grounded = true;

    [Tooltip("Useful for rough ground")]
    public float GroundedOffset = -0.14f;

    [Tooltip("The radius of the grounded check. Should match the radius of the CharacterController")]
    public float GroundedRadius = 0.28f;

    [Tooltip("What layers the character uses as ground")]
    public LayerMask GroundLayers;

    [Header("Cinemachine")]
    [Tooltip("The follow target set in the Cinemachine Virtual Camera that the camera will follow")]
    public GameObject CinemachineCameraTarget;

    [Tooltip("How far in degrees can you move the camera up")]
    public float TopClamp = 70.0f;

    [Tooltip("How far in degrees can you move the camera down")]
    public float BottomClamp = -30.0f;

    [Tooltip("Additional degress to override the camera. Useful for fine tuning camera position when locked")]
    public float CameraAngleOverride = 0.0f;

    [Tooltip("For locking the camera position on all axis")]
    public bool LockCameraPosition = false;

    // cinemachine
    private float _cinemachineTargetYaw;
    private float _cinemachineTargetPitch;

    // player
    private float _speed;
    private float _animationBlend;
    private float _targetRotation = 0.0f;
    private float _rotationVelocity;
    private float _verticalVelocity;
    private float _terminalVelocity = 53.0f;

    // timeout deltatime
    private float _jumpTimeoutDelta;
    private float _fallTimeoutDelta;

    // animation IDs
    private int _animIDSpeed;
    private int _animIDGrounded;
    private int _animIDJump;
    private int _animIDFreeFall;
    private int _animIDMotionSpeed;

    //#if ENABLE_INPUT_SYSTEM
    //    private PlayerInput _playerInput;
    //#endif
    //private InputValueInfo _playerInput;

    private Animator _animator;
    private CharacterController _controller;
    //private StarterAssetsInputs _input;
    private InputValueInfo _input;
    private GameObject _mainCamera;

    private const float _threshold = 0.01f;

    private bool _hasAnimator;

    private bool IsCurrentDeviceMouse
    {
        get
        {
            return false;
//#if ENABLE_INPUT_SYSTEM
//            return _playerInput.currentControlScheme == "KeyboardMouse";
//#else
//				return false;
//#endif
        }
    }

    #region OG 유니티 이벤트 함수

    private void Awake()
    {
        if (!m_isRunOGValue) return;
        // get a reference to our main camera
        if (_mainCamera == null)
        {
            _mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
        }
    }

    private void Start()
    {
        if (!m_isRunOGValue) return;

        _cinemachineTargetYaw = CinemachineCameraTarget.transform.rotation.eulerAngles.y;

        _hasAnimator = TryGetComponent(out _animator);
        _controller = GetComponent<CharacterController>();
        //_input = GetComponent<StarterAssetsInputs>();
//#if ENABLE_INPUT_SYSTEM
//        _playerInput = GetComponent<PlayerInput>();
//#else
//			Debug.LogError( "Starter Assets package is missing dependencies. Please use Tools/Starter Assets/Reinstall Dependencies to fix it");
//#endif

        AssignAnimationIDs();

        // reset our timeouts on start
        _jumpTimeoutDelta = JumpTimeout;
        _fallTimeoutDelta = FallTimeout;
    }

    

    #endregion

    #region ReApply 유니티 이벤트 함수

    protected override void Update()
    {
        if (!m_isActionProcess)
        {
            Move();
            return;
        } 

        //_hasAnimator = TryGetComponent(out _animator);

        JumpAndGravity();
        GroundedCheck();
        Move();
    }

    private void LateUpdate()
    {
        if (!m_isActionProcess) return;

        CameraRotation(); //CameraController<클래스>로 로직 이전 요망
    }

    #endregion

    #region ReApply Used Reference

    public override object[] Initlization(params object[] _ParsingParams)
    {
        m_isActionProcess = false; //전부 상속에 의해 변경할 수 있도록 수정할것

        var ApplyParams = base.Initlization(_ParsingParams);
        NewActionInfo GetActionInfo = (NewActionInfo)ApplyParams[0];
        TPSPackageInfo _getCalculInfo = ApplyControllerInfoData<TPSPackageInfo>(GetActionInfo.s_RequiredSubModuleParams);

        this.transform.SetParent(m_MainController.transform);
        this.transform.localPosition = Vector3.zero; this.transform.localRotation = Quaternion.identity;

        //CheckAllReset();
        m_ACMoveModule = m_MainController.gameObject.CheckComnectComponent<AnimationControllerMoveModule>();
        var GetPair = m_ACMoveModule.Initlization<CharacterController>(m_MainController as NewCharacterController);
        _animator = GetPair.Item1; _controller = GetPair.Item2 as CharacterController;
        _animator.runtimeAnimatorController = _getCalculInfo.spp_AniRuntimeController;
        //if (m_MainAnimator.avatar == null || !m_MainAnimator.avatar.Equals(_getCalculInfo.spp_ApplyAvatar))
        //    m_MainAnimator.avatar = _getCalculInfo.spp_ApplyAvatar;

        _input = FindParentsObjTypeByTemp<NewCharacterController>().I_GetSubModule<BInputController>().mspp_InputValues;

        #region Animation Controller Sub ModuleController Init (사용 안함 참고용)
        if (pp_AddSubControllers == null) pp_AddSubControllers = new();

        if (pp_AddSubControllers.Count > 0)
        {
            pp_AddSubControllers.ForEach(x =>
            {
                x.ExecuteChangeSubController();
                Destroy(x);
            });
            pp_AddSubControllers.Clear();
        }
        _getCalculInfo.spp_AddSMCWithAnimController.HForEach(x =>
        {
            var GetSubController = Instantiate(x, this.transform);
            GetSubController.Initlization(this); //Params는 없고 버퍼형식의 데이터 전달정도로 Model을 대처한다
            pp_AddSubControllers.Add(GetSubController);
        });
        #endregion

        #region Support Phycis Helper Init (사용 안함 참고용)
        //if (_getCalculInfo.spp_PhycisOutPutBase != null)
        //{
        //    m_ACMoveModule.InitRecycle(_getCalculInfo);
        //    m_AnimValuesInfo = new AnimValuesInfo();
        //    m_PhycisOutPutBase = Instantiate(_getCalculInfo.spp_PhycisOutPutBase, this.transform);
        //    m_InputInfo = GetController.I_GetSubModule<BInputController>().mspp_InputValues;
        //    m_PhycisOutPutBase.Initlization(GetController);
        //    m_PhycisOutPutBase.ActionPhycis();
        //    InitSettingByParermeterUpdate(true);
        //}
        #endregion

        #region Apply New Init (리팩 요망)

        CinemachineCameraTarget = Helper.ChildLinearStuctureSearch(m_MainController.transform).
        HFind(x => x.name == "CamTarget").gameObject;

        if (_mainCamera == null) _mainCamera = FindAnyObjectByType<Cinemachine.CinemachineBrain>().gameObject;// _mainCamera = Camera.main.gameObject;
        _cinemachineTargetYaw = CinemachineCameraTarget.transform.rotation.eulerAngles.y;
        _hasAnimator = _controller != null;
        AssignAnimationIDs();
        // reset our timeouts on start
        _jumpTimeoutDelta = JumpTimeout;
        _fallTimeoutDelta = FallTimeout;
        #endregion

        if (m_MainController is I_SubModulesCollection GetComplateSign) GetComplateSign.I_CheckSubModuleIsAllCanAction(this);
        else m_isActionProcess = true;

        return null;
    }

    public override void BreakPoint(bool _isBreak, SubModuleBreakType _BrackType = SubModuleBreakType.BreakAll, System.Type _GetType = null)
    {
        base.BreakPoint(_isBreak, _BrackType, _GetType);
        _verticalVelocity = 0f;
        //if(this.gameObject.activeSelf)_controller.Move(Vector3.zero);
        if (_animator != null) _animator.SetFloat(_animIDSpeed, 0f);
        m_MainController.GetComponent<Rigidbody>().linearVelocity = Vector3.zero;
    }

    #endregion

    #region Sub System Public Functions (**Interface Reference**) [보류]

    //후에 반드시 구현하여 체킹할것
    public void I_CheckSubModuleIsAllCanAction(ControllerBase _GetSubBase)
    {

    }
    public T I_GetSubModule<T>() where T : SubModuleControllerBase
    => ISCheckSubModule<T>(out T _OutPutBase) ? _OutPutBase : null;

    public bool ISCheckSubModule<T>(out T _OutPutBase) where T : SubModuleControllerBase
    {
        _OutPutBase = null; int FIDX = -1; FIDX =
        pp_AddSubControllers.HFindIndex(x => x.GetType().Name == typeof(T).Name);
        if (FIDX != -1)
        {
            _OutPutBase = pp_AddSubControllers[FIDX] as T;
            return true;
        }
        return false;
    }

    #endregion

    private void AssignAnimationIDs()
    {
        _animIDSpeed = Animator.StringToHash("Speed");
        _animIDGrounded = Animator.StringToHash("Grounded");
        _animIDJump = Animator.StringToHash("Jump");
        _animIDFreeFall = Animator.StringToHash("FreeFall");
        _animIDMotionSpeed = Animator.StringToHash("MotionSpeed");
    }

    private void GroundedCheck()
    {
        // set sphere position, with offset
        Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - GroundedOffset,
            transform.position.z);
        Grounded = Physics.CheckSphere(spherePosition, GroundedRadius, GroundLayers,
            QueryTriggerInteraction.Ignore);

        // update animator if using character
        if (_hasAnimator)
        {
            _animator.SetBool(_animIDGrounded, Grounded);
        }
    }

    private void CameraRotation()
    {
        // if there is an input and camera position is not fixed
        if (_input.s_LookPos.sqrMagnitude >= _threshold && !LockCameraPosition)
        {
            //Don't multiply mouse input by Time.deltaTime;
            float deltaTimeMultiplier = IsCurrentDeviceMouse ? 1.0f : Time.deltaTime;

            _cinemachineTargetYaw += _input.s_LookPos.x * deltaTimeMultiplier;
            _cinemachineTargetPitch += _input.s_LookPos.y * deltaTimeMultiplier;
        }

        // clamp our rotations so our values are limited 360 degrees
        _cinemachineTargetYaw = ClampAngle(_cinemachineTargetYaw, float.MinValue, float.MaxValue);
        _cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, BottomClamp, TopClamp);

        // Cinemachine will follow this target
        CinemachineCameraTarget.transform.rotation = Quaternion.Euler(_cinemachineTargetPitch + CameraAngleOverride,
            _cinemachineTargetYaw, 0.0f);
    }

    private void Move()
    {
        // set target speed based on move speed, sprint speed and if sprint is pressed
        if (!m_isActionProcess)
        {
            m_MainController.GetComponent<Rigidbody>().linearVelocity = Vector3.zero;
            _verticalVelocity = 0f;
            _controller.Move(Vector3.zero);
            _animator.SetFloat(_animIDSpeed, 0f);
            return;
        }


        float targetSpeed = _input.s_isRunLeftShift ? SprintSpeed : MoveSpeed;

        // a simplistic acceleration and deceleration designed to be easy to remove, replace, or iterate upon

        // note: Vector2's == operator uses approximation so is not floating point error prone, and is cheaper than magnitude
        // if there is no input, set the target speed to 0
        if (_input.s_MoveValue == Vector2.zero) targetSpeed = 0.0f;

        // a reference to the players current horizontal velocity
        float currentHorizontalSpeed = new Vector3(_controller.velocity.x, 0.0f, _controller.velocity.z).magnitude;

        float speedOffset = 0.1f;
        float inputMagnitude = _input.s_isAnalogMovement ? _input.s_MoveValue.magnitude : 1f;

        // accelerate or decelerate to target speed
        if (currentHorizontalSpeed < targetSpeed - speedOffset ||
            currentHorizontalSpeed > targetSpeed + speedOffset)
        {
            _speed = Mathf.Lerp(currentHorizontalSpeed, 
            targetSpeed * inputMagnitude, Time.deltaTime * SpeedChangeRate);

            // round speed to 3 decimal places
            _speed = Mathf.Round(_speed * 1000f) / 1000f;
        }
        else
        {
            _speed = targetSpeed;
        }

        _animationBlend = Mathf.Lerp(_animationBlend, targetSpeed, Time.deltaTime * SpeedChangeRate);
        if (_animationBlend < 0.01f) _animationBlend = 0f;

        Vector3 inputDirection = new Vector3(_input.s_MoveValue.x, 0.0f, _input.s_MoveValue.y).normalized;
        
        if (_input.s_MoveValue.magnitude > 0f)
        {
            _targetRotation = 
            Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg + _mainCamera.transform.eulerAngles.y;
            float rotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, _targetRotation, ref _rotationVelocity, RotationSmoothTime);
            m_MainController.transform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);
        }


        Vector3 targetDirection = Quaternion.Euler(0.0f, _targetRotation, 0.0f) * Vector3.forward;

        _controller.Move(targetDirection.normalized * (_speed * Time.deltaTime) +
                         new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime);
        if (_hasAnimator)
        {
            _animator.SetFloat(_animIDSpeed, _animationBlend);
            _animator.SetFloat(_animIDMotionSpeed, inputMagnitude);
        }
    }

    private void JumpAndGravity()
    {
        if (Grounded)
        {
            // reset the fall timeout timer
            _fallTimeoutDelta = FallTimeout;

            // update animator if using character
            if (_hasAnimator)
            {
                _animator.SetBool(_animIDJump, false);
                _animator.SetBool(_animIDFreeFall, false);
            }

            // stop our velocity dropping infinitely when grounded
            if (_verticalVelocity < 0.0f)
            {
                _verticalVelocity = -2f;
            }

            // Jump
            if (_input.s_isJump && _jumpTimeoutDelta <= 0.0f)
            {
                // the square root of H * -2 * G = how much velocity needed to reach desired height
                _verticalVelocity = Mathf.Sqrt(JumpHeight * -2f * Gravity);

                // update animator if using character
                if (_hasAnimator)
                {
                    _animator.SetBool(_animIDJump, true);
                }
            }

            // jump timeout
            if (_jumpTimeoutDelta >= 0.0f)
            {
                _jumpTimeoutDelta -= Time.deltaTime;
            }
        }
        else
        {
            // reset the jump timeout timer
            _jumpTimeoutDelta = JumpTimeout;

            // fall timeout
            if (_fallTimeoutDelta >= 0.0f)
            {
                _fallTimeoutDelta -= Time.deltaTime;
            }
            else
            {
                // update animator if using character
                if (_hasAnimator)
                {
                    _animator.SetBool(_animIDFreeFall, true);
                }
            }

            // if we are not grounded, do not jump
            _input.s_isJump = false;
        }

        // apply gravity over time if under terminal (multiply by delta time twice to linearly speed up over time)
        if (_verticalVelocity < _terminalVelocity)
        {
            _verticalVelocity += Gravity * Time.deltaTime;
        }
    }

    private static float ClampAngle(float lfAngle, float lfMin, float lfMax)
    {
        if (lfAngle < -360f) lfAngle += 360f;
        if (lfAngle > 360f) lfAngle -= 360f;
        return Mathf.Clamp(lfAngle, lfMin, lfMax);
    }

    private void OnDrawGizmosSelected()
    {
        Color transparentGreen = new Color(0.0f, 1.0f, 0.0f, 0.35f);
        Color transparentRed = new Color(1.0f, 0.0f, 0.0f, 0.35f);

        if (Grounded) Gizmos.color = transparentGreen;
        else Gizmos.color = transparentRed;

        // when selected, draw a gizmo in the position of, and matching radius of, the grounded collider
        Gizmos.DrawSphere(
            new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z),
            GroundedRadius);
    }

    private void OnFootstep(AnimationEvent animationEvent)
    {
        if (animationEvent.animatorClipInfo.weight > 0.5f)
        {
            if (FootstepAudioClips.Length > 0)
            {
                var index = Random.Range(0, FootstepAudioClips.Length);
                AudioSource.PlayClipAtPoint(FootstepAudioClips[index], transform.TransformPoint(_controller.center), FootstepAudioVolume);
            }
        }
    }

    private void OnLand(AnimationEvent animationEvent)
    {
        if (animationEvent.animatorClipInfo.weight > 0.5f)
        {
            AudioSource.PlayClipAtPoint(LandingAudioClip, transform.TransformPoint(_controller.center), FootstepAudioVolume);
        }
    }
}


[System.Serializable]
public struct TPSPackageInfo : ControllerBase.I_PurifiedParamsData, ControllerBase.I_PurifiedAddKey
{
    [field: SerializeField] public RuntimeAnimatorController spp_AniRuntimeController { get; private set; }
    //만일 AnimationController를 통해서 추가적인 SubController이 필요할때 아래 Controller를 정의하여 사용할 수 있도록 한다
    [field: SerializeField] public SubModuleControllerBase[] spp_AddSMCWithAnimController { get; private set; } 
    [field: SerializeField, PropertyOrder(int.MinValue)] public int spp_SubItemIDX { get; private set; }

    public string I_SubInfoIDX() => spp_SubItemIDX.ToString();
    public bool I_ISGetOutParams(ControllerBase _GetPPData) => _GetPPData is AnimationController;
}