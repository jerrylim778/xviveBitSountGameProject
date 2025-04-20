using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Commons.Helpers;
using Sirenix.OdinInspector;
using DG.Tweening;
using TMPro;

public class EndGamePopUpUnion : SystemBase, I_PopUpInfo, I_PopUpPush, I_PopUpPop, I_PopUpDecorDem
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

    [Header("Requied Values")]
    [SerializeField] private EndGamePopUpProductionBase m_ProjectionBase;
    [Header("GUIControlNow")]
    [SerializeField, ReadOnly] bool m_isVictory = false;
    [SerializeField, ReadOnly] private float m_TweeningSped = 0f;
    [SerializeField, ReadOnly] EndGameInfo m_RunninginfoNow;
    [SerializeField, Range(0f, 1f)] private float m_SetDemValue = 1f;
    [Header("GUIControl")]
    [SerializeField] Button m_MainExitBtn;
    [SerializeField] private EndGameInfo[] m_EndGameInfo;

    public Button pp_MainExitBtn => m_MainExitBtn;

    [Tooltip("Requied Values")]
    private I_Data m_ApplyData;
    private Dictionary<EndPopUpClickType, System.Action> m_DicAdditionalActions = new();
    private Dictionary<string, object> m_SavedTweeningSet = new();

    #region Required Functions

    public PopUpType I_GetPopUpType() => PopUpType.DefultPopUp;
    public SystemBase I_GetPopUpBase() => this;
    public float I_SetDem() => m_SetDemValue;

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
        //float ExitOGPosY = m_MainExitBtn.image.rectTransform.anchoredPosition.y;
        //m_MainExitBtn.image.rectTransform.anchoredPosition = new Vector2(-120f, ExitOGPosY);
        m_MainExitBtn.image.rectTransform.localScale = Vector3.zero;
        m_MainExitBtn.gameObject.SetActive(false);
        m_ProjectionBase.Initlization(this, m_RunninginfoNow, m_TweeningSped, m_ApplyData);
        if (m_isVictory) m_ProjectionBase.VictorySet(true);
        else m_ProjectionBase.FailSet(true);
    }

    public void InitEndGamePopUp(bool _isVictory, float _MainTweeningSped, 
    I_Data _ApplyData, params System.Tuple<EndPopUpClickType, System.Action>[] _ClickBtnInfosElems)
    {
        m_isVictory = _isVictory;
        m_TweeningSped = _MainTweeningSped;
        m_ApplyData = _ApplyData;
        Initlization();
        _ClickBtnInfosElems.HForEach(x => m_DicAdditionalActions.Add(x.Item1, x.Item2));
        //UISFX로 오디오 관리자로 보낼 수 있도록 수정할것 (**리팩요망**)
        //Appinstance.Instance.ms_AudioManager.PlaySound(m_isVictory ? "Lost_Item_Bonus_v1_wav" : "Lost_Item_Bonus_v2_wav");
    }

    public void PushOutEvent(System.Action _AddCallBack = null)
    {
        if (m_isVictory) m_ProjectionBase.VictorySet(false);
        else m_ProjectionBase.FailSet(false);
    }

    public void PopOutEvent(System.Action _EndCallBack = null)
    {
        SetButton(true);
        _EndCallBack?.Invoke();
        //if (m_isVictory) m_ProjectionBase.VictoryPopAction(_EndCallBack);
        //else m_ProjectionBase.FailPopAction(_EndCallBack);
    }

    public void ReadyToBtnInteraction(bool _isLock = false)
    {
        if (_isLock) { m_MainExitBtn.gameObject.SetActive(false);  return; }

        m_MainExitBtn.gameObject.SetActive(true);
        //m_MainExitBtn.image.rectTransform.DOScale(Vector3.one, 0.5f).SetDelay(/*m_isVictory ? 0.45f : */0.3f).
        //SetEase(Ease.OutBack).OnComplete(() =>
        //{
        //    SetButton(false);
        //    m_MainExitBtn.interactable = true;
        //});
        SetButton(false);
    }

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
            int ApplyIDX = FindBtnType(x);
            if (ApplyIDX != -1) ActionBtns[CountIDX] = m_RunninginfoNow.s_PartBtnByType[ApplyIDX].s_TypeBtn;
            CountIDX++;
        });

        if (_isReset)
        {
            m_MainExitBtn.onClick.RemoveAllListeners();
            ActionBtns.HForEach(x => { if (x != null) x.onClick.RemoveAllListeners(); });
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

    //인터페이스 PopUp의 Decoration을 정의하여 GamePlaySystem에서 Dem의 알파를 설정할 수 있도록 수정한다
    private void ApplyBtn(Button _ApplyBtn, GamePlayManager.GamePlayType _ApplyType, EndPopUpClickType _ButtonType = EndPopUpClickType.None)
    {
        if (_ApplyBtn == null) return;
        _ApplyBtn.onClick.AddListener(() =>
        m_MainControllSystem.MainPopUpPop<EndGamePopUpUnion>(() =>
        {
            if (_ButtonType != EndPopUpClickType.None &&
            m_DicAdditionalActions.ContainsKey(_ButtonType))
                m_DicAdditionalActions[_ButtonType]?.Invoke();
            Appinstance.Instance.ms_GamePlayManager.ExecuteChangeAllGameControll(_ApplyType);
        }));
    }

    public void ParticleControl(ParticleSystem _PlayParticle, bool _isPlay)
    {
        if (_PlayParticle.isPlaying)
        {
            _PlayParticle.Stop();
            if (!_isPlay) return;
        }
        _PlayParticle.Play();
        StartCoroutine(CO_ForceCycleByCondition(() => _PlayParticle.isPlaying, () => _PlayParticle.Play()));
    }

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

    #endregion

    #region 상속하거나 삭제해야될 값
    //[Header("*****인위적인 값 리팩 요망*****")]
    ////인위적인 값 다른곳에서 유저 정보를 가져와 올라간만큼 적용해야한다
    //[SerializeField, ReadOnly] private float test_UserUpExp = 0.35f;

    //#region (**Victory => Stars**) Tweening Support System Private Functions

    ////각 시간에 따라 3분할로 진행하여 3/3이상을 채웠을때 3개 그아래순으로 배치할 수 있도록 수정한다
    //private void SupportStarTweening(bool _isInit, Image _GetMainParImg, ref Dictionary<string, RectTransform[]> _InitDic, System.Action _EndCallBack = null)
    //{
    //    var ReturnValue = _InitDic;
    //    string[] ApplyKeyStrs = new string[3] { "StarParticles", "StarEmptyes", "StarMoveICONs" };
    //    if (_InitDic == null || _InitDic.Count <= 0)
    //    {
    //        Helper.HCountForEach(1, 3, _CountIDX => ReturnValue.Add(
    //        ApplyKeyStrs[_CountIDX - 1], _GetMainParImg.transform.GetChild(_CountIDX).ChildLinearStuctureSearch<RectTransform>()));
    //        _InitDic = ReturnValue;
    //    }

    //    var StarParticle = ReturnValue[ApplyKeyStrs[0]];
    //    var StarEmpty = ReturnValue[ApplyKeyStrs[1]];
    //    var MoveStars = ReturnValue[ApplyKeyStrs[2]];
    //    if (_isInit)
    //    {
    //        MoveStars[MoveStars.Length - 1].localScale = Vector3.one * 3f;
    //        MoveStars[MoveStars.Length - 1].gameObject.CheckComnectComponent<CanvasGroup>().alpha = 0f;
    //        Helper.HCountForEach(0, 1, _CountIDX =>
    //        {
    //            MoveStars[_CountIDX].anchoredPosition = new Vector2(-210f, -79);
    //            MoveStars[_CountIDX].rotation = Quaternion.identity;
    //            MoveStars[_CountIDX].localScale = Vector2.zero;
    //        });
    //        StarParticle.HForEach(x =>
    //        {
    //            x.GetComponent<ParticleSystem>().Stop();
    //            ParticleControl(x.GetComponent<ParticleSystem>(), false);
    //            x.gameObject.SetActive(false);
    //        });
    //        //_InitDic.HForEach(x => x.Value[0].parent.gameObject.SetActive(false));
    //        return;
    //    }

    //    StartMoveEmpty(_InitDic, 0, _EndCallBack);
    //    #region 한번에 전부 할당되는 방식
    //    //Helper.HCountForEach(0, 1, _CountIDX => MoveStars[_CountIDX].DOAnchorPos(StarEmpty[_CountIDX].anchoredPosition,
    //    //m_TweeningSped).OnComplete(() => ParticleControl(StarParticle[_CountIDX].GetComponent<ParticleSystem>(), true)));
    //    //var GetCG = MainStar.gameObject.CheckComnectComponent<CanvasGroup>();
    //    //DOTween.To(() => GetCG.alpha, x => GetCG.alpha = x, 1f, m_TweeningSped).SetEase(Ease.InCirc);
    //    //MainStar.DOScale(Vector3.one, m_TweeningSped).SetEase(Ease.InCirc).OnComplete(() =>
    //    //{
    //    //    ParticleControl(StarParticle[StarParticle.Length - 1].GetComponent<ParticleSystem>(), true);
    //    //    ParticleControl(_GetMainParImg.transform.GetChild(0).GetComponent<ParticleSystem>(), true);
    //    //});
    //    #endregion
    //}

    //private void StartMoveEmpty(Dictionary<string, RectTransform[]> _InitDic, int _RunIDX, System.Action _EndCallBack)
    //{
    //    if (_RunIDX <= 1)
    //    {
    //        //포물선 형태 Path를 사용하여 넣을 수 있도록 
    //        float DelyValue = _RunIDX == 0 ? m_TweeningSped * 0.5f : 0f;
    //        RectTransform MoveStar = _InitDic["StarMoveICONs"][_RunIDX], TargetStar = _InitDic["StarEmptyes"][_RunIDX];
    //        MoveStar.DOScale(Vector3.one, m_TweeningSped).SetDelay(DelyValue * 0.5f).SetEase(Ease.InExpo);
    //        MoveStar.DORotate(TargetStar.eulerAngles, m_TweeningSped * 1.2f).SetDelay(DelyValue).SetEase(Ease.InExpo);
    //        MoveStar.DOPath(CalculateCatmullRom(MoveStar, TargetStar), m_TweeningSped * 1.5f, PathType.CatmullRom).SetDelay(DelyValue).SetEase(Ease.InOutExpo).
    //        OnComplete(() =>
    //        {
    //            var GetPartParticle = _InitDic["StarParticles"][_RunIDX].GetComponent<ParticleSystem>();
    //            GetPartParticle.gameObject.SetActive(true); ParticleControl(GetPartParticle, true);
    //            StartMoveEmpty(_InitDic, _RunIDX + 1, _EndCallBack);
    //        });
    //        //MoveStar.DOAnchorPos(TargetStar.anchoredPosition, m_TweeningSped).SetDelay(m_TweeningSped).SetEase(Ease.InOutExpo).
    //        //pp_ListenModule.transform.DOPath(m_ArrowPathTweening, m_TweeningSped - 0.1f, PathType.CatmullRom)
    //        return;
    //    }

    //    RectTransform MainStar = _InitDic["StarMoveICONs"][_RunIDX],
    //    TargetEmpty = _InitDic["StarEmptyes"][_RunIDX], TargetEvent = _InitDic["StarParticles"][_RunIDX];
    //    var GetCG = MainStar.gameObject.CheckComnectComponent<CanvasGroup>();
    //    DOTween.To(() => GetCG.alpha, x => GetCG.alpha = x, 1f, m_TweeningSped).SetEase(Ease.InOutSine);
    //    MainStar.DOScale(Vector3.one, m_TweeningSped).SetDelay(0.1f).SetEase(Ease.InOutExpo).OnComplete(() =>
    //    {
    //        TargetEvent.gameObject.SetActive(true);
    //        ParticleControl(TargetEvent.GetComponent<ParticleSystem>(), true);
    //        _EndCallBack?.Invoke();
    //    });
    //}

    //private Vector3[] CalculateCatmullRom(RectTransform _StartRT, RectTransform _TargetRT)
    //{
    //    Vector3 SetArrow = -Vector3.up + (Vector3.right * 3f);
    //    Vector3 CalculateArrow = _StartRT.position + SetArrow * 100f;
    //    return new Vector3[3] { _StartRT.position, CalculateArrow, _TargetRT.position };
    //}

    //#endregion

    //#region (**Victory => Slider**) Tweening Support System Private Functions

    //private void SupportSliderTweening(bool _isInit, Slider _GetMainSlider)
    //{
    //    Transform[] SliderTrs = Helper.ChildLinearStuctureSearch(_GetMainSlider.transform);
    //    Image ShowLevelBG = _GetMainSlider.transform.GetChild(SliderTrs.Length - 1).GetComponent<Image>();
    //    TextMeshProUGUI ShowLevelTMPTxt = ShowLevelBG.transform.GetChild(0).GetComponent<TextMeshProUGUI>();
    //    //해당 값은 상위 객체인 Controller에서 모델(데이터)를 가져오는것으로 이후에 변경해야됨(리팩 요망 ******************************)
    //    TextMeshProUGUI TESTShowLevelSliderTMPTxt = _GetMainSlider.transform.GetChild(1).GetComponent<TextMeshProUGUI>();

    //    if (_isInit)
    //    {
    //        //해당 값은 상위 객체인 Controller에서 모델(데이터)를 가져오는것으로 이후에 변경해야됨
    //        TESTShowLevelSliderTMPTxt.text = $"{0}/{100}";

    //        ShowLevelBG.transform.localScale = Vector3.zero;
    //        ShowLevelTMPTxt.rectTransform.localScale = Vector3.one * 2.5f;
    //        ShowLevelTMPTxt.gameObject.CheckComnectComponent<CanvasGroup>().alpha = 0f;
    //        var GetSPosY = _GetMainSlider.image.rectTransform.anchoredPosition.y;
    //        m_SavedTweeningSet.Add("GetMainSliderOGPosX", _GetMainSlider.image.rectTransform.anchoredPosition.x);
    //        _GetMainSlider.image.rectTransform.anchoredPosition = new Vector2(100f, GetSPosY);
    //        _GetMainSlider.gameObject.CheckComnectComponent<CanvasGroup>().alpha = 0f;
    //        _GetMainSlider.value = 0f;
    //        _GetMainSlider.gameObject.SetActive(false);
    //        return;
    //    }

    //    _GetMainSlider.gameObject.SetActive(true);
    //    var GetCG = _GetMainSlider.GetComponent<CanvasGroup>();
    //    DOTween.To(() => GetCG.alpha, x => GetCG.alpha = x, 1f, m_TweeningSped).SetEase(Ease.InOutSine);
    //    float SliderTweeningSped = m_TweeningSped * 1.8f;
    //    _GetMainSlider.image.rectTransform.DOAnchorPosX((float)m_SavedTweeningSet["GetMainSliderOGPosX"], SliderTweeningSped).SetEase(Ease.InOutExpo).OnComplete(() =>
    //    {
    //        //Slider 포함 다른 Tweening 짜잘 요소 + 앞으로 가져와야될 모델(데이터)에 대해 적시이후에 PuasePopUp구현할 수 있도록
    //        //연계하는 버튼들은 모델 => 데이터가 완료되어야 가져올 수 있기 때문에 각 가져와야될 요소들을 정리해서 메모장에 적시할 수 있도록 수정한다
    //        //ShowLevelBG.transform.DORotate(Vector3.forward * 360f, m_TweeningSped).SetEase(Ease.InOutSine).SetLoops(3, LoopType.Yoyo).OnComplete(() => 
    //        //ShowLevelBG.transform.DORotate(Vector3.forward * 180f, SliderTweeningSped).SetLoops(3, LoopType.Restart).OnComplete(() => 
    //        ShowLevelBG.transform.DOScale(Vector3.one, SliderTweeningSped).OnComplete(() =>
    //        {
    //            ShowLevelBG.transform.rotation = Quaternion.identity;
    //            var GetCG = ShowLevelTMPTxt.GetComponent<CanvasGroup>();
    //            DOTween.To(() => GetCG.alpha, x => GetCG.alpha = x, 1f, SliderTweeningSped).SetEase(Ease.InOutSine);
    //            ShowLevelTMPTxt.transform.DOScale(Vector3.one, SliderTweeningSped).SetEase(Ease.InOutExpo);
    //            DOTween.To(() => _GetMainSlider.value, x =>
    //            {
    //                _GetMainSlider.value = x;
    //                TESTShowLevelSliderTMPTxt.text = $"{Mathf.FloorToInt(x * 100)}/{100}";
    //            }, test_UserUpExp, SliderTweeningSped).SetEase(Ease.OutBack);
    //        });
    //    });
    //}

    //#endregion

    #endregion
}

