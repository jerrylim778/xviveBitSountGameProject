using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Sirenix.OdinInspector;
using Commons.Helpers;

//AnimController과 상속관계 를 형성할 수 있는지의 여부를 따져보자 (현재는 AnimController의 대체로 사용함)
//[RequireComponent(typeof(AnimCallbackController))]
[RequireComponent(typeof(Animator)), RequireComponent(typeof(SpriteRenderer))]
public class StateMachineController : MonoBehaviour // : AnimationController 
{
    
}
//{
//    [Header("MainStateMachineHeader")]
//    [SerializeField, ReadOnly] private ThreeMatchCharacterController m_TMGCharacterController;
//    [SerializeField, ReadOnly] private CharacterStateBase m_SystemTMGStateBase;
//    [SerializeField, ReadOnly] private string m_AnimStateName;
//    [SerializeField, ReadOnly] private Animator m_MainAnimator;
//    [Tooltip("MainStateMachineHeader")]
//    private State m_GetCurrentState;
//    public CharacterStateBase pp_SystemTMGStateBase { get => m_SystemTMGStateBase; }
//    public ThreeMatchCharacterController pp_TMGCharacterController { get => m_TMGCharacterController; }

//    [Tooltip("MainStateMachineHeader")]
//    private bool m_isProcessAction = false;
//    private float m_CheckCountTime = 0f;
//    public bool pp_isProcessAction { get => m_isProcessAction; set => m_isProcessAction = value; } 
//    //private AnimCallbackController _animCallbackController;

//    public State pp_CurrentState { get => m_GetCurrentState;}
//    public Animator pp_MainAnimator { get => m_MainAnimator; }

//    //public void Initlization(ThreeMatchCharacterController _GetMainController, 
//    //ThreeMatchCharacterController.TMGCalculateInfo _CalculateInfo, ThreeMatchCharacterController.TMGSelectJobInfo _ApplySkillInfo)
//    //public void Initlization(ThreeMatchCharacterController _GetMainController, 
//    //ThreeMatchCharacterController.TMGCalculateInfo _CalculateInfo, ThreeMatchCharacterController.TMGSelectJobInfo _ApplySkillInfo)
//    {
//        m_isProcessAction = false;
//        CheckAllReset();
//        if (m_MainAnimator == null) m_MainAnimator = GetComponent<Animator>();
//        if(m_TMGCharacterController == null) m_TMGCharacterController = _GetMainController;
//        m_MainAnimator.runtimeAnimatorController = Instantiate(_CalculateInfo.spp_AniRuntimeController);
//        if (_CalculateInfo.spp_CharacterStateBase != null)
//        {
//            m_SystemTMGStateBase = Instantiate(_CalculateInfo.spp_CharacterStateBase, this.transform);
//            //m_InputInfo = m_TMGCharacterController.pp_InputController.mspp_InputValues; //Input적용시 사용할것
//            _CalculateInfo.spp_CharacterStateBase.Initlization(this);
//            m_SystemTMGStateBase = _CalculateInfo.spp_CharacterStateBase;
//            switch (m_SystemTMGStateBase)
//            {
//                case PlayerStateOutPut GetPlayerState:
//                    GetPlayerState.Initlization(this, _ApplySkillInfo.s_SkillAndStateInfos); 
//                    break;
//            }
//        }

//        m_isProcessAction = true;
//    }

//    private void CheckAllReset()
//    {
//        if (m_SystemTMGStateBase != null)
//        {
//            Destroy(m_SystemTMGStateBase.gameObject);
//            m_SystemTMGStateBase = null;
//        }
//    }

//    #region Main System Public Functions

//    //현재 상태를 변경
//    public virtual void ChangeState(State _ApplyState)
//    {
//        m_isProcessAction = false;
//        if (m_GetCurrentState == _ApplyState) return;

//        m_GetCurrentState?.Exit();
//        //OnAnimCallbackClear(); //When Anim CallBack 

//        m_GetCurrentState = _ApplyState;
//        m_GetCurrentState?.Enter();
//        //SetAnimState(m_GetCurrentState); //When Anim CallBack 
//        m_isProcessAction = true;
//    }

