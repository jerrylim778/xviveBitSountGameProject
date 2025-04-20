using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Commons.Helpers;
using DG.Tweening;
using TMPro;

public class MainLoadingSystem : MonoBehaviour
{
    [System.Serializable]
    public struct MainLoadingOutPutInfo
    {
        public SetMainLoadingType s_OutPutType;
        public Image s_OutPutBG;
    }

    public enum SetMainLoadingType
    {
        OutPutNothing,
        OutPutStarting,
        OutPutEffect,
        OutPutOnlyImgs, //구분해야한다
        OutPutEndAfter,
    }
    
    #region 메인 로딩 시스템에 관한 설명
    //1.대부분의 로직은 LoadingPopUp과 유사하지만
    //1-1.GUI에 대한 추가 데코레이션 작업
    //(다른 로딩창에 관한 (쉐이더 GUI를 추가할 예정) 보여줄 수 있는 특수 GUI => Viggiet에 대한 알파 보간 후처리 
    //1-2.메인 GUI창에서 LoadingManager<클래스>의 메카니즘과 Txt를 띄울 수 있도록 수정하기
    //1-3.메인로딩이 전부 끝난뒤에 처리해야될 작업 미리 전환할 수 있도록  (Viggiet쉐이더 이전에 발동후 마지막 GUI출력)
    #endregion
    [Header("ALL GUI Reference")]
    [SerializeField] private MainLoadingOutPutInfo[] m_MainLoadingOutPutInfo;
    [SerializeField] private Slider m_MainLoadPrograssSlider;
    //[SerializeField] private TextMeshProUGUI m_PrograssTMPTxt; //알맞은 TMP 폰트 전환이후에 변경할것
    [SerializeField] private Text m_PrograssTMPTxt;


    [Tooltip("Required Values")]
    private SetSceneType m_FinalChangeScene = SetSceneType.None, m_BeforSceneType = SetSceneType.None;
    private SetMainLoadingType m_SetMainLoadingType = SetMainLoadingType.OutPutNothing;
    private MapInfo m_ChangeMapInfoNow;
    private LoadingManager m_MainLoadingManager;
    private System.Action m_StartCallBack, m_EndCallBack;
    [Tooltip("GUI Tweening Reference")]
    private readonly System.UInt16 m_EffectTypeNumber = 0, m_EndEffectTypeNumber = 1;
    private Material m_MainLoadingGUIShaderMat;
    private float m_ControllSliderValue = 0f;
    private bool m_isActionTweening = false;
    private Dictionary<SetMainLoadingType, Image> m_MainLoadingOutPutDic;


    public void MainLoadingInit(SetMainLoadingType _MainLoadingType, 
    SetSceneType _SetSceneType, SetSceneType _BeforeSceneType, MapInfo _MapInfo,
    System.Action _CallBack, System.Action _EndCallBack)
    {
        if (m_MainLoadingManager == null) m_MainLoadingManager = Appinstance.Instance.ms_LoadingManager;
        #region SceneManager Init
        m_BeforSceneType = _BeforeSceneType;
        m_FinalChangeScene = _SetSceneType;
        m_SetMainLoadingType = _MainLoadingType;
        SceneManager.sceneLoaded += NextActionByScene;
        Appinstance.Instance.ms_LoadingManager.DELManager_ShowOther_Txt += ShowLoadTxt;
        Appinstance.Instance.ms_LoadingManager.DELManager_UpdateMainLoading_Prograss += MainLoadingPrograss;
        m_ChangeMapInfoNow = _MapInfo;
        m_StartCallBack = _CallBack;
        m_EndCallBack = _EndCallBack;
        m_isActionTweening = m_ChangeMapInfoNow != null &&
        Appinstance.Instance.ms_MyInfo.GetFindSavedMyInfoMapReferByIDX(m_ChangeMapInfoNow.s_ItemIDX) == null &&
        _SetSceneType != SetSceneType.TitleScene? !m_ChangeMapInfoNow.s_isOnlyInit : false;
        #endregion
        #region GUI OutPut Init
        //1.GUIs Init
        m_MainLoadingOutPutDic = new Dictionary<SetMainLoadingType, Image>();
        m_MainLoadingOutPutInfo.ToList().ForEach(x => m_MainLoadingOutPutDic.Add(x.s_OutPutType, x.s_OutPutBG));
        Image GetImg = m_MainLoadingOutPutDic[SetMainLoadingType.OutPutStarting];
        GetImg.GetComponent<CanvasGroup>().alpha = 0f;
        GetImg = m_MainLoadingOutPutDic[SetMainLoadingType.OutPutEffect];
        if (m_SetMainLoadingType == SetMainLoadingType.OutPutEffect && GetImg != null && GetImg.material != null)
        {
            m_MainLoadingGUIShaderMat = Instantiate(GetImg.material);
            GetImg.material = m_MainLoadingGUIShaderMat;
            if (m_MainLoadingGUIShaderMat.HasProperty("_MainPower")) m_MainLoadingGUIShaderMat.SetFloat("_MainPower", 0f);
        }
        GetImg = m_MainLoadingOutPutDic[SetMainLoadingType.OutPutOnlyImgs];
        if (m_SetMainLoadingType == SetMainLoadingType.OutPutOnlyImgs && GetImg != null)
        {
            Transform[] GetTrs = Helper.ChildLinearStuctureSearch(GetImg.transform); 
            GetImg.sprite = _MapInfo.s_SpriteArray?[0];
            GetTrs[1].GetComponent<Text>().text =
            Appinstance.Instance.ms_TranslateManager.ChangeTranslateLanguage(_MapInfo.s_ItemName);
            //GetTrs[1].GetComponent<TextMeshProUGUI>().text = //TMP UGUI 로 변경해야됨 
            //Appinstance.Instance.ms_TranslateManager.ChangeTranslateLanguage(_MapInfo.s_ItemName);
        }
        m_PrograssTMPTxt.text = string.Empty;

        //2.MainPrograss Init
        m_MainLoadPrograssSlider.gameObject.SetActive(false);
        m_MainLoadPrograssSlider.value = 0f;
        m_ControllSliderValue = 0f;

        #endregion
        m_MainLoadingOutPutDic.ToList().ForEach(x => x.Value.gameObject.SetActive(false)); //전체를 끄고 실행하기
        OutPutAction();
    }

