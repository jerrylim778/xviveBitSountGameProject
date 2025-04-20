using System.Collections.Generic;
using System.Collections;
using System.Linq;
using UnityEngine;
using Commons.Helpers;
using Commons;
using Sirenix.OdinInspector;
using CamControllerInitInfo = CameraController.CamControllerInitInfo;
using I_PurifiedCData = ControllerBase.I_PurifiedCData;
using I_PurifiedAddKey = ControllerBase.I_PurifiedAddKey;
using I_PurifiedParamsData = ControllerBase.I_PurifiedParamsData;



//CharacterItemInfo에 관한 Data정보 파싱에 컨테이너 역활 및 데이터 송수신에 관한 시스템 적인것들이 정의됨
public sealed partial class DataManager : MonoBehaviour
{
    #region DataManager.CharacterInfo에 관한 설명
    //1.엑셀에서 파싱한 스크립터블 데이터들을 타입별로 보관한다
    //2.Resources or Addressable을 통하여 캐릭터에 적용할 수 있는 System Prefab들을 Load 한다
    //3.로딩시 (부분 Sub 로딩 + 씬 전환) 정적으로 필요한 System  함수들을 정의하여 로딩의 구조를 정적과 캡슐화를
    //하여 데이터 MVC 패턴을 강제 한다 (데이터의 무결성과 보완을 위한 조치)
    #endregion
    #region 스킬 사용과 관련한것 (재사용시 리팩토링 후 사용 요망 **)
    //private readonly string m_ResourcesSystemPath = "Objects/DefultObject/Systems/CharacterSystems/";
    //private readonly string m_ResourcesAnimConPath = "Objects/AnimController/CharacterReference/";
    //private readonly string test_ResourcesScriptableJobInfo = "Datas/ScriptableData/SubScriptableData/TemporaryTest_SDataCharacterSkillInfo"; 
    #endregion

    [Header("Character Reference Control")]
    [SerializeField, ShowIf(nameof(test_isAppliedTemporyAll))] private bool test_isShowTempCharacterInfo = false;

    [Header("===TempCharacterDatas===")]
    [field: SerializeField, BoxGroup("CharacterRefer"), ShowIf(nameof(ODINToogleCharacterReference))] public NewActionInfo[] test_ListenNewActionInfo { get; private set; } //해당 데이터도 어디선가 가져와야됨
    [field: SerializeField, BoxGroup("CharacterRefer"), ShowIf(nameof(ODINToogleCharacterReference))] public SimpleCameraData[] test_SimpleCameraData { get; private set; }
    [field: SerializeField, BoxGroup("CharacterRefer"), ShowIf(nameof(ODINToogleCharacterReference))] public CamControllerInitInfo[] test_ListenCamData { get; private set; }
    [field: SerializeField, BoxGroup("CharacterRefer"), ShowIf(nameof(ODINToogleCharacterReference))] public InputDataInfo[] test_ApplyInputData { get; private set; }
    [field: SerializeField, BoxGroup("CharacterRefer"), ShowIf(nameof(ODINToogleCharacterReference))] public CalculateInfo[] test_ApplyCalculateData { get; private set; }
    [field: SerializeField, BoxGroup("CharacterRefer"), ShowIf(nameof(ODINToogleCharacterReference))] public TPSPackageInfo[] test_ApplyTPSPackageData { get; private set; }
    [field: SerializeField, BoxGroup("CharacterRefer"), ShowIf(nameof(ODINToogleCharacterReference))] public GameObject[] test_SupportCharacterSubInfos { get; private set; }

    private bool ODINToogleCharacterReference() => test_isAppliedTemporyAll && test_isShowTempCharacterInfo;

    private I_PurifiedCData LoadCompareTempCInfo<T>(string _GetKey) where T : I_PurifiedCData
    {
        var CompareTempSet = typeof(T).Name switch
        {
            nameof(NewActionInfo) => test_ListenNewActionInfo.ToList().OfType<I_PurifiedCData>().ToArray(),
            _ => null
        };
        return CompareTempSet == null ? null : CompareTempSet.HFind(x => (x is I_PurifiedAddKey GetAddKey && GetAddKey.I_SubInfoIDX() == _GetKey));
    }

    private I_PurifiedParamsData LoadCompareTempCParamInfo(ControllerBase _GetBase, string _GetKey) 
    {
        var CompareTempSet = _GetBase switch
        {
            RenewalCinemachineCameraController => test_ListenCamData.ToList().OfType<I_PurifiedParamsData>().ToArray(),
            CameraControllerBase => test_ListenCamData.ToList().OfType<I_PurifiedParamsData>().ToArray(),
            BInputController => test_ApplyInputData.ToList().OfType<I_PurifiedParamsData>().ToArray(),
            AnimationController => test_ApplyCalculateData.ToList().OfType<I_PurifiedParamsData>().ToArray(),
            TPSPackageSubModuleController => test_ApplyTPSPackageData.ToList().OfType<I_PurifiedParamsData>().ToArray(),
            _ => null
        };
        return CompareTempSet == null ? null : CompareTempSet.HFind(x => (x is I_PurifiedAddKey GetAddKey && GetAddKey.I_SubInfoIDX() == _GetKey));
    }

    //이후 삭제할 가능성이 가장 높음 (에초에 해다 클래스는 임시이기 때문에
    //반드시 전부 일반 정적형태 스크립터블로 전환해야된다
    public void ApplyArtificialParamsData(ControllerBase.I_PurifiedParamsData ApplyType)
    {
        switch(ApplyType)
        {
            case InputDataInfo GetInputInfo:
                int FIDX = -1; FIDX =
                test_ApplyInputData.HFindIndex(x => x.s_PowerType == GetInputInfo.s_PowerType);
                test_ApplyInputData[FIDX] = GetInputInfo;
                break;
        }
    }
}