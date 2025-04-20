using System.Collections.Generic;
using UnityEngine;
using Commons.Helpers;
using Sirenix.OdinInspector;
using DG.Tweening;
using System;

[DisallowMultipleComponent]
public class CameraController : CameraControllerBase
{
    #region Camera Controll Infos

    public enum ViewType
    {
		None = 0,
		FPS,
		TPS,
		POV //자유시점
    }
    public enum UpdateMode
	{
		Update,
		FixedUpdate,
		LateUpdate,
		FixedLateUpdate,
		NoUpdate
	}

	public enum MouseMoveType
	{
		AlwaysMouseRotType,
		Click_LeftClickType,
		Click_RightClickType,
		Click_MiddleClickType
	}

	public enum UseFOVType
	{
		None = 0, //FOV 사용하지 않음
		ApplyRatioType, //DataManager에 존재한 화면 비율에 따라 FOV를 설정 (고정됨)
		StaticFOVType, //고정된 값으로만 사용
		FOVZOomType //일반 카메라를 이동하는 Zoom방식에서 FOV를 통해 더욱 부드럽게 설정하기 위한 타입
	}

	//이거까지 만든건 좀 다소 무리가 있기 때문에
	//다른 방식으로 적용을 고려해야한다 (**리팩요망**)
	public enum StartMoveType
    {
		None = 0, //바로 시작한다 나머지는 대리자에 의해 참이될때만 실행된다
		StaticMoveStartType,
        MoveLogicType,
		MoveVirtualType //계산한 가상공간으로 이동하는것 (만일 캐릭터에 카메라가 들어온다면 들어온 매개변수에 의해서 계산되는 방식)
    }

    [System.Serializable]
	public struct CamControllerInitInfo : I_PurifiedParamsData, I_PurifiedAddKey
	{
		//이후에 계속 추가될 수 있으며 인덱스가 타입이외에 필요한경우가 발생한다면
		//이름을 지어서 (물로 유일값임) 이름에 따라 여러 스크립터블 오브젝트를 만들어 가지고 있다가 보낼 수 있도록 수정 요망
		public ViewType m_ViewType;
		public bool s_isCinamecine;
		[ShowIf(nameof(s_isCinamecine))] public CinemachineBodyType s_CinemachineBodyType;
		#region View Params
		public StartMoveType s_StartType;
		[ShowIf("@s_StartType == StartMoveType.StaticMoveStartType")] public Vector3 s_StartPos;
		#endregion
		[BoxGroup("Rotation Reference")] public bool s_isNotUseRot;
		#region Rotation Params
		[field: SerializeField, BoxGroup("Rotation Reference"), ShowIf(nameof(s_isNotUseRot))] public Vector3 spp_StaticRot { get; private set; }
		#endregion
		[BoxGroup("Follow Cam Reference"), ShowIf("@m_ViewType != ViewType.POV")] public bool s_isFollowSmooth;
		#region FollowSmooth Params
		[field: SerializeField, ShowIf("@m_ViewType != ViewType.POV && s_isFollowSmooth"), Range(0f, 10f), BoxGroup("Follow Cam Reference")] public float spp_FollowSped { get; private set; }
		#endregion
		[BoxGroup("Breath Effect Reference"), ShowIf("@m_ViewType == ViewType.FPS")] public bool s_isBreathingMotion;
		#region Breathing Params
		[field: SerializeField, Range(0f, 0.1f), ShowIf(nameof(s_isBreathingMotion))] public float spp_infinityBreahing { get; private set; }
		#endregion
		[BoxGroup("Use POV Reference"), ShowIf("@m_ViewType == ViewType.POV")] public bool s_UseCamInputController;
		#region View Out FOV & Orthographic Params
		//Ratio의 경우 DataManager<클래스>에서 데이터를 초기화때 가져와서 적용해야한다
		[BoxGroup("Use View Out Reference")] public bool s_isUsedOrthographic;
		[BoxGroup("Use FOV Reference"), 
		ShowIf(nameof(s_isUsedOrthographic))] public float s_OrthographicSizeValue;

