using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using Commons.Helpers;
using DG.Tweening;
using System;

//[모듈 기본 설명]
//1.노래 하나당 [음악 엘리먼트 ] [엘리먼트컨트롤 전체 엘리먼트] [컨트롤]
//[추가적인 뮤트 모듈로 인덱싱을 하며] [플레이 타이밍에 따라] 등 상호간 연결점을 둔 SubModuleController로서 동작을
//한다
//2.이 음악 엘리먼트는 사운드와 음악 제어를 할 수있도록 하고 음악 모듈을 SFX로 회전한다
public class ElemSinngerSubController : SubModuleControllerBase, I_OwnerShip //리스너에게 제어가 있는 객체
{
    [Header("Control Values")]
    [SerializeField] private bool m_isUsedAnim = true;
    [SerializeField, ShowIf("m_isUsedAnim")] private float m_Sensitivity = 5f; // 얼마나 민감하게 반응할지
    [SerializeField, ShowIf("m_isUsedAnim")] private float m_BaseMaxSpeed = 1f; //최대 속도
    [SerializeField, ReadOnly] private float[] m_Spectrum = new float[64];
    [Header("Required Values")]
    [SerializeField, ReadOnly] private Animator m_MainAnim;
    //[SerializeField, ReadOnly] private RequiredSoundGamePopUp m_GetRPopUp;

    [Tooltip("Apply Values")]
    private DataMonoMusicICONModule m_ReciveModule;
    private DataMonoMuteModule m_ControlMuteModule;
    private AudioASInfo m_ReciveAudioASInfo;

    [Tooltip("Control Values")]
    private bool m_isUpdateUpScale;
    private Vector3 m_CharacterOGScale;
    private readonly string[] JUMPAIMEARRAY = new string[3] { "Idle", "Jump", "Dance" };

    //[Tooltip("Hiden Support Values")]
    //private Transform m_SubDecorTR =>

    public int pp_ElemIDX { get; private set; }
    public bool pp_isOn { get => m_ReciveModule != null; }
    public AudioASInfo testpp_ReciveAudioASInfo => m_ReciveAudioASInfo; //Test Only나중에는 더 나은곳에서 돌아가는지 체크해야한다

    public override object[] Initlization(params object[] _ParsingParams)
    {
        var PartParams = base.Initlization(_ParsingParams);
        if (m_isUsedAnim) m_MainAnim = GetComponent<Animator>();
        CallAnimSetByIDX(0);
        m_isTestMode = m_MainController.pp_isTestMode;
        m_CharacterOGScale = this.transform.localScale;
        this.gameObject.SetActive(false);
        return null;
    }

    #region Main System Public Functions (**Main Or Init Reference**)

    public void ElemSinngerOutPutInit(int _ApplyIDX)
    {
        pp_ElemIDX = _ApplyIDX;
        if (!FindParentsObjTypeByTemp<SoundGameController>().pp_isOutPutOldVer)
        {
            var MainSR = this.GetComponent<SpriteRenderer>();
            var ApplySubDecorSR = this.transform.GetChild(0).GetComponent<SpriteRenderer>();

            float ApplyDuring = 0.65f;
            MainSR.DOColor(Helper.SetChangeColorAlpha(MainSR.color, 0.8f), ApplyDuring).SetDelay((pp_ElemIDX + 1) * 0.25f).SetEase(Ease.OutSine).OnComplete(() =>
            ApplySubDecorSR.DOColor(Helper.SetChangeColorAlpha(ApplySubDecorSR.color, 1f), ApplyDuring).SetEase(Ease.OutSine).OnComplete(() =>
            ApplySubDecorSR.DOColor(Helper.SetChangeColorAlpha(ApplySubDecorSR.color, 0f), ApplyDuring).SetEase(Ease.OutSine).OnComplete(() =>
            FindParentsObjTypeByTemp<SoundGameController>().I_CheckSubModuleIsAllCanAction(this))));
            return;
        }
        var GetOGPos = this.transform.position;
        this.transform.position = new Vector3(GetOGPos.x, -0.27f, GetOGPos.z);
        CallAnimSetByIDX(1);
        this.transform.DOMoveY(GetOGPos.y, 0.25f).SetDelay((pp_ElemIDX + 1) * 0.05f).SetEase(Ease.OutBack).OnComplete(() =>
        {
            CallAnimSetByIDX(0);
            FindParentsObjTypeByTemp<SoundGameController>().I_CheckSubModuleIsAllCanAction(this);
        });
    }

