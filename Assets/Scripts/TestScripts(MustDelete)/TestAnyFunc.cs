using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AI;
using Unity.AI.Navigation;
using DG.Tweening;
using Commons;
using Commons.Helpers;
using Sirenix.OdinInspector;
using TMPro;

public class TestAnyFunc : MonoBehaviour
{
    //[SerializeField] private CheckTimeSubModuleController m_GetCheckTime;
    [SerializeField] private TextMeshProUGUI m_GetCheckTimeTMPTxt, m_GetCheckPartTimeTMPTxt;
    [SerializeField] private float m_AddTime = 5f;
    private bool m_isOn = false;
    private bool m_isStartProcess = false;


    private void Start()
    {
        m_isStartProcess = true;
    }


    #region 유니티 이벤트 함수
    private void Update()
    {
        //if (Input.GetKeyDown(KeyCode.P))
        //{
        //    m_GetCheckTime.Initlization();
        //}

        //if (Input.GetKeyDown(KeyCode.N))
        //{
        //    m_GetCheckTime.BreakPoint(m_isOn);
        //    m_isOn = !m_isOn;
        //}

        if (Input.GetKeyDown(KeyCode.Space))
        {
            //들어가는 숫자를 조합하여 
            m_isStartProcess = false;
            //m_GetCheckTime.UpdateTimerByTweening(m_AddTime, _ApplyCallBack => 
            //{
            //    int MainValue = Mathf.FloorToInt(m_GetCheckTime.pp_CurrGameOverTime);
            //    int PartValue = Mathf.FloorToInt((m_GetCheckTime.pp_CurrGameOverTime - MainValue) * 100);
            //    m_GetCheckPartTimeTMPTxt.DOTmpText(PartValue.ToString("D2"), 0.25f, true, ScrambleMode.Numerals).SetEase(Ease.OutSine);
            //    m_GetCheckTimeTMPTxt.DOTmpText(MainValue.ToString(), 0.5f, true, ScrambleMode.Numerals).SetEase(Ease.OutSine).
            //    OnComplete(() => 
            //    {
            //        _ApplyCallBack?.Invoke();
            //        m_isStartProcess = true;
            //    });
            //});

        }

        //if (m_isStartProcess && m_GetCheckTime.pp_isSelfProcessUpdate)
        //{
        //    int MainValue = Mathf.FloorToInt(m_GetCheckTime.pp_CurrGameOverTime);
        //    int PartValue = Mathf.FloorToInt((m_GetCheckTime.pp_CurrGameOverTime - MainValue) * 100);
        //    m_GetCheckTimeTMPTxt.text = MainValue.ToString();
        //    m_GetCheckPartTimeTMPTxt.text = PartValue.ToString("D2");
        //}
        
    }
    #endregion

    #region Tycon까지의 테스트 로직들
    //[SerializeField] private TestDefultClass m_TestDefultClass;

    ////[SerializeField] private Sprite[] m_ApplySprArray;
    ////[SerializeField] private string[] m_ApplyStrArray;
    ////bool isActionFirst = false;
    ////[SerializeField] private RectTransform m_TestRT;

    //#region FieldInfo �� ���� �׽�Ʈ ����
    ////[System.Serializable]
    ////public class TestInfo
    ////{
    ////    private Sprite[] s_GetSprArray;
    ////    private string[] s_GetStrArray;
    ////    private int s_GetDefultValue;
    ////    public int s_GetDefultValue_2;
    ////    public TestInfo(string[] _GetSPR)
    ////    {
    ////        s_GetStrArray = _GetSPR;
    ////    }
    ////}

    ////private void TestByFieldInfos()
    ////{
    ////    TestInfo GetInfo = new TestInfo(m_ApplyStrArray);
    ////    #region Reflection ���� �׽�Ʈ
    ////    //System.Reflection.FieldInfo[] GetInfos = GetInfo.GetType().GetFields(System.Reflection.BindingFlags.Public |
    ////    //System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Static);

    ////    //GetInfos.ToList().ForEach(x =>
    ////    //{
    ////    //    Debug.LogFormat("{0}:::{1}", x.Name, x.GetValue(GetInfo));
    ////    //});
    ////    #endregion
    ////    if (Helper.GetFieldNameToClass(GetInfo, "s_GetStrArray", true, out System.Reflection.FieldInfo _GetOutField))
    ////    {
    ////        Debug.LogFormat("{0}", _GetOutField.Name);
    ////        if (!_GetOutField.FieldType.IsArray) Debug.LogFormat("{0}", _GetOutField.GetValue(GetInfo).ToString());
    ////        else ((System.Array)_GetOutField.GetValue(GetInfo)).Cast<object>().ToList().ForEach(x => Debug.Log(x));
    ////    }
    ////}
    //#endregion

