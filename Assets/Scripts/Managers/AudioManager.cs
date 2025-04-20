using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Audio;
using Sirenix.OdinInspector;
using Commons.Helpers;

[DisallowMultipleComponent]
public class AudioManager : MonoBehaviour
{
    public enum AudioSupportType
    {
        None = 0,
        ObjectPoolType,
        AddChannel
    }

    public enum AudioOutPutSettingType 
    {
        InstanceObj,
        AddList,
        DestroyListAndObj
    }

    //타입에 따라 Resources의 경우 초기 로딩을 구현하되
    //사용하는 객체 (Clip들만 가져와 사용할것) 만 메모리에 올릴 수 있도록 코드의 방향성을 잡을것 (로딩 필요 x)
    [Header("AudioControl")]
    private bool m_isShowAllASInfo = false;
    [ShowInInspector, PropertyOrder(int.MinValue)] public bool pp_isShowAllASInfo
    { get => m_isShowAllASInfo; set { m_isShowAllASInfo = value; ODIN_ShowAllAsInfo(m_isShowAllASInfo); } }

    [Header("AudioOutPutByType Reference")]
    #region 오디오 서포트에 관한 설명 (구현 이후 문제없음 삭제 요망)
    //풀 사용에 대한 전체적인 리팩토링 (풀을 사용하되 사용하면 최대 10이하로 풀을 형성할 수 있도록 수정한다)
    //만일 그것도 문제가 된다면 계속 
    //다른 타입으로는 채널수를 강제로 초기화때 늘리는 방식이다 해당 방식을 사용하면 풀로 인한 예외는 현져히 줄게 된다
    #endregion
    [SerializeField] private AudioSupportType m_AudioOutPutType;
    [SerializeField, Range(1, 10), ShowIf(nameof(ODIN_MaxCountByOutPutType))] private int m_AddMaxCount = 1;
    #region ODIN Reference By AudioOutPutType
    private bool ODIN_MaxCountByOutPutType() => m_AudioOutPutType != AudioSupportType.None;
    #endregion


    [Header("RequiredValues")]
    [SerializeField] private SDataLoadParsingType m_ParsingLoadType;
    [SerializeField] private AudioSource m_PrefabASChanelModule;

    [Header("AllSoundReference")]
    [SerializeField] private AudioMixer m_MainMixerGP;
    [TabGroup("AudioGroup", "BGM")]
    [SerializeField] AudioASInfo m_BGMASInfos;
    [TabGroup("AudioGroup", "Voice")]
    [SerializeField] AudioASInfo m_VoiceASInfos;
    [TabGroup("AudioGroup", "GameSFX")] //이후에 GameData<클래스>를 ItemInfo로 적용하여 해당 구간에서 가져오는 방식으로 리팩 요망 ***************************
    [SerializeField] AudioASInfo[] m_GameSFXASInfos; 
    [TabGroup("AudioGroup", "SFX")]
    [SerializeField] AudioASInfo[] m_SFXASInfos;
    [TabGroup("AudioGroup", "UISFX")]
    [SerializeField] AudioASInfo[] m_UISFXASInfos;

    [Header("RequiredValues")]
    [SerializeField, ReadOnly] private DataManager m_Datamanager;
    #region OdinDropDown => MFLGameLevelInfo
    private IEnumerable<ValueDropdownItem<AudioInfo>> ODIN_DropDownAudioResources() =>
    Helper.HODIN_ClassListAsDropdown<AudioInfo>(SavedAudioInfos, _GetItem => $"{_GetItem.s_AudioName} 오디오 클립: {_GetItem.s_AudioClip}");
    [Button("ODIN_AudioResources")] private void ODIN_AudioResources() => SavedAudioInfos.HODIN_AddItemByList<AudioInfo>();
    #endregion
    [SerializeField, InlineProperty, HideLabel, ValueDropdown(nameof(ODIN_DropDownAudioResources))] private List<AudioInfo> SavedAudioInfos = new();

    [Tooltip("RequiredValues")]
    private List<AudioASInfo> m_GetAllASForControls = new();
    private bool m_isProcessActions = false;
    [Tooltip("Required Count OutPut Type")]
    private List<AudioASInfo> m_ListenDestroyByOutPutType = new();
    private Dictionary<AudioType, int> m_DicCountByType; 

