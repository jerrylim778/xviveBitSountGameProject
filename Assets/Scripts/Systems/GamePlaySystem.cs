using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using Commons.Helpers;
using Sirenix.OdinInspector;

[DisallowMultipleComponent]
public class GamePlaySystem : SingleTon<GamePlaySystem>, I_StartControllerBase
{
    #region ODIN Support Reference
    public bool CheckOdinToggle() => m_RatioPopUpTr != null || m_RatioRequiredPopUpTr != null;
    #endregion
    [Header("Control Force GameSystem Reference")]
    [SerializeField] private bool m_isTestMode = false;
    [Header("Using System Now")]
    [SerializeField, ReadOnly] SystemInfoType m_MainStateType = SystemInfoType.NoneMainGUI;
    [SerializeField, ReadOnly] private SystemInfo m_SystemSectionNow;
    [SerializeField] private List<SystemInfo> m_AllSystemSection;
    [Header("Main GamePlaySystem Values")]
    [SerializeField, ReadOnly] public Canvas m_UsingMainCan;
    
    [SerializeField] private Transform m_GUITr, m_WPTr;
    [SerializeField] private Transform m_RatioPopUpTr, m_SpecialPopUpTr, m_RatioRequiredPopUpTr, m_RatioAspectOutTr;
    [SerializeField, ReadOnly, ShowIf(nameof(CheckOdinToggle))] private Transform m_PopUpTr, m_RequiredPopUpTr;
    [SerializeField] private SystemBase[] m_PrefabsAllSystemBases;

    [Header("Init Start Point")]
    [field: SerializeField] public Transform pp_StartCamTr { get; private set; }
    [field: SerializeField] public Transform pp_StartCharacterTr { get; private set; }
    [field: SerializeField] public RectTransform testpp_AllIncludedGUIPanel { get; private set; } //GUI중 제일 앞에있는 객체 (LoadingPopUp등으로 이후에 대처할것)

    [Tooltip("Required GamePlaySystem Values")]
    private bool m_isMainSystemActions = false;
    private Camera m_UseTitleCam;
    private NewPlayerCharacterController m_MainPlayer;

    [Tooltip("PopUpSubSystemReference")]
    private Stack<SystemBase> m_StackPopUp;
    //private System.Func<PopUpType, bool> m_PushReCallBack = null;

    //Property
    public Canvas pp_UsingMainCan { get => m_UsingMainCan;}
    public Transform testpp_PopUpTr { get => m_PopUpTr; } //패쇠성 로직이기 때문에 이후에 반드시 삭제후 캡슐화 형태로 호출 요망 ****

    public void Initlization(SystemInfoType? _StartType = null)
    {
        m_StackPopUp = new Stack<SystemBase>();
        m_MainPlayer = FindObjectsOfType<NewPlayerCharacterController>().ToList().Find(x => x.pp_isMine);
        m_UseTitleCam = Camera.main.gameObject.activeSelf ? Camera.main : FindObjectOfType<CameraController>().GetComponent<Camera>();
        if (pp_StartCamTr != null) pp_StartCamTr.SyncObjPosRot(m_UseTitleCam.transform);
        #region Ratio Init
        m_UsingMainCan = this.transform.ChildLinearStuctureSearch<Canvas>()[0];
        Vector2 ScreenSizeNow = Appinstance.Instance.ms_DataManager.pp_RatioApply;
        m_UsingMainCan.GetComponent<CanvasScaler>().referenceResolution = ScreenSizeNow;
        if(m_GUITr is RectTransform GetGUIRT) GetGUIRT.sizeDelta = ScreenSizeNow;
        if(m_RatioPopUpTr is RectTransform GetPopUpRT) GetPopUpRT.sizeDelta = ScreenSizeNow;
        if (m_SpecialPopUpTr is RectTransform GetSDPopUpRT) GetSDPopUpRT.sizeDelta = ScreenSizeNow;
        if (m_RatioRequiredPopUpTr is RectTransform GetRPopUpRT) GetRPopUpRT.sizeDelta = ScreenSizeNow;
        if(m_RatioAspectOutTr is RectTransform GetAspectOutRT) GetAspectOutRT.sizeDelta = ScreenSizeNow;
        #endregion
        #region SystemBase Init
        //PopUp Init
        m_PopUpTr = m_RatioPopUpTr.GetChild(0).GetComponent<RectTransform>();
        m_RequiredPopUpTr = m_RatioRequiredPopUpTr.GetComponent<RectTransform>();
        m_PopUpTr.gameObject.SetActive(false); 
        //Main Process Init
        List<SystemBase> GetAllSystemBase = new();
        m_AllSystemSection.ForEach(x => 
        { 
            if(x.s_MainSystemBase is not SystemWorldBase)
            x.s_MainSystemBase.gameObject.SetActive(false); 
            GetAllSystemBase.Add(x.s_MainSystemBase);
        });
        #region Check Duplicate
        GetAllSystemBase.AddRange(FindObjectsOfType<SystemBase>());
        GetAllSystemBase.FindAll(x => x.pp_isMainType).ForEach(x =>
        {
            if ((x is I_PopUpInfo).HDebug($"{x.GetType().Name}은 팝업에 해당하며 오르지 동적으로만 호출 가능합니다.!")) return;
            SystemBase GetSubBase = x.ModuleInitlization(m_MainPlayer, this, m_UseTitleCam);
            CheckAndSettingBases(x, GetSubBase);
            //중복되어을때 넣고 없다면 추가할 수 있도록 (MainGUI는 한번 추가하면 사라지지 않기 때문에)
            if (CheckDuplicateInfos(x, out int _ListIDX)) 
            m_AllSystemSection[_ListIDX] = new SystemInfo(x.pp_SystemInfoType, x, GetSubBase);
            else m_AllSystemSection.Add(new SystemInfo(x.pp_SystemInfoType, x, GetSubBase));
        });
        #endregion

        if (m_AllSystemSection.Count <= 0 && m_SystemSectionNow.s_MainSystemBase == null)
        {
            m_MainStateType = SystemInfoType.NoneMainGUI;
            m_isMainSystemActions = true;
            return;
        }
        m_MainStateType = _StartType != null? _StartType.Value : SystemInfoType.NoneMainGUI;
        m_SystemSectionNow = m_AllSystemSection[_StartType == null ?
        0 : m_AllSystemSection.ToList().FindIndex(x => x.s_SystemInfoType == _StartType.Value)];
        m_AllSystemSection.ToList().ForEach(x => GetCompareByBasesType(x, true)?.gameObject.SetActive(false));
        ExecuteOutPutSystemBase(m_SystemSectionNow.s_SystemInfoType);
        #endregion
    }

