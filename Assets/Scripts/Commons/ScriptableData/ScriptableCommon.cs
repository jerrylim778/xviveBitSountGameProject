using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Commons.Helpers;

#region Item 관련 스크립터블 데이터 관리에 관한 설명
//<**ScriptableCommon의 설명**>
//1.각 파싱되어 저장하게 되는 Class들을 스크립터블로 담을로 지정
//담는것 대부분이 클래스 형태로 묶여있다
//2.로컬 or 서버 or 인위적추가에 따라 각 클래스는 그 형태를 명시해야된다
//(그래야 나중에 제거나 동적 추가 삭제의 범위를 나눠 처리할 수 있다)

//현재까지 스크립터블로 전환해야하는 클래스 종류들
//ItemPotionInfo
//ItemGearInfo
//ItemWeapownInfo
#endregion


//오직 로컬 리소스 로드일때만 (서버는 파일이름으로 탐색하는 방식을 검토)
public abstract class ScriptableCommon : ScriptableObject 
{
    #region Common Required Values (계산 문제 때문에 지금은 사용하지 않음)
    //public enum RePathType
    //{
    //    None = 0,
    //    DefultObjectType,
    //    SpriteType,
    //    AnimClipType,
    //    AudioClipType,
    //    MatrialType,
    //    SubSDataType
    //}

    //[System.Serializable]
    //public struct ResourcesLoadPathInfo
    //{
    //    public RePathType m_RePathType;
    //    public string m_RePathStr;
    //}
    //[SerializeField] private ResourcesLoadPathInfo[] m_ResourcesLoadPathInfos;
    #endregion

    [Header("ResourcesLoadOnly")]
    #region 각 문자열로 파싱할경우
    [SerializeField] protected string ReGameObjectLoadPath = string.Empty;
    [SerializeField] protected string ReSpriteLoadPath = string.Empty;
    [SerializeField] protected string ReAnimationControllerLoadPath = string.Empty;
    [SerializeField] protected string ReAudioClipLoadPath = string.Empty;
    [SerializeField] protected string ReMaterialLoadPath = string.Empty;
    [SerializeField] protected string ReSubScriptableObjectLoadPath = string.Empty;
    #endregion

    [Tooltip("CheckIntanceError")]
    protected bool m_isProcessAction = false;
    protected ScriptableObjectManager m_ScriptableObjectManager;
    [HideInInspector] public List<string> pp_CheckErrorStack { get; protected set; }
    [HideInInspector] public List<ItemInfo> pp_DefultItemInfos { get; protected set; }

    #region Scriptable Common Required Public Functions

    public abstract ItemType ReciverGetType(ItemType _CompareType = ItemType.None);

    public abstract void Initialization(ScriptableObjectManager _GetManager);

    public abstract void DestroyElementByDB();

    public virtual void InPutDataByItem(string _TableElement, out bool _isComplateReciverInData)
    => _isComplateReciverInData = true;

    public virtual void InPutDataByItem(string _TableElement, System.Type _FindType, out bool _isComplateReciverInData)
    => _isComplateReciverInData = true;

    public abstract void UpdateProcess();

    #endregion

    #region Virtual Main Ssytem Functions