    public void Initlization()
    {
        m_Datamanager = Appinstance.Instance.ms_DataManager;
        m_DicCountByType = new() { { AudioType.WPSFX, 0 }, { AudioType.UISFX, 0 }, { AudioType.GameSFX, 0 } };
        m_GetAllASForControls.Add(m_BGMASInfos); m_GetAllASForControls.Add(m_VoiceASInfos); 
        m_GetAllASForControls.AddRange(m_GameSFXASInfos); m_GetAllASForControls.AddRange(m_SFXASInfos); m_GetAllASForControls.AddRange(m_UISFXASInfos);

        #region AS Parsing & Type Of Init

        if (m_ParsingLoadType == SDataLoadParsingType.ResourcesLoad)
        {
            AudioType[] GetLoadTypes = new AudioType[3] { AudioType.BGM, AudioType.GameSFX, AudioType.Voice };
            string ParsingAllAC = m_Datamanager.ResourcesParsingPath<AudioClip>();
            GetLoadTypes.HForEach(x => 
            Resources.LoadAll<AudioClip>($"{ParsingAllAC}{x}/").HForEach(y =>
            SavedAudioInfos.Add(new AudioInfo(y.name, x, y, false, 1f))));
            #region 풀어쓴 값들
            //Resources.LoadAll<AudioClip>($"{ParsingAllAC}BGM/").HForEach(x =>
            //SavedAudioInfos.Add(new AudioInfo(x.name, AudioType.BGM, x, 1f)));
            //Resources.LoadAll<AudioClip>($"{ParsingAllAC}Voice/").HForEach(x =>
            //SavedAudioInfos.Add(new AudioInfo(x.name, AudioType.Voice, x, 1f)));
            //Resources.LoadAll<AudioClip>($"{ParsingAllAC}GameSFX/").HForEach(x =>
            //SavedAudioInfos.Add(new AudioInfo(x.name, AudioType.GameSFX, x, 0.5f)));
            #endregion
        }
        if (m_AudioOutPutType == AudioSupportType.AddChannel)
        {
            Helper.HCountForEach(0, m_DicCountByType.Count - 1, _CountIDX =>
            {
                AudioType GetASType = _CountIDX switch
                { 0 => AudioType.GameSFX, 1 => AudioType.WPSFX, 2 => AudioType.UISFX, _ => AudioType.None };
                AudioASInfo[] GetArray = _CountIDX switch
                { 0 => m_GameSFXASInfos, 1 => m_SFXASInfos, 2 => m_UISFXASInfos, _ => null };
                Helper.HCountForEach(0, m_AddMaxCount, _CountIDX2 =>
                {
                    if (m_DicCountByType[GetASType] < m_AddMaxCount)
                    {
                        AudioSource GetNewASC =
                        Instantiate(m_PrefabASChanelModule, default, Quaternion.identity, GetArray[0].s_AudioSource.transform.parent);
                        GetNewASC.outputAudioMixerGroup = GetArray[0].s_AudioSource.outputAudioMixerGroup;
                        PartOfAddStatic(GetASType, new AudioASInfo(false, GetNewASC));
                        m_DicCountByType[GetASType] += 1;
                        m_GetAllASForControls.Add(GetArray[GetArray.Length - 1]);
                    }
                });
            });
        }
        #endregion

        m_isProcessActions = true;
    }

    #region Main System Public Functions (**Bit Sound Game Project Version (**이후 리팩토링 사용 or 전부 삭제 요망**)**)

    public AudioASInfo PlaySound(bool _isDirPlay, bool _isLoop, string _Name, AudioClip _PlaySoundClip, I_OwnerShip _GetOwnerShip = null)
    {
        AudioInfo? GetCallAudioInfos = null;
        int FIDX = -1; FIDX = SavedAudioInfos.HFindIndex(x => x.s_AudioName == _Name);
        if (FIDX != -1)  GetCallAudioInfos = SavedAudioInfos[FIDX];
        bool isOnlyApplyClipInfo = GetCallAudioInfos == null;
        if (isOnlyApplyClipInfo)
        GetCallAudioInfos = new AudioInfo(_Name, AudioType.GameSFX, _PlaySoundClip, _isLoop, 1);

        AudioASInfo ApplyAS = GetCallAudioInfos.Value.s_AudioType switch
        {
            AudioType.BGM => m_BGMASInfos,
            AudioType.Voice => m_VoiceASInfos,
            AudioType.GameSFX => FindListenAS(AudioType.GameSFX, m_GameSFXASInfos, _GetOwnerShip),
            _ => throw new System.Exception($"{nameof(SavedAudioInfos)}에 없어야되는 타입이 파싱되었습니다.!")
        };

        AdjustOutPutSound(ApplyAS, GetCallAudioInfos.Value, _isDirPlay, isOnlyApplyClipInfo);
        return ApplyAS;
    }

