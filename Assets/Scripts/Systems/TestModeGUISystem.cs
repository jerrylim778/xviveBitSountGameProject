using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Commons.Helpers;
using Commons.Helpers.Attribute;

public class TestModeGUISystem : MonoBehaviour
{
    #region TestModeGUI Support Infos Reference
    public enum GUIBGType { None = 0, ChooseFirstType, GUISelectType, GUISelectStaticType, SpeedSelectType }
    public enum RequiredGUIBGType { None = 0, ShowLogType, LiverTestType }
    [System.Serializable]
    public struct TestModeGUIBGInfo
    {
        public GUIBGType s_GUIBGType;
        public RectTransform s_BG;
    }
    [System.Serializable]
    public struct ChooseByTestModeInfo
    {
        public TestManager.TestModeType s_ChooseType;
        public Button s_ChooseBtn;
    }

    [System.Serializable]
    public struct TestModeRequiredBGInfo
    {
        public RequiredGUIBGType s_RequiredGUIBGType;
        public TestModeSourcesInfo s_TestModeSourcesInfo;
    }

    [System.Serializable]
    public struct TestModeSourcesInfo
    {
        public Image s_BG;
        public Button s_OpenBGBtn;
        public ScrollRect s_GetScroll;
        public Transform s_GetScrollContent;
        [HideInInspector] public bool s_isOnShow;
    }
    #endregion

    [Header("Main GUI")]
    [SerializeField] private Button m_ExitBtn;
    [SerializeField, Lim_InspectorReadOnly] private TestManager.TestModeType m_RunningForTMType;
    [SerializeField] private TestModeRequiredBGInfo[] m_TestModeRequiredBGInfo;
    [SerializeField] private TestModeGUIBGInfo[] m_TestModeGUIBGInfos;
    [Header("ChooseFirst Reference")]
    [SerializeField] private ChooseByTestModeInfo[] m_ChooseByTestModeBtns;
    [Tooltip("Main Values")]
    private TestManager m_TestManager;
    private Dictionary<GUIBGType, System.Tuple<bool, RectTransform>> m_DICInitTestModeGUI;
    private Dictionary<RequiredGUIBGType, TestModeSourcesInfo> m_DICRequiredTestModeGUI;

    public TestManager.TestModeType pp_RunningForTMType { get => m_RunningForTMType;}

    #region Initlization Test GUI System Reference
    //외부에서 인위적인 실행이기 때문에 대부분 Editor에서 호출할 가능성이 높음
    public void Initlization(TestManager _GetManager, System.Type _GetType, 
    TestManager.TestModeType _GetModeType = TestManager.TestModeType.None)
    {
        m_TestManager = _GetManager;
        Initlization();
        m_RunningForTMType = _GetType.Name.Contains("Editor") ?
        TestManager.TestModeType.EditorScriptsController : _GetModeType;
        m_TestManager.OrderTest(_GetType, m_RunningForTMType);
    }

    public void Initlization(params MonoBehaviour[] _GetApplyMonos)
    {
        m_TestManager = _GetApplyMonos[0] as TestManager;
        Initlization();
        ReturnInitOutBG(GUIBGType.ChooseFirstType);
    }

    public void Initlization()
    {
        GetComponent<RectTransform>().offsetMax = Vector2.zero;
        GetComponent<RectTransform>().offsetMin = Vector2.zero;
        m_ExitBtn.onClick.AddListener(() => m_TestManager.ExecuteOutTestMode(m_RunningForTMType));
        m_DICRequiredTestModeGUI = new Dictionary<RequiredGUIBGType, TestModeSourcesInfo>();
        m_TestModeRequiredBGInfo.ToList().ForEach(x =>
        {
            x.s_TestModeSourcesInfo.s_isOnShow = false;
            x.s_TestModeSourcesInfo.s_BG.gameObject.SetActive(false);
            AddBtnByRequredInfoType(x.s_RequiredGUIBGType, x.s_TestModeSourcesInfo);
            m_DICRequiredTestModeGUI.Add(x.s_RequiredGUIBGType, x.s_TestModeSourcesInfo);
        });
        
        m_DICInitTestModeGUI = new Dictionary<GUIBGType, System.Tuple<bool, RectTransform>>();
        m_TestModeGUIBGInfos.ToList().ForEach(x =>
        {
            m_DICInitTestModeGUI.Add(x.s_GUIBGType, new System.Tuple<bool, RectTransform>(false, x.s_BG)); 
            x.s_BG.gameObject.SetActive(false);
        });
    }
    #endregion

    //Main Test Mode By Type
    public void ReturnInitOutBG(GUIBGType _OutPutType)
    {
        m_DICInitTestModeGUI.ToList().ForEach(x => x.Value.Item2.gameObject.SetActive(false));
        bool isInit = m_DICInitTestModeGUI[_OutPutType].Item1;
        switch (_OutPutType)
        {
            case GUIBGType.ChooseFirstType:
                if (!isInit) ChooseFirstInit();
                break;
            case GUIBGType.GUISelectType:
                break;
            case GUIBGType.GUISelectStaticType:
                break;
            case GUIBGType.SpeedSelectType:
                break;
        }
        m_DICInitTestModeGUI[_OutPutType].Item2.gameObject.SetActive(true);
    }