    //private bool isSettingDataLoad = false; //�ش� �κ� SettingPopUp<Ŭ����> �����丵 ����� �ݵ�� �����Ұ�
    //private void TestBySettingData(SettingJsonType _GetType)
    //{
    //    if((_GetType == SettingJsonType.AllReset || _GetType == SettingJsonType.Save) && !isSettingDataLoad)
    //    {
    //        Debug.LogWarning("������ �ε� ���Ŀ� ������ �̷������ �մϴ�..!!");
    //        return;
    //    }
    //    if (_GetType == SettingJsonType.AllReset) Appinstance.Instance.ms_SettingManager.AllResetJsonData();
    //    else
    //    {
    //        Appinstance.Instance.ms_SettingManager.LocalSettingJsonData(_GetType);
    //        Appinstance.Instance.ms_SettingManager.AllSettingValueApplyInGame();
    //    }
    //    if (_GetType == SettingJsonType.Load) isSettingDataLoad = true;
    //    Debug.LogFormat("�ε��� ������ ���� 1 [{0}]",
    //    Appinstance.Instance.ms_SettingManager.ReciveSettingInfo<SettingCatecoryGamePlayInfo>(true).s_TranslateStr);
    //}
    //[SerializeField] private Text_Translate s_GetSubTxt;
    //[SerializeField] private int m_ApplyShaderInfoIDX;

    //[SerializeField] private GameObject m_MoveObj;
    //[SerializeField] private Transform m_StartTr, m_EndTr;

    //private void TableElemPartParoblicTweening(GameObject _MoveObj, Transform _StartTr, Transform _EndTr)
    //{
    //    _MoveObj.transform.position = _StartTr.position;
    //    float GetDistance = Vector3.Distance(_StartTr.position, _EndTr.position);
    //    Vector3 DiagonalUpPoint = _MoveObj.transform.position + (Vector3.right + Vector3.up) * (GetDistance * 0.5f);
    //    _MoveObj.transform.DOPath(new Vector3[3] { _StartTr.position, DiagonalUpPoint, _EndTr.position }, 0.5f, PathType.CatmullRom).SetEase(Ease.InOutSine);
    //}

    //#region Table Sub Module Controller Reference

    //[SerializeField] Transform m_MainTableTr;
    //[SerializeField, ReadOnly] Test_TableElemModule[] m_NewTable;
    //private bool test_isInitTweening = false;
    //private bool m_isActionProcess = false;


    //public void Test_InitTableController(MFLGameLevelInfo _GetLevelInfo)
    //{
    //    //MFLRespownSubController Process
    //    List<MFLGameItemInfo> GetAllItemInfo = new();
    //    GetAllItemInfo.AddRange(_GetLevelInfo.oro_ApplyOutCardItems);
    //    GetAllItemInfo.AddRange(_GetLevelInfo.oro_ApplyNotOutItems);
    //    GetAllItemInfo.ForEach(x =>
    //    {
    //        GameObject GetObj = Instantiate(x.s_PrefabObjs,
    //        new Vector2(Random.Range(-1f, 1f), Random.Range(-2f, 2f)) * 1.5f, Quaternion.Euler(Random.insideUnitSphere * 2.5f));
    //        GetObj.transform.position += Vector3.forward * 5f;
    //        GetObj.CheckComnectComponent<MFLItemModule>().Initlization(x);
    //        Destroy(GetObj.GetComponent<Rigidbody>());
    //    });

    //    //MFLTableSubController Process
    //    Test_TableElemModule[] GetModules = m_MainTableTr.ChildLinearStuctureSearch<Test_TableElemModule>();
    //    m_NewTable = new Test_TableElemModule[GetModules.Length];
    //    System.Action TestForceCallBack = () => test_isInitTweening = true;
    //    float DelyTime = 0f;
    //    for (int CountIDX = 0; CountIDX < m_NewTable.Length; CountIDX++)
    //    {
    //        if (m_NewTable[CountIDX] == null) m_NewTable[CountIDX] = GetModules[CountIDX];
    //        bool isLast = CountIDX >= m_NewTable.Length - 1;
    //        m_NewTable[CountIDX].Initlization(null, null, 0.32f, isLast, DelyTime, isLast ? TestForceCallBack : null);
    //        DelyTime += 0.085f;
    //    }
    //    StartCoroutine("Test_CO_WaitInitTweening");

    //}

    //IEnumerator Test_CO_WaitInitTweening()
    //{
    //    yield return new WaitUntil(() => test_isInitTweening);
    //    m_isActionProcess = true;
    //    //���Ŀ� Controller�� ��Ȱ
    //    //1.MFLController�� ���� Ŭ���� �������� ������ �߰� �ϵ� �˻縸 ����
    //    //2.�� Elem������� ���� �� � ����ó���� ���� ������
    //}