    #endregion

    #region Main System Public Functions

    public void PlaySound(string _PlaySavedName)
    {
        int FIDX = -1; FIDX = SavedAudioInfos.HFindIndex(x => x.s_AudioName == _PlaySavedName);
        if ((FIDX == -1).HDebug($"{_PlaySavedName} 이름의 오디로 정보는 존재하지 않습니다.!", Helper.HDType.Error)) return;

        AudioASInfo ApplyAS = SavedAudioInfos[FIDX].s_AudioType switch
        {
            AudioType.BGM => m_BGMASInfos,
            AudioType.Voice => m_VoiceASInfos,
            AudioType.GameSFX => FindListenAS(AudioType.GameSFX, m_GameSFXASInfos),
            _ => throw new System.Exception($"{nameof(SavedAudioInfos)}에 없어야되는 타입이 파싱되었습니다.!")
        };

        AdjustOutPutSound(ApplyAS, SavedAudioInfos[FIDX], false);
    }

    public void PlaySound(AudioInfo _GetAudioInfo)
    {
        AudioASInfo ApplyAS = _GetAudioInfo.s_AudioType switch
        {
            AudioType.WPSFX => FindListenAS(AudioType.WPSFX, m_SFXASInfos),
            AudioType.UISFX => FindListenAS(AudioType.UISFX, m_UISFXASInfos),
            _ => throw new System.Exception($"{nameof(SavedAudioInfos)}에 없어야되는 타입이 파싱되었습니다.!")
        };

        AdjustOutPutSound(ApplyAS, _GetAudioInfo);
    }

    public void StopSound(object _StopName, System.Action _EndCallBack = null)
    {
        AudioASInfo ASControl = _StopName switch
        {
            System.String GetStr => m_GetAllASForControls.HFind(x =>
            x.s_AudioInfo != null && x.s_AudioInfo.HasValue && x.s_AudioInfo.Value.s_AudioName == GetStr),
            AudioASInfo GetStopInfo => GetStopInfo,
            _ => null
        };    
        
        if (ASControl != null) ASControl.StopOrComplate(_EndCallBack);
    }

    public void SoundPlayOrPause(bool _isOn, AudioType _PartNotMuseType = default, I_CustomSoundEffect _ApplyEffect = null)  
    {
        foreach(var x in m_GetAllASForControls)
        {
            if (_isOn)
            {
                x.s_AudioSource.UnPause();
                if (_PartNotMuseType != default && x.spp_isPlaying &&
                _PartNotMuseType.HasFlags(x.s_AudioInfo.Value.s_AudioType))
                _ApplyEffect.SoundEffect(false, x.s_AudioSource); 
            }
            else 
            {
                if (_PartNotMuseType == default)
                { x.s_AudioSource.Pause(); continue; }

                if(_PartNotMuseType != default && x.spp_isPlaying &&
                _PartNotMuseType.HasFlags(x.s_AudioInfo.Value.s_AudioType))
                {
                    _ApplyEffect.SoundEffect(true, x.s_AudioSource);
                    continue;
                }
                x.s_AudioSource.Pause();
            }
        }
    }

    #endregion

    #region Main System Public Function (**Control Audio**)

    public void StopAllSound()
    {
        if(m_AudioOutPutType != AudioSupportType.None)
        {
            Helper.GetEnumByArray<AudioType>(true).HForEach(x => m_DicCountByType[x] = 0);
            m_ListenDestroyByOutPutType.Clear();
        }
        int CountIDX = 0, MaxCount = m_GetAllASForControls.Count;
        while (CountIDX < MaxCount)
        {
            AudioASInfo GetASInfo = m_GetAllASForControls[CountIDX];
            GetASInfo.StopOrComplate();
            if (GetASInfo.spp_isReturnPool || GetASInfo.s_AudioSource == null)
            {
                m_GetAllASForControls.RemoveAt(CountIDX);
                MaxCount = m_GetAllASForControls.Count;
            }
            else CountIDX++;
        }
    }

    public void ResetASSetting(AudioSource _ResetAs, System.Action<AudioSource> _ReSetCallBack)
    {
        int FIDX = -1; FIDX =
        m_GetAllASForControls.HFindIndex(x => x.s_AudioSource.Equals(_ResetAs));
        if ((FIDX == -1).HDebug($"{_ResetAs.name}은 현재 " +
        $"오디오 관리자에 포함되어있지 않습니다.!", Helper.HDType.Error)) return;

        _ReSetCallBack?.Invoke(m_GetAllASForControls[FIDX].s_AudioSource);
    }

