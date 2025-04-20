using UnityEngine;
using Cinemachine;
using Commons.Helpers;
using Sirenix.OdinInspector;
using DG.Tweening;
using ViewType = CameraController.ViewType;
using UpdateMode = CameraController.UpdateMode;
using CamControllerInitInfo = CameraController.CamControllerInitInfo;


public class CinemachineCameraController : CameraControllerBase
{
	#region CinemachineCam Info
	public enum CinemachineBodyType
	{
		None,
		CinemachineTransposer,
		CinemachineFramingTransposer,
		CinemachineTrackedDolly,
		CinemachineHardLockToTarget,
		CinemachineOrbitalTransposer,
		Cinemachine3rdPersonFollow
	}
	#endregion

	[Header("Required Values")]
	[SerializeField, ReadOnly] private CinemachineBodyType m_CinemachineBodyType;
	[SerializeField] private UpdateMode m_UpdateMode = UpdateMode.NoUpdate;
	[SerializeField] private CinemachineVirtualCamera m_VirtualCameraController;
	[SerializeField] private CinemachineBrain m_CinemachineBrain;
	[SerializeField, ReadOnly, ShowIf("@m_CinemachineBrain != null")] private CinemachineBrain m_InstanceCinemachineBrain;

	[Header("Main Values")]
	[SerializeField, ReadOnly] private ViewType m_ViewType;
	//[SerializeField] private Transform m_Target;
	[SerializeField, ShowIf(nameof(Odin_ViewPOVToggle))] private bool m_isUseInputController;
	[SerializeField, ReadOnly, ShowIf(nameof(Odin_ViewPOVToggle))] private BInputController m_ReciveInputCon;

	[Header("Rotate Reference")]
	[SerializeField] private bool m_isNotUseRot;

	[Tooltip("Required Value")]
	private CamControllerInitInfo? m_CamControllerModel;
	private System.Action m_ComplateCallBack = null;
	private System.Func<float> m_RatioCalculateCallBack;
	private System.Func<System.Action> m_LerpTargetComplateCallBack;

	[Tooltip("Control Value")]
	private Vector3 m_MainOffset; //현재 적용되지 않음
	private float distance, minDistance, maxDistance;

	public float mpp_MainRotX { get; private set; }
	public float mpp_MainRotY { get; private set; }

	#region ODIN Support Property

	private bool Odin_ViewPOVToggle() => m_ViewType == ViewType.POV;
	public Camera pp_UsedMainCam { get; private set; }

	#endregion

