using UnityEngine;
using Commons.Helpers;
using Sirenix.OdinInspector;
using DG.Tweening;

//후에 AnimationControllerModeModule과 연계하여 재활용 가능성이 있는지의 여부를
//판단하여 리팩토링 할 수 있도록 수정한다
public class ControllerColliderModule : MonoBehaviour
{
    [Header("Control Values")]
    [SerializeField] private bool m_isNotUseOnlyOneStackEvent = false; //Enter로 들어온것만 모든 이벤트를 발동하기
    [SerializeField] private bool m_isLimitTimeByEnterExit = false;
    [SerializeField, ReadOnly, ShowIf(nameof(m_isLimitTimeByEnterExit))] private bool m_isCanEnter = true, m_isCanExit = true;
    [SerializeField, ShowIf(nameof(m_isLimitTimeByEnterExit))] private TimeSetting[] m_TimeSettings;
    [SerializeField] LayerMask m_AddLayer;
    [SerializeField, ReadOnly] LayerMask m_EnterLayer;
    [Header("Required Values")]
    [SerializeField, ReadOnly] private SubModuleControllerBase m_DependentController;
    [SerializeField, ReadOnly] private ControllerBase m_EnterColliderObj;
    [SerializeField, ReadOnly] Rigidbody m_GetMainRB;
    private bool m_isActionProcess = false;
    [field: SerializeField, ReadOnly] public Collider m_MainCollider { get; private set; }
    private System.Func<Collider, bool> m_AddEnterCondition = null;
    private ControllerBase.ColliderEventType s_CurrEventType = ControllerBase.ColliderEventType.Exit;
    public bool pp_isEnter { get => s_CurrEventType == ControllerBase.ColliderEventType.Enter; }

    public void Initlization<T>(SubModuleControllerBase _GetComponent, LayerMask _EnterLayer, System.Func<Collider, bool> _AddEnterCondition = null) where T : Collider
    {
        m_DependentController = _GetComponent;
        m_EnterLayer = _EnterLayer;
        m_AddEnterCondition = _AddEnterCondition;
        m_MainCollider = this.gameObject.CheckComnectComponent<T>();
        m_isCanEnter = true; m_isCanExit = true;
        m_isActionProcess = true;

    }

    private void OnTriggerEnter(Collider other)
    {
        if (!m_isActionProcess || !m_isCanEnter) return;
        if (m_AddEnterCondition != null && m_AddEnterCondition.Invoke(other)) return;
        //if (m_isLimitTimeByEnterExit)
        //    m_TimeSettings[0].s_StartCounting = true;

        LayerMask GetMask = 1 << other.gameObject.layer;
        if ((m_EnterLayer == other.gameObject.layer || GetMask == m_AddLayer)
        && other.TryGetComponent<ControllerBase>(out ControllerBase _GetBases))
        {
            if (!_GetBases.pp_isMine) return;
            if (m_isLimitTimeByEnterExit && m_TimeSettings[0].s_StartCounting) return;
            if (m_isLimitTimeByEnterExit) m_TimeSettings[0].s_StartCounting = true;

            s_CurrEventType = ControllerBase.ColliderEventType.Enter;
            if (!m_isNotUseOnlyOneStackEvent) m_EnterColliderObj = other.gameObject.GetComponent<ControllerBase>();
            m_DependentController.ColliderEventCallBack(this, 
            m_isNotUseOnlyOneStackEvent ? _GetBases : m_EnterColliderObj, s_CurrEventType);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!m_isActionProcess || !m_isCanExit) return;
        if (m_AddEnterCondition != null && m_AddEnterCondition.Invoke(other)) return;
        

        LayerMask GetMask = 1 << other.gameObject.layer;
        if ((m_EnterLayer == other.gameObject.layer || GetMask == m_AddLayer)
        && other.TryGetComponent<ControllerBase>(out ControllerBase _GetBases))
        {
            if (!_GetBases.pp_isMine) return;
            if (m_isLimitTimeByEnterExit && m_TimeSettings[1].s_StartCounting) return;
            if (m_isLimitTimeByEnterExit) m_TimeSettings[1].s_StartCounting = true;
            

            s_CurrEventType = ControllerBase.ColliderEventType.Exit;
            m_DependentController.ColliderEventCallBack(this,
            m_isNotUseOnlyOneStackEvent ? _GetBases : m_EnterColliderObj, s_CurrEventType);
            if(!m_isNotUseOnlyOneStackEvent) m_EnterColliderObj = null;
        }
    }

    private void Update()
    {
        if (!m_isActionProcess) return;
        if (!m_isLimitTimeByEnterExit) return;

        if(m_TimeSettings[0].s_StartCounting) 
            m_isCanEnter = m_TimeSettings[0].CountingDefultTime();
        if (m_TimeSettings[1].s_StartCounting) 
            m_isCanExit = m_TimeSettings[1].CountingDefultTime();
    }
}

[System.Serializable]
public class TimeSetting
{
    public bool s_StartCounting = false;
    [ReadOnly] public float s_CurrTime;
    public float s_MaxTime;

    public TimeSetting(int _CurrTime, int _MaxTime)
    {
        s_CurrTime = _CurrTime;
        s_MaxTime = _MaxTime;
    }

    public bool CountingDefultTime()
    {
        if (s_CurrTime < s_MaxTime)
        {
            s_CurrTime += Time.unscaledDeltaTime * 1.5f;
            return false;
        }
        if (s_StartCounting) { s_CurrTime = 0f; s_StartCounting = false;}
        if (s_CurrTime >= s_MaxTime) return false;
        return true;
    }
}
