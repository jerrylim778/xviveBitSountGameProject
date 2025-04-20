using System.Collections.Generic;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Commons.Helpers;
using Sirenix.OdinInspector;
using DG.Tweening;
using TMPro;


public class EndGamePopUp : SystemBase, I_PopUpInfo, I_PopUpPush, I_PopUpPop
{
    #region MatchFactory관련 EndPopUp의 적용
    //EventListenType => MFLController_Continue 에 관한 설명
    //1.시간초과 =>  시간을 1분 더 줘서 그대로 시작한다
    //2.단상 Max => 모든 아이템을 다시 바운더리로 뿌리고 그대로 시작한다 
    //카드에 있던 정보는 (단상에 있는것만) 초기화 시킨다
    #endregion

    #region EndPopUp Infos
    

    [System.Serializable]
    public struct EndGameInfo
    {
        public bool s_isVictory;
        public RectTransform s_TitleBG;
        public RectTransform s_TitleInfos;
        public EndGameBtnInfo[] s_PartBtnByType;
    }

    [System.Serializable]
    public struct EndGameBtnInfo
    {
        public EndPopUpClickType s_BtnType;
        public Button s_TypeBtn;
    }


    #endregion

    [Header("GUIControlNow")]
    [SerializeField, ReadOnly] bool m_isVictory = false;
    [SerializeField, ReadOnly] private float m_TweeningSped = 0f;
    [SerializeField, ReadOnly] EndGameInfo m_RunninginfoNow;
    [Header("GUIControl")]
    [SerializeField] Button m_MainExitBtn;
    [SerializeField] private EndGameInfo[] m_EndGameInfo;

    [Tooltip("Requied Values")]
    private Dictionary<EndPopUpClickType, System.Action> m_DicAdditionalActions = new();
    private Dictionary<string, object> m_SavedTweeningSet = new();

    #region Required Functions

    public PopUpType I_GetPopUpType() => PopUpType.DefultPopUp;
    public SystemBase I_GetPopUpBase() => this;

    #endregion

    public override void Initlization()
    {
        //All Init        
        m_DicAdditionalActions.Clear();
        m_RunninginfoNow = m_EndGameInfo.HFind(x => m_isVictory == x.s_isVictory);
        var GetTrs = Helper.ChildLinearStuctureSearch(this.transform);
        GetTrs[0].gameObject.SetActive(m_isVictory);
        GetTrs[1].gameObject.SetActive(!m_isVictory);
        Helper.ChildLinearStuctureSearch(GetTrs[GetTrs.Length - 1]).HForEach(x => x.gameObject.SetActive(false));
        m_MainExitBtn.onClick.RemoveAllListeners();
        //Tweening init
        float ExitOGPosY = m_MainExitBtn.image.rectTransform.anchoredPosition.y;
        m_MainExitBtn.image.rectTransform.anchoredPosition = new Vector2(150f, ExitOGPosY);
        if (m_isVictory) VictorySet(true);
        else FailSet(true);
    }

    public void InitEndGamePopUp(bool _isVictory, float _MainTweeningSped, params System.Tuple<EndPopUpClickType, System.Action>[] _ClickBtnInfosElems)
    {
        m_isVictory = _isVictory;
        m_TweeningSped = _MainTweeningSped;
        _ClickBtnInfosElems.HForEach(x => m_DicAdditionalActions.Add(x.Item1, x.Item2));
        //UISFX로 오디오 관리자로 보낼 수 있도록 수정할것 (**리팩요망**)
        //Appinstance.Instance.ms_AudioManager.PlaySound(m_isVictory ? "Lost_Item_Bonus_v1_wav" : "Lost_Item_Bonus_v2_wav"); 
        Initlization();
    }

    public void PushOutEvent(System.Action _AddCallBack = null)
    {
        if (m_isVictory) VictorySet(false);
        else FailSet(false);
    }

    public void PopOutEvent(System.Action _EndCallBack = null)
    {
        SetButton(true);
        _EndCallBack?.Invoke();
    }

    #region (**Tweeing => Victory**) Sub System Private Functions