public abstract class EndGamePopUpProductionBase : MonoBehaviour
{
    [SerializeField] protected float m_AvarageTweeningSped;
    [SerializeField, ReadOnly] protected EndGamePopUpUnion m_MainPopUpUnion;
    [SerializeField, ReadOnly] protected EndGamePopUpUnion.EndGameInfo m_RunninginfoNow;
    protected I_Data m_ApplyData = null;

    public virtual void Initlization(EndGamePopUpUnion _GetPopUpUnion, EndGamePopUpUnion.EndGameInfo _RunninginfoNow,
    float _AvarageTweeningSped, I_Data _ApplyData = null)
    {
        m_ApplyData = _ApplyData;
        m_MainPopUpUnion = _GetPopUpUnion;
        m_RunninginfoNow = _RunninginfoNow;
        m_AvarageTweeningSped = _AvarageTweeningSped;
    }
    public abstract void VictorySet(bool _isinit);
    public abstract void FailSet(bool _isinit);

    public virtual void VictoryPopAction(System.Action _EndCallBack = null) { } //Do Nothing...
    public virtual void FailPopAction(System.Action _EndCallBack = null) { } //Do Nothing...
    protected int FindBtnType(EndPopUpClickType _GetType)
    => m_RunninginfoNow.s_PartBtnByType.HFindIndex(x => x.s_BtnType == _GetType);
}