    //#region Main System Public Functions


    //public void SendTableCalculateAction(MFLItemModule _CalculateModule)
    //{
    //    //Check Condition
    //    if (!m_isActionProcess) return;
    //    if (CheckApplyItemToTableElem(_CalculateModule, () =>
    //    {
    //        Collider[] GetColls = _CalculateModule.GetComponentsInChildren<Collider>();
    //        if (GetColls != null && GetColls.Length > 0) GetColls.HForEach(x => Destroy(x));
    //        if (_CalculateModule.TryGetComponent<Rigidbody>(out Rigidbody _GetOutRB)) Destroy(_GetOutRB);
    //        _CalculateModule.tag = "Untagged"; _CalculateModule.transform.GetChild(0).gameObject.tag = "Untagged";
    //    }).HDebug("�ܻ��� ���� á���ϴ�.!", Helper.HDType.Warning)) return;
    //}
    //#endregion

    //#region Sub System Private Functions

    ////�˻� ������ ���ο� �� �߰��ϱ�
    ////�ƴ� �ش� ����� �����Ҷ� m_Table���� ���� �����ϸ� ���ݾ�
    ////�� ���Ŀ� ���� �������� �����Ѵٸ� �����ϴ� �ǳ��� ��ġ�ϰ�
    ////���ٸ� �� ���ڸ��� �߰�
    ////�������� m_NewTable�� Listen�� �������ν� ��ü�� ������ ���� �� �ֵ��� �Ѵ� 
    //private bool CheckApplyItemToTableElem(MFLItemModule _ApplyItemModule, System.Action _ComplateCallBack)
    //{
    //    Test_TableElemModule GetLastElemInfo = m_NewTable[m_NewTable.Length - 1];
    //    bool isNotComplate = GetLastElemInfo.pp_isLast && GetLastElemInfo.pp_ListenModule != null;
    //    if (isNotComplate) return true;

    //    _ComplateCallBack?.Invoke();
    //    int FIDX = -1; FIDX = m_NewTable.HFindIndex(x => x.pp_ListenModule == null);
    //    if (FIDX != -1)
    //    {
    //        m_NewTable[FIDX].TablePushItem(Test_TableElemModule.TableElemProductionType.ReciveElem, _ApplyItemModule);
    //        return FIDX == -1;
    //    }

    //    FIDX = m_NewTable.ToList().FindLastIndex(x => x.pp_ListenModule.pp_CompareIDX == _ApplyItemModule.pp_CompareIDX);
    //    int ApplyIDX = FIDX + 1;
    //    m_NewTable[ApplyIDX].TablePushItem(Test_TableElemModule.TableElemProductionType.ReciveElem, _ApplyItemModule);
    //    for (int i = ApplyIDX; i < m_NewTable.Length; i++) //Helper�� ���� ��� (���� ���������� �ε����� �ѱ� �� �ֵ���
    //    {
    //        if (m_NewTable[i].pp_ListenModule != null) //��ġ�� �Ϸ�Ǿ� �������� �Բ� null�� �Ǳ� ������
    //            m_NewTable[i + 1].TablePushItem(Test_TableElemModule.TableElemProductionType.CarrySide, m_NewTable[i].pp_ListenModule, m_NewTable[i + 2]);
    //    }
    //    return FIDX == -1;
    //}

    //#endregion

    //#region Update Process => MFLController Reference

    //private GameObject m_ListenRayTargetObj;
    //[SerializeField] private GridLayoutGroup m_GetGridLayOut;

    //private void MouseRayTarget(System.Action<RaycastHit> _ComplateCallBack)
    //{
    //    Ray GetNewRay = Camera.main.ScreenPointToRay(Input.mousePosition);
    //    if (Physics.Raycast(GetNewRay, out RaycastHit _GetHit)) _ComplateCallBack?.Invoke(_GetHit);
    //}

    //public void Test_UpdateProcess()
    //{
    //    if (!m_isActionProcess) return;
    //    //GUI Ŭ�� ���� ���� �����ؾߵ�

    //    if (UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
    //        return;

    //    //Ŭ���� �������� ����ؾߵȴ� ************
    //    if (Input.GetMouseButton(0))
    //    {
    //        MouseRayTarget(_GetHit =>
    //        {
    //            if (m_ListenRayTargetObj != null && !m_ListenRayTargetObj.Equals(_GetHit.collider.gameObject))
    //            {
    //                m_ListenRayTargetObj.layer = LayerMask.NameToLayer("RayTarget");
    //                m_ListenRayTargetObj = null;
    //            }
    //            bool isComplate = _GetHit.collider.gameObject.tag == "MFL_DefultItem";
    //            m_ListenRayTargetObj = isComplate ? _GetHit.collider.gameObject : null;
    //            if (m_ListenRayTargetObj != null) m_ListenRayTargetObj.layer = LayerMask.NameToLayer(isComplate ? "OutLiner" : "RayTarget");
    //        });

