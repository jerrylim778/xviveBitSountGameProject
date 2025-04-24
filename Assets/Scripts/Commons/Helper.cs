using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
//using System;
//using System.Numerics;

namespace Commons.Helpers
{
    public static partial class Helper
    {
        #region Linq Helper Reference

        public static bool ISFindCollections<T, U>
        (T _CompareType, System.Func<bool> _ComapreSet, out U _OutPutType) where T : List<U> where U : Object
        {
            int FIDX = -1;
            FIDX = _CompareType.FindIndex(x => _ComapreSet.Invoke());
            //if(FIDX != -1)
            //{
            //_OutPutType = 
            //}
            _OutPutType = null;
            return false;
        }

        public static List<T> ChangeTypeByList<U, T>(this List<U> _GetChangedList)
        => _GetChangedList.OfType<T>().ToList();

        public static bool ISFindCondition<T>(this List<T> _GetChangedList, System.Func<T, bool> _Condition, out T _OutFIDX) where T : class
        {
            int FIDX = -1; FIDX = _GetChangedList.ToList().FindIndex(x => _Condition.Invoke(x));
            _OutFIDX = FIDX == -1 ? null : _GetChangedList[FIDX];
            return FIDX != -1;
        }

        public static bool ISArrayFindCondition<T>(this T[] _GetChangedArray, System.Func<T, bool> _Condition, out T _OutFIDX)
        {
            _OutFIDX = default(T);
            int FIDX = -1; FIDX = _GetChangedArray.ToList().FindIndex(x => _Condition.Invoke(x));//.HFindIndex(x => _Condition.Invoke(x));
            if(FIDX != -1) _OutFIDX = _GetChangedArray[FIDX];
            return FIDX != -1;
        }

        public static bool ISArrayFindIDXCondition<T>(this T[] _GetChangedArray, System.Func<T, bool> _Condition, out int _OutFIDX) /*where T : class*/
        {
            int FIDX = _GetChangedArray.HFindIndex(x => _Condition.Invoke(x));
            _OutFIDX = FIDX;
            return FIDX != -1;
        }

        public static bool ISFindCondition(List<object> _GetChangedList, System.Func<object, bool> _Condition, out object _OutFIDX)
        {
            int FIDX = -1; 
            FIDX = _GetChangedList.ToList().FindIndex(x => _Condition.Invoke(x));
            _OutFIDX = FIDX != -1 ? _GetChangedList[FIDX] : null;
            return FIDX != -1;
        }


        #endregion

        #region Setting Or Init Helper Reference

        public enum HDType { Defult = 0, Warning, Error }
        public static bool HDebug(
        this bool _isCompareTest, string _SetTxt, HDType _SetType = HDType.Defult)
        {
            if (!_isCompareTest) return false;
            switch (_SetType)
            {
                case HDType.Defult: Debug.Log(_SetTxt); break;
                case HDType.Warning: Debug.LogWarning(_SetTxt); break;
                case HDType.Error:Debug.LogError(_SetTxt); break;
            }
            return true;
        }

        public static T CheckComnectComponent<T>(this GameObject _CheckObj) where T : Component
        => _CheckObj.GetComponent<T>() == null ? _CheckObj.AddComponent<T>() : _CheckObj.GetComponent<T>();

        public static T CreateApplyComponent<T>(this GameObject _CheckObj, Transform _SetParTr = null) where T : Component
        {
            GameObject GetNewObj = new GameObject($"{_CheckObj.name}_{typeof(T).Name}");
            GetNewObj.transform.position = _CheckObj.transform.position;
            GetNewObj.transform.SetParent(_SetParTr);
            return  GetNewObj.CheckComnectComponent<T>();
        }

        //캐릭터의 탐색에 대한 방향을 부모에서 찾아서 Out 참조자에 의해 반환받는 형식으로 가져와
        //사용할 수 있도록 정의해보자
        public static bool SearchOutCollider<T>(GameObject _CheckObj, out T _FindObj) where T : MonoBehaviour
        {
            if (_CheckObj.GetComponent<T>() != null || _CheckObj.transform.parent != null
            && _CheckObj.transform.parent.GetComponent<T>() != null)
            {
                _FindObj = _CheckObj.GetComponent<T>() != null ? _CheckObj.GetComponent<T>() : _CheckObj.transform.parent.GetComponent<T>();
                return true;
            }
            _FindObj = null;
            return false;
        }

