using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using Commons.Helpers;
using DG.Tweening;

public class OutPutAudioEqulizeModule : ModuleMonoBase
{
    [System.Serializable]
    public enum OPAEType
    {
        OneObjectScaleType,
        StickArrayType,
        OneObjectSetRuleScaleType
    }

    //이후 따로 클래스를 통해 연출효과를 줄 수 있도록 수정한다
    [SerializeField] private bool m_isShowReadOnly; //상속 진행할것
    [SerializeField, HideIf("m_isTestMode")] protected bool m_isUsedLocalProduction; 
    [SerializeField, ReadOnly] private ModuleMonoBase m_MainModuleMonoBase;
    [SerializeField] private OPAEType m_OutPutAudioEqulType;
    
    [SerializeField] public float m_LerpSped, m_MaxCliping, m_MaxheightMultiplier;
    [SerializeField, ShowIf(
    "@m_OutPutAudioEqulType != OPAEType.StickArrayType")] private float m_MinCliping = 1;
    [SerializeField, ShowIf(
    "@m_OutPutAudioEqulType != OPAEType.StickArrayType")] private Transform m_ScaleTR;
    [SerializeField, ShowIf(
    "@m_OutPutAudioEqulType == OPAEType.StickArrayType")] private Transform[] m_StickArrayTRs;

    #region 타입별 이퀄라이저 아웃풋 Reference
    [Space(10f), Header("OneObjectSetRuleScaleType")]
    [SerializeField, ReadOnly, ShowIf(
    "@m_isShowReadOnly && m_OutPutAudioEqulType == OPAEType.OneObjectSetRuleScaleType")] private int m_CurrCountingSkipBeat;
    [SerializeField, ShowIf(
    "@m_OutPutAudioEqulType == OPAEType.OneObjectSetRuleScaleType")] private int m_SkipBeatCount;
    [SerializeField, ReadOnly, ShowIf(
    "@m_isShowReadOnly && m_OutPutAudioEqulType == OPAEType.OneObjectSetRuleScaleType")] private float m_CurrReciveBeatPower;
    [SerializeField, ShowIf(
    "@m_OutPutAudioEqulType == OPAEType.OneObjectSetRuleScaleType")] private float m_ForceRuleBounsMaxCliping;
    [SerializeField, ReadOnly, ShowIf(
    "@m_isShowReadOnly && m_OutPutAudioEqulType == OPAEType.OneObjectSetRuleScaleType")] private float m_RememBerBoundApply = 0f;
    [SerializeField, ReadOnly, ShowIf(
    "@m_isShowReadOnly && m_OutPutAudioEqulType == OPAEType.OneObjectSetRuleScaleType")] private float m_BounceResetTimer = 0f;
    [SerializeField, ShowIf(
    "@m_OutPutAudioEqulType == OPAEType.OneObjectSetRuleScaleType")] private float m_BounceResetTime = 0.5f; // 리셋 전 대기 시간(필요에 따라 조정)
    #endregion

    [Tooltip("Required Values")]
    private bool m_HasBouncedOnce = false;  // 바운스가 이미 발생했는지 추적하는 변수

    public int pp_IDXArray => m_OutPutAudioEqulType switch
    { OPAEType.StickArrayType => m_StickArrayTRs.Length, _ => 8 };

    private float[] targetHeights, currentHeights; 

    public override void Initlization(ModuleMonoBase _MainModuleBase, params object[] _OtherParams)
    {
        m_MainModuleMonoBase = _MainModuleBase;
        var GetMixSubController = _OtherParams[0] as AudioMixVisualizeSubController;
        System.Action EndCallBack = null;
        CheckAndApplyByParams<System.Action>(_OtherParams, x => EndCallBack = x);
        
        GetMixSubController.InitOutPutAudioSpectrum(true, this);
        if (m_isTestMode) SemiBreakPoint(true);
        else if(!m_isTestMode && m_isUsedLocalProduction) ActionBeforeOutPutProduction(true, EndCallBack);
    }

    public override void SemiBreakPoint(bool _isBreakPoint)
    {
        m_isProcessAction = _isBreakPoint;
        if (!_isBreakPoint)
        {
            ResetCycle();
            UpdateApplySmoothAnimation(true);
        }
    }

