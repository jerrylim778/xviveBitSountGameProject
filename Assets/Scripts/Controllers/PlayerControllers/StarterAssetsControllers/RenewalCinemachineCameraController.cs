using UnityEngine;
using Cinemachine;
using Sirenix.OdinInspector;
using Commons.Helpers;
using DG.Tweening;
using ViewType = CameraController.ViewType;
using StartMoveType = CameraController.StartMoveType;
using UpdateMode = CameraController.UpdateMode;
using CamControllerInitInfo = CameraController.CamControllerInitInfo;

public class RenewalCinemachineCameraController : SubModuleControllerBase, I_ActionEndCallSubModules
{
	[Header("Ratio Values")]
	[SerializeField] private float m_CalculateValue = 4f;
	[SerializeField] private SpriteRenderer m_SPCamInitSettingInstance;

	[Header("Cinamecine Required Values")]
	[SerializeField, ReadOnly] BaseCinamecineInfo m_BaseCinamecineInfoNow;
	[SerializeField] ControlCinemachineComponentBase[] m_GetAllControlCinBases;

	[Header("Required Values")]
	//[SerializeField, ReadOnly] private CinemachineBodyType m_CinemachineBodyType;
	[SerializeField] private UpdateMode m_UpdateMode = UpdateMode.NoUpdate;
	[SerializeField] private CinemachineVirtualCamera m_VirtualCameraController;
	[SerializeField] private CinemachineBrain m_CinemachineBrain;
	[SerializeField, ReadOnly, ShowIf("@m_CinemachineBrain != null")] private CinemachineBrain m_InstanceCinemachineBrain;

	[Header("Main Values")]
	[SerializeField, ReadOnly] private ViewType m_ViewType;
	[SerializeField, ReadOnly, ShowIf("@!Odin_ViewPOVToggle()")] private Transform m_Target;
	[SerializeField, ShowIf(nameof(Odin_ViewPOVToggle))] private bool m_isUseInputController;
	[SerializeField, ReadOnly, ShowIf(nameof(Odin_ViewPOVToggle))] private BInputController m_ReciveInputCon;

	[Header("Rotate Reference")]
	[SerializeField] private bool m_isNotUseRot;

	[Tooltip("Required Value")]
	private CamControllerInitInfo? m_CamControllerModel;
	private System.Action m_ComplateCallBack = null;

	//�� ��⿡�� ��Ʈ�� �����ҽ� �����Ұ�
	[Tooltip("Control Value")]
	private Vector3 m_MainOffset; //���� ������� ����
	private float distance, minDistance, maxDistance;

	public ControlCinemachineComponentBase pp_GetAllControlCinBaseNow => m_BaseCinamecineInfoNow.s_ControlCinBase;

	public float mpp_MainRotX { get; private set; }
	public float mpp_MainRotY { get; private set; }

	#region ODIN Support Property

	private bool Odin_ViewPOVToggle() => m_ViewType == ViewType.POV;

	public Camera pp_UsedMainCam { get; private set; }

	#endregion

