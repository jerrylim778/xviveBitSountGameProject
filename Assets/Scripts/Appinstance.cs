using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Sirenix.OdinInspector;

[DisallowMultipleComponent]
public class Appinstance : SingleTon<Appinstance>
{
    [Header("Connect Platform")]
    [ReadOnly] public PlatformManager.PlatformType ms_MainPlatform;

    [Header("None Mono")]
    
    public MyInfo ms_MyInfo;
    public DelEventManager ms_DelEventManager;
    public EventManager ms_EventManager;

    [Header("Plug Mono")]
    public TestManager ms_TestManager;
    public GamePlayManager ms_GamePlayManager;
    public DataManager ms_DataManager;
    //현재는 Test 상태
    public WebNetWorkManager ms_WebNetWorkManager; //Non Mono 로 활용될 수 있음
    public AssetBundleManager ms_AssetBundleManager;
    //현재는 Test 상태
    public AudioManager ms_AudioManager;
    public LoadingManager ms_LoadingManager;
    public SettingManager ms_SettingManager;
    public TranslateManager ms_TranslateManager;
    public ScriptableObjectManager ms_ScriptableObjectManager;

    [Tooltip("MyInfo Dpuil Reference")]
    private readonly string m_MyInfoResourcesPath = "Objects/DefultObject/InGame/Required/";
    public bool pp_isMyInfoInit { get => ms_MyInfo != null; }
    
    protected override void Awake()
    {
        base.Awake();
        if (!m_isOperatorMain) return;
        if (ms_MyInfo != null) ms_MyInfo = null;
        ms_MainPlatform = PlatformManager.InitPlatform();
        ms_DelEventManager = new DelEventManager();
        ms_EventManager = new EventManager();
        ms_GamePlayManager = FindAnyObjectByType<GamePlayManager>();
        ms_TestManager = FindAnyObjectByType<TestManager>();
        ms_DataManager = FindAnyObjectByType<DataManager>();
        //현재는 Test 상태
        ms_WebNetWorkManager = FindAnyObjectByType<WebNetWorkManager>();
        ms_AssetBundleManager = FindAnyObjectByType<AssetBundleManager>();
        //현재는 Test 상태
        ms_AudioManager = FindAnyObjectByType<AudioManager>();
        ms_SettingManager = FindAnyObjectByType<SettingManager>();
        ms_LoadingManager = FindAnyObjectByType<LoadingManager>();
        ms_TranslateManager = FindAnyObjectByType<TranslateManager>();
        ms_ScriptableObjectManager = FindAnyObjectByType<ScriptableObjectManager>();
    }

    private void Start()
    {
        if (!m_isOperatorMain) return;

        ms_TestManager?.Initlization();
        ms_DataManager?.Initlization();
        //현재는 Test 상태
        ms_WebNetWorkManager?.Initlization();
        ms_AssetBundleManager?.Initlization();
        //현재는 Test 상태
        ms_AudioManager?.Initlization();
        ms_LoadingManager?.Initlization();
        ms_SettingManager?.Initlization();
        ms_TranslateManager?.Initlization();
        ms_ScriptableObjectManager?.Initlization();
        ms_GamePlayManager?.Initlization();

    }

    public void ApplyMyInfo(MyInfo _GetInfo, out string _OutPutMyInfoPath)
    {
        _OutPutMyInfoPath = string.Empty;
        if (pp_isMyInfoInit && ms_MyInfo.Equals(_GetInfo))
        {
            Debug.LogErrorFormat("이미 MyInfo를 초기화가 진행되었습니다 MyInfo는 한번만 초기화가 가능합니다..!!");
            return;
        }
        ms_MyInfo = _GetInfo;
        _OutPutMyInfoPath = m_MyInfoResourcesPath;
    }
}

//Common으로 이전 요망
public interface I_CheckInit
{
    public void Initlization();

    public bool ISInitlization();
}

