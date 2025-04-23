using UnityEngine;
using Sirenix.OdinInspector;
using Commons.Helpers;

[System.Serializable]
public class AudioASInfo
{
    private I_OwnerShip s_ReciveOwnerKeyObject;

    [field: SerializeField] public AudioInfo? s_AudioInfo { get; private set; }
    [field: SerializeField] public AudioSource s_AudioSource { get; private set; }
    //현재는 삭제하는것으로 대채함 (이후엔 모두 ObjectPool패턴으로 정석로직으로 재설계 해야됨 (**리팩요망**)
    [field: SerializeField, ReadOnly] public bool spp_isReturnPool { get; private set; }
    [field: SerializeField, ReadOnly, ShowIf(nameof(odin_isShowRuningASInfoNow))] public bool spp_isPlayed { get; private set; }
    //[field: SerializeField, ReadOnly, ShowIf(nameof(odin_isShowRuningASInfoNow))] public System.TimeSpan spp_DSPTimeNow { get; private set; }
    [field: SerializeField, ReadOnly, ShowIf(nameof(odin_isShowRuningASInfoNow))] public double spp_DSPTimeNow { get; private set; }
    [field: SerializeField, ReadOnly, ShowIf(nameof(odin_isShowRuningASInfoNow))] public float spp_MaxPlayFreame { get; private set; }

    [SerializeField, ReadOnly, ShowIf(nameof(odin_isShowRuningASInfoNow))] public float s_CurrPlayFrame;
    public bool spp_isHaveOwnerKey => s_ReciveOwnerKeyObject != null;

    [Tooltip("Support Values")]
    private double s_PauseStartTime, s_TotalPausedDuration;

    #region ODIN_Support
    private bool odin_isShowRuningASInfoNow = false;
    public void ODIN_ShowAllRunningASInfo(bool _isOn) => odin_isShowRuningASInfoNow = _isOn;
    #endregion

    #region Sub AS Control Data Reference (사용하지 않음 이후 삭제 요망)

    //public enum ASSubControlType { None = 0, ApplyPosObjType }
    //[field: SerializeField, ReadOnly] public ASSubControlType spp_ASRunSubControlType { get; private set; }
    //private bool odin_isASCObjType { get => spp_ASRunSubControlType == ASSubControlType.ApplyPosObjType; }
    //[field: SerializeField, ReadOnly, ShowIf(nameof(odin_isASCObjType))] public bool spp_isASCFollow { get; private set; }
    //[field: SerializeField, ReadOnly, ShowIf(nameof(odin_isASCObjType))] public GameObject spp_ASCObj { get; private set; }

    #endregion

    public bool spp_isPlaying { get => s_AudioSource != null && s_AudioSource.isPlaying && s_AudioInfo != null && s_AudioInfo.HasValue; }

    public AudioASInfo(bool _isPoolRecive, AudioSource _SetApplyAS)
    {
        spp_isReturnPool = _isPoolRecive;
        s_AudioSource = _SetApplyAS;
    }

    public AudioASInfo(bool _isPoolRecive, AudioSource _SetApplyAS, I_OwnerShip _GetOwnerObject)
    {
        s_ReciveOwnerKeyObject = _GetOwnerObject;
        spp_isReturnPool = _isPoolRecive;
        s_AudioSource = _SetApplyAS;
    }

    //직렬화가 이미 이루어진 경우 따로 생성자 함수를 통해 초기화를 진행한다
    public void SerializeInitlization(bool _isPoolRecive, AudioSource _SetApplyAS, I_OwnerShip _GetOwnerObject)
    {
        s_ReciveOwnerKeyObject = _GetOwnerObject;
        spp_isReturnPool = _isPoolRecive;
        s_AudioSource = _SetApplyAS;
    }

    //Init Reference
    public void ApplyOrResetOwner(I_OwnerShip _GetShip = null)
    => s_ReciveOwnerKeyObject = _GetShip;

    public void UnConnectOwnerObjectKey(I_OwnerShip _GetOwnerObject)
    {
        if ((!s_ReciveOwnerKeyObject.Equals(_GetOwnerObject).HDebug("[논리오류]", Helper.HDType.Error))) return;

        s_ReciveOwnerKeyObject = null;
    }

    public void SetPlay(AudioInfo _GetASInfo, bool _isDirPlay)
    {
        s_AudioInfo = _GetASInfo;
        s_AudioSource.clip = _GetASInfo.s_AudioClip;
        s_CurrPlayFrame = 0f; spp_isPlayed = false;
        //spp_DSPTimeNow = System.TimeSpan.FromSeconds(AudioSettings.dspTime);
        spp_DSPTimeNow = AudioSettings.dspTime;
        spp_MaxPlayFreame = s_AudioInfo.Value.s_AudioClip.length * 60f;
        InitSubControlByASInfo(true);
        if (_isDirPlay)
        {
            //if(s_AudioSource.isPlaying)
            s_AudioSource.Stop(); s_AudioSource.Play();
        }
        //else s_AudioSource.Stop();
    }

