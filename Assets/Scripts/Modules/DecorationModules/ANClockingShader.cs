using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Commons.Helpers;
using DG.Tweening;

public class ANClockingShader : ShaderOutPutBase
{
    [Header("Apply By ANClockingShader Value")]
    [SerializeField] private bool m_isClockingOn = false;
    [SerializeField] private bool m_isDoneAndDestroy = false;
    private readonly string m_MainPropertyValueByShader = "_MainPower";


    public override void Initialization(ShaderByObjRendererSystem _GetControllSystem, NewShaderInfo _GetSInfo, Renderer _GetRunRenderer,
    System.Action _EndCallBack = null)
    {
        if (base.CheckBySyncOutPutBaseMaterial(m_MainPropertyValueByShader, _GetRunRenderer.material)) return;
        m_ComapreMainMat = _GetRunRenderer.material;
        m_GetSShaderInfo = _GetSInfo;
        m_ShaderControllSystem = _GetControllSystem;
        m_EndCallBack = _EndCallBack;
        float ApplyActionValue = m_isClockingOn ? m_GetSShaderInfo.s_EndValue :  m_GetSShaderInfo.s_StartValue;
        m_ShaderControllSystem.spp_RenderMeshInfo.Value.s_GetRender.materials.ToList().ForEach(x =>
        x.SetFloat(m_MainPropertyValueByShader, ApplyActionValue));
    }

    public override void ListenInitSetting(params object[] _SetValues)
    {
        m_isClockingOn = (bool)_SetValues[0];
        m_isDoneAndDestroy = (bool)_SetValues[1];
    }

    public override void OutPutShader(System.Action _AddEndCallBack = null)
    {
        if (base.CheckBySyncOutPutBaseMaterial(m_MainPropertyValueByShader, m_ComapreMainMat)) return;
        if (_AddEndCallBack != null) m_EndCallBack += _AddEndCallBack;
        int CountIDX = 0;
        Material[] GetActionsMats = m_ShaderControllSystem.spp_RenderMeshInfo.Value.s_GetRender.materials;
        GetActionsMats.ToList().ForEach(x =>
        {
            float ApplyActionValue = m_isClockingOn ? m_GetSShaderInfo.s_StartValue : m_GetSShaderInfo.s_EndValue;
            x.DOFloat(ApplyActionValue, m_MainPropertyValueByShader, m_GetSShaderInfo.s_TweeningSpeed).
            SetEase(Ease.InOutExpo).OnComplete(() =>
            { CountIDX++; if (CountIDX >= GetActionsMats.Length) OutPutShaderDone(); });
        });
    }

    protected override void OutPutShaderDone()
    {
        m_isClockingOn = !m_isClockingOn;
        //On 일때는 쉐이더 켜진 상태 즉 캐릭터가 보이는 구간으로 없애도 되지만 캐릭터가 없어지는 구간에는
        //다시 켜야되기 때문에 항상 Running상태로 존재해야한다
        if (m_isClockingOn) 
        m_ShaderControllSystem.RemoveWhenShaderOff(m_isDoneAndDestroy, m_GetSShaderInfo, this);
        m_EndCallBack?.Invoke();
    }
}