    //    }
    //    if (Input.GetMouseButtonDown(0))
    //    {
    //        MouseRayTarget(_GetHit =>
    //        {
    //            bool isComplate = _GetHit.collider.gameObject.tag == "MFL_DefultItem";
    //            m_ListenRayTargetObj = isComplate ? _GetHit.collider.gameObject : null;
    //            if (m_ListenRayTargetObj != null) m_ListenRayTargetObj.layer = LayerMask.NameToLayer(isComplate ? "OutLiner" : "RayTarget");
    //        });
    //    }
    //    if (Input.GetMouseButtonUp(0))
    //    {
    //        if (m_ListenRayTargetObj != null)
    //        {
    //            if (m_ListenRayTargetObj.layer == LayerMask.NameToLayer("OutLiner") &&
    //            m_ListenRayTargetObj.transform.parent.TryGetComponent<MFLItemModule>(out MFLItemModule _GetOutModule))
    //            {
    //                Debug.LogWarning("�ܻ����� �̵�.!");
    //                //GetSubModule<MFLTablesSubModuleController>().SendTableCalculateAction(_GetOutModule);
    //                SendTableCalculateAction(_GetOutModule);
    //            }
    //            m_ListenRayTargetObj.layer = LayerMask.NameToLayer("RayTarget");
    //            m_ListenRayTargetObj = null;
    //        }
    //    }

    //}

    //#endregion

    //#endregion

    //#region Camera Rect Boundary Test Referecne
    ////ȭ��� ���콺��  ������ ���� ī�޶� ���콺�� ��ġ�� �̵��ϴ� ����
    //[Header("CameraBoundary Reference")]
    //[SerializeField] private float edgeSize;
    //[SerializeField] private float moveSpeed;
    //[SerializeField] private Vector2 minBounds, maxBounds;

    //private void UpdateCameraMoveBoundary()
    //{
    //    Vector3 mousePosition = Input.mousePosition;
    //    Vector3 cameraPosition = transform.position;

    //    // ī�޶� �̵� ���� ���
    //    Vector3 moveDirection = Vector3.zero;

    //    moveDirection.x = mousePosition.x <= edgeSize ? -1 : mousePosition.x >= Screen.width - edgeSize ? 1 : 0; //�� / ���� ������
    //    moveDirection.z = mousePosition.y <= edgeSize ? -1 : mousePosition.y >= Screen.height - edgeSize ? 1 : 0;  //�� / �Ʒ� ������
    //    //if (mousePosition.x <= edgeSize)
    //    //{
    //    //    moveDirection.x = -1; // �������� �̵�
    //    //}
    //    //else if (mousePosition.x >= Screen.width - edgeSize)
    //    //{
    //    //    moveDirection.x = 1; // ���������� �̵�
    //    //}

    //    //if (mousePosition.y <= edgeSize)
    //    //{
    //    //    moveDirection.z = -1; // �Ʒ��� �̵�
    //    //}
    //    //else if (mousePosition.y >= Screen.height - edgeSize)
    //    //{
    //    //    moveDirection.z = 1; // ���� �̵�
    //    //}
    //    //������ ǥ�����۾� (ī�޶��� ������ ���Ұ��� �ƴ� ��ũ��������(ȭ���� Rect������ �����ؾߵ�)
    //    //Rect leftEdge = new Rect(0, 0, edgeSize, Screen.height);
    //    //Rect rightEdge = new Rect(Screen.width - edgeSize, 0, edgeSize, Screen.height);
    //    //Rect topEdge = new Rect(0, Screen.height - edgeSize, Screen.width, edgeSize);
    //    //Rect bottomEdge = new Rect(0, 0, Screen.width, edgeSize);
    //    //if (leftEdge.Contains(mousePosition))
    //    //{
    //    //    moveDirection.x = -1; // �������� �̵�
    //    //}
    //    //else if (rightEdge.Contains(mousePosition))
    //    //{
    //    //    moveDirection.x = 1; // ���������� �̵�
    //    //}

    //    // ī�޶� �̵�
    //    if (moveDirection != Vector3.zero)
    //    {
    //        cameraPosition += moveDirection.normalized * moveSpeed * Time.deltaTime;

    //        // �̵� ���� ����
    //        //cameraPosition.x = Mathf.Clamp(cameraPosition.x, minBounds.x, maxBounds.x);
    //        //cameraPosition.z = Mathf.Clamp(cameraPosition.z, minBounds.y, maxBounds.y);

