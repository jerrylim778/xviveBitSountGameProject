using System.Collections;
using UnityEngine;
using Sirenix.OdinInspector;
using Commons.Helpers;


//SoundGameProject MyInfo 데이터 송수신 체계
//원래는 서버로부터 데이터를 받는 구조였으나 개념은 비슷하지만 계정에 등록된 형태가 아닌 임의 데이터
//송수신 채계기 때문에 MyInfo의 형식을 빌렸으나 최종 데이터 송수신 전달채계는 WebController<클래스>가 아닌
//SDK 호출 관리자가 된다
public class SoundGameMyInfo : MyInfo
{
    private bool s_SoundMyInfoTestMode = false;
    [field: SerializeField] public SoundGameSavedData spp_SavedSoundUserInfo { get; private set; }
    [field: SerializeField] public AlbumInfo spp_SavedAlbumInfo { get; private set; }

    public SoundGameMyInfo(Appinstance _GetInstance, string _InitSaveIDX) : base(_GetInstance, _InitSaveIDX)
    {
        s_SoundMyInfoTestMode = TestMode_CheckByChildSubObject<SoundGameMyInfo>();
    }

    //리팩 요망
    protected override bool CheckCompareDBIDX(object _GetDBKey)
    {
        if (_GetDBKey != null && _GetDBKey is SoundGameSavedData _GetMySavedInfo)
        return _GetMySavedInfo.s_DBSavedIDX == spp_DBSavedIDX && !_GetMySavedInfo.Equals(default);
        return false;
    }

    public override ControllerBase[] ApplyModulesByInfo(params ItemInfo[] _ApplyParams)
    {
        ControllerBase[] GetBases = base.ApplyModulesByInfo(_ApplyParams);
        GetBases[0] = Object.FindAnyObjectByType<SoundGameController>();
        spp_SavedAlbumInfo = _ApplyParams.HFind(x => x is AlbumInfo) as AlbumInfo;
        return GetBases;
    }

    public override void ApplyNewSubModule(object _ApplyElem, bool _isSpecApply = false)
    {
        var GetUserInfo = spp_SavedSoundUserInfo;
        switch (_ApplyElem)
        {
            case int _AlbumInfo:
                break;
            case int[] ElemIDXArray:
                GetUserInfo.s_SavedElemItemIDX = ElemIDXArray;
                break;
        }

        spp_SavedSoundUserInfo = GetUserInfo;
    }

    //마지막 커스터마이징 구간까지 넘어간 이후에 최종 호출하여 저장할 수 있도록 수정한다
    public void SavedSoundInfoData(SoundGameSavedData? _GetData = null)
    {
        if(_GetData != null && _GetData.HasValue)
        spp_SavedSoundUserInfo = _GetData.Value;

        //테스트 환경에서만 해당 함수를 이용해 로컬로 저장하지만
        //그게 아니라면 SDK 관리자에서 송수신으로 제이슨전달할 수 있도록 수정할것
        if (s_SoundMyInfoTestMode) LocalSettingJsonData(Commons.SettingJsonType.Save);
        //else
    }


    //해당 부분에 저장데이터를 불러오는건 로딩 파싱을 사용에 여부를 떠나 동일하게 정의하여 호출한뒤 사용할 수 있도록
    public override void AllMyInfoLoadAndApplyAnyncParsing(System.Action _EndCallBack, out IEnumerator[] _GetCourtines)
    {
        _GetCourtines = null;

        //해당 구간에서 여러개의 저장 데이터가 나올 수 있다
        //저장데이터가 하나의 경우에는 그냥 적용가능한 상태로 존재
        if (s_SoundMyInfoTestMode)
        {
            spp_SavedSoundUserInfo =
            Appinstance.Instance.ms_DataManager.spp_TempSoundGameSavedData;
            s_MySavedInfo.s_SavedSoundGameInfo = spp_SavedSoundUserInfo;
        }
        
        bool isCreateNew = !CheckCompareDBIDX(s_MySavedInfo.s_SavedSoundGameInfo);
        if (isCreateNew) s_MySavedInfo.s_SavedSoundGameInfo = spp_SavedSoundUserInfo;
        else spp_SavedSoundUserInfo = spp_MySavedInfo.s_SavedSoundGameInfo;
    }
}

[System.Serializable]
public struct SoundGameSavedData : I_Data
{
    [ReadOnly] public string s_DBSavedIDX;
    public int s_AlbumItemIDX;
    //몇번째 스택에 저장했는지는 향후에 변경할 수 있도록 수정할것
    public int[] s_SavedElemItemIDX;
}
