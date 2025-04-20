using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Commons;
using Commons.Helpers;
using Commons.Helpers.ParsingTables;

public class ScriptableObjectManager : MonoBehaviour, I_CheckInit
{
    #region ScriptableObjectManager 관한 설명
    //1.기존 플렛폼별 스크립터블 데이터를 구분하던걸
    //통합할 수 있도록 한다
    //1-1.하지만 기존 테이블의 값들을 합쳐서 가져오는 방식은 그대로 진행한다
    //2.있는데이터를 중심으로 오버로딩하되 최대 3개 이상을 넘기지 않도록 한다
    //3.한 스크립터블 데이터에서 공집합 형태로써 안으로 파싱할 수 있는 이중 템플릿 
    //오버로딩 함수를 정의할 수 있도록 한다
    #endregion
    [Header("TestManager 혹은 GameManager에 이전요망")]
    private bool m_isLogErrorSData = false;
    [Header("Requried SDataManager Values")]
    [SerializeField] private ScriptableReciverInfo[] m_ScriptableCommons;
    private bool m_isInit = false, m_isDatasProcessActions = false;

    public void Initlization()
    {
        if (m_isInit) return;
        ScriptableCommon[] GetCommons = Resources.LoadAll<ScriptableCommon>("Datas/ScriptableData/");
        m_ScriptableCommons.ToList().ForEach(x => 
        {
            x.s_ScriptableBase = GetCommons.ToList().Find(y => x.s_SReciverType == y.ReciverGetType(x.s_SReciverType));
            x.s_ScriptableBase.DestroyElementByDB();
            x.s_ScriptableBase.Initialization(this);
        });
        m_isInit = true;
    }

    public bool ISInitlization() => m_isInit;

    #region Main System Public Functions

    public ItemInfo[] SDataParsingIDXArray<T>(int[] _GetIDXArray) where T : ScriptableCommon
    {
        T GetCommon = m_ScriptableCommons.ToList().Find(x => x.s_ScriptableBase is T) as T;
        List<ItemInfo> GetParsingInfos = new List<ItemInfo>();
        _GetIDXArray.ToList().ForEach(x =>
        {
            GetParsingInfos.Add(GetCommon.pp_DefultItemInfos.ToList().Find(y => x == y.s_ItemIDX));
            if (GetParsingInfos[GetParsingInfos.Count - 1] == null)
            Debug.LogErrorFormat("{0} 인덱스 값이 비어있던지 해당 스크립터블 데이터에 존재하지 않는 데이터 입니다..!!");
        });
        
        return GetParsingInfos.ToArray();
    }

    //전체 탐색후 찾는 법 (추상적으로 활용이 가능 데이터 많을수록 손해 불필요한 계산)
    public ItemInfo SDataParsingByItemIDX(int _GetIDX) 
    {
        int[] FindIDXs = new int[2];
        for(int i = 0; i < m_ScriptableCommons.Length; i++)
        {
            int FIDX = -1; FIDX =
            m_ScriptableCommons[i].s_ScriptableBase.pp_DefultItemInfos.ToList().FindIndex(x => x.s_ItemIDX == _GetIDX);
            if (FIDX != -1) { FindIDXs[0] = i; FindIDXs[1] = FIDX; break; }
        }
        return m_ScriptableCommons[FindIDXs[0]].s_ScriptableBase.pp_DefultItemInfos[FindIDXs[1]];
    }

    //템플릿으로 찾는 방법 (추상적으로 활용이 불가 데이터가 많을 수록 덜 손해)
    public U SDataParsingByItemIDX<T, U>(int _GetIDX) where T : ScriptableCommon where U : ItemInfo
    {
        T GetCommon = m_ScriptableCommons.ToList().Find(x => x.s_ScriptableBase is T).s_ScriptableBase as T;
        ItemInfo ReturnItem = GetCommon.pp_DefultItemInfos.ToList().Find(x => x.s_ItemIDX == _GetIDX);
        if (!(ReturnItem is U))
        {
            Debug.LogErrorFormat("{0}인덱스의 아이템은 타입 {1}이 아닙니다..!!", _GetIDX, typeof(U).Name);
            return null;
        }
        return ReturnItem as U;
    }

    public U[] SDatasParsingByType<T, U>() where T : ScriptableCommon where U : ItemInfo
    {
        T GetCommon = m_ScriptableCommons.ToList().Find(x => x.s_ScriptableBase is T).s_ScriptableBase as T;
        return GetCommon.pp_DefultItemInfos.FindAll(x => x is U).OfType<U>().ToArray();
    }

