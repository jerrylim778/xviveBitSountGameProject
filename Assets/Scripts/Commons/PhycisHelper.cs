using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Commons.Helpers;


public abstract class PhycisHelper
{
    public bool m_isMustCheckSubInit = false;
    protected PhycisOutPutBase m_GetPhycisOutPut;
    protected Rigidbody GetRB;
    [SerializeField] protected AnimValuesInfo m_AnimValuesInfo;
    [SerializeField] protected InputValueInfo m_InputInfo;
    

    public PhycisHelper(PhycisOutPutBase _getPhycisBase) 
    {
        m_GetPhycisOutPut = _getPhycisBase;
        m_InputInfo = m_GetPhycisOutPut.m_InputInfo;
        m_AnimValuesInfo = m_GetPhycisOutPut.m_AnimValuesInfo;
        //GetRB = m_GetPhycisOutPut.m_CharacterController.pp_MainRB;
        GetRB = m_GetPhycisOutPut.m_NewCharacterController.GetComponent<Rigidbody>();
    }

    public virtual void SubInitlization() { } //Do Nothing

    public virtual void OnDestroyByPhycisHelper() { } //Do Nothing

    public virtual void UpdatePhycis() { } //DoNothing

    public virtual void FixedUpdatePycis() { } //DoNothing

    public virtual void UpdateAnimationMove(AnimationController _getController = null) { } //DoNothing

    public virtual void CollisionEnterEventByPhycisHelper(GameObject _GetObj) { } //DoNothing
}

#region Direction (전방에 대한 로직)

public class Direction_Phycis : PhycisHelper
{
    private bool isCheckAddGroundPhycis = false;

    public Direction_Phycis(PhycisOutPutBase _getPhycisBase) : base(_getPhycisBase)
    {
        m_GetPhycisOutPut = _getPhycisBase;
        m_isMustCheckSubInit = true;
    }

    public override void SubInitlization()
    {
        isCheckAddGroundPhycis = m_GetPhycisOutPut.CalculationPhycisList.ToList().Exists(x => x is Ground_Phycis);
    }

    public override void UpdatePhycis()
    {
        base.UpdatePhycis();
        m_AnimValuesInfo.s_MoveValue = MoveDirection();
        m_AnimValuesInfo.s_isStrafing = m_GetPhycisOutPut.m_MoveMode == PhycisOutPutBase.MoveMode.Strafe;
    }

    public override void FixedUpdatePycis()
    {
        SetVelocityY();
        MoveFixed(m_GetPhycisOutPut.m_FixedUpdatePosition);
        m_GetPhycisOutPut.FixedTime = 0f;
        m_GetPhycisOutPut.m_FixedUpdatePosition = Vector3.zero;
        base.FixedUpdatePycis();
    }

    #region Input => Animaton

    private Vector2 MoveDirection()
    {
        //��ǥ��ġ������ ������Ϳ� ���� ���� ��� (�̰� ��� Ŭ������ �ش��)
        Vector3 ReturnFinalVec = Vector3.zero;
        switch (m_GetPhycisOutPut.m_MoveMode)
        {
            case PhycisOutPutBase.MoveMode.Directional:
                m_GetPhycisOutPut.moveDirection = Vector3.SmoothDamp(m_GetPhycisOutPut.moveDirection, new Vector3(0f, 0f, m_InputInfo.s_MoveValue.magnitude),
                ref m_GetPhycisOutPut.moveDirectionVelocity, m_GetPhycisOutPut.SmoothAccelerationTime);
                m_GetPhycisOutPut.moveDirection = Vector3.MoveTowards(m_GetPhycisOutPut.moveDirection, new Vector3(0f, 0f, m_InputInfo.s_MoveValue.magnitude),
                Time.deltaTime * m_GetPhycisOutPut.LinearAccelerationSpeed);
                ReturnFinalVec = m_GetPhycisOutPut.moveDirection * m_GetPhycisOutPut.forwardMlp; //된다면 이전것으로 변경할것 
                return new Vector2(ReturnFinalVec.x, ReturnFinalVec.z);
            case PhycisOutPutBase.MoveMode.Strafe:
                m_GetPhycisOutPut.moveDirection = Vector3.SmoothDamp(m_GetPhycisOutPut.moveDirection, new Vector3(m_InputInfo.s_MoveValue.x, 0f, m_InputInfo.s_MoveValue.y), 
                ref m_GetPhycisOutPut.moveDirectionVelocity, m_GetPhycisOutPut.SmoothAccelerationTime);
                m_GetPhycisOutPut.moveDirection = Vector3.MoveTowards(m_GetPhycisOutPut.moveDirection, new Vector3(m_InputInfo.s_MoveValue.x, 0f, m_InputInfo.s_MoveValue.y), 
                Time.deltaTime * m_GetPhycisOutPut.LinearAccelerationSpeed);
                //ReturnFinalVec = m_GetPhycisOutPut.m_CharacterController.transform.InverseTransformDirection(m_GetPhycisOutPut.moveDirection);
                ReturnFinalVec = m_GetPhycisOutPut.m_NewCharacterController.transform.InverseTransformDirection(m_GetPhycisOutPut.moveDirection);
                return new Vector2(ReturnFinalVec.x, ReturnFinalVec.z);
        }
        return Vector2.zero;
    }
    #endregion