    #endregion

    #region Sub System Private Functions

    private AudioASInfo FindListenAS(AudioType _GetASType, AudioASInfo[] _GetASArrayByType, I_OwnerShip _GetOnwerKey = null)
    {
        if (m_AudioOutPutType == AudioSupportType.ObjectPoolType && m_DicCountByType[_GetASType] < m_AddMaxCount &&
        _GetASArrayByType.HTrueForAll(x => x.spp_isPlaying && !x.spp_isPlayed && !x.spp_isHaveOwnerKey))
        {
            AudioSource GetNewASC =
            Instantiate(m_PrefabASChanelModule, default, Quaternion.identity, _GetASArrayByType[0].s_AudioSource.transform.parent);
            GetNewASC.outputAudioMixerGroup = _GetASArrayByType[0].s_AudioSource.outputAudioMixerGroup;
            m_DicCountByType[_GetASType] += 1;
            return new AudioASInfo(true, GetNewASC, _GetOnwerKey);
        }
        int FIDX = -1;
        FIDX = _GetASArrayByType.HFindIndex(x => !x.spp_isHaveOwnerKey && (!x.spp_isPlaying || x.spp_isPlayed));
        var ReturnAudioASInfo = _GetASArrayByType[FIDX == -1 ? 0 : FIDX]; //원래 로직
        if (_GetOnwerKey != null) ReturnAudioASInfo.ApplyOrResetOwner(_GetOnwerKey);
        return ReturnAudioASInfo; 
    }

    private void AdjustOutPutSound(AudioASInfo _FinalPlayAS, AudioInfo _ApplyAudioInfo,
    bool _isDirctionPlay = true, bool _isOnlyApplyClip = false)
    {
        if (m_AudioOutPutType == AudioSupportType.ObjectPoolType && 
        !m_GetAllASForControls.Contains(_FinalPlayAS)) m_GetAllASForControls.Add(_FinalPlayAS);

        var SetAS = _FinalPlayAS.s_AudioSource;
        if (SetAS.isPlaying) SetAS.Stop();

        SetAS.clip = _ApplyAudioInfo.s_AudioClip;
        if (!_isOnlyApplyClip)
        {
            SetAS.volume = _ApplyAudioInfo.s_Volume;
            SetAS.playOnAwake = _ApplyAudioInfo.s_isAwakeOnPlay;
            SetAS.minDistance = _ApplyAudioInfo.s_DistanceSoundMin;
            SetAS.maxDistance = _ApplyAudioInfo.s_DistanceSoundMax;
        }
        _FinalPlayAS.SetPlay(_ApplyAudioInfo, _isDirctionPlay);
    }

    #endregion

    #region Sub System Private Functions (**ODIN Reference**)

    private void ODIN_ShowAllAsInfo(bool _isOn)
    {
        m_BGMASInfos.ODIN_ShowAllRunningASInfo(_isOn);
        m_VoiceASInfos.ODIN_ShowAllRunningASInfo(_isOn);
        m_GameSFXASInfos.HForEach(x => x.ODIN_ShowAllRunningASInfo(_isOn));
        m_SFXASInfos.HForEach(x => x.ODIN_ShowAllRunningASInfo(_isOn));
        m_UISFXASInfos.HForEach(x => x.ODIN_ShowAllRunningASInfo(_isOn));
    }

    #endregion

    #region Main System Private Functions (**Update Reference**)

    private void UpdateCheckDspTimeByAS(List<AudioASInfo> _GetModuleInfos)
    {
        _GetModuleInfos.HForEach(x =>
        {
            if (x.spp_isPlaying && !x.s_AudioSource.loop) //Loop가 걸린 AudioSoruce는 오직 Clip을 다시 삽입했을때 일어난다
            {
                System.TimeSpan GetTS = System.TimeSpan.FromSeconds(AudioSettings.dspTime - x.spp_DSPTimeNow);
                float CalculateTime = (float)GetTS.TotalSeconds * 60;
                x.s_CurrPlayFrame = Mathf.Abs(x.spp_MaxPlayFreame - CalculateTime);
                if (x.s_CurrPlayFrame <= 0.8f && !x.s_AudioSource.loop)
                {
                    if(m_AudioOutPutType == AudioSupportType.ObjectPoolType) m_ListenDestroyByOutPutType.Add(x);
                    else StopSound(x);
                }
            }
        });

        if (m_AudioOutPutType != AudioSupportType.ObjectPoolType ||  
        m_ListenDestroyByOutPutType.Count <= 0) return;
        m_ListenDestroyByOutPutType.ForEach(x =>
        {
            if (m_AudioOutPutType != AudioSupportType.None)
            m_DicCountByType[x.s_AudioInfo.Value.s_AudioType] -= 1;
            _GetModuleInfos.Remove(x);
            StopSound(x);
        });
        m_ListenDestroyByOutPutType.Clear();
    }