    #region Main System Private Functions

    private void OutPutAction()
    {
        if (m_SetMainLoadingType == SetMainLoadingType.OutPutNothing || 
        m_FinalChangeScene == SetSceneType.TitleScene || m_BeforSceneType == SetSceneType.InGameScene)
        {
            m_MainLoadingOutPutDic[m_SetMainLoadingType].gameObject.SetActive(true);
            if (m_SetMainLoadingType == SetMainLoadingType.OutPutEffect)
            MainLoadingGUIShaderControll(true, m_EffectTypeNumber, () => m_StartCallBack?.Invoke());
            else m_StartCallBack?.Invoke();
            return;
            #region 아무것도 안보일때는 빈화면에 로딩창만 존재하는걸 기본 원칙으로 한다
            //MainLoadingGUIShaderControll(true, m_ActionTypeNumber, () =>
            //{
            //    MainLoadingGUIShaderControll(false, m_ActionTypeNumber);
            //    m_StartCallBack?.Invoke();
            //});
            #endregion
        }
        if(m_FinalChangeScene == SetSceneType.InGameScene)
        {
            Image GetImg = m_MainLoadingOutPutDic[SetMainLoadingType.OutPutStarting];
            GetImg.gameObject.SetActive(true);
            DOTween.To(() => GetImg.GetComponent<CanvasGroup>().alpha, x =>
            GetImg.GetComponent<CanvasGroup>().alpha = x, 1f, 3f).SetEase(Ease.InCirc).OnComplete(() =>
            {
                GetImg.gameObject.SetActive(false);
                m_MainLoadingOutPutDic[m_SetMainLoadingType].gameObject.SetActive(true);
                if (m_SetMainLoadingType == SetMainLoadingType.OutPutEffect)
                MainLoadingGUIShaderControll(true, m_EffectTypeNumber, () => m_StartCallBack?.Invoke());
                else m_StartCallBack?.Invoke();
            });
        }
    }

