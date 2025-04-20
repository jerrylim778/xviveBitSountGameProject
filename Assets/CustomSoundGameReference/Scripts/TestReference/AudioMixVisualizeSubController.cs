using UnityEngine;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using Commons.Helpers;
using System;

public class AudioMixVisualizeSubController : SubModuleControllerBase
{
    // �ͽ� ��� ������
    public enum MixingMethod
    {
        Sum,        // ��� ����� �ҽ� ����Ʈ���� ��
        Average,    // ��� ����� �ҽ� ����Ʈ���� ���
        Maximum     // �� ���ļ� �뿪�� �ִ밪 ���
    }

    [SerializeField, Range(0f, 20000f)] private float m_MaxFreq;

    [Header("Test Only")]
    [SerializeField, ReadOnly, ShowIf("m_isTestMode")] private int test_CoundIDX = 0;
    [SerializeField, ShowIf("m_isTestMode")] private List<ElemASBuffer> test_ListenaudioSources = new();

    [Space(20f)]

    [Header("OutPut Spectrum Values")]
    [SerializeField, ShowIf("m_isTestMode")] private List<OutPutAudioEqulizeModule> m_OutPutAudioEqulList = new();

    [Tooltip("Required Values")]
    [SerializeField, ReadOnly] private List<ElemASBuffer> audioSources = new();

    [Tooltip("FFT ������ ����")]
    public FFTWindow fftWindow = FFTWindow.BlackmanHarris;

    [Tooltip("����Ʈ�� ũ�� (2�� �ŵ����� ����: 128, 256, 512, 1024, 2048)")]
    public int spectrumSize = 512;

    [Tooltip("����� �ҽ� �ͽ� ���")]
    public MixingMethod mixingMethod = MixingMethod.Average;

    [Tooltip("���ļ� �뿪 ����� ���� ǥ��")]
    public bool showDebugInfo = false;

    // �� ����� �ҽ��� ����Ʈ�� ����� �迭
    private float[][] sourceSpectrums;
    private float[] combinedSpectrum; // ���� �ջ�/�ͽ̵� ����Ʈ��

    public override object[] Initlization(params object[] _ParsingParams)
    {
        var PartParams = base.Initlization(_ParsingParams);

        FindParentsObjTypeByTemp<SoundGameController>().I_CheckSubModuleIsAllCanAction(this);
        return null;
    }

    public override void BreakPoint(bool _isBreak, SubModuleBreakType _BrackType = SubModuleBreakType.BreakAll, Type _GetType = null)
    {
        base.BreakPoint(_isBreak, _BrackType, _GetType);
        if(_isBreak) RecycleArrays();
        pp_isSelfProcessUpdate = _BrackType == SubModuleBreakType.OnlyBreakElems;
    }

    private void RecycleArrays()
    {
        // ����� �ҽ��� ����Ʈ�� �迭 �ʱ�ȭ
        sourceSpectrums = new float[audioSources.Count][];
        for (int i = 0; i < audioSources.Count; i++)
        {
            sourceSpectrums[i] = new float[spectrumSize];
        }

        // ������ ����Ʈ�� �� �ִϸ��̼� �迭 �ʱ�ȭ
        combinedSpectrum = new float[spectrumSize];
        m_OutPutAudioEqulList.ForEach(x => x.ResetCycle());
    }

    #region Main System Public Functions (**Recive AudioSource Reference**)

    public void InitOutPutAudioSpectrum(bool _isApply, OutPutAudioEqulizeModule _ApplyOAE = null)
    {
        m_isActionProcess = false;
        if (_isApply) m_OutPutAudioEqulList.Add(_ApplyOAE);
        else m_OutPutAudioEqulList.Remove(_ApplyOAE);
        _ApplyOAE.ResetCycle();
        m_isActionProcess = true;
    }