	public override object[] Initlization(params object[] _ParsingParams)
	{
		m_isActionProcess = false;
		var ApplyParams = base.Initlization(_ParsingParams);
		if (ApplyParams[0] is I_PurifiedParamsData _ParamsData)
		m_CamControllerModel = (CamControllerInitInfo)_ParamsData;
		m_VirtualCameraController = InitCinemachineVirtual();
		pp_UsedMainCam = m_InstanceCinemachineBrain.GetComponent<Camera>();
		pp_UsedMainCam.clearFlags = CameraClearFlags.Skybox;

		m_ComplateCallBack = () =>
		{
			if (m_MainController is I_SubModulesCollection GetComplateSign) 
				GetComplateSign.I_CheckSubModuleIsAllCanAction(this);
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
			if (m_MainController is NewCharacterController)
				m_Target = Helper.ChildLinearStuctureSearch(m_MainController.transform).HFind(x => x.name == "CamTarget");
			//if (m_MainController is FlyFlipController _GetFlipCon)
			//	m_Target = _GetFlipCon.I_GetSubModule<FlipCharacterSubController>().transform;
			#region Other Game Reference
			//if (m_MainController is DragonBowController _GetDBowController)
			//	m_Target = _GetDBowController.pp_InstancePlayerSubcontroller.transform;
			//if (m_MainController is FlyKnifeSliceController _GetFKSController) //Fly Knife Slice Game 
			//m_Target = _GetFKSController.pp_InstanceFlyKnifeModule.transform;
			//if (m_MainController is RunnerController _GetRController) //Runner Game Refer
			//m_Target = _GetRController.pp_PlayerModuleNow;
			#endregion

			m_VirtualCameraController.transform.rotation = Quaternion.LookRotation(m_Target.forward);
			m_VirtualCameraController.m_Follow = m_Target;
		}
		else if (m_ViewType == ViewType.POV)
		{
			if (m_isUseInputController) m_ReciveInputCon = pp_MainController.transform.ChildLinearStuctureSearch<BInputController>()[0];
			m_Target = null;
		}
		#endregion

		#region Data Apply Reference
		m_isNotUseRot = m_CamControllerModel.Value.s_isNotUseRot;
		InitControlCinemachineBodyType(m_CamControllerModel.Value.s_CinemachineBodyType);
		
		if (m_CamControllerModel.Value.spp_isApplyCustom)
		{
			m_MainOffset = m_CamControllerModel.Value.spp_OffsetTarget;
			distance = m_CamControllerModel.Value.spp_MinZoom;
			minDistance = m_CamControllerModel.Value.spp_MinZoom;
			maxDistance = m_CamControllerModel.Value.spp_MaxZoom;
		}
		#endregion

		if (m_CamControllerModel.Value.s_StartType != StartMoveType.None)
		{
			InitListenStartPos(m_CamControllerModel.Value);
			return null;
		}
		m_UpdateMode = UpdateMode.LateUpdate;
		m_ComplateCallBack?.Invoke();
		return null;
	}

	public void I_EndCallSubModule()
    {
		m_BaseCinamecineInfoNow.s_ControlCinBase.Initlization(this,
		m_CamControllerModel.Value, m_VirtualCameraController, m_Target, m_BaseCinamecineInfoNow.s_GetActionCinamecineBase);
		if (m_MainController is I_SubModulesCollection GetComplateSign) GetComplateSign.I_CheckSubModuleIsAllCanAction(this);
		else m_isActionProcess = true;
	}

	public override void BreakPoint(bool _isBreak, SubModuleBreakType _BrackType = SubModuleBreakType.BreakAll, System.Type _GetType = null)
	{
		base.BreakPoint(_isBreak, _BrackType, _GetType);
		if (m_VirtualCameraController != null) 
			m_VirtualCameraController.m_Follow = _isBreak ? m_Target : null;
		m_BaseCinamecineInfoNow.s_ControlCinBase.BreakPoint(_isBreak, _BrackType);
	}

	#region Main System Private Functions (**Init Reference**)

	private CinemachineVirtualCamera InitCinemachineVirtual()
	{
		CinemachineVirtualCamera ReturnValue = null;
		if (Camera.main.gameObject != null)
		{
			bool isHaveMainCam = Camera.main.TryGetComponent<CinemachineBrain>(out CinemachineBrain _GetBrain);
			if (!isHaveMainCam) Destroy(Camera.main.gameObject);
			m_InstanceCinemachineBrain = isHaveMainCam ? _GetBrain : Instantiate(m_CinemachineBrain, null);
		}
		if (ReturnValue == null) ReturnValue = FindAnyObjectByType<CinemachineVirtualCamera>();
		return ReturnValue;
	}

	private void InitControlCinemachineBodyType(CinemachineBodyType _ChangeBody)
    {
		#region Apply CinemachineComponentBase By Type
		CinemachineComponentBase GetCinBase = _ChangeBody switch
		{
			CinemachineBodyType.Cinemachine3rdPersonFollow => m_VirtualCameraController.AddCinemachineComponent<Cinemachine3rdPersonFollow>(),
			CinemachineBodyType.CinemachineFramingTransposer => m_VirtualCameraController.AddCinemachineComponent<CinemachineFramingTransposer>(),
			_ => null
		};
		#endregion
		var GetConCinBase = Instantiate(m_GetAllControlCinBases[
		m_GetAllControlCinBases.HFindIndex(x => x.pp_CinemachineBodyType == _ChangeBody)], this.transform);
		GetConCinBase.Initlization(this, m_CamControllerModel.Value, m_VirtualCameraController, m_Target, GetCinBase); 
		m_BaseCinamecineInfoNow = new BaseCinamecineInfo(_ChangeBody, GetCinBase, GetConCinBase);
	}

