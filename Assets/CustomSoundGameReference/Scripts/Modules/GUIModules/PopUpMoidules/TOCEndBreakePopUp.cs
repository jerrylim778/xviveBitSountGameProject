using System;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using Commons.Helpers;
using Sirenix.OdinInspector;

public class TOCEndBreakePopUp : SystemBase, I_PopUpInfo, I_PopUpPush, I_PopUpPop, I_PopUpDecorDem
{
    [Header("Main Values")]
    [SerializeField] RectTransform m_MainBG;
    [SerializeField] Button m_TocMainBtn;
    [SerializeField] Animator m_CycleCGAnim, m_ClickAnim;
    [Header("Control Values")]
    [SerializeField, ReadOnly] private int m_CurrTOCCount;
    [SerializeField, ReadOnly] private int m_MaxTOCount;

    [Header("Required Values")]
    private System.Action m_EndCallBack = null;
    private System.Action<int, int> m_ApplyCountCallBack = null;
    #region Required Functions

    public PopUpType I_GetPopUpType() => PopUpType.DefultPopUp;
    public SystemBase I_GetPopUpBase() => this;

    public float I_SetDem() => 0f;

    #endregion

    public override void Initlization()
    {
        m_MainBG.localScale = Vector3.one * 1.5f;
        m_MainBG.gameObject.CheckComnectComponent<CanvasGroup>().alpha = 0f;
        m_CycleCGAnim.gameObject.CheckComnectComponent<CanvasGroup>().alpha = 0f;
        Helper.ChildLinearStuctureSearch(m_MainBG).HForEach(x => x.gameObject.SetActive(false));
    }

    public void InitTOCEndBrakePopUp(Transform _SyncMainBGTr, int _MaxCount, System.Action<int, int> _ApplyCountCallBack, System.Action _EndCallBack)
    {
        Initlization();
        m_MaxTOCount = _MaxCount;
        m_EndCallBack = _EndCallBack;
        m_ApplyCountCallBack = _ApplyCountCallBack;

        var GetWorldScreenTr = Camera.main.WorldToScreenPoint(_SyncMainBGTr.position);
        var GetMainOverlayCan = GamePlaySystem.Instance.pp_UsingMainCan.transform as RectTransform;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(GetMainOverlayCan, GetWorldScreenTr, null, out Vector2 _GetScreenPos);
        m_MainBG.anchoredPosition = _GetScreenPos;
        
    }

    public void PushOutEvent(Action _AddCallBack = null)
    {
        var ApplyCan = m_MainBG.gameObject.CheckComnectComponent<CanvasGroup>();
        DOTween.To(() => ApplyCan.alpha, x => ApplyCan.alpha = x, 1f, 0.45f).SetEase(Ease.InCirc);
        m_MainBG.DOScale(Vector3.one, 0.45f).SetEase(Ease.InCirc).OnComplete(() =>
        {
            m_CycleCGAnim.transform.SetParent(this.transform);
            m_CycleCGAnim.gameObject.SetActive(true);
            m_CycleCGAnim.CrossFade("Do_AlphaBlink", 0f);
            m_ClickAnim.CrossFade("Do_ScaleBlink", 0f);
            m_TocMainBtn.onClick.AddListener(OnClickTocMainBtn);
        });
    }

    private void OnClickTocMainBtn()
    {
        if (m_CurrTOCCount < m_MaxTOCount)
        {
            m_ApplyCountCallBack?.Invoke(m_CurrTOCCount, m_MaxTOCount);
            m_CurrTOCCount++;
            return;
        }

        m_TocMainBtn.onClick.RemoveAllListeners();
        GamePlaySystem.Instance.MainPopUpPop<TOCEndBreakePopUp>();
        //얼음깨지 로직 발동할것
    }

    public void PopOutEvent(Action _EndCallBack = null)
    {
        m_CycleCGAnim.CrossFade("NullEvent", 0f);
        m_ClickAnim.CrossFade("NullEvent", 0f);
        m_CycleCGAnim.gameObject.SetActive(false);
        var GetEndCG =
        m_ClickAnim.gameObject.CheckComnectComponent<CanvasGroup>();
        DOTween.To(() => GetEndCG.alpha, x => GetEndCG.alpha = x, 0f, 0.45f).
        OnComplete(() => 
        {
            m_EndCallBack?.Invoke();
            _EndCallBack?.Invoke();
        }); 
    }
}