    #region Animaton => Character
    private void MoveFixed(Vector3 _deltaPosition)
    {
        Vector3 velocity = m_GetPhycisOutPut.FixedTime > 0f ? _deltaPosition / m_GetPhycisOutPut.FixedTime : Vector3.zero;

        if (isCheckAddGroundPhycis)
        {
            velocity += Helper.ExtractHorizontal(m_GetPhycisOutPut.platformVelocity, m_GetPhycisOutPut.Gravity, 1f);

            if (m_AnimValuesInfo.s_isGround)
            {
                //���� ���˿� ���� ȸ�� �ӵ�
                if (m_GetPhycisOutPut.VelocityToGroundTangentWeight > 0f)
                {
                    Quaternion rotation = Quaternion.FromToRotation(GetRB.transform.up, m_GetPhycisOutPut.WhenGroundUpNormal);
                    velocity = Quaternion.Lerp
                    (Quaternion.identity, rotation, m_GetPhycisOutPut.VelocityToGroundTangentWeight) * velocity;
                }
            }
            else
            {
                #region ���� or �߷��ۿ� ������ ���� �߿� *******���߿� �������� �߷¹������� �÷��̾��� �ӷ¹����� ��Ÿ���� ����
                Vector3 ReturnVec3 = new Vector3(m_InputInfo.s_MoveValue.x, 0f, m_InputInfo.s_MoveValue.y);
                Vector3 AirMove = Helper.ExtractHorizontal(ReturnVec3 * m_GetPhycisOutPut.airSpeed, Physics.gravity, 1f);
                velocity = Vector3.Lerp(GetRB.linearVelocity, AirMove, Time.deltaTime * m_GetPhycisOutPut.airControl);
                #endregion
            }

            //���������� �����ߴ� �ð��� ���� ���� �ִ����� ��
            if (m_AnimValuesInfo.s_isGround && Time.time > m_GetPhycisOutPut.jumpEndTime)
                GetRB.linearVelocity = GetRB.linearVelocity - GetRB.transform.up * m_GetPhycisOutPut.stickyForce * Time.deltaTime;

            //���� ����� ũ�� ���� ����� ũ�⸦ �ٽ� ����ϱ� ���� ����
            Vector3 verticalVelocity = Helper.ExtractVertical(GetRB.linearVelocity , Physics.gravity, 1f);
            Vector3 horizontalVelocity = Helper.ExtractHorizontal(velocity, Physics.gravity, 1f);

            if (m_AnimValuesInfo.s_isGround)
            {
                //���� �ӵ� üũ(���� �޷��� �߽ø� 3f�� �̻��� �ȵǵ���)
                if (Vector3.Dot(verticalVelocity, Physics.gravity) < 0f)
                    verticalVelocity = Vector3.ClampMagnitude(verticalVelocity, m_GetPhycisOutPut.MaxVerticalVelocityOnGround);
            }

            GetRB.linearVelocity = horizontalVelocity + verticalVelocity;
        }
        else GetRB.linearVelocity = velocity;

        m_GetPhycisOutPut.forwardMlp = 1f;

        if (m_GetPhycisOutPut != null && m_GetPhycisOutPut.m_isOutPutDebugRay)
            Debug.DrawRay(GetRB.transform.position, GetRB.linearVelocity, Color.red, 30f);
    }
    #endregion

