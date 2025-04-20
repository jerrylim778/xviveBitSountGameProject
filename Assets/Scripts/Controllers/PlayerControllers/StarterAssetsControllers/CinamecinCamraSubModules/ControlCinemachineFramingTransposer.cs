using System.Collections;
using UnityEngine;
using Cinemachine;
using Sirenix.OdinInspector;
using DG.Tweening;
using ViewType = CameraController.ViewType;

public class ControlCinemachineFramingTransposer : ControlCinemachineComponentBase
{
	//[SerializeField] private float test_DistanceBeforeSomeActions = 0f, test_DistanceEndCall = 0f;
	//[SerializeField] private Vector3 test_StartPosBeforeActions = Vector3.zero, test_EndCallPosAction = Vector3.zero;
	[SerializeField, ReadOnly] private Vector3 m_ApplyAngles = Vector3.zero;
	[SerializeField, ReadOnly] private CinemachineFramingTransposer m_FramingTransposer;

	private readonly float STATICSOFTZONE = 0.3f;

	public override object[] Initlization(params object[] _ParsingParams)
	{
		var SubParams = base.Initlization(_ParsingParams);
		CheckAndApplyByParams<CinemachineFramingTransposer>(SubParams, x => m_FramingTransposer = x);

		//m_FramingTransposer.m_TrackedObjectOffset = m_CamControllerModel.spp_OffsetTarget; //따로 적용할 수 있도록 수정할것 (**리팩요망**)
		m_FramingTransposer.m_ScreenX = m_CamControllerModel.spp_OffsetTarget.x;
		m_FramingTransposer.m_ScreenY = m_CamControllerModel.spp_OffsetTarget.y;
		m_FramingTransposer.m_TrackedObjectOffset = m_CamControllerModel.spp_OffsetApplyTarget;
		var GetFT = m_FramingTransposer;
		var OGSpeed = m_CamControllerModel.spp_FollowSped;
		GetFT.m_XDamping = m_CamControllerModel.s_isFollowSmooth ? OGSpeed : 0f;
		GetFT.m_YDamping = m_CamControllerModel.s_isFollowSmooth ? OGSpeed : 0f;
		GetFT.m_ZDamping = m_CamControllerModel.s_isFollowSmooth ? OGSpeed : 0f;

		GetFT.m_SoftZoneWidth = STATICSOFTZONE;
		GetFT.m_SoftZoneHeight = STATICSOFTZONE;

		//if(m_CamControllerModel.s_isNotUseRot) 
		//m_ApplyAngles = m_CamControllerModel.spp_StaticRot;
		//if(m_CamControllerModel.spp_isApplyCustom)
		//m_FramingTransposer.m_CameraDistance = m_CamControllerModel.spp_MinZoom;

		#region 업데이트는 정적으로만 설정할 수 있도록 수정한다
		//GetDIs = Vector3.Distance(Camera.main.transform.position, _GetTarget.position);
		//if ((isHigher && GetDIs < GetFT.m_CameraDistance + 0.1f + _AddConditionValue) ||
		//(!isHigher && GetDIs > GetFT.m_CameraDistance - 0.1f - _AddConditionValue))
		//{
		//	GetFT.m_XDamping = OGSpeed; GetFT.m_YDamping = OGSpeed; GetFT.m_ZDamping = OGSpeed;
		//	m_LerpTargetComplateCallBack = null;
		//	return _EndCallBack;
		//}
		#endregion

		m_isActionProcess = true;
		return null;
	}

    public override void BreakPoint(bool _isBreak, SubModuleBreakType _BrackType = SubModuleBreakType.BreakAll, System.Type _GetType = null)
    {
		base.BreakPoint(_isBreak, _BrackType, _GetType);

		#region Brfore Project Reference (Dragon Bow Games)
		//var GetMCon = FindParentsObjTypeByTemp<DragonBowController>();

		//switch (_BrackType)
		//{
		//	case SubModuleBreakType.BreakAll:
		//		m_ApplyAngles = test_StartPosBeforeActions;
		//		m_FramingTransposer.m_CameraDistance = test_DistanceBeforeSomeActions;
		//		m_VirtualCameraController.transform.rotation = Quaternion.Euler(m_ApplyAngles);
		//		StartCoroutine(CO_SubBreakPointToPlay(() =>
		//		GetMCon.CheckBowSubModuleIsAllCanAction(this, SubModuleBreakType.PartBreak)));
		//		break;
		//	case SubModuleBreakType.PartBreak:
		//		GetMCon.CheckBowSubModuleIsAllCanAction(this, SubModuleBreakType.OnlyBreakElems);
		//		break;
		//	case SubModuleBreakType.OnlyBreakElems:
		//		m_ApplyAngles = m_CamControllerModel.spp_StaticRot;
		//		StartCoroutine(CO_SubBreakPointToPlay(null));
		//		DOTween.To(() => m_FramingTransposer.m_CameraDistance, x =>
		//		m_FramingTransposer.m_CameraDistance = x, m_CamControllerModel.spp_MinZoom, 1f).SetEase(Ease.InSine).OnComplete(() =>
		//		GetMCon.CheckBowSubModuleIsAllCanAction(this, SubModuleBreakType.BreakAfterEvent));
		//		break;
		//	case SubModuleBreakType.BreakAfterEvent:
		//		base.BreakPoint(_isBreak, _BrackType, _GetType);
		//		break;	
		//}
		#endregion
	}

	//상위객체에 따로 존재해야함 (**리팩 요망**)
	public void ChangeCamModel(float _Distance, Vector2 _WidOffset, Vector3 _StaticRot, Vector3 _CamOffset)
    {
		m_isActionProcess = false;

		m_ApplyAngles = _StaticRot;
		m_FramingTransposer.m_CameraDistance = _Distance;
		m_FramingTransposer.m_ScreenX = _WidOffset.x;
		m_FramingTransposer.m_ScreenY = _WidOffset.y;
		m_FramingTransposer.m_TrackedObjectOffset = _CamOffset;

		m_VirtualCameraController.transform.rotation = Quaternion.Euler(m_ApplyAngles);

		m_isActionProcess = true;
	}

	IEnumerator CO_SubBreakPointToPlay(System.Action _EndCallBack)
	{
		yield return new WaitUntil(() => Quaternion.Angle(m_MainController.transform.rotation, Quaternion.Euler(m_ApplyAngles)) <= 0.1f);
		_EndCallBack?.Invoke();
	}

	public override void ProcessUpdate(float _DeltaTime)
	{
		if (!m_isActionProcess) return;
		if (m_CamControllerModel.m_ViewType != ViewType.POV && m_Target != null)
		{
			var TargetRot = m_CamControllerModel.s_isNotUseRot ?
			Quaternion.Euler(m_ApplyAngles) :
			Quaternion.LookRotation(m_Target.transform.forward - Vector3.up * 0.3f);
			m_VirtualCameraController.transform.rotation = Quaternion.Slerp(
			m_VirtualCameraController.transform.rotation, TargetRot, 4f * _DeltaTime);
		}
	}
}