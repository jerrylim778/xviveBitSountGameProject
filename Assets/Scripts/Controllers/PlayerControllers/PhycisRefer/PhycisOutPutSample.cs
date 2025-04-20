using UnityEngine;
using Commons.Helpers;

public class PhycisOutPutSample : PhycisOutPutBase
{
    private readonly float s_MoveSpeed = 5f;
    private AnimationController m_GetMainAnimationController;
    private Rigidbody m_GetRB;
    private float s_InstanceCurrTime = 0f, s_InstanceMaxTime = 3f;
    private ParticleSystem m_StepDcorParticle;

    public override void Initlization(NewCharacterController _getMainController)
    {
        m_isActionPhycisBase = false;
        base.SubInitlization(_getMainController.gameObject);
        m_NewCharacterController = _getMainController;
        m_InputInfo = _getMainController.I_GetSubModule<BInputController>().mspp_InputValues;
        m_GetMainAnimationController = _getMainController.I_GetSubModule<AnimationController>();
        m_AnimValuesInfo = m_GetMainAnimationController.m_AnimValuesInfo;
        m_RuntimeAnimNow = m_GetMainAnimationController.pp_MainAnimator;
        m_GetRB = m_NewCharacterController.GetComponent<Rigidbody>();
        m_GetMainAnimationController.transform.localPosition = Vector3.zero;
        m_GetMainAnimationController.transform.localRotation = Quaternion.identity;
        this.transform.localPosition = Vector3.zero;
        this.transform.localRotation = Quaternion.identity;
        m_StepDcorParticle = Instantiate(Appinstance.Instance.ms_DataManager.test_SupportCharacterSubInfos.HFind(x =>
        x.name == "StepParticle").GetComponent<ParticleSystem>(), this.transform.position, this.transform.rotation, this.transform);
    }

    public override void ActionPhycis() => m_isActionPhycisBase = true;

    #region 유니티 이벤트 함수관련 상속들

    #region Animation Force 사용시 주석 해제할것
    //private void MoveActionValue(Vector3 _deltaPos, Quaternion _deltaRot)
    //{
    //    FixedTime += Time.deltaTime;
    //    m_FixedUpdatePosition += _deltaPos;
    //    m_FixedUpdateRotation *= _deltaRot;
    //}

    //protected override void ProcessAnimationMove()
    //{
    //    base.ProcessAnimationMove();
    //    CalculationPhycisList[1].UpdateAnimationMove(m_GetMainAnimationController);
    //    MoveActionValue(m_RuntimeAnimNow.deltaPosition, m_RuntimeAnimNow.deltaRotation);
    //}
    #endregion

    //전부 직접 로직을 넣되
    //InputValue와 RigidBody로 움직임을 줄 수 있도록 수정한다
    private bool m_isRotating = false;
    private Vector3 m_WorldMoveDirection;
    private Quaternion m_TargetRot;

    protected override void ProcessFixedUpdate()
    {
        base.ProcessFixedUpdate();

        #region 설정값이 먹히질 않음 (삭제 요망)
        //Vector3 camForward = m_InputInfo.s_LookCamForword;
        //Vector3 camRight = m_InputInfo.s_LookCamRight;
        //camForward.y = 0f;
        //camRight.y = 0f;
        //camForward.Normalize();
        //camRight.Normalize();
        //Vector3 worldMoveDirection = camForward * m_InputInfo.s_MoveValue.y + camRight * m_InputInfo.s_MoveValue.x;
        #endregion

        Vector3 worldMoveDirection = new Vector3(m_InputInfo.s_MoveValue.x, 0f, m_InputInfo.s_MoveValue.y);

        if (m_InputInfo.s_MoveValue.magnitude > 0.1f)
        {
            m_TargetRot = Quaternion.LookRotation(worldMoveDirection);
            var GetAngles = Quaternion.Angle(m_NewCharacterController.transform.rotation, m_TargetRot);
            if (Mathf.Abs(GetAngles - 180) < 20f) m_isRotating = true;
            if (m_isRotating) m_AnimValuesInfo.s_MoveValue = Vector2.zero;
            else
            {
                m_AnimValuesInfo.s_MoveValue = m_InputInfo.s_MoveValue.magnitude == 0 ?
                Vector2.zero : m_InputInfo.s_MoveValue;
                m_NewCharacterController.transform.position += worldMoveDirection * 3f * Time.deltaTime;
                m_NewCharacterController.transform.rotation = m_TargetRot;
                if (s_InstanceCurrTime < s_InstanceMaxTime)
                {
                    s_InstanceCurrTime += Time.unscaledDeltaTime * 10f;
                    return;
                }
                else
                {
                    s_InstanceCurrTime = 0f;
                    m_StepDcorParticle.Play();
                }
            }
            //회전의 정도에 따라 나눠야 함
        }
        else m_AnimValuesInfo.s_MoveValue = Vector2.zero; 
    }

    protected override void ProcessLateUpdate()
    {
        base.ProcessLateUpdate();
        

        if (m_isRotating)
        {
            m_NewCharacterController.transform.rotation = Quaternion.Slerp(
            m_NewCharacterController.transform.rotation, m_TargetRot, 22.5f * Time.deltaTime);

            var GetAngles = Quaternion.Angle(m_NewCharacterController.transform.rotation, m_TargetRot);
            if(GetAngles < 10f)
            {
                transform.rotation = m_TargetRot;
                m_isRotating = false;
            }
        }
    }
    #endregion
}
