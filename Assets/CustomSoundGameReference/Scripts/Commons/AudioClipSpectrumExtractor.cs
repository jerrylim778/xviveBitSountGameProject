using System.Collections.Generic;
using System.Numerics; // System.Numerics for Complex
using System;
using UnityEngine;
using Commons.Helpers;


[CreateAssetMenu(fileName = nameof(AudioClipSpectrumExtractor),
menuName = "DefultScriptableObject/" + nameof(AudioClipSpectrumExtractor), order = int.MaxValue)]
public class AudioClipSpectrumExtractor : ScriptableObject
{
    [SerializeField] private SDataAlbumLevelGameInfo m_AlbumLevelGameInfo;
    [SerializeField] private SDataSoundGameInfo m_SDataSoundGameInfo;
    [SerializeField] private List<WebClipBitSavedInfo> m_WebClipBitSavedInfo = new();

    public List<WebClipBitSavedInfo> pp_WebClipBitSavedInfo => m_WebClipBitSavedInfo;

    #region 현재는 런타임에서 비트를 가져옴 (**해당 방식 사용안함**)
    [SerializeField] private int fftWindowSize = 1024; // 예: 1024 또는 2048
    [SerializeField] private int sampleRate = 44100;

    //[Button("ODIN_AddDefultCharacterInfo")]
    //private void ODIN_AddDefultCharacterInfo() => 
    //m_WebClipBitSavedInfo.ForEach(x => 
    //{
    //    var pair = SavedData(x.s_SetClips);
    //    x.s_OutPutClipWave = pair.Item1;
    //    pair.Item2.ForEach(y => x.s_OutPutClipBit.Add(new BitInfo(y)));
    //});
    #endregion

    public void CreateOutParsingClip(AudioClip _GetClip)
    {
        var _GetClipInfo = new WebClipBitSavedInfo();
        _GetClipInfo.s_SetClips = _GetClip;
        var pair = SavedData(_GetClipInfo.s_SetClips);
        _GetClipInfo.s_OutPutClipWave = pair.Item1;
        pair.Item2.ForEach(y => _GetClipInfo.s_OutPutClipBit.Add(new BitInfo(y)));
        m_WebClipBitSavedInfo.Add(_GetClipInfo);
    }

    public void CreateOutParsingAudioSorce(AudioClip _GetClip, List<float[]> _GetBitList)
    {
        var _GetClipInfo = new WebClipBitSavedInfo();
        _GetClipInfo.s_SetClips = _GetClip;
        _GetBitList.ForEach(y => _GetClipInfo.s_OutPutClipBit.Add(new BitInfo(y)));
        m_WebClipBitSavedInfo.Add(_GetClipInfo);
        FindSDataSoundGameInfo(_GetClip.name, _GetClipInfo.s_OutPutClipBit);
    }

    private void FindSDataSoundGameInfo(string _GetClipName, List<BitInfo> _GetBitList)
    {
        if (m_SDataSoundGameInfo.pp_DSSoundgameClipInfos.
        ISFindCondition(x => x.s_AudioClipsInfo.CheckArrayNull(0) &&
        x.s_AudioClipsInfo[0].name == _GetClipName, out SoundGameClipInfo _GetInfo))
        _GetInfo.s_ReciveBitInfoByClipName = _GetBitList;
    }

    private void FindSDataLevelSoundGameInfo(string _GetClipName, List<BitInfo> _GetBitList)
    {
        if (m_AlbumLevelGameInfo.pp_DSAlbumLevelInfos.
        ISFindCondition(x => x.m_MainClipBGM.s_AudioClipsInfo.CheckArrayNull(0) &&
        x.m_MainClipBGM.s_AudioClipsInfo[0].name == _GetClipName, out AlbumInfo _GetInfo))
        _GetInfo.m_MainClipBGM.s_ReciveBitInfoByClipName = _GetBitList;
    }

    public void ForceApplyGameInfo() => m_WebClipBitSavedInfo.ForEach(x =>
    FindSDataSoundGameInfo(x.s_SetClips.name, x.s_OutPutClipBit));

    public void ForceApplyAlbumInfo() => m_WebClipBitSavedInfo.ForEach(x =>
    FindSDataLevelSoundGameInfo(x.s_SetClips.name, x.s_OutPutClipBit));

    #region 정적으로 저장하는 형태가 제대로 작동되지 않음 (보류)
    private System.Tuple<float[], List<float[]>> SavedData(AudioClip _SetClips)
    {
        float[] samples = new float[_SetClips.samples * _SetClips.channels];
        _SetClips.GetData(samples, 0);

        int frameCount = samples.Length / fftWindowSize;
        Debug.Log($"총 프레임 수: {frameCount}");

        var GetBitList = new List<float[]>();

        for (int i = 0; i < frameCount; i++)
        {
            float[] frame = new float[fftWindowSize];
            Array.Copy(samples, i * fftWindowSize, frame, 0, fftWindowSize);

            float[] spectrum = ComputeFFT(frame);
            GetBitList.Add(spectrum);
            // 여기에 spectrum 데이터를 저장하거나 시각화 가능
            Debug.Log($"[{i}] - Peak: {spectrum[0]} ...");
        }
        return new(samples, GetBitList);
    }

    // 간단한 FFT (Mono 채널 기준)
    private float[] ComputeFFT(float[] data)
    {
        int N = data.Length;
        Complex[] fftBuffer = new Complex[N];

        for (int i = 0; i < N; i++)
            fftBuffer[i] = new Complex(data[i], 0.0);

        FFT(fftBuffer);

        float[] spectrum = new float[N / 2];
        for (int i = 0; i < N / 2; i++)
            spectrum[i] = (float)fftBuffer[i].Magnitude;

        return spectrum;
    }

    // Cooley-Tukey FFT
    private void FFT(Complex[] buffer)
    {
        int N = buffer.Length;
        if (N <= 1) return;

        Complex[] even = new Complex[N / 2];
        Complex[] odd = new Complex[N / 2];

        for (int i = 0; i < N / 2; i++)
        {
            even[i] = buffer[i * 2];
            odd[i] = buffer[i * 2 + 1];
        }

        FFT(even);
        FFT(odd);

        for (int k = 0; k < N / 2; k++)
        {
            Complex t = Complex.Exp(-Complex.ImaginaryOne * 2.0 * Math.PI * k / N) * odd[k];
            buffer[k] = even[k] + t;
            buffer[k + N / 2] = even[k] - t;
        }
    }
    #endregion
}

[System.Serializable]
public class WebClipBitSavedInfo
{
    public AudioClip s_SetClips;
    public float[] s_OutPutClipWave;
    public List<BitInfo> s_OutPutClipBit = new();
}

[System.Serializable]
public struct BitInfo : I_Data
{
    public float[] s_MicroBitInfo;

    public BitInfo(float[]  _GetBitInfo)
    {
        s_MicroBitInfo = _GetBitInfo;
    }
}
