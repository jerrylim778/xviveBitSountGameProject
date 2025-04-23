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
    }

    //이후 따로 클래스를 통해 연출효과를 줄 수 있도록 수정한다
    [SerializeField, HideIf("m_isTestMode")] protected bool m_isUsedLocalProduction; 
    [SerializeField, ReadOnly] private ModuleMonoBase m_MainModuleMonoBase;
    [SerializeField] private OPAEType m_OutPutAudioEqulType;
    
    [SerializeField] public float m_LerpSped, m_MaxCliping, m_MaxheightMultiplier;
    [SerializeField, ShowIf(
    "@m_OutPutAudioEqulType == OPAEType.OneObjectScaleType")] private float m_MinCliping = 1;
    [SerializeField, ShowIf(
    "@m_OutPutAudioEqulType == OPAEType.OneObjectScaleType")] private Transform m_ScaleTR;
    [SerializeField, ShowIf(
    "@m_OutPutAudioEqulType == OPAEType.StickArrayType")] private Transform[] m_StickArrayTRs;
    public int pp_IDXArray => m_OutPutAudioEqulType switch
    { OPAEType.StickArrayType => m_StickArrayTRs.Length, _ => 18 };

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

    private void UpdateApplySmoothAnimation(bool _isDirctApplyValue = false)
    {
        if (!m_isProcessAction) return;

        bool isBarStick = m_OutPutAudioEqulType == OPAEType.StickArrayType;
        
        for (int i = 0; i < pp_IDXArray; i++)
        {
            currentHeights[i] = _isDirctApplyValue? targetHeights[i] :
            Mathf.Lerp(currentHeights[i], targetHeights[i], Time.deltaTime * m_LerpSped);


            if (!isBarStick) 
            {
                Vector3 lsize = m_ScaleTR.localScale;
                float Aspect = currentHeights[i] / m_MaxCliping;
                lsize = ReturnClamp(Aspect, m_MinCliping, m_MaxCliping);
                m_ScaleTR.localScale = lsize;
            }
            else
            {
                if (m_StickArrayTRs[i] == null) continue;

                Vector3 size = m_StickArrayTRs[i].localScale;
                size.y = Mathf.Clamp(currentHeights[i], 0f, m_MaxCliping);
                m_StickArrayTRs[i].localScale = size;
            }
        }
    }

    private Vector3 ReturnClamp(float _Curr, float _Min, float _Max) => new Vector3(
    Mathf.Clamp(_Curr, _Min, _Max), Mathf.Clamp(_Curr, _Min, _Max), Mathf.Clamp(_Curr, _Min, _Max));

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
                #region 좌우 나눠 각 구간 연출 진행
                bool _isCol = m_StickArrayTRs.Length % 2 == 0; //짝 홀
                int GetIDX = _isCol ?
                m_StickArrayTRs.Length / 2 : Mathf.RoundToInt(m_StickArrayTRs.Length / 2);
                var LHalfList = new List<Transform>(); var RHalfList = new List<Transform>();
                Helper.HCountForEach(0, GetIDX - 1, _CountIDX => LHalfList.Add(m_StickArrayTRs[_CountIDX]));
                Helper.HCountForEach(GetIDX, _isCol? GetIDX * 2 - 1 : GetIDX * 2, _CountIDX => RHalfList.Add(m_StickArrayTRs[_CountIDX]));
                LHalfList.Reverse();
                float Delay = 0f; LHalfList.HForEach(x =>
                {
                    x.localScale = new Vector3(x.localScale.x, 0f, x.localScale.z);
                    x.DOScaleY(m_MaxCliping, 0.45f).SetDelay(Delay).SetEase(Ease.OutBack).OnComplete(() =>
                    x.DOScaleY(0f, 0.25f).SetEase(Ease.OutExpo));
                    Delay += 0.05f;
                });
                int CoundIDX = 0; Delay = 0f; RHalfList.HForEach(x =>
                {
                    x.localScale = new Vector3(x.localScale.x, 0f, x.localScale.z);
                    x.DOScaleY(m_MaxCliping, 0.45f).SetDelay(Delay).SetEase(Ease.OutBack).OnComplete(() =>
                    x.DOScaleY(0f, 0.25f).SetEase(Ease.OutExpo).OnComplete(() => 
                    { CoundIDX++; if (CoundIDX >= RHalfList.Count) _EndCallBack?.Invoke(); }));
                    Delay += 0.05f;
                });
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

    //private void Update()
    //{
    //    if (!m_isTestMode) return;

    //    if (Input.GetKeyDown(KeyCode.L))
    //}
}