    #region private Function
    private void SetVelocityY()
    {
        m_GetPhycisOutPut.Gravity = Physics.gravity;
        m_GetPhycisOutPut.VerticalVelocity = (m_GetPhycisOutPut.Gravity == Vector3.up ?
        Vector3.up * GetRB.linearVelocity.y * 1f : Vector3.Project(GetRB.linearVelocity, m_GetPhycisOutPut.Gravity) * 1f);
        m_GetPhycisOutPut.VelocityY = m_GetPhycisOutPut.VerticalVelocity.magnitude;
        if (Vector3.Dot(m_GetPhycisOutPut.VerticalVelocity, m_GetPhycisOutPut.Gravity) > 0f)
            m_GetPhycisOutPut.VelocityY = - m_GetPhycisOutPut.VelocityY;
    }
    #endregion

}

#endregion

#region ������Ʈ ȸ���� ���� ��� Rotation

[System.Serializable]
public class Rotate_Phycis : PhycisHelper
{
    public Rotate_Phycis(PhycisOutPutBase _getPhycisBase) : base(_getPhycisBase)
    {
        m_GetPhycisOutPut = _getPhycisBase;
    }

    public override void FixedUpdatePycis()
    {
        base.FixedUpdatePycis();
        GetRB.MoveRotation(GetRB.gameObject.transform.rotation * m_GetPhycisOutPut.m_FixedUpdateRotation.normalized);
        m_GetPhycisOutPut.m_FixedUpdateRotation = Quaternion.identity;
        Rotate();
    }

    public override void UpdateAnimationMove(AnimationController _getController = null)
    {
        base.UpdateAnimationMove(_getController);
        Vector3 ForwordValue = _getController.pp_MainAnimator.deltaRotation * Vector3.forward;
        m_GetPhycisOutPut.deltaAngle += Mathf.Atan2(ForwordValue.x, ForwordValue.z) * Mathf.Rad2Deg;
    }

    public void Rotate()
    {
        float Angle = GetAngleFromForward(GetForwardDirection());

        if (m_InputInfo.s_MoveValue == Vector2.zero)
            Angle *= (1.01f - (Mathf.Abs(Angle) / 180f)) * 1f;
        //Debug.LogWarningFormat("회전 라디안 값 [angle]", angle);
        GetRB.MoveRotation(Quaternion.AngleAxis(Angle * Time.deltaTime * 30f, m_GetPhycisOutPut.m_NewCharacterController.transform.up) * GetRB.rotation);
    }

    public Vector3 GetForwardDirection()
    {
        bool isMoving = m_InputInfo.s_MoveValue != Vector2.zero;
        switch (m_GetPhycisOutPut.m_MoveMode)
        {
            case PhycisOutPutBase.MoveMode.Directional:
                Vector3 ReturnVec3 = new Vector3(m_InputInfo.s_MoveValue.x, 0f, m_InputInfo.s_MoveValue.y);
                if (isMoving) return ReturnVec3;
                return m_GetPhycisOutPut.m_LookInCameraDirection ? m_InputInfo.s_LookPos - GetRB.position : GetRB.transform.forward;
            case PhycisOutPutBase.MoveMode.Strafe:
                if (isMoving) return m_InputInfo.s_LookPos - GetRB.position;
                return m_GetPhycisOutPut.m_LookInCameraDirection ? m_InputInfo.s_LookPos - GetRB.position : GetRB.transform.forward;
        }
        return Vector3.zero;
    }

    public float GetAngleFromForward(Vector3 worldDirection)
    {
        Vector3 local = GetRB.transform.InverseTransformDirection(worldDirection);
        float angle = Mathf.Atan2(local.x, local.z) * Mathf.Rad2Deg;

        return angle;
    }

    public float GetAngleByRotate()
    {
        float angle = 0f;

        angle = -GetAngleFromForward(m_GetPhycisOutPut.lastForward) - m_GetPhycisOutPut.deltaAngle;
        m_GetPhycisOutPut.deltaAngle = 0f;
        m_GetPhycisOutPut.lastForward = GetRB.transform.forward;
        angle *= 0.2f * 0.01f;
        angle = Mathf.Clamp(angle / Time.deltaTime, -1f, 1f);
        return angle;
    }
}

#endregion

#region ������Ʈ �ٴڿ� �ִ����� ���� üũ ��� Ground

[System.Serializable]
public class Ground_Phycis : PhycisHelper
{
    public Ground_Phycis(PhycisOutPutBase _getPhycisBase) : base(_getPhycisBase)
    {
        m_GetPhycisOutPut = _getPhycisBase;
    }

    public override void UpdatePhycis()
    {
        base.UpdatePhycis();
        m_AnimValuesInfo.s_isGround = m_GetPhycisOutPut.isGround;
        m_AnimValuesInfo.s_Yvalocity =
        Mathf.Lerp(m_AnimValuesInfo.s_Yvalocity, m_GetPhycisOutPut.VelocityY, Time.deltaTime * 10f);
    }