    #region System GUI PopUp

    //Require일 경우 MainSteam보다 먼저 들어가 있어야한다
    //MainStream보다 위에 있어야 한다 또한 PopUp은 기존 MainStream에 해당되는 구조체에 넣을 수 없다는걸 명심해야한다
    public T ExecutePopUpStack<T>(I_Data _ApplyAddPopUpData = null) where T : I_PopUpInfo /*, System.Type _ApplyPopUpType = null*/
    {
        SystemBase GetPopUpStack = CheckInstancePopUp<T>(_ApplyAddPopUpData);
        PopUpType GetPopUpType = (GetPopUpStack as I_PopUpInfo).I_GetPopUpType();

        #region 각 타입에 따른 정의 (팝업을 생성한 뒤에 후처리 관련)
        switch (GetPopUpType)
        {
            #region 나머지 타입은 후처리할게 없으며 사용안할시 삭제 요망 **
            //case PopUpType.NotUseMainPopUpStack: //이후에 초기화시 사용할 수 없는것을 검사할 수 있는 로직도 초기화 시 구현 요망 **
            //    Debug.LogErrorFormat("해당 팝업은 메인 팝업 로직에서 사용할 수 없는 팝업입니다.!", GetPopUpStack.name);
            //    return default(T);
            //case PopUpType.InsideGUISubPartType: //사용안할경우 삭제할것
            //    #region 이전에 시도했던 작업들 CheckInstancePopUp으로 이전함 (이후 삭제할것)
            //    //************************************************
            //    //갔다와서 관련된 타입이 현재 GamePlaySystem 관리 팝업에 존재하는지의 여부
            //    //있다면 해당 팝업에 포함시키는 오버로딩 인터페이스 함수 or 해당 팝업 초기화 이후에 초기화 매개변수에 전달
            //    //************************************************
            //    //if (_ApplyPopUpType != null)
            //    //{
            //    //    if ((!CheckStackPopUpByType(_ApplyPopUpType, out SystemBase _FindBase).HDebug($"{_ApplyPopUpType.Name}타입이 반드시 존재해야 " +
            //    //    $"{typeof(T).Name}을 사용할 수 있습니다.!", Helper.HDType.Error))) return default(T);
            //    //    if (_FindBase is I_ApplySOPopUpToHPopUp GetASPTP) GetASPTP.I_ApplySubPopUp(GetPopUpStack as ForceSubOrderPopUp);
            //    //    break;
            //    //}
            //    //if((_SubTr == null).HDebug($"{PopUpType.InsideGUISubPartType} " +
            //    //$"타입에서는 매개변수로 받는 _SubTr을 필수적으로 적용해야 합니다", Helper.HDType.Error)) return false;
            //    //(GetPopUpStack.transform as RectTransform).anchoredPosition = Vector3.zero;
            //    #endregion
            //    break;
            #endregion
            case PopUpType.SpecialDefultPopUp: //따로 적용할것이 있다면 해당 타입에서 적용할 수 있도록 수정한다
                m_StackPopUp.Push(GetPopUpStack);
                break;
            case PopUpType.DefultPopUp:
                int FIDX = Helper.ChildLinearStuctureSearch(m_PopUpTr.parent).ToList().FindIndex(x => x.name == m_PopUpTr.name);
                if (!m_PopUpTr.gameObject.activeSelf) m_PopUpTr.gameObject.SetActive(true);
                else if (m_PopUpTr.gameObject.activeSelf || FIDX < m_PopUpTr.parent.childCount - 1)
                    m_PopUpTr.parent.GetChild(FIDX++).SetSiblingIndex(FIDX--);
                m_StackPopUp.Push(GetPopUpStack);
                break;
            case PopUpType.RequiredPopUp:
                Stack<SystemBase> GetSubStack = new Stack<SystemBase>();
                while (m_StackPopUp.Count > 0 && 
                (m_StackPopUp.Peek() as I_PopUpInfo).I_GetPopUpType() != PopUpType.RequiredPopUp)
                {
                    if ((m_StackPopUp.Peek() as I_PopUpInfo).I_GetPopUpType() != PopUpType.RequiredPopUp)
                        GetSubStack.Push(m_StackPopUp.Pop());
                }
                while (GetSubStack.Count > 0) 
                    m_StackPopUp.Push(GetSubStack.Pop());
                m_StackPopUp.Push(GetPopUpStack);
                break;
            //default: //다른 타입중 해당 팝업에 들어가면 안되는 타입이 존재하기에 아래 와같이 기본형으로 넣을 수 없다
            //    m_StackPopUp.Push(GetPopUpStack);
            //    break;
        }
        #endregion
        GetPopUpStack.ModuleInitlization(m_MainPlayer, this);
        return GetPopUpStack.GetComponent<T>();
    }

