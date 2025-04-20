using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Commons.Helpers;
using Sirenix.OdinInspector;

#region RenewalShaderControllerBase¿¡ ´ëÇÑ ¼³¸í
//Controllerbase¸¦ »ó¼ÓÇüÀ¸·Î ShaderOutPutÀ» ´ëÃ¼ÇÒ ¼ö ÀÖµµ·Ï ¼öÁ¤ÇÑ´Ù
//¿¹¸¦µé¾î Ä³¸¯ÅÍ°¡ °¡¿¬¼º SubController¿¡ ÇØ´ç °´Ã¼¸¦ µé°í ÀÖÀ»°æ¿ì ¹ßµ¿½Ã
//Á¸ÀçÇÏ´Â ¸ğµ¨(µ¥ÀÌÅÍ)¿¡ µû¶ó ±×´ë·Î À¯ÁöÇÒ ¼öµµ º¯°æÇÒ ¼öµµ ÀÖ°í
//³ª¸ÓÁö´Â ½¦ÀÌ´õÀÇ ÇÁ·ÎÆÛÆ¼ÀÇ °¢°¢ÀÇ Á¶Àı·Î ÀÌ·ç¾îÁö±â ¶§¹®¿¡
//Æ¯Á¤ ÇÑ°¡Áö ÀÌ»óÀÇ ¹æ½ÄÀ¸·Î¸¸ ÀÌ·ç¾îÁö´Â°Ç ¾Æ´Ò¼öµµ ÀÖ´Ù
//ScriptableCommonÀ» »ó¼Ó¹Ş°í ÀÖÁö¸¸ ±âº»ÇüÀ¸·Î Àû¿ëÇÏ´Â ItemInfo¸¦ »ó¼Ó¹Ş¾Æ »ç¿ëÇÒ ¼ö ÀÖµµ·Ï ÇÑ´Ù
#endregion
public class ShaderOutPutSubControllerBase : SubModuleControllerBase
{
    //1.¸ğµç ÇÁ·ÎÆÛÆ¼¯H Dictionary¸¦ ÅëÇØ °¡Á®¿Ã ¼ö ÀÖµµ·Ï ÇÑ´Ù
    //2.°¡Á®¿Â ÇÁ·ÎÆÛÆ¼¸¦ ÀÌ¿ëÇÏ¿© °¢ ±¸°£¿¡ È°¿ëÇÒ ¼öµµ 
    //»ó¼ÓÀ» ÅëÇÑ ±âº»ÀûÀÎ ½¦ÀÌ´õÈ¿°ú¸¦ ÁÙ¼öµµ ÀÖ´Ù
    public enum EffectOutPutType
    {
        None = 0, //¹Ù·Î ´ëÀÔÇÏ°í ³¡³²
        DirectTweeningType, //TweeningÀ» ¹Ù·Î ÁøÇà
        ListenTweeningType, //´ë±â¿¡¼­ ¸ğ¾Ò´Ù°¡ ActionShaderOutPutÇÔ¼ö È£Ãâ½Ã ÀÏ°ıÃ³¸®
    }

    [Header("Required Value")]
    [SerializeField, ReadOnly, ShowIf(nameof(odin_isShowReadOnly))] private Shader m_UsedMainShader;
    [SerializeField, ReadOnly, ShowIf(nameof(odin_isShowReadOnly))] private Material m_InstanceShader; //ÀÌÈÄ¿¡´Â ºÎºĞº° Mateiral¸¦ ÁÙ ¼ö ÀÖµµ·Ï ¼öÁ¤ÇÑ´Ù
    [SerializeField, ReadOnly, ShowIf(nameof(odin_isShowReadOnly))] private RenewalShaderInfo m_RenewalShaderInfo;
    [Tooltip("Control Value")]
    private Dictionary<string, System.Tuple<int, System.Action<int, object>>> m_DicShaderProperty = new(); 
    //private Dictionary<string, System.Tuple<int, ShaderPropertyType>> m_DicShaderProperty = new(); //{Key: PP Name } {Value : PP KeyIDX}

    public override object[] Initlization(params object[] _ParsingParams)
    {
        var PartParams = base.Initlization(_ParsingParams);
        m_RenewalShaderInfo = PartParams[0] as RenewalShaderInfo;
        m_UsedMainShader = m_RenewalShaderInfo.s_EffectMaterial.shader;

        int GetAllPPCount = m_UsedMainShader.GetPropertyCount();
        Helper.HCountForEach(0, GetAllPPCount - 1, _CountIDX =>
        {
            var GetPPName = m_UsedMainShader.GetPropertyName(_CountIDX);
            var GetPPType = m_UsedMainShader.GetPropertyType(_CountIDX);
            var GetID = m_UsedMainShader.FindPropertyIndex(GetPPName);
            m_DicShaderProperty.Add(GetPPName, new(GetID, InitReturnCallBachByPPtype(GetPPType)));
        });
        return null;
    }

    #region Sub System Private Functions (**Init Shader Effect Reference**)