    //        transform.position = cameraPosition;
    //    }
    //}

    //IEnumerator CO_WaitGridLayOutOff()
    //{
    //    var GetChildTrs = Helper.ChildLinearStuctureSearch(m_GetGridLayOut.transform);
    //    GetChildTrs.HForEach(x =>
    //    {
    //        x.gameObject.SetActive(true);
    //        Debug.Log($"���̾ƿ� ������ : {(x.transform as RectTransform).anchoredPosition}");
    //    });
    //    yield return new WaitForSeconds(0f);
    //    m_GetGridLayOut.enabled = false;
    //    GetChildTrs.HForEach(x =>
    //    Debug.Log($"���̾ƿ� ���İ� : {(x.transform as RectTransform).anchoredPosition}"));
    //}

    //#endregion

    //#region Character OutPut Test Reference

    ////private void TemporyMyCharacterOutPutReference()
    ////{
    ////    int GetIDX = -1;
    ////    var GetSManager = Appinstance.Instance.ms_ScriptableObjectManager;
    ////    var GetMyInfo = Appinstance.Instance.ms_MyInfo as CharacterMyInfo;
    ////    var GetCharacterInfo = GetSManager.SDataParsingByItemIDX<SDataCharacterInfo, CharacterItemInfo>(100);
    ////    var GetCController = GetMyInfo.ApplyModulesByInfo(GetCharacterInfo).HFind(x => x is NewCharacterController);
    ////    var GetActionInfo = GetMyInfo.spp_SavedUserInfo.s_NewActionInfo.Value;
    ////    var GetControlParams = GetActionInfo.FindRequiredParamsInfo<InputDataInfo>(ref GetIDX);
    ////    GetControlParams.s_PowerType = PowerType.MousePointTargetPower;
    ////    GetActionInfo.s_RequiredSubModuleParams[GetIDX] = GetControlParams;
    ////    UserInfo GetNewInfo = new UserInfo(string.Empty, GetActionInfo);
    ////    GetCController.Initlization(GetNewInfo);
    ////}

    //#endregion

    //#region Test Live Update NavMesh Surface (**Test 사용 안함 참고용 이후 삭제요망**)

    //public Transform target; // 목표 지점
    //public NavMeshSurface navMeshSurface; // 네비메시 Surface
    //public float speed = 3.5f; // 이동 속도
    //public float stoppingDistance = 0.5f; // 멈춤 거리

    //private NavMeshPath navPath; // 계산된 경로
    //private int currentCornerIndex = 0; // 현재 이동 중인 코너 인덱스
    //private bool isPathValid = false;
    ////원래 적용하려고 한 변수들
    //private Vector2 s_ReturnTarget = Vector2.zero;
    //private Transform m_CharacterRB;

    //private Vector3 NavAgentPathMove(Vector3 directionToTarget)
    //{
    //    NavMeshPath CurrNPath = new NavMeshPath(); //현재 방향 0 다음 방향 1로써 최단 경로 탐색을 진행함
    //    if (NavMesh.CalculatePath(m_CharacterRB.position, s_ReturnTarget, NavMesh.AllAreas, CurrNPath))
    //    {
    //        if (CurrNPath.status == NavMeshPathStatus.PathComplete &&
    //        CurrNPath.corners.Length > 1) // 경로가 유효하다면, 다음 이동 방향 계산
    //        {
    //            Vector3 NextCorner = CurrNPath.corners[1]; // 현재 위치에서 다음 경로의 코너
    //            Vector3 MoveDirection = (NextCorner - m_CharacterRB.position).normalized;
    //            return MoveDirection * directionToTarget.magnitude; // 방향 * 원래 거리 크기
    //        }
    //    }
    //    return Vector3.zero; // 유효한 경로가 없을 경우, 정지 벡터 반환 directionToTarget; 
    //}

    //void CalculateUpdate()
    //{
    //    // 네비메시 실시간 갱신
    //    navMeshSurface.BuildNavMesh();

    //    // 경로 계산
    //    CalculatePathToTarget();

    //    // 경로를 따라 이동
    //    if (isPathValid)
    //    {
    //        FollowPath();
    //    }
    //}

    //void CalculatePathToTarget()
    //{
    //    // 목표 지점까지의 경로 계산
    //    bool isGenerateNavPath =
    //    UnityEngine.AI.NavMesh.CalculatePath(transform.position, target.position, UnityEngine.AI.NavMesh.AllAreas, navPath);
    //    isPathValid = isGenerateNavPath && navPath.status == NavMeshPathStatus.PathComplete;
    //    if(isGenerateNavPath) currentCornerIndex = 0; // 경로를 새로 계산하면 첫 번째 코너부터 시작
    //}