	public override object[] Initlization(params object[] _ParsingParams)
	{
		m_isActionProcess = false;
		bool isDirectActionCondition = m_isTestMode;
		var ApplyParams = base.Initlization(_ParsingParams);
		if(ApplyParams[0] is NewActionInfo GetActionInfo)
		m_CamControllerModel = ApplyControllerInfoData<CamControllerInitInfo>(GetActionInfo.s_RequiredSubModuleParams);
		//if (ApplyParams[0] is RunnerPlayInfo GetRActionInfo)
		//m_CamControllerModel = (CamControllerInitInfo)GetRActionInfo.s_PairSubControlBase.HFind(x => x.Item1.GetType().Name == this.GetType().Name).Item2;
		//init Cinemachine Cam
		m_VirtualCameraController = InitCinemachineVirtual();
		pp_UsedMainCam = m_InstanceCinemachineBrain.GetComponent<Camera>();

		m_ComplateCallBack = () =>
		{
			if (m_MainController is I_SubModulesCollection GetComplateSign) GetComplateSign.I_CheckSubModuleIsAllCanAction(this);
			else m_isActionProcess = true;
		};

		if (m_CamControllerModel == null)
		{
			m_isNotUseRot = true;
			m_ComplateCallBack?.Invoke();
			return null;
		}
		#region Main View Init Reference
		m_ViewType = m_CamControllerModel.Value.m_ViewType;
		if (m_ViewType != ViewType.POV && m_MainController != null && (m_MainController.pp_isMine || m_MainController.pp_isTempMine))
		{
			if(m_MainController is NewCharacterController)
			m_Target = Helper.ChildLinearStuctureSearch(m_MainController.transform).HFind(x => x.name == "CamTarget");
			//if (m_MainController is RunnerController _GetRController)
			//m_Target = _GetRController.pp_PlayerModuleNow;
		}
		else if (m_ViewType == ViewType.POV)
		{
			if (m_isUseInputController) m_ReciveInputCon = pp_MainController.transform.ChildLinearStuctureSearch<BInputController>()[0];
			m_Target = null;
		}

		m_VirtualCameraController.transform.rotation = Quaternion.LookRotation(m_Target.forward);
		m_isNotUseRot = m_CamControllerModel.Value.s_isNotUseRot;
		m_VirtualCameraController.m_Follow = m_Target;
		#endregion

		#region 각 조건에 따라 적용될 수 있는 파라미터
		var GetC3P = m_VirtualCameraController.GetCinemachineComponent<CinemachineFramingTransposer>();
		m_CinemachineBodyType = CinemachineBodyType.CinemachineFramingTransposer;
		if (m_isNotUseRot)
        {
            #region Cinemachine3rdPersonFollow 전용
            //GetC3P.ShoulderOffset = m_CamControllerModel.Value.spp_StaticRot;
            //GetC3P.VerticalArmLength = 0.3f;
            #endregion
            //m_VirtualCameraController.m_LookAt = m_Target;
            //SetAngles(Quaternion.Euler(m_CamControllerModel.Value.spp_StaticRot));
        }
        #region 현재 사용하지 않는 카메라 적용 사항들(**향후 리팩토링 후 적용할 수 있도록 리팩 요망**)
        //if (m_SmoothFollow)
        //	followSpeed = CamApplyInfo.Value.spp_FollowSped;
        //if (m_isBreathingMotion)
        //	m_InfinityApplySpped = CamApplyInfo.Value.spp_infinityBreahing;
        //if (m_UseFOVType == UseFOVType.ApplyRatioType) //DataManager<클래스>에서 관련된 데이터를 가져와 적용한다 (초기화때만 진행)
        #endregion
        if (m_CamControllerModel.Value.spp_isApplyCustom)
		{
			m_MainOffset = m_CamControllerModel.Value.spp_OffsetTarget;
			distance = m_CamControllerModel.Value.spp_MinZoom;
			#region Cinemachine3rdPersonFollow 전용
			//GetC3P.CameraDistance = distance;
			//GetC3P.Damping = m_MainOffset;
			#endregion
			//minDistance = m_CamControllerModel.Value.spp_MinZoom;
			//maxDistance = m_CamControllerModel.Value.spp_MaxZoom;
			//var CinemachineFT = m_VirtualCameraController.GetCinemachineComponent<CinemachineFramingTransposer>();
			GetC3P.m_TrackedObjectOffset = m_CamControllerModel.Value.spp_OffsetTarget;
			GetC3P.m_CameraDistance = distance;
        }
        #endregion

        //View Out Init
        //m_RatioCalculateCallBack = SettingCameraInit(m_CinemachineBrain.OutputCamera);
		//CalculateRatio(m_RatioCalculateCallBack());


		m_UpdateMode = UpdateMode.LateUpdate;
		m_ComplateCallBack?.Invoke();
		return null;
	}

	#region Main System Private Functions (**Init Reference**)