    #region GUI PopUp Main System Public Functions

    public void MainPopUpPush<T>(/*System.Action _AddCallBack = null*/) where T : I_PopUpPush
    {
        SystemBase GetObj = m_StackPopUp.Peek();
        if (GetObj is I_PopUpDecorDem SetDem) SetDemPopUpTr(SetDem.I_SetDem());
        else SetDemPopUpTr();
        if ((GetObj is not T).HDebug($"{typeof(T).Name} 는 현재 대기중인 팝업과 일치하지 않습니다.!", Helper.HDType.Error)) return;
        #region Push때 값 비교하기 (논리 오류로 인해 보류 => 이후 삭제 요망..)
        //if (!m_PushReCallBack.Invoke((GetObj as I_PopUpInfo).I_GetPopUpType()))
        //{
        //    m_PushReCallBack = null;
        //    return;
        //}
        //m_PushReCallBack = null;
        #endregion
        (GetObj.GetComponent<T>() as I_PopUpPush).PushOutEvent(/*_AddCallBack*/);
    }

    public void MainPopUpPop<T>(System.Action _EndCallBack = null, bool _isTestMode = false) where T : I_PopUpPop
    {
        SystemBase GetObj = m_StackPopUp.Count <= 0? null : m_StackPopUp.Pop();

        if (GetObj == null || GetObj is not T || typeof(T).Name != GetObj.GetType().Name)
        {
            string ErrorMessage = string.Format("잘못된 PopUp기능 사용입니다 클래스 {0} I_PopUpPop 가 있는지 확인하세요", typeof(T).Name);
            if (_isTestMode) Debug.LogWarning(ErrorMessage);
            else Debug.LogError(ErrorMessage);
            return;
        }
        (GetObj.GetComponent<T>() as I_PopUpPop).PopOutEvent(() => 
        {
            Destroy(GetObj.gameObject);
            if ((GetObj as I_PopUpInfo).I_GetPopUpType() != PopUpType.DefultPopUp)
            { _EndCallBack?.Invoke(); return; }
            if (m_StackPopUp.ToList().FindAll(x => (x as I_PopUpInfo).
            I_GetPopUpType() == PopUpType.DefultPopUp).Count == 0) m_PopUpTr.gameObject.SetActive(false);
            #region MainStreamBaseType 또한 PopUp의 일순위로 들어갈때 사용한다
            //if (m_StackPopUp.Count == 0 && m_StackPopUp.Peek().pp_PopUpType != PopUpType.NotPopUp)
            //    m_PopUpTr.gameObject.SetActive(false);
            //else if (m_StackPopUp.Count <= 1 && m_StackPopUp.Peek().pp_PopUpType == PopUpType.NotPopUp)
            //{
            //    m_PopUpTr.gameObject.SetActive(false);
            //    CheckNotGUIFinder();
            //}
            #endregion
            else
            {
                int FIDX = Helper.ChildLinearStuctureSearch(m_PopUpTr.parent).ToList().
                FindIndex(x => x.name == m_StackPopUp.Peek().name);
                m_PopUpTr.SetSiblingIndex(FIDX--);

                if (m_StackPopUp.Peek() is I_PopUpDecorDem SetDem) SetDemPopUpTr(SetDem.I_SetDem());
                else SetDemPopUpTr();
            }
            _EndCallBack?.Invoke();
        });
    }
    #endregion

    #region GUI PopUp Sub System Functions

