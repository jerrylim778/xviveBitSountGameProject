using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

public class MonoReciveASClipByBitInfos : MonoBehaviour
{
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

    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.Space))
        {
            ResetMainAudioClipTime();
            m_isRunSaved = true;
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
