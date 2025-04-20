using UnityEngine;
using UnityEngine.EventSystems;
using Sirenix.OdinInspector;
using Commons.Helpers;

//사용안함 (말그대로 로직만 참고하기 위해 다른곳에서 가져옴
public class Test_JoyStickSubPopUp : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [Header("Control JoyStick")]
    [SerializeField, ReadOnly] private Vector2 m_OGPosByBGRT;
    [SerializeField, ReadOnly] private bool m_isUsingJoyStick = false; //, m_isCanDrag = false;
    [SerializeField, Range(0f, 10f)] private float m_LeverRange = 10f;
    [SerializeField, Range(0f, 10f)] private float m_ScreenBlockBoundary = 0f;
    [Header("RequiredValues_UIReference")]
    [SerializeField, ReadOnly] private RectTransform m_BaseRT;
    [SerializeField] private RectTransform m_BGRT, m_LeverRT;
    [Header("RequiredValues_OtherReference")]
    [SerializeField, ReadOnly] Camera m_CanvasCamera;
    [SerializeField, ReadOnly] Canvas m_UsingMainCanvas;

    [Header("OutPut Power")]
    [field: SerializeField, ReadOnly] public Vector2 pp_GetMainPower { get; private set; }

    private void Start()
    {
        var StaticRatio = Appinstance.Instance != null ?
        Appinstance.Instance.ms_DataManager.pp_RatioApply : new Vector2(640f, 960f);
        Initialise(FindAnyObjectByType<Canvas>(), StaticRatio);
    }

    public void Initialise(Canvas _CompareMainCanvas, Vector2 _SetRatio)
    {
        //이후 팝업에 포함된 GamePlaySystem<클래스>의 Canvas에 따른다
        m_UsingMainCanvas = _CompareMainCanvas;

        m_BaseRT = GetComponent<RectTransform>();

        if (_CompareMainCanvas.renderMode == RenderMode.ScreenSpaceCamera)
            m_CanvasCamera = _CompareMainCanvas.worldCamera;

        Vector2 StaticPoint = new Vector2(0.5f, 0.5f);
        m_BaseRT.sizeDelta = _SetRatio;
        m_BGRT.pivot = StaticPoint;
        m_LeverRT.pivot = StaticPoint;
        m_LeverRT.anchorMin = StaticPoint;
        m_LeverRT.anchorMax = StaticPoint;
        m_LeverRT.anchoredPosition = Vector2.zero;

        m_isUsingJoyStick = false;

        m_OGPosByBGRT = m_BGRT.anchoredPosition;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        //향우 WorkdPos에 위치한 Canvas의 클릭에 대한 연결 System을 설계 시 사용할것
        //m_isCanDrag = !WorldSpaceRaycaster.Raycast(eventData);

        //if (!m_isCanDrag) return;

        //m_BGRT.gameObject.SetActive(true);

        m_BGRT.anchoredPosition = ScreenPointToAnchoredPosition(eventData.position);

        m_isUsingJoyStick = true;

        OnDrag(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!m_isUsingJoyStick)
            return;

        Vector2 position = RectTransformUtility.WorldToScreenPoint(m_CanvasCamera, m_BGRT.position); //카메라가 없다면 기존의 m_BGRT의 Position 값을 반환
        Vector2 radius = m_BGRT.sizeDelta * 0.5f;
        pp_GetMainPower = (eventData.position - position) / (radius * m_UsingMainCanvas.scaleFactor); //캔퍼스 크기에 맞게 조정함 (ScreenSpace일때만 가능)
        HandleInput(pp_GetMainPower.magnitude, pp_GetMainPower.normalized);//, radius, m_CanvasCamera); //후에 상속에 대비한 형태가 될 수 있다
        m_LeverRT.anchoredPosition = pp_GetMainPower * radius * m_LeverRange;
    }

    private void HandleInput(float _GetVecMag, Vector2 _ArrowNormalVec)
    {
        if (_GetVecMag > m_ScreenBlockBoundary)
        {
            if (_GetVecMag > 1)
                pp_GetMainPower = _ArrowNormalVec;
        }
        else pp_GetMainPower = Vector2.zero;
    }
    

    public void OnPointerUp(PointerEventData eventData)
    {
        //WorldSpaceRaycaster.OnPointerUp(eventData); //다른 클릭이 가능하다는것을 알림 (WorldPos의 Canvas클릭을 할 수 있다는것을 알림)

        if (!m_isUsingJoyStick)
            return;

        //m_BGRT.gameObject.SetActive(false);
        pp_GetMainPower = Vector2.zero;
        m_LeverRT.anchoredPosition = Vector2.zero;

        m_isUsingJoyStick = false;
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

    #region Sub System Private Function
    //private void SetStaticRect(RectTransform _ApplyRT, Vector2 _StaticRect, bool _isSetAOffset = false)
    //{
    //    _ApplyRT.pivot = _StaticRect;
    //    Vector2 GetVH = _ApplyRT.sizeDelta;
    //    if (_isSetAOffset)
    //    {
    //        _ApplyRT.anchorMin = _StaticRect;
    //        _ApplyRT.anchorMax = _StaticRect;
    //    }
    //    _ApplyRT.sizeDelta = GetVH;
    //}
    #endregion

    #region 엥커가 0,0 에서만 작업된다고 가정하고 적용한 로직들
    //public void OnBeginDrag(PointerEventData eventData)
    //{
    //    //시작시 클릭한 지점으로부터
    //    float MX = Input.mousePosition.normalized.x;
    //    float MY = Input.mousePosition.normalized.y;
    //    ControlJoystickLever(/*new Vector2(MX, MY) - Vector2.one*/ new Vector2(MX, MY), eventData);
    //}

    //public void OnDrag(PointerEventData eventData)
    //{
    //    float MX = Input.mousePosition.normalized.x;
    //    float MY = Input.mousePosition.normalized.y;
    //    ControlJoystickLever(/*new Vector2(MX, MY) - Vector2.one*/ new Vector2(MX, MY), eventData);
    //}

    //public void OnEndDrag(PointerEventData eventData)
    //{
    //    lever.anchoredPosition = Vector2.zero;
    //}

    //public void ControlJoystickLever(Vector2 _StartPos, PointerEventData eventData)
    //{
    //    Debug.Log($"{_StartPos} ::: {eventData.position} ::: {eventData.position - rectTransform.anchoredPosition} ::: {rectTransform.anchoredPosition}");
    //    var inputDir = eventData.position - rectTransform.anchoredPosition;
    //    var clampedDir = inputDir.magnitude < leverRange ? inputDir : inputDir.normalized * leverRange;
    //    lever.anchoredPosition = clampedDir;
    //    pp_GetMainPower = clampedDir / leverRange;
    //}
    #endregion
}
