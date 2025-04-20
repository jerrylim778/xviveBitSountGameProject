using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Sirenix.OdinInspector;

//Curr All Scene ComapreType Infos
public enum SetSceneType
{
    None = 0,
    IntroScene, //없을 수 있음
    TitleScene,
    InGameScene,
    TestModeScene,
    //MFLGameScene, // MFL Game Reference
    //TyconGameScene,
    //TyconGameSceneLiteVersion,
    //TyconGameSceneSimpleVersion
    //RunnerGameScene,
    //RunnerGameScene2,
    //RunnerGameScene3
    //Simple2DRunnerScene
    //DragonBowGameScene
    //FlyFlipGameScene
    SoundGameScene
}


namespace Commons
{

    //Json Infos
    public enum SettingJsonType
    {
        None = 0,
        First,
        AllReset,
        Save,
        Load
    }

    //후에 Flag로 만들어 파싱하는 구간을 연합할 수 있도록 한다
    //Table Infos
    public enum ParsingTableType
    {
        None = 0,
        TranslationInfo,//번역 테이블
        LoadingInfo, //로딩 테이블
        //해당 구역까지 
        ItemInfo,
        ItemDescirptionInfo, //선택 테이블(설명)
        ItemInstanceSpriteInfo, //선택 테이블(Sprite사진)
        ItemInstanceObjectInfo, //선택 테이블(프리팹)
        ItemInstanceAnimControllerInfo, //선택 테이블(프리팹)
        ItemOtherAllClipsInfo, //선택 테이블(프리팹)
        ItemPotionInfo,
        ItemPartsInfo,
        ItemGearInfo,
        ItemWeapownInfo,
        MapInfo,//맵관련 테이블
        CharacterInfo, //캐릭터 테이블
        #region MFLReference
        MatchFactoryItemInfo
        #endregion
        #region TMGReference
        //ThreeMatchGameItemInfo,
        //ThreeMatchGameBachInfo
        #endregion
    }
    //Scritpable Item Infos



    //Character Infos
    #region [Action] Phycis Info Reference
    public enum PlayerState
    {
        None,
        BeforeInGame, //캐릭터가 씬내에 진입전에 처리해야될 상태
        RealTimeInGame, //캐릭터가 씬내에 존재할때 넣을 상태
        OutBeforeInGame, //캐릭터가 씬내에 없어지기 전에 처리해야될 상태
        StaticAction, //캐릭터의 일시 정지 상태 입력값이 리턴처리되어야함
        DefultAction //움직일 수 있는타입
    }

    [System.Serializable]
    public struct PlayerActionInfo
    {
        public PlayerState s_PlayerState; //언제 발동할껀지
        public ActionControllType s_ActionControllType; //어떻게 발동한껀지 
        public ActionInfo s_ActionInfo; //애니와 물리적 동작을 나타내기 위한 변수    
    }

    public enum ActionControllType //Phycis 및 Decoration 둘다 사용함
    {
        None = 0,
        UserCostomImletType, //커스텀으로 바로처리
        UserCostomSepProType, //커스텀으로 순차처리
    }


    [System.Serializable]
    public struct ActionInfo
    {
        //public string s_ActionKeyIDX; //후에 스크립터블 데이터에서 가져올 가능성을 대비한 ActionInfo의 키값
        [SerializeField, ReadOnly] public PowerType s_PowerType;
        public CalculateInfo s_CalculateInfo;
        public SubModuleControllerBase[] s_SubModuleBase;
    }

    [System.Serializable] //해당 구역은 절대 변경되어서는 안되는 구조체 값
    public struct CalculateInfo : ControllerBase.I_PurifiedParamsData, ControllerBase.I_PurifiedAddKey
    {
        [field: SerializeField] public RuntimeAnimatorController spp_AniRuntimeController { get; private set; }
        [field: SerializeField] public Avatar spp_ApplyAvatar { get; private set; }
        [field: SerializeField] public PhycisOutPutBase spp_PhycisOutPutBase { get; private set; }
        //만일 AnimationController를 통해서 추가적인 SubController이 필요할때 아래 Controller를 정의하여 사용할 수 있도록 한다
        [field: SerializeField] public SubModuleControllerBase[] spp_AddSMCWithAnimController { get; private set; }

        [field: SerializeField, PropertyOrder(int.MinValue)] public int spp_SubItemIDX { get; private set; }
        public string I_SubInfoIDX() => spp_SubItemIDX.ToString();
        public bool I_ISGetOutParams(ControllerBase _GetPPData) => _GetPPData is AnimationController;
    }