    private SystemBase CheckInstancePopUp<T>(I_Data _ApplyAddPopUpData = null) where T : I_PopUpInfo
    {
        System.Predicate<SystemBase> FindCondition =
        x => x.pp_SystemInfoType == SystemInfoType.PopUpStack && x.GetComponent<T>() != null;
        #region 추가된 Add Type에 따라 다른 반환에 관한 로직 
        //(로직을 분산하여 기존 ExecutePopUpStack에서 나머지 로직을 처리할 가능성이 존재 (리팩여부 판단및 수정요망)
        if (_ApplyAddPopUpData != null)
        {
            var GetCommonPrefabs = m_PrefabsAllSystemBases.ToList().FindAll(x => 
            x.pp_SystemInfoType == SystemInfoType.PopUpStack && x.GetComponent<T>() != null);
            switch (_ApplyAddPopUpData)
            {
                case AddPopUpinfo DefultData: //존재할 가능성이 있음
                    break;
                case SubOrderAddPopUpInfo SubOrderData:
                    #region Check Is SubPopUp
                    if ((!CheckStackPopUpByType(SubOrderData.s_DependentType,
                    out SystemBase _GetDPopUp) && _GetDPopUp is I_ControlSubPopUpInfo).HDebug($"SubOrderPopUp인 {SubOrderData.s_AbsSubOderPartName} 은 " +
                    $"{SubOrderData.s_DependentType}와 해당 객체속 {nameof(I_ControlSubPopUpInfo)} 상속이 필히 존재해야합니다.!", Helper.HDType.Error)) return null;
                    #endregion
                    //이미 존재하고 있다면 관련된 타입에서 탐색 후 반환
                    var GetSubPopUp = _GetDPopUp as I_ControlSubPopUpInfo;
                    if (GetSubPopUp.I_GetAllSubPopUps().ISArrayFindCondition<ForceSubOrderPopUp>(x =>
                    x.name == SubOrderData.s_AbsSubOderPartName, out ForceSubOrderPopUp GetSPopUp))
                    return GetSPopUp;
                    //없다면 종속할 팝업객체에 전달
                    var GetPrefabBySubPopUp = GetCommonPrefabs.Find(x => x.name == SubOrderData.s_AbsSubOderPartName);
                    var InstancePopUp = Instantiate(GetPrefabBySubPopUp, _GetDPopUp.transform) as ForceSubOrderPopUp;
                    InstancePopUp.gameObject.name = InstancePopUp.gameObject.name.ReplaceRemoveStrValue("(Clone)");
                    return GetSubPopUp.I_ApplySubPopUp(InstancePopUp);
            }
        }
        #endregion
        SystemBase ComparePopUpBase = m_PrefabsAllSystemBases.HFind(x => FindCondition(x));
        I_PopUpInfo GetPopUpInfo = ComparePopUpBase as I_PopUpInfo;
        PopUpType GetPopUpType = GetPopUpInfo.I_GetPopUpType();
        SystemBase GetPopUpStack = Instantiate(ComparePopUpBase,
        GetPopUpType == PopUpType.DefultPopUp ? m_PopUpTr.parent :
        GetPopUpType == PopUpType.SpecialDefultPopUp ? m_SpecialPopUpTr :
        GetPopUpType == PopUpType.RequiredPopUp ? m_RequiredPopUpTr : null);
        return GetPopUpStack;
    }

    public bool CheckStackPopUpByType(System.Type _FindType, out SystemBase _OutPopUp)
    {
        _OutPopUp = null;
        if (m_StackPopUp.Count <= 0) return false;
        _OutPopUp = m_StackPopUp.ToList().Find(x => 
        x.name.ReplaceRemoveStrValue("(Clone)") == _FindType.Name);
        return _OutPopUp != null;
    }
    
    public T FindStackPopUpByTemp<T>() where T : SystemBase
    {
        if (m_StackPopUp.Count <= 0) return null;
        SystemBase GetGUIPopUpBase = m_StackPopUp.ToList().Find(x =>
        x is T && x is I_PopUpInfo && ((x is I_PopUpPush) || (x is I_PopUpPop)));
        return GetGUIPopUpBase != null? GetGUIPopUpBase as T : null;
    }

    public SystemBase[] FindAllByPopUpInfoType(PopUpType _CompareType)
    => m_StackPopUp.ToList().FindAll(x => (x as I_PopUpInfo).I_GetPopUpType() == _CompareType).ToArray();

    private void SetDemPopUpTr(float _ChangeValue = 1)
    {
        var GetDemCG = m_PopUpTr.gameObject.CheckComnectComponent<CanvasGroup>();
        GetDemCG.alpha = _ChangeValue;
        //var GetDem = m_PopUpTr.GetComponent<UnityEngine.UI.Image>();
        //var GetColor = GetDem.color; GetColor.a = _ChangeValue;
        //GetDem.color = GetColor;
    }

    #endregion

    #region GUI PopUp Sub System Public Functions (**Only Use Required PopUp Reference**)

    public void RPopUpControllerBrackPoint(bool _isBreak,
    SubModuleControllerBase.SubModuleBreakType _BrackType = default, System.Type _GetType = null)
    {
        var GetAllRPopUp = FindAllByPopUpInfoType(PopUpType.RequiredPopUp).OfType<SystemBaseGUIPopUpRequired>();
        GetAllRPopUp.HForEach(x => x.BreakPoint(_isBreak, _BrackType, _GetType));
    }

    #endregion

    #region MainStreamBaseType 또한 PopUp의 일순위로 들어갈때 사용한다
    //private void CheckNotGUIFinder()
    //{
    //    //현재 마지막으로 남아있는 Stack이 다시 원래의 m_GameSystemInfoNow에 지정된다
    //    //만약 0이라면 있는것중 켜저있는 GUI가 제어권을 쥐게 된다
    //    //그것도 없다면 NonGUIState 상태를 유지한다
    //    if(m_GameSystemInfoNow == null || !m_GameSystemInfoNow.Value.s_GetGUIBase.Equals(GetBase))
    //    {

    //    }
    //}
    #endregion

    #endregion