		//타입이 필요없음 사실상 디바이스 비율에 맞게 원근감을 조절해야되기 때문에 타입없이 기본으로 설정해야됨
		[BoxGroup("Use FOV Reference"), 
		ShowIf("@!s_isUsedOrthographic ")] public UseFOVType s_UseFOVType;
		[BoxGroup("Use FOV Reference"), 
		ShowIf("@!s_isUsedOrthographic && s_UseFOVType == UseFOVType.StaticFOVType")] public float s_StaticFOVValue;
		[BoxGroup("Use FOV Reference"), 
		ShowIf("@!s_isUsedOrthographic && s_UseFOVType == UseFOVType.FOVZOomType")] public float s_FOVMinZoom, s_FOVMaxZoom;
        #endregion
        #region Common Other Params
        [field: SerializeField, ShowIf("@m_ViewType != ViewType.POV"), BoxGroup("Controll All Condition Type")] public bool spp_isApplyCustom { get; private set; }
		[field: SerializeField, ShowIf("@m_ViewType != ViewType.POV && spp_isApplyCustom"), BoxGroup("Controll All Condition Type")] public Vector3 spp_OffsetTarget { get; private set; }
		[field: SerializeField, ShowIf("@m_ViewType != ViewType.POV && spp_isApplyCustom"), BoxGroup("Controll All Condition Type")] public Vector3 spp_OffsetApplyTarget { get; private set; }
		[field: SerializeField, ShowIf("@m_ViewType != ViewType.POV && spp_isApplyCustom"), Range(0f, 100f), BoxGroup("Controll All Condition Type")] public float spp_MinZoom { get; private set; }
		[field: SerializeField, ShowIf("@m_ViewType != ViewType.POV && spp_isApplyCustom"), Range(0f, 100f), BoxGroup("Controll All Condition Type")] public float spp_MaxZoom { get; private set; }
        #endregion
        #region 외부에서 추가 생성은 각 변수의 지정마다 할 수 있도록 한다(생성자 사용 지양 요망)
        //public CamControllerInitInfo(bool _isNotUseRot, bool _isFollowSmooth, bool _isZoomInAndOut, ViewType _ViewType)
        //{
        //	m_ViewType = _ViewType;
        //	s_isNotUseRot = _isNotUseRot;
        //	s_isFollowSmooth = _isFollowSmooth;
        //	s_isZoomInAndOut = _isZoomInAndOut;
        //	s_isBreathingMotion = _ViewType == ViewType.FPS; //그 이외에 시점에 따라 조정해야되는것들을 해당 구간에서 모두 조정이후에 초기화를 실행할 수 있도록 수정설계한다
        //}
        #endregion
        public string I_SubInfoIDX() => m_ViewType.ToString();
		public bool I_ISGetOutParams(ControllerBase _GetPPData) => _GetPPData is CameraController;
	}

	#endregion

	[SerializeField] private ViewType m_ViewType;
	//[SerializeField, ShowIf("@m_ViewType != ViewType.POV")] private Transform m_Target;
	[SerializeField, ReadOnly, ShowIf("@m_ViewType != ViewType.POV")] private Transform m_ListenTarget;
    [SerializeField, ShowIf("@m_ViewType == ViewType.POV")] private Transform m_RotationSpace; //공회전일때만 가능함
    [SerializeField] private UpdateMode m_UpdateMode = UpdateMode.NoUpdate;
	[SerializeField] private bool lockCursor = true;

	
	[Header("Position Reference")]
	[SerializeField] private bool m_SmoothFollow;
	[SerializeField] private bool m_isBreathingMotion = false;
	[SerializeField] private Vector3 m_MainOffset = new Vector3(0, 1.5f, 0.5f);
	[SerializeField] private float followSpeed = 10f;
	[SerializeField, Range(0f, 0.1f)] private float m_InfinityApplySpped = 0.0f;
	[Header("POV Position Refeence")]
	[SerializeField, ReadOnly, ShowIf("@m_ViewType == ViewType.POV")] private Vector3 m_POVMovePower;
	[SerializeField, ReadOnly, ShowIf("@m_ViewType == ViewType.POV")] private bool m_isUseInputController;
	[SerializeField, ReadOnly, ShowIf("@m_ViewType == ViewType.POV && m_isUseInputController")] private BInputController m_ReciveInputCon; 