	private CinemachineVirtualCamera InitCinemachineVirtual()
    {
		CinemachineVirtualCamera ReturnValue = null;
		if(Camera.main.gameObject != null)
		{
			bool isHaveMainCam = Camera.main.TryGetComponent<CinemachineBrain>(out CinemachineBrain _GetBrain);
			if(!isHaveMainCam) Destroy(Camera.main.gameObject);
			m_InstanceCinemachineBrain = isHaveMainCam ? _GetBrain : Instantiate(m_CinemachineBrain, null);
		}
		if(ReturnValue == null) ReturnValue = FindAnyObjectByType<CinemachineVirtualCamera>();
		//if (ReturnValue != null) Destroy(this.GetComponent<CinemachineVirtualCamera>());
		if (ReturnValue == null)
        {
			this.transform.position = m_InstanceCinemachineBrain.transform.position;
			ReturnValue.gameObject.CheckComnectComponent<CinemachineVirtualCamera>();
		}
		return ReturnValue;
	}

	#endregion

	#region Sub System Functions (**Control Params Reference**)

	private void CalculateRatio(float _CalculateCurrRatioX)
    {
		Vector2 SpriteSize = m_SPCamInitSettingInstance.sprite.rect.size / m_SPCamInitSettingInstance.sprite.pixelsPerUnit;
		float ApplyRatioX = SpriteSize.x * Screen.height / Screen.width * m_CalculateValue;
        float SettingViewDis = pp_UsedMainCam.orthographic ?
        m_CamControllerModel.Value.s_OrthographicSizeValue : m_CamControllerModel.Value.s_StaticFOVValue;
        if (pp_UsedMainCam.orthographic) m_VirtualCameraController.m_Lens.OrthographicSize = ApplyRatioX > SettingViewDis ? ApplyRatioX : SettingViewDis;
        else m_VirtualCameraController.m_Lens.FieldOfView = ApplyRatioX > SettingViewDis ? ApplyRatioX : SettingViewDis;
    }

	//향후 추상화를 통해 시네머신의 각 객체로 파생하여 적용될 수 있도록 수정할것
	public override void ChangeTarget(Transform _GetTarget, float _AddSpeed = default, float _AddConditionValue = default, System.Action _EndCallBack = null)
	{
		SetAngles(Quaternion.LookRotation(m_VirtualCameraController.transform.forward));

		//이후 System.Reflection 형태로 템플릿과 문자열로 저장 로드할 수 있도록 수정할것 **리팩 요망**
		var GetFT = m_VirtualCameraController.GetCinemachineComponent<CinemachineFramingTransposer>();
		var OGSpeed = GetFT.m_XDamping;
		GetFT.m_XDamping = _AddSpeed != default ? _AddSpeed : OGSpeed;
		GetFT.m_YDamping = _AddSpeed != default ? _AddSpeed : OGSpeed;
		GetFT.m_ZDamping = _AddSpeed != default ? _AddSpeed : OGSpeed;
		m_VirtualCameraController.m_Follow = _GetTarget;
		float GetDIs = Vector3.Distance(pp_UsedMainCam.transform.position, _GetTarget.position);
		bool isHigher = GetDIs > GetFT.m_CameraDistance;
		m_LerpTargetComplateCallBack = () =>
		{
			GetDIs = Vector3.Distance(Camera.main.transform.position, _GetTarget.position);
			if ((isHigher && GetDIs < GetFT.m_CameraDistance + 0.1f + _AddConditionValue) ||
			(!isHigher && GetDIs > GetFT.m_CameraDistance - 0.1f - _AddConditionValue))
			{
				GetFT.m_XDamping = OGSpeed; GetFT.m_YDamping = OGSpeed; GetFT.m_ZDamping = OGSpeed;
				m_LerpTargetComplateCallBack = null;
				return _EndCallBack;
			}
			return null;
		};
	}

	#endregion

	#region Main Update Process