        public enum SyncType { None, NotIntsnce, NotApplyPos , NotApplyRot}

        public static GameObject NewObjAndMakeSyncPosRot(this GameObject _OutPutObj, 
        Transform _ApplyTr, SyncType _GetType = SyncType.None)
        {
            GameObject GetNewObj = _GetType != SyncType.NotIntsnce? Object.Instantiate(_OutPutObj, _ApplyTr) : _OutPutObj;
            if (_GetType != SyncType.NotApplyPos) GetNewObj.transform.localPosition = Vector3.zero;
            if (_GetType != SyncType.NotApplyRot) GetNewObj.transform.localRotation = Quaternion.Euler(Vector3.zero);
            return GetNewObj;
        }

        public static void SyncObjPosRot(this Transform _GetOGTr, Transform _ApplyTr, bool _isApplyTrPar = false)
        {
            if (_isApplyTrPar) _GetOGTr.transform.SetParent(_ApplyTr);
            _GetOGTr.position = _ApplyTr.position;
            _GetOGTr.rotation = Quaternion.Euler(_ApplyTr.eulerAngles);
        }

        public static void SetAbleComponent<T>(this Component _FromObj, bool _isActive) where T : Component
        {
            if(typeof(T).Name == nameof(Collider)) _FromObj.GetComponent<Collider>().enabled = _isActive;
            else if (typeof(T).Name == nameof(Behaviour)) (_FromObj.GetComponent(typeof(T)) as Behaviour).enabled = _isActive;
        }

        #endregion

        #region Iterator System Helper Reference

        #region 일반 배열 반복문 System Functions Reference
        #region 반복문을 대체해서 사용할 경우 주의해야될 사항 설명
        //성능 저하의 정도
        //Action (대리자 + 이벤트) 구조 이기 때문에 약간의 오버해드가 발생할 수 있다
        //데이터가 적을 때는 거의 차이가 없을 수 있음 
        //데이터가 많거나 해당 메서드가 매우 빈번하게 호출될 때는 약간의 성능 차이가 누적되어 눈에 띄는 차이가 발생할 수 있음
        //최적화할 수 있는 방법
        //람다 함수나 콜백을 너무 빈번하게 사용하는 것이 문제가 될 경우, 꼭 필요한 경우에만 사용하고 단순 반복에서는 직접 처리를 고려해 볼 수 있음
        //성능이 정말 중요한 경우라면 프로파일링을 통해 얼마나 큰 차이가 나는지 실제로 측정해보고, 그에 따라 개선 여부를 결정하는 것이 좋음
        //결론적으로 성능 저하는 있겠지만 대부분의 경우 실질적인 차이는 미미할 가능성이 높다. 따라서 성능보다 코드 가독성과 재사용성을 중시하는 경우 이런 식의 함수형 패턴은 유용할 수 있음
        #endregion

        public static int HFindIndex<T>(this IList<T> _ApplyArray, System.Predicate<T> _ComparePreMatch)
        {
            if (_ComparePreMatch == null) return -1; // new System.ArgumentNullException(nameof(_ComparePreMatch));

            for (int i = 0; i < _ApplyArray.Count; i++)
                if (_ComparePreMatch.Invoke(_ApplyArray[i]))
                    return i;

            return -1; //default(int)
        }

        public static bool HTrueForAll<T>(this IList<T> _ApplyArray, System.Predicate<T> _ComparePreMatch)
        {
            if (_ComparePreMatch == null) new System.ArgumentNullException(nameof(_ComparePreMatch));

            int TrueForAllCount = 0;

            for (int i = 0; i < _ApplyArray.Count; i++)
            {
                if (_ComparePreMatch.Invoke(_ApplyArray[i])) TrueForAllCount++;
                if(TrueForAllCount >= _ApplyArray.Count) return true;
            }

            return false;
        }

        public static bool HExist<T>(this IList<T> _ApplyArray, System.Predicate<T> _ComparePreMatch)
        {
            if (_ComparePreMatch == null) new System.ArgumentNullException(nameof(_ComparePreMatch));

            for (int i = 0; i < _ApplyArray.Count; i++)
                if (_ComparePreMatch.Invoke(_ApplyArray[i]))
                    return true;

            return false;
        }

