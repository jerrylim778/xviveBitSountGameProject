using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Commons.Helpers;
using I_PurifiedParamsData = ControllerBase.I_PurifiedParamsData;

public class CharacterMyInfo : MyInfo
{
    [field: SerializeField] public UserInfo spp_SavedUserInfo { get; private set; }
    [field: SerializeField] public CharacterItemInfo spp_SavedSdataCInfo { get; private set; }

    private bool s_CMyInfoTestMode = false;

    public CharacterMyInfo(Appinstance _GetInstance, string _InitSaveIDX) : base(_GetInstance, _InitSaveIDX)
    {
        s_CMyInfoTestMode = TestMode_CheckByChildSubObject<CharacterMyInfo>();
    }

    protected override bool CheckCompareDBIDX(object _GetDBKey)
    {
        if (!base.CheckCompareDBIDX(_GetDBKey)) return false;
        bool isComplate = false;
        if (_GetDBKey is MySavedInfo _GetMySavedInfo)
        isComplate = _GetMySavedInfo.s_SavedUserDatas.s_DBSavedIDX == spp_DBSavedIDX;
        (!isComplate).HDebug("잘못된 데이터가 들어왔습니다.!", Helper.HDType.Error);
        return isComplate;
    }

    public override ControllerBase[] ApplyModulesByInfo(params ItemInfo[] _ApplyParams)
    {
        if (!s_CMyInfoTestMode && spp_SavedSdataCInfo != null)
        _ApplyParams = _ApplyParams.ToList().Concat(new[] { spp_SavedSdataCInfo }).ToArray();

        ControllerBase[] GetBases = base.ApplyModulesByInfo(_ApplyParams);
        if ((GetBases.Length <= 0 && GetBases[0] != null && _ApplyParams[0] is not CharacterItemInfo).
        HDebug($"{nameof(CharacterMyInfo)}에서 필요한 정보가 없습니다.!", Helper.HDType.Error)) return null;
        var CInfo = _ApplyParams[0] as CharacterItemInfo;
        List<SubModuleControllerBase> GetBase = new();
        I_PurifiedParamsData[] _OutPutPDs = new I_PurifiedParamsData[3];

        var GetStartTr = GamePlaySystem.Instance != null && GamePlaySystem.Instance.pp_StartCharacterTr != null ?
        GamePlaySystem.Instance.pp_StartCharacterTr : null;
        var MainController = GetStartTr == null? Object.Instantiate(CInfo.s_PrefabObjs) :
        Object.Instantiate(CInfo.s_PrefabObjs, GetStartTr.position, GetStartTr.rotation, null);
        NewActionInfo GetNAInfo = (NewActionInfo)m_DataManager.CompareInfoType<NewActionInfo>(CInfo.s_NewActionInfoIDX.ToString());
        GetBase.Add(m_DataManager.GetCompareComponent</*CameraController*/CinemachineCameraController>(true, CameraController.ViewType.TPS.ToString(), ref _OutPutPDs[0]));
        GetBase.Add(m_DataManager.GetCompareComponent<BInputController>(false, CInfo.s_CharacterUsePowerType.ToString(), ref _OutPutPDs[1]));
        GetBase.Add(m_DataManager.GetCompareComponent</*AnimationController*/ TPSPackageSubModuleController>(false, CInfo.s_CalculateInfoIDX.ToString(), ref _OutPutPDs[2]));
        //GetBase.Add(m_DataManager.FindComponentByTemp<UserGameArrowSubModuleController>());

        ApplyNewSubModule(GetNAInfo); ApplyNewSubModule(CInfo);
        //Helper.HCountForEach(0, 2, _CountIDX => 
        int _CountIDX = 0; GetBase.ForEach(x => 
        {
            if (GetBase[_CountIDX] != null) ApplyNewSubModule(GetBase[_CountIDX]);
            if (_CountIDX < _OutPutPDs.Length && _OutPutPDs[_CountIDX] != null) ApplyNewSubModule(_OutPutPDs[_CountIDX]);
            _CountIDX++;
        });
        
        GetBases[0] = MainController.GetComponent<NewCharacterController>();
        return GetBases;
    }