    public override void FixedUpdatePycis()
    {
        base.FixedUpdatePycis();
        GroundCheck();
    }

    #region private function

    private void GroundCheck()
    {
        Vector3 platformVelocityTarget = Vector3.zero;
        m_GetPhycisOutPut.platformAngularVelocity = Vector3.zero;
        float stickyForceTarget = 0f;

        m_GetPhycisOutPut.hit = GetSpherecastHit();

        m_GetPhycisOutPut.WhenGroundUpNormal = GetRB.transform.up;
        Vector3 GetTargetArrow = Vector3.Project(GetRB.position - m_GetPhycisOutPut.hit.point, GetRB.transform.up);
        m_GetPhycisOutPut.groundDistance = GetTargetArrow.magnitude;

        bool findGround = Time.time > m_GetPhycisOutPut.jumpEndTime && m_GetPhycisOutPut.VelocityY <
        m_GetPhycisOutPut.jumpPower * 0.5f;

        if (findGround)
        {
            bool g = m_GetPhycisOutPut.isGround;
            m_GetPhycisOutPut.isGround = false;

            float groundHeight = !g ? m_GetPhycisOutPut.AirborneThreshold * 0.5f : m_GetPhycisOutPut.AirborneThreshold;

            Vector3 horizontalVelocity = Helper.ExtractHorizontal(GetRB.linearVelocity, Physics.gravity, 1f);

            float velocityF = horizontalVelocity.magnitude;

            if (m_GetPhycisOutPut.groundDistance < groundHeight)
            {
                stickyForceTarget = m_GetPhycisOutPut.groundStickyEffect * velocityF * groundHeight;

                if (m_GetPhycisOutPut.hit.rigidbody != null)
                {
                    platformVelocityTarget = m_GetPhycisOutPut.hit.rigidbody.GetPointVelocity(m_GetPhycisOutPut.hit.point);
                    m_GetPhycisOutPut.platformAngularVelocity =
                        Vector3.Project(m_GetPhycisOutPut.hit.rigidbody.angularVelocity, GetRB.transform.up);
                }

                m_GetPhycisOutPut.isGround = true;
            }
        }

        m_GetPhycisOutPut.platformVelocity =
            Vector3.Lerp(m_GetPhycisOutPut.platformVelocity, platformVelocityTarget,
            Time.deltaTime * m_GetPhycisOutPut.platformFriction);

        m_GetPhycisOutPut.stickyForce = stickyForceTarget;
    }

    private RaycastHit GetSpherecastHit()
    {
        Vector3 up = GetRB.transform.up;
        Ray ray = new Ray(GetRB.position + up * m_GetPhycisOutPut.AirborneThreshold, -up);
        RaycastHit hit = new RaycastHit();
        hit.point = GetRB.transform.position - GetRB.transform.transform.up * m_GetPhycisOutPut.AirborneThreshold;
        hit.normal = GetRB.transform.up;
        Physics.SphereCast(ray, m_GetPhycisOutPut.SpherecastRadius, out hit,
            m_GetPhycisOutPut.AirborneThreshold * 2f, m_GetPhycisOutPut.GroundLayers);
        return hit;
    }
    #endregion
}

#endregion

#region ������Ʈ ȸ�ǿ� ���� ��ü �̵� (��κ� Target or AI���� ���) AIDirection


//1.���� Mode Directional������ ��밡���ϴ� Strafe�� �ٸ�������� �����ؾ��ϰų�
//Strafe�� ����ҽ� Camera�� �ִ��� ���ο� ���� ����� �� �������� �����ؾ��Ѵ�
//2.���������� ��ǲ�� ���� ��Ʈ���� �ؾ��ϱ� ������ ��ǲ���� �ٲ㼭 Diction�� ������Ѿ� �Ѵ�
//�ٽø��� �ִϸ��̼ǿ� ���� ���� �������°� �ش� �������� �ʿ�ġ �ʰ� ������ ���� ������ Ȱ��������
//�� ������ �����ϱ� ���� ��Ÿ ������ �ش� ������ �����Ͽ� Target�� ���� AI �̵��� ������ �� �ֵ��� �ϴ°��� �� 1��ǥ��

[System.Serializable]
public class AIMove_Phycis : PhycisHelper
{
    //private float s_TargetObstacleSize = 0.0f;
    //private Vector3 s_TargetObstacleEndPoint = Vector3.zero;