    [System.Serializable]
    public struct InputDataInfo : ControllerBase.I_PurifiedParamsData, ControllerBase.I_PurifiedAddKey
    {
        //public int s_InputItemIDX;
        public PowerType s_PowerType;
        public ControllerBase.I_PurifiedParamsData s_SubApplyParams;
        [ShowIf(nameof(ODIN_ToggleAISetting))] public InputTargetInfo s_InputTargetInfo; //위의 Param데이터로 통패합해야됨
        private bool ODIN_ToggleAISetting() => s_PowerType == PowerType.TargetPower ||
        s_PowerType == PowerType.AITargetPower || s_PowerType == PowerType.MousePointTargetPower;
        public string I_SubInfoIDX() => s_PowerType.ToString();
        public bool I_ISGetOutParams(ControllerBase _GetPPData) => _GetPPData is BInputController;
    }

    public enum PowerType
    {
        NotNeedAnyPower, //실질적인 파워가 필요없음(Idle동작만 취하는 모든 플레이어에 해당)
        ListenSelfPower, //플레이어를 따라가다가 다시 해당 캐릭터에 맞춰 기다리기 위해 적용
        SelfInputPower, 
        SelfJoyStickPower, 
        TargetPower, //타겟지점까지의 움직임을 나타냄
        MousePointTargetPower, 
        AITargetPower, //구분에 따라 네비메쉬를 적용기 되기 때문에 향후 필요없을시 삭제요망
        MouseAndCamBaoundaryPower, //마우스와 화면의값에 의해 힘이 정해지는 타입
    }

    #region Only Use Input Target (**InputTarget에서만 사용가능**)
    public enum TargetPowerSpeed //타겟파워일경우 속도를 어떤식으로 조정할것인지
    {
        None = 0, //일정한 수준으로 이동함
        TargetDistanceSpeedType, //거리에 따라 이동속도 증가
        TargetLerpByTimeSpeedType, //점점 보간하여 속도 증가 (시간이 존재함)
    }

    //전부 임시로 하고 각나왔다 싶음 바로 GeneratorCharactersSubInfo와 SubController에 대입할지를 빠르게 정해야한다
    //1.갔다와서 배이킹한 프리팹 인스턴싱했을때 캐릭터가 연계해서 움직이는지
    //2.인스턴싱한 뒤에 중간다리 역활을 동적으로 어떻게 이을지에 대한것 고민해서 적용할것
    [System.Serializable]
    public struct InputTargetInfo
    {
        public bool s_isUseSelfLiveNavSurface; //사용하려면 Phycishelper로 이전시켜 사용해야됨
        public bool s_isUseNavPathFinder;
        public TargetPowerSpeed s_TargetPowerSpeed;
    }
    #endregion
    #endregion

    #region [Decoration] Shader Data Struct Reference

    [System.Serializable]
    public struct ShaderActionInfo
    {
        public PlayerState s_PlayerState; //언제 발동할껀지
        public ActionControllType s_ShaderActionType; //어떻게 발동한껀지 
        public ShaderInfo[] s_GetOutPutShaderInfo; //리소스 로드할것과 머터리얼을 구분짓기 위함
    }


    //후에 처리해야될 쉐이더가 많아진다면 전부 스크립터블 오브젝트로 전환한다 (현재는 NewShaderInfo<스크립터블 데이터 클래스>형태로 대체됨 삭제요망...)
    [System.Serializable]
    public struct ShaderInfo
    {
        public string s_ActionInfoSIDX;
        public float WaitTime;
        public ShaderPlayerType s_ShaderPlayerType; //어디에 발동한껀지
        public bool s_isReverseShader; //역방향 명령을 진행할 쉐이더만 True 
        public bool s_isOn;
        public bool s_isControllerStateEnd;
        public float s_TweeningSpeed;
        public Transform s_UseParents;
        public Object s_InstanceObj;//게임오브젝트일지 머터리얼을 로드할지 모르기때문
        public string s_ShaderOutPutBaseName; //각 오브젝트가 어떻게 발동할지에 관한 카테고리형 추상 클래스
                                              //public ShaderOutPutBase s_ShaderOutPutBase; //각 오브젝트가 어떻게 발동할지에 관한 카테고리형 추상 클래스
        [HideInInspector] public int s_ApplyObjIDX; //적용된 머터리얼을 타 구간에서 실행하기 위한 조치(재설계 요망)
        [HideInInspector] public int s_ApplyObjIDX_2; //적용된 머터리얼을 타 구간에서 실행하기 위한 조치(재설계 요망)
    }

