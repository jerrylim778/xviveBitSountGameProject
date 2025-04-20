using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using Commons.Helpers;
using TMPro;

public class GUIMB_SoundGameModule_2 : SystemBase
{
    [Header("===Control Values===")]
    [SerializeField] private bool m_isTestMode = false; //다른 클래스에 대부분 사용한다면 부모클래스로 격상 요망..
    [SerializeField] private bool m_isUseStartWhiteFlash = false;
    [SerializeField] private bool m_isLoadingMainProcessParsing = true;
    [Header("===Main GUI Values===")]
    [SerializeField] private Image m_SubBG;
    [SerializeField] private Button m_GameStartBtn;
    [Header("===GUI Production Valus===")]
    [SerializeField] private RectTransform m_MainLogo;
    [SerializeField] private RectTransform m_StartBeforeProductionBG;
    [SerializeField] private RectTransform m_StartAfterProductionBG;
    [SerializeField] private Image m_ProductionBGPanel;
    [SerializeField] private TextMeshProUGUI m_ProductionTMPTxt;
    [SerializeField] private Color[] m_ApplyColorPanels;
    [SerializeField] private RectTransform[] m_ElecShaderImgs;
    private bool m_isNextStep = false;

    public override void Initlization()
    {
        SubTweening(false);

        GamePlaySystem.Instance.ExecutePopUpStack<RequiredBitDropPopUp>().Initlization();
        GamePlaySystem.Instance.MainPopUpPush<RequiredBitDropPopUp>();
        m_MainLogo.gameObject.SetActive(false);
        m_GameStartBtn.gameObject.SetActive(false);
        //m_GameStartBtn.gameObject.CheckComnectComponent<CanvasGroup>().alpha = 0f;
        //ProudctionInit
        m_ProductionTMPTxt.text = string.Empty;
        m_ElecShaderImgs.HForEach(x => x.localScale = new Vector3(x.localScale.x, 0f, x.localScale.z));
        var GetAlpha = m_ProductionBGPanel.color; GetAlpha.a = 0; m_ProductionBGPanel.color = GetAlpha;
        m_StartBeforeProductionBG.gameObject.SetActive(true);
        m_StartAfterProductionBG.gameObject.SetActive(false);
        System.Action ComplateAction = () => StartCoroutine(CO_WaitAction(0.3f, () => AddStartEvent(() =>
        {
            m_MainLogo.transform.localScale = Vector3.zero;
            m_GameStartBtn.transform.localScale = Vector3.zero;
            m_MainLogo.gameObject.SetActive(true);
            m_GameStartBtn.gameObject.SetActive(true);
            m_MainLogo.DOScale(Vector3.one, 0.25f).SetEase(Ease.InOutExpo);
            m_GameStartBtn.image.rectTransform.DOScale(Vector3.one, 0.25f).SetDelay(0.15f).
            SetEase(Ease.InOutExpo).OnComplete(() => SubTweening(true));
            //CanvasGroup GetCG = m_GameStartBtn.gameObject.GetComponent<CanvasGroup>();
            //DOTween.To(() => GetCG.alpha, x => GetCG.alpha = x, 0.5f, 0.35f).SetEase(Ease.OutBounce).OnComplete(() => SubTweening(true));
        })));

        if (!m_isLoadingMainProcessParsing)
        {
            ComplateAction();
            return;
        }

        DataManager GetManager = Appinstance.Instance.ms_DataManager;
        m_MainControllSystem.ExecutePopUpStack<LoadingPopUp>().InitLoadingPopUp(GetManager.AllParsingInfosActions(), ComplateAction);
        m_MainControllSystem.MainPopUpPush<LoadingPopUp>();
    }

    private void SubTweening(bool _isAction)
    {
        m_GameStartBtn.interactable = _isAction;
        m_GameStartBtn.gameObject.SetActive(_isAction);
        if (!_isAction)
        {
            m_GameStartBtn.onClick.RemoveAllListeners();
            return;
        }
        m_GameStartBtn.onClick.AddListener(() => ExecuteNextStep(SystemInfoType.NoneMainGUI)); //다음 GUIMB로 넘어갈 수 있도록 수정할것
    }

    #region Sub System private Functions (**Starting Tweening Reference**)

