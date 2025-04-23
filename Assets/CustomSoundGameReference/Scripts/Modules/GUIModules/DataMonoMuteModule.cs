using UnityEngine;
using UnityEngine.UI;
using Commons.Helpers;
using Sirenix.OdinInspector;
using DG.Tweening;

public class DataMonoMuteModule : ModuleMonoBase
{
    [Header("Required Values")]
    [SerializeField] private bool m_ApplyNewVer;
    [SerializeField, ReadOnly] private SoundGameController m_GetSoundController;
    [SerializeField, ReadOnly] private ElemSinngerSubController m_GetMainController;
    [SerializeField] private Slider m_WaitMusicSlider;
    [SerializeField, HideIf("m_ApplyNewVer")] private Button m_MuteSingleBtn, m_MuteOtherBtn;
    [SerializeField, ShowIf("m_ApplyNewVer")] private Button m_DeleteMusicBtn;
    [SerializeField, ShowIf("m_ApplyNewVer")] private RectTransform m_MaskByBtnSubDecor;

    //private bool m_isBreakPoint;
    private Button[] m_BtnControllers;
    private System.Action m_EndCallBack;
    private Sequence m_SeqMaskSubDecor;
    private Vector2 m_MaskSubDecorSizeD;

    public bool pp_isReadyToSlider => m_WaitMusicSlider.gameObject.activeSelf;

    public override void Initlization(ControllerBase _MainBase, params object[] _OtherParams) 
    {
        m_GetMainController = _MainBase as ElemSinngerSubController;
        bool isPlayingNow = (bool)_OtherParams[0];
        m_EndCallBack = _OtherParams[1] as System.Action;
        m_GetSoundController = m_GetMainController.FindParentsObjTypeByTemp<SoundGameController>();
        if (m_ApplyNewVer)
        {
            m_DeleteMusicBtn.interactable = false;
            m_MaskSubDecorSizeD = m_MaskByBtnSubDecor.sizeDelta;
        }
        else
        {
            m_BtnControllers = new Button[3] { m_MuteSingleBtn, m_MuteOtherBtn, m_DeleteMusicBtn };
            m_BtnControllers.HForEach(x => x.interactable = false);
        }

        m_DeleteMusicBtn.onClick.AddListener(() =>
        {
            if (m_ApplyNewVer) m_DeleteMusicBtn.interactable = false; 
            else m_BtnControllers.HForEach(x => x.interactable = false);
            this.gameObject.SetActive(false);
            m_GetMainController.DeleteThisSound(this);
        });
        m_WaitMusicSlider.gameObject.SetActive(isPlayingNow);
        if (!isPlayingNow)
        {
            m_EndCallBack?.Invoke();
            this.gameObject.SetActive(false);
        }
    }

    public void StartInitAgain() => m_WaitMusicSlider.gameObject.SetActive(true);

    public override void SemiBreakPoint(bool _isBreakPoint)
    {
        m_isProcessAction = _isBreakPoint;
        if(!m_ApplyNewVer)
        {
            m_BtnControllers.HForEach(x => x.interactable = _isBreakPoint);
            m_DeleteMusicBtn.interactable = true;
        }
        if (_isBreakPoint && m_ApplyNewVer) TweeningNewProductionRectOutPut();
    }

    public void TweeningNewProductionRectOutPut()
    {
        m_DeleteMusicBtn.interactable = false;
        m_MaskByBtnSubDecor.sizeDelta = Vector2.zero;
        m_SeqMaskSubDecor.ResetSequce(true);
        m_SeqMaskSubDecor = DOTween.Sequence();
        m_SeqMaskSubDecor.Append(
        m_MaskByBtnSubDecor.DOSizeDelta(m_MaskSubDecorSizeD, 0.85f).SetEase(Ease.OutBack));
        m_SeqMaskSubDecor.OnComplete(() => m_DeleteMusicBtn.interactable = true);
    }


    private void Update()
    {
        if (!m_isProcessAction || !m_WaitMusicSlider.gameObject.activeSelf) return;
        m_WaitMusicSlider.value = m_GetSoundController.pp_CycleCurrTime;

        if(m_WaitMusicSlider.value >= 0.99f || m_GetSoundController.pp_CycleCurrTime >= 0.99f)
        {
            m_WaitMusicSlider.gameObject.SetActive(false);
            m_EndCallBack?.Invoke();
            this.gameObject.SetActive(false);
            return;
        }
    }
}