    private void VictorySet(bool _isinit)
    {
        //ParticleSystem MainParticle = m_RunninginfoNow.s_TitleBG.GetChild(0).GetComponent<ParticleSystem>();
        Image MainTitleBG_Board = m_RunninginfoNow.s_TitleBG.GetChild(0).GetComponent<Image>();
        Image MainTitleBG_Star = m_RunninginfoNow.s_TitleBG.GetChild(1).GetComponent<Image>();
        #region Board  Sub Reference
        //Title
        Image[] SubImgs = new Image[2]; Helper.HCountForEach(0, 1, _CountIDX =>
        SubImgs[_CountIDX] = MainTitleBG_Board.transform.GetChild(_CountIDX).GetComponent<Image>());
        TextMeshProUGUI BoardTMPTxt = MainTitleBG_Board.transform.GetChild(2).GetComponent<TextMeshProUGUI>();
        //Start
        Dictionary<string, RectTransform[]> DicStarPackage = new();
        ParticleSystem MainParticle = MainTitleBG_Star.transform.GetChild(0).GetComponent<ParticleSystem>();
        #endregion
        Slider Slider_EXP = m_RunninginfoNow.s_TitleInfos.GetChild(0).GetComponent<Slider>();
        RectTransform RewordBG_Now = m_RunninginfoNow.s_TitleInfos.GetChild(1).GetComponent<RectTransform>();
        RectTransform RewordBG_NextGame = m_RunninginfoNow.s_TitleInfos.GetChild(2).GetComponent<RectTransform>();
        Button DefultReStartBtn = m_RunninginfoNow.s_PartBtnByType[FindBtnType(EndPopUpClickType.DefultReStart)].s_TypeBtn;
        Button GoNextStageBtn = m_RunninginfoNow.s_PartBtnByType[FindBtnType(EndPopUpClickType.GoNextLevel)].s_TypeBtn;

        if (_isinit)
        {
            SubImgs.HForEach(x => x.transform.localScale = Vector3.zero);
            //Bordar => Title
            m_SavedTweeningSet.Add("BoardTMPTxtOGAPosY", BoardTMPTxt.rectTransform.anchoredPosition.y);
            BoardTMPTxt.rectTransform.anchoredPosition = Vector2.up * -50f;
            MainTitleBG_Board.transform.localScale = Vector3.zero; MainTitleBG_Board.gameObject.SetActive(false);
            //Bordar => Start
            Color GetColor = MainTitleBG_Star.color;
            m_SavedTweeningSet.Add("MainTitleBG_StarOGColor", GetColor);
            GetColor.a = 0f; MainTitleBG_Star.color = GetColor;
            m_SavedTweeningSet.Add("MainParticleOGScale", MainParticle.transform.localScale);
            MainParticle.Stop();
            MainParticle.transform.localScale = Vector3.zero;
            MainParticle.gameObject.SetActive(false);
            SupportStarTweening(true, MainTitleBG_Star, ref DicStarPackage);
            //info => Slider
            SupportSliderTweening(true, Slider_EXP);

            RewordBG_Now.gameObject.SetActive(false); RewordBG_NextGame.gameObject.SetActive(false);
            GoNextStageBtn.transform.localScale = Vector3.zero; DefultReStartBtn.transform.localScale = Vector3.zero; //Button Init
            return;
        }

        MainTitleBG_Board.gameObject.SetActive(true);
        float BoardSped = m_TweeningSped * 3.5f;
        MainTitleBG_Board.transform.DOScale(Vector3.one, BoardSped).SetEase(Ease.OutBack).OnComplete(() =>
        SubImgs.HForEach(x => x.transform.DOScale(Vector3.one, BoardSped).SetDelay(0.35f).SetEase(Ease.InOutBack))); //돌리는것으로 이후에 변경할것
        BoardTMPTxt.rectTransform.DOAnchorPosY((float)m_SavedTweeningSet["BoardTMPTxtOGAPosY"], BoardSped).SetDelay(0.85f).SetEase(Ease.OutExpo);

        MainTitleBG_Star.DOColor((Color)m_SavedTweeningSet["MainTitleBG_StarOGColor"], BoardSped * 1.8f);
        SupportSliderTweening(false, Slider_EXP);
        SupportStarTweening(false, MainTitleBG_Star, ref DicStarPackage, () =>
        {
            MainParticle.gameObject.SetActive(true); ParticleControl(MainParticle, true);
            MainParticle.transform.DOScale((Vector3)m_SavedTweeningSet["MainParticleOGScale"], m_TweeningSped).SetEase(Ease.InOutExpo);
            //Button OutPut
            m_MainExitBtn.gameObject.SetActive(true); DefultReStartBtn.gameObject.SetActive(true); GoNextStageBtn.gameObject.SetActive(true);
            DefultReStartBtn.transform.DOScale(Vector3.one, m_TweeningSped * 1.2f).SetEase(Ease.InOutBack);
            GoNextStageBtn.transform.DOScale(Vector3.one, m_TweeningSped * 1.2f).SetEase(Ease.InOutBack).SetDelay(0.1f);
            m_MainExitBtn.image.rectTransform.DOAnchorPosX(0f, m_TweeningSped * 1.15f).SetEase(Ease.OutBack).SetDelay(0.25f).OnComplete(() => SetButton(false));
        });
    }