	[Header("Rotation Reference")]
    [SerializeField] private bool m_isNotUseRot = false;
    [SerializeField] private float m_RotationSensitivity = 3.5f; //마우스 감도 
	[SerializeField] private float yMinLimit = -20;
	[SerializeField] private float yMaxLimit = 80;
	[SerializeField] private MouseMoveType m_MouseMoveType = MouseMoveType.AlwaysMouseRotType;

	[Header("Distance Reference")]
	[SerializeField, ReadOnly] private float m_Listendictance = 0f;
	[SerializeField] private float distance = 10.0f;
	[SerializeField] private float minDistance = 4;
	[SerializeField] private float maxDistance = 10;
	[SerializeField] private float zoomSpeed = 10f;
	[SerializeField] private float zoomSensitivity = 1f;
	[Header("FOV Reference")]
	[SerializeField] private UseFOVType m_UseFOVType = UseFOVType.None;
	[SerializeField, ShowIf("@m_UseFOVType != UseFOVType.None")] private float m_FOVSmoothTime = 0.2f;
	[SerializeField, ReadOnly, ShowIf("@m_UseFOVType == UseFOVType.FOVZOomType")] private bool m_isOnForFOVZoom;
	[SerializeField, ReadOnly, ShowIf("@m_UseFOVType == UseFOVType.FOVZOomType")] private float m_FOVZoomMin, m_FOVZoomMax;

	//Bloking 해당 콜라이더를 막기만 하고 레이에 따른 앞뒤의 간극 조정은 타입에 따라 다르게 설정해야된다
	[Header("Blocking Reference")]
	[SerializeField] private LayerMask m_BlockingLayers;
	[SerializeField] private float m_BlockingRadius = 1f;
	[SerializeField] private float m_BlockingSmoothTime = 0.1f;
	[SerializeField] private float m_BlockingOriginOffset;
	[SerializeField, Range(0f, 1f)] private float m_BlockedOffset = 0.5f;

	[Tooltip("Control Values")]
	System.Func<System.Action> m_LerpTargetComplateCallBack = null;

	//[Tooltip("Property")]
	public UpdateMode pp_UpdateMode { set => m_UpdateMode = value;}

	private Vector2 m_ListenMainRotPower = Vector2.zero;
	// 카메라의 현재 x 회전
	public float ms_MainRotX { get; private set; }
	// 카메라의 현재 y 회전
	public float ms_MainRotY { get; private set; }
	//캐릭터와의 거리를 재계산한 값
	public float distanceTarget { get; private set; }

	// 줌 프로퍼티
	private float zoomAdd
	{
		get
		{
			float scrollAxis = Input.GetAxis("Mouse ScrollWheel");
			if (scrollAxis > 0) return -zoomSensitivity;
			if (scrollAxis < 0) return zoomSensitivity;
			return 0;
		}
	}

	[Tooltip("Linked Values")]
	private Vector3 m_TargetDistance, position;
	private Quaternion m_Rotation = Quaternion.identity;
	private Vector3 m_SmoothPosition;
	private Camera m_ControllCam;
	private bool m_isFixedFrame;
	private float fixedDeltaTime;
	private float m_BlockedDistance = 10f, m_BlockedDistanceV;
	[Tooltip("공회전 상태에서 필요한 변수들")]
	private Quaternion m_MainRotValue = Quaternion.identity;
	private Vector3 lastUp;
	public Camera pp_ControllCam { get => m_ControllCam; }

	[Tooltip("Required Value")]
	private System.Action m_ComplateCallBack = null;
	[Tooltip("Control Data Values")]
	[SerializeField, ReadOnly] public CamControllerInitInfo? m_CamControllerModel { get; private set; }

