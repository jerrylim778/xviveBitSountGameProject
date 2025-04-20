using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using Commons;
using Commons.Helpers;
using Sirenix.OdinInspector;
using System;

public class AnimationController : SubModuleControllerBase, I_SubModulesCollection
{
    [Header("MainAniControllHeader")]
    [SerializeField, Range(0f, 10f)] private float m_AnimSpeedMultiplier = 1f;
    [SerializeField, ReadOnly] private Animator m_MainAnimator;
    [SerializeField, ReadOnly] private InputValueInfo m_InputInfo;
    [SerializeField, ReadOnly] private AnimationControllerMoveModule m_ACMoveModule;
    public AnimValuesInfo m_AnimValuesInfo = null;

    [Header("ProcessValueByMainAniControll")]
    [SerializeField, ReadOnly] private PhycisOutPutBase m_PhycisOutPutBase;
    [Header("AnimController Connect SubController")]
    [field: SerializeField, ReadOnly] public List<SubModuleControllerBase> pp_AddSubControllers { get; private set; }
    
    [Tooltip("System Value Refernce")]
    private List<System.Action> m_AddRuntimeParermeter = new List<System.Action>();
    private bool isActionAniController = false;
    private bool m_isPhycisSample = false;

    public Animator pp_MainAnimator { get => m_MainAnimator; }

    #region 기존 로직 연계 된것 전부 삭제후 해당 로직도 삭제 처리할것
    //1.해당 클래스는 항시 CharacterController<클래스>와 상주하기 때문에
    //처음 초기값을 전부 리셋하고 다시 들어가야한다 (NewPlayerCharacterController<클래스>에 존재하는데 이것도 변경하고 삭제할 수 있도록 한다)
    public void Initlization(CalculateInfo _getCalculInfo)
    {
        isActionAniController = false;
        CheckAllReset();
        if (m_MainAnimator == null) m_MainAnimator = GetComponent<Animator>();        
        m_MainAnimator.runtimeAnimatorController = _getCalculInfo.spp_AniRuntimeController;
        if (_getCalculInfo.spp_PhycisOutPutBase != null)
        {
            m_AnimValuesInfo = new AnimValuesInfo();
            m_PhycisOutPutBase = Instantiate(_getCalculInfo.spp_PhycisOutPutBase, this.transform);
            m_InputInfo = GetComponent<NewPlayerCharacterController>().pp_InputController.mspp_InputValues;
            m_PhycisOutPutBase.Initlization(GetComponent<NewPlayerCharacterController>());
            m_PhycisOutPutBase.ActionPhycis();
            InitSettingByParermeterUpdate(true);
        }
        isActionAniController = true;
    }
    #endregion

    public override object[] Initlization(params object[] _ParsingParams)
    {
        isActionAniController = false; //전부 상속에 의해 변경할 수 있도록 수정할것

        var ApplyParams = base.Initlization(_ParsingParams);
        NewActionInfo GetActionInfo = (NewActionInfo)ApplyParams[0];
        CalculateInfo _getCalculInfo = ApplyControllerInfoData<CalculateInfo>(GetActionInfo.s_RequiredSubModuleParams);
        
        NewCharacterController GetController = m_MainController as NewCharacterController;
        this.transform.SetParent(m_MainController.transform); 
        this.transform.position = Vector3.zero; this.transform.rotation = Quaternion.identity;
        
        CheckAllReset();
        m_ACMoveModule = m_MainController.gameObject.CheckComnectComponent<AnimationControllerMoveModule>();
        m_MainAnimator = m_ACMoveModule.Initlization<CapsuleCollider>(m_MainController as NewCharacterController, this).Item1;
        if (pp_AddSubControllers == null) pp_AddSubControllers = new();
        m_MainAnimator.runtimeAnimatorController = _getCalculInfo.spp_AniRuntimeController;
        if(m_MainAnimator.avatar == null || !m_MainAnimator.avatar.Equals(_getCalculInfo.spp_ApplyAvatar))
        m_MainAnimator.avatar = _getCalculInfo.spp_ApplyAvatar;

        #region Animation Controller Sub ModuleController Init
        if (pp_AddSubControllers.Count > 0)
        {
            pp_AddSubControllers.ForEach(x => 
            {
                x.ExecuteChangeSubController();
                Destroy(x);
            });
            pp_AddSubControllers.Clear();
        }
        _getCalculInfo.spp_AddSMCWithAnimController.HForEach(x =>
        {
            var GetSubController = Instantiate(x, this.transform);
            GetSubController.Initlization(this); //Params는 없고 버퍼형식의 데이터 전달정도로 Model을 대처한다
            pp_AddSubControllers.Add(GetSubController);
        });
        #endregion

        if (_getCalculInfo.spp_PhycisOutPutBase != null)
        {
            m_ACMoveModule.InitRecycle(_getCalculInfo);
            m_AnimValuesInfo = new AnimValuesInfo();
            m_PhycisOutPutBase = Instantiate(_getCalculInfo.spp_PhycisOutPutBase, this.transform);
            m_InputInfo = GetController.I_GetSubModule<BInputController>().mspp_InputValues;
            m_PhycisOutPutBase.Initlization(GetController);
            m_PhycisOutPutBase.ActionPhycis();
            InitSettingByParermeterUpdate(true);
        }
        //m_isActionProcess로 전부 변경해야 한다 <**리팩 요망 ************************************************************>
        if (!m_isTestMode)
        {
            if (m_MainController is I_SubModulesCollection GetComplateSign) GetComplateSign.I_CheckSubModuleIsAllCanAction(this);
            else /*m_isActionProcess = true;*/ isActionAniController = true;
        }
        //m_isActionProcess로 전부 변경해야 한다 <**리팩 요망 ************************************************************>
        return null;
    }