    //Controller This Data

    //해당 브레크를 AusioSorce 일시 혹은 멈출시 시킨다
    public override void BreakPoint(bool _isBreak, SubModuleBreakType _BrackType = SubModuleBreakType.BreakAll, Type _GetType = null)
    {
        if (_BrackType == SubModuleBreakType.BreakAll)
            base.BreakPoint(_isBreak, _BrackType, _GetType);

        //_isBreak
        if (pp_isOn)
        {
            if (!m_ControlMuteModule.pp_isReadyToSlider)
            {
                m_ReciveAudioASInfo.ElemSoundPlayOrPause(_isBreak);
                CallAnimSetByIDX(_isBreak ? 2 : 0);
            }
            m_ControlMuteModule.SemiBreakPoint(_isBreak); //멈추게할때 재생하는것만 세미멈출게 플레이할게
        }
    }
    #endregion

    #region Main System Public Functions 

    public void AllRestart()
    {
        if (!pp_isOn || m_ReciveAudioASInfo == null ||
        m_ControlMuteModule.pp_isReadyToSlider) return;

        m_ReciveAudioASInfo.Restart();
    }

    public void OffCharacterDragEvent(bool _isTweening) => m_isUpdateUpScale = _isTweening;

    #endregion

    #region Main System Public Functions (**Linked Mute Module Reference**)

    public void DeleteThisSound(DataMonoMuteModule _CompareModule)
    {
        if ((m_ControlMuteModule == null ||
        !_CompareModule.Equals(m_ControlMuteModule)).HDebug("[오류발생]", Helper.HDType.Error)) return;

        BreakPoint(false, SubModuleBreakType.PartBreak);
        CallAnimSetByIDX(1);
        m_ReciveAudioASInfo.StopOrComplate();
        m_ReciveModule.SemiBreakPoint(true);
        m_ReciveAudioASInfo = null; m_ReciveModule = null;
        var GetOGPosY = this.transform.position.y;
        this.transform.DOMoveY(-0.27f, 0.35f).SetEase(Ease.OutBack).OnComplete(() =>
        this.transform.DOMoveY(GetOGPosY, 0.35f).SetEase(Ease.OutBack).OnComplete(() =>
        CallAnimSetByIDX(0)));
    }

    #endregion

    #region Main System Sub Functions (**Apply Infos**)
    public void ApplyAudioInfos(DataMonoMusicICONModule _GetModule)
    {
        if (pp_isOn) return;
        bool isPlayingNow =
        FindParentsObjTypeByTemp<SoundGameController>().PlayingNow();
        bool isMusicOn =
        FindParentsObjTypeByTemp<SoundGameController>().pp_isMusicOn;
        m_ReciveModule = _GetModule;
        m_ReciveModule.SemiBreakPoint(false);

        //음악 라운드 킬 + 음악 컨트롤러(AudioASInfo) 생성한다
        m_ReciveAudioASInfo =
        Appinstance.Instance.ms_AudioManager.PlaySound(false, _GetModule.pp_SoundGameClipInfo.s_isLoop,
        _GetModule.pp_SoundGameClipInfo.s_ItemName, AudioType.GameSFX, _GetModule.pp_SoundGameClipInfo.s_AudioClipsInfo[0], this);
        m_ReciveAudioASInfo.ElemSoundPlayOrPause(false);

        if (m_ControlMuteModule == null)
        {
            m_ControlMuteModule =
            FindParentsObjTypeByTemp<SoundGameController>().pp_RequiredSoundGamePopUp.
            InstanceDataMonoMuteModule(this.transform);
            System.Action EndCallBack = () => BreakPoint(isMusicOn);
            m_ControlMuteModule.Initlization(this, isPlayingNow, EndCallBack);
        }
        else
        {
            m_ControlMuteModule.gameObject.SetActive(true);
            if (isPlayingNow) m_ControlMuteModule.StartInitAgain();
            else BreakPoint(isMusicOn);
        }
        m_ControlMuteModule.SemiBreakPoint(true);
    }