        //Predicate bool 형식의 대리자 Func는 반환 타입의 대리자라 이해해라
        //ex)System.Predicate => public delegate bool Predicate<T>(T obj);
        //ex)System.Func => public delegate TResult Func<in T1, out TResult>(T1 arg1); in T1 => 16개 매개변수 params와 비슷한 개념
        public static T HFind<T>(this IList<T> _ApplyArray, System.Predicate<T> _ComparePreMatch)
        {
            if (_ComparePreMatch == null) new System.ArgumentNullException(nameof(_ComparePreMatch));

            for (int i = 0; i < _ApplyArray.Count; i++)
            if (_ComparePreMatch.Invoke(_ApplyArray[i]))
            return _ApplyArray[i];

            return default(T);
        }

        public static void HForEach<T>(this IEnumerable<T> _ApplyArray, System.Action<T> _RunCallBack)
        { 
            foreach (T x in _ApplyArray)
                _RunCallBack.Invoke(x);
        }

        public enum HFEReturnType { None = 0, Break, Continue }

        #region 사용 예제
        //SpawnPositions.HForEach(x =>
        //{
        //    if (CountIDX<m_GameRequiedInfo.spp_Columns) return Helper.HFEReturnType.Break;    
        //    x = BottomRight + new Vector2(m_GameRequiedInfo.spp_Columns* CandySize.x, m_GameRequiedInfo.spp_Rows* CandySize.y);
        //    CountIDX++;
        //    return Helper.HFEReturnType.None;
        //});
        #endregion
        public static void HForEach<T>(this IEnumerable<T> _ApplyArray, System.Func<T, HFEReturnType> _RunCallBack)
        {
            foreach (T x in _ApplyArray)
            {
                HFEReturnType GetType = _RunCallBack.Invoke(x);
                if (GetType == HFEReturnType.Break) break;
                else if (GetType == HFEReturnType.Continue) continue;
            }
        }

        #endregion

        public static void HCountForEach(int _StartCount, int _ForceCount, System.Action<int> _RunCallBack)
        {
            if (_ForceCount < 0) return;
            for (int i = _StartCount; i < _ForceCount + 1; i++)
                _RunCallBack.Invoke(i);
        }

        public static T[] ArrayAddRemoveStruct<T>(T[] _GetOGArray, T _GetAdd, bool _isAdd)
        {
            List<T> GetList = _GetOGArray.ToList();
            if (_isAdd) GetList.Add(_GetAdd);
            else GetList.Remove(_GetAdd);
            return GetList.ToArray();
        }

        //public static Dictionary<T, U> ReturnInfoByTupleInfoType<T, U, S>(S _GetStuct) where S : struct
        //{
        //    _GetAnyType;
        //}

        public static bool IsStructNull<T>(this T _GetStuct) where T : struct
        {
            System.Nullable<T> GetNA = null;
            GetNA = _GetStuct;
            return GetNA != null && GetNA.HasValue;
        }

        public static Transform[] ChildLinearStuctureSearch(Transform _GetParent)
        {
            List<Transform> ReturnValue = new List<Transform>();
            for (int i = 0; i < _GetParent.childCount; i++)
                ReturnValue.Add(_GetParent.GetChild(i));

            return ReturnValue.ToArray();
        }

        public static T[] ChildLinearStuctureSearch<T>(this Transform _GetParent) where T : UnityEngine.Object
        {
            List<T> ReturnValue = new List<T>();
            for (int i = 0; i < _GetParent.childCount; i++)
                if (_GetParent.GetChild(i).GetComponent<T>() != null)
                    ReturnValue.Add(_GetParent.GetChild(i).GetComponent<T>());

            return ReturnValue.ToArray();
        }

        //부모의 선형 구조 탐색
        public static T ParentNonLinearStuctureSearch<T>(this Transform _StartChildTr) where T : Behaviour
        {
            T GetReturnValue = null;
            if (_StartChildTr == null) return GetReturnValue;
            Transform ParentsStartTr = _StartChildTr.parent;
            while (GetReturnValue == null && ParentsStartTr != null)
            {
                if (ParentsStartTr.GetComponent<T>() != null)
                {
                    GetReturnValue = ParentsStartTr.GetComponent<T>();
                    break;
                }

                ParentsStartTr = ParentsStartTr.parent;
            }

            return GetReturnValue;
        }

