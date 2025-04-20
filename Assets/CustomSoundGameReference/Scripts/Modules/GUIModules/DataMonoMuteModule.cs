using UnityEngine;
using UnityEngine.UI;
using Commons.Helpers;
using Sirenix.OdinInspector;

public class DataMonoMuteModule : ModuleMonoBase
{
    [Header("Required Values")]
    [SerializeField, ReadOnly] private SoundGameController m_GetSoundController;
    [SerializeField, ReadOnly] private ElemSinngerSubController m_GetMainController;
    [SerializeField] private Slider m_WaitMusicSlider;
    [SerializeField] private Button m_MuteSingleBtn, m_MuteOtherBtn, m_DeleteMusicBtn;

    private bool /*m_IsFirst, */m_isBreakPoint;
    private Button[] m_BtnControllers;
    private System.Action m_EndCallBack;

    public bool pp_isReadyToSlider => m_WaitMusicSlider.gameObject.activeSelf;
    

    public override void Initlization(ControllerBase _MainBase, params object[] _OtherParams) 
    {
        m_GetMainController = _MainBase as ElemSinngerSubController;
        bool isPlayingNow = (bool)_OtherParams[0];
        m_EndCallBack = _OtherParams[1] as System.Action;
        m_GetSoundController = m_GetMainController.FindParentsObjTypeByTemp<SoundGameController>();
        m_BtnControllers = new Button[3] { m_MuteSingleBtn, m_MuteOtherBtn, m_DeleteMusicBtn };
        m_BtnControllers.HForEach(x => x.interactable = false);

        m_DeleteMusicBtn.onClick.AddListener(() =>
        {
            m_BtnControllers.HForEach(x => x.interactable = false);
            this.gameObject.SetActive(false);
            m_GetMainController.DeleteThisSound(this);
        });
        m_WaitMusicSlider.gameObject.SetActive(isPlayingNow);
        if (!isPlayingNow) m_EndCallBack?.Invoke();
    }

    public void StartInitAgain() => m_WaitMusicSlider.gameObject.SetActive(true);

    public override void SemiBreakPoint(bool _isBreakPoint)
    {
        m_isProcessAction = _isBreakPoint;
        m_BtnControllers.HForEach(x => x.interactable = _isBreakPoint);
        m_DeleteMusicBtn.interactable = true;
        //if (m_IsFirst) return; m_IsFirst = true;
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
