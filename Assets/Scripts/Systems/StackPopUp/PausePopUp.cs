using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Sirenix.OdinInspector;
using DG.Tweening;
using Commons.Helpers;
using TMPro;

public class PausePopUp : SystemBase, I_PopUpInfo, I_PopUpPush, I_PopUpPop
{
    public enum PausePopUpBtnType { None = 0, ExitBtnType, ContinueBtnType }
    [Header("Required Values")]
    [SerializeField] private bool m_isDoTweening = true;
    [SerializeField, ReadOnly] private float m_TweeningSped = 0f;
    [SerializeField, ReadOnly] private MFLGameReowrd m_OutPutMFLGameReowrd;
    [Header("GUI Reference")]
    [SerializeField] private Button m_ExitBtns;
    [SerializeField] private Button[] m_ContinueBtn;
    [SerializeField] private TextMeshProUGUI m_PanelCoinTMPTxt, m_PanelDefultItemTMPTxt;
    [Tooltip("Required Values")]
    private Dictionary<PausePopUpBtnType, System.Action> m_DicClickAddEvent = new();


    #region Required Functions

    public SystemBase I_GetPopUpBase() => this;
    public PopUpType I_GetPopUpType() => PopUpType.DefultPopUp;

    #endregion

    public override void Initlization() 
    {
        this.transform.localScale = Vector3.zero;
        m_ExitBtns.interactable = false;
        m_ContinueBtn.HForEach(x => x.interactable = false);
        //각 버튼또한 추가적인 Tweening을할 수 있도록 한다
    }

    public bool InitPausePopUp(float _TweeningSped, MFLGameReowrd _GetRewordData, 
    params System.Tuple<PausePopUpBtnType, System.Action>[] _ClicksCallBacks)
    {
        Initlization();
        m_TweeningSped = _TweeningSped;
        m_OutPutMFLGameReowrd = _GetRewordData;
        m_PanelCoinTMPTxt.text = m_OutPutMFLGameReowrd.s_RewordCoinCount.ToString() + "B";
        m_PanelDefultItemTMPTxt.text = m_OutPutMFLGameReowrd.s_RewordGamePlayItemCount.ToString();
        _ClicksCallBacks.HForEach(x => m_DicClickAddEvent.Add(x.Item1, x.Item2));
        return m_isDoTweening;
    }

    public void PushOutEvent(System.Action _AddCallBack = null)
    {
        Debug.Log(this.transform.localScale);
        this.transform.DOScale(Vector3.one * 0.5f, m_TweeningSped).SetEase(Ease.InOutBack).OnComplete(() =>
        {
            Debug.Log(this.transform.localScale);
            m_ExitBtns.onClick.AddListener(() =>
            {
                CheckAndCallBackByType(PausePopUpBtnType.ExitBtnType)?.Invoke();
                m_MainControllSystem.MainPopUpPop<PausePopUp>();
                Appinstance.Instance.ms_GamePlayManager.ExecuteChangeAllGameControll(GamePlayManager.GamePlayType.End);
            });
            m_ContinueBtn.HForEach(x => 
            {
                x.onClick.AddListener(() =>
                {
                    CheckAndCallBackByType(PausePopUpBtnType.ContinueBtnType)?.Invoke(); //각각 따로 있을 수 있다
                    m_MainControllSystem.MainPopUpPop<PausePopUp>();
                    Appinstance.Instance.ms_GamePlayManager.ExecuteChangeAllGameControll(GamePlayManager.GamePlayType.Play);
                });
                x.interactable = true; 
            });
            m_ExitBtns.interactable = true;
        });
    }

    public void PopOutEvent(System.Action _EndCallBack = null)
    {
        _EndCallBack?.Invoke();
    }

    #region Sub System Private Functions 

    private System.Action CheckAndCallBackByType(PausePopUpBtnType _GetType)
    {
        if (!m_DicClickAddEvent.ContainsKey(_GetType)) return null;
        return m_DicClickAddEvent[_GetType];
    }

    #endregion

}

[System.Serializable]
public struct MFLGameReowrd : I_Data
{
    public int s_RewordCoinCount;
    public int s_RewordGamePlayItemCount;
}

