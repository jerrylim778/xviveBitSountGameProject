using UnityEngine;
using UnityEngine.UI;
using Sirenix.OdinInspector;
using Commons.Helpers;
using DG.Tweening;

//데이터와 처리가 같이 들어있는 Component
public class DataMonoMusicICONModule : ModuleMonoBase
{
    //[SerializeField] private SoundGameClipInfo m_SoundGameClipInfo;
    [SerializeField, ReadOnly] private RequiredSoundGamePopUp m_MainPopUp;
    [SerializeField, ReadOnly] private Sprite[] m_ApplySprArray;

    [field: SerializeField, ReadOnly] public int m_ItemIDX { get; private set; }
    [field: SerializeField, ReadOnly] public string m_CallClipName { get; private set; }
    [field: SerializeField, ReadOnly] public AudioClip m_ASClip { get; private set; }
    public Vector2 pp_ICONOGAnhoredPos { get; private set; }
    public SoundGameClipInfo pp_SoundGameClipInfo { get; private set; }

    public bool pp_isProcessAction => m_isProcessAction;
    public RectTransform pp_SetRT { get; private set; }//=> this.transform.GetChild(1).GetComponent<RectTransform>();
    public RectTransform pp_EmptyRT { get; private set; }// this.transform.GetChild(0).GetComponent<RectTransform>();
    public RectTransform pp_OGRT => this.GetComponent<RectTransform>();

    [Tooltip("Support Values")]
    private Sequence m_PickSeq;
    private CanvasGroup m_SetRTCG;
    private bool m_isSetRTAlpha;

    public override void Initlization(I_PopUpInfo _MainPopUpBase, params object[] _OtherParams)
    {
        m_MainPopUp = _MainPopUpBase as RequiredSoundGamePopUp;
        var GetClipInfo = _OtherParams[0] as SoundGameClipInfo;
        m_ItemIDX = GetClipInfo.s_ItemIDX;
        m_CallClipName = GetClipInfo.s_ItemName;
        m_ApplySprArray = GetClipInfo.s_SpriteArray;
        pp_SoundGameClipInfo = GetClipInfo;
        pp_SetRT = this.transform.GetChild(1).GetComponent<RectTransform>();
        pp_EmptyRT = this.transform.GetChild(0).GetComponent<RectTransform>();
        m_SetRTCG = pp_SetRT.gameObject.CheckComnectComponent<CanvasGroup>();
        this.transform.GetChild(0).GetChild(0).GetComponent<Image>().sprite = m_ApplySprArray[0];
        this.transform.GetChild(1).GetChild(0).GetComponent<Image>().sprite = m_ApplySprArray[0];
        pp_ICONOGAnhoredPos = pp_OGRT.anchoredPosition;
    }

    public override void SemiBreakPoint(bool _isBreakPoint)
    {
        m_isProcessAction = _isBreakPoint;
        pp_SetRT.gameObject.SetActive(_isBreakPoint);
        ResetEvnet();
    }

    public void ResetEvnet()
    {
        Helper.ResetSequce(m_PickSeq);
        pp_SetRT.transform.SetParent(this.transform);
        pp_SetRT.localScale = Vector3.one;
        m_isSetRTAlpha = false; m_SetRTCG.alpha = 1f;
        pp_SetRT.anchoredPosition = pp_EmptyRT.anchoredPosition;
    }

    public void PickEvent()
    {
        pp_SetRT.transform.SetParent(m_MainPopUp.transform);
        Helper.ResetSequce(m_PickSeq);
        m_PickSeq = DOTween.Sequence();
        m_PickSeq .Append(pp_SetRT.DOScale(Vector3.one * 1.35f, 0.55f).SetEase(Ease.InOutBack));
    }

    public void DragEvent(bool _isTweening) => m_isSetRTAlpha = _isTweening;

    private void Update()
    {
        if (!m_isProcessAction) return;

        m_SetRTCG.alpha = Mathf.Lerp(m_SetRTCG.alpha, m_isSetRTAlpha ? 0.5f : 1, Time.deltaTime * 10f);
    }
}


