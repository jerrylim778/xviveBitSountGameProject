using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Sirenix.OdinInspector;
using Commons.Helpers;
using UnityEngine.EventSystems;


#region ForceSubOrderPopUp 에 관한 설명
/// <summary>
/// ForceSubOrderPopUp 에 관한 설명
///1.GUI View에 해당되며 작동하는 SubModuleController들을 가지고 있다
///2.해당 View는 어디에 위치할것인지 어떤 팝업에만 부착할것인지(강제 종속)를 나타내는 추상 클래스이다
///3.단일로 써도될 수 있지만 View에 추가적인 작업은 데코레이션을 통한 하위객체에서 이루어질 수 있도록 한다
///4.마지막으로 같은 GamePlaySystem<클래스>로 호출이 가능한 형태이며
///해당 관리 객체의 종속은 받지않으며 각 강제 종속하는 팝업에 영향을 미친다
/// </summary>
#endregion
public class ForceSubOrderPopUp : SystemBase, I_PopUpInfo, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    public enum EventCallBackType { Down = 0, Drag = 1, Up = 2 }

    [Header("Required Value")]
    [SerializeField] protected string m_ForceOrderPopUpType = string.Empty;
    [SerializeField, ReadOnly, HideIf("@string.IsNullOrEmpty(m_ForceOrderPopUpType)")] protected SystemBase m_DependentPopUp;
    [SerializeField] protected BachRectInfo m_BachRectType;
    [Header("Controller Reference")]
    [SerializeField] protected SubModuleControllerBase m_SupportSubOrderPrefab;
    [SerializeField] protected ParamsSubPopUpControllerInfo m_ParamsControllerInfos;
    [Header("GUI Reference")]
    [SerializeField] private RectTransform[] pp_AllGUIReference;
    [SerializeField, ReadOnly] private List<RectTransform> pp_UsedGUIReference = new(); //굳이 사용하는걸 활용하는건 필요없을 수 있기에 이후 점검후 삭제요망 **
    [Header("ConnectEventData")]
    System.Action<PointerEventData>[] m_EventDataCallBacks; //향후 Dictionary로 변경한다
    //Property
    [field: SerializeField, HideIf("@pp_SupportSubControllerBase == null")] public SubModuleControllerBase pp_SupportSubControllerBase { get; protected set; }


    #region Sub Order Required Functions
    public PopUpType I_GetPopUpType() => PopUpType.InsideGUISubPartType;

    public SystemBase I_GetPopUpBase() => m_DependentPopUp;

    #endregion

    public override void Initlization() { } //상속에 의해 필요해질 수 있는 가능성이 존재함

    public virtual System.Tuple<RectTransform, BachRectInfo, SubModuleControllerBase> Initlization(SystemBase _GetPopUpBase)
    {
        if ((_GetPopUpBase is not I_PopUpInfo || _GetPopUpBase.GetType().Name != m_ForceOrderPopUpType).
        HDebug($"{nameof(SystemBase)}가 팝업형태가 아니거나 종속된 유일값이랑 일치하지 않습니다.!", Helper.HDType.Error))
            return null;

        m_DependentPopUp = _GetPopUpBase;

        pp_SupportSubControllerBase =
        Instantiate(m_SupportSubOrderPrefab, this.transform.position, Quaternion.identity, this.transform);
        RunningSupportEventData(
        pp_SupportSubControllerBase.Initlization(m_DependentPopUp, this, m_ParamsControllerInfos));

        return new System.Tuple<RectTransform, BachRectInfo, SubModuleControllerBase>(
        this.GetComponent<RectTransform>(), m_BachRectType, pp_SupportSubControllerBase);
    }

    #region 각 객체들을 문자열로 받았을때 주석해제 후 사용(사용을 하지 않으시 이후에 삭제 요망)

    public virtual RectTransform FindOutPutGUIView(string _FindName, bool _isUsedResourcess = false)
    {
        var SearchList = _isUsedResourcess ? pp_UsedGUIReference.ToArray() : pp_AllGUIReference;
        int FIDX = -1; FIDX = SearchList.ToList().FindIndex(x => x.name.ReplaceRemoveStrValue("(Clone)") == _FindName);
        if (FIDX != -1)
        {
            var GetApplyRT = SearchList[FIDX];
            if (!_isUsedResourcess && !pp_UsedGUIReference.Contains(GetApplyRT)) pp_UsedGUIReference.Add(GetApplyRT);
            return GetApplyRT;
        }
        (FIDX == -1).HDebug($"찾고자 하는 타입 {_FindName}의 이름이 일치하지 않거나 콜렉션에 존재하지 않습니다.!", Helper.HDType.Error);
        return null;
    }

    public virtual T FindOutPutGUIView<T>(string _FindName, bool _isUsedResourcess) where T : class
    {
        var SearchList = _isUsedResourcess ? pp_UsedGUIReference.ToArray() : pp_AllGUIReference;
        if (SearchList.HFind(x => x.name == _FindName).TryGetComponent<T>(out T _OutPutType))
        {
            var GetApplyRT = (_OutPutType as Component).GetComponent<RectTransform>();
            if (!_isUsedResourcess && !pp_UsedGUIReference.Contains(GetApplyRT)) pp_UsedGUIReference.Add(GetApplyRT);
            return _OutPutType;
        }
        return null;
    }

    #endregion

    #region 유니티 이벤트 함수

    private void RunningSupportEventData(params object[] _CheckParams)
    {
        //리팩토링 할것
        switch (_CheckParams)
        {
            case System.Action<PointerEventData>[] GetActionsEvent:
                m_EventDataCallBacks = GetActionsEvent;
                break;
        }
    }

    private bool CheckCanNotUpdate(EventCallBackType _CompareType)
    => m_EventDataCallBacks == null || m_EventDataCallBacks.Length <= (int)_CompareType;

    public void OnPointerDown(PointerEventData eventData)
    {
        if (CheckCanNotUpdate(EventCallBackType.Down)) return;
        m_EventDataCallBacks[(int)EventCallBackType.Down]?.Invoke(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (CheckCanNotUpdate(EventCallBackType.Drag)) return;
        m_EventDataCallBacks[(int)EventCallBackType.Drag]?.Invoke(eventData);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (CheckCanNotUpdate(EventCallBackType.Up)) return;
        m_EventDataCallBacks[(int)EventCallBackType.Up]?.Invoke(eventData);
    }

    #endregion
}