    #region Main Process System Functions
    public void ExecuteOutPutSystemBase(SystemInfoType _currType)
    {
        m_isMainSystemActions = false;

        SystemInfo BeforeProcess = m_SystemSectionNow;
        int FIDX = -1; FIDX = m_AllSystemSection.HFindIndex(x => x.s_SystemInfoType == _currType);
        if(_currType != SystemInfoType.NoneMainGUI)
        m_SystemSectionNow = FIDX != -1 ? m_AllSystemSection[FIDX] : CreateNewSystemBase(_currType).Value;

        if (!CheckDuplicateInfos(m_SystemSectionNow.s_MainSystemBase, out int _ListIDX) || (_currType == SystemInfoType.PopUpStack).HDebug(
        $"팝업관련 SystemBase는 메인 프로세스에서 활용할 수 없습니다..! [{m_SystemSectionNow.s_MainSystemBase.GetType().Name}]", Helper.HDType.Error))
            return;

        if (BeforeProcess.s_MainSystemBase != null)
        {
            if (BeforeProcess.s_SubSystemBase != null) BeforeProcess.s_SubSystemBase.ExecuteNextStep(_currType);
            GetCompareByBasesType(BeforeProcess, true)?.gameObject.SetActive(false);
        }

        if (_currType == SystemInfoType.NoneMainGUI || m_SystemSectionNow.s_MainSystemBase == null)
        {
            m_SystemSectionNow = new SystemInfo();
            m_MainStateType = SystemInfoType.NoneMainGUI; 
            m_isMainSystemActions = true; 
            return;
        }

        //SystemBase GetRunNextBase = GetCompareByBasesType(m_SystemSectionNow, true);
        m_SystemSectionNow.s_MainSystemBase.gameObject.SetActive(true);
        m_SystemSectionNow.s_MainSystemBase.Initlization();

        SystemWorldBase GetSystemWP = GetCompareByBasesType(m_SystemSectionNow, false) as SystemWorldBase;
        if (GetSystemWP != null) WorldPosStateBeforeEntry(GetSystemWP, BeforeProcess);

        m_isMainSystemActions = true;
    }

    public SystemBase FindModuleMainOrSubNow(SystemBase _GetBase)
    {
        if (m_SystemSectionNow.s_MainSystemBase == null) return null;
        if (m_SystemSectionNow.s_MainSystemBase.Equals(_GetBase)) return m_SystemSectionNow.s_SubSystemBase;
        if (m_SystemSectionNow.s_SubSystemBase.Equals(_GetBase)) return m_SystemSectionNow.s_MainSystemBase;
        return null;
    }

    public T FindModuleByTemp<T>() where T : SystemBase
    => m_AllSystemSection.Find(x => x.s_MainSystemBase is T /*|| x.s_SubSystemBase is T*/).s_MainSystemBase as T;

    private void WorldPosStateBeforeEntry(SystemWorldBase _GetSubBase, SystemInfo _BeforeProcess)
    {
        if (_GetSubBase.CheckCanLerpCamByWorldPos(m_UseTitleCam.transform.position))
        _GetSubBase.SetCamMoveThisPoint(m_UseTitleCam, _BeforeProcess.s_SystemInfoType, () => _GetSubBase.Initlization());
    }

    private SystemInfo? CreateNewSystemBase(SystemInfoType _CreateType)
    {
        if (_CreateType == SystemInfoType.NoneMainGUI || (_CreateType == SystemInfoType.PopUpStack).HDebug(
        $"팝업관련 SystemBase는 메인 프로세스에서 활용할 수 없습니다..!", Helper.HDType.Error))
        return null;
        SystemBase GetMainBase =
        m_PrefabsAllSystemBases.ToList().Find(x => x.pp_SystemInfoType == _CreateType && x.pp_isMainType);
        SystemBase GetSubBase = GetMainBase.ModuleInitlization(m_MainPlayer, this, m_UseTitleCam);
        CheckAndSettingBases(GetMainBase, GetSubBase);
        return new SystemInfo(GetMainBase.pp_SystemInfoType, GetMainBase, GetSubBase);
    }

    #endregion

    #region Sub System Private Functions

    private void CheckAndSettingBases(SystemBase _MainSsytem, SystemBase _SubSystem)
    {
        if (_SubSystem == null) return;
        if ((CheckSubModule(_MainSsytem, _SubSystem)).HDebug(
        $"{_MainSsytem.name} 지점이 Sub와 똑같은 ModuleType입니다 이는 될 수 없습니다..!!")) return;
        ApplySystemBase(_MainSsytem);
        ApplySystemBase(_SubSystem);
    }

    private void ApplySystemBase(SystemBase _GetBases)
    {
        Transform GetParentTr = _GetBases is SystemWorldBase ? m_WPTr : m_GUITr;
        if (!Helper.ChildLinearStuctureSearch(GetParentTr).ToList().
        Exists(x => x.GetComponent<SystemBase>().Equals(_GetBases)))
        _GetBases.transform.SetParent(GetParentTr);
    }

    private SystemBase GetCompareByBasesType(SystemInfo _GetInfo, bool _isGUIType) //매개변수를 변경하여 가독성을 높일 수 있도록 수정한다
    {
        if (_GetInfo.s_SubSystemBase == null)
        {
            if ((_isGUIType && !(_GetInfo.s_MainSystemBase is SystemWorldBase)) ||
            (!_isGUIType && _GetInfo.s_MainSystemBase is SystemWorldBase)) return _GetInfo.s_MainSystemBase;
            return null;
        }
        if(_isGUIType) 
        return _GetInfo.s_MainSystemBase is SystemWorldBase ? _GetInfo.s_SubSystemBase : _GetInfo.s_MainSystemBase;
        else return _GetInfo.s_MainSystemBase is SystemWorldBase ? _GetInfo.s_MainSystemBase : _GetInfo.s_SubSystemBase;
    }

    private bool CheckDuplicateInfos(SystemBase _CompareBase, out int _ListIDX)
    {
        _ListIDX = -1; _ListIDX = m_AllSystemSection.HFindIndex(x => x.s_MainSystemBase.Equals(_CompareBase));
        return _ListIDX != -1;
    }