    #endregion

    #region (**Tweeing => Fail**) Sub System Private Functions
    private void FailSet(bool _isinit)
    {
        ParticleSystem MainParticle = m_RunninginfoNow.s_TitleBG.GetChild(0).GetComponent<ParticleSystem>();
        TextMeshProUGUI TmpMainTxt = m_RunninginfoNow.s_TitleBG.GetChild(1).GetComponent<TextMeshProUGUI>();
        TextMeshProUGUI SubTxt_Long = m_RunninginfoNow.s_TitleInfos.GetChild(0).GetComponent<TextMeshProUGUI>();
        TextMeshProUGUI SubTxt_Short = m_RunninginfoNow.s_TitleInfos.GetChild(1).GetComponent<TextMeshProUGUI>();
        Button UseItemContinueBtn = m_RunninginfoNow.s_PartBtnByType[FindBtnType(EndPopUpClickType.UseItemContinue)].s_TypeBtn;
        Button ContinueBtn = m_RunninginfoNow.s_PartBtnByType[FindBtnType(EndPopUpClickType.UseItemReStart)].s_TypeBtn;
        Image SubCountICON = m_RunninginfoNow.s_TitleInfos.GetChild(2).GetComponent<Image>();
        Image SubMaskICON = m_RunninginfoNow.s_TitleInfos.GetChild(3).GetComponent<Image>();
        if (_isinit)
        {
            TmpMainTxt.transform.localScale = Vector3.one * 2f;
            TmpMainTxt.rectTransform.anchoredPosition = new Vector2(-1000f , -250f);
            m_SavedTweeningSet.Add("MainParticleOGScale", MainParticle.gameObject.transform.localScale);
            m_SavedTweeningSet.Add("SubTxt_Long_OGTxt", SubTxt_Long.text);
            m_SavedTweeningSet.Add("SubTxt_Short_OGTxt", SubTxt_Short.text);
            MainParticle.gameObject.transform.localScale = Vector3.zero;
            SubTxt_Long.text = string.Empty; SubTxt_Short.text = string.Empty;
            (SubMaskICON.transform.GetChild(0) as RectTransform).anchoredPosition = Vector2.right * -90f;
            SubCountICON.transform.localScale = Vector3.zero;
            SubCountICON.gameObject.SetActive(false);
            ContinueBtn.transform.localScale = Vector3.zero; UseItemContinueBtn.transform.localScale = Vector3.zero;
            return;
        }

        //각 버튼들 클릭시 자동 PopUpPop이후에 ReStart할 수 있도록
        //PopUpPop일때는 해당 타입에 따라 각 버튼들을 리셋시킬 수 있도록 수정한다
        ParticleControl(MainParticle, true);
        MainParticle.gameObject.transform.DOScale(
        (Vector3)m_SavedTweeningSet["MainParticleOGScale"], m_TweeningSped * 1.8f).SetDelay(0.15f).SetEase(Ease.InOutExpo);
        TmpMainTxt.rectTransform.DOAnchorPosX(-80f, m_TweeningSped * 1.5f).OnComplete(() =>
        TmpMainTxt.rectTransform.DOAnchorPosX(0f, m_TweeningSped * 2f).SetEase(Ease.OutCirc).OnComplete(() =>
        {
            TmpMainTxt.rectTransform.DOScale(Vector3.one, m_TweeningSped).SetEase(Ease.InOutExpo);
            TmpMainTxt.rectTransform.DOAnchorPosY(-100f, m_TweeningSped).SetEase(Ease.InOutExpo).OnComplete(() =>
            {
                SubCountICON.gameObject.SetActive(true);
                SubTxt_Long.DOTmpText(m_SavedTweeningSet["SubTxt_Long_OGTxt"].ToString(), m_TweeningSped * 1.1f, true, ScrambleMode.None).OnComplete(() =>
                SubTxt_Short.DOTmpText(m_SavedTweeningSet["SubTxt_Short_OGTxt"].ToString(), m_TweeningSped, true, ScrambleMode.None).OnComplete(() => 
                {
                    SubCountICON.rectTransform.DOScale(Vector3.one, m_TweeningSped).SetEase(Ease.OutBack).OnComplete(() => 
                    {
                        (SubMaskICON.rectTransform.GetChild(0) as RectTransform).DOAnchorPosX(0f, m_TweeningSped).SetEase(Ease.OutExpo);
                        m_MainExitBtn.gameObject.SetActive(true); ContinueBtn.gameObject.SetActive(true); UseItemContinueBtn.gameObject.SetActive(true);
                        UseItemContinueBtn.transform.DOScale(Vector3.one, m_TweeningSped * 1.2f).SetEase(Ease.InOutBack);
                        ContinueBtn.transform.DOScale(Vector3.one, m_TweeningSped * 1.2f).SetEase(Ease.InOutBack).SetDelay(0.1f);
                        m_MainExitBtn.image.rectTransform.DOAnchorPosX(0f, m_TweeningSped * 1.15f).SetEase(Ease.OutBack).SetDelay(0.25f).OnComplete(() => SetButton(false));
                    });
                }));
            });
        }));
    }
    #endregion

