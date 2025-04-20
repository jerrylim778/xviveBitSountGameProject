using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class DelEventManager
{

    //public delegate void DEL_SaveAllMyInfoItem();
    //public event DEL_SaveAllMyInfoItem DEL_SaveAllMyInfo_Item;
    //public void DELSaveAllMyInfoItem()
    //{
    //    DEL_SaveAllMyInfo_Item?.Invoke();
    //}

    #region GamePlayManager Delegate Reference (이후에 GM으로 옮길 가능성이 높음)
    //GameManager<클래스>에서 파생된 대리자들은 차후 GameManager<클래스>에서 관리할 수 있도록 수정한다
    public delegate void DEL_GMSetCusor(bool _isActive);
    public event DEL_GMSetCusor DEL_GMSet_Cusor;
    public void DELGMSetCusor(bool _isActive)
    {
        DEL_GMSet_Cusor?.Invoke(_isActive);
    }
    #endregion

    #region All Loading Reference (전부 LoadingManager로 이전함)
    //Loading Manager Reference (이후에 Loading관리자로 이전 요망)
    //public delegate void DEL_UpdateProcess(int _UpdateCount); //옮김
    //public event DEL_UpdateProcess DEL_Update_Process;
    //public void DELUpdateProcess(int _UpdateCount)
    //{
    //    DEL_Update_Process?.Invoke(_UpdateCount);
    //}

    //public delegate void DEL_UpdateGCCollectByAction(bool _isComplate, int _UpdateProcessCount); //옮김
    //public event DEL_UpdateGCCollectByAction DEL_UpdateGCCollect_ByAction;
    //public void DELUpdateGCCollectByAction(bool _isComplate, int _UpdateProcessCount) 
    //{
    //    DEL_UpdateGCCollect_ByAction?.Invoke(_isComplate, _UpdateProcessCount);
    //}

    //Loading PopUp Reference
    //public delegate void DEL_ShowOtherTxt(string _ShowTxt); //옮김
    //public event DEL_ShowOtherTxt DEL_ShowOther_Txt;
    //public void DELShowOtherTxt(string _ShowTxt)
    //{
    //    DEL_ShowOther_Txt?.Invoke(_ShowTxt);
    //}

    //MainLoading Reference
    //public delegate void DEL_UpdateMainLoadingPrograss(int _CurrentCount, int _MaxCount); //옮김
    //public event DEL_UpdateMainLoadingPrograss DEL_UpdateMainLoading_Prograss;
    //public void DELUpdateMainLoadingPrograss(int _CurrentCount, int _MaxCount)
    //{
    //    DEL_UpdateMainLoading_Prograss?.Invoke(_CurrentCount, _MaxCount);
    //}
    #endregion

    //MyInfo Reference
    public delegate void DEL_UpdateMyInfoFirstinit(MyInfo _GetFirstInfo);
    public event DEL_UpdateMyInfoFirstinit DEL_UpdateMyInfo_Firstinit;
    public void DELUpdateMyInfoFirstinit(MyInfo _GetFirstInfo)
    => DEL_UpdateMyInfo_Firstinit?.Invoke(_GetFirstInfo);

    //DataManager Reference (**ChangeScene MapInfo**)
    public delegate void DEL_MemoryAllResetDELEvntByChangeScene();
    public event DEL_MemoryAllResetDELEvntByChangeScene DEL_MemoryAllResetDELEvntBy_ChangeScene;
    public void DELMemoryAllResetDELEvntByChangeScene(bool _isMemoryRemove)
    {
        DEL_MemoryAllResetDELEvntBy_ChangeScene?.Invoke();
        if (_isMemoryRemove && Appinstance.Instance != null) //DELUpdateGCCollectByAction(true, 1);
            Appinstance.Instance.ms_LoadingManager.DELManagerUpdateGCCollect(true, 1);
    }

    //DataManager Reference (**Required Parsing Data**)
    public delegate void DEL_RequiredParsingComplate();
    public event DEL_RequiredParsingComplate DEL_RequiredParsing_Complate;
    public void DELRequiredParsingComplate(System.Action _ComplateAfterCallBack = null)
    {
        DEL_RequiredParsing_Complate?.Invoke();
        _ComplateAfterCallBack?.Invoke();
    }

    //CharacterController Reference
    public delegate void DEL_MainCharacterAnimCallBack();
    public event DEL_MainCharacterAnimCallBack DEL_MainCharacterAnim_CallBack;
    public void DELMainCharacterAnimCallBack()
    {
        DEL_MainCharacterAnim_CallBack?.Invoke();
    }

    //GamePlaySystem => SystemBase Reference
    public delegate void DEL_PlayOrPauseProcess(bool _isActive, bool _isHoldUpdate);
    public event DEL_PlayOrPauseProcess DEL_PlayOrPause_Process;
    public void DELPlayOrPauseProcess(bool _isActive, bool _isHoldUpdate)
    {
        DEL_PlayOrPause_Process?.Invoke(_isActive, _isHoldUpdate);
    }

    public delegate void DEL_InitCameraControlStartPos(Vector3 _GetStartPos, bool _isRemoveDelData, System.Action _EndCallBack = null);
    public event DEL_InitCameraControlStartPos DEL_InitCameraControl_StartPos;
    public void DELInitCameraControlStartPos(Vector3 _GetStartPos, bool _isRemoveDelData, System.Action _EndCallBack = null)
    {
        DEL_InitCameraControl_StartPos?.Invoke(_GetStartPos, _isRemoveDelData, _EndCallBack);
    }

    #region Let It Diner 에서 사용한 일전에 대리자들 (참고하여 사용할것을 추려서 사용할것)
    ////Production Reference
    //public delegate void DEL_UpdateProductionProcess(int _SetIDX, object _SetItemTarget);
    //public event DEL_UpdateProductionProcess DEL_UpdateProduction_Process;
    //public void DELUpdateProductionProcess(int _SetIDX, object _SetItemTarget)
    //{
    //    DEL_UpdateProduction_Process?.Invoke(_SetIDX, _SetItemTarget);
    //}

    ////Interaction Required PopUp Reference
    //public delegate void DEL_InteractionPopUpInputSubPopUp(InteractionRequiredPopUp.SubPopUPGUIInfo _InputInfo);
    //public event DEL_InteractionPopUpInputSubPopUp DEL_InteractionPopUp_InputSubPopUp;
    //public void DELInteractionPopUpInputSubPopUp(InteractionRequiredPopUp.SubPopUPGUIInfo _InputInfo)
    //{
    //    DEL_InteractionPopUp_InputSubPopUp?.Invoke(_InputInfo);
    //}

    //public delegate void DEL_SubNoticeByInteraction(InteractionRequiredPopUp.InteractionSubNoticeType _OutPutSNType, string _OutPutStr);
    //public event DEL_SubNoticeByInteraction DEL_SubNoticeBy_Interaction;
    //public void DELSubNoticeByInteraction(InteractionRequiredPopUp.InteractionSubNoticeType _OutPutSNType, string _OutPutStr)
    //{
    //    DEL_SubNoticeBy_Interaction?.Invoke(_OutPutSNType, _OutPutStr);
    //}

    ////쨋든 OpenCV의 SubModule에서 어떠한 조건이 충족되는걸 만들고 해당 대리자를 호출하여 관련된 값을 줄 수 있도록 
    //public delegate void DEL_WarningUpValueByInteractionSubPopUp(float _UpdateValue, bool _isUpScale);
    //public event DEL_WarningUpValueByInteractionSubPopUp DEL_WarningUpValueByInteraction_SubPopUp;
    //public void DELWarningUpValueByInteractionSubPopUp(float _UpdateValue, bool _isUpScale)
    //{
    //    DEL_WarningUpValueByInteraction_SubPopUp?.Invoke(_UpdateValue, _isUpScale);
    //}

    //public delegate void DEL_RealTimeRayModuleEvent(object _SetObj = null, System.Action<bool> _ClickCallBack = null,
    //System.Action<Vector2> _RotPowerCallback = null, System.Action<float> _ZoomCallBack = null);
    //public event DEL_RealTimeRayModuleEvent DEL_RealTimeRayModule_Event;
    //public void DELRealTimeRayModuleEvent(object _SetObj = null, System.Action<bool> _ClickCallBack = null,
    //System.Action<Vector2> _RotPowerCallback = null, System.Action<float> _ZoomCallBack = null)
    //{
    //    DEL_RealTimeRayModule_Event?.Invoke(_SetObj, _ClickCallBack, _RotPowerCallback, _ZoomCallBack);
    //}

    ////Interaction Module Reference
    //public delegate void DEL_PickUpItemUpdate(int _ItemIDX);
    //public event DEL_PickUpItemUpdate DEL_PickUpItem_Update;
    //public void DELPickUpItemUpdate(int _ItemIDX)
    //{
    //    DEL_PickUpItem_Update?.Invoke(_ItemIDX);
    //}

    //public delegate void DEL_ModuleConditionComplateSet(int _ConditionIDX, System.Func<bool> _EndConditionCallBack = null);
    //public event DEL_ModuleConditionComplateSet DEL_ModuleCondition_ComplateSet;
    //public void DELModuleConditionComplateSet(int _ConditionIDX, System.Func<bool> _EndConditionCallBack = null)
    //{
    //    DEL_ModuleCondition_ComplateSet?.Invoke(_ConditionIDX, _EndConditionCallBack);
    //}
    #endregion
}
