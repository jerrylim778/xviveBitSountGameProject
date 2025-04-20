using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

public class TestAnyFunc_AudioVisualer : MonoBehaviour
{
    [SerializeField] private AudioClipSpectrumExtractor m_AudioClipSpectrumExtractor;
    [SerializeField] private WebClipBitSavedInfo m_GetCurrBitInfos;
    [SerializeField, ReadOnly] private bool m_isProcessAction;
    [SerializeField, Range(0f, 20000f)] private float m_MaxFreq;

    public AudioSource audioSource;
    public RectTransform[] bars;
    public float heightMultiplier = 500f;
    public FFTWindow fftWindow = FFTWindow.BlackmanHarris;
    public int spectrumSize = 512;
    private float[] spectrum;
    
    

    // 평활화를 위한 변수 추가
    public float smoothSpeed = 5f;
    private float[] targetHeights;
    private float[] currentHeights;

    // 주파수 대역 디버깅용
    public bool showDebugInfo = false;

    void Start()
    {
        audioSource.playOnAwake = false;
        spectrum = new float[spectrumSize];
        targetHeights = new float[bars.Length];
        currentHeights = new float[bars.Length];
        audioSource.Stop();
    }

    private void InitThisASInfo()
    {
        m_GetCurrBitInfos =
        m_AudioClipSpectrumExtractor.
        pp_WebClipBitSavedInfo.Find(x => x.s_SetClips.name == audioSource.clip.name);
    }

    private float[] UpdateThisAnimClipLeg(BitInfo[] valueArray)
    {
        if (audioSource.isPlaying && audioSource.clip != null)
        {
            // 현재 재생 시간 가져오기
            float currentTime = audioSource.time;
            // 현재 재생 백분율 계산 (0.0 ~ 1.0)
            float playbackPercentage = currentTime / audioSource.clip.length;
            // 배열에서 해당 백분율에 맞는 인덱스 계산
            int index = Mathf.Clamp(Mathf.FloorToInt(playbackPercentage * valueArray.Length), 0, valueArray.Length - 1);
            // 계산된 인덱스로 배열의 값 가져오기
            return index > -1 && index < valueArray.Length ?
            valueArray[index].s_MicroBitInfo : null;
            // 값 사용 예시
            //Debug.Log($"현재 시간: {currentTime}, 백분율: {playbackPercentage:P2}, 인덱스: {index}, 값: {value}");
        }
        return null;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            m_isProcessAction = false;
            InitThisASInfo();
            if (audioSource.isPlaying) audioSource.Stop();
            audioSource.Play();
            m_isProcessAction = true;
        }

        if (!m_isProcessAction) return;

        //audioSource.GetSpectrumData(spectrum, 0, fftWindow);
        spectrum = UpdateThisAnimClipLeg(m_GetCurrBitInfos.s_OutPutClipBit.ToArray());

        // 주파수 밴드별 에너지 계산   
        CalculateFrequencyBands();

        // 부드러운 애니메이션 적용
        for (int i = 0; i < bars.Length; i++)
        {
            currentHeights[i] = Mathf.Lerp(currentHeights[i], targetHeights[i], Time.deltaTime * smoothSpeed);

            Vector2 size = bars[i].sizeDelta;
            size.y = Mathf.Clamp(currentHeights[i], 2f, 300f);
            bars[i].sizeDelta = size;

            if (showDebugInfo /*&& i < 5*/) // 처음 5개 막대의 디버그 정보만 표시
            {
                float freq = GetBandLowerFrequency(i, bars.Length);
                float freqHigh = GetBandUpperFrequency(i, bars.Length);
                Debug.Log($"Bar {i}: {freq:F0}Hz - {freqHigh:F0}Hz, Height: {size.y:F2}");
            }
        }
    }

    void CalculateFrequencyBands()
    {
        // 각 막대별로 해당하는 주파수 범위의 에너지 평균을 계산
        for (int barIndex = 0; barIndex < bars.Length; barIndex++)
        {
            float lowFreq = GetBandLowerFrequency(barIndex, bars.Length);
            float highFreq = GetBandUpperFrequency(barIndex, bars.Length);

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
                if(spectrum != null && i > 0 && i < spectrum.Length) sum += spectrum[i];
                sampleCount++;
            }

            // 평균 계산 (0으로 나누기 방지)
            float average = sampleCount > 0 ? sum / sampleCount : 0;

            // 제곱근 적용으로 낮은 값도 잘 보이게 (선택사항)
            average = Mathf.Sqrt(average);

            // 타겟 높이 설정
            targetHeights[barIndex] = average * heightMultiplier;
        }
    }

    // 막대 인덱스에 따른 하한 주파수 계산
    float GetBandLowerFrequency(int barIndex, int totalBars)
    {
        // 20Hz ~ 20000Hz 구간을 로그 스케일로 분할
        float minFreq = 20f;
        float maxFreq = m_MaxFreq; //10000f;//20000f;
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
}