	private void InitListenStartPos(CamControllerInitInfo _ApplyCamInfo)
	{
		BreakPoint(true);
		switch (_ApplyCamInfo.s_StartType)
		{
			case StartMoveType.MoveLogicType:
				Appinstance.Instance.ms_DelEventManager.DEL_InitCameraControl_StartPos += ReciveStartPos;
				break;
			case StartMoveType.StaticMoveStartType:
				ReciveStartPos(_ApplyCamInfo.s_StartPos, false);
				break;
			case StartMoveType.MoveVirtualType:
				Vector3 CurrCalculatePos = m_VirtualCameraController.transform.position; 
				ReciveStartPos(CurrCalculatePos, false);
				break;
		}
	}

	private void ReciveStartPos(Vector3 _GetMovePos, bool _isRemoveDelData, System.Action _EndCallBack = null)
	{
		if (m_isActionProcess) return;
		m_VirtualCameraController.transform.DOMove(_GetMovePos, 0.65f).SetEase(Ease.InOutCirc).OnComplete(() =>
		{
			m_ComplateCallBack?.Invoke();
			_EndCallBack?.Invoke();
			if (_isRemoveDelData) Appinstance.Instance.ms_DelEventManager.DEL_InitCameraControl_StartPos -= ReciveStartPos;
		});
	}

	#endregion

	#region Main System Private Functions (**Main Update Process**)

	private void UpdateTransform(float _DeltaTime)
    {
		CalculateRatio();
		m_BaseCinamecineInfoNow.s_ControlCinBase.ProcessUpdate(_DeltaTime);
	}

	#endregion

	#region Sub System Functions (**Control Params Reference**)

	private void CalculateRatio()
	{
		Vector2 SpriteSize = m_SPCamInitSettingInstance.sprite.rect.size / m_SPCamInitSettingInstance.sprite.pixelsPerUnit;
		float ApplyRatioX = SpriteSize.x * Screen.height / Screen.width * m_CalculateValue;
		float SettingViewDis = pp_UsedMainCam.orthographic ?
		m_CamControllerModel.Value.s_OrthographicSizeValue : m_CamControllerModel.Value.s_StaticFOVValue;
		if (pp_UsedMainCam.orthographic) m_VirtualCameraController.m_Lens.OrthographicSize = ApplyRatioX > SettingViewDis ? ApplyRatioX : SettingViewDis;
		else m_VirtualCameraController.m_Lens.FieldOfView = ApplyRatioX > SettingViewDis ? ApplyRatioX : SettingViewDis;
	}

	public void SetAngles(Quaternion rotation, bool _isLerp = false)
	=> m_Target.rotation = rotation;

	//���� �߻�ȭ�� ���� �ó׸ӽ��� �� ��ü�� �Ļ��Ͽ� ����� �� �ֵ��� �����Ұ�
	public void ChangeTarget(Transform _GetTarget, float _AddSpeed = default, float _AddConditionValue = default, System.Action _EndCallBack = null)
	{
		SetAngles(Quaternion.LookRotation(m_VirtualCameraController.transform.forward));

        #region �߻�ȭ �� base�� ���� ��(�����Ͱ� �� �Ѱܼ� �� �Ű������� �������� �����Ұ�
        //var GetFT = m_VirtualCameraController.GetCinemachineComponent<CinemachineFramingTransposer>();
        //var OGSpeed = GetFT.m_XDamping;
        //GetFT.m_XDamping = _AddSpeed != default ? _AddSpeed : OGSpeed;
        //GetFT.m_YDamping = _AddSpeed != default ? _AddSpeed : OGSpeed;
        //GetFT.m_ZDamping = _AddSpeed != default ? _AddSpeed : OGSpeed;
        //m_VirtualCameraController.m_Follow = _GetTarget;
        //float GetDIs = Vector3.Distance(pp_UsedMainCam.transform.position, _GetTarget.position);
        //bool isHigher = GetDIs > GetFT.m_CameraDistance;
        //m_LerpTargetComplateCallBack = () =>
        //{
        //	GetDIs = Vector3.Distance(Camera.main.transform.position, _GetTarget.position);
        //	if ((isHigher && GetDIs < GetFT.m_CameraDistance + 0.1f + _AddConditionValue) ||
        //	(!isHigher && GetDIs > GetFT.m_CameraDistance - 0.1f - _AddConditionValue))
        //	{
        //		GetFT.m_XDamping = OGSpeed; GetFT.m_YDamping = OGSpeed; GetFT.m_ZDamping = OGSpeed;
        //		m_LerpTargetComplateCallBack = null;
        //		return _EndCallBack;
        //	}
        //	return null;
        //};
        #endregion
    }

