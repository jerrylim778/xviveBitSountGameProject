using UnityEngine;
using Sirenix.OdinInspector;
using Commons.Helpers;

public class ReflectPhycisModule : MonoBehaviour
{
    //단순 충돌시 객체끼리 반사하고자 하는 객체에 추가해서 적용할것
    //다른 객체의 Collider계산에 대한값과 비슷하기에 적절히 섞어서 사용할 수 있도록 수정해보자
    [SerializeField, Range(0f, 10f)] private float m_ReflectPower = 5f;
    [SerializeField, ReadOnly] private bool m_isOnce = false, m_isDesWhenModuleDes =false;
    [SerializeField, ReadOnly] private LayerMask? m_ThrowLayer;
    [SerializeField, ReadOnly] private int m_EnterCount = 0;
    [SerializeField, ReadOnly] private Rigidbody m_GetMainRB;
    [SerializeField, ReadOnly] private Collider m_GetMainCollider;
    

    public virtual void Initlization<T>(LayerMask? _StaticLayer = null, bool _isOnce = false, bool _isDesWhenModuleDes = false) where T : Collider
    {
        m_isOnce = _isOnce;
        m_isDesWhenModuleDes = _isDesWhenModuleDes;
        m_ThrowLayer = _StaticLayer;
        m_GetMainCollider = this.gameObject.CheckComnectComponent<T>();
        m_GetMainRB = this.gameObject.CheckComnectComponent<Rigidbody>();
    }

    #region 조건에 상관없이 계속 실행됨 (허나 특정 조건을 만족하면 반드시 Stay에서 반사를 실행하는것으로 변경해야됨)
    //private void OnCollisionStay(Collision collision)
    //{
    //    if (m_isOnce && m_EnterCount > 1) return;
    //    if (m_ThrowLayer != null && m_ThrowLayer.HasValue &&
    //    m_ThrowLayer.Value != collision.gameObject.layer) return;

    //    m_EnterCount++;
    //    var ColliderSurfaceArrow = collision.contacts[0].normal;
    //    var CurrPhycisArrow = m_GetMainRB.linearVelocity;
    //    //반사 빽터 계산
    //    var MoveReflect = Vector3.Reflect(CurrPhycisArrow, ColliderSurfaceArrow);
    //    m_GetMainRB.linearVelocity += MoveReflect * m_ReflectPower;
    //}
    #endregion

    private void OnCollisionEnter(Collision collision)
    {
        if (m_isOnce && m_EnterCount > 1) return;
        if (m_ThrowLayer != null && m_ThrowLayer.HasValue &&
        m_ThrowLayer.Value != collision.gameObject.layer) return;

        m_EnterCount++;
        bool _isRight = System.Convert.ToBoolean(Random.Range(0, 1));
        //현재는 좌우로 단순 힘을 줘서 겹치는것을 방지하는 역활만 적용한다
        m_GetMainRB.AddForce(this.transform.position + 
        (_isRight ? Vector3.right : -Vector3.right) * m_ReflectPower, ForceMode.Force);
        #region 주석된 코드 참고하여 반사 백터 즉 정석적으로 적용을 시도할것
        //단순 좌우로 Force를 주는걸로 대체하지만 반드시 반사 백터를 이용하여 자연스러운 
        //마찰효과를 주어야함
        //var ColliderSurfaceArrow = collision.contacts[0].normal;
        //var CurrPhycisArrow = m_GetMainRB.linearVelocity;
        ////반사 빽터 계산
        //var MoveReflect = Vector3.Reflect(CurrPhycisArrow, ColliderSurfaceArrow);
        //m_GetMainRB.linearVelocity += MoveReflect * m_ReflectPower;
        #endregion
    }

    private void OnDestroy()
    {
        if (!m_isDesWhenModuleDes) return;
        Destroy(m_GetMainRB);
        Destroy(m_GetMainCollider);
    }
}