    public U[] SDatasParsingByItemElem<T, U, S>(S _ElemType) where T : ScriptableCommon where U : ItemInfo
    {
        //T GetCommon = m_ScriptableCommons.ToList().Find(x => x.s_ScriptableBase is T).s_ScriptableBase as T;
        //U[] GetAllParsingByTemp = GetCommon.pp_DefultItemInfos.FindAll(x => x is U).OfType<U>().ToArray();
        U[] GetAllParsingByTemp = SDatasParsingByType<T, U>();

        if ((GetAllParsingByTemp[0] is not I_CompareVarByItemInfo GetItemInfo).HDebug(
        $"오직 {nameof(I_CompareVarByItemInfo)}을 다중상속한 {nameof(ItemInfo)} 만 사용이 가능합니다.!", Helper.HDType.Warning)) return null;
        return GetAllParsingByTemp.ToList().FindAll(x => (x as I_CompareVarByItemInfo).I_SetCompare<S>(_ElemType)).ToArray();
    }

    #endregion

    #region Main Parsing Public Functions (Apply DB)

    public void SetApplyScriptableObject(System.Action _EndCallBack = null)
    {
        #region 파싱할 서버정보에 관한 설명
        //서버에서 탐색하는게 (텍스트) 내정보 + 아이템 정보 + (에셋) 번들에셋
        //이 되며 그중에 스크립터블 데이터중 Local 타입은 Table정보로
        //서버는 아이템 정보로 하며
        //오브젝트 또한 번들로 할지 Resources로 할지 타입이 분리되어 있다
        //따라서 해당 구간에 파싱할 서버정보는 텍스트로 되어있는 아이템정보가 되겠다
        #endregion
        //(파싱 + (내정보에 관한 제이슨 정보 파싱 + 캐시 로컬로 부터 번들 파싱))
        //1.서버로드 (서버에 저장되어있는 내정보 (제이슨형태) + 각 아이템 정보 (제이슨형태) + 각 Object형태의 번들 로드(캐싱이 없다면)
        //2.번들 비동기 로드
        StartCoroutine(GetAsyncApplyData(_EndCallBack)); //3.TextAaset형태로 존재하는 로컬 DB 파싱

    }

    #region 이전로직에서 대게 서버 DB에서 파싱할때 사용될껄로 예상하여 정의한 것 (DB서버 구축이후 리팩토링뒤에 삭제요망..)    
    //public void SetChangeReciverJsonStrData(ScriptabeReciverType _ChangeType, string[] _getStr)
    //{
    //    if (isComplateAllTableData) isComplateAllTableData = false;
    //    if (_ChangeType == ScriptabeReciverType.None)
    //    {
    //        Debug.LogErrorFormat("전환하고자 하는 타입이 명시되어야 합니다 {0}", _ChangeType);
    //        return;
    //    }

    //    ScriptableReciverInfo GetReciver =
    //    m_SReciverInfoAll[System.Array.FindIndex(m_SReciverInfoAll, x => x.s_SReciverType == _ChangeType)];
    //    //후에 제이슨에서 가져올때 최대한 로컬 DB테이블과 비슷하게 가져와야되기 때문에
    //    //List<string>형으로 아이템 데이터를 가져와야 된다(엑셀 테이블 + TableManager<클래스>참조
    //    StartCoroutine(GetAsyncChangeData(GetReciver, _getStr));
    //}
    #endregion

    IEnumerator GetAsyncApplyData(System.Action _EndCallBack = null)
    {
        foreach(ScriptableReciverInfo y in m_ScriptableCommons)
        {
            CheckScriptableNonInstanceObj(y, true);
            string[] GetTableValue =  TableManager.AddSheetsByTable(y.s_ParsingTableTypeArray);
            yield return new WaitForSeconds(0.05f);
            foreach (string x in GetTableValue)
            {
                bool isComplateReciverInData = false;

                if (y.s_isParsingTempType)
                y.s_ScriptableBase.InPutDataByItem(x, ReturnTypeByCompareItemType(y.s_SReciverType), out isComplateReciverInData);
                else y.s_ScriptableBase.InPutDataByItem(x, out isComplateReciverInData);
                yield return new WaitUntil(() => isComplateReciverInData);
                yield return new WaitForSeconds(0.03f);
            }
            CheckScriptableNonInstanceObj(y, false);
            if (y.s_ScriptableBase is I_AdditionalAfterEvent GetEvent) 
            GetEvent.I_ScriptableCommonAfterEvent(y.s_SReciverType);
        }
        CheckScriptableKeyDepuil(); 
        _EndCallBack?.Invoke();
        m_isDatasProcessActions = true;
    }

    #endregion

    #region Search Scriptable Error System All Functions
    //애러는 모든 스크립터블 오브젝트의 변환과정이 끝날때를 원칙으로 한다
    //1.에러 첫번째는 한 모듈에 Sprite or WorldObj가 하나라도 없다면 전부 출력하고
    //2.나머지는 키값이 중복된게 있다면 출력하는 에러다
    //3.에러는 위에 나타난 에러의 종류로 나타내고 그 후에 어떤 종류의 연합 시트의 스크립터블에셋인지

