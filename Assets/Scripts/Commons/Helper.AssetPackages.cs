using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using Sirenix.OdinInspector;


namespace Commons.Helpers
{
    public static partial class Helper
    {
        #region Do Tween Helper Reference
        public static void ResetSequce(this Sequence _GetSeq, bool _isComplate = true)
        {
            if (_GetSeq != null) { if(_isComplate) _GetSeq.Complete(); _GetSeq.Kill(); }
            _GetSeq = null;
        }

        public static TweenerCore<string, string, StringOptions> DOTmpText(
        this TMPro.TextMeshProUGUI target, string endValue, float duration, bool richTextEnabled = true, ScrambleMode scrambleMode = ScrambleMode.None, string scrambleChars = null)
        {
            if (endValue == null)
            {
                if (Debugger.logPriority > 0) Debugger.LogWarning("You can't pass a NULL string to DOText: an empty string will be used instead to avoid errors");
                endValue = "";
            }
            TweenerCore<string, string, StringOptions> t = DOTween.To(() => target.text, x => target.text = x, endValue, duration);
            t.SetOptions(richTextEnabled, scrambleMode, scrambleChars).SetTarget(target);
            return t;
        }

        #endregion

        #region Odin Inspactor Helper Reference

        public static IEnumerable<ValueDropdownItem<T>> HODIN_ClassListAsDropdown<T>(IEnumerable<T> _ApplyItems, System.Func<T, string> _ReturnValue)
        {
            foreach (var item in _ApplyItems)
                yield return new ValueDropdownItem<T>(_ReturnValue(item), item);
        }
        public static void HODIN_AddItemByList<T>(this List<T> _GetList)
        {
            if (_GetList == null)
            {
                _GetList = new();
                return;
            }
            T ApplyTemp = (T)System.Activator.CreateInstance(System.Type.GetType(typeof(T).Name, true));
            _GetList.Add(ApplyTemp);
        }

        #endregion
    }
}