    public void ResetCycle()
    {
        targetHeights = new float[pp_IDXArray];
        currentHeights = new float[pp_IDXArray];
    }

    public void ApplyTargetHeights(int _ApplyIDX, float _ApplyValue)
    {
        targetHeights[_ApplyIDX] = _ApplyValue * m_MaxheightMultiplier;
        UpdateApplySmoothAnimation();
    }

    private Vector3 ReturnClamp(float _Curr, float _Min, float _Max)
    => new Vector3(Mathf.Clamp(_Curr, _Min, _Max),
    Mathf.Clamp(_Curr, _Min, _Max), Mathf.Clamp(_Curr, _Min, _Max));

    #region Main System Private Functions (**Update Reference**)

    private void UpdateApplySmoothAnimation(bool _isDirctApplyValue = false)
    {
        if (!m_isProcessAction) return;

        bool isBarStick = m_OutPutAudioEqulType == OPAEType.StickArrayType;
        bool isRandomScale = m_OutPutAudioEqulType == OPAEType.OneObjectSetRuleScaleType;

        if (isRandomScale && m_HasBouncedOnce)
        {
            if (m_BounceResetTimer < m_BounceResetTime)
                m_BounceResetTimer += Time.deltaTime * 0.5f;
            else m_HasBouncedOnce = false;

            m_RememBerBoundApply = Mathf.Lerp(m_RememBerBoundApply, m_CurrReciveBeatPower, Time.deltaTime * m_LerpSped);
            m_ScaleTR.localScale = ReturnClamp(m_RememBerBoundApply, m_MinCliping, m_MaxCliping);
            return;
        }

        float RememBerPower = 0f;
        for (int i = 0; i < pp_IDXArray; i++)
        {
            if (!isBarStick && RememBerPower < targetHeights[i])
                RememBerPower = targetHeights[i];

            if (isBarStick)
            {
                currentHeights[i] = _isDirctApplyValue ? targetHeights[i] :
                Mathf.Lerp(currentHeights[i], targetHeights[i], Time.deltaTime * m_LerpSped);

                if (m_StickArrayTRs[i] == null) continue;

                Vector3 size = m_StickArrayTRs[i].localScale;
                size.y = Mathf.Clamp(currentHeights[i], 0f, m_MaxCliping);
                m_StickArrayTRs[i].localScale = size;
            }
        }

        if (isBarStick) return;

        if (m_OutPutAudioEqulType == OPAEType.OneObjectScaleType)
        {
            var ApplyPower = 0f;
            ApplyPower = Mathf.Lerp(ApplyPower, RememBerPower, Time.deltaTime * m_LerpSped);
            Vector3 lsize = m_ScaleTR.localScale;
            lsize = ReturnClamp(ApplyPower, m_MinCliping, m_MaxCliping);
            m_ScaleTR.localScale = lsize;
        }

        if (!isRandomScale) return;

        if (!m_HasBouncedOnce && RememBerPower >= m_ForceRuleBounsMaxCliping)
        {
            if (m_CurrCountingSkipBeat >= m_SkipBeatCount)
            {
                m_CurrReciveBeatPower = RememBerPower;
                m_CurrCountingSkipBeat = 0;
                m_BounceResetTimer = 0f; m_RememBerBoundApply = 0f;
                m_HasBouncedOnce = true;
                return;
            }

            m_CurrCountingSkipBeat++;
        }

        float getScaleAny = m_ScaleTR.localScale.x;
        getScaleAny -= Time.deltaTime * m_LerpSped * 3f;
        getScaleAny = Mathf.Max(0f, getScaleAny);
        m_ScaleTR.localScale = new Vector3(getScaleAny, getScaleAny, getScaleAny);
    }

    #endregion

    #region Sub System Private Functions (**OutPut Elem Production**)