        //템플릿 내부에 있는 배열에서 중복된 값이 있을때 사용 요망.... (참조와 구조체를 구분하여 중복값을 걸러낸다 => 참조는 같은 메모리(주소 값이 동일)일 경우로 찾는다)
        public static bool CheckDepuil<T>(T[] _GetList, out T _OutPutIfDupuil)
        {
            _OutPutIfDupuil = default(T);
            if (typeof(T).IsValueType)
            {
                HashSet<T> uniqueItems = new();
                foreach (T one in _GetList) if (!uniqueItems.Add(one))
                    {
                        _OutPutIfDupuil = one;
                        return true;
                    }
                return false;
            }
            for (int i = 0; i < _GetList.Length; i++) //class 참조일경우
                for (int j = 0; j < _GetList.Length; j++) if (i != j && _GetList[i].Equals(_GetList[j]))
                    { _OutPutIfDupuil = _GetList[i]; return true; }
            return false;
        }

        #endregion

        #region Collection System Helper Reference

        //public static bool CheckListNull<T>(this List<T> _GetCheckList, int _CheckCount)
        //=> _GetCheckList != null && _GetCheckList.Count > _CheckCount;

        public static bool CheckArrayNull<T>(this IList<T> _GetCheckArray, int _CheckCount)
        => _GetCheckArray != null && _CheckCount < _GetCheckArray.Count;

        #region SplitListMiddleCount => 컬렉션을 정확히 반으로
        /// <summary>
        /// 컬렉션을 정확히 반으로 쪼개 해당 지점으로부터 
        /// 첫 배열부터 진행할 수 있습니다.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="_GetList"></param>
        /// <returns></returns>
#endregion
        public static List<List<T>> SplitListMiddleCount<T>(this IList<T> _GetList) where T : class
        {
            bool _isCol = _GetList.Count % 2 == 0; //짝 홀
            int GetIDX = _isCol ?
            _GetList.Count / 2 : Mathf.RoundToInt(_GetList.Count / 2);
            var LHalfList = new List<T>(); var RHalfList = new List<T>();
            Helper.HCountForEach(0, GetIDX - 1, _CountIDX => LHalfList.Add(_GetList[_CountIDX]));
            Helper.HCountForEach(GetIDX, _isCol ? GetIDX * 2 - 1 : GetIDX * 2, _CountIDX => RHalfList.Add(_GetList[_CountIDX]));
            LHalfList.Reverse();
            return new List<List<T>>(2) { LHalfList, RHalfList };
        }

        #endregion

        #region String Helper Referecne

        public static string HTrimEnd(this string _GetOGStr, char _RemoveIncludeChar)
        {
            int GetIDX = -1; GetIDX = _GetOGStr.IndexOf(_RemoveIncludeChar);
            return GetIDX >= 0 ? _GetOGStr.Substring(0, GetIDX) : _GetOGStr;
        }

        #region Enum Helper Reference

        public static bool HasFlags<T>(this T flags, T checkFlags) where T : System.Enum
        {
            int flagsValue = System.Convert.ToInt32(flags);
            int checkFlagsValue = System.Convert.ToInt32(checkFlags);

            return (flagsValue & checkFlagsValue) == checkFlagsValue;
        }

        public static string[] GetEnumByStringArray<T>(bool _isRemoveNone = false) where T : System.Enum
        {
            List<string> GetValues = System.Enum.GetNames(typeof(T)).ToList();
            if (_isRemoveNone)
            {
                int FIDX = -1; FIDX = GetValues.FindIndex(x => x == "None");
                if (FIDX != -1) GetValues.RemoveAt(FIDX);
            }

            return GetValues.ToArray();
        }

        public static T[] GetEnumByArray<T>(bool _isRemoveNone = false) where T : System.Enum
        {
            var GetStrs = GetEnumByStringArray<T>(_isRemoveNone);
            T[] ReturnValue = new T[GetStrs.Length];
            HCountForEach(0, ReturnValue.Length - 1, _CountIDX =>
            ReturnValue[_CountIDX] = StringToEnum<T>(GetStrs[_CountIDX]));
            return ReturnValue;
        }

        public static string IsFindStringToEnum<T>(this string _GetStr) where T : System.Enum
        => GetEnumByStringArray<T>().HExist(x => x == _GetStr) ? _GetStr : "None";

        public static T StringToEnum<T>(this string _GetStr) where T : System.Enum
        => (T)System.Enum.Parse(typeof(T), _GetStr);

        #endregion

        public static string ReplaceRemoveStrValue(this string _GetStr, string _RemoveValue, bool _isApplyTrim = false)
        {
            string GetRStr = _GetStr.Replace(_RemoveValue, "");
            if (_isApplyTrim) GetRStr.Trim();
            return GetRStr;
        }