//    // 현재 재생 중인 애니메이션의 프레임을 반환함
//    public float GetCurrentAnimFrame()
//    {
//        var clipInfo = m_MainAnimator.GetCurrentAnimatorClipInfo(0)[0].clip;
//        var stateInfo = m_MainAnimator.GetCurrentAnimatorStateInfo(0);
//        var time = clipInfo.isLooping ? stateInfo.normalizedTime % 1f : Mathf.Clamp01(stateInfo.normalizedTime);
//        var frame = clipInfo.length * time * clipInfo.frameRate;

//        return frame;
//    }

//    public void PlayAnim(string newState, float percent = 0f)
//    {
//        if (m_AnimStateName == newState) return;

//        //_animator.Play(newState, 0);
//        m_MainAnimator.CrossFade(newState, percent);
//        m_AnimStateName = newState;
//    }

//    #endregion

//    #region Sub System Public Functions

//    // 현재 상태를 확인
//    public bool IsState(State state) => m_GetCurrentState == state;
//    public bool IsAnimPlaying(string name) => m_MainAnimator.IsAnimPlaying(name);
//    public bool IsAnimPlayed(string name) => m_MainAnimator.IsAnimPlayed(name);

//    public Vector2 InputAxis() //InputController로 이전 요망 현재는 테스트용도 (따라서 삭제 요망....)
//    {
//        return Vector2.zero;
//    }

//    public float GetCurrntClipTime() => m_MainAnimator.GetCurrentAnimatorClipInfo(0).Length;

//    //만일 애니가 정상적으로 작동되지 않을때 사용하는 함수들

//    public void CountByCheckProcessNotWork()
//    {
//        m_CheckCountTime += Time.deltaTime;
//        if(m_CheckCountTime < 4f)
//        {
//            m_CheckCountTime = 0f;
//            CheckProcessNotWork();
//        }
//    }

//    public void CheckProcessNotWork()
//    {
//        //if(!m_isProcessAction && m_GetCurrentState != null && m_SystemTMGStateBase.pp_CurrentPlayerState != m_SystemTMGStateBase.pp_DefultPlayerState)
//        //m_SystemTMGStateBase.ChangeState(pp_SystemTMGStateBase.pp_DefultPlayerState);
//    }

//    #endregion

//    #region P Project Reference => AnimCallbackController 메카니즘 참고 (만일 각각의 State Anim Clip에서 CallBack이 존재할시 로직 구성후 사용 요망..)
//    //public AnimCallbackController AddAnimCallback()
//    //{
//    //    var clip = _animator.GetCurrentAnimatorClipInfo(0)[0].clip;
//    //    return _animCallbackController.Init(clip);
//    //}

//    //public void OnAnimCallbackClear() => _animCallbackController.OnExit();

//    // State와 애니메이션이 1:1 매칭이 보장된다면 이 함수를 사용할 필요가 없고, 클립마다 첫 프레임에 트리거 추가 자동화 가능
//    //public void SetAnimState(State newState)
//    //{
//    //    if (newState is not IAddAnimCallback) return;

//    //    _animCallbackController.CurrentState = newState as IAddAnimCallback;
//    //}
//    #endregion

//    #region 유니티 이벤트 함수

//    protected virtual void OnCollisionEnter(Collision collision)
//    {
//        if (!m_isProcessAction || collision.gameObject.layer == LayerMask.NameToLayer("Ground"))// ||
//            //collision.gameObject.GetComponent<EventTriggerSystem>() != null) //이벤트 처리구간은 다른 회픽기동을 사용하기 위함
//            return;

//        //m_PhycisOutPutBase.OnCollistionEnterEvent(collision.gameObject);
//    }

//    protected virtual void OnCollisionEnter2D(Collision2D collision)
//    {
//        if (/*!m_isProcessAction || */collision.gameObject.GetComponent<SpriteRenderer>() == null ||
//            collision.gameObject.GetComponent<SpriteRenderer>().sortingLayerName == "BackGround") return;

//        //m_PhycisOutPutBase.OnCollistionEnterEvent(collision.gameObject);
//    }

//    protected virtual void FixedUpdate()
//    {
//        if (!m_isProcessAction) return;
//        m_GetCurrentState?.StateFixedUpdate();
//    }

//    protected virtual void Update()
//    {
//        if (!m_isProcessAction) return;
//        m_GetCurrentState?.StateUpdate();
//    }

