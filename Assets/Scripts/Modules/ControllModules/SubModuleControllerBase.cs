using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Sirenix.OdinInspector;

public abstract class SubModuleControllerBase : ControllerBase
{
    #region SubModules Infos Refernece

    public enum SubModuleBreakType 
    {
        BreakAll, 
        PartBreak,
        OnlyBreakElems, //자기자신을 제외한 모든 배열 요소에 해당됨
        BreakAfterEvent
    }

    #endregion

    [Header("===Test 목적 (리팩요망)")]
    public int pp_GetModelItemIDX { get; protected set; }

    [Header("===SubControllBase===")]
    [field: SerializeField] public bool pp_isRequiredSubModule { get; protected set; }
    //싱글톤과 같이 씬에서 오직 하나만 존재하며 한번 인스턴싱이 진행된다면 True고 다시 삭제후 생성이 안됨
    [field: SerializeField] public bool pp_isInstanceOnlyOnce { get; protected set; } 
    //향후 인터페이스 I_CheckInit 인터페이스를 각 자식에서 상속받고 삭제할것
    [field: SerializeField, ReadOnly, ShowIf(nameof(odin_isShowReadOnly))] public bool pp_isInitComplate { get; protected set; } 
    [field: SerializeField, ReadOnly, ShowIf(nameof(odin_isShowReadOnly))] public bool pp_isSelfProcessUpdate { get; protected set; }

    protected ControllerBase m_MainController;
    public ControllerBase pp_MainController { get => m_MainController; } //Field로 인스팩터에 확인할 수 있도록 전체 변경 요망 **

    public override object[] Initlization(params object[] _ParsingParams)
    {
        List<object> GetParsingList = _ParsingParams.ToList();
        object[] GetBases = GetParsingList.FindAll(x => x is ControllerBase).ToArray();
        //if ((GetBases.Length > 1).HDebug($"{nameof(SubModuleControllerBase)} " +
        //$"초기화시 매개변수 {nameof(ControllerBase)}는 오직 하나여야 합니다.!", Helper.HDType.Error)) return null;
        m_MainController = GetBases != null && GetBases.Length > 0 ? GetBases[0] as ControllerBase : null;
        if(m_MainController != null) GetParsingList.Remove(m_MainController);
        return m_MainController != null? GetParsingList.ToArray() : null;
    }

    public virtual void BreakPoint(bool _isBreak, 
    SubModuleBreakType _BrackType = default, System.Type _GetType = null)
    => m_isActionProcess = _isBreak;

    public virtual void ExecuteChangeSubController() { } //DO NOTHING...

    public virtual void ProcessUpdate() { } //DO NOTHING...

    public virtual void ProcessUpdate(float _DeltaTime) { } //DO NOTHING...

    #region Sub System Private Functions (**Support Parents System Reference**)

    public T FindParentsObjTypeByTemp<T>() where T : ControllerBase
    {
        ControllerBase ReturnValue = pp_MainController;
        while (ReturnValue != null)
        {
            if(ReturnValue.GetType().Name == typeof(T).Name)
                break;
            if(ReturnValue is SubModuleControllerBase _GetSubBase)
            ReturnValue = _GetSubBase.pp_MainController ?? null;
        }
        return ReturnValue as T;
    }

    #endregion

    #region 변수를 (템플릿 or 문자열) 형식으로 가져올때만 사용할것 (사용에 의미가 없다면 삭제 요망...)

    public virtual T ApplySubModuleActions<T>() where T : Component
    {
        return this.GetType().GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public |
        System.Reflection.BindingFlags.NonPublic).ToList().Find(x => typeof(T).Name == x.FieldType.Name).GetValue(this) as T;
    }

    public virtual object ApplySubModuleActions(string _TempsName)
    {
        return this.GetType().GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public |
        System.Reflection.BindingFlags.NonPublic).ToList().Find(x => _TempsName == x.FieldType.Name).GetValue(this);
    }

    public virtual T[] ApplySubModuleActionsArray<T>()
    {
        return this.GetType().GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public |
        System.Reflection.BindingFlags.NonPublic).ToList().FindAll(x => x is T).OfType<T>().ToList().ToArray();
    }
    #endregion

    #region 유니티 이벤트 함수
    protected override void Update()
    {
        base.Update();
        if (!pp_isSelfProcessUpdate || !m_isActionProcess) return;

        ProcessUpdate();
    }
    #endregion
}

public interface I_ActionEndCallSubModules
{
    public void I_EndCallSubModule();
}