    public void AddAudioSource(ElemASBuffer source)
    {
        if (source != null && !audioSources.Contains(source))
        {
            audioSources.Add(source);
            if (m_isTestMode)
            {
                if (!source.m_AudioSource.isPlaying)
                source.m_AudioSource.Stop();
                source.m_AudioSource.Play();
            }
            // �迭 ���ʱ�ȭ
            RecycleArrays();
        }
    }

    
    public void RemoveAudioSource(ElemASBuffer source)
    {
        if (audioSources.Contains(source))
        {
            audioSources.Remove(source);
            if(m_isTestMode) source.m_AudioSource.Stop();
            // �迭 ���ʱ�ȭ
            RecycleArrays();
        }
    }

    // �ͽ� ��� ����
    public void SetMixingMethod(MixingMethod method) => mixingMethod = method;
    #endregion

    #region Main System Public Functions (**Update Check Curr Audio Lite State**)

    private void GetAllSpectrumData()
    {
        // �� ����� �ҽ����� ����Ʈ�� ������ ��������
        for (int i = 0; i < audioSources.Count; i++)
        {
            if (audioSources[i] != null && audioSources[i].m_AudioSource.isPlaying)
            {
                //audioSources[i].GetSpectrumData(sourceSpectrums[i], 0, fftWindow);
                sourceSpectrums[i] = audioSources[i].UpdateThisAnimClipLeg();
            }
            else // ��� ���� �ƴϰų� null�� �ҽ��� 0���� �ʱ�ȭ
            {
                if (audioSources[i] != null && !audioSources[i].IsListenOrPaused()) audioSources.RemoveAt(i);
                System.Array.Clear(sourceSpectrums[i], 0, sourceSpectrums[i].Length);
            }
        }
    }

    private void CombineSpectrumData()
    {
        // ���� ���յ� ����Ʈ�� �ʱ�ȭ
        System.Array.Clear(combinedSpectrum, 0, combinedSpectrum.Length);

        // ����� �ҽ��� ������ ����
        if (audioSources.Count == 0) return;

        // ���õ� �ͽ� ��Ŀ� ���� ����Ʈ�� ����
        switch (mixingMethod)
        {
            case MixingMethod.Sum:
                // ��� ����Ʈ�� ���� ����
                for (int sourceIndex = 0; sourceIndex < audioSources.Count; sourceIndex++)
                {
                    for (int i = 0; i < spectrumSize; i++)
                    {
                        if (sourceSpectrums[sourceIndex].CheckArrayNull(0) &&
                        i > 0 && i < sourceSpectrums[sourceIndex].Length)
                        combinedSpectrum[i] += sourceSpectrums[sourceIndex][i];
                    }
                }
                break;

            case MixingMethod.Average:
                // ��� ����Ʈ�� ���� ��� ���
                for (int sourceIndex = 0; sourceIndex < audioSources.Count; sourceIndex++)
                {
                    for (int i = 0; i < spectrumSize; i++)
                    {
                        if(sourceSpectrums[sourceIndex].CheckArrayNull(0) &&
                        i > 0 && i < sourceSpectrums[sourceIndex].Length)
                        combinedSpectrum[i] += sourceSpectrums[sourceIndex][i];
                    }
                }

                // Ȱ�� ����� �ҽ� ���� ������ (0���� ������ ����)
                int activeSources = GetActiveSourceCount();
                if (activeSources > 0)
                {
                    for (int i = 0; i < spectrumSize; i++)
                    {
                        combinedSpectrum[i] /= activeSources;
                    }
                }
                break;

            case MixingMethod.Maximum:
                // �� ���ļ����� �ִ밪 ���
                for (int sourceIndex = 0; sourceIndex < audioSources.Count; sourceIndex++)
                {
                    for (int i = 0; i < spectrumSize; i++)
                    {
                        combinedSpectrum[i] = Mathf.Max(combinedSpectrum[i], sourceSpectrums[sourceIndex][i]);
                    }
                }
                break;
        }
    }

    // Ȱ�� ������ ����� �ҽ� ���� ��ȯ
    private int GetActiveSourceCount()
    {
        int count = 0;
        foreach (var source in audioSources)
        {
            if (source != null && source.m_AudioSource.isPlaying)
            {
                count++;
            }
        }
        return count;
    }

    #endregion

    #region Main System Public Functions (**Update Calculate Audio Spectrum**)

