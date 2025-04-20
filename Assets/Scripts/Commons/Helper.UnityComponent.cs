using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Commons.Helpers
{
    public static partial class Helper
    {
        [System.Flags]
        public enum FlagsVectorArrowType
        {
            Up = 1 << 0,
            Left = 1 << 1,
            Right = 1 << 2,
            Down = 1 << 3,
            Middle = 1 << 4
        }

        #region Support Animator Component
        public static bool IsAnimPlaying(this Animator _GetAnim, string _CompareName)
        => _GetAnim.GetCurrentAnimatorStateInfo(0).IsName(_CompareName);

        //public static bool IsAnimPlaying(this Animator _GetAnim, int _CompareHash)
        //=> _GetAnim.GetCurrentAnimatorStateInfo(0).GetHashCode(_CompareHash);

        public static bool IsAnimPlayed(this Animator _GetAnim, string _CompareName)
        {
            if (!_GetAnim.IsAnimPlaying(_CompareName)) return false;
            return _GetAnim.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1f;
        }
        #endregion

        #region Support GUI Rect Size Components

        public static void RemeasureAchorSize(this RectTransform _ChangeRT, FlagsVectorArrowType _GetType)
        {
            Vector2 ReturnValue = _GetType switch
            {
                FlagsVectorArrowType.Up => new Vector2(0.5f, 1f),
                FlagsVectorArrowType.Down => new Vector2(0.5f, 0f),
                FlagsVectorArrowType.Left => new Vector2(0f, 0.5f),
                FlagsVectorArrowType.Right => new Vector2(1f, 0.5f),
                FlagsVectorArrowType.Middle => Vector2.one * 0.5f,
                _ => Vector2.zero
            };
            #region 피벗을 동적으로 움직일시 함께 변경된 엥커 포지션을 함께 움직이기 위한 조치
            //Vector2 OldWPos = _ChangeRT.position, OldPivot = _ChangeRT.pivot;
            //Vector2 PivotDelta = ReturnValue - OldPivot;
            //Vector2 PivotOffset = new Vector2(PivotDelta.x * _ChangeRT.rect.width, PivotDelta.y * _ChangeRT.rect.height);
            //Vector2 OldOffsetMin = _ChangeRT.offsetMin, OldOffsetMax = _ChangeRT.offsetMax;
            //Vector3 OldWPos = _ChangeRT.position;
            //_ChangeRT.anchorMin = ReturnValue; _ChangeRT.anchorMax = ReturnValue;
            //_ChangeRT.position = OldWPos;
            //_ChangeRT.pivot = ReturnValue;
            //_ChangeRT.offsetMin = OldOffsetMin; _ChangeRT.offsetMax = OldOffsetMax;
            //_ChangeRT.anchoredPosition -= PivotOffset;
            #endregion
            _ChangeRT.anchorMin = ReturnValue; _ChangeRT.anchorMax = ReturnValue;
            _ChangeRT.pivot = ReturnValue;
            _ChangeRT.anchoredPosition = Vector2.zero;
        }

        #endregion

        #region Support GameObject Component

        public static GameObject FindOrCreateByTag(string _FindTag)
        {
            var GetObjByTag = GameObject.FindGameObjectWithTag(_FindTag);
            if (GetObjByTag != null) return GetObjByTag;
            var GetNewObj = new GameObject(_FindTag);
            GetNewObj.transform.position = Vector3.zero;
            GetNewObj.transform.rotation = Quaternion.identity;
            GetNewObj.tag = _FindTag;
            return GetNewObj;
        }

        public static GameObject FindOrCreateByChildName(this Transform _GetMainTr, string _FIndChildName)
        {
            var GetObjByNames = Helper.ChildLinearStuctureSearch(_GetMainTr);
            if(GetObjByNames.ISArrayFindCondition(x => x.name == _FIndChildName, out Transform _GetTr))
            return _GetTr.gameObject;
            var GetNewObj = new GameObject(_FIndChildName);
            GetNewObj.transform.position = _GetMainTr.transform.position;
            GetNewObj.transform.rotation = Quaternion.Euler(_GetMainTr.transform.eulerAngles);
            GetNewObj.transform.SetParent(_GetMainTr);
            return GetNewObj;
        }

        #endregion
    }
}