#region 싱글톤 로직 관련 이후에 이전조치 요망
public class SingleTon<T> : MonoBehaviour where T : MonoBehaviour
{
    [SerializeField] protected bool m_isUseDDOL = false;
    protected bool m_isOperatorMain = false;
    protected bool m_isOrderDestory = false;
    //public bool pp_isOperatorMain { get; private set; }

    public static T Instance
    {
        get
        {
            if (Reference == null && (Reference = FindObjectOfType<T>()) == null)
                Debug.LogWarningFormat("현재 씬에 {0}이라는 클래스가 존재하지 않습니다..!!", typeof(T).Name);

            return Reference;
        }
    }

    protected static T Reference = null;

    protected virtual void Awake() //sealed 으로 선언하여 자식에서는 다른방식으로 호출할 방법을 간구해야된다 (재설계 요망)
    {
        if (m_isUseDDOL)
        {
            //처음진입
            if (FindObjectsOfType<T>().OfType<SingleTon<T>>().ToList().TrueForAll(x => !x.CompareOnlyTempSameType(typeof(T))))
            {
                if (!this.Equals(FindObjectsOfType<T>().OfType<SingleTon<T>>().ToArray()[0])) return;
                DontDestroyOnLoad(this.gameObject); this.m_isOperatorMain = true; return;
            }

            if (!m_isOperatorMain)
            { m_isOrderDestory = true; Destroy(this.gameObject); return; }
        }
    }

    public bool CompareOnlyTempSameType(System.Type _CompareType)
    {
        if(_CompareType.Name != typeof(T).Name)
        {
            Debug.LogErrorFormat("해당 함수는 오직 같은 클래스 " +
            "{0} 끼리만 호출 가능합니다..!!", typeof(T).Name);
            return false;
        }

        return m_isOperatorMain;
    }

protected void Reset()
    {
#if UNITY_EDITOR
        if (FindObjectsOfType<T>().Length > 1)
        {
            UnityEditor.EditorUtility.DisplayDialog
            ("Singleton Error", $"There should never be more than 1 reference of {typeof(T).Name}!", "OK");
            DestroyImmediate(this);
        }
#endif
    }
}

#endregion

#region Scritpable Item Infos
[System.Serializable]
public class ItemInfo
{
    [Header("======SData Required Values======")]
    public int s_ItemIDX = -1;
    public string s_ItemName;
    public ItemType s_ItemType;
    public ScriptableDataType s_ScriptableDataType;
    public SDataLoadParsingType s_LoadObjectParsingType;
}

//(V) 적용한 버전이 존재하다면 (V)표시를 주석으로 적시 요망..!!
public enum ItemType
{
    None = 0,
    CharacterType,
    PotionType,
    PartsType,
    QuestType, //아이템에 어디에 사용할 퀘스트까지 같이 역고 그 역활을 퀘스트의 처리 조건인 QuestReference에서 진행한다
    QuestReferenceType, //아이템에 어디에 사용할 퀘스트까지 같이 역고 그 역활을 퀘스트의 처리 조건인 QuestReference에서 진행한다
    MapType, //(V)
    ShaderType,
    RunnerBaseInfoType,
    RunnerDefultItemType,
    RunnerSelectItemType,
    RunnerSpecialItemType,
    #region Before UsedItemTypes
    //TYCGameContentType,
    //TYCGameBlockType,
    //TYCGameDefultLevelType,
    //MFLGameItemType,
    //MFLGameLevelType,
    //ThreeMatchGameType,
    //ThreeMatchBachType,
    #endregion
}

#region ItemInfo Main Parent Reference
public enum VarOutPut_ItemParentType
{
    None = 0,
    DescirptionInfoType,
    InstanceSpriteInfoType,
    InstanceObjectInfoType,
    InstanceAnimType,
}