    public virtual ItemInfo BaseInPutDataByItem<T>(T ReturnType, string _TableElement, out string[] _LeftOutSplitTables) where T : ItemInfo
    {
        #region 타입을 지역으로 비교하여 리턴 방법(하드코딩 발전 때문에 보류)
        //ItemInfo ReturnType = typeof(T).Name switch 
        //{
        //    nameof(MapInfo) => new MapInfo(),
        //    nameof(ThreeMatchGameItemInfo) => new ThreeMatchGameItemInfo(),
        //    nameof(ThreeMatchGameItemBachInfo) => new ThreeMatchGameItemBachInfo(),
        //    _ => null
        //};
        #endregion
        int ReturnCount = 5;
        string[] SplitTables = _TableElement.Split(',');
        ReturnType.s_ItemIDX = int.Parse(SplitTables[0]);
        ReturnType.s_ItemName = SplitTables[1];
        ReturnType.s_ItemType = Helper.StringToEnum<ItemType>(SplitTables[2]);
        ReturnType.s_ScriptableDataType = Helper.StringToEnum<ScriptableDataType>(SplitTables[3]);
        ReturnType.s_LoadObjectParsingType = Helper.StringToEnum<SDataLoadParsingType>(SplitTables[4]);

        if (ReturnType is DescirptionInfo ApplyDescription)
        {
            ApplyDescription.s_Descirption = SplitTables[ReturnCount];
            ReturnCount++;
        }
        if (ReturnType is InstanceSpriteInfo ApplySprArray)
        {
            ApplySprArray.s_SpriteArray = ParsingTempByStrElem<Sprite>(SplitTables[ReturnCount], ApplySprArray.s_LoadObjectParsingType);
            #region 탬플릿화 함수를 각각 풀어 쓸때의 이전 로직 (**ParsingTempByStrElem()함수의 이상 감지시 아래 주석 해제후 사용 요망**)
            //윗부분은 LoadType과 같은 부분으로 템플릿화 함수
            //string[] GetSprArray = Helper.SplitStrValue(SplitTables[6], (object)'?');
            //int CountIDX = 0; ApplySprArray.s_SpriteArray = new Sprite[GetSprArray.Length];
            //GetSprArray.ToList().ForEach(x =>
            //{
            //    ApplySprArray.s_SpriteArray[CountIDX] = LoadType<Sprite>(ApplySprArray.s_LoadObjectParsingType, x);
            //    CountIDX++;
            //});
            #endregion
            ReturnCount++;
        }
        if (ReturnType is InstanceObjectInfo IntanceObj)
        {
            IntanceObj.s_PrefabObjs = LoadType<GameObject>(IntanceObj.s_LoadObjectParsingType, SplitTables[ReturnCount]);
            ReturnCount++;
        }
        if (ReturnType is InstanceAnim IntanceAnim)
        {
            IntanceAnim.s_RunTimeAnim = LoadType<RuntimeAnimatorController>(IntanceAnim.s_LoadObjectParsingType, SplitTables[ReturnCount]);
            ReturnCount++;
        }
        if (ReturnType is OtherAllClips OtherClips)
        {
            OtherClips.s_AudioClipsInfo = ParsingTempByStrElem<AudioClip>(SplitTables[ReturnCount], OtherClips.s_LoadObjectParsingType);
            /*if(OtherClips.s_AudioClipsInfo.Length > 0) */ReturnCount++;

            OtherClips.s_VidioClipsInfo = ParsingTempByStrElem<UnityEngine.Video.VideoClip>(SplitTables[ReturnCount], OtherClips.s_LoadObjectParsingType);
            /*if (OtherClips.s_VidioClipsInfo.Length > 0) */ReturnCount++;
        }

        _LeftOutSplitTables = SplitTables.ToList().GetRange(ReturnCount, SplitTables.Length - ReturnCount).ToArray();
        return ReturnType;
    }

    #endregion

    #region Virtual Sub System Functions

    public virtual List<int> CheckDuplicateKeyIDX(ItemType _CompareType)
    {
        List<int> GetKeys = new List<int>();
        pp_DefultItemInfos.ForEach(x => GetKeys.Add(x.s_ItemIDX));
        return GetKeys;
    }

    protected virtual string ApplyErrorMessageByParsingItem(ItemInfo _GetInfo, bool _isAddParents)
    {
        string SetErrorMge = string.Empty;
        if(_GetInfo.s_ItemIDX == -1 || string.IsNullOrEmpty(_GetInfo.s_ItemName) || _GetInfo.s_ItemType == ItemType.None) 
        SetErrorMge += "가져올 유일키값 인덱스, 이름 , 타입, ";
        if (CheckSDataByTemp<DescirptionInfo>(_GetInfo, "s_Descirption")) SetErrorMge += "인스턴싱할 이미지, ";
        if (CheckSDataByTemp<InstanceObjectInfo>(_GetInfo, "s_PrefabObjs")) SetErrorMge += "인스턴싱할 오브젝트, ";
        if (CheckSDataByTemp<InstanceAnim>(_GetInfo, "s_RunTimeAnim")) SetErrorMge += "인스턴싱할 애니, ";
        
        if (_isAddParents && !string.IsNullOrEmpty(SetErrorMge)) pp_CheckErrorStack.Add(
        string.Format("아이템 키 {0} 에서 {1} 가 없습니다...!!", _GetInfo.s_ItemIDX, SetErrorMge));

        return SetErrorMge;
    }

    protected bool CheckSDataByTemp<T>(ItemInfo _GetInfo, string _VarName, bool _isArray = false) where T : ItemInfo
    {
        if (!(_GetInfo is T)) return false;
        return !Helper.GetFieldNameToClass((_GetInfo as T), _VarName, true, out System.Reflection.FieldInfo _GetOutField);
    }

    #endregion

    #region Virual Sub System Functions (Load Reference)

    private System.Func<string, T> ReturnParsingTemp<T>(SDataLoadParsingType _pType = SDataLoadParsingType.None)
    {
        return _Temp =>
        {
            switch (typeof(T).Name)
            {
                #region Example Other Data Type
                //case short ApplyShort: return (T)(short.Parse(_Temp) as object);
                //case long ApplyLong: return (T)(long.Parse(_Temp) as object);
                #endregion
                case nameof(System.String): return (T)(_Temp.ToString() as object);
                case nameof(System.Int32): return (T)(int.Parse(_Temp) as object);
                case nameof(System.Single): return (T)(float.Parse(_Temp) as object);
                case nameof(GameObject): return (T)(LoadType<GameObject>(_pType, _Temp) as object);
                case nameof(Sprite) : return (T)(LoadType<Sprite>(_pType, _Temp) as object);
                case nameof(AudioClip) : return (T)(LoadType<AudioClip>(_pType, _Temp) as object);
                default: return default;
            }
        };
    }

