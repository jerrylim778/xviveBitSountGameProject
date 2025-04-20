using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Commons.Helpers.Attribute;

public class LoadingManager : MonoBehaviour
{
    
    #region NewLoadingManager의 전체 설명
    //1.가능하면 어떠한 구간에서든지 로딩이 필요한지점에
    //해당 기능을 호출할 때 기능이 돌아갈 수 있도록 설계
    //2.호출할때 매개변수에 Table만을 할당하여 Table에서
    //요구하는 각 클래스의 함수들을 문자열로 가져와 배치하는 형태로
    //만들 수 있도록
    //3.모든 로딩구간은 그 로딩이 끝났을때를 기점으로 병행처리가 가능하도록
    //돌리는 지점정하면(되로록 최소화)코루틴의 구간으로 처리할 수 있도록 설계
    #endregion

    #region 추가적으로 로딩에서 생각해야할것

    //1.Appinstance나 현재 작업하는곳 LoadingManager에 Test함수를 놔서
    //모든 과정을 해당 과정을 파싱했다 가정하는 함수를 만든다
    //이는 말그대로 가정이기 때문에 파싱 구간을 훼이크로 임하지만
    //다시말해 이 함수를 제외한 다른구간에는 절대 훼이크로 진행해서는 안된다

    //2.또한 랜덤을 지정해 TimeOut이나 error 출력을 유도하여 틀린 조건에 있을때 
    //또한 대비할 수 있도록 만들어보자

    #endregion

    #region 로딩의 조건 사용자의 정보 파싱에 대한 설명
    //SubTitle로 넘어가기전에 처리해야될 것들
    //1.(서버로 부터 나에 해당하는 ID값)
    //1-1. 서버는 FireBase와 NodeJS를 통한 웹서버 MySQL로 구축 그전까지 고정 IP 할당
    //2.(Text로 된 DB데이터를 토대로 TableManager에서 모든 정보를 파싱) => MyInfo에 보관
    //2-1.서버로부터 받은 Text데이터를 토대로 전부 인스턴싱화 하여 스크립터블 오브젝트에 저장
    //2-2.저장된 스크립터블 오브젝트는 GameManager나 MyInfo에서 가지고 있음
    //3.스크립터블 오브젝트를 토대로 TitleScene내에 인스턴싱을 해야할 구간에 배치와 인스턴싱을 진행
    #endregion

    [Header("LoadProcess")]
    [Lim_InspectorReadOnly, SerializeField] private int CurrentLoadIDX = 0;
    [Lim_InspectorReadOnly, SerializeField] private int MaxLoadIDX = 0;
    //[Lim_InspectorReadOnly, SerializeField] private List<string> LoadOutPutStrList = new List<string>();
    int BeforeLoadIDX = 0;
    public int pp_MaxLoadIDX { get { return MaxLoadIDX; } }

    [Header("MainLoadingPrograss")]
    [SerializeField] private MainLoadingSystem m_Prefab_MainLoadingSystem;

    [Tooltip("GC_Collect_Reference")]
    private bool m_isStartLoading, m_isWaitSub, m_isWaitActionSub = false;
    private System.Action m_ReturnUpdateLoadingProcess = null;

    [Tooltip("MainLoading Required Values")]
    private DelEventManager m_DelEventManager;
    private TranslateManager m_TranslateManager;
    #region Del Event By LoadingManager Reference
    //기본 로딩 업데이트에 관한 대리자
    public delegate void DELManager_UpdateProcess(int _UpdateCount);
    public event DELManager_UpdateProcess DELManager_Update_Process;
    //메니져의 GCCollect와 관련한 대리자
    public delegate void DELManager_UpdateGCCollect(bool _isComplate, int _UpdateProcessCount);
    public event DELManager_UpdateGCCollect DELManager_UpdateGC_Collect;
    public void DELManagerUpdateGCCollect(bool _isComplate, int _UpdateProcessCount)
    => DELManager_UpdateGC_Collect?.Invoke(_isComplate, _UpdateProcessCount);
    //대개 Loading을 구성하는 GUI에 활용하는 대리자
    public delegate void DELManager_ShowOtherTxt(string _ShowTxt); 
    public event DELManager_ShowOtherTxt DELManager_ShowOther_Txt;
    //대개 Loading이 끝날때 호출되는 로직
    public delegate void DELManager_LoadingComplate();
    public event DELManager_LoadingComplate DELManager_Loading_Complate;
    //오직 MainLoadingSystem에서만 활용하는 대리자
    public delegate void DELManager_UpdateMainLoadingPrograss(int _CurrentCount, int _MaxCount);
    public event DELManager_UpdateMainLoadingPrograss DELManager_UpdateMainLoading_Prograss;
    #endregion

    public void Initlization()
    {
        m_DelEventManager = Appinstance.Instance.ms_DelEventManager;
        m_TranslateManager = Appinstance.Instance.ms_TranslateManager;
    }


    #region Main System Public Functions

