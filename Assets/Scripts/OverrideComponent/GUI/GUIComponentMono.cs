using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class GUIComponentMono : MonoBehaviour
{
    private bool m_isConnectSetGUI = false;
    private UIBehaviour m_MainConnectGUI;

    private void RequiredInitialization()
    {
        if (!m_isConnectSetGUI)
        {
            m_MainConnectGUI = this.GetComponent<UIBehaviour>();
            m_isConnectSetGUI = true;
        }
        if (!m_isConnectSetGUI) Debug.LogErrorFormat("초기화 되지 않는 함수가 존재합니다..!! {0}", this.gameObject.name);
    }

    #region 유니티 이벤트 함수
    void Awake()
    { RequiredInitialization();
        if (m_isConnectSetGUI && m_MainConnectGUI is I_MonoEventAwake) (m_MainConnectGUI as I_MonoEventAwake).I_Awake(); }
    void OnEnable()
    { RequiredInitialization();
        if (m_isConnectSetGUI && m_MainConnectGUI is I_MonoEventOnEnable) (m_MainConnectGUI as I_MonoEventOnEnable).I_OnEnable(); }
    void Start()
    { RequiredInitialization();
        if (m_isConnectSetGUI && m_MainConnectGUI is I_MonoEventStart) (m_MainConnectGUI as I_MonoEventStart).I_Start(); }
    void OnDestroy()
    { RequiredInitialization();
        if (m_isConnectSetGUI && m_MainConnectGUI is I_MonoEventAllHideOnce) (m_MainConnectGUI as I_MonoEventAllHideOnce).I_Destroy();
    m_isConnectSetGUI = false;}
    void OnApplicationQuit()
    { RequiredInitialization();
        if (m_isConnectSetGUI && m_MainConnectGUI is I_MonoEventAllHideOnce) (m_MainConnectGUI as I_MonoEventAllHideOnce).I_OnAppQuit(); 
    m_isConnectSetGUI = false;}
    #endregion
}

//이후에 다른 함수사용 또한 이터페이스로 정의하여 가져올것

public interface I_MonoEventOnEnable
{
    public void I_OnEnable();
}

public interface I_MonoEventAwake
{
    public void I_Awake();
}

public interface I_MonoEventStart
{
    public void I_Start();
}

public interface I_MonoEventAllHideOnce
{
    public void I_Destroy();
    public void I_OnAppQuit();
}