	public override object[] Initlization(params object[] _ParsingParams)
	{
		m_isActionProcess = false;
		bool isDirectActionCondition = m_isTestMode;
		m_ComplateCallBack = () =>
		{
			if (m_MainController is I_SubModulesCollection GetComplateSign)	GetComplateSign.I_CheckSubModuleIsAllCanAction(this);
			else m_isActionProcess = true;
		};
		var ApplyParams = base.Initlization(_ParsingParams);
		NewActionInfo GetActionInfo = (NewActionInfo)ApplyParams[0];
		CamControllerInitInfo? CamApplyInfo = ApplyControllerInfoData<CamControllerInitInfo>(GetActionInfo.s_RequiredSubModuleParams);
		

		if (CamApplyInfo == null) 
		{
			m_UpdateMode = UpdateMode.LateUpdate;
			m_isNotUseRot = true;
			m_ComplateCallBack?.Invoke();
			return null; 
		}
		m_CamControllerModel = CamApplyInfo;
		#region Main View Init Reference
		m_ViewType = CamApplyInfo.Value.m_ViewType;
		if (m_ViewType != ViewType.POV && m_MainController != null && (m_MainController.pp_isMine || m_MainController.pp_isTempMine))
		{
			var GetApplyCamTarget = Helper.ChildLinearStuctureSearch(m_MainController.transform).HFind(x => x.name == "CamTarget");
			m_Target = GetApplyCamTarget != null ? GetApplyCamTarget : null; m_RotationSpace = null;
			m_ListenTarget = m_Target;
		}
		else if (m_ViewType == ViewType.POV)
		{
			if(m_isUseInputController)
			m_ReciveInputCon = pp_MainController.transform.ChildLinearStuctureSearch<BInputController>()[0];
			m_Target = null; m_ListenTarget = null; m_RotationSpace = this.transform;
			lastUp = m_RotationSpace != null ? m_RotationSpace.up : Vector3.up;
		}
        #endregion
        #region Other Apply Params Reference
        m_isNotUseRot = CamApplyInfo.Value.s_isNotUseRot;
		m_SmoothFollow = CamApplyInfo.Value.s_isFollowSmooth;
		m_isBreathingMotion = CamApplyInfo.Value.s_isBreathingMotion;
		m_UseFOVType = CamApplyInfo.Value.s_UseFOVType;
		isDirectActionCondition = CamApplyInfo.Value.s_StartType == StartMoveType.None;
		if(!isDirectActionCondition) InitListenStartPos(CamApplyInfo.Value);
		#endregion
		#region 각 조건에 따라 적용될 수 있는 파라미터
		if (m_isNotUseRot)
			SetAngles(Quaternion.Euler(CamApplyInfo.Value.spp_StaticRot), true);
		if (m_SmoothFollow)
			followSpeed = CamApplyInfo.Value.spp_FollowSped;
		if (m_isBreathingMotion)
			m_InfinityApplySpped = CamApplyInfo.Value.spp_infinityBreahing;
        //if (m_UseFOVType == UseFOVType.ApplyRatioType) //DataManager<클래스>에서 관련된 데이터를 가져와 적용한다 (초기화때만 진행)
        if (CamApplyInfo.Value.spp_isApplyCustom) 
		{
			m_MainOffset = CamApplyInfo.Value.spp_OffsetTarget;
			distance = CamApplyInfo.Value.spp_MinZoom;
			minDistance = CamApplyInfo.Value.spp_MinZoom;
			maxDistance = CamApplyInfo.Value.spp_MaxZoom;
		}
		#endregion
		m_UpdateMode = UpdateMode.LateUpdate;
        if (isDirectActionCondition) 
			m_ComplateCallBack?.Invoke();
		return null;
    }

	#region Sub System Functions (**Control Params Reference**)

	public void SetChangeUpdateState(UpdateMode _currUpdate, Transform _SettingTr = null)
	{
		m_UpdateMode = _currUpdate;
		this.transform.SetParent(m_UpdateMode == UpdateMode.NoUpdate ? _SettingTr : null);
		//AppInstance.Instance.ms_DelEventManager.DELGMSetCusor(!lockCursor);
		if (m_UpdateMode == UpdateMode.NoUpdate && _SettingTr != null) 
		this.gameObject.NewObjAndMakeSyncPosRot(_SettingTr, Helper.SyncType.NotIntsnce);
	}