    //다른것으로 리팩 요망
    protected override void ApplyNewSubModule(object _ApplyElem, bool _isSpecApply = false)
    {
        if (_ApplyElem is not NewActionInfo  &&
        (spp_SavedUserInfo.s_NewActionInfo == null || !spp_SavedUserInfo.s_NewActionInfo.HasValue)) return;
        NewActionInfo GetActionInfo = _ApplyElem is NewActionInfo GetActionData ?
        GetActionData : spp_SavedUserInfo.s_NewActionInfo.Value;

        switch (_ApplyElem)
        {
            case bool _GetBoolean:
                GetActionInfo.s_isShowProduction = _GetBoolean;
                break;
            case CharacterItemInfo _GetItemInfo:
                GetActionInfo.s_ApplyGameCharacterInfo = _GetItemInfo;
                break;
            case SubModuleControllerBase _GetSubModuleBase:
                GetActionInfo.s_SubModuleBase = GetActionInfo.s_SubModuleBase == null ? new SubModuleControllerBase[1] { _GetSubModuleBase } :
                GetActionInfo.s_SubModuleBase.ToList().Concat(new[] { _GetSubModuleBase }).ToArray();
                break;
            case I_PurifiedParamsData _GetSubRBaseParams:
                GetActionInfo.s_RequiredSubModuleParams = 
                GetActionInfo.s_RequiredSubModuleParams == null ?  new I_PurifiedParamsData[1] { _GetSubRBaseParams } :
                GetActionInfo.s_RequiredSubModuleParams.ToList().Concat(new[] { _GetSubRBaseParams }).ToArray();
                break;
        }
        spp_SavedUserInfo = new UserInfo(spp_DBSavedIDX, GetActionInfo);
    }

    public override void AllMyInfoLoadAndApplyAnyncParsing(System.Action _EndCallBack, out IEnumerator[] _GetCourtines)
    {
        base.AllMyInfoLoadAndApplyAnyncParsing(_EndCallBack, out _GetCourtines);
        _GetCourtines = _GetCourtines.ToList().Concat(new[] { CO_WaitParsingCMyInfo(_EndCallBack) }).ToArray();
    }

    IEnumerator CO_WaitParsingCMyInfo(System.Action _EndCallBack = null)
    {
        if (s_CMyInfoTestMode)
        {
            spp_SavedUserInfo = new UserInfo(spp_DBSavedIDX, null);
            _EndCallBack?.Invoke();
            yield break;
        }
        //아직은 저장할 데이터가 없다고 가정하고 넘긴다
        #region 향후 CharacterMyInfo에 저장 데이터가 존재할시 아래 주석 참고(**해당 주석은 필히 타이쿤 전용 메모장에 추가할것**)
        //테스트 모드가 아니라면 파싱된 NewActionInfo 데로 적용할 수 있도록 수정한다
        //허나 저장데이터가 내 캐릭터 기준 동일하다면 넣지 않아도 상관없다
        //하지만 저장 위치나 가지고 있던 Actioninfo데로 배출할땐 필요하다
        //따라서 파싱 방식을 두가지로 나눈다
        //(<1> : NewActionInfo내의 인덱스에 따른 파싱 <2> Character가 가지고 있는 현재 NewActionInfo를 그대로 출력 등이 존재한다)
        #endregion
        if (spp_SavedUserInfo.s_NewActionInfo != null && spp_SavedUserInfo.s_NewActionInfo.HasValue)
        spp_SavedSdataCInfo = m_GetSDataManager.SDataParsingByItemIDX<
        SDataCharacterInfo, CharacterItemInfo>(spp_SavedUserInfo.s_NewActionInfo.Value.s_ActionInfoLocalIDX);

    }
}
