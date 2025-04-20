using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class LoadingPopUp : SystemBase, I_PopUpInfo, I_PopUpPush, I_PopUpPop
{
    [Header("GUIControlReference")]
    [SerializeField] private TextMeshProUGUI m_MainTex;
    [SerializeField] private Image m_MainBG, m_LoadingICON;
    [Tooltip("Required Values")]
    private bool m_isUpdateProcess = false, m_isWaitPopUpMotion = false;
    private LoadingManager m_LoadingManager;
    private System.Action m_EndCallBack = null;

    #region Required Functions

    public SystemBase I_GetPopUpBase() => this;
    public PopUpType I_GetPopUpType() => PopUpType.DefultPopUp;
    private void ReciveLoadingTxt(string _GetTxt)
    {
        if (string.IsNullOrEmpty(_GetTxt)) return; 
        m_MainTex.text = _GetTxt;
    }
    private void ComplatePopUpPopOrder()
    {
        m_MainControllSystem?.MainPopUpPop<LoadingPopUp>();
        m_LoadingManager.DELManager_Loading_Complate -= ComplatePopUpPopOrder;
    }

    #endregion

    public override void Initlization()
    {
        m_LoadingManager = Appinstance.Instance.ms_LoadingManager;
        m_LoadingManager.DELManager_ShowOther_Txt += ReciveLoadingTxt;
        m_LoadingManager.DELManager_Loading_Complate += ComplatePopUpPopOrder;
        m_MainTex.text = "Loading...";
        m_MainBG.rectTransform.localScale = Vector3.zero;
        m_isUpdateProcess = true;
    }

    public void InitLoadingPopUp(System.Action _LoadingRunCallBack, System.Action _EndCallBack = null)
    {
        Initlization();
        m_EndCallBack = _EndCallBack;
        _LoadingRunCallBack?.Invoke();
    }

    public void InitLoadingPopUp(Dictionary<int, System.Action> _UsedMainLoadingProcessCallBack, System.Action _EndCallBack = null)
    {
        Initlization();
        m_EndCallBack = _EndCallBack;
        Appinstance.Instance.ms_LoadingManager.StartLoadProcessDefult(0.5f, _UsedMainLoadingProcessCallBack);
    }

    public void PushOutEvent(System.Action _AddCallBack = null)
    {
        if (_AddCallBack != null) m_EndCallBack += _AddCallBack;
        m_MainBG.rectTransform.DOScale(Vector3.one, 0.35f).SetEase(Ease.OutExpo).OnComplete(() =>
        m_isWaitPopUpMotion = true);
    }

    public void PopOutEvent(Action _EndCallBack = null)
    {
        if (m_isWaitPopUpMotion)
        {
            m_isUpdateProcess = false;
            m_LoadingManager.DELManager_ShowOther_Txt -= ReciveLoadingTxt;
            m_EndCallBack?.Invoke();
            _EndCallBack?.Invoke();
            return;
        }
        if(_EndCallBack != null) m_EndCallBack += _EndCallBack;
        StartCoroutine(CO_WaitOutPutMotion(m_EndCallBack));
    }

    IEnumerator CO_WaitOutPutMotion(System.Action _EndCallBack)
    {
        yield return new WaitUntil(() => m_isWaitPopUpMotion);
        m_isUpdateProcess = false;
        m_LoadingManager.DELManager_ShowOther_Txt -= ReciveLoadingTxt;
        _EndCallBack?.Invoke();
    }

    public override void UpdateProcess()
    {
        if (!m_isUpdateProcess) return;
        base.UpdateProcess();

        m_LoadingICON.rectTransform.Rotate(m_LoadingICON.rectTransform.forward * 3f);
    }
}
