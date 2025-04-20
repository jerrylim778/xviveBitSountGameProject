using System.Collections.Generic;
using System.Collections;
using System.Linq;
using UnityEngine;
using Sirenix.OdinInspector;
using Commons.Helpers;
using ParsingPathType = PlatformManager.ParsingPathType;
using PairTwoStr = System.Tuple<string, string>;


public class AssetBundleManager : MonoBehaviour
{
    #region AssetBundleManager 클래스 설계에 대한 설명
    //1.기존 Project P의 어드레서블과 구조가 유사하다
    //UniTask로 사용할 수 있는 설계를 간구후 구조를 적용할 수 있도록 한다
    //2.필수로 NetworkManager<클래스>가 필요한데
    //해당 클래스는 필히 모듈 형태로 가져오되 결과값을 제이슨 혹은 제이슨 형식의 string으로
    //받아와서 바로 처리할 수 있는 형태로 설계가 되어야 한다
    //3.플렛폼 메니져를 통해 로컬 저장소에 저장할지 여부 + 로드하고 없애버릴지를 결정
    //(Web은 당연히 쿠기*(WebGL은 쿠기보단 IndexDB라는 웹만의 특화된 WebGL제공 데이터 저장소가 더 적합하다 (GPT 참고))
    //여야 하며 StreamAsset을 사용할 수 있는 전구간 마찬가지로 추가 옵션을 적용할 수 있도록 한다
    //4.에셋번들의 API를 잘 확인하여 데이터 파싱에 적합한지 오브젝트 인스턴쉐이팅에 적합한 함수인지
    //잘 구분하여 오버로딩화 작업을 겉힐 수 있도록 한다 (모듈형식으로 필요한 메니져를 긁어오되 그게 구조가 되면 안된다)
    //5.마지막으로 기존 메인 메카니즘 (CSV테이블 파싱 => 스크립터블 데이터화)에서 Bundle 파싱을 적절히 배치하여 
    //구조적으로 번들 파싱으로 로드하는 과정을 설계에 대한 전개도를 그린 후에 구체적인 작업을 할것
    #endregion

    #region 번들로드 규칙에 관한 설명
    //1.캐시로컬 경로에 번들이 있다면 해당 경로에 모든 번들을 가져온다
    //2.없다면 번들이 있는 서버로부터 파싱후 캐싱로컬에 저장한다
    //3.가져온 번들은 각 Object 환경에 따라 나뉜다
    //4.번들 이름은 {씬타입_카테고리타입_(서버기준임)최신 업데이트 날짜} 로 저장로드한다 
    #endregion

    #region Bundle Manager Infos(Datas)
    public enum BundleParsingType
    {
        SceneByBundleType,
        LoadOnceBundleType,//씬에 따라 나눠진 모든 번들을 한번에 로드하는 방식 (가급적 사용 x)
    }
    public enum SheaveBundleCatecoryType //기준에 따라 묶은 번들에 관한 타입
    {
        None = 0,
        DefultPrefab,
        MapPrefab,
        Texture,
        Material,
        AnimController, //Controller에 포함되어있는 Clips들도 전부 함께 묶여있어야 함
        AllOtherClips
    }
    #endregion

    [Header("Bundle Parsing Controll")]
    [SerializeField] private BundleParsingType m_LoadOnceAllBundle; 
    [SerializeField, ShowIf(nameof(DOIN_LoadAllBundleToggle))] private bool m_isFlammabilityBundle = false;
    #region ODIN Reference By AudioOutPutType
    private bool DOIN_LoadAllBundleToggle() => m_LoadOnceAllBundle == BundleParsingType.LoadOnceBundleType;
    #endregion
    [Header("Required Values")]
    [SerializeField, ReadOnly] private SetSceneType m_BundlePasingSceneNow;
    [SerializeField, ReadOnly] private System.DateTime m_LastUpdateTime;
    [SerializeField, ReadOnly] private PlatformManager.PlatformType m_PlatformType;

    [Tooltip("Required Values")]
    private bool m_isNotUseCacheParsing = false;
    private DelEventManager m_DelEventManager;
    private WebNetWorkManager m_WebNetWorkManager;
    private List<UnityEngine.Object> m_LoadBundleAssets = new();
    private Dictionary<SheaveBundleCatecoryType, AssetBundle> m_DicLoadBundles = new();

    public void Initlization()
    {
        m_PlatformType = Appinstance.Instance.ms_MainPlatform;
        m_WebNetWorkManager = Appinstance.Instance.ms_WebNetWorkManager;
        m_DelEventManager = Appinstance.Instance.ms_DelEventManager;
        m_isFlammabilityBundle = m_LoadOnceAllBundle == BundleParsingType.SceneByBundleType;

        Helper.GetEnumByArray<SheaveBundleCatecoryType>(true).HForEach(x => m_DicLoadBundles.Add(x, null));
        m_DelEventManager.DEL_MemoryAllResetDELEvntBy_ChangeScene += MemoryResetByABM;
    }

