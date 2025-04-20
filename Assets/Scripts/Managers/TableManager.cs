using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Commons.Helpers;


namespace Commons.Helpers.ParsingTables
{

    public static class TableManager
    {
        #region Overload Table Load Public Functions

        public static string[][] TableParsingAllRowCol(ParsingTableType _TableType)
        => GetAllRowTableStack(_TableType, true).Select(x => x.Split(',')).ToArray();

        public static string TableParsingPartsRawCol(string _Key, string _ValueTypeKey, ParsingTableType _TableType)
        {
            string[] GetRowAllArray = GetAllRowTableStack(_TableType, true); //행열을 전부 구해야되기 때문에 무조건 상위설명을 포함
            string[] RealRowArray = GetRowAllArray[0].Split(','); //각 열(세로)에 대한 키값 비교
            string[] RealCollArray = new string[GetRowAllArray.Length]; //각 행(가로)에 대한 키값 비교
            int CountiDX = 0; RealRowArray.ToList().ForEach(x =>
            { RealRowArray[CountiDX] = GetRowAllArray[CountiDX].Split(',')[0]; CountiDX++; });

            string[][] ElementKeyValue = GetRowAllArray.Select(x => x.Split(',')).ToArray();

            int RowIDX = -1, ColIDX = -1;
            RowIDX = System.Array.IndexOf(RealRowArray, _Key);
            ColIDX = System.Array.IndexOf(RealCollArray, _ValueTypeKey);

            if (RowIDX == -1 || ColIDX == -1)
                return string.Empty;

            return ElementKeyValue[RowIDX][ColIDX];
        }

        #region 이후에 반드시 리팩토링 해야되는 부분이 있으나 기존것에서 파싱이 안되는 문제가 있을 수 있기 때문에 그대로 진행한다
        public static string[] AddSheetsByTable(ParsingTableType[] _TableTypeArray)
        {
            if ((_TableTypeArray == null || _TableTypeArray.Length <= 0).HDebug(
            "테이블에 관한 타입이 없습니다..!!", Helper.HDType.Warning)) return new string[0];
            List<string> AddAllStrValueList = new List<string>();
            string[] LastLegOnlyKeyValue = GetFirstPartOfAllRow(_TableTypeArray[_TableTypeArray.Length - 1]);
            int WIDX = 0;
            while (WIDX < _TableTypeArray.Length)//테이블의 배열임을 명심하자
            {
                List<string> GetRowDataList = GetAllRowTableStack(_TableTypeArray[WIDX]).ToList();
                #region 만일 인덱스가 1000이라면 관련된 사항에서 기존 100인덱스와 StartWith이 같게 인식하기 때문에 아래 주석 해제 후 사용
                //관련된 인덱스ItemInfo만 통과할 수 있도록 ItemType을 비교하여 같은것만 통과 시킬 수 있도록 변경해야함
                //if (WIDX == 0 && _CompareItemType != ItemType.None && _TableTypeArray[WIDX] == ParsingTableType.ItemInfo)
                //{
                //    GetRowDataList.ForEach(x =>
                //    {
                //        string[] GetElementSubData = x.Split(',');
                //        if ((ItemType)System.Enum.Parse(typeof(ItemType), GetElementSubData[9]) != _CompareItemType)
                //            GetRowDataList.Remove(x);
                //    });
                //}
                //OutKeyRowStack(WIDX == 0 ? true : false, AddAllStrValueList, GetRowData, LastLegOnlyKeyValue);
                //GetRowData.ToList().ForEach(x =>
                #endregion
                GetRowDataList.ForEach(x =>
                {
                    if (WIDX == 0)
                    {
                        LastLegOnlyKeyValue.ToList().ForEach(y =>
                        {
                            if (x.StartsWith(y)) //만약 키값이 같다면 해당 구간에 더해줘야한다
                                AddAllStrValueList.Add(x);
                        });
                    }
                    else
                    {
                        for (int i = 0; i < AddAllStrValueList.Count; i++)
                        {
                            //여기서 키값을 빼준다음 그 문자열로 비교해준다
                            string[] OnlyKey = AddAllStrValueList[i].Split(',');
                            if (x.StartsWith(OnlyKey[0])) //만약 키값이 같다면 해당 구간에 더해줘야한다
                            {
                                string NonKeyX = x.Replace(OnlyKey[0] + ",", "");
                                AddAllStrValueList[i] += NonKeyX != "" ?
                                AddAllStrValueList[i].EndsWith(",") ? NonKeyX : "," + NonKeyX :
                                !AddAllStrValueList[i].EndsWith(",") ? ",," : ",";
                            }
                        }
                    }
                });
                WIDX++;
            }

            return AddAllStrValueList.ToArray();
        }
        #endregion

        #region Row Reference [행 => 가로]

        public static string[] GetAllRowTableStack(ParsingTableType _TableType, bool _isIncludeTopDes = false)
        {
            List<string> GetAllStrList = new List<string>();
            TextAsset TxtAsset = Resources.Load<TextAsset>("Datas/TextAsset/" + _TableType.ToString());
            if (TxtAsset != null)
            {
                string[] GetAllStr = TxtAsset.text.Split('\n');
                for (int i = _isIncludeTopDes ? 0 : 1; i < GetAllStr.Length; i++)
                    if (GetAllStr[i] != "\r" && GetAllStr[i] != "") GetAllStrList.Add(GetAllStr[i]);
            }

            return GetAllStrList.ToArray();
        }

        public static string[] GetFirstPartOfAllRow(ParsingTableType _TableType, bool _isIncludeTopDes = false)
        {
            string[] GetSubStr = GetAllRowTableStack(_TableType, _isIncludeTopDes);
            string[] GetRealRowValueArray = new string[GetSubStr.Length];
            for (int i = 0; i < GetSubStr.Length; i++)
            {
                string[] GetAllRowSubStr = GetSubStr[i].Split(',');
                GetRealRowValueArray[i] = GetAllRowSubStr[0];
            }

            return GetRealRowValueArray;
        }


        public static string[] GetParseAllTablesByKey(int _keyValue, ParsingTableType _TableType, bool _isIncludeTopDes = false)
        {
            string[] GetAllStr = GetAllRowTableStack(_TableType, _isIncludeTopDes);
            foreach (string one in GetAllStr)
            {
                if (one.StartsWith(_keyValue.ToString()))
                {
                    string[] KeySubStr = one.Split(',');
                    if (KeySubStr[KeySubStr.Length - 1].EndsWith("\r"))
                        KeySubStr[KeySubStr.Length - 1] = KeySubStr[KeySubStr.Length - 1].ReplaceRemoveStrValue("\r"); ;
                    return KeySubStr;
                }
            }

            return null;
        }

        #endregion

        #endregion
    }

}