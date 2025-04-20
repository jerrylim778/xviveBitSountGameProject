using UnityEngine;
using Sirenix.OdinInspector;

public class OutPutAudioEqulizeModule : ModuleMonoBase
{
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

    private float[] targetHeights, currentHeights; // 부드러운 애니메이션을 위한 변수

    public override void Initlization(ModuleMonoBase _MainModuleBase, params object[] _OtherParams)
    {
        m_MainModuleMonoBase = _MainModuleBase;
        var GetMixSubController = _OtherParams[0] as AudioMixVisualizeSubController;
        GetMixSubController.InitOutPutAudioSpectrum(true, this);
        if(m_isTestMode) SemiBreakPoint(true);
    }

    public override void SemiBreakPoint(bool _isBreakPoint)
    => m_isProcessAction = _isBreakPoint;

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

    private void UpdateApplySmoothAnimation()
    {
        if (!m_isProcessAction) return;

        bool isBarStick = m_OutPutAudioEqulType == OPAEType.StickArrayType;
        
        for (int i = 0; i < pp_IDXArray; i++)
        {
            // 부드러운 애니메이션 적용
            currentHeights[i] = Mathf.Lerp(currentHeights[i], targetHeights[i], Time.deltaTime * m_LerpSped);


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

                // UI 막대 크기 업데이트
                Vector3 size = m_StickArrayTRs[i].localScale;
                size.y = Mathf.Clamp(currentHeights[i], 1f, m_MaxCliping);
                m_StickArrayTRs[i].localScale = size;
            }
        }
    }

    private Vector3 ReturnClamp(float _Curr, float _Min, float _Max) => new Vector3(
    Mathf.Clamp(_Curr, _Min, _Max), Mathf.Clamp(_Curr, _Min, _Max), Mathf.Clamp(_Curr, _Min, _Max));

    [System.Serializable]
    public enum OPAEType
    {
        OneObjectScaleType,
        StickArrayType,
    }

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