	//상속화 요망**
	private void UpdateTransform(float _DeltaTime)
    {
		CalculateRatio(-1);
		//if (m_RatioCalculateCallBack != null)
		//CalculateRatio(m_RatioCalculateCallBack());

        if (m_LerpTargetComplateCallBack != null)
			m_LerpTargetComplateCallBack()?.Invoke();

		//리팩 요망 **
		if(m_ViewType != ViewType.POV && m_Target != null && m_CinemachineBodyType == CinemachineBodyType.CinemachineFramingTransposer)
        {
            #region Offset에 따른 카메라 방향각도 조절 시도 1
            //var LookDirection = m_Target.transform.forward - m_VirtualCameraController.transform.position;
            //var GetRotNow = Quaternion.LookRotation(LookDirection);
            //Vector3 CameraPosition = m_Target.transform.position + m_MainOffset;
            //Vector3 LookDirection = (m_Target.transform.position + m_Target.transform.forward * 5f) - CameraPosition;// 타겟의 전방 방향 + 타겟 위치를 약간 아래로 바라보게끔 오프셋 추가
            //var ArrowToCam = m_Target.transform.position - m_VirtualCameraController.transform.position;
            #endregion
            var GetRotNow = Quaternion.LookRotation(m_Target.transform.forward - Vector3.up * 0.3f);
			m_VirtualCameraController.transform.rotation = Quaternion.Slerp(
			m_VirtualCameraController.transform.rotation, GetRotNow, 3f * Time.deltaTime);

            #region 상하좌우로 이동시 각도를 Ray로 계산하여 시야각을 변경할 수 있도록 수정한다
            //Debug.DrawRay(m_Target.position, -m_Target.up * 1.5f);
            //if (Physics.Raycast(m_Target.position, -m_Target.up * 1.5f, out RaycastHit RayHit))
            //{
            //	if ((m_Target.position.y) > RayHit.point.y)
            //		m_VirtualCameraController.transform.position = m_VirtualCameraController.transform.position + Vector3.up * -RayHit.point.y;
            //}
            #endregion
        }

    }

	#endregion

	#region Sub System Private Functions

	private Sequence m_GetDistanceSeq;

	public override void SetAngles(Quaternion rotation, bool _isLerp = false)
	=> m_Target.rotation = rotation;

	//임시 템플릿으로 나누고 오버로딩화 해야됨
	public void SetDistanceAndOffsetY(float _ChangeValue, bool _ReturnOG = false)
    {
		var GetC3P = m_VirtualCameraController.GetCinemachineComponent<CinemachineFramingTransposer>();
		m_CinemachineBodyType = CinemachineBodyType.CinemachineFramingTransposer;
		
		if (_ReturnOG)
        {
			GetC3P.m_TrackedObjectOffset = m_CamControllerModel.Value.spp_OffsetTarget;
			GetC3P.m_CameraDistance = distance;
			return;
		}
		
		var OGOffset = GetC3P.m_TrackedObjectOffset;
		if (m_GetDistanceSeq != null) m_GetDistanceSeq.Kill();
		m_GetDistanceSeq = null;
		m_GetDistanceSeq = DOTween.Sequence();

		m_GetDistanceSeq.Append(DOTween.To(() => GetC3P.m_TrackedObjectOffset.y, x => GetC3P.m_TrackedObjectOffset.y = x,
		OGOffset.y + _ChangeValue, 0.35f).SetEase(Ease.OutSine));
		m_GetDistanceSeq.Append(DOTween.To(() => GetC3P.m_CameraDistance, x => GetC3P.m_CameraDistance = x,
		GetC3P.m_CameraDistance + _ChangeValue, 0.35f).SetEase(Ease.OutSine));

		//var OGOffset = GetC3P.m_TrackedObjectOffset;
		//var ApplyValue = OGOffset.y + _ChangeValue;
		//GetC3P.m_TrackedObjectOffset = new Vector3(OGOffset.x, ApplyValue, OGOffset.z);
		//GetC3P.m_CameraDistance = ApplyValue;
	}

	#endregion

	#region Sub System PrivateFunctions (**Main Process**)

	public override void BreakPoint(bool _isBreak, SubModuleBreakType _BrackType = SubModuleBreakType.BreakAll, System.Type _GetType = null)
    {
        base.BreakPoint(_isBreak, _BrackType, _GetType);
		if(m_VirtualCameraController != null)
		m_VirtualCameraController.m_Follow = _isBreak ? m_Target : null;
    }