[System.Serializable]
public class DescirptionInfo : ItemInfo
{
    [Header("======SData Custom======")]
    [Sirenix.OdinInspector.TabGroup("Tabs", "Descirption")]
    [TextArea(5, 10)] 
    public string s_Descirption;
}
[System.Serializable]
public class InstanceSpriteInfo : DescirptionInfo
{
    [Sirenix.OdinInspector.TabGroup("Tabs", "Sprits")]
    [Commons.Helpers.Attribute.Lim_SpritePriview(100, 100)]
    public Sprite[] s_SpriteArray;
}
[System.Serializable]
public class InstanceObjectInfo : InstanceSpriteInfo
{
    [Sirenix.OdinInspector.TabGroup("Tabs", "Prefab")]
    public GameObject s_PrefabObjs;
}

[System.Serializable]
public class InstanceAnim : InstanceObjectInfo
{
    [Sirenix.OdinInspector.TabGroup("Tabs", "AnimController")]
    public RuntimeAnimatorController s_RunTimeAnim;
    //public AudioClip
}

public class OtherAllClips : InstanceObjectInfo
{
    [Sirenix.OdinInspector.TabGroup("Tabs", "OtherClips => AudioClip")]
    public AudioClip[] s_AudioClipsInfo;
    //[Sirenix.OdinInspector.TabGroup("Tabs", "OtherClips => AnimClip")]
    //public AnimationClip[] s_AnimClipsInfo;
    [Sirenix.OdinInspector.TabGroup("Tabs", "OtherClips => VedioClip")]
    public UnityEngine.Video.VideoClip[] s_VidioClipsInfo;
}

#endregion


#region Character Reference

public enum CharacterType { None = 0, MainUserCharacter, Mob, NPC }

[System.Serializable]
public class CharacterItemInfo : OtherAllClips, I_Data 
{
    [Header("======ItemInfo => Character Reference======")]
    public CharacterType s_CharacterType;
    public Commons.PowerType s_CharacterUsePowerType;
    public int s_NewActionInfoIDX;
    public int s_CalculateInfoIDX;
    //public int s_InputPowerIDX;
    //[Header("Not Parsing Data")]
    //[System.NonSerialized, ShowInInspector, ReadOnly, ShowIf("@s_CharacterUsePowerType != Commons.PowerType.NotNeedAnyPower")] 
    //public Commons.PowerType s_CharacterUsePowerType;
}

public class SkillCharacterItemInfo : CharacterItemInfo
{
    public SelectJobType s_SelectJobType;
}

#endregion

#region Amount Reference

[System.Serializable]
public class PartsItemInfo : InstanceObjectInfo
{
    public UnityEngine.HumanBodyBones s_PartsType;
    public string s_SubScriptableItemIDX;
    public Material s_NeedMaterial;
}

[System.Serializable]
public class GearItemInfo : PartsItemInfo
{
    public int Defence;//방어력
    public int Speed; //이속능력
}

[System.Serializable]
public class WeaponItemInfo : PartsItemInfo
{
    //public WeapownType s_WeapownType;
    //public WeaponControll s_WeapownControll;
    public int Accuracy; //정확도
    public int Damege; //피해량
    public int FireRate; // 사거리
    public int Mobility; // 사격속도
    public int Stability; // 안정성
}

#endregion

#region Skill Reference

//원래는 전직에 따른 스킬들에 모음을 가져오는 역활 =>
//ThreeMatch 타입에 따라 나와야하는 Anim + 로직을 넣음
public enum SelectJobType
{
    None = 0,
    ThreeMatchItemType1,
    //ThreeMatchItemType2 //앞으로 아이템의 종류가 추가되면 새로운 타입에 따라 추가할것
    //BerserkerType, //원래 사용 용도 예시
    //Knight
}

#endregion

#region Quest Reference

//[System.Serializable]
//public class QuestReferenceInfo : DescirptionInfo
//{
//    public bool s_isQuestDone;
//    public QuestReferenceType s_QuestReferenceType;
//    public ApplyInventoryHaveInfo[] s_ApplyInvenHaveInfo; //위 사항 구현 이후에 삭제 요망
//    public QuestRewordInfo[] s_QuestRewordInfo;
//}

#endregion

#region Production Reference