	public void ChangeTarget(Transform _GetTarget, float _AddSpeed = default, float _AddConditionValue = default, System.Action _EndCallBack = null)
    {
		if ((m_ViewType == ViewType.POV || m_ViewType == ViewType.None).
		HDebug("카메라 타겟팅은 특정 상태에서만 가능합니다.!", Helper.HDType.Error) ||
		(m_LerpTargetComplateCallBack != null).HDebug("아직 완료되지 않는 시점이 존재합니다.!", Helper.HDType.Error)) return;

		m_ListenTarget = _GetTarget;
		var OGSpeed = followSpeed;
		if (_AddSpeed != default) followSpeed = _AddSpeed;
		m_LerpTargetComplateCallBack = () => 
		{
			var GetDIs = Vector3.Distance(this.transform.position, m_ListenTarget.position);
			if (GetDIs < distance + 0.1f + _AddConditionValue) 
            {
				followSpeed = OGSpeed;
				m_LerpTargetComplateCallBack = null;
				return _EndCallBack;
			}
			return null;
		};
	}

    #region Angle Control Referecne

    public void SetAngles(Quaternion rotation, bool _isLerp = false)
	{
		Vector3 euler = rotation.eulerAngles;
        if (_isLerp)
        {
			m_ListenMainRotPower = 
			new Vector2(euler.y, euler.x);
			return;
        }
		m_ListenMainRotPower = new Vector2(euler.y, euler.x);
		this.ms_MainRotX = euler.y;
		this.ms_MainRotY = euler.x;
	}

	public void SetAngles(float yaw, float pitch)
	{
		this.ms_MainRotX = yaw;
		this.ms_MainRotY = pitch;
	}

	public void SetZoomParams(float _CurrDis, float _Min, float _Max)
	{
		minDistance = _Min;
		maxDistance = _Max;
		m_Listendictance = _CurrDis;
	}

	// 최소값과 최대값을 제한하여 위아래의 회전을 매개변수에 따른 제약을 주기위한 로직
	private float ClampAngle(float angle, float min, float max)
	{
		if (angle < -360) angle += 360;
		if (angle > 360) angle -= 360;
		return Mathf.Clamp(angle, min, max);
	}
    #endregion

    #endregion

    #region Sub System Private Functions (**Compare Update Reference**)

    private bool CheckCalculateAbsHigher(float _Compare1, float _Compare2, float _ConditionValue)
	=> Mathf.Abs(Mathf.Abs(_Compare1) - Mathf.Abs(_Compare2)) > _ConditionValue;

	private bool CheckCalculateAbsLasser(float _Compare1, float _Compare2, float _ConditionValue)
	=> Mathf.Abs(_Compare1) - Mathf.Abs(_Compare2) < _ConditionValue;

	#endregion

	#region Sub System Functions (**Init Support Reference**)

	private void InitListenStartPos(CamControllerInitInfo _ApplyCamInfo)
    {
		switch (_ApplyCamInfo.s_StartType)
		{
			case StartMoveType.MoveLogicType:
				Appinstance.Instance.ms_DelEventManager.DEL_InitCameraControl_StartPos += ReciveStartPos;
				break;
			case StartMoveType.StaticMoveStartType:
				ReciveStartPos(_ApplyCamInfo.s_StartPos, false);
				break;
			case StartMoveType.MoveVirtualType:
				Vector3 CurrCalculatePos = position; ReciveStartPos(CurrCalculatePos, false);
				break;
		}
	}

	private void ReciveStartPos(Vector3 _GetMovePos, bool _isRemoveDelData, System.Action _EndCallBack = null)
	{
		this.transform.DOMove(_GetMovePos, 0.65f).SetEase(Ease.InOutCirc).OnComplete(() =>
		{
			m_ComplateCallBack?.Invoke();
			_EndCallBack?.Invoke();
			if (_isRemoveDelData) Appinstance.Instance.ms_DelEventManager.DEL_InitCameraControl_StartPos -= ReciveStartPos;
		});
	}

