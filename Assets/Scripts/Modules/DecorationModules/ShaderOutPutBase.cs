using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Commons.Helpers;
using Commons.Helpers.Attribute;

public abstract class ShaderOutPutBase : MonoBehaviour
{
    [SerializeField] protected Material m_MainOutPutMaterial;
    [SerializeField] protected ShaderByObjRendererSystem m_ShaderControllSystem;
    protected NewShaderInfo m_GetSShaderInfo;
    protected Material m_ComapreMainMat = null;
    protected System.Action m_EndCallBack = null;

    public Material pp_GetMainOutPutMaterial { get => m_MainOutPutMaterial; }

    public abstract void Initialization(ShaderByObjRendererSystem _GetControllSystem,
    NewShaderInfo _GetSInfo, Renderer _GetRunRenderer, System.Action _EndCallBack = null);

    public abstract void ListenInitSetting(params object[] _SetValues);

    public virtual void ListenInitSettingByName(string _FindName, object _SetValue)
    {
        if ((!Helper.SetFieldNameToClass(this, _FindName, _SetValue)).HDebug(
        $"{_FindName}이라는 변수가 {this.GetType().Name}의 변수에 존재하지 않습니다.!", Helper.HDType.Warning)) return;
    }

    public virtual bool CheckCompareMaterialName(Material _CampareInstanceMat, out string _CompareStr)
    {
        _CompareStr = _CampareInstanceMat.name.ReplaceRemoveStrValue("(Clone) (Instance)");
        return m_MainOutPutMaterial.name == _CompareStr.Trim();
    }

    protected virtual bool CheckBySyncOutPutBaseMaterial(string _GetProperty, Material _CampareInstanceMat)
    => (!CheckCompareMaterialName(_CampareInstanceMat, out string _OutCStr)).HDebug(
    $"인스턴싱된 머터리얼이 현재 적용하려는 Renderer의 머터리얼과 다릅니다.! {_OutCStr}", Helper.HDType.Error) ||
    (!_CampareInstanceMat.HasProperty(_GetProperty)).HDebug($"적용하고자 하는 {this.name} 에서는 " +
    $"프러퍼티 {_GetProperty} 가 반드시 존재해야 합니다.!", Helper.HDType.Error);

    public abstract void OutPutShader(System.Action _AddEndCallBack = null);

    protected abstract void OutPutShaderDone();
}