    public AIMove_Phycis(PhycisOutPutBase _getPhycisBase) : base(_getPhycisBase)
    {
        m_isMustCheckSubInit = true;
        m_GetPhycisOutPut = _getPhycisBase;
    }

    public override void SubInitlization()
    {
        if (!m_GetPhycisOutPut.CalculationPhycisList.
        ToList().Exists(x => x is Direction_Phycis || x is Rotate_Phycis || x is Ground_Phycis))
        {
            Debug.LogErrorFormat("{0} �ش� PhycisHelper�� ����Ϸ��� �ʼ������� " +
            "�̵� ȸ�� �ٴ�üũ�� �ʿ��մϴ�..!!", nameof(AIMove_Phycis));
            return;
        }
    }

    #region Main System Public Functions

    public override void CollisionEnterEventByPhycisHelper(GameObject _GetObj)
    {
        ObstacleOtherWay(_GetObj);
    }

    public override void FixedUpdatePycis()
    {
        base.FixedUpdatePycis();
        #region ���� ��� + �Ÿ��� ���� ���� ���� ���ϴ� ����ʹ� �ٸ� ���� ���޵� *(���� ����)
        //if (m_GetPhycisOutPut.m_InputInfo.s_BreakPoint)
        //{
        //    #region ������ �Ÿ�(�Ÿ��� ������ �Ÿ��������� �ٸ������� ��� ����)
        //    //float GetDIs = s_TargetObstacleSize < GetRB.position.magnitude ?
        //    //Mathf.Abs(s_TargetObstacleSize - GetRB.position.magnitude) :
        //    //(s_TargetObstacleEndPoint - GetRB.position).magnitude;
        //    //float GetDIs = Mathf.Abs(s_TargetObstacleSize - GetRB.position.magnitude);
        //    //Debug.LogFormat("{0}:::{1}:::{2}", GetDIs, GetRB.position, s_TargetObstacleEndPoint);
        //    //m_GetPhycisOutPut.m_InputInfo.s_BreakPoint = GetDIs > 0.3f;
        //    #endregion
        //    Transform GetRDownRayTr = m_GetPhycisOutPut.transform.GetChild(0);
        //    Debug.DrawRay(GetRDownRayTr.position, GetRDownRayTr.right * 2f, Color.red);
        //    RaycastHit RayHit;
        //    if(Physics.Raycast(GetRDownRayTr.position, GetRDownRayTr.right * 2f, out RayHit, 2f))
        //        if(RayHit.collider.gameObject != null) s_isCheckSubAction = true;
                
        //    if (s_isCheckSubAction)
        //    if(RayHit.collider == null)
        //    {
        //        m_GetPhycisOutPut.m_InputInfo.s_BreakPoint = false;
        //        m_GetPhycisOutPut.m_InputInfo.s_MoveValue = Vector2.zero;
        //    }
        //}
        #endregion
    }

    #endregion

    #region ȸ�� �⵿ ����

    #region �Ϲ� ȸ�� �⵿