    public void StartLoadProcessDefult(float _WaitEndTime, Dictionary<int, System.Action> _GetActions)
    {
        if (m_isStartLoading) return;
        m_isStartLoading = true;
        DELManager_Update_Process += UpdateProcess;
        m_DelEventManager.DELPlayOrPauseProcess(false, true);

        //Loading Init
        CurrentLoadIDX = 0; BeforeLoadIDX = 0;
        
        MaxLoadIDX = _GetActions.Count;
        System.Tuple<string, System.Action>[] ApplyActions = new System.Tuple<string, System.Action>[_GetActions.Count];
        int CountIDX = 0; _GetActions.ToList().ForEach(x =>  { ApplyActions[CountIDX] = new System.Tuple<string, System.Action>(
        m_TranslateManager == null? string.Empty :  m_TranslateManager.ChangeTranslateLanguage(x.Key.ToString(), true), x.Value); CountIDX++; });
        StartCoroutine(CO_CallOrderControll(_WaitEndTime, ApplyActions));
        CurrentLoadIDX++;
    }

    public IEnumerator CO_GCCollectByLoadAfter(float _CollectWaitTime, bool _isUseGCCollect, System.Action _ReciveFuc)
    {
        //전에 돌아가고 있는 스택을 대기 (전에 돌아가는 스택또한 같은 코루틴을 돌기에 정지됨)
        yield return new WaitUntil(() => !m_isWaitSub); 
        m_isWaitSub = true; //순서 중요한 반드시 확인 바람
        DELManager_UpdateGC_Collect += SetComplateNextStep;
        m_isWaitActionSub = true; 
        _ReciveFuc.Invoke();
        yield return new WaitUntil(() => !m_isWaitActionSub);
        if(_isUseGCCollect) System.GC.Collect(); //가비지 콜렉션 발동 후 대기
        #region 대기 시간
        float SubTime = 0f;
        while (SubTime < _CollectWaitTime)
        {
            yield return null;
            SubTime += Time.unscaledDeltaTime * 3f;
        }
        #endregion
        yield return new WaitUntil(() => SubTime >= _CollectWaitTime);
        DELManager_UpdateGC_Collect -= SetComplateNextStep;
        m_ReturnUpdateLoadingProcess?.Invoke(); m_ReturnUpdateLoadingProcess = null;
        m_isWaitSub = false; m_isWaitActionSub = false;
    }

    //오직 비동기 작업에서만 호출하는것을 권장한다(비동기 씬로드 등등..)
    private void SetComplateNextStep(bool _isComplate, int _UpdateProcessCount)
    {
        //리팩토링이 되는지 점검
        m_ReturnUpdateLoadingProcess = () => UpdateProcess(_UpdateProcessCount);
        if (_isComplate) m_isWaitActionSub = false;
    }

    #region MainLoadingPrograss Reference

    public MainLoadingSystem SetLoadingPrograss(bool _isCallFilePath = false)
    {
        if (FindObjectOfType<MainLoadingSystem>() != null)
        {
            //Debug.LogError("메인로딩이 씬내에 존재하여 삭제하고 새로 생성합니다..!!");
            Debug.LogError("현재 메인로딩이 존재합니다 메인로딩은 한번에 한번만 처리가 가능합니다");
            Destroy(FindObjectOfType<MainLoadingSystem>().gameObject);
            return null;
        }

        MainLoadingSystem GetNewLoadingSystem = Instantiate(_isCallFilePath? Resources.Load<MainLoadingSystem>
        ("PopUpReference/" + nameof(MainLoadingSystem)) : m_Prefab_MainLoadingSystem, this.transform);
        if (GetNewLoadingSystem == null)
        {
            Debug.LogErrorFormat("경로에 해당되는 메인 로딩이 존재하지 않거나 프리팹이 존재하지 않습니다!! {0}",
            "PopUpReference/" + nameof(MainLoadingSystem));
            return null;
        }

        return GetNewLoadingSystem;
    }

    #endregion

    #endregion

    #region Mian System Private Function

    private void UpdateProcess(int _LoadCount) //후에 로딩의 계산에 의한 로딩증가를 노릴 수 있도록 설계한다
    {
        if (CurrentLoadIDX > MaxLoadIDX)
        {
            Debug.LogError("어디선가 로드를 두번처리했습니다 런타입 or 논리오류");
            return;
        }

        CurrentLoadIDX += _LoadCount;
        m_isWaitActionSub = false;
        DELManager_UpdateMainLoading_Prograss?.Invoke(CurrentLoadIDX, MaxLoadIDX);
    }

    IEnumerator CO_CallOrderControll(float _WaitTime, System.Tuple<string, System.Action>[] _CallActions) 
    {   
        int i = 0;
        while (i < _CallActions.Length)
        {
            yield return new WaitUntil(() => CurrentLoadIDX > BeforeLoadIDX);
            if (i == 0) CurrentLoadIDX -= 1;
            BeforeLoadIDX = CurrentLoadIDX;
            this.DELManager_ShowOther_Txt?.Invoke(_CallActions[i].Item1);
            _CallActions[i].Item2?.Invoke();
            i++;
        }
        yield return new WaitUntil(() => CurrentLoadIDX == MaxLoadIDX - 1);
        CurrentLoadIDX = 0; MaxLoadIDX = 0; BeforeLoadIDX = 0;
        //LoadOutPutStrList.Clear();
        this.DELManager_Update_Process -= UpdateProcess;
        m_DelEventManager.DELPlayOrPauseProcess(true, false);
        StartCoroutine(CO_WaitSectionThisLoad(_WaitTime));//시간도 따로 관리할 수 있도록 한다
    }

    IEnumerator CO_WaitSectionThisLoad(float _WaitTime)
    {
        yield return new WaitForSeconds(_WaitTime);
        DELManager_Loading_Complate?.Invoke();
        m_isStartLoading = false;
    }

    #endregion
}