    private void MemoryResetByABM()
    {
        Helper.GetEnumByArray<SheaveBundleCatecoryType>(true).HForEach(x => 
        {
            m_DicLoadBundles[x].Unload(m_isFlammabilityBundle); //언로드 참이면 다시 진입할때 모든 과정을 다시 로드해야된다
            if (m_isFlammabilityBundle) m_DicLoadBundles[x] = null;
        });
        m_LoadBundleAssets.Clear();
    }

    #region Main System Public Functios

    public T LoadObjectByParsingBundle<T>(string _ParsingBundleName) where T : UnityEngine.Object
    {
        var GetTempArray = m_LoadBundleAssets.ToList().FindAll(x => x.name == _ParsingBundleName);
        if(GetTempArray.Count > 0)
        {
            T GetTemp = GetTempArray.Find(x => x is T) as T;
            return GetTemp;
        }
        #region 각 번들파싱한 Object의 속성이 아닌 이름으로만 파싱할때의 로직(**인식 안되는 문제에 의해 Sprite를 그대로 파싱함**)
        //if (m_LoadBundleAssets.ISFindCondition(x => x.name == _ParsingBundleName, out UnityEngine.Object _OutPutObj))
        //{
        //    if ((_OutPutObj is not T).HDebug($"{_ParsingBundleName} 는 " +
        //    $"{typeof(T).Name} 타입이 아닙니다.!", Helper.HDType.Error)) return null;
        //    return _OutPutObj as T;
        //}
        #endregion
        return null;
    }

    #endregion

    #region Main System Public Functios (Parsing Load)
    //정확히는 서버로부터 에셋번들을 파싱받은뒤
    //해당 파일로부터 비동기 번들 파싱을 진행한다(압축해제의 과정)

    public void LoadStartBundleParsing(SetSceneType _GetSceneType, long _UpdateTime, System.Action _EndCallBack = null)
    {
        m_BundlePasingSceneNow = _GetSceneType; 
        m_LastUpdateTime = new System.DateTime(_UpdateTime); //638689329008008127
        switch (m_LoadOnceAllBundle)
        {
            case BundleParsingType.SceneByBundleType:
                StartCoroutine(CO_LoadAllBySceneTypeAsync(m_LastUpdateTime, _EndCallBack));
                break;
            case BundleParsingType.LoadOnceBundleType:
                //[**보류**] 전씬에 따라 존재하는지의 여부또한 가려야 한다
                //CO_LoadDataBundleByCatecoryTypeAsync(m_BundlePasingSceneNow, _UpdateTime, _EndCallBack);
                break;
        }
    }