    private void ObstacleOtherWay(GameObject _GetObstacleObj, bool _isRight = false)
    {
        #region ��ֹ� ȸ�ǿ� ���� ����
        //�ǹ��� �߽����� �����Ͽ� ���� �Ž���ü ũ�⸦ �˾Ƴ� ��
        //�÷��̾ ���� ������� �鿡 ���̸�
        //�ش� �������� �߽����� �¿��� ���������� �̵��Ѵ�
        //�̵��� �ٽ� ��ǥ��带 �����ϸ� �ش� �������� ���� �̵��ϰ� �ȴ�
        #endregion

        #region �¿� �������� Ȯ���ؼ� �ش� ��ġ���� �̵��ϱ� ���� �����Ѱ�(�浹�� ������ ���� ã�� ���� ���)
        //�ݶ��̴��� ���� �浹���� ���� Ȯ���ϰ� �Ѱ�����
        //Collision GetCollider = _GetObstacleObj.GetComponent<Collision>();
        //int touchedFaceIndex = -1;
        //if (GetCollider != null)
        //{
        //    GetCollider.contacts.ToList().ForEach(x => 
        //    {
        //        float GetDisByContent = Vector3.Distance(this.transform.position.normalized, x.normal);
        //        if (GetDisByContent < 0.1f)
        //            touchedFaceIndex = System.Array.FindIndex(GetCollider.contacts, y => y.normal == x.normal);
        //    });
        //}
        #endregion

        m_GetPhycisOutPut.m_InputInfo.s_BreakPoint = false;
        MeshFilter GetMeshFilter = _GetObstacleObj.GetComponent<MeshFilter>();
        Vector3 GetLocalScale = _GetObstacleObj.transform.localScale;
        if (GetMeshFilter != null)
        {
            Mesh GetMesh = GetMeshFilter.sharedMesh;
            float MaxScale = Mathf.Max(Mathf.Max(GetLocalScale.x, GetLocalScale.y), GetLocalScale.z);
            //float ObstacleSize = GetMesh.bounds.size.magnitude * MaxScale;//���� ����
            //�ϴ� �߰������� ������ �ѵڿ� �̵����� ��������
            //��Ȯ���� �浹������ Ȯ���Ͽ� �ش� ������ ������ ���� �ش� ������ �޶Ǵ� �������� �������� Ž���� �ڿ�
            //�� ������ ���� ���� �����ؾ� ��Ȯ�� �׸�ŭ �̵��ϴ� ������ ���� �� �ִ�
            float ObstacleSize = (GetMesh.bounds.size.magnitude * MaxScale) * 1.3f; 

            Vector3 GetTargetDis = new Vector3(m_GetPhycisOutPut.m_InputInfo.s_MoveValue.x, 0f, m_GetPhycisOutPut.m_InputInfo.s_MoveValue.y);
            Vector3 GetOtherWayTarget =
            _isRight ? -Vector3.Cross(GetTargetDis, Vector3.up) * ObstacleSize : Vector3.Cross(GetTargetDis, Vector3.up) * ObstacleSize;
            //���� ���� ������ ��ֹ��� �ִٸ� ȸ���Ҷ� �¿�� ������ ����� ����� �ٲ���
            #region �¿� �������� Ȯ���ؼ� �ش� ��ġ���� �̵��ϱ� ���� �����Ѱ�
            //GetMesh.GetVertices(faceVertices, touchedFaceIndex * 3); // ���÷� 3���� �������� �ִٰ� ����
            // �¿� �������� �ε���
            //int leftVertexIndex = (touchedFaceIndex * 3 + 1) % GetMesh.vertexCount;
            //int rightVertexIndex = (touchedFaceIndex * 3 + 2) % GetMesh.vertexCount;
            // �¿� �������� ��ġ
            //Vector3 leftVertex = Vector3.Scale(GetMesh.vertices[leftVertexIndex], GetLocalScale);
            //Vector3 rightVertex = Vector3.Scale(GetMesh.vertices[rightVertexIndex], GetLocalScale);
            #endregion
            m_GetPhycisOutPut.m_InputInfo.s_MoveValue = new Vector2(GetOtherWayTarget.x, GetOtherWayTarget.z).normalized;
            m_GetPhycisOutPut.m_InputInfo.s_BreakPoint = true;
            float GetMaxSize = (GetMesh.bounds.size.magnitude + MaxScale);
            float Time = ObstacleSize / GetMaxSize;
            #region ����� ��� ����
            if (m_GetPhycisOutPut != null && m_GetPhycisOutPut.m_isOutPutDebugLog)
            {
                Debug.LogWarningFormat("�����Ͽ� ���� �޽��� ������ {0}", ObstacleSize);
                Debug.LogWarningFormat("�Ÿ� / �ӷ� = [�ð� : {0}] �ӷ� �� ũ�� {1} �Ÿ� �Ž������� {2}", Time, GetMaxSize, ObstacleSize);
            }
            #endregion
            #region �ڷ�ƾ ���(�׽�Ʈ �뵵 �Ŀ� ���� ���
            //������ �Ÿ�(Update���� ��ǥ���� ���������� üũ)�� ���� ��� ���Ŀ� ������ �� �ֵ��� �ؾ��Ѵ�//*(�ڷ�ƾ ����� ���� ���� ���)
            m_GetPhycisOutPut.ProcessUseCoroutine(true, CO_GetWaitTimeByDis(Time));
            #endregion
        }
    }
    #region �׽�Ʈ ���� ���
    IEnumerator CO_GetWaitTimeByDis(float _WaitTime)
    {
        yield return new WaitForSeconds(_WaitTime);
        m_GetPhycisOutPut.m_InputInfo.s_BreakPoint = false;
    }
    #endregion
    #endregion

    #region Ư�� ȸ�� �⵿
    //������ ������Ʈ�� ���� �� ĳ������ ���̸� �߻��Ͽ� �¿��� ��Ʈ���� �����Ѵ�
    //private void 

    #endregion

    #endregion
}

#endregion
