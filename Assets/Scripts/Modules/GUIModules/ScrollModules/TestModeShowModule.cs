using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Commons.Helpers;
using Commons.Helpers.Attribute;

public class TestModeShowModule : MonoBehaviour
{
    [SerializeField, Lim_InspectorReadOnly] private TestModeGUISystem.RequiredGUIBGType m_ThisModuleGUIBGType;
    [SerializeField] RectTransform[] m_GetOutPutBG;

    public TestModeGUISystem.RequiredGUIBGType pp_ThisModuleGUIBGType { get => m_ThisModuleGUIBGType; }

    public void Initlization(TestModeGUISystem.RequiredGUIBGType _CurrType, TestModeGUISystem.TestModeSourcesInfo _GetInfo, params object[] _ApplyObject)
    {
        m_ThisModuleGUIBGType = _CurrType;
        m_GetOutPutBG.ToList().ForEach(x => x.gameObject.SetActive(false));
        RectTransform ApplyRT = m_GetOutPutBG[m_ThisModuleGUIBGType == TestModeGUISystem.RequiredGUIBGType.ShowLogType ? 0 : 1];
        Text[] GetApplyTxt = ApplyRT.transform.GetChild(0).ChildLinearStuctureSearch<Text>();
        ApplyRT.gameObject.SetActive(true);
        Button UseButton = this.GetComponent<Button>();
        UseButton.interactable = false;
        switch (m_ThisModuleGUIBGType)
        {
            case TestModeGUISystem.RequiredGUIBGType.ShowLogType:
                #region Check Support BG Child
                RectTransform ShowDetailLogRT = _GetInfo.s_BG.transform.GetChild(1) as RectTransform;
                if (ShowDetailLogRT.gameObject.activeSelf) ShowDetailLogRT.gameObject.SetActive(false);
                #endregion
                #region Init ShowLogs
                TestManager.OutPutLogInfo GetLogInfo = (TestManager.OutPutLogInfo)_ApplyObject[0];
                Color ApplyColor = GetLogInfo.s_OutPUtHDType switch
                {
                    Helper.HDType.Defult => Color.white,
                    Helper.HDType.Warning => Color.yellow,
                    Helper.HDType.Error => Color.red,
                    _ => Color.gray
                };
                GetApplyTxt.ToList().ForEach(x => x.color = ApplyColor);
                GetApplyTxt[0].text = GetLogInfo.s_OutPUtHDType.ToString();
                GetApplyTxt[1].text = GetLogInfo.s_OutPutStr;
                UseButton.onClick.AddListener(() => ShowLogDetail(GetLogInfo.s_OutPutStr, ShowDetailLogRT));
                UseButton.interactable = true;
                #endregion
                break;
            case TestModeGUISystem.RequiredGUIBGType.LiverTestType:
                #region Init LiverTest
                TestManager.LiverOutPutInfo GetLiverInfo = (TestManager.LiverOutPutInfo)_ApplyObject[0];
                GetApplyTxt[0].text = GetLiverInfo.s_LiverName;
                UseButton.onClick.AddListener(() => GetLiverInfo.s_GetCallback?.Invoke());
                UseButton.interactable = true;
                #endregion
                break;
        }
    }

    private void ShowLogDetail(string _GetTxt, RectTransform _GetDetailBG)
    {
        _GetDetailBG.GetChild(0).GetComponent<Text>().text = _GetTxt;
        _GetDetailBG.GetChild(1).GetComponent<Button>().onClick.AddListener(() => _GetDetailBG.gameObject.SetActive(false));
    }
}