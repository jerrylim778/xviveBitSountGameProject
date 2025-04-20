using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using Commons.Helpers;


public class GUIMB_SoundGameModule : SystemBase
{
    [Header("===Control Values===")]
    [SerializeField] private bool m_isTestMode = false; 
    [SerializeField] private bool m_isLoadingMainProcessParsing = true;
    [Header("===GUI_MFLTestModeReference===")]
    [SerializeField] private Image m_SubBG;
    [SerializeField] private Button m_GameStartBtn;
    public override void Initlization()
    {
        SubTweening(false);
        Camera.main.transform.position = new Vector3(0f, 10f, Camera.main.transform.position.z);
        m_SubBG.gameObject.SetActive(false);
        m_SubBG.gameObject.CheckComnectComponent<CanvasGroup>().alpha = 0f;
        System.Action ComplateAction = () =>
        {
            m_SubBG.gameObject.SetActive(true);
            CanvasGroup GetCG = m_SubBG.gameObject.GetComponent<CanvasGroup>();
            DOTween.To(() => GetCG.alpha, x => GetCG.alpha = x, 1f, 0.35f).SetEase(Ease.OutBounce).OnComplete(() => SubTweening(true));
        };

        if (!m_isLoadingMainProcessParsing)
        {
            ComplateAction();
            return;
        }

        DataManager GetManager = Appinstance.Instance.ms_DataManager;
        m_MainControllSystem.ExecutePopUpStack<LoadingPopUp>().InitLoadingPopUp(GetManager.AllParsingInfosActions(), ComplateAction);
        m_MainControllSystem.MainPopUpPush<LoadingPopUp>();
    }

    private void SubTweening(bool _isAction)
    {
        m_GameStartBtn.interactable = _isAction;
        if (!_isAction)
        {
            m_GameStartBtn.onClick.RemoveAllListeners();
            return;
        }
        m_GameStartBtn.onClick.AddListener(() => ExecuteNextStep(SystemInfoType.Test_GUIMB_SoundGameProjectSelectMusic));
    }

    public override void ExecuteNextStep(SystemInfoType _NextType)
    {
        SubTweening(false);
        base.ExecuteNextStep(_NextType);
    }


}