    #region Anim Controller Init Reference

    public override void ExecuteChangeSubController()
    {
        base.ExecuteChangeSubController();
        CheckAllReset();
    }

    private void CheckAllReset()
    {
        if (m_PhycisOutPutBase != null)
        {
            m_PhycisOutPutBase.BreakPoint(false);
            Destroy(m_PhycisOutPutBase.gameObject);
        }
        if (m_PhycisOutPutBase != null) m_PhycisOutPutBase = null;
        if (m_AnimValuesInfo != null) m_AnimValuesInfo = null;
        InitSettingByParermeterUpdate(false);
    }

    private void InitSettingByParermeterUpdate(bool _isInit)
    {
        if (!_isInit) m_AddRuntimeParermeter.Clear();
        else
        {
            m_isPhycisSample = m_PhycisOutPutBase is PhycisOutPutSample;
            #region 물리적 선택에 의한 작업들
            if (m_PhycisOutPutBase.CalculationPhycisList.Exists(x => x is Rotate_Phycis))
            {
                Rotate_Phycis AniTurnAngle =
                (Rotate_Phycis)m_PhycisOutPutBase.CalculationPhycisList.Find(x => x is Rotate_Phycis);
                m_AddRuntimeParermeter.Add(() => m_MainAnimator.SetFloat("Turn",
                Mathf.Lerp(m_MainAnimator.GetFloat("Turn"), AniTurnAngle.GetAngleByRotate(), Time.deltaTime * 5f)));
                m_AddRuntimeParermeter.Add(() => m_MainAnimator.SetFloat("Right", m_AnimValuesInfo.s_MoveValue.x));
                m_AddRuntimeParermeter.Add(() => m_MainAnimator.SetBool("IsStrafing", m_AnimValuesInfo.s_isStrafing));
            }

            if (m_PhycisOutPutBase.CalculationPhycisList.Exists(x => x is Ground_Phycis))
                m_AddRuntimeParermeter.Add(() => { m_MainAnimator.SetBool("isGround", m_AnimValuesInfo.s_isGround);});
            #endregion
            #region (0 Layer만 해당) 단순 상태 변환과 관련한 작업들
            if (m_MainAnimator.parameters.ToList().Exists(x => x.name == "StateModeIDX"))
            m_AddRuntimeParermeter.Add(() => m_MainAnimator.SetInteger("StateModeIDX", m_AnimValuesInfo.s_ChangeStateIDX));
            #endregion
            #region Layer에 대한 작업들
            #region 레이어와 관련된 설명
            //레이어가 1 이상이면 단일로 실행되는 이벤트형태의 레이어 동작들
            //이벤트 2이상이면 Phycis 레이어를 추가하는 방식으로 적용된다
            //오직 이벤트 (추가적인 레이어 이벤트는 서브 레이어 패턴으로 빼서 재설계 하자)
            #endregion
            //if (m_MainAnimator.layerCount > 1)//이건 임시 따라서 후에 재설계 요망***
            //{
            //    if(m_MainAnimator.GetLayerWeight(1) < 1) m_MainAnimator.SetLayerWeight(1, 1);
            //    m_AddRuntimeParermeter.Add(() => 
            //    { if (m_InputInfo.s_isLayer1EventAction) m_MainAnimator.SetTrigger("TriLayer1Event1");});
            //}
            //if (m_MainAnimator.layerCount > 2) 
            //m_AddRuntimeParermeter.Add(() =>
            //{
            //    //m_MainAnimator.SetLayerWeight(2, m_MainAnimator.GetInteger("Layer2EventIDX") != 0 &&
            //    //m_MainAnimator.GetLayerWeight(1) < 1 ? 1 : 0);
            //    //물리적으로 어떤 지점으로 이동과 회전을 준다음 해당 이벤트를 발동해야될꺼 같다
            //    m_MainAnimator.SetInteger("Layer2EventIDX", m_InputInfo.s_isLayer2EventIDX);
            //});
            ////if (m_MainAnimator.layerCount > 3){ } //물리적 계산이 필요한 레이어 (따라서 서브 파티(Composite 등등)방식으로 활용시 재설계가 필요하다
            #endregion
        }
    }