    private bool CheckSubModule(SystemBase _KeyValue, SystemBase _CompareValue)
    => (_KeyValue is SystemBase && _CompareValue is SystemBase) ||
    (_KeyValue is SystemWorldBase && _CompareValue is SystemWorldBase);

    #endregion

    #region 유니티 이벤트 함수

    //각 맵이나 진행에 따라 시작타입을 모듈형태로 넣을 수 있음 (또한 해당 값도 무조건 DataManager에서 가져올 수 있도록
    //게임 전체 BGM도 향후 System에서 관리할것
    public void SC_Start()
    {
        DataManager GetManager = Appinstance.Instance.ms_DataManager;
        //씬메니져를 따로둬서 해당 객체에서 검사하는 방식으로 변경해야됨 (변경이후 삭제 요망..)
        if (GetManager.pp_SceneType == SetSceneType.IntroScene || 
        GetManager.pp_SceneType == SetSceneType.TitleScene) Initlization(SystemInfoType.Title_Enter);
        else Initlization();
    }

    private void Start()
    {
        if (m_isTestMode) Initlization();
    }

    private void Update()
    {
        if (m_StackPopUp == null) return; //초기화가 안된상태로 간주
        
        m_StackPopUp.ToList().ForEach(x => x.UpdateProcess());

        if (!m_isMainSystemActions) return;

        //전체를 돌리는건 마찬가지지만 변경될 순간에는 전체 Action을 손을본다
        m_AllSystemSection.ForEach(x =>
        {
            if (x.s_MainSystemBase.pp_isPartControllSystem) x.s_MainSystemBase.UpdateProcess();
            if (x.s_SubSystemBase != null &&
            x.s_SubSystemBase.pp_isPartControllSystem) x.s_SubSystemBase.UpdateProcess();
        });
    }
    #endregion

}

#region 기존 인게임 체계 추상 클래스들

public abstract class SystemBase : MonoBehaviour
{
    [Header("Odin Show ReadOnly")]
    [SerializeField] protected bool odin_isShowReadOnly = false;
    [Header("Required SystemBase Value")]
    [SerializeField] protected SystemInfoType m_SystemInfoType;
    [SerializeField, ShowIf(nameof(ISODIN_ToggleIFCheckType))] private SystemBase m_ConnectBase;
    [SerializeField, ShowIf(nameof(ISODIN_ToggleFalse))] protected bool m_isConnectBaseListen = false;
    [SerializeField, ReadOnly, ShowIf(nameof(ISODIN_ToggleFalse))] protected SystemBase m_InstanceBase = null;
    #region ODINShowIF => InstanceBase
    private bool ISODIN_ToggleIFCheckType() => m_SystemInfoType != SystemInfoType.PopUpStack && m_SystemInfoType != SystemInfoType.NoneMainGUI;
    private bool ISODIN_ToggleFalse() => m_ConnectBase != null;
    #endregion

    [Tooltip("Main Values")]
    protected GamePlaySystem m_MainControllSystem;
    protected NewPlayerCharacterController m_MainPlayer; //이거 MainController로 변경 리팩 요망 ****************
    protected ControllerBase m_MainController; 
    protected bool m_isPartControllSystem = false;
    #region Property
    public bool pp_isPartControllSystem { get=> m_isPartControllSystem;}
    public SystemInfoType pp_SystemInfoType {get => m_SystemInfoType; }
    public SystemBaseModuleType pp_SystemBaseModuleType { get => m_ConnectBase == null ?
    SystemBaseModuleType.Single : m_isConnectBaseListen ? SystemBaseModuleType.MultiListenMain : SystemBaseModuleType.MultiMain; }
    public bool pp_isMainType { get => pp_SystemBaseModuleType != SystemBaseModuleType.MultiSub && pp_SystemBaseModuleType != SystemBaseModuleType.MultiListenSub; }
    #endregion

    public virtual SystemBase ModuleInitlization(
    NewPlayerCharacterController _MainController, GamePlaySystem _GetSystem = null, params object[] _SendLeftParams)
    {
        if (m_MainPlayer == null) m_MainPlayer = _MainController;
        if (m_MainControllSystem == null) m_MainControllSystem = _GetSystem == null? GamePlaySystem.Instance : _GetSystem;
        if (m_SystemInfoType != SystemInfoType.PopUpStack && m_ConnectBase != null) m_InstanceBase = Instantiate(m_ConnectBase);
        return m_InstanceBase;
    }
    //이걸로 로직 변경 이후 위에 로직 변경할것
    public SystemBase ModuleInitlization(
    ControllerBase _MainController, GamePlaySystem _GetSystem = null)
    {
        if (m_MainPlayer == null) m_MainController = _MainController;
        if (m_MainControllSystem == null) m_MainControllSystem = _GetSystem == null ? GamePlaySystem.Instance : _GetSystem;
        //if(m_SystemBaseModuleType != SystemBaseModuleType.Single && m_SystemInfoType != SystemInfoType.PopUpStack)
        if (m_SystemInfoType != SystemInfoType.PopUpStack && m_ConnectBase != null) m_InstanceBase = Instantiate(m_ConnectBase);
        return m_InstanceBase;
    }

    public abstract void Initlization();

    public virtual void UpdateProcess() { } //DONOTIONG....

    public virtual void ExecuteNextStep(SystemInfoType _NextType) 
    => m_MainControllSystem.ExecuteOutPutSystemBase(_NextType);

    public virtual void PlayOrPauseProcess(bool _isActive, bool _isHoldUpdate) 
    => m_isPartControllSystem = !_isHoldUpdate;
}




