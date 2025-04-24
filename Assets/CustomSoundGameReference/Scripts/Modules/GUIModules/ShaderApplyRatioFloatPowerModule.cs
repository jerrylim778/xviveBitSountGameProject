using UnityEngine;
using Sirenix.OdinInspector;
using Commons.Helpers;
using DG.Tweening;

public class ShaderApplyRatioFloatPowerModule : MonoBehaviour
{
    [SerializeField, ReadOnly] private bool m_isActionProcess;
    [SerializeField] private bool m_isApplyRevert;
    [SerializeField] private string m_OrderStrProperty;
    [SerializeField, ReadOnly] private float targetMin = 0f, targetMax = 1f;
    [SerializeField, ReadOnly] private Material m_EffectMat;
    
    private Sequence m_EffectSeq;

    public void Initlization(float _ValueMin, float _ValueMax, string _PropertyOrder = "")
    {
        m_OrderStrProperty = string.IsNullOrEmpty(_PropertyOrder)? m_OrderStrProperty : _PropertyOrder;
        var ApplyRenderer = this.GetComponent<Renderer>();
        m_EffectMat = Instantiate(ApplyRenderer.material);
        ApplyRenderer.material = m_EffectMat;
        targetMin = _ValueMin; targetMax = _ValueMax;
        m_EffectMat.SetFloat(m_OrderStrProperty, _ValueMin);
        m_isActionProcess = true;
    }

    public void TweeningPowerForMat(bool _isOn)
    {
        if (!m_isActionProcess) return;

        if (!this.transform.GetChild(1).gameObject.activeSelf)
            this.transform.GetChild(1).gameObject.SetActive(true);
        m_EffectSeq.ResetSequce(true);
        m_EffectSeq = DOTween.Sequence();

        m_EffectMat.SetFloat(m_OrderStrProperty, _isOn ? 5f : 0.83f);
        m_EffectSeq.Append(
        m_EffectMat.DOFloat(0.83f, m_OrderStrProperty, 0.45f).SetEase(Ease.OutSine));
    }


    public void ActionForMat(float _Value, CamFSMatRatioBuffer? _GetBuffer = null)
    {
        if (!m_isActionProcess) return;
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

        if(m_isApplyRevert) _Value = Mathf.Abs(_Value - targetMax);
        if (m_EffectSeq != null) m_EffectSeq.ResetSequce();

        float ClampValue = Mathf.Clamp(_Value, targetMin, targetMax);
        m_EffectMat.SetFloat(m_OrderStrProperty, ClampValue);
    }
}
