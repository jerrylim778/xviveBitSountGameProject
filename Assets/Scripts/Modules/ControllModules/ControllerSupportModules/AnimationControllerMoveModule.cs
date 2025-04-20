using UnityEngine;
using Sirenix.OdinInspector;
using Commons;
using Commons.Helpers;


//이후에 많은 역활이 있을시 SubModuleController<클래스>로 격상하여 적용하되
//나머지는 아래와같이 단순 이넵트를 사용할때 말고는 일체 사용을 금지함
[RequireComponent(typeof(Animator))]
public class AnimationControllerMoveModule : MonoBehaviour
{
    [Header("Required Values")]
    [SerializeField, ReadOnly] private Animator m_CharacterAnimator;
    [SerializeField, ReadOnly] private NewCharacterController m_GetMainController;
    [SerializeField, ReadOnly] private AnimationController m_AnimationController;
    [SerializeField, ReadOnly] private Collider m_GetMainCollider;
    [SerializeField, ReadOnly] private Rigidbody m_GetMainRB;
    [Tooltip("Control Values")]
    private bool m_ModuleProcessAction = false;

    public System.Tuple<Animator, Collider> Initlization<T>(NewCharacterController _GetMainController, AnimationController _AnimationController = null) where T : Collider
    {
        m_ModuleProcessAction = false;
        m_GetMainController = _GetMainController;
        m_AnimationController = _AnimationController;
        m_CharacterAnimator = this.gameObject.CheckComnectComponent<Animator>();
        m_GetMainCollider = this.gameObject.CheckComnectComponent<T>();
        m_ModuleProcessAction = true;
        return new(m_CharacterAnimator, m_GetMainCollider);
    }

    public void InitRecycle(CalculateInfo _getCalculInfo)
    {
        if (m_GetMainRB != null) m_GetMainRB.linearVelocity = Vector3.zero;
        if(m_GetMainRB != null &&
        _getCalculInfo.spp_AddSMCWithAnimController == null || _getCalculInfo.spp_PhycisOutPutBase == null)
        {
            Destroy(m_GetMainRB);
            return;
        }
        m_GetMainRB = this.gameObject.CheckComnectComponent<Rigidbody>();
        m_GetMainRB.constraints = RigidbodyConstraints.FreezeRotation;
    }



    private void OnAnimatorMove()
    {
        if (!m_ModuleProcessAction || m_AnimationController == null) return;

        m_AnimationController.UpdateOnAnimatorMove();
    }
}
