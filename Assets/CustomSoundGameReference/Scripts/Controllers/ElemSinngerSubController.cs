using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using Commons.Helpers;
using DG.Tweening;
using System;

//[���� ���� ����]
//1.��� �ϳ��� [��� ���Ұ� ] [��Ҹ������� ��ü ���Ұ�] [����]
//[�߰��� ���� ������ �ε����� ���] [�巹�� �õ��� ����] �� ����� ������ �� SubModuleController�ν� ������
//�ִ´�
//2.�� ���� �����ʹ� ���ν� ���� ������ �� �ֵ��� �ϰ� ��� ����� SFX�� ���ȴ�
public class ElemSinngerSubController : SubModuleControllerBase, I_OwnerShip //�������� ������ �ִ� ��ü
{
    [Header("Control Values")]
    [SerializeField] private bool m_isUsedAnim = true;
    [SerializeField] private float m_Sensitivity = 5f; // �󸶳� �ΰ��ϰ� ��������
    [SerializeField] private float m_BaseMaxSpeed = 1f; //�ִ� �ӵ�
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

    public int pp_ElemIDX { get; private set; }
    public bool pp_isOn { get => m_ReciveModule != null; }
    public AudioASInfo testpp_ReciveAudioASInfo => m_ReciveAudioASInfo; //Test Only���Ŀ��� �� ��ҿ��� ���ư��� üũ�ؾ��Ѵ�

    public override object[] Initlization(params object[] _ParsingParams)
    {
        var PartParams = base.Initlization(_ParsingParams);
        if(m_isUsedAnim) m_MainAnim = GetComponent<Animator>();
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
        //var GetThisRT = this.GetComponent<RectTransform>();
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

    //�ش� ������ AusioSorce ��� Ȥ�� ������ ���Խ�ų��
    public override void BreakPoint(bool _isBreak, SubModuleBreakType _BrackType = SubModuleBreakType.BreakAll, Type _GetType = null)
    {
        if(_BrackType == SubModuleBreakType.BreakAll)
        base.BreakPoint(_isBreak, _BrackType, _GetType);

        //_isBreak
        if (pp_isOn)
        {
            if(!m_ControlMuteModule.pp_isReadyToSlider)
            {
                m_ReciveAudioASInfo.ElemSoundPlayOrPause(_isBreak);
                CallAnimSetByIDX(_isBreak ? 2 : 0);
            }
            m_ControlMuteModule.SemiBreakPoint(_isBreak); //�����϶� �����ϴ°͸� �����Ұ� �����Ұ�
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
        !_CompareModule.Equals(m_ControlMuteModule)).HDebug("[�������]", Helper.HDType.Error)) return;
        
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

        //���� ������ �� + ��� ��Ʈ�ѷ�(AudioASInfo) ��������
        m_ReciveAudioASInfo =
        Appinstance.Instance.ms_AudioManager.PlaySound(false, _GetModule.pp_SoundGameClipInfo.s_isLoop,
        _GetModule.pp_SoundGameClipInfo.s_ItemName, _GetModule.pp_SoundGameClipInfo.s_AudioClipsInfo[0], this);
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
        (_CallIDX < 0 || _CallIDX >= JUMPAIMEARRAY.Length).HDebug("�߸��� �ִ� ȣ��", Helper.HDType.Error)) return;
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
        if(m_Spectrum.CheckArrayNull(0)) foreach (float val in m_Spectrum) 
        intensity = Mathf.Max(intensity, val);

        // ��Ʈ ���� ����
        float beatStrength = intensity * m_Sensitivity;

        // �ִϸ����� ���ǵ� ��� (0~maxSpeed ���̷� ����)
        float newSpeed = Mathf.Clamp(beatStrength, 0f, m_BaseMaxSpeed);
        if(m_isUsedAnim) m_MainAnim.speed = newSpeed;
    }

    private float[] UpdateThisAnimClipLeg(BitInfo[] valueArray)
    {
        if(!pp_isOn || m_ReciveAudioASInfo == null || m_ControlMuteModule.pp_isReadyToSlider) return null;

        var audioSource = m_ReciveAudioASInfo.s_AudioSource;
        if (audioSource.isPlaying && audioSource.clip != null)
        {
            // ���� ��� �ð� ��������
            float currentTime = audioSource.time;
            // ���� ��� ����� ��� (0.0 ~ 1.0)
            float playbackPercentage = currentTime / audioSource.clip.length;
            // �迭���� �ش� ������� �´� �ε��� ���
            int index = Mathf.Clamp(Mathf.FloorToInt(playbackPercentage * valueArray.Length), 0, valueArray.Length - 1);
            // ���� �ε����� �迭�� �� ��������
            return index > -1 && index < valueArray.Length? 
            valueArray[index].s_MicroBitInfo : null;
            // �� ��� ����
            //Debug.Log($"���� �ð�: {currentTime}, �����: {playbackPercentage:P2}, �ε���: {index}, ��: {value}");
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
