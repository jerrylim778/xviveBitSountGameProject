using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Commons;

//기존 311Ver 참고하고 새롭게 리팩토링된 시스템 스택 팝업으로 재정의할것
//(관련된 함수 정의후 아래것 주석처리로 모든 데이터를 SettingManager에 넣을 수 있도록 수정설계 요망....
public class SettingPopUp : MonoBehaviour
{
    [Header("SettingProcess")]
    [SerializeField] private Button[] SettingCategoryBtnArray;
    [SerializeField] private RectTransform SettingBG;
    [SerializeField] private Image VFXSettingBG; //필요치 않을 수 있음
    [Header("SettingBaseProcess")]
    [SerializeField] SettingCatecoryInfo m_CatecoryInfoNow;
    [SerializeField] SettingCatecoryInfo[] m_CatecoryInfoAll;
    [SerializeField] private SettingReferenceInfo[] m_ReferenceInfo;
    [Tooltip("Required Values")]
    private bool m_isAnySettingOneChanged = false;

    #region 반드시 다시 정의할것
    public void BothAllUIEvent()
    {
        m_isAnySettingOneChanged = true;
        //if (GameManager.instance.isLogAll) Debug.LogWarning("셋팅값 변경됨" + m_isAnySettingOneChanged);
    }

    //저장되는 원리를 잘 이해 해야한다
    private void SaveAllDataByPopUp()
    {
        m_CatecoryInfoAll.ToList().ForEach(x => x.s_CategoryBase.ApplyPopUpSendInfoData());
        Appinstance.Instance.ms_SettingManager.AllSettingValueApplyInGame();
        Appinstance.Instance.ms_SettingManager.LocalSettingJsonData(SettingJsonType.Save);
        //참고로 리셋타입은 (아래와 같은 방법 참고 => 무조건 기존 로직을 참고해라 해당 함수에서는 사용하는게 아님)*****
        //Appinstance.instance.m_SettingManager.AllResetJsonData();
        //m_CatecoryInfoAll.ToList().ForEach(x => x.s_CategoryBase.ApplyPopUpOutPut());
        //m_isAnySettingOneChanged = false;
        //m_TitleUIManager.PopPopUp();
    }

    public void SetOutPutReference(SettingPartOfButton _SetInfo)
    {
        //SettingReferenceInfo GetReferInfo = null;
        //m_ReferenceInfo.ToList().ForEach(x =>
        //{
        //    if (x.s_CatecoryBG.gameObject.activeSelf)
        //        x.s_CatecoryBG.gameObject.SetActive(false);

        //    if (x.s_OutPutReferType == _SetInfo.s_OutPutReferType)
        //        GetReferInfo = x;
        //});

        //if (GetReferInfo != null)
        //{
        //    for (int i = 0; i < GetReferInfo.s_ReferTxtArray.Length; i++)
        //    {
        //        GetReferInfo.s_ReferTxtArray[i].text =
        //            Appinstance.instance.m_TranslateManager.CheckFindLanguage(_SetInfo.s_ReferTxtArray[i]);
        //        switch (GetReferInfo.s_OutPutReferType)
        //        {
        //            case SettingReferenceType.SingleImgType:
        //                GetReferInfo.s_SendImgArray[0].sprite = _SetInfo.s_SendSprArray[0];
        //                continue;
        //            case SettingReferenceType.CompareImgType:
        //                GetReferInfo.s_SendImgArray[i].sprite = _SetInfo.s_SendSprArray[i];
        //                continue;
        //        }
        //    }
        //    GetReferInfo.s_CatecoryBG.gameObject.SetActive(true);
        //}
        //else
        //    Debug.LogWarning("해당 [SettingPartOfButton]의 타입이 [Reference]의 타입중에 존재하지 않습니다");
    }
    #endregion
}

#region SettingPopUp Required Logic Reference
[System.Serializable]
public abstract class SettingCategoryInfoBase
{
    public SettingCatrcoryType s_CategoryGetType;
    public abstract void SettingValueApplyInGame(out bool _isCategorySettingApply);
}

public abstract class SettingCategoryBase : MonoBehaviour
{
    #region 옵션 카테고리 부모 클래스에 대한 설명
    //메니져는 SettingPopUp<클래스>를 두고 
    //각 카테고리별 로직들을 제어하는 역활로 가자
    //메니져의 역활
    //1.카테코리별 다른 창을 띄우고 초기값을 발동할 수 있도록
    //(초기화된값을 OutPut하기 위함)
    //2.카테고리내에 EventTrigger의 발동에 따라 Reference를 나타내주는 역활
    #endregion
    [SerializeField] protected SettingPopUp m_SettingPopUpManager;
    [SerializeField] protected SettingPartOfButton[] m_GetButtonByCategory;
    public bool pp_isUpdateSetting { get; private set; }

    //초기값
    public virtual void CategoryProcessFirst(SettingPopUp _GetPopUp)
    {
        if (m_SettingPopUpManager == null) m_SettingPopUpManager = _GetPopUp;
    }

    #region Setting In and OutPut Flow 설정값 저장과 로드 로직
    #region 설정값 저장 로드에 대한 설명
    //SettingManager에서 각 카테고리별 아웃풋과 서버저장 + 각 시스템에 적용 등등을
    //대기(저장하기 전까진 대기상태)의 데이터 + 실제 저장한 데이터를 나눠서
    //대기구간에서 저장이 되기전까지 모든 비교를 마친다음 원래 대기상태에서 하나라도 
    //다를경우 저장의 노티스여부를 띄울지같은 업데이트된 bool변수를 참으로 변경한다
    //그중 스크립터블 오브젝트 처럼 카테고리별 역으로 정보에 따라 나타내야 하는 구간은 
    //각 하위객체에서 정의하면 가독성과 수정에 용이할꺼 같다
    #endregion
    public abstract void ApplyPopUpOutPut();
    public abstract void ApplyPopUpSendInfoData();

