using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Commons.Helpers.Attribute
{
    public class Lim_SpritePriviewAttribute : PropertyAttribute
    {
        public float spp_Wigth { get; private set; }
        public float spp_Hight { get; private set; }

        public Lim_SpritePriviewAttribute(float _GetWH, float _GetHG)
        {
            spp_Wigth = _GetWH;
            spp_Hight = _GetHG;
        }
    }

    public class Lim_InspectorReadOnly : PropertyAttribute
    {
        
    }

    public class Lim_FoldoutAttribute : PropertyAttribute
    {
        public bool ssp_isReadOnlyToogle;
        public string ssp_SetStr;
        
        public Lim_FoldoutAttribute(bool _isToggle , string _GetStr)
        {
            ssp_isReadOnlyToogle = _isToggle;
            ssp_SetStr = _GetStr;
        }
    }

    public class Lim_FoldoutArrayAttribute : PropertyAttribute
    {
        public bool ssp_isReadOnlyToogle;
        public string ssp_SetStr;
        public string[] ssp_ExcludeProperties;

        public Lim_FoldoutArrayAttribute(bool _isToggle, string _GetStr, params string[] _ExcludeProperties)
        {
            ssp_isReadOnlyToogle = _isToggle;
            ssp_SetStr = _GetStr;
            ssp_ExcludeProperties = _ExcludeProperties;
        }
    }
}

[System.Serializable]
public class TestDefultClass
{
    [/*Commons.Helpers.Attribute.Lim_Foldout(false, ""),*/ SerializeField] private string m_OutPutTestStr = string.Empty;
    public int s_SubInt = -1;
    public string s_SubStr_2 = string.Empty;
    public Sprite s_Spr_1;
    //[Commons.Helpers.Attribute.Lim_SpritePriview(100, 100), SerializeField] private Sprite s_Spr_1;
}