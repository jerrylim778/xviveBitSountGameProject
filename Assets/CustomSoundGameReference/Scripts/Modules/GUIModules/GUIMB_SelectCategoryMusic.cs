using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Sirenix.OdinInspector;
using Commons.Helpers;
using DG.Tweening;


public class GUIMB_SelectCategoryMusic : SystemBase, I_OwnerShip
{
    [Header("===Control Values===")]
    [SerializeField] private bool m_isUseStartWhiteFlash = false;
    [SerializeField] private float m_CurrTweeningSped;
    [Header("===Current Values===")]
    [SerializeField, ReadOnly] private AlbumInfo m_CurrAlbumInfo;
    [SerializeField, ReadOnly] private AudioASInfo m_CurrAudioASinfo;
    [SerializeField, ReadOnly] private GUIAlbumModule m_CurrInstanceAlbumModule;
    [SerializeField, ReadOnly] private ElemASBuffer m_CurrElemASBuffer;
    [Header("===GUI Required Values===")]
    //[SerializeField] private Image m_MainBG;
    [SerializeField] private Image m_MainTitle;
    [SerializeField] private OutPutAudioEqulizeModule m_BounsTItleBG;
    [SerializeField] private Button m_LeftArrowBtn, m_RightArrowBtn, m_SelectBtn;
    [SerializeField, ReadOnly] private SpriteRenderer m_MapModuleBG/*, m_FrontPanel*/;
    [Header("===Required Values===")]
    [SerializeField] private Color m_EmptyColor;
    [SerializeField] private GUIAlbumModule m_CurrAlbumModulePrefab;
    [SerializeField, ReadOnly] private AudioMixVisualizeSubController m_AudioMixVisualizeSubController;
    private List<AlbumInfo> m_AlbumInfos = new(); //이후에 반드시 스크립터블 데이터화 진행할것

    private bool m_isFirst = false;
    private bool m_isStartRotByCurrAlbumInfo = false;

    public override void Initlization()
    {
        BtnReset(false);
        if (!m_isFirst)
        {
            m_AlbumInfos = 
            Appinstance.Instance.ms_DataManager.spp_DSDataAlbumInfo.pp_DSAlbumLevelInfos;
            m_CurrAlbumInfo = m_AlbumInfos[0];
            m_RightArrowBtn.onClick.AddListener(() => OnClickArrowBtn(false));
            m_LeftArrowBtn.onClick.AddListener(() => OnClickArrowBtn(true));
            m_SelectBtn.onClick.AddListener(() => ExecuteNextStep(SystemInfoType.NoneMainGUI));
            m_isPartControllSystem = true;
            #region Tweening Init
            //m_ApplyImg.gameObject.SetActive(false);
            m_BounsTItleBG.gameObject.SetActive(false);
            m_SelectBtn.transform.localScale = Vector3.zero;
            var GetMTCG = m_MainTitle.transform.gameObject.CheckComnectComponent<CanvasGroup>();
            GetMTCG.alpha = 0f;
            var GetBtns = new Button[2] { m_LeftArrowBtn, m_RightArrowBtn };
            int CountIDX = 0; GetBtns.HForEach(x =>
            {
                var GetIR = x.image.rectTransform;
                var GetAPos = GetIR.anchoredPosition;
                GetIR.anchoredPosition = new Vector2(CountIDX == 0 ? 50f : -50f, GetAPos.y);
                GetIR.DOAnchorPosX(CountIDX == 0 ? -50f : 50f, 0.65f).SetEase(Ease.OutSine);
                CountIDX++;
            });
            DOTween.To(() => GetMTCG.alpha, x => GetMTCG.alpha = x, 1f, 0.65f).SetEase(Ease.OutSine).OnComplete(() =>
            m_MainTitle.transform.DOScale(Vector3.one * 1.25f, 0.6f).SetEase(Ease.OutBounce).OnComplete(() =>
            m_MainTitle.transform.DOScale(Vector3.one, 0.6f).SetEase(Ease.OutBack).OnComplete(() =>
            ApplyInfos(true, m_CurrAlbumInfo, () =>
            {
                m_SelectBtn.interactable = false;
                m_SelectBtn.transform.DOScale(Vector3.one, 0.65f).
                SetEase(Ease.InOutExpo).OnComplete(() => m_SelectBtn.interactable = true);
            }))));
            #endregion
            return;
        }

        
        AfterProductionCamAndGUI(true, _EndCallBack : () => BeforeInitByOutPost(true));
    }