    private System.Action<int, object> InitReturnCallBachByPPtype(ShaderPropertyType _GetType) => _GetType switch
    {
        ShaderPropertyType.Texture => (PPIDX, _Params) => m_InstanceShader.SetTexture(PPIDX, _Params as Texture),
        ShaderPropertyType.Int => (PPIDX, _Params) => m_InstanceShader.SetInt(PPIDX, (int)_Params),
        ShaderPropertyType.Float => (PPIDX, _Params) => m_InstanceShader.SetFloat(PPIDX, (float)_Params),
        _ => null
    };

    #endregion
    

    //Æ®À§´×ÀÌ µ¿ÀÛÇÒ¶§¸é ÇöÀç °¡Áö°í ÀÖ´Â ÇÁ·ÎÆÛÆ¼ÀÇ °ª¿¡¼­ º¯°æÇÏ°íÀÚ ÇÏ´Â _ApplyParams 
    //·Î º¸°£ º¯°æÇÒ ¼ö ÀÖµµ·Ï ÇÑ´Ù (Texture³ª ±×¿¡ ÁØÇÏ´Â Å¬·¡½º´Â ÇØ´çµÇÁö ¾ÊÀ½
    public void ForceOrderShader<T>(EffectOutPutType _EffectOutPutType, string _GetPropertyName, T _ApplyParams)
    {
        if ((_EffectOutPutType != EffectOutPutType.None && !_ApplyParams.GetType().IsValueType).
        HDebug($"°ª Å¸ÀÔ¸¸ º¸°£ º¯°æÀÌ °¡´ÉÇÕ´Ï´Ù.! ÇöÀç Å¸ÀÔ {_ApplyParams.GetType().Name}", Helper.HDType.Error)) return;
        var GetPairShaderPP = m_DicShaderProperty[_GetPropertyName];
        if(_EffectOutPutType == EffectOutPutType.None)
        {
            GetPairShaderPP.Item2?.Invoke(GetPairShaderPP.Item1, _ApplyParams);
            return;
        }
        //¿ø·¡°ª°ú º¸°£ÇÒ °ªÀ» µ¿½Ã¿¡ °¡Áö°í ÀÖ´Â ¸®½ºÆ®¿¡¼­ µé°í ÀÖ´Ù°¡ ActionShaderOutPutÀÏ¶§ ÀüºÎ ¹ßµ¿ÇÒ°Í
        #region Ãß°¡ÀûÀ¸·Î Å¸ÀÔÀ» ³ª´²¾ß ÇÒ½Ã »ç¿ëÇÒ°Í (¹®Á¦¾øÀ»½Ã »èÁ¦¿ä¸Á **)
        //switch (_EffectOutPutType)
        //{
        //    case :
        //        GetPairShaderPP.Item2?.Invoke(GetPairShaderPP.Item1, _ApplyParams);
        //        break;
        //    case EffectOutPutType.DirectTweeningType:

        //        break;
        //    case EffectOutPutType.ListenTweeningType:

        //        break;
        //}
        #endregion
    }

    //ÀÌÈÄ °¡Á®¿Â ¸ŞÀÎ ¿ÀºêÁ§Æ®Æ÷ÇÔ ÇÏÀ§ ÀüÃ¼¿¡ Renderer µ¹¾Æ¼­ Mateiral°¹¼ö + TextureÁ¤º¸¸¦ 
    //°¡Á®¿Í º¯È¯ÇÏ´Â°ÍÀ» ÁøÇàÇÑ´Ù NewPlayerCharacterController<½ºÅ©¸³Æ®> => ShaderOutPutBase<Å¬·¡½º> ÂüÁ¶ÇÏ¿© ÀüºÎ °¡Á®¿Í ¼öÁ¤ ¿ä¸Á **
    public virtual void ActionShaderOutPut(GameObject _ChangeMainObj, System.Action _EndCallBack = null) 
    {

    }
    
}


//ÇâÈÄ SDataShaderInfoÀÇ °ªÀ» ¾Æ·¡ÀÇ Å¬·¡½º·Î ÀüÃ¼ º¯È¯ÇÒ ¼ö ÀÖµµ·Ï ¼öÁ¤ÇÑ´Ù
[System.Serializable]
public class RenewalShaderInfo : InstanceSpriteInfo
{
    public bool s_ChangeStatic; //º¯È¯ÀÌÈÄ ½¦ÀÌ´õ¸¦ ¿øº¹ÇÏÁö ¾Ê´Â´Ù
    public Material s_EffectMaterial;
    [ShowIf("@s_EffectShader != null")] public ShaderOutPutSubControllerBase s_EffectShader;
    [System.NonSerialized, ShowInInspector, HideIf("@s_EffectShader != null"), ReadOnly]
    [GUIColor(1f, 0.5f, 0f), HideLabel] private string odin_WarningPlaceholder = "±âº»ÇüÀÎ ShaderOutPutSubControllerBase¸¸ È°¿ëÇÕ´Ï´Ù.!";
}