#region SubOrderInfo Data Reference
#region SubOrderAddPopUpInfo에 관한 설명 (**사용시 필히 정독할것**)
/// <summary>
///SubOrderAddPopUpInfo에 관한 설명 (**사용시 필히 정독할것**)
///다른 재활용을 이용해야 되는 서브 팝업과의 차이점은
///추상적 가치에 종속되어있기에 같은 템플릿이지만 다른 함수를 찾아야 되기
///때문에 추가 Info를 만들어 해당되는 팝업을 가져오기 위한 조치로 정의되었다
///따라서 만일 ForceSubOrderPopUp<클래스>를 상속받아 사용하면
///호출시 해당 추가 데이터 같은 절차가 필요없이 호출하면 되겠다
/// </summary>
#endregion
public struct SubOrderAddPopUpInfo : I_Data
{
    public System.Type s_DependentType;
    public string s_AbsSubOderPartName;

    public SubOrderAddPopUpInfo(System.Type _GetType, string _PartName)
    {
        s_DependentType = _GetType;
        s_AbsSubOderPartName = _PartName;
    }
}

[System.Serializable]
public struct BachRectInfo
{
    public bool s_UseSubRect;
    public Vector2 s_Pivot;
    [ShowIf(nameof(s_UseSubRect))] public Rect s_BachRect;
}


[System.Serializable]
public struct ParamsSubPopUpControllerInfo : ControllerBase.I_PurifiedParamsData
{
    public string s_KeyGUITypeName;
    public string[] s_ReciveGUINames; //해당 부분 전부 RectTranform형태로 바로 드레그 드롭으로 리팩 요망 (하드코딩이 되기 쉽다) **
    [Header("ParsingApplyReference")]
    [ReadOnly] public RectTransform[] m_ReciveGUIReference;
    public bool I_ISGetOutParams(ControllerBase _GetPPData)
    => _GetPPData.GetType().Name == s_KeyGUITypeName;
}
#endregion