    //void FollowPath()
    //{
    //    if (currentCornerIndex < navPath.corners.Length)
    //    {
    //        Vector3 currentCorner = navPath.corners[currentCornerIndex];
    //        Vector3 direction = (currentCorner - transform.position).normalized;

    //        // 이동
    //        transform.position += direction * speed * Time.deltaTime;

    //        // 회전
    //        Quaternion targetRotation = Quaternion.LookRotation(direction);
    //        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 5f);

    //        // 현재 코너에 도달했는지 확인
    //        if (Vector3.Distance(transform.position, currentCorner) <= stoppingDistance)
    //        {
    //            currentCornerIndex++; // 다음 코너로 이동
    //        }
    //    }
    //}

    //#region 여러개의 네비메쉬가 존재했을때의 로직
    ////Unity.AI.Navigation.NavMeshSurface[] m_NavMeshSurfaces;

    ////void CalculateShortestPath()
    ////{
    ////    float shortestDistance = float.MaxValue;
    ////    Vector3[] bestPath = null;

    ////    foreach (var surface in m_NavMeshSurfaces)
    ////    {
    ////        UnityEngine.AI.NavMeshPath tempPath = new UnityEngine.AI.NavMeshPath();
    ////        if (UnityEngine.AI.NavMesh.CalculatePath(transform.position, target.position, UnityEngine.AI.NavMesh.AllAreas, tempPath))
    ////        {
    ////            // 유효한 경로가 있는 경우 경로 길이를 계산
    ////            float pathDistance = CalculatePathDistance(tempPath.corners);
    ////            if (pathDistance < shortestDistance)
    ////            {
    ////                shortestDistance = pathDistance;
    ////                bestPath = tempPath.corners;
    ////            }
    ////        }
    ////    }

    ////    // 최단 경로 설정
    ////    if (bestPath != null)
    ////    {
    ////        pathCorners = bestPath;
    ////        currentCornerIndex = 0;
    ////    }
    ////}

    ////float CalculatePathDistance(Vector3[] corners)
    ////{
    ////    float distance = 0f;
    ////    for (int i = 0; i < corners.Length - 1; i++)
    ////    {
    ////        distance += Vector3.Distance(corners[i], corners[i + 1]);
    ////    }
    ////    return distance;
    ////}

    ////void FollowPath()
    ////{
    ////    if (currentCornerIndex < pathCorners.Length)
    ////    {
    ////        Vector3 currentCorner = pathCorners[currentCornerIndex];
    ////        Vector3 direction = (currentCorner - transform.position).normalized;

    ////        // 이동
    ////        transform.position += direction * speed * Time.deltaTime;

    ////        // 회전
    ////        Quaternion targetRotation = Quaternion.LookRotation(direction);
    ////        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 5f);

    ////        // 현재 코너에 도달했는지 확인
    ////        if (Vector3.Distance(transform.position, currentCorner) <= stoppingDistance)
    ////        {
    ////            currentCornerIndex++; // 다음 코너로 이동
    ////        }
    ////    }
    ////}
    //#endregion

    //#endregion

    //#region Reat Test NavMesh Prefabs

    //[SerializeField] private GameObject m_GetNavPrefabs;
    //private void InstanceNavMeshPrefabs()
    //{
    //    var GetBlocks = Instantiate(m_GetNavPrefabs);
    //    GetBlocks.SetActive(true);
    //    GetBlocks.transform.position = m_GetNavPrefabs.transform.position;
    //    GetBlocks.GetComponentsInChildren<NavMeshSurface>().HForEach(x =>
    //    { if (x.navMeshData == null) x.BuildNavMesh(); });
    //}

    //#endregion

    //#region 유니티 이벤트 함수

    //void Start()
    //{
    //}

    //void Update()
    //{
    //    //Test_UpdateProcess();

    //    if (Input.GetKeyDown(KeyCode.N))
    //    {
    //        InstanceNavMeshPrefabs();
    //    }

    //    if (Input.GetKeyDown(KeyCode.Space))
    //    {
    //        var GetChildTrs = Helper.ChildLinearStuctureSearch(m_GetGridLayOut.transform);
    //        GetChildTrs.HForEach(x =>
    //        {
    //            x.gameObject.SetActive(true);
    //            Debug.Log($"���̾ƿ� ������ : {(x.transform as RectTransform).anchoredPosition}");
    //        });
    //        m_GetGridLayOut.enabled = false;
    //        GetChildTrs.HForEach(x =>
    //        Debug.Log($"���̾ƿ� ���İ� : {(x.transform as RectTransform).anchoredPosition}"));
    //        //StartCoroutine("CO_WaitGridLayOutOff");
    //    }