public abstract class SystemWorldBase : SystemBase
{
    [Header("Required Values")]
    [SerializeField] private SystemWPOutPutType m_SystemWPOutPutType = SystemWPOutPutType.OutPutBefore;
    [SerializeField, ReadOnly] protected Transform m_CamTarget;
    [SerializeField] protected Animator m_SystemWPAnim;
    [SerializeField] protected GameObject[] m_WPReference;

    [Header("Custom To StateChange Action")]
    [Header("Use Cam Move")] //인터페이스화하여 카메라 움직임 뿐 아니라 추상적으로 상태변형시 적용할 사항을 만들언낼것 (**리팩 요망**)
    [SerializeField] protected bool m_isUseViewLerpCam = false;
    [SerializeField, ShowIf(nameof(m_isUseViewLerpCam))] protected float m_ViewLerpSpeed = 0f;
    //삭제할 가능성 높음
    protected bool m_isActionByCharacter = false;

    public override SystemBase ModuleInitlization(
    NewPlayerCharacterController _MainController, GamePlaySystem _GetSystem = null, params object[] _SendLeftParams)
    {
        m_InstanceBase = base.ModuleInitlization(_MainController, _GetSystem);
        m_CamTarget = (_SendLeftParams[0] as Camera).transform;
        return m_InstanceBase;
    }

    public bool CheckCanLerpCamByWorldPos(Vector3 _GetCamPosition)
    => _GetCamPosition != m_CamTarget.position && m_isUseViewLerpCam;

    public virtual void SetCamMoveThisPoint(Camera _UseCamTarget, SystemInfoType _BeforeProcess,
    System.Action _EndCallBack = null)
    {
        if (!m_isUseViewLerpCam) return;
        float ChooseSpeed = m_ViewLerpSpeed == 0f ? 0.85f : m_ViewLerpSpeed;
        _UseCamTarget.transform.DORotate(m_CamTarget.eulerAngles, ChooseSpeed).SetEase(Ease.InOutCirc);
        _UseCamTarget.transform.DOMove(m_CamTarget.position, ChooseSpeed).SetEase(Ease.InOutCirc).
        OnComplete(() => _EndCallBack?.Invoke());
    }

    public virtual void ActionAnim(object _ActionAnim)
    {
        switch (_ActionAnim)
        {
            case System.Int32 GetIntValue:
                m_SystemWPAnim.SetInteger("WorkingIDX", (int)_ActionAnim);
                break;
            default:
                m_SystemWPAnim.SetTrigger("WorkingTri");
                break;
        }
    }

    //삭제하거나 ControllerBase로 변경 리팩 요망 **
    public virtual bool CheckMainPlayerTrigger(GameObject _GetObj)
    {
        if (m_MainPlayer == null || _GetObj.GetComponent<NewPlayerCharacterController>() == null ||
        !m_MainPlayer.Equals(_GetObj.GetComponent<NewPlayerCharacterController>()))
        return false;

        return true;
    }

    #region 상속 유니티 이벤트 함수

    public virtual void OnTriggerEnter(Collider other)
    {
        if (!m_isPartControllSystem || !CheckMainPlayerTrigger(other.gameObject)) return;

        m_isActionByCharacter = true;
    }

    public virtual void OnTriggerExit(Collider other)
    {
        if (!m_isPartControllSystem || !CheckMainPlayerTrigger(other.gameObject)) return;

        m_isActionByCharacter = false;
    }
    #endregion
}

#region (이전작업) SystemBase For GUI
//GUI에 추가적으로 사용할게 없다면 삭제 요망..
//public abstract class SystemGUIBase : SystemBase
//{
//    [Header("Required Select Base Set")]
//    [SerializeField] protected PopUpType m_PopUpType;
//    public PopUpType pp_PopUpType { get => m_PopUpType }

//}
#endregion

#endregion

#region 인게임 추상 클래스에서 파생된 팝업관련 클래스

public class SystemBaseGUIPopUpRequired : SystemBase, I_PopUpInfo, I_SubModulesCollection, I_ControlSubPopUpInfo
{
    [Header("Need Sub Controller In GUI")]
    [SerializeField, ReadOnly] protected List<ForceSubOrderPopUp> m_InstanceViewModules = new();
    [SerializeField, ReadOnly] protected List<SubModuleControllerBase> m_InstanceViewOutSubControllers = new();

    #region Required PopUp Reference
    public SystemBase I_GetPopUpBase() => this;
    public PopUpType I_GetPopUpType() => PopUpType.RequiredPopUp;
    #endregion

    #region Sub Module Controller Reference

    public void I_CheckSubModuleIsAllCanAction(ControllerBase _GetSubBase)
    {
    }

    public T I_GetSubModule<T>() where T : SubModuleControllerBase
    => ISCheckSubModule<T>(out T _OutPutBase) ? _OutPutBase : null;

    public bool ISCheckSubModule<T>(out T _OutPutBase) where T : SubModuleControllerBase
    {
        //_OutPutBase = null;
        //bool isComplate = m_InstanceViewOutSubControllers.ISFindCondition<SubModuleControllerBase>
        //(x => x is T, out SubModuleControllerBase _GetModule);
        //_GetModule = _OutPutBase;
        //return isComplate;
        _OutPutBase = null;
        return false;
    }
    #endregion

    #region SubPopUpInfo Reference

    public ForceSubOrderPopUp[] I_GetAllSubPopUps() => m_InstanceViewModules.ToArray();

