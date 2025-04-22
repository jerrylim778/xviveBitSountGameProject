using UnityEngine;
using Sirenix.OdinInspector;
using DG.Tweening;
using Commons.Helpers;

public class CameraProductionSubController : SubModuleControllerBase
{
    [SerializeField, ReadOnly] private SpriteRenderer m_ICEPanelSRBG;
    [SerializeField, ReadOnly] private Material m_EffectMat;

    private Sequence m_MoveCamSeq, m_CamMatSeq;

    public Material pp_EffectMat_ICEPanel => 
    this.transform.ChildLinearStuctureSearch<SpriteRenderer>()[1].material;

    public override object[] Initlization(params object[] _ParsingParams)
    {
        var PartParms = base.Initlization(_ParsingParams);
        m_ICEPanelSRBG = this.transform.ChildLinearStuctureSearch<SpriteRenderer>()[1];
        m_EffectMat = m_ICEPanelSRBG.material;

        m_ICEPanelSRBG.gameObject.SetActive(true);
        m_EffectMat.DOFloat(5f, "_ICEAgePower", 0.45f).SetEase(Ease.InOutExpo).OnComplete(() =>
        {
            m_ICEPanelSRBG.gameObject.SetActive(false);
            FindParentsObjTypeByTemp<SoundGameController>().I_CheckSubModuleIsAllCanAction(this);
            //this.transform.DOMoveY(1f, 0.65f).SetEase(Ease.OutBack).OnComplete(() =>
            //FindParentsObjTypeByTemp<SoundGameController>().I_CheckSubModuleIsAllCanAction(this));
        });
        return null;
    }

    public void MoveStageTweeningCam(float _MovePosY, float _SetDelay, System.Action _EndCallBack = null)
    {
        m_MoveCamSeq.ResetSequce();
        m_MoveCamSeq = DOTween.Sequence();

        m_MoveCamSeq.Append(
        this.transform.DOMoveY(_MovePosY, 1.2f).SetDelay(_SetDelay).SetEase(Ease.OutSine).SetEase(Ease.OutBack).
        OnComplete(() => _EndCallBack?.Invoke()));
    }

    public void CamFullScreenTweeningMat(bool _isOn)
    {
        if (!this.transform.GetChild(1).gameObject.activeSelf)
        this.transform.GetChild(1).gameObject.SetActive(true);
        m_CamMatSeq.ResetSequce(true);
        m_CamMatSeq = DOTween.Sequence();

        m_EffectMat.SetFloat("_ICEAgePower", _isOn? 5f : 0.83f);
        m_CamMatSeq.Append(
        m_EffectMat.DOFloat(0.83f, "_ICEAgePower", 0.45f).SetEase(Ease.OutSine));
    }

    public void CamFullScreenActionMat(float _Value, CamFSMatRatioBuffer? _GetBuffer = null)
    {
        if (!this.transform.GetChild(1).gameObject.activeSelf)
        this.transform.GetChild(1).gameObject.SetActive(true);
        const float targetMin = 0.75f, targetMax = 3f;
        #region Ratio Calculate
        if (_GetBuffer != null)
        {
            var currRatio = _GetBuffer.Value;
            float clampedValue = Mathf.Clamp(
            _Value, currRatio.s_MinValue, currRatio.s_MaxValue);

            // 원본 범위에서의 비율 계산 (0~1)
            float ratio = (clampedValue - currRatio.s_MinValue) / 
            (currRatio.s_MaxValue - currRatio.s_MinValue); 

            // 같은 비율을 타겟 범위에 적용
            _Value = targetMin + (ratio * (targetMax - targetMin));
        }
        #endregion

        _Value = Mathf.Abs(_Value - targetMax);
        Debug.Log(_Value);
        if (m_CamMatSeq != null) m_CamMatSeq.ResetSequce();

        float ClampValue = Mathf.Clamp(_Value, targetMin, targetMax);
        m_EffectMat.SetFloat("_ICEAgePower", ClampValue);
    }

    //public void MoveTweeningCam()
    //{

    //}

    //private void ShakeCam() 
    //{

    //}
}

public struct CamFSMatRatioBuffer : I_Buffer
{
    public float s_MinValue;
    public float s_MaxValue;

    public CamFSMatRatioBuffer(float _MinValue, float _MaxValue)
    {
        s_MinValue = _MinValue;
        s_MaxValue = _MaxValue;
    }

}