    public void ActionBeforeOutPutProduction(bool _isOn, System.Action _EndCallBack = null)
    {
        switch (m_OutPutAudioEqulType)
        {
            case OPAEType.StickArrayType:
                //0.03 0
                //0 1.2
                var GetParticles =
                this.transform.ChildLinearStuctureSearch<ParticleSystem>();
                if (GetParticles.Length > 0)
                {
                    var ApplyParticle = GetParticles[0];
                    var GetOGScale = ApplyParticle.transform.localScale;
                    ApplyParticle.transform.DOScaleY(_isOn? 0.8f : 1.2f, 0.25f).SetEase(Ease.OutSine).OnComplete(() =>
                    ApplyParticle.transform.DOScaleX(_isOn? 0.1f : 0.8f, 0.25f).SetEase(Ease.Linear).OnComplete(() =>
                    ApplyParticle.transform.DOScaleX(_isOn? 2f : 0f, 0.65f).SetEase(Ease.OutSine).OnComplete(() => 
                    {
                        if (!_isOn)
                        {
                            ApplyParticle.transform.localScale = new Vector3(0.03f, 0f, 1f);
                            _EndCallBack?.Invoke();
                        }
                    })));
                }
                if (!_isOn) return;

                var Get2DLists = m_StickArrayTRs.SplitListMiddleCount();
                int CountIDX = 0; Get2DLists.ForEach(y =>  //HCountForEach(0, Get2DLists.Count - 1, _CountIDX => 
                {
                    float Delay = 0f; y.HForEach(x =>
                    {
                        x.localScale = new Vector3(x.localScale.x, 0f, x.localScale.z);
                        x.DOScaleY(m_MaxCliping, 0.45f).SetDelay(Delay).SetEase(Ease.OutBack).OnComplete(() =>
                        x.DOScaleY(0f, 0.25f).SetEase(Ease.OutExpo).OnComplete(() =>
                        { CountIDX++; if (CountIDX >= y.Count) { _EndCallBack?.Invoke(); _EndCallBack = null; } }));
                        Delay += 0.05f;
                    });
                });
                #region 좌우 나눠 각 구간 연출 진행 (예전 버전)
                //bool _isCol = m_StickArrayTRs.Length % 2 == 0; //짝 홀
                //int GetIDX = _isCol ?
                //m_StickArrayTRs.Length / 2 : Mathf.RoundToInt(m_StickArrayTRs.Length / 2);
                //var LHalfList = new List<Transform>(); var RHalfList = new List<Transform>();
                //Helper.HCountForEach(0, GetIDX - 1, _CountIDX => LHalfList.Add(m_StickArrayTRs[_CountIDX]));
                //Helper.HCountForEach(GetIDX, _isCol? GetIDX * 2 - 1 : GetIDX * 2, _CountIDX => RHalfList.Add(m_StickArrayTRs[_CountIDX]));
                //LHalfList.Reverse();
                //float Delay = 0f; LHalfList.HForEach(x =>
                //{
                //    x.localScale = new Vector3(x.localScale.x, 0f, x.localScale.z);
                //    x.DOScaleY(m_MaxCliping, 0.45f).SetDelay(Delay).SetEase(Ease.OutBack).OnComplete(() =>
                //    x.DOScaleY(0f, 0.25f).SetEase(Ease.OutExpo));
                //    Delay += 0.05f;
                //});
                //int CoundIDX = 0; Delay = 0f; RHalfList.HForEach(x =>
                //{
                //    x.localScale = new Vector3(x.localScale.x, 0f, x.localScale.z);
                //    x.DOScaleY(m_MaxCliping, 0.45f).SetDelay(Delay).SetEase(Ease.OutBack).OnComplete(() =>
                //    x.DOScaleY(0f, 0.25f).SetEase(Ease.OutExpo).OnComplete(() => 
                //    { CoundIDX++; if (CoundIDX >= RHalfList.Count) _EndCallBack?.Invoke(); }));
                //    Delay += 0.05f;
                //});
                #endregion
                break;
            case OPAEType.OneObjectScaleType:
                _EndCallBack?.Invoke();
                break;
        }
    }

    #endregion

    private void Start()
    {
        if (!m_isTestMode) return;
        ModuleMonoBase ApplyMonoBase = null;
        Initlization(ApplyMonoBase, FindAnyObjectByType<AudioMixVisualizeSubController>());
    }
}
