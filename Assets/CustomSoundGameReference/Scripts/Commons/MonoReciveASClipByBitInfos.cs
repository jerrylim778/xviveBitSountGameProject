using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Sirenix.OdinInspector;
using Commons.Helpers;

public class MonoReciveASClipByBitInfos : MonoBehaviour
{
    [SerializeField] private bool m_BitStart = false;
    [SerializeField, ReadOnly, ShowIf("m_BitStart")] private bool m_isBitStartOn;

    [SerializeField] private AudioClipSpectrumExtractor m_AudioClipSpectrumExtractor;
    [SerializeField] private List<float[]> m_ReciveSubList = new();
    [SerializeField] private AudioSource m_ASInfo;
    [SerializeField, ReadOnly] public bool m_isRunSaved = false;
    private double m_DSPTimeNow;
    private float m_CurrDSPRunTime;
    private float pp_CycleCurrTime, pp_CycleMaxTime;
    

    private bool UpdateCheckCycle()
    {
        System.TimeSpan GetTS = System.TimeSpan.FromSeconds(AudioSettings.dspTime - m_DSPTimeNow);
        float CalculateTime = (float)GetTS.TotalSeconds * 60;
        m_CurrDSPRunTime = Mathf.Abs(pp_CycleMaxTime - CalculateTime);
        pp_CycleCurrTime = Mathf.Abs(1 - (m_CurrDSPRunTime / pp_CycleMaxTime));
        Debug.Log($"{pp_CycleCurrTime} :: {m_CurrDSPRunTime} :: {m_DSPTimeNow} ");
        if(m_CurrDSPRunTime <= 0.5f)
        {
            m_isRunSaved = false;
            m_ASInfo.Stop();
            m_AudioClipSpectrumExtractor.CreateOutParsingAudioSorce(m_ASInfo.clip, m_ReciveSubList);
        }
        return pp_CycleCurrTime > 0.5f;
    }

    private void ResetMainAudioClipTime()
    {
        m_ReciveSubList.Clear();
        m_DSPTimeNow = AudioSettings.dspTime;
        m_CurrDSPRunTime = 0; pp_CycleCurrTime = 0f;
        pp_CycleMaxTime = m_ASInfo.clip.length * 60f;
        if (m_ASInfo.isPlaying) m_ASInfo.Stop();
        m_ASInfo.Play();
    }

    [ContextMenu(nameof(ForceApplyGameInfo))]
    private void ForceApplyGameInfo()
    => m_AudioClipSpectrumExtractor.ForceApplyGameInfo();

    [ContextMenu(nameof(ForceApplyLevelInfo))]
    private void ForceApplyLevelInfo()
    => m_AudioClipSpectrumExtractor.ForceApplyAlbumInfo();


    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.P))
        {
            #region 이전 작업 관련
            if (m_BitStart)
            {
                m_isBitStartOn = !m_isBitStartOn;
                var GetAudioMixCon =
                FindAnyObjectByType<AudioMixVisualizeSubController>();
                GetAudioMixCon.BreakPoint(m_isBitStartOn,
                SubModuleControllerBase.SubModuleBreakType.OnlyBreakElems);
                var GetAllList =
                FindObjectsOfType<OutPutAudioEqulizeModule>().ToList().FindAll(x => !x.pp_isTestMode);
                GetAllList.HForEach(x =>
                {
                    if (m_isBitStartOn)
                    {
                        ModuleMonoBase NullMonoBase = null;
                        x.Initlization(NullMonoBase, GetAudioMixCon);
                    }
                    else x.SemiBreakPoint(m_isBitStartOn);
                });
            }
            else
            {
                ResetMainAudioClipTime();
                m_isRunSaved = true;
            }
            #endregion
        }

        if (m_isRunSaved)
        {
            if (UpdateCheckCycle())
            {
                float[] ApplySpectrum = new float[64];
                m_ASInfo.GetSpectrumData(ApplySpectrum, 0, FFTWindow.Rectangular);
                m_ReciveSubList.Add(ApplySpectrum);
            }
        }
    }
}