    //GamePlaySystem이 아닌 외부에서 보이기 전에 초기화를 해야될때 호출
    public void BeforeInitByOutPost(bool _isTweening)
    {
        var GetBtns = new Button[2] { m_LeftArrowBtn, m_RightArrowBtn };
        int CountIDX = 0, ComplateIDX = 0; GetBtns.HForEach(x =>
        {
            var GetIR = x.image.rectTransform;
            if (!_isTweening)
            {
                var GetAPos = GetIR.anchoredPosition;
                GetIR.anchoredPosition = new Vector2(CountIDX == 0 ? 50f : -50f, GetAPos.y);
            }
            else GetIR.DOAnchorPosX(CountIDX == 0 ? -50f : 50f, 0.65f).SetEase(Ease.OutSine).OnComplete(() => 
            {
                ComplateIDX++;
                if(ComplateIDX >= GetBtns.Length) ApplyInfos(false, m_CurrAlbumInfo, () =>
                {
                    m_SelectBtn.interactable = false;
                    m_SelectBtn.transform.DOScale(Vector3.one, 0.65f).
                    SetEase(Ease.InOutExpo).OnComplete(() => m_SelectBtn.interactable = true);
                });
            });
            CountIDX++;
        });
        if (!_isTweening)
        {
            m_isStartRotByCurrAlbumInfo = true;
            m_MainTitle.transform.localScale = Vector3.one;
            m_BounsTItleBG.transform.localScale = Vector3.zero;
            m_SelectBtn.transform.localScale = Vector3.zero;
        }
    }

    #region Main System Private Functions

    private void ApplyInfos(bool _isFirst, AlbumInfo _CurrAlbumInfo, System.Action _EndCallBack = null)
    {
        if (_isFirst)
        {
            //m_FrontPanel = m_MapModuleBG.transform.GetChild(0).GetComponent<SpriteRenderer>();
            m_MapModuleBG = FindAnyObjectByType<SoundGameController>().pp_MainMapModuleBase;
            m_MapModuleBG.sprite = _CurrAlbumInfo.s_SpriteArray[1];
            var GetBGColor = m_MapModuleBG.color; GetBGColor.a = 0;
            m_MapModuleBG.color = GetBGColor;
        }
        if(m_CurrInstanceAlbumModule == null) 
        m_CurrInstanceAlbumModule = Instantiate(m_CurrAlbumModulePrefab, m_MainTitle.rectTransform);
        m_CurrInstanceAlbumModule.Initlization(_CurrAlbumInfo.s_ItemIDX, _CurrAlbumInfo.s_SpriteArray[0], () => 
        {
            m_isStartRotByCurrAlbumInfo = false;
            m_CurrInstanceAlbumModule.transform.rotation = Quaternion.Euler(Vector3.zero);
            m_BounsTItleBG.transform.localScale = Vector3.zero;
            m_BounsTItleBG.gameObject.SetActive(true);
            m_MainTitle.transform.DOScale(Vector3.one * 1.1f, 0.45f).SetEase(Ease.OutBack).OnComplete(() =>
            InitAudioBaouns(true, m_CurrAlbumInfo));
        });

        _EndCallBack?.Invoke();
        BtnReset(true);
    }

    private void OnClickArrowBtn(bool _isLeft)
    {
        
    }

    #region 오디오 이퀄라이저 적용 로직

    private void InitAudioBaouns(bool _isAdd, AlbumInfo _CurrAlbumInfo = null)
    {
        if (m_AudioMixVisualizeSubController == null)
        m_AudioMixVisualizeSubController = FindAnyObjectByType<AudioMixVisualizeSubController>();

        if (_isAdd) m_CurrAudioASinfo = Appinstance.Instance.ms_AudioManager.PlaySound(true,
        _CurrAlbumInfo.m_MainClipBGM.s_isLoop, _CurrAlbumInfo.m_MainClipBGM.s_ItemName, 1, AudioType.BGM, _CurrAlbumInfo.m_MainClipBGM.s_AudioClipsInfo[0], this);

        if (_isAdd) m_CurrElemASBuffer = new ElemASBuffer(/*true, */m_CurrAudioASinfo.s_AudioSource, _CurrAlbumInfo.m_MainClipBGM);
        if (_isAdd) m_AudioMixVisualizeSubController.AddAudioSource(m_CurrElemASBuffer);
        else m_AudioMixVisualizeSubController.RemoveAudioSource(m_CurrElemASBuffer);

        //if (_isAdd) m_AudioMixVisualizeSubController.Initlization(null);
        m_AudioMixVisualizeSubController.BreakPoint(_isAdd, SubModuleControllerBase.SubModuleBreakType.OnlyBreakElems);
        if (_isAdd)
        {
            ModuleMonoBase ApplyMonoBase = null;
            m_BounsTItleBG.Initlization(ApplyMonoBase, m_AudioMixVisualizeSubController);
            m_BounsTItleBG.SemiBreakPoint(true);
        }
        else
        {
            m_CurrAudioASinfo.StopOrComplate();
            m_AudioMixVisualizeSubController.
            InitOutPutAudioSpectrum(false, m_BounsTItleBG);
            m_CurrAudioASinfo = null;
            m_CurrElemASBuffer = null;
        }
    }