        public static string[] SplitStrValue(string _GetStrValue, object _SplitStrs)
        {
            switch(_SplitStrs)
            {
                case char SetChar:
                    if (!_GetStrValue.Contains(SetChar)) { 
                    Debug.LogErrorFormat("{0} 문자열에 {1}라는 문자가 존재하지 않습니다..!!", _GetStrValue, SetChar); return null; }
                    return _GetStrValue.Split(SetChar);
                case string SetStr:
                    return _GetStrValue.SplitStrArrayValue(new string[] { SetStr });
                case string[] SetStrs:
                    return _GetStrValue.SplitStrArrayValue(SetStrs);
            }
            return null;
        }

        public static string[] SplitStrArrayValue(this string _GetStrValue, string[] _SplitStrs)
        {
            int FIDX = -1; FIDX = _SplitStrs.ToList().FindIndex(x => _GetStrValue.Contains(x));
            if (FIDX == -1) Debug.LogWarningFormat(
            "{0} 문자열에 {1}라는 문자가 존재하지 않습니다..!!", _GetStrValue, _SplitStrs[FIDX]);
            string[] ReturnValue = _GetStrValue.Split(_SplitStrs, System.StringSplitOptions.None).Where(x => !string.IsNullOrEmpty(x)).ToArray();
            return ReturnValue;
        }

        public static string ExtractContent(string _GetStr) //문자열에 특수기호안에 문자를 추출하기 위한 Helper
        {
            string ReturnStr = string.Empty, Parttern = @"\((.*?)\)"; ;
            System.Text.RegularExpressions.Match GetMath = System.Text.RegularExpressions.Regex.Match(_GetStr, Parttern);
            return ReturnStr = GetMath.Success ? GetMath.Groups[1].Value : string.Empty;
        }

        
        #endregion

        #region String To Other Reference

        public static T[] GetSplitStrArrayTemp<T>(string _GetOGStr, string _GetSpltStr)
        {
            if (!_GetOGStr.Contains(_GetSpltStr)) return null;
            return char.TryParse(_GetSpltStr, out char OutValue) ? _GetOGStr.Split(OutValue).OfType<T>().ToArray() :
            _GetOGStr.Split(new string[1] { _GetSpltStr }, System.StringSplitOptions.None).OfType<T>().ToArray();
        }

        public static T[] ExtractContentByTempArray<T>(string _GetStr, char _SplitChar)
        {
            string[] GetExtraStrs = ExtractContent(_GetStr).Split(_SplitChar);
            List<T> GetArrayTemp = new List<T>();
            GetExtraStrs.ToList().ForEach(x =>
            {
                try { GetArrayTemp.Add((T)System.Convert.ChangeType(x, typeof(T))); }
                catch
                {
                    Debug.LogErrorFormat("{0} 해당 문자열을 템플릿값으로 변경할 수 없습니다..!!", x);
                    GetArrayTemp.Add(default(T));
                }
            });
            return GetArrayTemp.ToArray();
        }


        public static Vector3[] GetStrToTransformArray(string _SetParsingArrayElement)
        {
            List<Vector3> POSROTSCALE = new List<Vector3>();
            //string GetNewStrElement = _SetParsingArrayElement;
            if (_SetParsingArrayElement.StartsWith("["))
                _SetParsingArrayElement = _SetParsingArrayElement.Replace("[", "");

            string[] SubStr = _SetParsingArrayElement.Split('+'); //.Split('\'');

            POSROTSCALE.Add(GetStringToVectorByTable(SubStr[0]));
            POSROTSCALE.Add(GetStringToVectorByTable(SubStr[1]));
            POSROTSCALE.Add(GetStringToVectorByTable(SubStr[2]));
            return POSROTSCALE.ToArray();
        }

        public static Vector3 GetStringToVectorByTable(string _SubParsingArrayElement)
        {
            if (string.IsNullOrEmpty(_SubParsingArrayElement)) return Vector3.zero;
            string GetNewStrElement = _SubParsingArrayElement.StartsWith("[") ?
            _SubParsingArrayElement.Replace("[", "") : _SubParsingArrayElement;

            string[] SubinSubPosStr = GetNewStrElement.Split('?');
            float x = 0f; float y = 0f; float z = 0f;
            float.TryParse(SubinSubPosStr[0], out x);
            float.TryParse(SubinSubPosStr[1], out y);
            float.TryParse(SubinSubPosStr[2], out z);
            Vector3 NewVec = new Vector3(x, y, z);
            return NewVec;
        }