    //서버로부터 최신 업데이트된 시간을 알아야한다
    IEnumerator CO_LoadAllBySceneTypeAsync(System.DateTime _UpdateTime, System.Action _EndCallBack = null)
    {
        foreach (var x in Helper.GetEnumByArray<SheaveBundleCatecoryType>(true))
        {
            bool _isComplate = false;
            StartCoroutine(CO_LoadDataBundleByCatecoryTypeAsync(x, _UpdateTime, () => _isComplate = true));
            yield return new WaitUntil(() => _isComplate);
            yield return new WaitForSeconds(0.2f);
        }
        yield return new WaitForSeconds(0.3f);
        _EndCallBack?.Invoke();
    }

    
    IEnumerator CO_LoadDataBundleByCatecoryTypeAsync(SheaveBundleCatecoryType _CatecoryType, System.DateTime _UpdateTime, System.Action _EndCallBack = null)
    {
        //캐싱된 경로에서 번들없을시 서버 파일 요청
        AssetBundle ParsingBundle = null;
        AssetBundleCreateRequest GetRequestBundle = null;
        if ((!GenerateFileName(true, _CatecoryType, _UpdateTime.Ticks, out string OutFileName)).HDebug(
        "파일이름 생성에 필요한 조건이 성립되지 않습니다.!", Helper.HDType.Error)) yield break;

        m_isNotUseCacheParsing =
        (!GenerateFileName(false, _CatecoryType, _UpdateTime.Ticks, out string OutFilePath)).
        HDebug("파일이름 생성에 필요한 조건이 성립되지 않습니다.!", Helper.HDType.Warning);

        #region Request Bundle Parsing Reference
        if (m_isNotUseCacheParsing || !System.IO.File.Exists(OutFilePath))
        {
            bool _isComplateRequest = false;
            #region Get 송신으로 임의의 한개의 번들만 가져오는 방식
            //m_WebNetWorkManager.CallWebRequest(
            //UsingWebAPIType.UWRType, RequestURL.MainServerHeader, RestAPIType.Get_UnityBundle, ReciveReturnType.Bundle, _Bundle => 
            //{
            //    ParsingBundle = _Bundle as AssetBundle;
            //    _isComplateRequest = true;
            //});
            #endregion
            m_WebNetWorkManager.CallWebRequest(
            UsingWebAPIType.UWRType, RequestURL.MainServerHeader, RestAPIType.Post, ReciveReturnType.ByteArray, _Bundle =>
            {
                if (!m_isNotUseCacheParsing && _Bundle != null && _Bundle is byte[])
                {
                    IEnumerator IterAsync = PlatformManager.
                    SetPlatformSaveDataAsync(ParsingPathType.LocalBundle, OutFileName, _Bundle, () => _isComplateRequest = true);
                    if(IterAsync != null) StartCoroutine(IterAsync);
                }
                else
                {
                    if (m_isNotUseCacheParsing && (_Bundle != null && _Bundle is byte[] GetByteArray))
                    GetRequestBundle = AssetBundle.LoadFromMemoryAsync(GetByteArray);
                    _isComplateRequest = true;
                }

            }, m_WebNetWorkManager.SendPostCallRest(new PairTwoStr("ReciverClient", $"{_CatecoryType}"), new PairTwoStr("ClientToServerFileName", OutFileName)));
            yield return new WaitUntil(() => _isComplateRequest);
        }

        //캐싱된 경로에서 번들을 비동기 파싱
        if (!m_isNotUseCacheParsing && System.IO.File.Exists(OutFilePath)) 
        GetRequestBundle = AssetBundle.LoadFromMemoryAsync(System.IO.File.ReadAllBytes(OutFilePath));
        #endregion

        if (GetRequestBundle != null)
        {
            yield return new WaitUntil(() => GetRequestBundle.isDone);
            ParsingBundle = GetRequestBundle.assetBundle;
        }
        
        if (ParsingBundle == null)
        {
            _EndCallBack?.Invoke();
            yield break;
        }

        m_DicLoadBundles[_CatecoryType] = ParsingBundle;

        var ParsingAllObj = 
        m_DicLoadBundles[_CatecoryType].LoadAllAssets<UnityEngine.Object>();

        if(ParsingAllObj != null && ParsingAllObj.Length > 0)
        {
            #region 파싱하는 타입에 따라 번들내에 알맞은 타입이 존재하는지 검사로직 (안되서 현재는 보류됨)
            //(종속성 판단 에러이기 때문에 이후에 반드시 해당 로직을 정상 작동시킬것)
            //foreach (var x in ParsingAllObj)
            //if ((!CheckObjectByCatecoryType(_CatecoryType, x)).HDebug($"{x.name}값은 {_CatecoryType}타입이 아닙니다.!", Helper.HDType.Error))
            //{
            //    _EndCallBack?.Invoke();
            //    yield break;
            //}
            #endregion

            m_LoadBundleAssets.AddRange(ParsingAllObj);
        }

        #region 임시 파싱한 오브젝트 출력 확인시 추석해제후 사용할것
        //m_LoadBundleAssets.ToList().ForEach(x =>
        //{
        //    GameObject GetObj = Instantiate(x, Vector3.zero, Quaternion.identity) as GameObject;
        //    if (GetObj.TryGetComponent<Rigidbody>(out Rigidbody _GetRB))
        //        Destroy(_GetRB);
        //});
        #endregion

        _EndCallBack?.Invoke();
    }

    #endregion

    #region Sub System Private Functions

    //Gameobject 이외에 인식이 안되는 케스팅 에러가 존재함
    private bool CheckObjectByCatecoryType(SheaveBundleCatecoryType _GetType, UnityEngine.Object _ParsingObj) => _GetType switch
    {
        SheaveBundleCatecoryType.DefultPrefab => _ParsingObj is GameObject,
        SheaveBundleCatecoryType.MapPrefab => _ParsingObj is GameObject,
        SheaveBundleCatecoryType.Texture => _ParsingObj is Texture,
        SheaveBundleCatecoryType.Material => _ParsingObj is Material,
        SheaveBundleCatecoryType.AnimController => _ParsingObj is RuntimeAnimatorController,
        SheaveBundleCatecoryType.AllOtherClips => _ParsingObj is UnityEngine.Object,
        _ => false
    };

    private bool GenerateFileName(bool _GetOnlyFileName, SheaveBundleCatecoryType _CatecoryType, long _UpdateTickTime, out string _OutReferenceStr)
    {
        _OutReferenceStr = 
        $"{m_BundlePasingSceneNow.ToString().ToLower()}_{_CatecoryType.ToString().ToLower()}_{_UpdateTickTime}"; //번들 이름 => {씬타입||카테고리타입||최신 업데이트 날짜} 
        if (_GetOnlyFileName) return !string.IsNullOrEmpty(_OutReferenceStr);

        PlatformManager.GetPlatformLoadData(ParsingPathType.LocalBundle, _OutReferenceStr, false, out object _OutReference);
        _OutReferenceStr = _OutReference != null? _OutReference.ToString() : string.Empty;
        return !string.IsNullOrEmpty(_OutReferenceStr);
    }
    #endregion
}