    private void AddStartEvent(System.Action _AfterOutCallBacks)
    {
        //이전 작업 뒤에 내려올것
        var GetICEMat =
        FindAnyObjectByType<CameraProductionSubController>().pp_EffectMat_ICEPanel;
        GetICEMat.SetFloat("_ICEAgePower", 5f);
        GetICEMat.DOFloat(0.83f, "_ICEAgePower", 0.45f).SetEase(Ease.OutSine);
        int ComplateIDX = 0; m_ElecShaderImgs.HForEach(x =>
        {
            var GetSizeDelta = x.localScale;
            x.DOScaleY(600f, 0.45f).
            SetEase(Ease.OutCirc).OnComplete(() =>
            {
                ComplateIDX++;
                if (m_ElecShaderImgs.Length >= ComplateIDX)
                    TweeningPanelAndTxtChange("HIT", m_ApplyColorPanels[0],
                    () => AddStartEvent_2(_AfterOutCallBacks));
            });
        });

    }

    private void AddStartEvent_2(System.Action _AfterOutCallBacks)
    {
        MoveLine(false, m_ElecShaderImgs[0]);
        MoveLine(true, m_ElecShaderImgs[1], () =>
        {
            var GetDJRT = m_StartAfterProductionBG.GetChild(0) as RectTransform;
            GetDJRT.anchoredPosition = new Vector2(GetDJRT.anchoredPosition.x, -500f);
            m_StartAfterProductionBG.gameObject.SetActive(true);
            GetDJRT.DOAnchorPosY(0f, 0.75f).SetDelay(0.05f).SetEase(Ease.OutSine);
            TweeningPanelAndTxtChange("THE", m_ApplyColorPanels[1]);
        }, () =>
        TweeningPanelAndTxtChange("BEAT", m_ApplyColorPanels[2], () =>
        {
            var GetSubCG = m_ProductionTMPTxt.gameObject.CheckComnectComponent<CanvasGroup>();
            DOTween.To(() => GetSubCG.alpha, x => GetSubCG.alpha = x, 0f, 0.5f).SetEase(Ease.InBounce).OnComplete(() =>
            m_ProductionTMPTxt.DOTmpText(string.Empty, 0.15f, true, ScrambleMode.All).OnComplete(() =>
            {
                m_ProductionBGPanel.gameObject.SetActive(false);
                m_ProductionTMPTxt.gameObject.SetActive(false);
                _AfterOutCallBacks?.Invoke();
            }));
        }));
    }

    private void MoveLine(bool _isLeft, RectTransform _ApplyImg,
    System.Action _MiddleCallBack = null, System.Action _EndCallBack = null) =>
    _ApplyImg.DOAnchorPosX(_isLeft ? -50f : 50f, 0.85f).SetEase(Ease.InQuad).OnComplete(() =>
    {
        _MiddleCallBack?.Invoke();
        _ApplyImg.DOAnchorPosX(_isLeft ? -210f : 210f, 1.5f).
        SetEase(Ease.OutQuad).OnComplete(() => _EndCallBack?.Invoke());
    });

    private void TweeningPanelAndTxtChange(string _ApplyStr, Color _ChangeColor,
    System.Action _AfterOutCallBacks = null)
    {
        m_ProductionBGPanel.DOColor(_ChangeColor, 0.25f).
        SetEase(Ease.InOutElastic);//.OnComplete(() =>
        m_ProductionTMPTxt.DOTmpText(_ApplyStr, 0.15f, true, ScrambleMode.All).
        SetEase(Ease.InOutElastic).OnComplete(() => _AfterOutCallBacks?.Invoke());
    }

    IEnumerator CO_WaitAction(float _WaitTime, System.Action _EndCallBack)
    {
        yield return new WaitForSeconds(_WaitTime);
        _EndCallBack();
    }

    #endregion


    public override void ExecuteNextStep(SystemInfoType _NextType)
    {
        if (m_isNextStep) return; m_isNextStep = true;

        if (_NextType == SystemInfoType.NoneMainGUI)
        {
            SubTweening(false);
            System.Action EndCallBack = () =>
            {
                GamePlaySystem.Instance.MainPopUpPop<RequiredBitDropPopUp>();
                Appinstance.Instance.ms_GamePlayManager.ExecuteChangeAllGameControll(
                GamePlayManager.GamePlayType.StartingSet, true, new MyItemInfoBuffer(null));
                m_MainControllSystem.ExecuteOutPutSystemBase(_NextType);
            };

            if (!m_isUseStartWhiteFlash)
            {
                EndCallBack();
                return;
            }
            var GetCG = GamePlaySystem.Instance.testpp_AllIncludedGUIPanel.gameObject.CheckComnectComponent<CanvasGroup>();
            GetCG.alpha = 0f; GetCG.transform.GetChild(0).gameObject.SetActive(true);
            DOTween.To(() => GetCG.alpha, x => GetCG.alpha = x, 1f, 0.4f).SetEase(Ease.InSine).OnComplete(() => EndCallBack());
            return;
        }
        SubTweening(false);
        base.ExecuteNextStep(_NextType);
    }


}