    //    if (Input.GetKeyDown(KeyCode.M)) 
    //    {
    //        //TableElemPartParoblicTweening(m_MoveObj, m_StartTr, m_EndTr);
    //        var GetEndTr = m_EndTr.GetComponent<TableElemSubModuleController>();
    //        //var GetStartTr = m_StartTr.GetComponent<TableElemSubModuleController>();
    //        //GetStartTr.TablePushItem(TableElemSubModuleController.TableElemProductionType.ReciveElem, m_MoveObj.GetComponent<MFLItemModule>());
    //        m_MoveObj.transform.position = m_StartTr.transform.position;
    //        //GetEndTr.pp_ListenModule = m_MoveObj.GetComponent<MFLItemModule>();
    //        //GetEndTr.pp_ItemPivotTr = GetEndTr.transform;
    //        //GetEndTr.ActionAfterChangeState(TableElemSubModuleController.TableElemProductionType.CarrySide, GetEndTr);
    //    }

    //    if (Input.GetMouseButton(0))
    //    {
    //        UpdateCameraMoveBoundary();
    //    }

    //    #region ������ ��� �׽�Ʈ ȯ���� TestManager<Ŭ����>�� �����ߴ�
    //    //if (Input.GetKeyDown(KeyCode.L))
    //    //{
    //    //    //TestByFieldInfos();
    //    //    //TestBySettingData(SettingJsonType.Load);
    //    //    MyInfo GetInfo = new MyInfo(Appinstance.Instance, "");
    //    //    GetInfo.AllMyInfoLoadAndApplyAnyncParsing(() =>
    //    //    Appinstance.Instance.ms_LoadingManager.DELManagerUpdateGCCollect(true, 1), out IEnumerator[] _GetCourtines);
    //    //    _GetCourtines.ToList().ForEach(x => StartCoroutine(x));
    //    //}

    //    //if (Input.GetKeyDown(KeyCode.S))
    //    //{
    //    //    //TestBySettingData(SettingJsonType.Save);
    //    //    Appinstance.Instance.ms_GamePlayManager.StartChangeSceneProcess(
    //    //    MainLoadingSystem.SetMainLoadingType.OutPutOnlyImgs, SetSceneType.TitleScene, 900);
    //    //}

    //    //if (Input.GetKeyDown(KeyCode.R))
    //    //{
    //    //    //TestBySettingData(SettingJsonType.AllReset);
    //    //    Appinstance.Instance.ms_GamePlayManager.ExecuteChangeAllGameControll(GamePlayManager.GamePlayType.RePlay);
    //    //}

    //    //if (Input.GetKeyDown(KeyCode.V))
    //    //{
    //    //    System.Globalization.CultureInfo GetInfo = System.Globalization.CultureInfo.CurrentCulture;
    //    //    System.Globalization.CultureInfo[] GetInfos =
    //    //    System.Globalization.CultureInfo.GetCultures(System.Globalization.CultureTypes.AllCultures);
    //    //    string GetStrs = string.Empty;
    //    //    GetInfos.ToList().ForEach(x => GetStrs += x + "\n");
    //    //    Debug.LogFormat("���� ���� {0}", GetInfo.Name);
    //    //    Debug.LogFormat("�� ����� \n {0}", GetStrs);
    //    //}

    //    //if (Input.GetKeyDown(KeyCode.A))
    //    //{
    //    //    //ms_DelEventManager.DELUpdateGCCollectByAction(true, 1){
    //    //    Appinstance.Instance.ms_ScriptableObjectManager.SetApplyScriptableObject(() =>
    //    //    Appinstance.Instance.ms_LoadingManager.DELManagerUpdateGCCollect(true, 1));
    //    //    //s_GetSubTxt.gameObject.SetActive(true);
    //    //    #region �迭�� ���� ������ Test
    //    //    //string[][] GetAction_1 = new string[5][]
    //    //    //{
    //    //    //    //0,0\1,0\2,0
    //    //    //    //0,1\1,1\2,1
    //    //    //    //0,2\1,2\2,2
    //    //    //    //5X4
    //    //    //    new string[] { "�ε���", "ASD", "BSD" ,"CSD" },
    //    //    //    new string[] { "100", "A", "B" ,"C" },
    //    //    //    new string[] { "200", "D", "E" ,"F" },
    //    //    //    new string[] { "300", "G", "H" ,"I" },
    //    //    //    new string[] { "400", "J", "K" ,"L" }
    //    //    //};