        #endregion

        #region Calculate Helper Reference

        public static Vector3 ExtractHorizontal(Vector3 v, Vector3 normal, float weight)
        {
            if (weight <= 0f) return Vector3.zero;
            if (normal == Vector3.up) return new Vector3(v.x, 0f, v.z) * weight;
            Vector3 tangent = v;
            Vector3.OrthoNormalize(ref normal, ref tangent);
            return Vector3.Project(v, tangent) * weight;
        }

        public static Vector3 ExtractVertical(Vector3 v, Vector3 verticalAxis, float weight)
        {
            if (weight <= 0f) return Vector3.zero;
            if (verticalAxis == Vector3.up) return Vector3.up * v.y * weight;
            return Vector3.Project(v, verticalAxis) * weight;
        }

        #endregion

        #region Reflection Helper Reference

        public static System.Reflection.FieldInfo[] GetAllFieldsToClass(object _ClassType)
        {
            System.Reflection.FieldInfo[] GetFieldInfoArray = _ClassType.GetType().GetFields(System.Reflection.BindingFlags.Public |
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Static);
            return GetFieldInfoArray;
        }


        //[배열일경우]선 배열인지 확인하고 배열값에 하나라도 없다면 없고 null경우에는 모두 false로 배출한다
        //배열에 배열일 경우 즉 2차원 배열일 경우는 취급하지 않는다
        public static bool GetFieldNameToClass
        (object _ClassType, string _CompareStr, bool _isCheckNull, out System.Reflection.FieldInfo _GetOutField)
        {
            _GetOutField = null;
            System.Reflection.FieldInfo[] GetFieldInfoArray = GetAllFieldsToClass(_ClassType);
            foreach (System.Reflection.FieldInfo x in GetFieldInfoArray)
            {
                if (x.Name == _CompareStr)
                {
                    if (_isCheckNull && x.GetValue(_ClassType) == null) return false;
                    if (_isCheckNull && x.FieldType.IsArray &&
                    (((System.Array)x.GetValue(_ClassType)).Cast<object>().ToList().Exists(y => y == null || y.ToString() == "null") ||
                    ((System.Array)x.GetValue(_ClassType)).Cast<object>().ToList().Count <= 0))
                    #region 만일 배열의 null값을 못찾을시 해당 구간을 주석해제후 사용할 수 있도록
                    //{
                        //List<object> GetList = ((System.Array)x.GetValue(_ClassType)).Cast<object>().ToList();
                        //for(int i = 0; i < GetList.Count; i++)
                        //{
                        //    if(GetList[i].ToString() == "null")
                        //    return false;
                        //}
                        //if (GetList.Exists(y => y == null || y.ToString() == null))
                        //    return false;
                    //}
                    #endregion
                    return false; _GetOutField = x; return true;
                }
            }
            return false;
        }

        public static bool SetFieldNameToClass(object _ClassType, string _CompareStr, object _SetValue)
        {
            if (GetFieldNameToClass(_ClassType, _CompareStr, true, out System.Reflection.FieldInfo _GetField))
            {
                _GetField.SetValue(_ClassType, _SetValue);
                return true;
            }

            return false;
        }

        public static object GetCompareSaveFieldInfo<T>(this object _CompareType, string _GetFieldName)
        {
            object ReturnValue = null;
            System.Reflection.FieldInfo GetFieldInfo = _CompareType.GetType().GetField(_GetFieldName);
            if ((GetFieldInfo == null || GetFieldInfo.Name != _GetFieldName). 
            HDebug($"{_GetFieldName}해당 값은 {nameof(MySavedInfo)}에 포함되지 않는 데이터 타입입니다.!", Helper.HDType.Error)) return null;
            if (GetFieldInfo.FieldType.IsArray)
            {
                System.Array GetArray = (System.Array)GetFieldInfo.GetValue(_CompareType);
                if (GetArray == null) GetArray = System.Array.Empty<T>();
                ReturnValue = (object)GetArray;
            }
            return ReturnValue != null? ReturnValue : GetFieldInfo.GetValue(_CompareType);
        }

