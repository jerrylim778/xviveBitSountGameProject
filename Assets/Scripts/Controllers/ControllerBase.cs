using UnityEngine;
using System.Linq;
using Commons.Helpers;


#region ControllerBase 사용 목적에 관한 설명
//1.프로젝트 적용시 MyInfo (Model)을 각 게임에 맞는 Main System(Controller)에 따라 나타내기 (View)를 위함
//쉽게 말해 GmaePlayManager와 System에서 Controller적용을 정적 상태에서 각 게임에 맞게 유연한 MVC 체게를 만들기 위함

//2.한 데이터에 따라 초기화때 여러 데이터들에 의해 Controller<클래스>의 모든 Controller가 반 결정(중간에 변동될 수 있음)
//되는 형태에서 Builder체제를 유지하지만 각 Controller들끼리의 교체되거나 상태를 변동시킬 수 있기에 팩토리 기능또한 섞여있다

//3.추가로 상속하여 사용하는 SubControllerBase는 ControllerBase를 상속받은 MainController에서 다발로 가지고 있으며
//이로써 비선형 형태로 이루어진 Composite 패턴으로 모든 Controller들을 관리할 수 있다
#endregion
#region ControllerBase 사용 세밀 규칙에 관한 설명
//1.각 데이터로 들어온 모델들은 사용할 수 있는 구조체나 기타 객체등으로 정제하는 과정을 걷히고
//해당 구조체로 사용한다

//2.해당 구조체는 정의되지 않는 인터페이스를 상속되어야 하며 ControllerBase는
//해당 인터페이스를 클래스 형변환하여 사용해야 한다
#endregion

//************************[리팩요망]************************
//적용안된 Controller => CharacterController *(특히 상속한뒤에 GmaePlayManager<클래스>와 System에서의 모든
//프로세스를 ControllerBase를 중심으로 동적 교체가 가능해야 된다 
//************************[리팩요망]************************
public abstract class ControllerBase : MonoBehaviour
{
    #region ControllerBase Info(Data) Reference

    public interface I_PurifiedCData {  } //Do Not Define...

    public interface I_PurifiedAddKey
    {
        public string I_SubInfoIDX();
    }
    
    public interface I_PurifiedParamsData 
    {
        public bool I_ISGetOutParams(ControllerBase _GetPPData); 
    }

    #endregion

    #region ControllerBase Support Data Reference
    public enum ColliderEventType { Enter, Stay, Exit }
    #endregion
    [Header("Odin Show ReadOnly")]
    [SerializeField] protected bool odin_isShowReadOnly = false;
    [Header("===MainControllBase===")]
    [SerializeField] protected bool m_isTestMode = false;
   
    [Tooltip("===RequiredCostomComponent===")]
    protected MyInfo m_ReciveMyInfo = null; //CharacterInfo나 기타 다른방식을 상속받아
    protected bool m_isActionProcess = false;
    public bool pp_isGameOver { get; protected set; }//Player => 게임불가(쓰러진)상태 Controller => 패배조건 도달
    public bool pp_isTempMine { get; protected set; } //MyInfo는 없으나 나인것을 확인해야될때 사용됨
    public bool pp_isMine { get => m_ReciveMyInfo != null; }
    public bool pp_isTestMode { get => m_isTestMode; } //중첩되는게 있어 반드시 이후에 삭제해야됨

    public abstract object[] Initlization(params object[] _ParsingParams);
    //protected abstract void BuilderByActionBoundary(I_PurifiedCData _SetInfo);
    public virtual void CallEndController(bool _isSubCheck, I_Data AppliedDataSet = null)
    {
        if (this is SubModuleControllerBase) 
            return;

        pp_isGameOver = true;
        ForcePlayOrStopOrder(false);
    }

    public virtual void ForcePlayOrStopOrder(bool _isPlay)
    {
        if ((this is SubModuleControllerBase).HDebug(
        $"{nameof(SubModuleControllerBase)}는 강제 실행이 불가능합니다.!", Helper.HDType.Error)) return;
        m_isActionProcess = _isPlay;
    }

    #region Connect Event Functions

    protected virtual bool CheckAndApplyByParams<T>(object[] _ParsingParams, System.Action<T> _ComplateCallBack)
    {
        int FIDX = -1; FIDX = 
        _ParsingParams.ToList().FindIndex(x => x != null && (x.GetType().Name == typeof(T).Name || x is T));
        if (FIDX != -1) _ComplateCallBack?.Invoke((T)_ParsingParams[FIDX]);
        return FIDX != -1;
    }

    protected virtual T ApplyControllerInfoData<T>(I_PurifiedParamsData[] GetInfoData) where T : I_PurifiedParamsData
    => (T)GetInfoData.HFind(x => x is T);

    public virtual void ColliderEventCallBack(ControllerColliderModule _GetColModule,
    ControllerBase _GetController, ColliderEventType _ParsingEvent) { } //DoNoting

    #endregion

    #region 유니티 이벤트 함수

    protected virtual void Update() { } //DONOTHING...

    #endregion
}

public interface I_SubModulesCollection
{
    public void I_CheckSubModuleIsAllCanAction(ControllerBase _GetSubBase);
    public T I_GetSubModule<T>() where T : SubModuleControllerBase;
    public bool ISCheckSubModule<T>(out T _OutPutBase) where T : SubModuleControllerBase;
}

#region 인터페이스 설명
/// <summary>
/// 정확히는 한 MainController에 필수와 가연성등과 같이 
/// SubModuleController 지휘를 집단마다 다르게 하기 위한 추상 클래스
/// </summary>
#endregion
public interface I_SubModulesDivide 
{
    public void I_InitRequiredSubModules(
     SubModuleControllerBase[] _GetBase, ControllerBase.I_PurifiedParamsData[] _CompareParamsDatas);
    public SubModuleControllerBase[] I_GetInstanceAllSubController();
}

//현재는 사용하지 않으나 이후 사용가능성이 높음
public interface I_SubControllerPreOrder
{
    public void I_InitPreOrder();
}

#region 다중상속을 유도하기 위해 인터페이스를 템플릿화 요망 [보류]

//public interface I_BuilderAction
//{
//    void I_BuilderByActionBoundary(ControllerBase.I_PurifiedCData _SetInfo);
//}

//public interface I_AppliedSubTemp<T> where T : class
//{
//    T I_GetTemp();
//}

//public abstract class SI_BuilderAction
//{
//    protected abstract void SI_BuilderByActionBoundary(ControllerBase.I_PurifiedCData _SetInfo);
//}

//public class TestSIBuilder : I_AppliedSubTemp<SI_BuilderAction>
//{
//    public void I_GetAppliedSubTemp()
//    {

//    }
//}

#endregion