	//private void ReciveMoveTr(Transform _MoveTr, bool _isRemoveDelData, System.Action _EndCallBack = null)
	//{
	//	this.transform.DOMove(_MoveTr.position, 0.65f).SetEase(Ease.InOutCirc).OnComplete(() =>
	//	this.transform.DOMove(_MoveTr.position, 0.65f).SetEase(Ease.InOutCirc).OnComplete(() =>
	//	{
	//		m_ComplateCallBack?.Invoke();
	//		_EndCallBack?.Invoke();
	//		if (_isRemoveDelData) Appinstance.Instance.ms_DelEventManager.DEL_InitCameraControl_StartPos -= ReciveStartPos;
	//	});
	//}


	#endregion

	#region System Update Functions

	#region Input Update Process(후에 InputController로 이전조치 될 수 있음)
	// Camera전용 InputController에 대해 로직 이전 처리(후에 InputController<클래스>에 할당가능하다면 다시 로직 이전 요망)
	// UnityInputSystem의 로직데로 전부 재정의 해야한다
	public void UpdateInput()
	{
		if (!m_isActionProcess) return;
		if (!m_ControllCam.enabled || m_CamControllerModel == null || !m_CamControllerModel.HasValue) return;

        // 마우스 회전구간에 커서의 방향성
        //AppInstance.Instance.ms_DelEventManager.DELGMSetCusor(!lockCursor);
        Cursor.lockState = lockCursor ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = lockCursor ? false : true;

        bool isRotateAction = m_MouseMoveType == MouseMoveType.AlwaysMouseRotType ||
		(m_MouseMoveType == MouseMoveType.Click_LeftClickType && Input.GetMouseButton(0)) || 
		(m_MouseMoveType == MouseMoveType.Click_RightClickType && Input.GetMouseButton(1)) || 
		(m_MouseMoveType == MouseMoveType.Click_MiddleClickType && Input.GetMouseButton(2));

		// 감도 재계산
		if (!m_isNotUseRot && isRotateAction)
		{
			ms_MainRotX += Input.GetAxis("Mouse X") * m_RotationSensitivity;
			ms_MainRotY = ClampAngle(ms_MainRotY - Input.GetAxis("Mouse Y") * m_RotationSensitivity, yMinLimit, yMaxLimit);
			if (m_ViewType == ViewType.POV) m_MainRotValue = Quaternion.Euler(new Vector3(ms_MainRotX, ms_MainRotY, 0));
		}

		if (m_ViewType == ViewType.POV && m_isUseInputController && m_ReciveInputCon.mspp_InputValues != null)
        {
			var MoveValue = m_ReciveInputCon.mspp_InputValues.s_MoveValue;
			m_POVMovePower = new Vector3(MoveValue.x, 0f, MoveValue.y);
		}

		if (m_UseFOVType == UseFOVType.FOVZOomType)
		{
			m_FOVZoomMin = m_CamControllerModel.Value.s_FOVMinZoom;
			m_FOVZoomMax = m_CamControllerModel.Value.s_FOVMaxZoom;
			m_isOnForFOVZoom = Input.GetMouseButton(1);
		}
        // 거리값 최소최대에 대한 제한
        distanceTarget = Mathf.Clamp(distanceTarget + zoomAdd, minDistance, maxDistance);
	}
    #endregion

    #region Main Update Process

    // 재설계 보류 (먼저 한곳에 몰아넣은후에 F3 패턴 적용하자)
    public void UpdateProcess()
	{
		UpdateTransform(Time.deltaTime);
		UpdateCamValue(Time.deltaTime);
	}