//[System.Serializable]
//public class ProductionCollectionItemInfo : ItemInfo
//{
//    public int[] s_ElemItemIDXs; //각 스크립터블 아이템을 값을 파싱해서 가져올건지에 대한 로직
//    public CollectionProductionInfo s_CollectionProductionInfo;
//}

//[System.Serializable]
//public class ProductionItemInfo : ItemInfo
//{
//    //어떤 로직실행 이후에 TimeLine이 발동될 수 있도록(Collection에서만 단일당 제어가 가능하다) (보류)
//    public bool s_isWaitForThisStack; 
//    public bool s_isTimeLineForceDone; 

//    public ProductionApplyTarget s_PATarget;
//    public ProductionInfo s_ProductionInfo;
//}

//[System.Serializable]
//public class CameraProductionInfo : ProductionItemInfo, I_ProductionLocalLogic
//{
//    //public enum CameraLocalLogicType { ShakeCam,  } //만일 단일 로직으로써 이외에도 더 있다면 타입으로 나눠야함

//    public bool s_isUseOGCam; //카메라 본체에서 적용할 사항이 있는지의 여부
//    public bool s_isChildSet; //자식에서 연출할 요소가 있는지의 여부
//    public float s_ApplyPowerValue;
//    public GameObject[] s_ApplyChildObjs; //자식의 인덱스관련(Volume 혹은 쉐이더 등등이 포함됨)
//    //public int[] s_ApplyChildCount; //자식의 인덱스관련(Volume 혹은 쉐이더 등등이 포함됨)
//    public void SetLocalLogicAction(Object _GetObj = null)
//    {
//        //Camera GetCam = null; GetCam = _GetObj as Camera;
//        //if (GetCam == null) GetCam = Object.FindObjectOfType<CameraController>().GetComponent<Camera>();
//        Camera GetCam = _GetObj as Camera;
//        GetCam.DOShakePosition(0.85f, UnityEngine.Random.insideUnitCircle, (int)s_ApplyPowerValue, 0, false);
//        //GetCam.DOShakeRotation(0.85f, Vector3.forward, 1, 1, false);
//    }
//}

//[System.Serializable]
//public class ObjectsProductionInfo : ProductionItemInfo, I_ProductionLocalLogic
//{
//    public enum ObjectsLocalLogicType { None, Particle, ShaderOffEmission }

//    public bool s_isUseOGObj; //자식에서 연출할 요소가 있는지의 여부
//    public bool s_isChildSet; //자식에서 연출할 요소가 있는지의 여부
//    public ObjectsLocalLogicType s_ObjectsLocalLogicType;
//    public float s_ApplyPowerValue;
//    public GameObject s_MainControllObj;
//    public ObjectsSubPartChildInfo[] s_ObjChildInfo;
//    //public int[] s_ApplyChildIDXs; 
//    public void SetLocalLogicAction(Object _GetObj = null)
//    {
//        int FIDX = -1; FIDX = s_ObjChildInfo.ToList().FindIndex(x => 
//        _GetObj.GetInstanceID() == x.s_ApplyObj.GetInstanceID());
//        if (s_MainControllObj == null && FIDX == -1)
//        {
//            Debug.LogErrorFormat("오브젝트 연출에서 개별 로직을 호출하고 싶다면 인덱스로 찾는게 아닌" +
//            "오브젝트 형식으로 찾아야 합니다..!!"); return;
//        }
//        ObjectsLocalLogicType GetType = s_MainControllObj != null ? 
//        s_ObjectsLocalLogicType : s_ObjChildInfo[FIDX].s_ObjectsChildType;
//        switch (GetType)
//        {
//            case ObjectsLocalLogicType.Particle:
//                ParticleSystem GetParticle = (_GetObj as GameObject).GetComponent<ParticleSystem>();
//                if (GetParticle.isPlaying) GetParticle.Stop();
//                GetParticle.Play();
//                break;
//            case ObjectsLocalLogicType.ShaderOffEmission: //이거 방법 있으니 참고하여 HDR을 꺼지거나 켜질 수 있도록 하자
//                Renderer GetRender = (_GetObj as GameObject).GetComponent<Renderer>();
//                if (GetRender.material.shader.name != "Universal Render Pipeline / Lit") return;
//                GetRender.sharedMaterial = Object.Instantiate(GetRender.sharedMaterial);
//                GetRender.sharedMaterial.SetColor("_EmissionColor", Color.black); //HDR을 끝다는 의미 White면 체크상태 유지
//                break;
//        }