//    protected virtual void LateUpdate()
//    {
//        if (!m_isProcessAction) return;
//        m_GetCurrentState?.StateLateUpdate();
//    }

//    #region 024 Project Reference
//    //void Update()
//    //{
//    //    if (!isActionAniController)
//    //        return;

//    //    if (m_PhycisOutPutBase != null)
//    //    {
//    //        m_MainAnimator.SetFloat("Forword", m_AnimValuesInfo.s_MoveValue.y);
//    //        m_AddRuntimeParermeter.ToList().ForEach(x => x?.Invoke());
//    //    }

//    //    m_MainAnimator.speed = m_AnimSpeedMultiplier;
//    //}

//    //void OnAnimatorMove()
//    //{
//    //    if (!isActionAniController || m_PhycisOutPutBase == null)
//    //        return;

//    //    m_PhycisOutPutBase.OnAniActionMoveEvent();
//    //    m_MainAnimator.speed = m_AnimSpeedMultiplier;
//    //}
//    #endregion

//    #endregion
//}

//public enum CharacterDefultStateType { None = 0, StateOfAppearanceBefore, Idle, IdleCountDown, Walk, Run, Grow, Set, Ground, Jump }

//public abstract class CharacterStateBase : MonoBehaviour
//{
//    [Header("==CharacterStateBase==")]
//    [Header("Required Values")]
//    [SerializeField, ReadOnly] protected StateMachineController m_RunStateMachine;
//    [SerializeField, ReadOnly] protected State m_DefultState;
//    #region TMG Item State적용에 관한 설명
//    //<**m_InteractionStateDic**>는 우선카운트 데로 하지만 정석은 ItemInfo => CompareIDX데로 키값을 넣어야 한다
//    //현재는 중복이 없다는 가정하에 초기화 로직을 구성했다는것을 명심해라
//    #endregion
//    protected readonly Dictionary<CharacterDefultStateType, State> m_CharacterBaseStateDic = new();
//    protected readonly Dictionary<int, CharacterSkillState> m_InteractionStateDic = new(); //Skill과 동일시 보면 되곘다

//    public CharacterDefultStateType pp_DefultPlayerState { get; private set; }
//    public CharacterDefultStateType pp_CurrentPlayerState { get; private set; }

//    public StateMachineController pp_RunStateMachine { get => m_RunStateMachine; }

//    //public CharacterStateBase(StateMachineController _GetAnimControllHeader) //문제 없을시 삭제 요망..
//    public void Initlization(StateMachineController _GetAnimControllHeader)
//    {
//        m_RunStateMachine = _GetAnimControllHeader;
//    }

//    public virtual void ChangeState(CharacterDefultStateType _GetType, bool _isChangeDefultState = false)
//    {
//        if ((!m_CharacterBaseStateDic.ContainsKey(_GetType)).HDebug($"해당 타입은 현재 적용한것에는 있지 않습니다.!", Helper.HDType.Error)) return;
//        pp_CurrentPlayerState = _GetType;
//        if (_isChangeDefultState)
//        {
//            pp_DefultPlayerState = _GetType;
//            m_DefultState = m_CharacterBaseStateDic[_GetType];
//        }
//        ExecuteDefultAction(m_CharacterBaseStateDic[_GetType]);
//    }

//    public virtual void ChangeInteractionState(int _GetKey)
//    {
//        if ((!m_InteractionStateDic.ContainsKey(_GetKey)).
//        HDebug($"해당 타입은 현재 적용한것에는 있지 않습니다.!", Helper.HDType.Warning)) return;
//        ExecuteDefultAction(m_InteractionStateDic[_GetKey]);
//    }
//    #region Main System Protected Functions
//    protected virtual void ExecuteDefultAction(State _GetMacine) => m_RunStateMachine.ChangeState(_GetMacine);
//    public bool IsAnimPlaying(string _CompareAnimStateName) => m_RunStateMachine.IsAnimPlaying(_CompareAnimStateName);
//    public bool IsAnimPlayed(string _CompareAnimStateName) => m_RunStateMachine.IsAnimPlayed(_CompareAnimStateName);

//    #endregion

//    #region Sub System Public Functions

//    public void RemoveAppledSkills(int _ItemIDX)
//    {
//        if (!m_InteractionStateDic.ContainsKey(_ItemIDX)) return;

//        m_InteractionStateDic.Remove(_ItemIDX);
//    }