    void CalculateFrequencyBands(int _BarRtLeg, System.Action<int, float> _UpdataCallBack) //���ٸ� ������� ��� Target�� 8���� ���� �����Ѵ�
    {
        // �� ���뺰�� �ش��ϴ� ���ļ� ������ ������ ����� ���
        for (int barIndex = 0; barIndex < _BarRtLeg; barIndex++)
        {
            float lowFreq = GetBandLowerFrequency(barIndex, _BarRtLeg);
            float highFreq = GetBandUpperFrequency(barIndex, _BarRtLeg);

            int lowIndex = FrequencyToSpectrumIndex(lowFreq);
            int highIndex = FrequencyToSpectrumIndex(highFreq);

            // �ε��� ���� ����
            lowIndex = Mathf.Clamp(lowIndex, 0, spectrumSize - 1);
            highIndex = Mathf.Clamp(highIndex, 0, spectrumSize - 1);

            // �ּ� 1���� ������ ���Եǵ���
            highIndex = Mathf.Max(highIndex, lowIndex + 1);

            float sum = 0f;
            int sampleCount = 0;

            // �ش� ���ļ� �뿪�� ��� ���� �ջ�
            for (int i = lowIndex; i <= highIndex; i++)
            {
                sum += combinedSpectrum[i];
                sampleCount++;
            }

            // ��� ��� (0���� ������ ����)
            float average = sampleCount > 0 ? sum / sampleCount : 0;

            // ������ �������� ���� ���� �� ���̰�
            average = Mathf.Sqrt(average);

            // Ÿ�� ���� ����
            _UpdataCallBack(barIndex, average);
            //targetHeights[barIndex] = average * _MultiplierSped;
        }
    }

    // ���� �ε����� ���� ���� ���ļ� ���
    float GetBandLowerFrequency(int barIndex, int totalBars)
    {
        // 20Hz ~ 20000Hz ������ �α� �����Ϸ� ����
        float minFreq = 20f;
        float maxFreq = m_MaxFreq;//20000f;
        float logMin = Mathf.Log10(minFreq);
        float logMax = Mathf.Log10(maxFreq);

        float t = (float)barIndex / totalBars;
        float logFreq = Mathf.Lerp(logMin, logMax, t);
        return Mathf.Pow(10f, logFreq);
    }

    // ���� �ε����� ���� ���� ���ļ� ���
    float GetBandUpperFrequency(int barIndex, int totalBars)
    {
        return GetBandLowerFrequency(barIndex + 1, totalBars);
    }

    int FrequencyToSpectrumIndex(float freq)
    {
        float nyquist = AudioSettings.outputSampleRate / 2f;
        return Mathf.FloorToInt(freq / nyquist * spectrumSize);
    }

    #endregion

    #region Sub System Private Function (**TestOnly Reference**)

    private void TestForceApplyMusic()
    {
        if (!m_isTestMode) return;

        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (test_ListenaudioSources.Count <= test_CoundIDX) return;
            m_isActionProcess = false;
            AddAudioSource(test_ListenaudioSources[test_CoundIDX]);
            test_CoundIDX++;
            m_isActionProcess = true;
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            if (test_CoundIDX <= 0) return;
            m_isActionProcess = false;
            test_CoundIDX--;
            RemoveAudioSource(test_ListenaudioSources[test_CoundIDX]);
            m_isActionProcess = true;
        }
    }

    #endregion

    #region ����Ƽ �̺�Ʈ �Լ�

    public override void ProcessUpdate()
    {
        TestForceApplyMusic();

        if (!m_isActionProcess) return;

        // ��� ����� �ҽ����� ����Ʈ�� ������ ��������
        GetAllSpectrumData();

        // ����Ʈ�� ������ ��ġ�� (������ �ͽ� ��Ŀ� ����)
        CombineSpectrumData();

        m_OutPutAudioEqulList.ForEach(x =>
        CalculateFrequencyBands(x.pp_IDXArray,
        (_AIDX, _Value) => x.ApplyTargetHeights(_AIDX, _Value)));
    }

    #endregion
}