    private void ExitAction(System.Action _EndCallBack, System.UInt16? _TypeNumber = null)
    {
        m_MainLoadingGUIShaderMat = null;
        m_MainLoadingOutPutDic[m_SetMainLoadingType].gameObject.SetActive(false);
        System.Action AddEndCallBack = () =>
        {
            _EndCallBack?.Invoke();
            m_MainLoadingOutPutDic.ToList().ForEach(x => x.Value.gameObject.SetActive(false));
            Destroy(this.gameObject);
        };

        Image GetImg = m_MainLoadingOutPutDic[SetMainLoadingType.OutPutEndAfter];
        if (GetImg == null && _TypeNumber == null) { AddEndCallBack?.Invoke(); return;}
        #region End Effect Init
        GetImg.transform.SetParent(this.transform);
        GetImg.transform.SetAsLastSibling();
        GetImg.gameObject.SetActive(true);
        Transform[] GetAllTrs = Helper.ChildLinearStuctureSearch(this.transform);
        GetAllTrs.ToList().ForEach(x => 
        { if(!x.Equals(GetAllTrs[GetAllTrs.Length - 1])) x.gameObject.SetActive(false);});
        Transform GetEndTr = GetImg.transform.GetChild((int)_TypeNumber);
        GetEndTr.SetParent(this.transform);
        GetEndTr.SetAsLastSibling();
        GetEndTr.gameObject.SetActive(true);
        #endregion

        switch (_TypeNumber)
        {
            case 0:
                GetImg.gameObject.SetActive(false);
                DOTween.To(() => GetEndTr.GetComponent<CanvasGroup>().alpha,
                x => GetEndTr.GetComponent<CanvasGroup>().alpha = x, 0f, 1.2f).
                SetEase(Ease.InCirc).OnComplete(() => AddEndCallBack?.Invoke());
                break;
            case 1:
                StartCoroutine(CO_EndEffectType1(GetImg, GetEndTr.GetComponent<Image>(), AddEndCallBack));
                break;
        }
    }

    private void MainLoadingPrograss(int _CurrentCount, int _MaxCount)
    {
        if (m_SetMainLoadingType == SetMainLoadingType.OutPutNothing)
        {
            if (_CurrentCount < _MaxCount)
                return;

            m_EndCallBack?.Invoke();
            Appinstance.Instance.ms_LoadingManager.DELManager_ShowOther_Txt -= ShowLoadTxt;
            Appinstance.Instance.ms_LoadingManager.DELManager_UpdateMainLoading_Prograss -= MainLoadingPrograss;
            m_MainLoadingOutPutDic.ToList().ForEach(x => x.Value.gameObject.SetActive(false));
            m_MainLoadingOutPutDic = null;
            Destroy(this.gameObject);
            return;
        }

        UpdateSliderValue(((float)1 / (float)_MaxCount), () =>
        {
            if (_CurrentCount < _MaxCount || m_ControllSliderValue < 1)
                return;

            Appinstance.Instance.ms_LoadingManager.DELManager_ShowOther_Txt -= ShowLoadTxt;
            Appinstance.Instance.ms_LoadingManager.DELManager_UpdateMainLoading_Prograss -= MainLoadingPrograss;
            if (m_isActionTweening) ExitAction(m_EndCallBack, m_EndEffectTypeNumber);
            else
            {
                m_MainLoadingOutPutDic.ToList().ForEach(x => x.Value.gameObject.SetActive(false));
                m_MainLoadingOutPutDic = null;
                Destroy(this.gameObject);
                m_EndCallBack?.Invoke();
            }
        });
    }

    #endregion

    #region Sub System Private Funcstions

    #region GUI Reference
    //Sequence GetNewSquence = DOTween.Sequence(); //만일 Kill함수가 먹히질 않는다면 선언후 제약을 할 수 있도록 한다
    private void UpdateSliderValue(float _AddValue, System.Action _EndCallBack)
    {
        DOTween.Kill(m_MainLoadPrograssSlider.value);
        //DOTween.Kill(GetNewSquence);
        m_ControllSliderValue = m_MainLoadPrograssSlider.value + _AddValue;
        DOTween.To(() => m_MainLoadPrograssSlider.value, x => m_MainLoadPrograssSlider.value = x,
        m_ControllSliderValue, 0.35f).SetEase(Ease.OutExpo).OnComplete(() => _EndCallBack?.Invoke());
        if (m_ControllSliderValue > 0) m_MainLoadPrograssSlider.gameObject.SetActive(true);
    }

    private void ShowLoadTxt(string _GetTxt)  => m_PrograssTMPTxt.text = _GetTxt;

    private void MainLoadingGUIShaderControll(bool _isInit, System.UInt16 _TypeNumber, System.Action _EndCallBack = null)
    {
        if (m_MainLoadingGUIShaderMat == null || !m_MainLoadingGUIShaderMat.HasProperty("_MainPower")) return;
        switch (_TypeNumber)
        {
            case 0:
                if (_isInit)
                {
                    m_MainLoadingGUIShaderMat.DOFloat(0.85f, "_MainPower", 0.85f).
                    SetEase(Ease.OutBack).OnComplete(() => 
                    { _EndCallBack?.Invoke(); MainLoadingGUIShaderControll(false, _TypeNumber); });
                    return;
                }
                m_MainLoadingGUIShaderMat.DOFloat(UnityEngine.Random.Range(0.68f, 0.89f), "_MainPower",
                UnityEngine.Random.Range(0.09f, 0.16f)).SetEase(Ease.OutBack).OnComplete(() =>
                MainLoadingGUIShaderControll(false, _TypeNumber));
                return;
        }
    }
    #endregion