//    }
//}

//#region Objects Sub Helper Infos

//[System.Serializable]
//public struct ObjectsSubPartChildInfo
//{
//    public bool s_isUseTimeLineThisObj;
//    public bool s_isDestroyWhenDone;
//    public ObjectsProductionInfo.ObjectsLocalLogicType s_ObjectsChildType;
//    public GameObject s_ApplyObj; //오브젝트를 재활용하여 적용할때
//    public int s_ApplyChildIDX;
//    public StaticTimeLineTrackInfo s_ApplyTLTrackInfo;
//}

//[System.Serializable]
//public struct StaticTimeLineTrackInfo
//{
//    public string s_TrackName;
//    public int s_ApplyTimeLineIDX;
//}

//#endregion

//[System.Serializable]
//public class CharacterProductionInfo : ProductionItemInfo
//{
//    [Header("Controll Main Header")]
//    public bool s_isNotMineCharacter; //내 캐릭터인지의 여부
//    [Header("OnlyUse Other Character")]
//    public bool s_isDestroyWhenDone; //상대 오브젝트 연출이후 삭제할지의 여부
//    [Header("OnlyUse My Character")]
//    public bool s_isOnlyLockAction; //그냥 정적인 상태를 유지만 하고 싶을때 사용
//    public bool s_isLerpMoveStartPoint; //시작지점에 보간 이동후 연출하길 원할때(내 캐릭터에만 적용 가능)
//    public GameObject s_InstancePrefabs; //내가 아닌 캐릭터의 OutPut을 진행할때 사용 (후에 분리 처리한다)
//    public ActionInfo s_ActionInfo;
//}

//public interface I_ProductionLocalLogic
//{
//    public void SetLocalLogicAction(Object _GetObj = null);
//}

#endregion

#region Map Reference
//1. Map과 관련되서는 이후에 추가될 사항을 한씬에 비동기적으로 배치하는데에 있다
//2. 현재는 정적으로 모든 씬의 맵들이 배치될 예정이여서 그러한것들을 각 맵Info에 따라
//존재하는 씬내의 맵과 비교하여 (Tag로 전체 씬의 맵을 비교할 수 있도록) 존재한다면 리턴하는 방향으로
//넘길 수 있도록 설계한다 (가지고 있는건 DataManager<클래스>에서 임시로 가지고 있다가 ScriptableObjectData로 옮길 수 있도록 한다)

[System.Serializable]
public class MapInfo : InstanceObjectInfo
{
    [Header("======MapData Reference======")]
    //여긴 맵의 정보만 있어야 한다 맵의 저장과 로드는 MyInfo를 참고하여 다른경로로 저장로드해야한다
    public bool s_isOnlyInit; // 오직 초기화 상태만 적용하는것들(타이틀 or 튜토리얼 등등) //s_isActionTweening;
    public bool s_isUseStaticScene; //씬에 정적으로존재하는지의 여부
    public UnityEngine.Rendering.VolumeProfile s_MainDefultVolumeProfile;
    public Light s_MainDirectionalLight;
    public Material s_SkyBox;
    public Color s_FogColor;
    public float s_FogDensity;
    //public ProgressUpdateInfo[] s_ProgressUpdateInfo;
    //public InGameProgressInfo[] s_MapInGameProgressInfos; //각 맵마다 진행도에 관한 데이터를 열거한것
}

public enum MapDifficultyType
{
    None = 0,
    Easy,
    Normal,
    Hard,
    Master
}

#endregion

#endregion