    public void CheckScriptableKeyDepuil() //키값 중복 검사  (모든 에셋 검사)
    {
        //리스트 합치기 
        List<int> KeyErrorList = new List<int>();
        m_ScriptableCommons.ToList().ForEach(x => KeyErrorList.AddRange(x.s_ScriptableBase.CheckDuplicateKeyIDX(x.s_SReciverType)));

        //리스트 중복검사 
        HashSet<int> uniqueItems = new HashSet<int>();
        HashSet<int> duplicateItems = new HashSet<int>();
        foreach (int one in KeyErrorList) if (!uniqueItems.Add(one)) duplicateItems.Add(one);

        if (duplicateItems.Count > 0)
        {
            string OutPutAllStr = string.Empty;
            duplicateItems.ToList().ForEach(x => OutPutAllStr += x + "\n");
            Debug.LogErrorFormat("[스크립터블 키 중복 에러] \n {0}", OutPutAllStr);
        }
    }

    private void CheckScriptableNonInstanceObj(ScriptableReciverInfo _ChangeType, bool _isInit)
    {
        //인스턴싱 유무 검사 (각 에셋만 검사)
        if (_ChangeType.s_ScriptableBase.pp_CheckErrorStack.Count > 0)
        {
            if (_isInit) _ChangeType.s_ScriptableBase.pp_CheckErrorStack.Clear();
            else
            {
                string OutPutAllStr = string.Empty;
                _ChangeType.s_ScriptableBase.pp_CheckErrorStack.ForEach(x => OutPutAllStr += x + "\n");
                if (!m_isLogErrorSData) Debug.LogWarningFormat("[스크립터블 인스턴싱 에러] \n{0}", OutPutAllStr);
                else Debug.LogErrorFormat("[스크립터블 인스턴싱 에러] \n{0}", OutPutAllStr);
            }
        }
    }
    #endregion

    #region Sub System Private Functions

    private System.Type ReturnTypeByCompareItemType(ItemType _CompareType)
    {
        //ItemType GetType = Helper.StringToEnum<ItemType>(_CompareType);
        System.Type ReturnValue = _CompareType switch
        {
            ItemType.MapType => typeof(MapInfo),
            //ItemType.TYCGameContentType => typeof(TYCContentsInfo),
            //ItemType.TYCGameBlockType => typeof(TYCBlockConstructInfo),
            //ItemType.MFLGameItemType => typeof(MFLGameItemInfo),
            //ItemType.MFLGameLevelType => typeof(MFLGameLevelInfo),
            _ => null
        };
        if ((ReturnValue == null).HDebug($"{_CompareType}은 아직 비교 로직 조건에 로직을 적용하지 않았습니다.! " +
        $"로직을 업데이트 하십쇼.!", Helper.HDType.Error)) return null;
        return ReturnValue;
    }

    #endregion

    #region 유니티 이벤트 함수
    void Update()
    {
        if (!m_isDatasProcessActions) return;

        m_ScriptableCommons.ToList().ForEach(x => x.s_ScriptableBase.UpdateProcess());
    }

    void OnDestroy()
    {
        if (!m_isInit) return;
        m_ScriptableCommons.ToList().ForEach(x => x.s_ScriptableBase.DestroyElementByDB());
    }
    

    void OnApplicationQuit()
    {
        if (!m_isInit) return;
        m_ScriptableCommons.ToList().ForEach(x => x.s_ScriptableBase.DestroyElementByDB());
    }
    #endregion
}

[System.Serializable]
public class ScriptableReciverInfo
{
    public bool s_isParsingTempType;
    public ItemType s_SReciverType;
    public ParsingTableType[] s_ParsingTableTypeArray;
    public ScriptableCommon s_ScriptableBase;
}

public interface I_AddOtherSData
{
    public void I_ResetApplyItems();
    public void I_ChangedAllApplyItems();
}

public interface I_CompareVarByItemInfo
{
    public bool I_SetCompare<T>(T _ElemType);
}

//필요한 ScriptableCommon에 대입할 수 있도록
public interface I_AdditionalAfterEvent
{
    public void I_ScriptableCommonAfterEvent(ItemType _CompareType);

}

//사용을 지양하고 왠만하면 상속자를 통한 파싱으로 가져올 수 있도록 수정해보자
//(현재 사용안하고 이후에도 사용안할시 삭제요망)
public interface I_ParsingSubReferenceObject
{
    public T I_FindObjInfoByName<T>(string _GetName) where T : UnityEngine.Object;
}
