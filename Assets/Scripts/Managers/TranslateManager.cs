using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Commons;
using Commons.Helpers;
using Commons.Helpers.Attribute;
using Commons.Helpers.ParsingTables;
using System.Globalization;


[DisallowMultipleComponent]
public class TranslateManager : MonoBehaviour, I_CheckInit
{
    public enum TranslateType { None = 0, KR, EG }

    [Header("Translate Values")]
    [Lim_InspectorReadOnly, SerializeField] private TranslateType m_TranslateType = TranslateType.None;
    [Tooltip("Translate Required(PreLoadTranslate)")]
    private string[][] m_GetTranslateAllStack = null;
    private System.Tuple<string, string>[] m_BeforHandTranslateNow = null;
    private readonly Dictionary<TranslateType, System.Tuple<string, string>[]> m_BeforHandTranslateStack = new();

    [Tooltip("Translate Sub Reference")]
    private bool m_isInit = false;
    public delegate void DEL_TranslateAllText();
    public event DEL_TranslateAllText DELManager_TranslateAll_Text;

    #region [리팩토링 중요 포인트] 전부 구현 이후에 주석 삭제 요망...
    //1.함수는 각 테이블에서 키값으로 타입을 값으로 키에 대한 번역본을 들고 있을 수 있도록 한다
    //2.해당 번역 컬렉션을 이용할 수 있는 경우의 수는 키값으로 찾는것뿐이다
    //3.나머지 각 나라별 언어끼리 연동할 수 있는건 우선 번역 컬렉션에 키값을 확인한 뒤에
    //해당 키값을 번역 행과 키 열을 기입하여 가져오는 방법으로 진행한다
    //4.그리고 해당 방식들이 시간복잡도가 가중될 수록 많은 양의 번역처리가 제대로 이루어지지
    //않기 때문에 번역할 순번의 크기를 나눠서 순차적으로 처리될 수 있도록 설계한다(많이 그래도 안된다면 비동기로 진행하자)
    //[초기화 설정]
    //번역의 설정값을 확인한 이후에 셋팅값을 가져와야함
    //그 전의 번역들은 전부 나라가 어딘지를 확인한 이후에 가져올 수 있도록
    //한국 ko-KR , 미국 en-US (참고로 모든 영어권 국가는 en-으로 시작됨), 일본 JP 중국 CN
    #endregion
    public void Initlization()
    {
        if (m_isInit) return;
        string GetCCName = CultureInfo.CurrentCulture.Name;
        if (GetCCName.Contains("-"))
        {
            if (GetCCName.Split('-')[0] == "en") m_TranslateType = TranslateType.EG;
            else if (GetCCName.Split('-')[1] == "KR") m_TranslateType = TranslateType.KR;
        }
        if (m_TranslateType == TranslateType.None) m_TranslateType = TranslateType.EG;
        GetBeforeHandCurrNationOfType(); //비동기로 이후에 실행할 수 있도록
        ChangeTraslate(m_TranslateType.ToString());
    }

    public bool ISInitlization() => m_isInit;

    #region Main System Private Functions

    #region 배열 순서가 햇갈릴때 참고
    //[세로][가로]
    //0,0 0,1 0,2
    //1,0 1,1 2,1
    //[1][j]타입이 됨
    //[i][0]키값이 됨
    //j가 먼저 늘어나고 다음 I가 늘어나는 순으로 입력해야됨
    //[메인 배열 길이][i]각 값은 다를 수 있음
    #endregion