	private void UpdateCamValue(float _DeltaTime)
    {
		if (m_UseFOVType == UseFOVType.None) return;
		m_ControllCam.fieldOfView = m_UseFOVType switch
		{
			UseFOVType.StaticFOVType => Mathf.SmoothDamp(m_ControllCam.fieldOfView, m_CamControllerModel.Value.s_StaticFOVValue, ref m_BlockedDistanceV, m_FOVSmoothTime),
			UseFOVType.FOVZOomType => Mathf.SmoothDamp(m_ControllCam.fieldOfView, (m_isOnForFOVZoom ? m_FOVZoomMin : m_FOVZoomMax), ref m_BlockedDistanceV, m_FOVSmoothTime),
			_ => m_ControllCam.fieldOfView
		};
		if (m_UseFOVType == UseFOVType.FOVZOomType)
		{
			//해상도 값이 계산중이라면 그값을 비례해서 FOV를 재계산해야한다(**리팩 요망 * *)
			if (m_ControllCam.fieldOfView < (m_FOVZoomMin + 0.001f)/*40.001f*/) m_ControllCam.fieldOfView = m_FOVZoomMin;
			if (m_ControllCam.fieldOfView > (m_FOVZoomMax - 0.001f)/*59.999f*/) m_ControllCam.fieldOfView = m_FOVZoomMax;
		}
    }

	private void UpdateTransform(float deltaTime)
	{
		if (!m_ControllCam.enabled) return;

		if (m_LerpTargetComplateCallBack != null) m_LerpTargetComplateCallBack()?.Invoke();

		// 주 회전값
		m_Rotation = Quaternion.AngleAxis(ms_MainRotX, Vector3.up) * Quaternion.AngleAxis(ms_MainRotY, Vector3.right);

		#region 공 회전 상태일때 (**현재 회전이 제대로 적용되지 않음 리팩 요망**)
		if (m_ViewType == ViewType.POV && m_RotationSpace != null)
        {
            if (!m_isNotUseRot) //다른 회전방식으로 적용해야됨
			{
				m_MainRotValue = Quaternion.FromToRotation(lastUp, m_RotationSpace.up) * m_MainRotValue;
				m_Rotation = m_MainRotValue * m_Rotation;

				lastUp = m_RotationSpace.up;
			}

			if(m_POVMovePower.magnitude != 0) transform.position += m_POVMovePower;
		}
        #endregion

        //타겟에 따른 회전의 계산
        //(거리에 따른 앞뒤의 거리계산 +카메라 Target Offset의 거리와의 보간 + SphereCast에 의해 벽을 확인 후에 다시 거리계산)
        if (m_ViewType != ViewType.POV && m_Target != null) //이후에 각 상황별 제약사항을 만들어 해당 제약사항에 따라 처리 이후 적용될 수 있도록 수정할것
		{
            if (!m_Target.Equals(m_ListenTarget))
            {
				m_Target = m_ListenTarget;
				//return;
			}

			// 줌에 따른 거리 조절
			distance += (distanceTarget - distance) * zoomSpeed * deltaTime;

			// 보간 형태로 움직일건지 수정(230325 로직 수정)
			if (!m_SmoothFollow) m_SmoothPosition = m_Target.position;
			else m_SmoothPosition = Vector3.Lerp(m_SmoothPosition, m_Target.position, deltaTime * followSpeed);
            #region Smooth Lerp Listen To OG Value Move
            if (m_SmoothFollow && CheckCalculateAbsLasser(distance, m_Listendictance, 1.5f))//&& (Mathf.Abs(distance) - Mathf.Abs(m_Listendictance)) < 1.5f)
			distance = Mathf.Lerp(distance, m_Listendictance, deltaTime * followSpeed);
			if (m_isNotUseRot &&
			(CheckCalculateAbsHigher(m_ListenMainRotPower.x, ms_MainRotX, 0.05f) || CheckCalculateAbsHigher(m_ListenMainRotPower.y, ms_MainRotY, 0.05f)))
			{
				ms_MainRotX = Mathf.Lerp(ms_MainRotX, m_ListenMainRotPower.x, deltaTime * followSpeed * 2f);
				ms_MainRotY = Mathf.Lerp(ms_MainRotY, m_ListenMainRotPower.y, deltaTime * followSpeed * 2f);
			}
			#endregion

			// Offset에 따른 위치와 회전의 수정
			Vector3 t = m_SmoothPosition + m_Rotation * m_MainOffset;
			Vector3 f = m_Rotation * -Vector3.forward;

			//구체형태 케스트를 Point지점 계산하여 Layer에 따라 해당 방향으로 위치 이동
			if (m_BlockingLayers != -1)
			{
				RaycastHit hit;
				if (Physics.SphereCast(t - f * m_BlockingOriginOffset, m_BlockingRadius, f, out hit,
				m_BlockingOriginOffset + distanceTarget - m_BlockingRadius, m_BlockingLayers))
				{
					m_BlockedDistance = Mathf.SmoothDamp(m_BlockedDistance, hit.distance + m_BlockingRadius * (1f - m_BlockedOffset)
						- m_BlockingOriginOffset, ref m_BlockedDistanceV, m_BlockingSmoothTime);
				}
				else m_BlockedDistance = distanceTarget;

				distance = Mathf.Min(distance, m_BlockedDistance);
			}

			//카메라 숨쉬는 연출 로직을 담당함
			Vector3 NewInfinity = Vector3.zero;
			if (m_isBreathingMotion)
            {
				float GetInfiX = Mathf.Sin(Time.time) * m_InfinityApplySpped;
				float GetInfiY = Mathf.Cos(Time.time) * m_InfinityApplySpped;
				NewInfinity = new Vector3(GetInfiX, GetInfiY, 0f);
			}
			position = t + f * distance + NewInfinity;
			//position = t + f * distance; //원래 로직

			// 캐릭터까지의 카메라 이동 다시말해서 캐릭터와 카메라의 거리를 유지하는 값을 대입한다고 생각하면 된다
			if(m_isActionProcess) 
				transform.position = position;
		}

		//회전을 정지하고자 할때 원래는 캐릭터의 전방형태를 유지했으나 현재는 카메라의 전방으로 변경
		//transform.rotation = m_isNotUseRot ? Quaternion.LookRotation(m_Target.forward) : m_Rotation;
		transform.rotation = m_Rotation;
	}
    #endregion