    #region Sub System Private Functions

    private int FindBtnType(EndPopUpClickType _GetType)
    => m_RunninginfoNow.s_PartBtnByType.HFindIndex(x => x.s_BtnType == _GetType);

    private void SetButton(bool _isReset)
    {
        EndPopUpClickType[] AllTypeByType = new EndPopUpClickType[2];
        Button[] ActionBtns = new Button[2];
        AllTypeByType[0] = m_isVictory ? EndPopUpClickType.DefultReStart : EndPopUpClickType.UseItemContinue;
        AllTypeByType[1] = m_isVictory ? EndPopUpClickType.GoNextLevel : EndPopUpClickType.UseItemReStart;
        int CountIDX = 0; AllTypeByType.HForEach(x => 
        {
            ActionBtns[CountIDX] = m_RunninginfoNow.s_PartBtnByType[FindBtnType(x)].s_TypeBtn;
            CountIDX++;
        });

        if (_isReset)
        {
            m_MainExitBtn.onClick.RemoveAllListeners();
            ActionBtns.HForEach(x => x.onClick.RemoveAllListeners());
            return;
        }

        ApplyBtn(m_MainExitBtn, GamePlayManager.GamePlayType.End);
        CountIDX = 0; AllTypeByType.HForEach(x =>
        {
            var GetTuple = TupleByReturnType(x);
            ApplyBtn(ActionBtns[CountIDX], GetTuple.Item1, GetTuple.Item2);
            CountIDX++;
        });
        
    }

    private System.Tuple<GamePlayManager.GamePlayType, EndPopUpClickType> TupleByReturnType(EndPopUpClickType _CompareType)
    {
        GamePlayManager.GamePlayType GetPlayType = _CompareType switch
        {
            EndPopUpClickType.DefultReStart => GamePlayManager.GamePlayType.RePlay, //**같은** 레벨(모델데이터)를 가지고 다시 씬으로 진입
            EndPopUpClickType.GoNextLevel => GamePlayManager.GamePlayType.RePlay, //**다른** 레벨(모델데이터)를 가지고 다시 씬으로 진입
            EndPopUpClickType.UseItemContinue => GamePlayManager.GamePlayType.Play, //각 조건에 맞게 다른 결과를 주고 그대로 플레이
            EndPopUpClickType.UseItemReStart => GamePlayManager.GamePlayType.RePlay, //**같은** 레벨(모델데이터)를 가지고 다시 씬으로 진입
            _ => GamePlayManager.GamePlayType.None
        };
        return new System.Tuple<GamePlayManager.GamePlayType, EndPopUpClickType>(GetPlayType, _CompareType);
    }

