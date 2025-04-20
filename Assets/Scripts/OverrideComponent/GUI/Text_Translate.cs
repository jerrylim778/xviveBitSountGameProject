using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(GUIComponentMono))]
public class Text_Translate : Text, I_StartControllerBase ,I_MonoEventAwake, I_MonoEventOnEnable, I_MonoEventAllHideOnce
{
    private bool m_isAddDel = false;
    public void SC_Start()
    {
        if (m_isAddDel) return;
        if (Appinstance.Instance == null || Appinstance.Instance.ms_TranslateManager == null)
        {
            Debug.LogErrorFormat("InterScene의 초기화를 일부 " +
            "마무리되어야 사용가능합니다..!! {0}", this.gameObject.name);
            return;
        }
        Appinstance.Instance.ms_TranslateManager.DELManager_TranslateAll_Text += OnChangeByTranslate;
        OnChangeByTranslate();
        m_isAddDel = true;
    }

    private void OnChangeByTranslate()
    {
        string GetStr = Appinstance.Instance.ms_TranslateManager.ChangeTranslateLanguage(this.text);
        this.text = GetStr;
    }


    #region ConnectMonoGUIComponent 이벤트 함수

    public void I_OnEnable()
    {
        if (m_isAddDel && !Appinstance.Instance.ms_TranslateManager.CheckTranslateLanguage(this.text)) OnChangeByTranslate();
        if (m_isAddDel || Appinstance.Instance == null || Appinstance.Instance.ms_TranslateManager == null) return;
        Appinstance.Instance.ms_TranslateManager.DELManager_TranslateAll_Text += OnChangeByTranslate;
        m_isAddDel = true;
    }

    public void I_Awake()
    {
        if (m_isAddDel || Appinstance.Instance == null || Appinstance.Instance.ms_TranslateManager == null) return;
        Appinstance.Instance.ms_TranslateManager.DELManager_TranslateAll_Text += OnChangeByTranslate;
        m_isAddDel = true;
    }

    public void I_Destroy()
    {
        if (!m_isAddDel) return;
        Appinstance.Instance.ms_TranslateManager.DELManager_TranslateAll_Text -= OnChangeByTranslate;
        m_isAddDel = false;
    }

    public void I_OnAppQuit()
    {
        if (!m_isAddDel) return;
        Appinstance.Instance.ms_TranslateManager.DELManager_TranslateAll_Text -= OnChangeByTranslate;
        m_isAddDel = false;
    }
    #endregion

    #region 유니티 이벤트 함수

    //protected override void Awake()
    //{
    //    base.Awake();
    //    if (m_isAddDel || Appinstance.Instance == null || Appinstance.Instance.ms_TranslateManager == null) return;
    //    Appinstance.Instance.ms_TranslateManager.DELManager_TranslateAll_Text += OnChangeByTranslate;
    //    m_isAddDel = true;
    //}
    //protected override void OnDestroy()
    //{
    //    base.OnDestroy();
    //    if (!m_isAddDel) return;
    //    Appinstance.Instance.ms_TranslateManager.DELManager_TranslateAll_Text -= OnChangeByTranslate;
    //    m_isAddDel = false;
    //}

    //private void OnApplicationQuit()
    //{
    //    if (!m_isAddDel) return;
    //    Appinstance.Instance.ms_TranslateManager.DELManager_TranslateAll_Text -= OnChangeByTranslate;
    //    m_isAddDel = false;
    //}
    #endregion
}
