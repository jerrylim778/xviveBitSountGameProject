using UnityEngine;
using Sirenix.OdinInspector;
using Commons.Helpers;

public class AudioASModule : ModuleMonoBase, I_OwnerShip
{
    [SerializeField, ReadOnly] private SpriteRenderer m_MainSR;
    [SerializeField, ReadOnly] private ElemCategorySubCollModule m_MainControlModule;
    [SerializeField] private AudioASInfo m_SubAudioInfo;

    [Tooltip("Support Values")]
    private Color m_OGColor;

    public bool pp_isTurnOn { get; private set; }

    public override void Initlization(ModuleMonoBase _MainBase, params object[] _OtherParams) 
    {
        m_MainControlModule = _MainBase as ElemCategorySubCollModule;
        var GetAudioSorce = _OtherParams[0] as AudioSource;
        var GetClip = _OtherParams[1] as AudioClip;
        //Sprite Init
        m_MainSR = this.GetComponent<SpriteRenderer>();
        m_OGColor = m_MainSR.color;
        //Local AudioASInfo Init
        var AModuleInfo = new AudioInfo(GetClip.name, AudioType.GameSFX, GetClip, false, 1);
        if (m_SubAudioInfo == null || m_SubAudioInfo.s_AudioSource == null)
        m_SubAudioInfo.SerializeInitlization(false, GetAudioSorce, this);
        m_SubAudioInfo.SetPlay(AModuleInfo, false);

        m_MainSR.color = pp_isTurnOn ? m_OGColor : Color.gray;
    }

    //private void OnCollisionEnter2D(Collision2D collision)
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!m_isProcessAction || !pp_isTurnOn ||
        collision.gameObject.layer != LayerMask.NameToLayer("ContentsWall")) return;

        m_SubAudioInfo.s_AudioSource.Play();
    }

    public void SwichTurnBtn()
    {
        pp_isTurnOn = !pp_isTurnOn;

        m_MainSR.color = pp_isTurnOn ? m_OGColor : Color.gray;
    }

    public override void SemiBreakPoint(bool _isBreakPoint)
    => m_isProcessAction = _isBreakPoint;
}