    private void ApplyBtn(Button _ApplyBtn, GamePlayManager.GamePlayType _ApplyType, EndPopUpClickType _ButtonType = EndPopUpClickType.None)
    {
        _ApplyBtn.onClick.AddListener(() =>
        {
            m_MainControllSystem.MainPopUpPop<EndGamePopUp>();
            if(_ButtonType != EndPopUpClickType.None && 
            m_DicAdditionalActions.ContainsKey(_ButtonType)) m_DicAdditionalActions[_ButtonType]?.Invoke();
            Appinstance.Instance.ms_GamePlayManager.ExecuteChangeAllGameControll(_ApplyType);
        });
    }

    private void ParticleControl(ParticleSystem _PlayParticle, bool _isPlay)
    {
        if (_PlayParticle.isPlaying)
        {
            _PlayParticle.Stop();
            if (!_isPlay) return;
        }
        _PlayParticle.Play();
        StartCoroutine(CO_ForceCycleByCondition(() => _PlayParticle.isPlaying, () => _PlayParticle.Play()));
    }

    #endregion

    #region (**Victory => Stars**) Tweening Support System Private Functions

    //각 시간에 따라 3분할로 진행하여 3/3이상을 채웠을때 3개 그아래순으로 배치할 수 있도록 수정한다
    private void SupportStarTweening(bool _isInit, Image _GetMainParImg, ref Dictionary<string, RectTransform[]> _InitDic, System.Action _EndCallBack = null)
    {
        var ReturnValue = _InitDic;
        string[] ApplyKeyStrs = new string[3] { "StarParticles", "StarEmptyes", "StarMoveICONs" };
        if (_InitDic == null || _InitDic.Count <= 0)
        {
            Helper.HCountForEach(1, 3, _CountIDX => ReturnValue.Add(
            ApplyKeyStrs[_CountIDX - 1], _GetMainParImg.transform.GetChild(_CountIDX).ChildLinearStuctureSearch<RectTransform>()));
            _InitDic = ReturnValue;
        }

        var StarParticle = ReturnValue[ApplyKeyStrs[0]];
        var StarEmpty = ReturnValue[ApplyKeyStrs[1]];
        var MoveStars = ReturnValue[ApplyKeyStrs[2]];
        if (_isInit)
        {
            MoveStars[MoveStars.Length - 1].localScale = Vector3.one * 3f;
            MoveStars[MoveStars.Length - 1].gameObject.CheckComnectComponent<CanvasGroup>().alpha = 0f;
            Helper.HCountForEach(0, 1, _CountIDX => 
            {
                MoveStars[_CountIDX].anchoredPosition = new Vector2(-210f, -79);
                MoveStars[_CountIDX].rotation = Quaternion.identity;
                MoveStars[_CountIDX].localScale = Vector2.zero;
            });
            StarParticle.HForEach(x =>
            {
                x.GetComponent<ParticleSystem>().Stop();
                ParticleControl(x.GetComponent<ParticleSystem>(), false);
                x.gameObject.SetActive(false);
            });
            //_InitDic.HForEach(x => x.Value[0].parent.gameObject.SetActive(false));
            return;
        }

        StartMoveEmpty(_InitDic, 0, _EndCallBack);
        #region 한번에 전부 할당되는 방식
        //Helper.HCountForEach(0, 1, _CountIDX => MoveStars[_CountIDX].DOAnchorPos(StarEmpty[_CountIDX].anchoredPosition,
        //m_TweeningSped).OnComplete(() => ParticleControl(StarParticle[_CountIDX].GetComponent<ParticleSystem>(), true)));
        //var GetCG = MainStar.gameObject.CheckComnectComponent<CanvasGroup>();
        //DOTween.To(() => GetCG.alpha, x => GetCG.alpha = x, 1f, m_TweeningSped).SetEase(Ease.InCirc);
        //MainStar.DOScale(Vector3.one, m_TweeningSped).SetEase(Ease.InCirc).OnComplete(() =>
        //{
        //    ParticleControl(StarParticle[StarParticle.Length - 1].GetComponent<ParticleSystem>(), true);
        //    ParticleControl(_GetMainParImg.transform.GetChild(0).GetComponent<ParticleSystem>(), true);
        //});
        #endregion
    }