    private void PartOfAddStatic(AudioType _GetASType, AudioASInfo _GetASInfo)
    {
        switch (_GetASType)
        {
            case AudioType.GameSFX:
                m_GameSFXASInfos = m_GameSFXASInfos.ToList().Concat(new[] { _GetASInfo }).ToArray();
                break;
            case AudioType.WPSFX:
                m_SFXASInfos = m_SFXASInfos.ToList().Concat(new[] { _GetASInfo }).ToArray();
                break;
            case AudioType.UISFX:
                m_UISFXASInfos = m_UISFXASInfos.ToList().Concat(new[] { _GetASInfo }).ToArray();
                break;
        }
    }

    #endregion

    #region 유니티 이벤트 함수
    void Update()
    {
        if (!m_isProcessActions) return;

        UpdateCheckDspTimeByAS(m_GetAllASForControls);
    }
    #endregion

    #region (**Support AS Array Reference**) Sub System Private Functions (보류 => 이후 삭제요망)
    //타입을 하나로 통합하기 위한 과정 가독성은 좀 떨어지지만 풀어쓴게 더 효율적인다 (보류 => 이후 삭제요망)
    //private T SetOutSetting<T>(AudioSupportType _AudioOutPutType, AudioType _GetASType, AudioOutPutSettingType _OrderType) where T : class
    //{
    //    if (_AudioOutPutType == AudioSupportType.None)
    //        return null;
    //    bool isPool = _AudioOutPutType == AudioSupportType.ObjectPoolType;

    //    switch (_OrderType)
    //    {
    //        case AudioOutPutSettingType.InstanceObj:
    //            var MainElemASInfo = GetASInfoArray(_GetASType)[0];
    //            AudioSource GetNewASC = Instantiate(m_PrefabASChanelModule, default,
    //            Quaternion.identity, MainElemASInfo.s_AudioSource.transform.parent);
    //            GetNewASC.outputAudioMixerGroup = MainElemASInfo.s_AudioSource.outputAudioMixerGroup;
    //            return new AudioASInfo(true, GetNewASC) as T;
    //        case AudioOutPutSettingType.AddList:
    //            break;
    //        case AudioOutPutSettingType.DestroyListAndObj:
    //            if (!isPool) return null;
    //            break;
    //    }
    //    return null;
    //}

    //private AudioASInfo[] GetASInfoArray(AudioType _GetASType)
    //{
    //    if (_GetASType == AudioType.None ||
    //    _GetASType == AudioType.BGM || _GetASType == AudioType.Voice) return null;

    //    return _GetASType switch
    //    {
    //        AudioType.GameSFX => m_GameSFXASInfos,
    //        AudioType.WPSFX => m_SFXASInfos,
    //        AudioType.UISFX => m_UISFXASInfos,
    //        _ => null
    //    };
    //}

    #endregion
}

//ObjectPoolManager<클래스>구현하면 그곳으로 해당 타입을 옮길 수 있도록
public enum SetPoolType
{
    InstanceType,
    ListAdd,

}


#region Local Sound Effect Reference

public interface I_CustomSoundEffect
{
    public void SoundEffect(bool _isOn, AudioSource _ApplyAS);
}

public class CustomReverveVersion_1 : I_CustomSoundEffect
{
    public void SoundEffect(bool _isOn, AudioSource _ApplyAS)
    {
        var LowPass = _ApplyAS.gameObject.CheckComnectComponent<AudioLowPassFilter>();
        var HighPass = _ApplyAS.gameObject.CheckComnectComponent<AudioHighPassFilter>();
        if (_isOn)
        {
            LowPass.cutoffFrequency = 500f;
            HighPass.cutoffFrequency = 50f;
        }
        else
        {
            UnityEngine.Object.Destroy(LowPass);
            UnityEngine.Object.Destroy(HighPass);
        }
        _ApplyAS.pitch = _isOn ? 0.6f : 1f;
    }
    
}

#endregion