    #region Scene Reference

    //해당 부분 재설계 요망 비동기 로딩이기 때문에 해당 부분에서 모든과정이 돌아가는 구조로 가는게 맞다
    //따라서 사실상 LoadingManager<클래스>에 존재하여 타입별 적용해야되는것이 맞다
    public IEnumerator CO_StepNextSceneAsync(SetSceneType _GetType, bool _isFindName = false)
    {
        AsyncOperation AsyncOp = _isFindName ?
        SceneManager.LoadSceneAsync(FindScenesNameByParams(_isFindName, _GetType).ToString()) : SceneManager.LoadSceneAsync((int)FindScenesNameByParams(_isFindName, _GetType));
        AsyncOp.allowSceneActivation = false;
        float PrograssNow = 0f, MaxValue = 1 / m_MainLoadingManager.pp_MaxLoadIDX;
        while (!AsyncOp.isDone)
        {
            yield return null;
            if (PrograssNow < MaxValue)
            {
                float GetPrograssValue = /*AsyncOp.progress **/ (MaxValue / AsyncOp.progress);
                PrograssNow += GetPrograssValue;
            }
            else
            {
                AsyncOp.allowSceneActivation = true;
                yield break;
            }
        }
    }

    private void NextActionByScene(Scene _GetNextScene, LoadSceneMode _NextSceneMode)
    {
        SceneManager.sceneLoaded -= NextActionByScene;

        Appinstance.Instance.ms_LoadingManager.DELManagerUpdateGCCollect(true, 1);
    }

    //씬이름을 다른 방법(DataManager 시트 or (빌드 직전 + 초기화된 빌드 셋팅 값) 강제적인 로컬 제이슨 적시 후 파싱)으로 가져와야 할 수도 있다
    private object FindScenesNameByParams(bool _isFindName, SetSceneType _GetType)
    {
        if (_isFindName)
        return string.Format("{0}_{1}", Application.productName, _GetType);

        int FIDX = -1; FIDX =
        Helper.GetEnumByStringArray<SetSceneType>(true).ToList().FindIndex(x => x == _GetType.ToString());
        return FIDX;
    }
    #endregion

    #endregion

    #region Effect Corutine Sub System Priavte Functions

    IEnumerator CO_EndEffectType1(Image _GetMainBG, Image _EffectBG, System.Action _EndCallBack)
    {
        _EffectBG.sprite = m_ChangeMapInfoNow.s_SpriteArray[1];
        Text GetTxt = _EffectBG.transform.GetChild(0).GetComponent<Text>(); //해당 부분 TMPText로 변경해야됨
        string OGText = GetTxt.text; GetTxt.text = string.Empty;
        yield return new WaitForSeconds(0.6f);
        _GetMainBG.gameObject.SetActive(false);
        _EffectBG.gameObject.SetActive(true);
        yield return new WaitForSeconds(0.8f);
        bool isComplate = false; GetTxt.gameObject.SetActive(true);
        GetTxt.DOText(string.Format("{0} {1}", OGText, m_ChangeMapInfoNow.s_ItemName),
        0.95f, true, ScrambleMode.All).OnComplete(() => isComplate = true);
        yield return new WaitUntil(() => isComplate);
        yield return new WaitForSeconds(1.2f);
        float[] GetNumbers = Helper.ExtractContentByTempArray<float>(_EffectBG.sprite.name, '%');
        _EffectBG.rectTransform.ChangePivotStaticPos(new Vector2(GetNumbers[0], GetNumbers[1]));
        _EffectBG.rectTransform.DOScale(Vector3.one * 80f, 0.95f).SetEase(Ease.InCirc).OnComplete(() => _EndCallBack?.Invoke());
    }

    #endregion
}

//필요없음 삭제 요망...(후에 로딩 후처리 과정이 빡세게 필요할시 추상 클래스를 따로 만들어 구조화 요망..)
//public abstract class EndEffectByLoadingSystem
//{
//    protected LoadingManager m_LoadingManager;
//    protected Image[] m_ControllBGs;

//    public virtual void Initlization(System.Action _EndCallBack)
//    {
//        m_ControllBGs[1].gameObject.SetActive(true);
//        m_ControllBGs[0].gameObject.SetActive(false);
//        //_EndCallBack?.Invoke();
//    }
//}