	public void ReciveStartPos(float _TweeningSped = 0.65f, Transform _PointTr = null, 
	Vector3 _MovePos = default, Quaternion _TurnRot = default, System.Action _EndCallBack = null)
	{
		if (m_isActionProcess) return;
		_PointTr = _PointTr == null ? m_Target : _PointTr;
		_MovePos = _MovePos == default ? _PointTr.position : _MovePos;
		_TurnRot = _TurnRot == default ? _PointTr.rotation : _TurnRot;
		
		this.transform.DORotateQuaternion(_TurnRot /*Quaternion.LookRotation(_PointTr.forward - Vector3.up * 25f)*/, 0.65f).SetEase(Ease.InOutCirc);
		this.transform.DOMove(_MovePos/*_PointTr.position + Vector3.up * 25f*/, 0.65f).SetEase(Ease.InOutCirc).OnComplete(() =>
		_EndCallBack?.Invoke());
	}

	#endregion

	#region 유니티 이벤트 함수 

	protected override void Update()
    {
        base.Update();
		if (!m_isActionProcess) return;

		if (m_UpdateMode == UpdateMode.Update) UpdateTransform(Time.deltaTime);
	}

    private void LateUpdate()
    {
		if (!m_isActionProcess) return;

		if (m_UpdateMode == UpdateMode.LateUpdate) UpdateTransform(Time.deltaTime);
	}

    #endregion
}

public class CameraControllerBase : SubModuleControllerBase
{
	[Header("Required Values")]
	[SerializeField] protected float m_CalculateValue;
	[SerializeField] private SpriteRenderer m_SPCamInitSettingPrefab;
	[SerializeField/*, ReadOnly, ShowIf("@m_SPCamInitSettingInstance != null")*/] protected SpriteRenderer m_SPCamInitSettingInstance;
	[SerializeField] protected Transform m_Target;

	public virtual void SetAngles(Quaternion rotation, bool _isLerp = false) { }
	public virtual void ChangeTarget(Transform _GetTarget, float _AddSpeed = default, float _AddConditionValue = default, System.Action _EndCallBack = null) { }

	#region Main System Private Functions (**Init Reference**)
	protected virtual System.Func<float> SettingCameraInit(Camera _GetUsedCamNow)
	{
		//Calculate SR Init
		if(m_SPCamInitSettingInstance == null)
        {
			m_SPCamInitSettingInstance = Instantiate(m_SPCamInitSettingPrefab,
			this.transform.position, Quaternion.LookRotation(this.transform.forward), this.transform);

			m_SPCamInitSettingInstance.transform.position = (m_Target != null ? m_Target.position :
			Physics.Raycast(this.transform.position, this.transform.forward, out RaycastHit hit) ? hit.transform.position : Vector3.zero);

			m_SPCamInitSettingInstance.transform.localScale =
			PixelPerUnitToSRWorldPos(m_SPCamInitSettingInstance.sprite, Appinstance.Instance.ms_DataManager.pp_RatioApply);
		}
		
		return () => m_SPCamInitSettingInstance.bounds.size.x * Screen.height / Screen.width * m_CalculateValue;
	}
	#endregion

	//Helper로 이전될 가능성이 높음 (CameraController에 존재하기 부적합)
	protected Vector3 PixelPerUnitToSRWorldPos(Sprite _CalculateSpr, Vector2 _SettingSize)
	{
		Vector2 SpriteOGSize = _CalculateSpr.bounds.size;
		Vector2 TargetWorldSize = _SettingSize / _CalculateSpr.pixelsPerUnit;
		Vector3 ReturnValue =
		new Vector3(TargetWorldSize.x / SpriteOGSize.x, TargetWorldSize.y / SpriteOGSize.y, 1f);
		return ReturnValue * 0.8f;
	}
}