//    #endregion
//}

//public class State
//{
//    public virtual void Enter() { } //DoNothing
//    public virtual void Exit() { } //DoNothing

//    //업데이트 주문은 인터페이스로 따로 구분 요망..
//    public virtual void StateFixedUpdate() { } //DoNothing
//    public virtual void StateLateUpdate() { } //DoNothing
//    public virtual void StateUpdate() { } //DoNothing
//}

//#region CharacterBase StateMacine Reference

//public class CharacterDefultState : State
//{
//    protected CharacterStateBase m_Owner;

//    public CharacterDefultState(CharacterStateBase _Owner)
//    {
//        m_Owner = _Owner;
//    }
//}
//public class AppearanceBeforeState : CharacterDefultState
//{
//    bool m_isFirst = false;

//    public AppearanceBeforeState(CharacterStateBase _Owner) : base(_Owner) { }

//    public override void Enter()
//    {
//        m_Owner.pp_RunStateMachine.PlayAnim("AppearanceBeforeAnim");
//    }

//    public override void StateUpdate()
//    {
//        base.StateUpdate();

//        if (m_Owner.IsAnimPlayed("AppearanceBeforeAnim") && !m_isFirst)
//        {
//            m_Owner.ChangeState(CharacterDefultStateType.Idle);
//            //EventManager.Instance.Notify(EventListenType.InGameUsed_ShapesManager_ComplateGameStart, new ComplateCountData(1)); //이런식으로 단계를 기다릴 수도 있음
//            m_isFirst = true;
//        }
//    }
//}


//public class IdleState : CharacterDefultState
//{
//    public IdleState(CharacterStateBase _Owner) : base(_Owner) { }

//    public override void Enter() 
//    {
//        m_Owner.pp_RunStateMachine.PlayAnim("CharacterIdleAnim");
//    }

//    //예시 로직
//    public override void StateFixedUpdate()
//    {
//        base.StateFixedUpdate();
//        if (m_Owner.pp_RunStateMachine.InputAxis().magnitude != 0) m_Owner.ChangeState(CharacterDefultStateType.Run);
//    }

//    public override void Exit()
//    {
//        base.Exit();
//    }
//}

//public class WalkState : CharacterDefultState
//{
//    public WalkState(CharacterStateBase _Owner) : base(_Owner) { }

//    public override void Enter()
//    {
//        m_Owner.pp_RunStateMachine.PlayAnim("CharacterWalkAnim");
//    }

//    //...
//}

////public class RunState : CharacterDefultState
////{

////}
//#endregion

//#region Skill StateMacine Reference 

////이후 스크립터블 데이터화 이후에  DataManager<클래스>에 종속될 수 있도록 수정한다 
////데이터화 하기 위해선 스크립터블 상속구조를 이후에 구현해야만 함
////[**리팩토링**] 현재 Shape과 SkillState의 동일 참조로 사용한다 이는 햇갈릴 가능성이 많으며 이후 Skill에서 Shape에게 배출하는 형태로 변경할 수 있도록 한다
//[System.Serializable]
//public class SkillDataInfo : InstanceAnim, I_Data
//{
//    [Header("======SkillData Reference======")]
//    public bool s_isRequiredApplyObj;
//    public bool s_interactionAnim;
//    public SelectJobType s_JobType; //대표가 아니라 소속된 전직이 뭔지 각각마다 할당하기 위함
//    public GameObject[] s_OtherObjs;
//    public string s_ActionAnimSkill;
//}

//public class CharacterSkillState : CharacterDefultState
//{
//    protected readonly SkillDataInfo m_SkillDataInfo;
//    public CharacterSkillState(CharacterStateBase _Owner, SkillDataInfo _GetSkillData) : base(_Owner)
//    {
//        m_SkillDataInfo = _GetSkillData;
//    }

//    public override void Enter()
//    {
//        base.Enter();
//        m_Owner.pp_RunStateMachine.PlayAnim(m_SkillDataInfo.s_ActionAnimSkill);
//    }
//}

////TMG 때의 예시 상속 스크립트
////public class Type0State : CharacterSkillState
////{
////    public Type0State(CharacterStateBase _Owner, SkillDataInfo _GetSkillData) : base(_Owner, _GetSkillData)
////    {

////    }
////}
//#endregion