    public void ShowMuteModule(bool _isOn)
    {
        if (m_ControlMuteModule == null ||
        m_ControlMuteModule.pp_isReadyToSlider) return;

        m_ControlMuteModule.gameObject.SetActive(_isOn);
    }

    #endregion

    #region Sub System Private Functions (**AnimReference**)
    private void CallAnimSetByIDX(int _CallIDX)
    {
        if (!m_isUsedAnim) return;
        if (m_MainAnim.GetCurrentAnimatorStateInfo(0).IsName(JUMPAIMEARRAY[_CallIDX]) ||
        (_CallIDX < 0 || _CallIDX >= JUMPAIMEARRAY.Length).HDebug("잘못된 애님 호출", Helper.HDType.Error)) return;
        m_MainAnim.speed = 1;
        //m_CurrAnimRunIDX = _CallIDX;
        m_MainAnim.CrossFade(JUMPAIMEARRAY[_CallIDX], 0f);
    }

    private void UpdateAnimSpedByBit()
    {
        if (!pp_isOn || m_ReciveAudioASInfo == null || m_ControlMuteModule.pp_isReadyToSlider ||
        (m_isUsedAnim && !m_MainAnim.GetCurrentAnimatorStateInfo(0).IsName(JUMPAIMEARRAY[2]))) return;

        //m_ReciveAudioASInfo.s_AudioSource.GetSpectrumData(m_Spectrum, 0, FFTWindow.Rectangular);
        m_Spectrum = UpdateThisAnimClipLeg(
        m_ReciveModule.pp_SoundGameClipInfo.s_ReciveBitInfoByClipName.ToArray());

        float intensity = 0f;
        if (m_Spectrum.CheckArrayNull(0)) foreach (float val in m_Spectrum)
                intensity = Mathf.Max(intensity, val);

        // 비트 강도 계산
        float beatStrength = intensity * m_Sensitivity;

        // 애니메이션 스피드 조절 (0~maxSpeed 사이로 고정)
        float newSpeed = Mathf.Clamp(beatStrength, 0f, m_BaseMaxSpeed);
        if (m_isUsedAnim) m_MainAnim.speed = newSpeed;
    }

    private float[] UpdateThisAnimClipLeg(BitInfo[] valueArray)
    {
        if (!pp_isOn || m_ReciveAudioASInfo == null || m_ControlMuteModule.pp_isReadyToSlider) return null;

        var audioSource = m_ReciveAudioASInfo.s_AudioSource;
        if (audioSource.isPlaying && audioSource.clip != null)
        {
            // 현재 재생 시간 가져오기
            float currentTime = audioSource.time;
            // 현재 재생 위치의 비율 (0.0 ~ 1.0)
            float playbackPercentage = currentTime / audioSource.clip.length;
            // 배열에서 해당 비율에 맞는 인덱스 계산
            int index = Mathf.Clamp(Mathf.FloorToInt(playbackPercentage * valueArray.Length), 0, valueArray.Length - 1);
            // 해당 인덱스의 배열의 값 가져오기
            return index > -1 && index < valueArray.Length ?
            valueArray[index].s_MicroBitInfo : null;
            // 값 출력 디버그
            //Debug.Log($"현재 시간: {currentTime}, 비율값: {playbackPercentage:P2}, 인덱스: {index}, 값: {value}");
        }
        return null;
    }
    #endregion

    public override void ProcessUpdate()
    {
        base.ProcessUpdate();

        this.transform.localScale = Vector3.Lerp(this.transform.localScale, m_isUpdateUpScale ?
        m_CharacterOGScale * 1.35f : m_CharacterOGScale, Time.deltaTime * 10f);

        if (!m_isActionProcess) return;

        UpdateAnimSpedByBit();
    }
}