    #endregion

    #endregion

    #region Sub System Private Functions

    private void BtnReset(bool _isOn)
    {
        m_LeftArrowBtn.interactable = _isOn;
        m_RightArrowBtn.interactable = _isOn;
        m_SelectBtn.interactable = _isOn;
    }

    private void AfterProductionCamAndGUI(bool _isReturn, float _SetDelay = 0f, System.Action _EndCallBack = null)
    {
        var GetCamPCon = FindAnyObjectByType<CameraProductionSubController>();
        var ApplyEaseByCondition = _isReturn? Ease.InOutBack : Ease.InCirc;
        (this.transform as RectTransform).DOAnchorPosY(_isReturn ? 0f : 960f, _isReturn ? 1.25f : 0.2f).
        SetDelay(_isReturn ? 0.05f : 0f).SetEase(ApplyEaseByCondition).OnComplete(() => { if (_isReturn) _EndCallBack?.Invoke(); });
        GetCamPCon.MoveStageTweeningCam(_isReturn ? 10f : 0f, _SetDelay, _isReturn, () => 
        { if (!_isReturn) _EndCallBack?.Invoke(); });
    }

    #endregion

    #region Main System Public Functions (**Main Process Reference**)

    public override void ExecuteNextStep(SystemInfoType _NextType)
    {
        BtnReset(false);
        m_isPartControllSystem = false;
        var GetDataManager = Appinstance.Instance.ms_DataManager;
        var GetGameManager = Appinstance.Instance.ms_GamePlayManager;
        if (!m_isFirst)
        {
            var GetMyInfo = GetDataManager.TempInitSoundGameMyInfo();
            GetMyInfo.AllMyInfoLoadAndApplyAnyncParsing(null, out System.Collections.IEnumerator[] _GetOutCour);
        }
        else GetGameManager.GetFindControllerBaseByTemp<SoundGameController>().BeforeInitByOutPost();
        if (_NextType == SystemInfoType.NoneMainGUI)
        {
            System.Action EndCallBack = () =>
            {
                InitAudioBaouns(false);
                Appinstance.Instance.ms_AudioManager.PlaySound("GoGameSound");
                AfterProductionCamAndGUI(false, _EndCallBack: () => 
                {
                    GetGameManager.ExecuteChangeAllGameControll(
                    GamePlayManager.GamePlayType.StartingSet, true, new MyItemInfoBuffer(new ItemInfo[1] { m_CurrAlbumInfo }));
                    m_isFirst = true;
                    base.ExecuteNextStep(_NextType);
                    
                });
            };
            if (!m_isUseStartWhiteFlash) EndCallBack();
            else
            {
                var GetCG = GamePlaySystem.Instance.testpp_AllIncludedGUIPanel.gameObject.CheckComnectComponent<CanvasGroup>();
                GetCG.alpha = 0f; GetCG.transform.GetChild(0).gameObject.SetActive(true);
                DOTween.To(() => GetCG.alpha, x => GetCG.alpha = x, 1f, 0.4f).SetEase(Ease.InSine).OnComplete(() => EndCallBack());
            }
            return;
        }
        base.ExecuteNextStep(_NextType);
    }

    private void Update()
    {
        if (m_isStartRotByCurrAlbumInfo && m_CurrInstanceAlbumModule != null)
        m_CurrInstanceAlbumModule.transform.Rotate(Vector3.forward * -300f * Time.deltaTime);
    }

    public override void UpdateProcess()
    {
        base.UpdateProcess();

        //if(m_isStartRotByCurrAlbumInfo && m_CurrAlbumModulePrefab != null)
        //m_CurrAlbumModulePrefab.transform.Rotate(Vector3.forward * -30f * Time.deltaTime);

        if (m_MapModuleBG != null && m_MapModuleBG.sprite != null && m_MapModuleBG.color.a < 1f)
        {
            var GetColor = m_MapModuleBG.color;
            GetColor.a = Mathf.Lerp(GetColor.a, 1, Time.deltaTime * 3f);
            m_MapModuleBG.color = GetColor;
        }
    }
    #endregion
}

[System.Serializable]
public class AlbumInfo : InstanceSpriteInfo
{
    //[BoxGroup("AlbumInfo")] public Color m_ApplyBGColor;
    [BoxGroup("AlbumInfo")] public SoundGameClipInfo m_MainClipBGM;
    [BoxGroup("AlbumInfo")] public SDataSoundGameInfo m_ApplySoundGameInfo;
}