    private void StartMoveEmpty(Dictionary<string, RectTransform[]> _InitDic, int _RunIDX, System.Action _EndCallBack)
    {
        if(_RunIDX <= 1)
        {
            //포물선 형태 Path를 사용하여 넣을 수 있도록 
            float DelyValue = _RunIDX == 0 ? m_TweeningSped * 0.5f : 0f;
            RectTransform MoveStar = _InitDic["StarMoveICONs"][_RunIDX], TargetStar = _InitDic["StarEmptyes"][_RunIDX];
            MoveStar.DOScale(Vector3.one, m_TweeningSped).SetDelay(DelyValue * 0.5f).SetEase(Ease.InExpo);
            MoveStar.DORotate(TargetStar.eulerAngles, m_TweeningSped * 1.2f).SetDelay(DelyValue).SetEase(Ease.InExpo);
            MoveStar.DOPath(CalculateCatmullRom(MoveStar, TargetStar), m_TweeningSped * 1.5f, PathType.CatmullRom).SetDelay(DelyValue).SetEase(Ease.InOutExpo).
            OnComplete(() =>
            {
                var GetPartParticle = _InitDic["StarParticles"][_RunIDX].GetComponent<ParticleSystem>();
                GetPartParticle.gameObject.SetActive(true); ParticleControl(GetPartParticle, true);
                StartMoveEmpty(_InitDic, _RunIDX + 1, _EndCallBack);
            });
            //MoveStar.DOAnchorPos(TargetStar.anchoredPosition, m_TweeningSped).SetDelay(m_TweeningSped).SetEase(Ease.InOutExpo).
            //pp_ListenModule.transform.DOPath(m_ArrowPathTweening, m_TweeningSped - 0.1f, PathType.CatmullRom)
            return;
        }

        RectTransform MainStar = _InitDic["StarMoveICONs"][_RunIDX],
        TargetEmpty = _InitDic["StarEmptyes"][_RunIDX], TargetEvent = _InitDic["StarParticles"][_RunIDX];
        var GetCG = MainStar.gameObject.CheckComnectComponent<CanvasGroup>();
        DOTween.To(() => GetCG.alpha, x => GetCG.alpha = x, 1f, m_TweeningSped).SetEase(Ease.InOutSine);
        MainStar.DOScale(Vector3.one, m_TweeningSped).SetDelay(0.1f).SetEase(Ease.InOutExpo).OnComplete(() =>
        {
            TargetEvent.gameObject.SetActive(true);
            ParticleControl(TargetEvent.GetComponent<ParticleSystem>(), true);
            _EndCallBack?.Invoke();
        });
    }

    private Vector3[] CalculateCatmullRom(RectTransform _StartRT, RectTransform _TargetRT)
    {
        Vector3 SetArrow = -Vector3.up + (Vector3.right * 3f); 
        Vector3 CalculateArrow = _StartRT.position + SetArrow * 100f;
        return new Vector3[3] { _StartRT.position, CalculateArrow, _TargetRT.position };
    }

    #endregion

    [Header("*****인위적인 값 리팩 요망*****")]
    //인위적인 값 다른곳에서 유저 정보를 가져와 올라간만큼 적용해야한다
    [SerializeField, ReadOnly] private float test_UserUpExp = 0.35f;

    #region (**Victory => Slider**) Tweening Support System Private Functions