    protected virtual T[] ParsingTempByStrElem<T>(string _SplitTablesElement, SDataLoadParsingType _pType = SDataLoadParsingType.None)
    {
        string[] GetTMBachDefult = Helper.SplitStrValue(_SplitTablesElement, (object)'?');
        List<T> ReturnValue = new();
        foreach (string x in GetTMBachDefult) ReturnValue.Add(ReturnParsingTemp<T>(_pType).Invoke(x));
        return ReturnValue.ToArray();
    }

    //갔다와서 Sprite와 그밖에 넘겨야할 사항을 전부 다시 번들화하여 가져올 수 있도록 수정
    protected virtual T LoadType<T>(SDataLoadParsingType _pType, string _FileName) where T : Object
    {
        #region 에셋번들일경우(나중에 관리자 설계할때 생각하자)
        //있다면 캐시폴더를 뒤져서 없다면
        //번들 캐시 폴더에 없다면 서버에 요청하여 단일 번들 파일만 가져올 수 있도록
        //파일이름으로 서버DB에서 찾아 서버DB에서 해당 요소만 말아서 보내주고
        //끝나거나 나갈때 없애주거나 나중에 다시 탐색하기 위해 놓는방식을 채택해도 좋다
        #endregion
        switch (_pType)
        {
            case SDataLoadParsingType.ResourcesLoad:
                string FinalPath = (typeof(T).Name == nameof(GameObject) ||
                     typeof(T).Name == nameof(Light) ? ReGameObjectLoadPath :
                     typeof(T).Name == nameof(Sprite) ? ReSpriteLoadPath :
                     typeof(T).Name == nameof(RuntimeAnimatorController) ? ReAnimationControllerLoadPath :
                     typeof(T).Name == nameof(AudioClip) ? ReAudioClipLoadPath :
                     typeof(T).Name == nameof(Material) ? ReMaterialLoadPath :
                     ReSubScriptableObjectLoadPath) + _FileName;
                T ReturnValue = Resources.Load<T>(FinalPath);
                return ReturnValue;
            case SDataLoadParsingType.BundleLoad:
                return  Appinstance.Instance.ms_AssetBundleManager.LoadObjectByParsingBundle<T>(_FileName);
            case SDataLoadParsingType.AdressableLoad: //관련된 함수 구현 요망
                //return Appinstance.Instance.ms_AssetBundleManager.LoadObjectByParsingBundle<T>(_FileName); 
                break;
                
        }
        return null;
    }

    #endregion
}

#region Scriptable Info reference Common에 이전요망...
public enum SDataLoadParsingType
{
    None = 0,
    ResourcesLoad,
    BundleLoad,
    AdressableLoad
}

public enum ScriptableDataType
{
    Artificial, //None에 해당하며 디폴트는 인위적인 값이라는 뜻    
    SeverDB,
    LocalDB
}
#endregion

#region 모두 각 스크립트로 전환 (스크립터블이 꺼지고 켜질때 인식하지 못함

//[CreateAssetMenu(fileName = nameof(ScriptableItemPotionInfo)
//    , menuName = "ScriptableObject/ItemPotionInfo", order = int.MaxValue)]
//public class ScriptableItemPotionInfo : ScriptableObject
//{
//    [SerializeField] private List<ItemPotionInfo> m_SItemPotionInfo = new List<ItemPotionInfo>();
//    public List<ItemPotionInfo> pp_SItemPotionInfo { get { return m_SItemPotionInfo; } }
//}

//[CreateAssetMenu(fileName = nameof(ScriptableItemGearInfo)
//    , menuName = "ScriptableObject/ItemGearInfo", order = int.MaxValue)]
//public class ScriptableItemGearInfo : ScriptableObject
//{
//    [SerializeField] private List<ItemGearInfo> m_SItemGearInfo = new List<ItemGearInfo>();
//    public List<ItemGearInfo> pp_SItemGearInfo { get { return m_SItemGearInfo; } }
//}


//[CreateAssetMenu(fileName = nameof(ScriptableItemWeappownInfo)
//    , menuName = "ScriptableObject/ItemWeappownInfo", order = int.MaxValue)]
//public class ScriptableItemWeappownInfo : ScriptableObject
//{
//    [SerializeField] private List<ItemWeapownInfo> m_ItemWeapownInfo = new List<ItemWeapownInfo>();
//    public List<ItemWeapownInfo> pp_ItemWeapownInfo { get { return m_ItemWeapownInfo; } }
//}

#endregion