	#endregion

    #region 유니티 이벤트 함수

    protected override void Update()
	{
		base.Update();

		//CalculateRatio();

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


[System.Serializable]
public struct BaseCinamecineInfo
{
	public CinemachineBodyType s_CinemachineBodyType;
	public CinemachineComponentBase s_GetActionCinamecineBase;
	public ControlCinemachineComponentBase s_ControlCinBase;

	public BaseCinamecineInfo(CinemachineBodyType _CinemachineBodyType,
	CinemachineComponentBase _GetActionCinamecineBase, ControlCinemachineComponentBase _ControlCinBase)
	{
		s_CinemachineBodyType = _CinemachineBodyType;
		s_GetActionCinamecineBase = _GetActionCinamecineBase;
		s_ControlCinBase = _ControlCinBase;
	}
}

public class ControlCinemachineComponentBase : SubModuleControllerBase
{
	[Header("Required Value")]
	[SerializeField, ReadOnly] protected RenewalCinemachineCameraController m_RCinCamController;
	[SerializeField, ReadOnly] protected CamControllerInitInfo m_CamControllerModel;
	[SerializeField, ReadOnly, HideIf("@m_Target == null")] protected Transform m_Target;
	[SerializeField, ReadOnly, HideIf("@m_VirtualCameraController == null")] protected CinemachineVirtualCamera m_VirtualCameraController;


	[field: SerializeField] public CinemachineBodyType pp_CinemachineBodyType { get; private set; }

	public override object[] Initlization(params object[] _ParsingParams)
    {
        var PartParams = base.Initlization(_ParsingParams);
		m_CamControllerModel = (CamControllerInitInfo)PartParams[0];
		m_VirtualCameraController = PartParams[1] as CinemachineVirtualCamera;
		m_Target = PartParams[2] as Transform;
		m_RCinCamController = pp_MainController as RenewalCinemachineCameraController;
		return PartParams;
    }

	public virtual void ChangeTarget(Transform _GetTarget, float _AddSpeed = default,
	float _AddConditionValue = default, System.Action _EndCallBack = null) { } //Do Nothing...

}

public class ControlCinemachine3rdPersonFollow : ControlCinemachineComponentBase
{
	[SerializeField, ReadOnly] private Cinemachine3rdPersonFollow m_3rdPersonFollow;

	public override object[] Initlization(params object[] _ParsingParams)
	{
		var SubParams = base.Initlization(_ParsingParams);
		CheckAndApplyByParams<Cinemachine3rdPersonFollow>(SubParams, x => m_3rdPersonFollow = x);

		m_3rdPersonFollow.ShoulderOffset = m_CamControllerModel.spp_OffsetTarget;
		m_3rdPersonFollow.CameraDistance = m_CamControllerModel.spp_MinZoom;
		#region �ٸ� �� ���� ���� ����
		//m_3rdPersonFollow.VerticalArmLength =  //�ش� ���� �ִ��� �������� ������ �� �ֵ��� �����Ұ�
		//ī�޶� Ÿ���� �󸶳� ���������� �����ϴ���.�� ��(ī�޶� ����)�� ������ ������ ���� �� �ֽ��ϴ�. 
		//���� ī�޶� Ÿ���� ���ο� ��ġ�� ������� �� �ɸ��� �뷫���� �ð��Դϴ�. ���� �������� �� �ܴ��� ȿ���� ��Ÿ����, ���� Ŭ���� �� �帴�����ϴ�
		//m_3rdPersonFollow.Damping
		#endregion
		m_isActionProcess = true;
		return null;
	}
}