    #endregion

    #region Main System Public Functions (**Main Process**)

    public override void BreakPoint(bool _isBreak, 
	SubModuleBreakType _BrackType = SubModuleBreakType.BreakAll, Type _GetType = null)
    => base.BreakPoint(_isBreak, _BrackType, _GetType);

    #endregion

    #region 유니티 이벤트 함수

    // 초기화를 기준으로 카메라의 현재 변환에 대한 매개변수를 설정
    void Awake()
	{
		Vector3 GetAnlges = transform.eulerAngles;
		ms_MainRotX = GetAnlges.y;
		ms_MainRotY = GetAnlges.x;
		m_ListenMainRotPower = new Vector2(ms_MainRotX, ms_MainRotY);
		m_Listendictance = distance;
		distanceTarget = distance;
		m_SmoothPosition = transform.position;

		m_ControllCam = GetComponent<Camera>();
        if (!m_ControllCam.Equals(Camera.main))
		Destroy(Camera.main.gameObject);

		lastUp = m_RotationSpace != null ? m_RotationSpace.up : Vector3.up;
    }



	protected override void Update()
	{        
		if (!m_isActionProcess) return;

		if (m_UpdateMode == UpdateMode.Update) UpdateProcess();
	}

	void FixedUpdate()
	{
		if (!m_isActionProcess) return;

		m_isFixedFrame = true;
		fixedDeltaTime += Time.deltaTime;
		if (m_UpdateMode == UpdateMode.FixedUpdate) UpdateProcess();
	}

	void LateUpdate()
	{
		if (m_UpdateMode == UpdateMode.LateUpdate)
        {
			UpdateInput();
			UpdateProcess();
		}
            

		if (m_UpdateMode == UpdateMode.FixedLateUpdate && m_isFixedFrame)
		{
			UpdateTransform(fixedDeltaTime);
			fixedDeltaTime = 0f;
			m_isFixedFrame = false;
		}
	}
	#endregion
}