    private void SupportSliderTweening(bool _isInit, Slider _GetMainSlider)
    {
        Transform[] SliderTrs = Helper.ChildLinearStuctureSearch(_GetMainSlider.transform);
        Image ShowLevelBG = _GetMainSlider.transform.GetChild(SliderTrs.Length - 1).GetComponent<Image>();
        TextMeshProUGUI ShowLevelTMPTxt = ShowLevelBG.transform.GetChild(0).GetComponent<TextMeshProUGUI>();
        //해당 값은 상위 객체인 Controller에서 모델(데이터)를 가져오는것으로 이후에 변경해야됨(리팩 요망 ******************************)
        TextMeshProUGUI TESTShowLevelSliderTMPTxt = _GetMainSlider.transform.GetChild(1).GetComponent<TextMeshProUGUI>();
        
        if (_isInit)
        {
            //해당 값은 상위 객체인 Controller에서 모델(데이터)를 가져오는것으로 이후에 변경해야됨
            TESTShowLevelSliderTMPTxt.text = $"{0}/{100}";

            ShowLevelBG.transform.localScale = Vector3.zero;
            ShowLevelTMPTxt.rectTransform.localScale = Vector3.one * 2.5f;
            ShowLevelTMPTxt.gameObject.CheckComnectComponent<CanvasGroup>().alpha = 0f;
            var GetSPosY = _GetMainSlider.image.rectTransform.anchoredPosition.y;
            m_SavedTweeningSet.Add("GetMainSliderOGPosX", _GetMainSlider.image.rectTransform.anchoredPosition.x);
            _GetMainSlider.image.rectTransform.anchoredPosition = new Vector2(100f, GetSPosY);
            _GetMainSlider.gameObject.CheckComnectComponent<CanvasGroup>().alpha = 0f;
            _GetMainSlider.value = 0f;
            _GetMainSlider.gameObject.SetActive(false);
            return;
        }

        _GetMainSlider.gameObject.SetActive(true);
        var GetCG = _GetMainSlider.GetComponent<CanvasGroup>();
        DOTween.To(() => GetCG.alpha, x => GetCG.alpha = x, 1f, m_TweeningSped).SetEase(Ease.InOutSine);
        float SliderTweeningSped = m_TweeningSped * 1.8f;
        _GetMainSlider.image.rectTransform.DOAnchorPosX((float)m_SavedTweeningSet["GetMainSliderOGPosX"], SliderTweeningSped).SetEase(Ease.InOutExpo).OnComplete(() =>
        {
            //Slider 포함 다른 Tweening 짜잘 요소 + 앞으로 가져와야될 모델(데이터)에 대해 적시이후에 PuasePopUp구현할 수 있도록
            //연계하는 버튼들은 모델 => 데이터가 완료되어야 가져올 수 있기 때문에 각 가져와야될 요소들을 정리해서 메모장에 적시할 수 있도록 수정한다
            //ShowLevelBG.transform.DORotate(Vector3.forward * 360f, m_TweeningSped).SetEase(Ease.InOutSine).SetLoops(3, LoopType.Yoyo).OnComplete(() => 
            //ShowLevelBG.transform.DORotate(Vector3.forward * 180f, SliderTweeningSped).SetLoops(3, LoopType.Restart).OnComplete(() => 
            ShowLevelBG.transform.DOScale(Vector3.one, SliderTweeningSped).OnComplete(() => 
            {
                ShowLevelBG.transform.rotation = Quaternion.identity;
                var GetCG = ShowLevelTMPTxt.GetComponent<CanvasGroup>();
                DOTween.To(() => GetCG.alpha, x => GetCG.alpha = x, 1f, SliderTweeningSped).SetEase(Ease.InOutSine);
                ShowLevelTMPTxt.transform.DOScale(Vector3.one, SliderTweeningSped).SetEase(Ease.InOutExpo);
                DOTween.To(() => _GetMainSlider.value, x => 
                {
                    _GetMainSlider.value = x;
                    TESTShowLevelSliderTMPTxt.text = $"{Mathf.FloorToInt(x * 100)}/{100}";
                }, test_UserUpExp, SliderTweeningSped).SetEase(Ease.OutBack);
            });
        });
    }

    #endregion

    #region 각 코루틴 함수 전부 Helper로 적용할 수 있도록

    IEnumerator CO_ForceCycleByCondition(System.Func<bool> _ComparePreMatch, System.Action _RunningCallBack)
    {
        bool isCheckCycle = false;
        while (true)
        {
            yield return null;
            _RunningCallBack();
            isCheckCycle = _ComparePreMatch();
            if (isCheckCycle) break;
        }
    }

    IEnumerator CO_WaitControl(float _WaitTime, System.Action _EndCallBack, YieldInstruction _ConditionYield = null)
    {
        if (_ConditionYield != null) yield return _ConditionYield;
        yield return new WaitForSeconds(_ConditionYield != null ? _WaitTime * 0.5f : _WaitTime);
        _EndCallBack?.Invoke();
    }

    #endregion
}

public enum EndPopUpClickType 
{
    None = 0, 
    UseItemContinue, 
    UseItemReStart, 
    DefultReStart, 
    GoNextLevel, 
    End 
}