    public SystemBase I_ApplySubPopUp(ForceSubOrderPopUp _GetPopUp)
    {
        var GetPairPackage = _GetPopUp.Initlization(this);
        BachByReciveRect(GetPairPackage.Item1, GetPairPackage.Item2);
        m_InstanceViewModules.Add(_GetPopUp);
        if (GetPairPackage.Item3 != null) m_InstanceViewOutSubControllers.Add(GetPairPackage.Item3);
        return _GetPopUp;
    }

    #endregion

    public override void Initlization()
    {
        this.GetComponent<RectTransform>().sizeDelta = Appinstance.Instance.ms_DataManager.pp_RatioApply;
    }

    public virtual void BreakPoint(bool _isBreak,
    SubModuleControllerBase.SubModuleBreakType _BrackType = default, System.Type _GetType = null) 
    {
        if(_BrackType == SubModuleControllerBase.SubModuleBreakType.BreakAll)
        {
            m_InstanceViewOutSubControllers.HForEach(x => x.BreakPoint(_isBreak));
            return;
        }
        //나머지는 상속에 의해서 정할 수 있도록 수정한다
    }

    protected virtual void BachByReciveRect(RectTransform _GetRect, BachRectInfo _BachRect)
    {
        _GetRect.SetParent(this.transform);
        _GetRect.pivot = _BachRect.s_Pivot;
        if (_BachRect.s_UseSubRect)
        {
            _GetRect.anchorMin = _BachRect.s_BachRect.min;
            _GetRect.anchorMax = _BachRect.s_BachRect.max;
            _GetRect.sizeDelta = new Vector2(_BachRect.s_BachRect.width, _BachRect.s_BachRect.height);
        }
    }
}

#endregion

#region 이후에 Common으로 이전요망

[System.Serializable]
public struct SystemInfo
{
    public SystemInfoType s_SystemInfoType;
    public SystemBase s_MainSystemBase;
    [ReadOnly] public SystemBase s_SubSystemBase;

    public SystemInfo(SystemInfoType _SystemType, SystemBase _MainBase, SystemBase _SubBase)
    {
        s_SystemInfoType = _SystemType;
        s_MainSystemBase = _MainBase;
        s_SubSystemBase = _SubBase;
    }
}

public enum SystemInfoType
{
    //None = 0,
    NoneMainGUI = 0,
    PopUpStack = 1, //팝업은 타입을 가지고 있는것만으로도 메인로직 어떤거라든지 에러가 발생한다
    Title_Enter, //앞에 Title이라고 적시되어있는 구간은 Title씬에서만 동작하는 System의 메인 프로세스이다
    Test_GUIMB_ForceStartDirectInGame,
    Test_GUIMB_SoundGameProjectSelectMusic,
    WPMB_PlayMoterMove
}

public enum SystemBaseModuleType
{
    Single = 0, //단일적으로만 사용하는것
    MultiMain, //다중으로 한번에 소환
    MultiSub, //다중으로 한번에 소환
    MultiListenMain, //다중으로 소환하되 대기상태로 이후에 들일 수 있도록 하는것
    MultiListenSub, //다중으로 소환하되 대기상태로 이후에 들일 수 있도록 하는것
}

public enum SystemWPOutPutType
{
    OutPutBefore, //등장전 단계 즉 인스턴스가 되지 않는 단계를 뜻함
    OutPutListen, //등장은 하였으나 
    OutPutAfter // 모든 등장이 마무리 되었을 단계 (VFX같은 데코레이션 이후에 모든 동작을 할 수 있도록 한다)
}

public enum VectorArrowType { Up, Left, Right, Down, Forword }

#region 팝업을 사용할때 사용하자 (필요치 않으면 삭제 요망..)
public enum PopUpType
{
    DefultPopUp,
    RequiredPopUp,
    InsideGUISubPartType,
    SpecialDefultPopUp, //기본 팝업이긴 하나 딤(팝업 막는 BG)나 일반적인것에서 살짝 벗어난 형태

    //NotUseMainPopUpStack //GamePlaySystem<클래스>에서
    ////돌아가는게 아닌 타 SubPopUp에 사용하지만 기존 사용하고자 하는 메카니즘(추상클래스)들이 같을때 사용
}

public interface I_PopUpInfo
{
    public PopUpType I_GetPopUpType();
    public SystemBase I_GetPopUpBase();
}

public interface I_ControlSubPopUpInfo
{
    public SystemBase I_ApplySubPopUp(ForceSubOrderPopUp _GetPopUp);

    public ForceSubOrderPopUp[] I_GetAllSubPopUps();
}

public interface I_PopUpPush
{
    public void PushOutEvent(System.Action _AddCallBack = null);
}

public interface I_PopUpPop
{
    public void PopOutEvent(System.Action _EndCallBack = null);
}

public interface I_PopUpDecorDem
{
    public float I_SetDem();
}

//SubPopUp 즉 GamePlaySystem<클래스>에서 사용하지 않는 PopUp에 해당됨
public interface I_SubPopUpLocalLogic
{
    public void SubPopUpLocalLogic(object _ApplyPar = null);
}

//현재는 사용하지 않으나 객체가 공통되고 프리팹이 다른경우 사용을 고려한다
public struct AddPopUpinfo : I_Data
{
    public string s_AbsSubOderPartName;
    public AddPopUpinfo(string _PartName)
    {
        s_AbsSubOderPartName = _PartName;
    }
}
#endregion

#endregion
