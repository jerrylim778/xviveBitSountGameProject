using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Commons.Helpers;
using DG.Tweening;
using TMPro;


public class NoticePopUp : SystemBase, I_PopUpInfo, I_PopUpPush, I_PopUpPop
{
    public enum NoticeBtnType { Continue, Cancel}
    [System.Serializable]
    public struct NoticeBtnInfo
    {
        public NoticeBtnType s_NoticeBtnType;
        public Button s_OutPutBtn;
    }
    [Header("===NoticeReference===")]
    [Header("Control Values")]
    [SerializeField] private float m_TweeningSped = 0.35f;
    [Header("Required Values")]
    [SerializeField] private TextMeshProUGUI m_MainTMPTxt;
    [SerializeField] private RectTransform m_MainBG;
    [SerializeField] private RectTransform m_NoticeTitle;
    [SerializeField] private RectTransform m_BtnPanelBG;
    [SerializeField] private NoticeBtnInfo[] m_AllBtns;
    
    [Tooltip("Required Values")]
    private Dictionary<NoticeBtnType, Button> m_DicSettingBtn = new();
    private System.Action<bool> m_BtnCallBacks;

    #region Required Functions
    public SystemBase I_GetPopUpBase() => this;
    public PopUpType I_GetPopUpType() => PopUpType.DefultPopUp;

    #endregion

    public override void Initlization()
    {
        Helper.ChildLinearStuctureSearch(this.transform).
        HForEach(x => x.gameObject.SetActive(false));
        m_BtnPanelBG.localScale = Vector3.right;
        m_MainBG.transform.localScale = Vector3.right;
        m_NoticeTitle.anchoredPosition += Vector2.up * 100f;
        m_NoticeTitle.gameObject.CheckComnectComponent<CanvasGroup>().alpha = 0f;
        m_AllBtns.HForEach(x => m_DicSettingBtn.Add(x.s_NoticeBtnType, x.s_OutPutBtn));
    }

    public void InitNotice(string _NoticeTxt, System.Action _ContinueBtn = null)
    {
        Initlization();
        m_MainTMPTxt.text = _NoticeTxt;
        m_DicSettingBtn[NoticeBtnType.Cancel].gameObject.SetActive(false);
        m_BtnCallBacks = _isComplate =>
        {
            m_MainControllSystem.MainPopUpPop<NoticePopUp>();
            if (_ContinueBtn != null && _isComplate) _ContinueBtn?.Invoke();
        };
    }

    public void InitNotice(string _NoticeTxt, System.Action<bool> _ChooseBtnCallBack)
    {
        Initlization();
        m_MainTMPTxt.text = _NoticeTxt;
        m_BtnCallBacks = _ChooseBtnCallBack;
    }

    public void PushOutEvent(System.Action _AddCallBack = null)
    {
        Helper.ChildLinearStuctureSearch(this.transform).
        HForEach(x => x.gameObject.SetActive(true));
        m_MainBG.DOScaleY(1f, m_TweeningSped).SetEase(Ease.InOutCirc).OnComplete(() => 
        {
            var ChangeArrow = m_NoticeTitle.anchoredPosition + Vector2.up * -100f;
            var GetCG = m_NoticeTitle.gameObject.CheckComnectComponent<CanvasGroup>();
            DOTween.To(() => GetCG.alpha, x => GetCG.alpha = x, 1f, m_TweeningSped).SetEase(Ease.OutSine);
            m_NoticeTitle.DOAnchorPos(ChangeArrow, m_TweeningSped).SetEase(Ease.OutExpo);
            m_BtnPanelBG.DOScaleY(1f, m_TweeningSped).SetEase(Ease.InOutCirc).OnComplete(() =>
            m_DicSettingBtn.HForEach(x =>
            {
                bool Complate = x.Key == NoticeBtnType.Continue;
                if (x.Value.gameObject.activeSelf) x.Value.onClick.AddListener(() => 
                {
                    x.Value.gameObject.SetActive(false);
                    m_BtnCallBacks(Complate);
                });
            }));
        });
    }

    public void PopOutEvent(System.Action _EndCallBack = null)
    {
        m_NoticeTitle.gameObject.SetActive(false);
        m_MainBG.DOScaleY(0f, m_TweeningSped).SetEase(Ease.InOutExpo).OnComplete(() =>
        _EndCallBack?.Invoke());
    }
}
