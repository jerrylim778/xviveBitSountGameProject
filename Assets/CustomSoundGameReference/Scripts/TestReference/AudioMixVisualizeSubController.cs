using UnityEngine;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using Commons.Helpers;
using System;

public class AudioMixVisualizeSubController : SubModuleControllerBase
{
    // 믹싱 방식 열거형
    public enum MixingMethod
    {
        Sum,        // 모든 오디오 소스 스펙트럼의 합
        Average,    // 모든 오디오 소스 스펙트럼의 평균
        Maximum     // 각 주파수 대역의 최대값 사용
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

    [Tooltip("FFT 윈도우 유형")]
    public FFTWindow fftWindow = FFTWindow.BlackmanHarris;

    [Tooltip("스펙트럼 크기 (2의 거듭제곱 권장: 128, 256, 512, 1024, 2048)")]
    public int spectrumSize = 512;

    [Tooltip("오디오 소스 믹싱 방식")]
    public MixingMethod mixingMethod = MixingMethod.Average;

    [Tooltip("주파수 대역 디버깅 정보 표시")]
    public bool showDebugInfo = false;

    // 각 오디오 소스별 스펙트럼 저장용 배열
    private float[][] sourceSpectrums;
    private float[] combinedSpectrum; // 최종 합산/믹싱된 스펙트럼

    public override object[] Initlization(params object[] _ParsingParams)
    {
        var PartParams = Initlization(_ParsingParams);

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
        // 오디오 소스별 스펙트럼 배열 초기화
        sourceSpectrums = new float[audioSources.Count][];
        for (int i = 0; i < audioSources.Count; i++)
        {
            sourceSpectrums[i] = new float[spectrumSize];
        }

        // 합쳐진 스펙트럼 및 애니메이션 배열 초기화
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
            // 배열 재초기화
            RecycleArrays();
        }
    }

    
    public void RemoveAudioSource(ElemASBuffer source)
    {
        if (audioSources.Contains(source))
        {
            audioSources.Remove(source);
            if(m_isTestMode) source.m_AudioSource.Stop();
            // 배열 재초기화
            RecycleArrays();
        }
    }

    // 믹싱 방식 변경
    public void SetMixingMethod(MixingMethod method) => mixingMethod = method;
    #endregion

    #region Main System Public Functions (**Update Check Curr Audio Lite State**)

    private void GetAllSpectrumData()
    {
        // 각 오디오 소스에서 스펙트럼 데이터 가져오기
        for (int i = 0; i < audioSources.Count; i++)
        {
            if (audioSources[i] != null && audioSources[i].m_AudioSource.isPlaying)
            {
                //audioSources[i].GetSpectrumData(sourceSpectrums[i], 0, fftWindow);
                sourceSpectrums[i] = audioSources[i].UpdateThisAnimClipLeg();
            }
            else // 재생 중이 아니거나 null인 소스는 0으로 초기화
            {
                if (audioSources[i] != null && !audioSources[i].IsListenOrPaused()) audioSources.RemoveAt(i);
                System.Array.Clear(sourceSpectrums[i], 0, sourceSpectrums[i].Length);
            }
        }
    }

    private void CombineSpectrumData()
    {
        // 먼저 결합된 스펙트럼 초기화
        System.Array.Clear(combinedSpectrum, 0, combinedSpectrum.Length);

        // 오디오 소스가 없으면 종료
        if (audioSources.Count == 0) return;

        // 선택된 믹싱 방식에 따라 스펙트럼 결합
        switch (mixingMethod)
        {
            case MixingMethod.Sum:
                // 모든 스펙트럼 값을 더함
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
                // 모든 스펙트럼 값의 평균 계산
                for (int sourceIndex = 0; sourceIndex < audioSources.Count; sourceIndex++)
                {
                    for (int i = 0; i < spectrumSize; i++)
                    {
                        if(sourceSpectrums[sourceIndex].CheckArrayNull(0) &&
                        i > 0 && i < sourceSpectrums[sourceIndex].Length)
                        combinedSpectrum[i] += sourceSpectrums[sourceIndex][i];
                    }
                }

                // 활성 오디오 소스 수로 나누기 (0으로 나누기 방지)
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
                // 각 주파수에서 최대값 사용
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

    // 활성 상태인 오디오 소스 개수 반환
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

    void CalculateFrequencyBands(int _BarRtLeg, System.Action<int, float> _UpdataCallBack) //없다면 계산해준 모든 Target를 8개로 임의 산정한다
    {
        // 각 막대별로 해당하는 주파수 범위의 에너지 평균을 계산
        for (int barIndex = 0; barIndex < _BarRtLeg; barIndex++)
        {
            float lowFreq = GetBandLowerFrequency(barIndex, _BarRtLeg);
            float highFreq = GetBandUpperFrequency(barIndex, _BarRtLeg);

            int lowIndex = FrequencyToSpectrumIndex(lowFreq);
            int highIndex = FrequencyToSpectrumIndex(highFreq);

            // 인덱스 범위 보정
            lowIndex = Mathf.Clamp(lowIndex, 0, spectrumSize - 1);
            highIndex = Mathf.Clamp(highIndex, 0, spectrumSize - 1);

            // 최소 1개의 샘플은 포함되도록
            highIndex = Mathf.Max(highIndex, lowIndex + 1);

            float sum = 0f;
            int sampleCount = 0;

            // 해당 주파수 대역의 모든 샘플 합산
            for (int i = lowIndex; i <= highIndex; i++)
            {
                sum += combinedSpectrum[i];
                sampleCount++;
            }

            // 평균 계산 (0으로 나누기 방지)
            float average = sampleCount > 0 ? sum / sampleCount : 0;

            // 제곱근 적용으로 낮은 값도 잘 보이게
            average = Mathf.Sqrt(average);

            // 타겟 높이 설정
            _UpdataCallBack(barIndex, average);
            //targetHeights[barIndex] = average * _MultiplierSped;
        }
    }

    // 막대 인덱스에 따른 하한 주파수 계산
    float GetBandLowerFrequency(int barIndex, int totalBars)
    {
        // 20Hz ~ 20000Hz 구간을 로그 스케일로 분할
        float minFreq = 20f;
        float maxFreq = m_MaxFreq;//20000f;
        float logMin = Mathf.Log10(minFreq);
        float logMax = Mathf.Log10(maxFreq);

        float t = (float)barIndex / totalBars;
        float logFreq = Mathf.Lerp(logMin, logMax, t);
        return Mathf.Pow(10f, logFreq);
    }

    // 막대 인덱스에 따른 상한 주파수 계산
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

    #region 유니티 이벤트 함수

    public override void ProcessUpdate()
    {
        TestForceApplyMusic();

        if (!m_isActionProcess) return;

        // 모든 오디오 소스에서 스펙트럼 데이터 가져오기
        GetAllSpectrumData();

        // 스펙트럼 데이터 합치기 (선택한 믹싱 방식에 따라)
        CombineSpectrumData();

        m_OutPutAudioEqulList.ForEach(x =>
        CalculateFrequencyBands(x.pp_IDXArray,
        (_AIDX, _Value) => x.ApplyTargetHeights(_AIDX, _Value)));
    }

    #endregion
}