    //Required Test Mode By Type
    private void AddBtnByRequredInfoType(RequiredGUIBGType _CompareType, TestModeSourcesInfo _GetSubInfo)
    {
        _GetSubInfo.s_OpenBGBtn.onClick.AddListener(() => OnClickShowBGBtn(_CompareType, () =>
        {
            _GetSubInfo.s_BG.gameObject.SetActive(true);
            
            int SetCount = _CompareType switch
            {
                RequiredGUIBGType.ShowLogType => m_TestManager.pp_OutPutLogInfoStack.Count,
                RequiredGUIBGType.LiverTestType => m_TestManager.pp_OutPutLiverInfoStack.Count,
                _ => 0
            };
            while (SetCount > 0)
            {
                OutPutTestModeLog(_CompareType, _GetSubInfo,
                _CompareType switch
                {
                    RequiredGUIBGType.ShowLogType => m_TestManager.pp_OutPutLogInfoStack.Pop(),
                    RequiredGUIBGType.LiverTestType => m_TestManager.pp_OutPutLiverInfoStack.Pop(),
                    _ => 0
                });
                SetCount--;
            }
        }));
    }

    #region Sub System Private Functions [Reqried TestMode Info]
    #region 나중에 참고해서 쓰라고 함수 지우지 않음(아님 Helper에 지정하여 다른곳에서도 해당 방식처럼 사용할것 그 이후 삭제요망..)
    //private void ApplyRequiredDictionary<T>(string _VarName, System.Func<T> _ActionByVar)
    //{
    //    TestModeSourcesInfo GetInfo = m_DICRequiredTestModeGUI[RequiredGUIBGType.ShowLogType];
    //    if ((!Helper.SetFieldNameToClass(GetInfo, _VarName, _ActionByVar.Invoke())).
    //    HDebug($"{GetInfo.GetType().Name}에서 {_VarName}이라는 변수가 존재하지 않거나 타입이 맞지 않습니다.!", Helper.HDType.Error))
    //    return;

    //    int GetPairIDX =
    //    m_DICRequiredTestModeGUI.ToList().FindIndex(x => x.Key == RequiredGUIBGType.ShowLogType);
    //    m_DICRequiredTestModeGUI.ToList()[GetPairIDX] =
    //    new KeyValuePair<RequiredGUIBGType, TestModeSourcesInfo>(RequiredGUIBGType.ShowLogType, GetInfo);
    //}
    #endregion

    private bool ApplyLogState(RequiredGUIBGType _CurrType)
    {
        TestModeSourcesInfo GetInfo = m_DICRequiredTestModeGUI[_CurrType];
        bool isCompareOnShow = GetInfo.s_isOnShow;
        isCompareOnShow = !isCompareOnShow; 
        GetInfo.s_isOnShow = isCompareOnShow;
        m_DICRequiredTestModeGUI[_CurrType] = GetInfo;
        return isCompareOnShow;
    }

    private void OnClickShowBGBtn(RequiredGUIBGType _CurrType, System.Action _ComparetoCallBack)
    {
        bool isOn = ApplyLogState(_CurrType);
        m_DICRequiredTestModeGUI[_CurrType].s_BG.gameObject.SetActive(m_DICRequiredTestModeGUI[_CurrType].s_isOnShow);
        m_DICRequiredTestModeGUI.ToList().ForEach(x =>
        x.Value.s_OpenBGBtn.transform.SetSiblingIndex(x.Key == _CurrType ? x.Value.s_OpenBGBtn.transform.parent.childCount - 1 : 0));

        if (isOn)
        {
            m_DICRequiredTestModeGUI[_CurrType].s_GetScrollContent.ChildLinearStuctureSearch<TestModeShowModule>().ToList().ForEach(x =>
            x.gameObject.SetActive(_CurrType == x.pp_ThisModuleGUIBGType));
            _ComparetoCallBack?.Invoke();
        }
        //else { } //아직 존재하지 않음
    }

    #region Show Modules Reference
    //이건 자동으로 Helper의 로직에 TestManager<클래스>를 탐색하여 삽입하면 되겠다 
    public void OutPutTestModeLog(RequiredGUIBGType _CurrType, TestModeSourcesInfo _GetInfo, params object[] _ApplyObject)
    {
        TestModeShowModule GetModule =
        Instantiate(m_TestManager.TestOutPutRequiredObj<TestModeShowModule>(false, false), _GetInfo.s_GetScrollContent/*_SetPar*/);
        GetModule.Initlization(_CurrType, _GetInfo, _ApplyObject);
    }
    #endregion

    #endregion

    #region Type OutPut BG Sub System Private Functions

    private void ChooseFirstInit()
    {
        m_ChooseByTestModeBtns.ToList().ForEach(x =>
        x.s_ChooseBtn.onClick.AddListener(() =>
        {
            m_ChooseByTestModeBtns.ToList().ForEach(x => x.s_ChooseBtn.onClick.RemoveAllListeners());
            m_DICInitTestModeGUI[GUIBGType.ChooseFirstType].Item2.gameObject.SetActive(false);
            m_TestManager.OrderTest(this.GetType(), x.s_ChooseType);
        }));
        RectTransform GetBG = m_DICInitTestModeGUI[GUIBGType.ChooseFirstType].Item2;
        m_DICInitTestModeGUI[GUIBGType.ChooseFirstType] = new System.Tuple<bool, RectTransform>(true, GetBG);
    }

    #endregion

    private void OnDestroy()
    {
        m_DICRequiredTestModeGUI?.ToList().ForEach(x => x.Value.s_OpenBGBtn.onClick.RemoveAllListeners());
        m_ChooseByTestModeBtns.ToList().ForEach(x => x.s_ChooseBtn.onClick.RemoveAllListeners());
    }
}