    //저장시
    public virtual string ApplyStrValueByUIEvnetType<T>(Selectable _GetEventInfo)
    {
        System.Type GetTypeTemplate = typeof(T);
        string SaveApplyStr = string.Empty;
        switch (GetTypeTemplate.Name)
        {
            case "Dropdown":
                Dropdown GetDD = (Dropdown)_GetEventInfo;
                SaveApplyStr = GetDD.options[GetDD.value].text;
                break;
            case "Slider":
                Slider GetSL = (Slider)_GetEventInfo;
                SaveApplyStr = ((int)GetSL.value).ToString();
                break;
                //case "Toggle_ButtonSystem":
                //    Toggle_ButtonSystem GetTB = (Toggle_ButtonSystem)_GetEventInfo;
                //    SaveApplyStr = GetTB.isOn.ToString();
                //    break;
        }
        return SaveApplyStr;
    }

    //로드시
    public virtual Selectable ApplyUIEvnetTypeByStrValue<T>(Selectable _GetEventInfo, string _ApplyStr)
    {
        System.Type GetTypeTemplate = typeof(T);
        switch (GetTypeTemplate.Name)
        {
            case "Dropdown":
                Dropdown GetDD = (Dropdown)_GetEventInfo;//.s_Selectable;
                GetDD.value = GetDD.options.FindIndex(x => x.text == _ApplyStr);
                return GetDD;
            case "Slider":
                Slider GetSL = (Slider)_GetEventInfo;//.s_Selectable;
                Text GetOutPutText = GetSL.transform.parent.GetChild(1).GetComponent<Text>();
                float GetValue = float.Parse(_ApplyStr);
                GetSL.value = (int)GetValue; GetOutPutText.text = ((int)GetValue).ToString();
                GetSL.onValueChanged.AddListener((_value) => GetOutPutText.text = ((int)_value).ToString());
                return GetSL;
                //case "Toggle_ButtonSystem":
                //    Toggle_ButtonSystem GetTB = (Toggle_ButtonSystem)_GetEventInfo;//.s_Selectable;
                //    GetTB.CheckToogleState(bool.Parse(_ApplyStr), false);
                //    return GetTB;
        }

        return null;
    }

    //모든 UIEvent들 초기화
    public virtual void InitAllSelectableEvent(bool _isAddListen)
    {
        foreach (SettingPartOfButton one in m_GetButtonByCategory)
        {
            one.s_UIEventAction.interactable = _isAddListen;
            switch (one.s_UIEventAction)
            {
                case Button customButton:
                    if (_isAddListen) customButton.onClick.AddListener(m_SettingPopUpManager.BothAllUIEvent);
                    else customButton.onClick.RemoveAllListeners();
                    break;
                case Dropdown customDropDown:
                    if (_isAddListen)
                        customDropDown.onValueChanged.AddListener((int _value) => m_SettingPopUpManager.BothAllUIEvent());
                    else customDropDown.onValueChanged.RemoveAllListeners();
                    break;
                case Slider customSlider:
                    if (_isAddListen)
                        customSlider.onValueChanged.AddListener((float _value) => m_SettingPopUpManager.BothAllUIEvent());
                    else customSlider.onValueChanged.RemoveAllListeners();
                    break;
                //case Toggle_ButtonSystem customToggleButton:
                //    if (_isAddListen)
                //        customToggleButton.onValueChanged.AddListener((bool _value) => m_SettingPopUpManager.BothAllUIEvent());
                //    else customToggleButton.onValueChanged.RemoveAllListeners();
                //break;
                default:
                    //if(GameManager.instance.isLogAll)
                    Debug.LogWarningFormat("변경할 UIEvent타입이 없습니다..!! {0}", one.s_CatecoryEventButtonBG.name);
                    break;
            }
        }
    }

    #endregion

    #region 각 카테고리내에 버튼들의 데코레이션
    public virtual void OnActiveTriggerEnter(Image _BGImg) => SetColorBGAlpha(_BGImg, true);
    public virtual void OnActiveTriggerExit(Image _BGImg) => SetColorBGAlpha(_BGImg, false);

    protected virtual void SetColorBGAlpha(Image _GetImg, bool _isActive)
    {
        //1.첫번째로 각 트위닝을 IsActive마다 실시한다
        //2.이미지를 비교한 정보에서 관련된 Reference를 출력하기 위해
        //SettingPopUp에서 관련된걸 찾아 Reference를 켜준다
        if (!pp_isUpdateSetting)
            return;

        Color GetColor = _GetImg.color;
        GetColor = _isActive ? Color.red : Color.gray;
        GetColor.a = _isActive ? 0.9f : 0.6f;
        _GetImg.color = GetColor;

        if (_isActive)
        {
            int FIDX = -1;
            FIDX = System.Array.FindIndex(m_GetButtonByCategory, x => x.s_CatecoryEventButtonBG.name == _GetImg.name);
            if (FIDX != -1) m_SettingPopUpManager.SetOutPutReference(m_GetButtonByCategory[FIDX]);
        }
    }
    #endregion
}

#endregion