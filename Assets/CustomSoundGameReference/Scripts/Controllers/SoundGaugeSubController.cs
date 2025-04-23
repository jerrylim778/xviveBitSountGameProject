using UnityEngine;
using Sirenix.OdinInspector;
using DG.Tweening;
using Commons.Helpers;
using System;

public class SoundGaugeSubController : SubModuleControllerBase
{
    //조건에 관한 설명
    //1.소리가 찼을때 Mask효과를 Tweening형태로 적용할 수 있도록 수정한다
    //각 소리 정보들이 찼을때 함수를 호출함으로써 게이지가 찰 수 있도록
    //2.소리가 없으면 게이지가 오르지 않고
    //2-1.소리가 하나라도 있으면 게이지가 서서히 오를 수 있도록
    //2-2.소리하나당 0.2의 추가 적용이 있으며 사라질땐 없어진다

    [Header("Control Values")]
    [SerializeField, ReadOnly] private bool m_isUpdateSliderValue;
    [Header("SR Control Values")]
    [SerializeField] private Transform m_SliderTRVer;
    [SerializeField] private Transform m_SubDcorMask;
    [Header("Required Valeus")]
    [SerializeField] private CameraProductionSubController m_CamProductionCon;

    [Tooltip("Required Values")]
    private Sequence m_SeqMask, m_SeqMaskComplate;
    private SoundGameController m_SoundMainController;

    public float pp_GaugeValue { get; private set; }

    public override object[] Initlization(params object[] _ParsingParams)
    {
        var GetPartParms = base.Initlization(_ParsingParams);
        m_SoundMainController = m_MainController as SoundGameController;
        m_CamProductionCon = m_SoundMainController.I_GetSubModule<CameraProductionSubController>();
        m_isUpdateSliderValue = false;
        pp_GaugeValue = 0f; AddWPSlider(0);
        var GetTrs = Helper.ChildLinearStuctureSearch(this.transform);
        GetTrs.HForEach(x => x.gameObject.SetActive(false));
        return null;
    }

    public override void BreakPoint(bool _isBreak, SubModuleBreakType _BrackType = SubModuleBreakType.BreakAll, Type _GetType = null)
    {
        base.BreakPoint(_isBreak, _BrackType, _GetType);
    }

    public void ElemSinngerOutPutInit()
    {
        var GetTrs = Helper.ChildLinearStuctureSearch(this.transform);
        var ApplyScale = GetTrs[0].localScale;
        GetTrs[0].localScale = ApplyScale * 1.5f;
        var GetFSR = GetTrs[0].GetComponent<SpriteRenderer>();
        var GetICONSR = GetTrs[GetTrs.Length - 1].GetComponent<SpriteRenderer>();
        
        Helper.SetChangeColorAlpha(GetFSR.color, 0f);
        Helper.SetChangeColorAlpha(GetICONSR.color, 0f);

        GetTrs[0].gameObject.SetActive(true);
        GetFSR.DOColor(Helper.SetChangeColorAlpha(GetFSR.color, 1f) , 0.85f).SetEase(Ease.InCirc);
        GetTrs[0].DOScale(ApplyScale, 0.85f).SetEase(Ease.InCirc).SetEase(Ease.OutBounce).OnComplete(() => 
        {
            GetTrs.HForEach(x => x.gameObject.SetActive(true)); 
            MaskTweening();
            GetICONSR.DOColor(Helper.SetChangeColorAlpha(GetICONSR.color, 0.5f), 1.25f).//등장 쉐이더 추가할것
            SetEase(Ease.InOutExpo).OnComplete(() =>
            m_SoundMainController.I_CheckSubModuleIsAllCanAction(this));
        });
    }

    //SoundGame에서 데이터를 가지고 있는 함수에서 호출할 수 있도록
    public void ApplySoundStack(bool _isAdd)
    {
        if (!m_isActionProcess) return;
        if(_isAdd) MaskTweening();
        AddWPSlider(_isAdd? 0.1f : -0.1f);
        m_isUpdateSliderValue =
        m_SoundMainController.PlayingNow();
    }

    private void MaskTweening()
    {
        m_SeqMaskComplate.ResetSequce(true); m_SeqMask.ResetSequce(true); 
        m_SeqMask = DOTween.Sequence();
        m_SubDcorMask.transform.localScale = Vector3.one * 2.2f;
        m_SeqMask.Append(m_SubDcorMask.DOScale(Vector3.one, 0.65f).SetEase(Ease.OutSine));
        m_SeqMask.OnComplete(() => 
        {
            m_SeqMaskComplate = DOTween.Sequence();
            m_SeqMaskComplate.Append(m_SubDcorMask.DOScale(Vector3.one * 2.2f, 0.65f).SetDelay(0.2f).SetEase(Ease.InSine));
        });
    }

    private void AddWPSlider(float _ApplyValue)
    {
        pp_GaugeValue += _ApplyValue;
        if (pp_GaugeValue >= 1f)
        {
            //게임 종료 => 웹뷰 띄우기
            pp_GaugeValue = 0f; //테스트 용도
        }
        m_CamProductionCon.CamFullScreenActionMat(pp_GaugeValue, new CamFSMatRatioBuffer(0f, 1f));
        var GetLS = m_SliderTRVer.transform.localScale;
        GetLS.y = pp_GaugeValue;
        m_SliderTRVer.transform.localScale = GetLS;
    }

    public override void ProcessUpdate()
    {
        if (!m_isActionProcess || !m_isUpdateSliderValue ||
        !m_SoundMainController.pp_isMusicOn) return;

        AddWPSlider(Time.deltaTime * 0.025f);
    }
}
