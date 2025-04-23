using UnityEngine;

[System.Serializable]
public class ElemASBuffer : I_Buffer
{
    public AudioSource m_AudioSource;
    public SoundGameClipInfo m_ASClipInfo;
    //public bool m_isListenMusic = false;

    public ElemASBuffer(/*bool _isListenMusic,*/ AudioSource _AudioSource, SoundGameClipInfo _ASClipInfo)
    {
        m_AudioSource = _AudioSource;
        m_ASClipInfo = _ASClipInfo;
        //m_isListenMusic = _isListenMusic;
    }

    //public bool IsListenOrPaused()
    //=> !m_AudioSource.isPlaying && (m_AudioSource.time > 0f || m_isListenMusic);


    public float[] UpdateThisAnimClipLeg()
    {
        if (!m_AudioSource.isPlaying) return null;

        var audioSource = m_AudioSource;
        if (audioSource.isPlaying && audioSource.clip != null)
        {
            var GetListByBit = m_ASClipInfo.s_ReciveBitInfoByClipName;
            // 현재 재생 시간 가져오기
            float currentTime = audioSource.time;
            // 현재 재생 백분율 계산 (0.0 ~ 1.0)
            float playbackPercentage = currentTime / audioSource.clip.length;
            // 배열에서 해당 백분율에 맞는 인덱스 계산
            int index = Mathf.Clamp(Mathf.FloorToInt(playbackPercentage * GetListByBit.Count), 0, GetListByBit.Count - 1);
            // 계산된 인덱스로 배열의 값 가져오기
            return index > -1 && index < GetListByBit.Count ?
            GetListByBit[index].s_MicroBitInfo : null;
            // 값 사용 예시
            //Debug.Log($"현재 시간: {currentTime}, 백분율: {playbackPercentage:P2}, 인덱스: {index}, 값: {value}");
        }
        return null;
    }
}
