using UnityEngine;
using Sirenix.OdinInspector;
using DG.Tweening;
using Commons.Helpers;

public class CameraProductionSubController : SubModuleControllerBase
{
    [SerializeField, ReadOnly] private SpriteRenderer m_ICEPanelSRBG;
    [SerializeField, ReadOnly] private Material m_EffectMat;

    private Sequence m_MoveCamSeq;

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

    public void MoveTweeningCam()
    {

    }

    private void ShakeCam() 
    {

    }
}