    public void StopOrComplate(System.Action _EndCallBack = null)
    {
        s_AudioSource.Stop();
        InitSubControlByASInfo(false);
        spp_isPlayed = true; s_AudioInfo = null;
        //s_CurrPlayFrame = 0f; //spp_MaxPlayFreame = 0f;
        ApplyOrResetOwner(null);
        _EndCallBack?.Invoke();
        if (spp_isReturnPool) Object.Destroy(this.s_AudioSource.gameObject);
    }

    public void ElemSoundPlayOrPause(bool _isOn/*, bool _isOnlyCheckPause*/, I_CustomSoundEffect _ApplyEffect = null)
    {
        if (_isOn)
        {
            if (!s_AudioSource.isPlaying)
            {
                s_AudioSource.Stop();
                s_AudioSource.Play();
            }
            else 
                s_AudioSource.UnPause();
            return;
        }
        if (spp_isPlaying && _ApplyEffect != null)
            _ApplyEffect.SoundEffect(false, s_AudioSource);
        s_AudioSource.Pause();
    }

    public void Restart(System.Action _EndCallBack = null)
    {
        if (s_AudioInfo == null || !s_AudioInfo.HasValue) return;

        if (!s_AudioSource.isPlaying) s_AudioSource.Stop();
        s_AudioSource.Play();
    }

    //정지와 일시 정지를 구분하기 위함
    public bool IsPaused()
    => !s_AudioSource.isPlaying && s_AudioSource.time > 0f;

    #region Audio관리자 or 외부에서 따로 가져가 계산함 (SoundGame Project 참고)

    //public float ElemClipCountCalculate()
    //{
    //    if (s_CurrPlayFrame <= 0f)
    //    {
    //        spp_DSPTimeNow = AudioSettings.dspTime;
    //        s_CurrPlayFrame = 0;
    //    }
    //    System.TimeSpan GetTS = System.TimeSpan.FromSeconds(AudioSettings.dspTime - spp_DSPTimeNow - s_TotalPausedDuration);
    //    float CalculateTime = (float)GetTS.TotalSeconds * 60;
    //    s_CurrPlayFrame = Mathf.Abs(spp_MaxPlayFreame - CalculateTime);
    //    //Debug.Log($" {s_CurrPlayFrame}::{ spp_DSPTimeNow}:::{s_TotalPausedDuration}");
    //    return s_CurrPlayFrame;
    //}

    #endregion

    private void InitSubControlByASInfo(bool _isStart)
    {
        if (s_AudioInfo == null || !s_AudioInfo.HasValue)
            return;

        s_AudioSource.loop = _isStart ? s_AudioInfo.Value.s_isLoop : false;
        s_AudioSource.volume = _isStart ? s_AudioInfo.Value.s_Volume : 1f;

        switch (s_AudioInfo.Value.s_SubSData)
        {
            case AudioWPInfo AWPInfo:
                if (!AWPInfo.s_FollowSound)
                {
                    s_AudioSource.transform.position = _isStart ?
                    AWPInfo.s_Owner.transform.position : Vector3.zero;
                    return;
                }
                //이후 소리가 각 인스턴싱에 따라 따로올 수 있도록 수정한다 (업데이트는 따로 Manager로 돌리거나 코루틴으로 돌릴것)
                break;
        }
    }
}


[System.Serializable]
public struct AudioInfo
{
    [Header("Data Reference")]
    public string s_AudioName;
    public AudioType s_AudioType;
    public I_SubASCData s_SubSData;
    [Header("Data Reference")]
    public AudioClip s_AudioClip;
    public bool s_isAwakeOnPlay, s_isLoop;
    public float s_Volume;
    public float s_DistanceSoundMin;
    public float s_DistanceSoundMax;

    public AudioInfo(string _AudioName, AudioType _AudioType, AudioClip _AudioClip,
    bool _isLoop, float _Volume)
    {
        s_AudioName = _AudioName;
        s_AudioType = _AudioType;
        s_AudioClip = _AudioClip;
        s_Volume = _Volume;
        s_isLoop = _isLoop;
        s_SubSData = null;
        s_isAwakeOnPlay = false;
        s_DistanceSoundMin = 1f;
        s_DistanceSoundMax = 200f;
    }
}

#region Sub Control Data

[System.Flags]
public enum AudioType
{
    None = 0,
    BGM = 1 << 0,
    Voice = 1 << 1,
    WPSFX = 1 << 2,
    UISFX = 1 << 3,
    GameSFX = 1 << 4
}

public interface I_OwnerShip { } //Do Not Define,,,
public interface I_SubASCData { } //Do Not Define,,,

public struct AudioWPInfo : I_SubASCData
{
    public bool s_FollowSound;
    public GameObject s_Owner;

    public AudioWPInfo(bool _FollowSound, GameObject _Owner)
    {
        s_FollowSound = _FollowSound;
        s_Owner = _Owner;
    }
}

#endregion