    #endregion

    #region Sub System Public Functions

    public override void BreakPoint(bool _isBreak, SubModuleBreakType _BrackType = SubModuleBreakType.BreakAll, Type _GetType = null)
    {
        if (m_PhycisOutPutBase != null) m_PhycisOutPutBase.BreakPoint(_isBreak);
        if (m_PhycisOutPutBase is PhycisOutPutDefult) m_MainAnimator.SetFloat("Forword", 0f);
        else if (m_PhycisOutPutBase is PhycisOutPutSample) m_MainAnimator.SetBool("IsAction", false);
        if (_BrackType == SubModuleBreakType.BreakAll)
        {
            base.BreakPoint(_isBreak, _BrackType, _GetType);
            isActionAniController = _isBreak; //m_isActionProcess로 변경 이후 삭제 요망 ************************************************************        
            return;
        }
    }

    public void SetAnimWeight(int _LayerIDX, float _GetWeight)
    {
        if ((_LayerIDX >= pp_MainAnimator.layerCount).HDebug(
        "현재 레이어보다 높은 수의 Control을 요구합니다 이는 있을 수 없습니다.!", Helper.HDType.Error)) 
        return;
        
        m_MainAnimator.SetLayerWeight(_LayerIDX, _GetWeight);
    }

    #endregion

    #region Sub System Public Functions (**Interface Reference**) [보류]

    //후에 반드시 구현하여 체킹할것
    public void I_CheckSubModuleIsAllCanAction(ControllerBase _GetSubBase)
    {
        
    }
    public T I_GetSubModule<T>() where T : SubModuleControllerBase
    => ISCheckSubModule<T>(out T _OutPutBase) ? _OutPutBase : null;

    public bool ISCheckSubModule<T>(out T _OutPutBase) where T : SubModuleControllerBase
    {
        _OutPutBase = null; int FIDX = -1; FIDX =
        pp_AddSubControllers.HFindIndex(x => x.GetType().Name == typeof(T).Name);
        if (FIDX != -1)
        {
            _OutPutBase = pp_AddSubControllers[FIDX] as T;
            return true;
        }
        return false;
    }

    #endregion

    #region 유니티 이벤트 함수

    protected override void Update() //ProcessUpdate로 변경 상위객체에서 컨트롤할 수 있도록 수정한다
    {
        if (!isActionAniController)
            return;

        if (m_PhycisOutPutBase != null)
        {
            if(!m_isPhycisSample) m_MainAnimator.SetFloat("Forword", m_AnimValuesInfo.s_MoveValue.y);
            else m_MainAnimator.SetBool("IsAction", m_AnimValuesInfo.s_MoveValue.magnitude != 0);
            m_AddRuntimeParermeter.ToList().ForEach(x => x?.Invoke());
        }

        m_MainAnimator.speed = m_AnimSpeedMultiplier;
    }

    public void UpdateOnAnimatorMove() // void OnAnimatorMove()
    {
        if (!isActionAniController || m_PhycisOutPutBase == null)
            return;

        m_PhycisOutPutBase.OnAniActionMoveEvent();
        m_MainAnimator.speed = m_AnimSpeedMultiplier;
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer("Ground"))// ||
            //collision.gameObject.GetComponent<EventTriggerSystem>() != null) //이벤트 처리구간은 다른 회픽기동을 사용하기 위함
            return;

        //m_PhycisOutPutBase.OnCollistionEnterEvent(collision.gameObject);
    }

    #endregion
}

//[System.Serializable]
public class AnimValuesInfo
{
    public Vector2 s_MoveValue;
    public bool s_isGround;
    public bool s_isJump;
    public bool s_isStrafing;
    //public float s_AirTime;
    public float s_Yvalocity;

    public int s_ChangeStateIDX;
}

