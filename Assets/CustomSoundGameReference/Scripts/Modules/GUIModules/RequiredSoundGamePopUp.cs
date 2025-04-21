using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;
using Sirenix.OdinInspector;
using Commons.Helpers;
using System;

public class RequiredSoundGamePopUp : SystemBaseGUIPopUpRequired, I_PopUpPush, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [Header("Required Values")]
    [SerializeField, ReadOnly] private bool m_isActionProcess;
    [SerializeField, ReadOnly] private SoundGameController m_MainSoundController;
    [Header("Record Reference")]
    [SerializeField] private RectTransform m_ReCordBG;
    [SerializeField] private Button m_ReCordBtn;
    [Header("Music Out Side Reference")]
    [SerializeField] private RectTransform m_MOutSideBG;
    [SerializeField] private Button m_MOutSideMusicStopBtn;
    [SerializeField] private Slider m_MOutSideCycleSlider;
    [Header("ElemMuteController Reference")]
    [SerializeField] private DataMonoMuteModule m_DataMonoMuteModulePrefab;
    [Header("SoundButton Reference")]
    [SerializeField] private ScrollRect m_MainScrollRect;
    [SerializeField] private RectTransform m_ICONModuleBG;
    [SerializeField] private List<DataMonoMusicICONModule> m_DataMonoMusicICONModules = new();

    [Tooltip("Control Values")]
    private bool m_isUsingJoyStick;
    private Sequence m_KillOffSeq, m_KillOnSeq;
    [Tooltip("Required Values")]
    private DataMonoMusicICONModule m_ListanSelectCurrModule;
    

    public DataMonoMusicICONModule pp_SelectCurrModule { get; private set; }

    public override void Initlization()
    {
        base.Initlization();
    }

    public void InitRequiredSoundGamePopUp(SoundGameController _GetController, SoundGameClipInfo[] _GetInfos)
    {
        Initlization();
        m_MainController = _GetController;
        m_MainSoundController = m_MainController as SoundGameController;

        #region Main Events Tweening Init
        //m_ICONModuleBG �ش� �κп� BG�� �ʱ�ȭ ��ų ����� �����Ұ�

        m_ReCordBG.gameObject.SetActive(false);
        m_MOutSideBG.gameObject.SetActive(false);

        m_ReCordBtn.interactable = false;
        m_MOutSideMusicStopBtn.interactable = false;
        m_MOutSideCycleSlider.interactable = false;
        m_MOutSideCycleSlider.value = 0f;

        m_ReCordBG.anchoredPosition = new Vector2(-20f, -20f);
        m_MOutSideBG.anchoredPosition = new Vector2(-80f, -26.8f);
        
        m_ReCordBG.gameObject.CheckComnectComponent<CanvasGroup>().alpha = 0f;
        m_MOutSideBG.gameObject.CheckComnectComponent<CanvasGroup>().alpha = 0f;

        m_MOutSideMusicStopBtn.image.rectTransform.GetChild(0).gameObject.SetActive(true);
        m_MOutSideMusicStopBtn.image.rectTransform.GetChild(1).gameObject.SetActive(false);
        #endregion

        _GetInfos.HForEach(x => 
        {
            var ReciveICON = Instantiate(x.s_PrefabObjs, m_ICONModuleBG);
            LayoutRebuilder.ForceRebuildLayoutImmediate(m_ICONModuleBG);
            var GetModule = ReciveICON.GetComponent<DataMonoMusicICONModule>();
            GetModule.gameObject.CheckComnectComponent<CanvasGroup>().alpha = 0f;
            GetModule.Initlization(this, x); m_DataMonoMusicICONModules.Add(GetModule);
        });
        
    }

    #region Main System Public Functions 

    //Required => MainController
    public bool ISGrapICONModule(out DataMonoMusicICONModule _GetModule)
    {
        bool isHaveListen = m_ListanSelectCurrModule != null;
        //if (isHaveListen && _CompareController != null) 
        //m_ListanSelectCurrModule.SemiBreakPoint(false);
        _GetModule = m_ListanSelectCurrModule;
        m_ListanSelectCurrModule = null;
        
        return isHaveListen;
    }

    //SubController => Required => SubController
    public DataMonoMuteModule InstanceDataMonoMuteModule(Transform _ReciveTr)
    {
        var GetModulePrefab =
        Instantiate(m_DataMonoMuteModulePrefab, this.transform);
        Vector3 screenPos = Camera.main.WorldToScreenPoint(_ReciveTr.position);
        Vector2 canvasLocalPos;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
        GamePlaySystem.Instance.pp_UsingMainCan.GetComponent<RectTransform>(), screenPos, null, out canvasLocalPos))
        {
            canvasLocalPos.y -= 30f;
            GetModulePrefab.GetComponent<RectTransform>().anchoredPosition = canvasLocalPos;
        }
        
        return GetModulePrefab;
    }

    #endregion

    #region Main System Public Functions (**MainProcess**)

    public override void BreakPoint(bool _isBreak, SubModuleControllerBase.SubModuleBreakType _BrackType = 
    SubModuleControllerBase.SubModuleBreakType.BreakAll, Type _GetType = null)
    {
        base.BreakPoint(_isBreak, _BrackType, _GetType);
        m_isActionProcess = _isBreak;
        m_ReCordBtn.interactable = _isBreak;
        m_MOutSideMusicStopBtn.interactable = _isBreak;
        //m_MOutSideCycleSlider.value = 0f;
        m_DataMonoMusicICONModules.HForEach(x => x.SemiBreakPoint(_isBreak));
    }

    public void PushOutEvent(System.Action _AddCallBack = null)
    {
        var ContentsSizeFilter = m_ICONModuleBG.GetComponent<ContentSizeFitter>();
        var GetLG = m_ICONModuleBG.GetComponent<LayoutGroup>();
        ContentsSizeFilter.enabled = false;
        GetLG.enabled = false;


        m_ReCordBG.gameObject.SetActive(true);
        var GetCG = m_ReCordBG.gameObject.CheckComnectComponent<CanvasGroup>();
        DOTween.To(() => GetCG.alpha, x => GetCG.alpha = x, 1f, 0.85f).SetEase(Ease.OutSine);
        m_ReCordBG.DOAnchorPosX(50f, 0.65f).SetEase(Ease.OutCirc);

        m_MOutSideBG.gameObject.SetActive(true);
        var GetCG_2 = m_MOutSideBG.gameObject.CheckComnectComponent<CanvasGroup>();
        DOTween.To(() => GetCG_2.alpha, x => GetCG_2.alpha = x, 1f, 0.85f).SetDelay(0.15f).SetEase(Ease.OutSine);
        m_MOutSideBG.DOAnchorPosX(-50, 0.65f).SetDelay(0.15f).SetEase(Ease.OutCirc);
        
        int ComplateIDX = 0; int CountIDX = 0; float ApplyDur = 0.2f;
        m_DataMonoMusicICONModules.HForEach(x =>
        {
            x.pp_OGRT.anchoredPosition = new Vector2(x.pp_ICONOGAnhoredPos.x, -400f);
            x.gameObject.CheckComnectComponent<CanvasGroup>().alpha = 1f;
            //float ApplyDur = (CountIDX + 1) * 0.1f; //Debug.Log(ApplyDur);
            x.pp_OGRT.DOAnchorPosY(x.pp_ICONOGAnhoredPos.y, ApplyDur/*UnityEngine.Random.Range(0.55f, 1f)*/).
            SetEase(Ease.OutSine).OnComplete(() => 
            {
                ComplateIDX++;
                if (ComplateIDX >= m_DataMonoMusicICONModules.Count)
                {
                    m_MainSoundController.I_CheckSubModuleIsAllCanAction(m_MainController);
                    m_MOutSideMusicStopBtn.onClick.AddListener(OnClickMSideBtn);
                    ContentsSizeFilter.enabled = true;
                    GetLG.enabled = true;
                }
            });
            ApplyDur += 0.15f; CountIDX++;
        });
        
    }

    #endregion

    #region Sub System Private Functions (**Main Button Events**)

    private void OnClickMSideBtn()
    {
        bool isMusicOn = m_MainSoundController.MusicPlayOrPause();
        var OffBtnICON = m_MOutSideMusicStopBtn.image.rectTransform.GetChild(isMusicOn ? 1 : 0);
        var OnBtnICON = m_MOutSideMusicStopBtn.image.rectTransform.GetChild(isMusicOn ? 0 : 1);
        ResetSequce(m_KillOffSeq); 
        m_KillOffSeq = DOTween.Sequence(); 

        m_KillOffSeq.Append(OffBtnICON.DOScale(Vector3.zero, 0.2f).SetEase(Ease.InExpo).OnComplete(() => 
        {
            ResetSequce(m_KillOnSeq);
            m_KillOnSeq = DOTween.Sequence();
            OffBtnICON.gameObject.SetActive(false);
            OnBtnICON.transform.localScale = Vector3.zero;
            OnBtnICON.gameObject.SetActive(true);
            m_KillOnSeq.Append(OnBtnICON.DOScale(Vector3.one, 0.2f).SetEase(Ease.InExpo));
        }));
    }

    //���Ŀ� �ݵ�� ���� Helper.AssetPackage<Ŭ����>�� ������ų��
    private void ResetSequce(Sequence _GetSeq)
    {
        if (_GetSeq != null)
        {
            _GetSeq.Complete();
            _GetSeq.Kill();
        }
        _GetSeq = null;
    }

    #endregion

    #region 유니티 이벤트 함수
    //추가로 작업할 팝업이 SubPopup으로부터 상속받고 ControllerBase에서 동작하도록 만들어야 한다

    public void OnPointerDown(PointerEventData eventData)
    {
        m_ListanSelectCurrModule = null;
        if (m_isUsingJoyStick || !m_isActionProcess) return;
        if (!eventData.pointerCurrentRaycast.gameObject.TryGetComponent<DataMonoMusicICONModule>(out
        DataMonoMusicICONModule _GetModule) || !_GetModule.pp_isProcessAction) return;

        if ((!m_DataMonoMusicICONModules.Exists(x => x.m_ItemIDX == _GetModule.m_ItemIDX)).HDebug("[논리오류]", Helper.HDType.Error))
            return;

        m_MainScrollRect.enabled = false;
        pp_SelectCurrModule = _GetModule; 
        pp_SelectCurrModule.PickEvent();

        m_isUsingJoyStick = true;

        OnDrag(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!m_isUsingJoyStick || pp_SelectCurrModule == null)
            return;

        pp_SelectCurrModule.pp_SetRT.position = eventData.position;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!m_isUsingJoyStick || pp_SelectCurrModule == null)
            return;

        pp_SelectCurrModule.pp_SetRT.anchoredPosition = pp_SelectCurrModule.pp_EmptyRT.anchoredPosition;
        pp_SelectCurrModule.ResetEvnet();

        m_ListanSelectCurrModule = pp_SelectCurrModule;
        pp_SelectCurrModule = null;
        m_MainScrollRect.enabled = true;

        m_isUsingJoyStick = false;
    }


    public override void UpdateProcess()
    {
        base.UpdateProcess();

        if (!m_isActionProcess) return;

        if (!m_MainSoundController.PlayingNow() || !m_MainSoundController.pp_isMusicOn) return;

        m_MOutSideCycleSlider.value = m_MainSoundController.pp_CycleCurrTime;
    }


    #endregion
}