using UnityEngine;
using Sirenix.OdinInspector;
using Commons.Helpers;

//한행에서 관리될 모듈
public class ElemCategorySubCollModule : ModuleMonoBase
{
    [Space(10f)]
    [Header("Control Values")]
    [SerializeField] private bool m_isTestMode;
    [Header("Required Values")]
    [SerializeField, ShowIf("m_isTestMode")] private AudioClip test_TempAudioClip;
    [SerializeField, ReadOnly] private BitMakeSubController m_MainController;
    [SerializeField, ReadOnly] private SoundGameClipInfo m_CurrSoundGameClipInfo;

    [SerializeField, ReadOnly] private AudioASModule[] m_ColModule;
    
    public override void Initlization(ControllerBase _MainBase, params object[] _OtherParams)
    {
        m_MainController = _MainBase as BitMakeSubController;

        if (!m_isTestMode)
        {
            m_CurrSoundGameClipInfo = _OtherParams[0] as SoundGameClipInfo;
            if ((!m_CurrSoundGameClipInfo.s_AudioClipsInfo.CheckArrayNull(0)).
            HDebug("현재 음원이 존재하지 않습니다.!", Helper.HDType.Error)) return;
        }

        var ApplyClips = m_isTestMode ? test_TempAudioClip :
        m_CurrSoundGameClipInfo.s_AudioClipsInfo[0];

        m_ColModule = this.transform.GetChild(0).ChildLinearStuctureSearch<AudioASModule>();
        m_ColModule.HForEach(x => x.Initlization(this, this.GetComponent<AudioSource>(), ApplyClips));
    }

    

    public override void SemiBreakPoint(bool _isBreakPoint)
    {
        m_isProcessAction = _isBreakPoint;
        m_ColModule.HForEach(x => x.SemiBreakPoint(_isBreakPoint));
    }
}
