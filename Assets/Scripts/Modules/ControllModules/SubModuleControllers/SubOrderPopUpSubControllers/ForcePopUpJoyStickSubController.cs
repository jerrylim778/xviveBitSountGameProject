using UnityEngine;
using UnityEngine.EventSystems;
using Sirenix.OdinInspector;
using Commons.Helpers;
using System;

public class ForcePopUpJoyStickSubController : SubModuleControllerBase//, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [Header("====Temp Required Values====")]
    [SerializeField, ReadOnly] private CharacterICNRequiredPopUp m_GetRequiredPopUp;
    [SerializeField, ReadOnly] private ForceSubOrderPopUp m_VIewModuleForceSubOrder;
    [SerializeField, ReadOnly] private ParamsSubPopUpControllerInfo m_ReciveGUIReferenceInfo;
    [Header("Control JoyStick")]
    [SerializeField] private bool m_isShowJoyStick;
    [SerializeField, Range(0f, 10f)] private float m_LeverRange = 10f;
    [SerializeField, Range(0f, 10f)] private float m_ScreenBlockBoundary = 0f;
    [Header("Control JoyStick ReadOnly")]
    [SerializeField, ReadOnly] private bool m_isUsingJoyStick = false; //, m_isCanDrag = false;
    [SerializeField, ReadOnly] private Vector2 m_OGPosByBGRT;
    [Header("RequiredValues_UIReference")]
    [SerializeField, ReadOnly] private RectTransform m_BaseRT;
    [SerializeField, ReadOnly] private RectTransform m_BGRT, m_LeverRT;
    [Header("RequiredValues_OtherReference")]
    [SerializeField, ReadOnly] Canvas m_UsingMainCanvas;
    [SerializeField, ReadOnly] Camera m_CanvasCamera;
    [Header("OutPut Power")]
    [field: SerializeField, ReadOnly] public Vector2 pp_GetMainPower { get; private set; }

    public override object[] Initlization(params object[] _ParsingParams)
    {
        m_GetRequiredPopUp = _ParsingParams[0] as CharacterICNRequiredPopUp;
        m_VIewModuleForceSubOrder = _ParsingParams[1] as ForceSubOrderPopUp;
        m_ReciveGUIReferenceInfo = (ParamsSubPopUpControllerInfo)_ParsingParams[2];
        if ((!m_ReciveGUIReferenceInfo.I_ISGetOutParams(this)).HDebug($"가져온 SubController의 파라미터가 " +
        $"{nameof(ForcePopUpJoyStickSubController)}이 아닙니다.!", Helper.HDType.Error)) return null;

        var GetSubFindNames = m_ReciveGUIReferenceInfo.s_ReciveGUINames;
        m_UsingMainCanvas = GamePlaySystem.Instance.pp_UsingMainCan;
        m_BaseRT = m_VIewModuleForceSubOrder.FindOutPutGUIView(GetSubFindNames[0], false);
        m_BGRT = m_VIewModuleForceSubOrder.FindOutPutGUIView(GetSubFindNames[1], false);
        m_LeverRT = m_VIewModuleForceSubOrder.FindOutPutGUIView(GetSubFindNames[2], false);
        Vector2 SetRatio = m_UsingMainCanvas.GetComponent<RectTransform>().sizeDelta;

        #region Init Controller
        if (m_UsingMainCanvas.renderMode == RenderMode.ScreenSpaceCamera)
            m_CanvasCamera = m_UsingMainCanvas.worldCamera;

        Vector2 StaticPoint = new Vector2(0.5f, 0.5f);
        m_BaseRT.sizeDelta = SetRatio;
        m_BGRT.pivot = StaticPoint;
        m_LeverRT.pivot = StaticPoint;
        m_LeverRT.anchorMin = StaticPoint;
        m_LeverRT.anchorMax = StaticPoint;
        m_BGRT.anchoredPosition = Vector2.zero;
        m_LeverRT.anchoredPosition = Vector2.zero;

        m_isUsingJoyStick = false;

        m_OGPosByBGRT = m_BGRT.anchoredPosition;
        if (!m_isShowJoyStick) m_BGRT.gameObject.SetActive(false);
        #endregion

        System.Action<PointerEventData>[] GetEventPointer =  
        new System.Action<PointerEventData>[3] { OnPointerDown, OnDrag, OnPointerUp };
        m_isActionProcess = true;
        return GetEventPointer;
    }

    public override void BreakPoint(bool _isBreak, SubModuleBreakType _BrackType = SubModuleBreakType.BreakAll, Type _GetType = null)
    {
        base.BreakPoint(_isBreak, _BrackType, _GetType);
        m_BaseRT.GetComponent<UnityEngine.UI.Image>().raycastTarget = _isBreak;
        /*if(!_isBreak)*/ ResetJoyStick();
    }

    private void ResetJoyStick()
    {
        pp_GetMainPower = Vector2.zero;
        m_LeverRT.anchoredPosition = Vector2.zero;
        m_isUsingJoyStick = false;
        m_BGRT.anchoredPosition = m_OGPosByBGRT;
        if (!m_isShowJoyStick) m_BGRT.gameObject.SetActive(false);
    }

    #region On Handler Event System Functions

    public void OnPointerDown(PointerEventData eventData)
    {
        //향우 WorkdPos에 위치한 Canvas의 클릭에 대한 연결 System을 설계 시 사용할것
        //m_isCanDrag = !WorldSpaceRaycaster.Raycast(eventData); //if (!m_isCanDrag) return;
        if (!m_isActionProcess)
        {
            ResetJoyStick();
            return;
        }

        if (!m_isShowJoyStick) m_BGRT.gameObject.SetActive(true);

        m_BGRT.anchoredPosition = ScreenPointToAnchoredPosition(eventData.position);

        m_isUsingJoyStick = true;

        OnDrag(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!m_isActionProcess || !m_isUsingJoyStick)
        {
            ResetJoyStick();
            return;
        }

        Vector2 position = RectTransformUtility.WorldToScreenPoint(m_CanvasCamera, m_BGRT.position); //카메라가 없다면 기존의 m_BGRT의 Position 값을 반환
        Vector2 radius = m_BGRT.sizeDelta * 0.5f;
        pp_GetMainPower = (eventData.position - position) / (radius * m_UsingMainCanvas.scaleFactor); //캔퍼스 크기에 맞게 조정함 (ScreenSpace일때만 가능)
        HandleInput(pp_GetMainPower.magnitude, pp_GetMainPower.normalized);//, radius, m_CanvasCamera); //후에 상속에 대비한 형태가 될 수 있다
        m_LeverRT.anchoredPosition = pp_GetMainPower * radius * m_LeverRange;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        //WorldSpaceRaycaster.OnPointerUp(eventData); //다른 클릭이 가능하다는것을 알림 (WorldPos의 Canvas클릭을 할 수 있다는것을 알림)
        if (!m_isActionProcess || !m_isUsingJoyStick)
            return;

        ResetJoyStick();
    }

    #endregion

    #region Sub System Private Functions

    private void HandleInput(float _GetVecMag, Vector2 _ArrowNormalVec)
    {
        if (_GetVecMag > m_ScreenBlockBoundary)
        {
            if (_GetVecMag > 1)
                pp_GetMainPower = _ArrowNormalVec;
        }
        else pp_GetMainPower = Vector2.zero;
    }

    protected Vector2 ScreenPointToAnchoredPosition(Vector2 _EventScreenPos)
    {
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(m_BaseRT, _EventScreenPos, m_CanvasCamera, out Vector2 _LocalPoint))
        {
            Vector2 PivotOffset = m_BaseRT.pivot * m_BaseRT.sizeDelta;
            return _LocalPoint - (m_BGRT.anchorMax * m_BaseRT.sizeDelta) + PivotOffset;
        }
        return Vector2.zero;
    }

    #endregion
}