    public enum ShaderPlayerType
    {
        None = 0,
        PlayerMaterial, //플레이어의 자체 머터리얼 수정 (전용 스크립트 필요)
        PlayerBoundary, //플레이어의 주변에 발동 (전용 스크립트 필요)
        PlayerBoundaryFollow, //플레이어의 주변에 발동 (전용 스크립트 필요 + 따라다님)
        EventEffect, //VisualEffect or Particle로 처리함
        EventEffectFollow, //VisualEffect or Particle로 따라와서 처리함
        GUIShaderAction //캐릭터에서 직접적인 GUIShader인터렉션할때 발동
    }


    //쉐이더를 담음 새로운 로직(09/19일자에 새로 변형함)
    [System.Serializable]
    public struct RendererMeshMaterialInfo
    {
        public Renderer s_GetRender;
        public MaterialInfo[] s_MaterialsArray;
    }

    //해당 구간은 한번만 실행되어야 하기에 초기화 함수에서 넣은뒤로 수정을 할 수 없도록 수정해야함
    [System.Serializable]
    public struct MaterialInfo
    {
        [SerializeField, ReadOnly] private Material s_GetMat;
        [SerializeField, ReadOnly] private Texture s_BaseMap;
        [SerializeField, ReadOnly] private Texture s_NomalMap;
        [SerializeField, ReadOnly] private Texture s_MatallicMap;

        public Material spp_GetMat {get => s_GetMat; }
        public Texture spp_BaseMap {get => s_BaseMap; }
        public Texture spp_NomalMap {get => s_NomalMap; }
        public Texture spp_MatallicMap {get => s_MatallicMap; }

        public MaterialInfo(Material _GetOGMat, Texture _GetOGMainTex,
        Texture _GetOGNormalMap, Texture _GetOGMatallicMap)
        {
            s_GetMat = _GetOGMat;
            s_BaseMap = _GetOGMainTex;
            s_NomalMap = _GetOGMainTex;
            s_MatallicMap = _GetOGMainTex;
        }
    }

    #endregion

    #region Collection_Storage Reference

    public enum CollectionPlayerSavedType
    {
        None = 0,
        Coll_Inventory,
        Coll_Amount
    }

    public class CollectionInitInfo
    {
        public string s_DBSavedIDX = string.Empty; //저장한 닉네임 혹은 송수신할 최종 키값에 해당
        public NewPlayerCharacterController s_GetCharacterController = null;
    }

    public class ItemInventoryInfo : CollectionInitInfo
    {
        public ItemInfo[] s_SavedItemInfo;

        public ItemInventoryInfo(ItemInfo[] _getInfos)
        {
            s_SavedItemInfo = _getInfos;
        }
    }

    #endregion

    //System GUI Infos
    #region Setting PopUp Reference

    //여기까지가 카테고리를 동작하기 위해 필요한것

    [System.Serializable]
    public struct SettingCatecoryInfo
    {
        public SettingCatrcoryType s_CatrcoryType;
        public SettingCategoryBase s_CategoryBase;
    }

    public enum SettingCatrcoryType
    {
        None = 0,
        Gameplay,
        Controller,
        Sound,
        Graphic
    }

    //여기까지가 카테고리내에 동작하는것
    [System.Serializable]
    public class SettingReferenceInfo
    {
        public RectTransform s_CatecoryBG;
        public SettingReferenceType s_OutPutReferType;
        public Text[] s_ReferTxtArray;
        public Image[] s_SendImgArray;
    }

    //차후 옵션에 설정값이 많아지면 해당 구간도 스크립터블 오브젝트로 전환 요망
    //또한 번역본은 전부 인덱스로만 전달한다
    [System.Serializable]
    public struct SettingPartOfButton
    {
        public Image s_CatecoryEventButtonBG;
        public SettingReferenceType s_OutPutReferType;
        public string[] s_ReferTxtArray;
        public Sprite[] s_SendSprArray;
        public Selectable s_UIEventAction;
    }

    public enum SettingReferenceType
    {
        None = 0,
        NonImgType,
        SingleImgType,
        CompareImgType
    }

    #endregion
}