    //해당 함수 비동기로 진행되야 작동될 수도 있다 리팩토링 요망....
    private void GetBeforeHandCurrNationOfType()
    {
        m_GetTranslateAllStack = TableManager.TableParsingAllRowCol(ParsingTableType.TranslationInfo);
        //m_BeforHandTranslateStack = new Dictionary<TranslateType, System.Tuple<string, string>[]>();
        string[] GetTranslateStrs = Helper.GetEnumByStringArray<TranslateType>(true);
        for (int i = 1; i < m_GetTranslateAllStack[0].Length; i++)
        {
            System.Tuple<string, string>[] SetNewTuple = new System.Tuple<string, string>[m_GetTranslateAllStack.Length - 1];
            if (GetTranslateStrs.ToList().TrueForAll(x => x != m_GetTranslateAllStack[0][i]))
            {
                Debug.LogErrorFormat("해당 번역은 타입에 존재하지 않습니다..!! " +
                "발견한 타입 [{0}] ", m_GetTranslateAllStack[0][i]);
                return;
            }
            for (int iq = 1; iq < m_GetTranslateAllStack.Length; iq++)
            SetNewTuple[iq - 1] = new System.Tuple<string, string>(m_GetTranslateAllStack[iq][0], m_GetTranslateAllStack[iq][i]);
            m_BeforHandTranslateStack.Add(Helper.StringToEnum<TranslateType>(m_GetTranslateAllStack[0][i]), SetNewTuple);
        }
    }
    #endregion

    #region Main System Public Functions

    public void ChangeTraslate(string _GetType)
    {
        m_TranslateType = Helper.StringToEnum<TranslateType>(!_GetType.StartsWith("(") ?
        _GetType :  _GetType[1].ToString() + _GetType[2].ToString());
        m_BeforHandTranslateNow = m_BeforHandTranslateStack.ToList().Find(x => x.Key == m_TranslateType).Value;
        DELManager_TranslateAll_Text?.Invoke();
    }

    public bool CheckTranslateLanguage(string _AnyKey)
    {
        TranslateType GetType = m_BeforHandTranslateStack.ToList().Find(x => x.Value.ToList().Exists(y => _AnyKey == y.Item2)).Key;
        if (m_TranslateType == GetType) return true;
        if (GetType == TranslateType.None)
        {
            Debug.LogWarningFormat("번역 타입에 존재하지 않는 언어이거나 " +
            "테이블에 존재하지 않는 이름입니다.! 오류 텍스트 : [{0}]", _AnyKey);
            return true;
        }
        return false;
    }

    //반복문을 덜 사용하도록 리팩토링 요망
    public string ChangeTranslateLanguage(string _AnyKey, bool _isFindKey = false)
    {
        if (_isFindKey && int.TryParse(_AnyKey, out int _OutInt))
        {
            int FIDX = -1; FIDX =
            m_BeforHandTranslateNow.ToList().FindIndex(x => int.Parse(x.Item1) == _OutInt);
            return FIDX == -1? string.Empty : m_BeforHandTranslateNow[FIDX].Item2;
        }

        if (CheckTranslateLanguage(_AnyKey))
            return _AnyKey;

        int RowIDX = -1, ColIDX = -1;
        //m_GetTranslateAllStack[i][0]; //키 인덱스
        //m_GetTranslateAllStack[0][j]; //번역 타입
        RowIDX = m_GetTranslateAllStack.ToList().FindIndex(x => x.ToList().Exists(y => y == _AnyKey));
        ColIDX = m_GetTranslateAllStack[0].ToList().FindIndex(x => m_TranslateType.ToString() == x); //지점된 언어 타입을 가져올때

        #region 반복으로 전체 탐색 방식 (미리 인덱스를 가지고 있는 방식으로 리팩토링 요망)
        //for (int i = 1; i < m_GetTranslateAllStack.Length; i++)
        //{
        //    for (int j = 1; j < m_GetTranslateAllStack[i].Length; j++)
        //    {
        //        if (_AnyKey == m_GetTranslateAllStack[i][j])
        //        {
        //            RowIDX = i; ColIDX = j;
        //            break;
        //        }
        //    }
        //}
        #endregion
        if (RowIDX == -1 && ColIDX == -1)
        {
            Debug.LogWarningFormat("번역할 문자가 테이블에 존재치 않습니다..!! {0}", _AnyKey);
            return _AnyKey;
        }
        return m_GetTranslateAllStack[RowIDX][ColIDX];
    }

    #endregion
}