        public static object SetCompareFieldInfo<T>(this object _CompareType, T _GetData, string _GetFieldName)
        {
            System.Reflection.FieldInfo GetFieldInfo = _CompareType.GetType().GetField(_GetFieldName);
            //템플릿 타입도 맞는지 이후에 확인해야되나 Array비교시 요소또한 점검해야된다
            if ((GetFieldInfo == null /*|| GetFieldInfo.FieldType != typeof(T)*/ || GetFieldInfo.Name != _GetFieldName). 
            HDebug($"{_GetData.GetType().Name}해당 값은 {nameof(MySavedInfo)}에 포함되지 않는 데이터 타입입니다.!", Helper.HDType.Error)) 
            return null;
            bool isArray = GetFieldInfo.FieldType.IsArray; T[] GetFieldArray = null;
            if (isArray)
            {
                System.Array GetArray = (System.Array)GetFieldInfo.GetValue(_CompareType);
                if (GetArray == null) GetArray = System.Array.Empty<T>();
                GetFieldArray = (T[])GetArray;
                GetFieldArray = GetFieldArray.Concat(new[] { _GetData }).ToArray();
            }
            if (_CompareType.GetType().IsValueType)
            {
                var GetFieldValue = GetFieldInfo.GetValue(_CompareType);
                //구조체 복사본 수정 값타입에 따른 조치 (클래스는 없어도 무방)
                GetFieldInfo.SetValueDirect(__makeref(GetFieldValue), isArray ? GetFieldArray : _GetData);
            }
            GetFieldInfo.SetValue(_CompareType, isArray ? GetFieldArray : _GetData);
            return _CompareType;
        }

        #region Reflection으로 프로젝트에 해당되는 클래스가 존재 여부 파악 관련
        #region 프로젝트 내부에 해당되는 코드가 있는지의 여부에 관한 설명
        //1.단순 탐색뿐 아니라 존재치 아니 한다면 NameSpace나 기타 에셋의 DLL으로 재탐색을
        //실시하여 최종적인 클래스가 존재하는지를 확인해야된다
        //2.어셈블리로 빌드 후 런타임에서도 찾을 수도 있다
        //2-1.각 플렛폼별 최적화 (안드로이드 => LI2CPP , WEB =>) 을 진행할때 사용하지 않는
        //클래스는 삭제 처리되고 빌드가 된다 따라서 xlks.xml 파일로 프로젝트내부의 파일과 함께
        //빌드하여 빌드 최적화 이후 삭제 처리를 방지하여 찾는방법으로 해당 방식을 찾아야 한다
        #endregion

        public static bool SearchClassInProject(string _FindClassName)
        {
            return true; //우선 무조건 있다고 가정하고 적용한다
        }


        #endregion

        #region 각 존재하는 필드값을 다른 방향으로 이름으로 찾을 수있도록 수정 이후에 삭제요망..
        //public static string[] FindPropertiesNameByClass(object _ClassType)
        //{
        //    //System.Reflection.FieldInfo[] GetFieldInfos = GetAllFieldsToClass(_ClassType);
        //    //List<string> ReturnValues = new List<string>();
        //    //GetFieldInfos.ToList().ForEach(x => ReturnValues.Add(x.Name));
        //    //return ReturnValues.ToArray();
        //    List<string> ChangeNames = new List<string>();
        //    System.Reflection.PropertyInfo[] PPInfos = _ClassType.GetType().GetProperties();
        //    PPInfos.ToList().ForEach(x => ChangeNames.Add(x.Name));
        //    return ChangeNames.ToArray();
        //    //단일 적으로 찾을때 사용할것
        //    //int FIDX = -1; FIDX = PPInfos.ToList().FindIndex(x => x.Name == _CompareStr);
        //    //return FIDX == -1 ? string.Empty : PPInfos[FIDX].Name;
        //}
        #endregion
        #endregion

        #region GUI Setting System Reference

        public static void ChangePivotStaticPos(this RectTransform _GetChangedRT, Vector2 _NewPivot)
        {
            Vector3 OGLPos = _GetChangedRT.localPosition;
            Vector2 DeltaPivot = _NewPivot - _GetChangedRT.pivot, OGSize = _GetChangedRT.rect.size;
            Vector3 DeltaLPos = new Vector3(DeltaPivot.x * OGSize.x, DeltaPivot.y * OGSize.y, 0f);
            _GetChangedRT.pivot = _NewPivot;
            _GetChangedRT.localPosition = OGLPos + DeltaLPos;
        }

        #endregion
    }
}