    //    //    ////�̻��¿��� Ű���� ���� �� �ִ� ����� ã�Ƽ� ����
    //    //    //for (int i = 1; i < GetAction_1[0].Length; i++)
    //    //    //{
    //    //    //    for (int j = 1; j < GetAction_1.Length; j++)
    //    //    //    {
    //    //    //        Debug.LogFormat("{0}::{1}", GetAction_1[j][0], GetAction_1[j][i]);
    //    //    //    }
    //    //    //}
    //    //    ////for (int i = 1; i < GetAction_1.Length; i++)
    //    //    ////{
    //    //    ////    Debug.LogFormat(GetAction_1[i][0]);
    //    //    ////}
    //    //    ////for (int i = 1; i < GetAction_1[0].Length; i++)
    //    //    ////{
    //    //    ////    Debug.LogFormat(GetAction_1[0][i]);
    //    //    //    //for (int iq = 1; iq < GetAction_1.Length; iq++)
    //    //    //    //        Debug.LogFormat(GetAction_1[iq][0]);
    //    //    //    //Debug.LogFormat("{0},{1} ::: {2}", iq, i, GetAction_1[iq][i]);
    //    //    //    //for (int iq = 1; iq < GetAction_1[i].Length; iq++)
    //    //    //    //    Debug.Log(GetAction_1[0][iq]);
    //    //    //    //for (int j = 0; j < GetAction_1[i].Length; j++)
    //    //    ////}
    //    //    #endregion
    //    //}
    //    //if (Input.GetKeyDown(KeyCode.J))
    //    //{
    //    //    #region ���ε����� ���� ���� Ư�� ������Ʈ �Ľ� ȣ�� ����
    //    //    //MapInfo GetInfo =
    //    //    //Appinstance.Instance.ms_ScriptableObjectManager.SDataParsingByItemIDX<SDataMapInfo, MapInfo>(901);
    //    //    //RenderSettings.sun = GetInfo.s_MainDirectionalLight.
    //    //    //gameObject.NewObjAndMakeSyncPosRot(null, Helper.SyncType.NotApplyRot).GetComponent<Light>();
    //    //    #endregion

    //    //    Appinstance.Instance.ms_GamePlayManager.StartChangeSceneProcess(
    //    //    MainLoadingSystem.SetMainLoadingType.OutPutOnlyImgs, SetSceneType.InGameScene, 902);
    //    //    //MainLoadingSystem.SetMainLoadingType.OutPutEffect, SetSceneType.InGameScene, 900);

    //    //    #region UGUI �ǹ� ����� X Y ��ġ ������ ���� ���� (Helper�� �����Ͽ� ����ߴ�)
    //    //    //Debug.Log(Helper.ExtractContent("IDX900_SubICON_1_(0.51%0.6)"));
    //    //    //if (!isActionFirst)
    //    //    //{
    //    //    //    float[] GetNumbers =
    //    //    //    Helper.ExtractContentByTempArray<float>(m_TestRT.GetComponent<Image>().sprite.name, '%');
    //    //    //    Vector3 Oldposition = m_TestRT.localPosition;
    //    //    //    Vector2 OldSIze = m_TestRT.rect.size;
    //    //    //    Vector2 DeltaPivot = new Vector2(GetNumbers[0], GetNumbers[1]) - m_TestRT.pivot;
    //    //    //    Vector3 DeltaPosition = new Vector3(DeltaPivot.x * OldSIze.x, DeltaPivot.y * OldSIze.y, 0f);
    //    //    //    m_TestRT.pivot = new Vector2(GetNumbers[0], GetNumbers[1]);
    //    //    //    m_TestRT.localPosition = Oldposition + DeltaPosition;
    //    //    //    //Vector2 NewSIze = m_TestRT.rect.size;
    //    //    //    //1920 1080 - 1900.1 972
    //    //    //    isActionFirst = true;
    //    //    //}
    //    //    //else m_TestRT.DOScale(Vector2.one * 110f, 1.15f).SetEase(Ease.InCirc).OnComplete(() => isActionFirst = false);
    //    //    #endregion
    //    //}
    //    #endregion

    //    //if (Input.GetKeyDown(KeyCode.Alpha2))
    //    //{
    //    //    FindObjectsOfType<NewPlayerCharacterController>().ToList().Find(x => x.pp_isMine).ChangeState(PowerType.SelfInputPower);
    //    //}

    //    //if (Input.GetKeyDown(KeyCode.Alpha3))
    //    //{
    //    //    m_ApplyShaderInfo
    //    //}
    //}

    ////private string ExtractContent(string _GetStr)
    ////{
    ////    string ReturnStr = string.Empty, Parttern = @"\((.*?)\)"; ;
    ////    System.Text.RegularExpressions.Match GetMath = System.Text.RegularExpressions.Regex.Match(_GetStr, Parttern);
    ////    return ReturnStr = GetMath.Success ? GetMath.Groups[1].Value : string.Empty;
    ////}

    //#endregion
    #endregion
}

