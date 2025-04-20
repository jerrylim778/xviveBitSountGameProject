using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using Commons.Helpers;
using Sirenix.OdinInspector;

public class GUIMB_TestModeDirectInGame : SystemBase
{
    [Header("===Control Values===")]
    [SerializeField] private bool m_isTestMode = false; //다른 클래스에 대부분 사용한다면 부모클래스로 격상 요망..
    [SerializeField] private bool m_isUseStartWhiteFlash = false;
    [SerializeField] private bool m_isLoadingMainProcessParsing = true;
    [Header("===GUI_MFLTestModeReference===")]
    [SerializeField] private Image m_SubBG;
    [SerializeField] private Button m_GameStartBtn;
    public override void Initlization()
    {
        SubTweening(false);
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
        m_GameStartBtn.onClick.AddListener(() => ExecuteNextStep(SystemInfoType.NoneMainGUI));
    }

    public override void ExecuteNextStep(SystemInfoType _NextType)
    {
        if (_NextType == SystemInfoType.NoneMainGUI)
        {
            SubTweening(false);
            var GetSManager = Appinstance.Instance.ms_ScriptableObjectManager;
            var GetDataManager = Appinstance.Instance.ms_DataManager;
            var GetItemInfos = new List<ItemInfo>();
            #region 원래는 각 MyInfo에서 저장된 인덱스에 따른 Controller를 배출해 GamePlayManager<클래스>로 보내야한다
            //GetItemInfos.Add(GetSManager.SDataParsingByItemIDX<SDataCharacterInfo, CharacterItemInfo>(199));
            //if(GetDataManager.pp_SceneType != SetSceneType.TyconGameSceneSimpleVersion)
            //{
            //    int ApplyDLIDX = GetDataManager.pp_SceneType == SetSceneType.TyconGameSceneLiteVersion ? 501 : 500;
            //    GetItemInfos.Add(GetSManager.SDataParsingByItemIDX<SDataTYCInfo, TYCDefultLevelInfo>(ApplyDLIDX));
            //}
            //if (m_isTestMode) FindAnyObjectByType<NewCharacterController>().Initlization(null);
            //else 
            #endregion
            #region Runner Game Reference
            //GetItemInfos.Add(GetSManager.SDataParsingByItemIDX<SDataRunnerInfo, RunnerBaseInfo>(200));
            #endregion

            System.Action EndCallBack = () => 
            {
                Appinstance.Instance.ms_GamePlayManager.ExecuteChangeAllGameControll(
                GamePlayManager.GamePlayType.StartingSet, true, new MyItemInfoBuffer(GetItemInfos.ToArray()));
                base.ExecuteNextStep(_NextType);
            };
            if (!m_isUseStartWhiteFlash) EndCallBack();
            else
            {
                var GetCG = GamePlaySystem.Instance.testpp_AllIncludedGUIPanel.gameObject.CheckComnectComponent<CanvasGroup>();
                GetCG.alpha = 0f; GetCG.transform.GetChild(0).gameObject.SetActive(true);
                DOTween.To(() => GetCG.alpha, x => GetCG.alpha = x, 1f, 0.4f).SetEase(Ease.InSine).OnComplete(() => EndCallBack());
            }
            return;
        }
        SubTweening(false);
        base.ExecuteNextStep(_NextType);
    }


}
