using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using Commons.Editor.EditorCommons;
using Commons.Editor.EditorCommons.Helpers;
using Commons.Helpers;

#region 현재는 에디터 메니져는 사용하지 않지만 이후에 EditorWindow나 프러퍼티 전체를 이용할때 사용할것으로 예상..)
//[CustomEditor(typeof(SDataMapInfo))]
//[CanEditMultipleObjects] //에디터 코드 사용시 여러 오브젝트에 동시 선택시 동일하게 적용가능하도록 만들어줌
public sealed partial class EditorManager : Editor
{
    #region 오직 테스트 용도이다 이후에 삭제 요망...
    //private SerializedProperty pp_TestPP;
    //private SDataMapInfo pp_TestMapInfo;

    //void OnEnable()
    //{
    //    pp_TestPP = serializedObject.FindProperty("m_SItemMapInfo");
    //    pp_TestMapInfo = (target as SDataMapInfo);
    //}

    //public override void OnInspectorGUI()
    //{
    //    // 각 에디터에서 사용하고자 하는 이벤트의 싱크를 맞춰준다나 뭐라나
    //    serializedObject.Update();

    //    GUIStyle GetHelpBox = EditorStyles.helpBox;
    //    GUILayout.BeginVertical(GetHelpBox);
    //    EditorGUILayout.PropertyField(pp_TestPP, true);
    //    if(GUILayout.Button("Generate MapInfos"))
    //    {
    //        pp_TestMapInfo.pp_DefultItemInfos.Add(new ItemInfo() { s_ItemType = ItemType.MapType });
    //    }
    //    GUILayout.EndVertical();

    //    serializedObject.ApplyModifiedProperties();
    //}
    #endregion
}
#endregion

#region EditorWindow Reference

#region EditorWindowByTableParsing 사용 목적 설명
//로컬 + 서버(추후 함께 배치되는 형태로 진행할 예정)DB를
//모두 파싱하여 보여주고 그것을 전부 모듈형태로 볼 수 있는 형태로 변경
//해당 파싱은 중복 누락 검사 + 로컬 테이블은 TextAsset형태로 전환할 수 있도록
//서버는 단순 검사만 진행하는 구간으로 넣는다
//전체 검사와 파싱 버튼이 존재하며 각각은 따로 파싱하는것과 검사가 있다
#endregion
public class EditorWindowByTableParsing : EditorWindow
{
    [Tooltip("Required By This EditorWindows")]
    [SerializeField] private List<GearItemInfo> m_EditorGearInfo = new List<GearItemInfo>();
    [SerializeField] private List<WeaponItemInfo> m_EditorWeaponInfo = new List<WeaponItemInfo>();
    [SerializeField] private List<MapInfo> m_EditorMapInfo = new List<MapInfo>();
    private List<System.Tuple<string, List<ItemInfo>>> m_ItemInfoAllResources;
    private SerializedObject m_EditorInstanceObjSupport;
    private SerializedProperty[] m_EditorInstancePPSupports;
    private Vector2[] m_GetAddScrollPartArray;
    private bool[] m_isShowHoldArray;//, m_isScrollsActive;
    private bool m_isProcessAction = false;

    [Tooltip("EditorWindows Sub Infos")]
    private string m_BlockingNotice = string.Empty;
    private Dictionary<string, Texture2D> m_EditorWindowApplyTexInfos;

    private void OnEnable()
    {
        //[매우중요 !!!] 일전에 서버와 로컬에 있는 DB를 전부 파싱해서 넣어야한다 
        //ScriptableObjectManager 클래스의 기능들을 전부 가져와 파싱해서 넣을 수 있도록 하자
        #region Required By This EditorWindows Init
        m_ItemInfoAllResources = new List<System.Tuple<string, List<ItemInfo>>>(3)
        {
            new System.Tuple<string, List<ItemInfo>>("m_EditorGearInfo", m_EditorGearInfo.ChangeTypeByList<GearItemInfo, ItemInfo>()),
            new System.Tuple<string, List<ItemInfo>>("m_EditorWeaponInfo", m_EditorWeaponInfo.ChangeTypeByList<WeaponItemInfo, ItemInfo>()),
            new System.Tuple<string, List<ItemInfo>>("m_EditorMapInfo", m_EditorMapInfo.ChangeTypeByList<MapInfo, ItemInfo>()),
        };
        m_EditorInstanceObjSupport = new SerializedObject(this);
        m_EditorInstancePPSupports = new SerializedProperty[m_ItemInfoAllResources.Count];
        m_isShowHoldArray = new bool[m_ItemInfoAllResources.Count];
        int CountIDX = 0; m_ItemInfoAllResources.ToList().ForEach(x =>
        {
            m_EditorInstancePPSupports[CountIDX] = m_EditorInstanceObjSupport.FindProperty(x.Item1); 
            CountIDX++;
        });
        #endregion

        #region EditorWindows Sub Infos Init
        m_EditorWindowApplyTexInfos = new Dictionary<string, Texture2D>();
        #region 배치할 스크롤뷰에 관한 초기화 
        m_GetAddScrollPartArray = new Vector2[1]; //[4]; 현재는 메인 스크롤뷰만 작동할 수 있도록 설계함
        //m_isScrollsActive = new bool[m_GetAddScrollPartArray.Length];
        //m_GetAddScrollPartArray.ToList().ForEach(x => x = Vector2.zero);
        #endregion
        m_EditorWindowApplyTexInfos.Add(
        "RefreshTex", AssetDatabase.LoadAssetAtPath<Texture2D>(
        EditorHelper.CheckPathIsNotCreate("Assets/AssetReference/SampleTex/RefeshICON.png")));
        #region 관련된 스크립터블 데이터를 초반에 생성하고자 할때
        string[] GetStrByPP = new string[1] { nameof(SDataMapInfo) };
        GetStrByPP.ToList().ForEach(x =>
        {
            string ApplyPath = EditorHelper.CheckPathIsNotCreate(
            string.Format("Assets/Resources/Datas/ScriptableData/{0}.asset", x));

            //if (AssetDatabase.LoadAssetAtPath(ApplyPath, System.Type.GetType(x)) == null)//해당 방법으로 시도시 계속 통과됨
            if (Resources.Load<ScriptableCommon>(ApplyPath.SplitStrArrayValue(new string[] { "Resources/", ".asset" })[1]) == null)
            {
                ScriptableCommon GetSCommon = (ScriptableCommon)ScriptableObject.CreateInstance(x);
                AssetDatabase.CreateAsset(GetSCommon, ApplyPath);
                AssetDatabase.SaveAssets();
                EditorUtility.FocusProjectWindow();
                Selection.activeObject = GetSCommon;
            }
        });
        #endregion
        #endregion

        m_isProcessAction = true;
    }

    void OnGUI()
    {
        if (!m_isProcessAction)
        {
            GUILayout.Label(m_BlockingNotice,
            EditorHelper.CreateLabelStyle(new object[] { TextAnchor.MiddleCenter, FontStyle.Bold, 15 }));
            return;
        }
        #region Main Title OutPut GUI Reference
        GUILayout.Label("테이블 검사 및 파싱 설정 구간입니다.", 
        EditorHelper.CreateLabelStyle(new object[] { TextAnchor.MiddleCenter,FontStyle.Bold, 13}));
        GUILayout.Label("This is the table inspection and parsing setting section.", 
        EditorHelper.CreateLabelStyle(new object[] { TextAnchor.MiddleCenter, 10}));

        GUILayout.BeginHorizontal();
        //로컬테이블의 데이터를 전부 파싱을 마련할 수 있도록 한다
        EditorHelper.CreateEditorBtn("(로컬)엑셀 => TextAsset", null, () =>
        {
            //테스트 예시
            //EditorHelper.ShowProgressBarAsync(null, "Parsing All LocalTable", "LocalExcel to ScriptableObject", 1000)
            StartingByAsyncThreadTask("로컬 테이블 TextAsset으로 파싱중...",
            EditorHelper.ShowProgressBarAsync(EditorHelper.XLSXParsingTables(),
            "Parsing All LocalTable", "LocalExcel to ScriptableObject", 1000));
        });
        EditorHelper.CreateEditorBtn("(서버 + 로컬)엑셀 중복 및 누락 검사", null, () =>
        { Debug.Log("중복검사 시작!!"); });
        EditorHelper.CreateEditorBtn(new GUIContent(GetFindDicApplyGUITex("RefreshTex"))/*"®"*/, null, () =>
        { Debug.Log("(서버 + 로컬)테이블을 EidtorScriptableObject으로 재 파싱!"); }, 32);
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();
        //Test 관련 (이후 EditorWindow를 추가로 만들어서 단순 로직 테스트를 에디터모드로 테스트할 수 있도록 수정요망*************************************************************************************
        EditorHelper.CreateEditorBtn("PlayerSystemPrefab 폴더내의 갯수"/*"®"*/, null, () =>
        {
            string GetPath = $"{Application.dataPath}/{nameof(Resources)}/Objects/DefultObject/Systems/CharacterSystems";
            EditorHelper.GetFilesByFolder(EditorHelper.FileInfoOutPutType.PullPath, true, GetPath).
            HForEach(x => Debug.LogFormat("현재 파일 이름 : {0}", x));
        }, 32);
        GUILayout.EndHorizontal();
        #endregion

        #region Sub Parts OutPut GUI Reference
        //여기부턴 DropDown할 수 있도록 수정
        m_EditorInstanceObjSupport.Update();
        m_GetAddScrollPartArray[0] = GUILayout.BeginScrollView(m_GetAddScrollPartArray[0]);
        int CountIDX = 0;
        m_EditorInstancePPSupports.ToList().ForEach(x =>
        {
            string GetName = x.name;
            m_isShowHoldArray[CountIDX] = EditorGUILayout.Foldout(m_isShowHoldArray[CountIDX], GetName,
            EditorHelper.CreateLabelStyle(new object[] { EditorStyles.foldout , 15,
            FontStyle.Bold, TextAnchor.MiddleLeft }));//, new System.Tuple<float,float>(500, 100) }));
            #region 이후에 구역으로 나눠서 스크롤을 띄울 수 있도록 수정 요망
            //m_isScrollsActive[CountIDX] = m_ItemInfoAllResources[CountIDX].Item2.Count >= 5;
            //if (m_isScrollsActive[CountIDX]) m_GetAddScrollPartArray[CountIDX + 1] =
            //GUILayout.BeginScrollView(m_GetAddScrollPartArray[CountIDX + 1], GUILayout.Height(10));
            #endregion
            if (m_isShowHoldArray[CountIDX])
            {
                GUILayout.BeginHorizontal();
                EditorHelper.CreateEditorBtn($"(서버 + 로컬)테이블{GetName}엑셀 중복 및 누락 검사", null, () =>
                { Debug.Log($"{GetName} 중복검사 시작!!"); });
                GUILayout.EndHorizontal();
                EditorGUILayout.PropertyField(x, true);
            }
            #region 마찬가지로 구역에 따라 스크롤뷰가 동작할 수 있도록
            //if (m_isScrollsActive[CountIDX]) GUILayout.EndScrollView();
            #endregion
            CountIDX++;
        });
        GUILayout.EndScrollView();
        m_EditorInstanceObjSupport.ApplyModifiedProperties();
        #endregion
    }

    private async void StartingByAsyncThreadTask(string _NoticeByBlocking , System.Threading.Tasks.Task _GetTask)
    { m_BlockingNotice = _NoticeByBlocking; m_isProcessAction = false; await _GetTask; m_isProcessAction = true; }

    private Texture2D GetFindDicApplyGUITex(string _FindKey)
    {
        if ((!m_EditorWindowApplyTexInfos.ContainsKey(_FindKey)).HDebug($"{_FindKey} 라는 이름의 EditorTex 경로인 " +
        $"Assets/AssetReference/SampleTex/RefeshICON.png 에 존재하지 않습니다.!", Helper.HDType.Error)) return null;
        return m_EditorWindowApplyTexInfos[_FindKey];
    }
    //=> m_EditorWindowApplyTexInfos.ToList().Find(x => x.Key == _FindKey).Value;

    #region Eidtor의 전체 업데이트로 전역 입력을 배제하는 방법 (먹히질 않는다 다른 방식으로 전체 차단 활성화 설계 요망..)
    //전체 입력을 막으려면 각 존재하는 모든 창의 OnGUI에 블라킹할 GUI.Box를 배치해야지 해결될꺼 같다
    //private static void ShowBox() => EditorApplication.update += GUIUpdate;
    //private static void HideBox() => EditorApplication.update -= GUIUpdate;
    //private static void GUIUpdate()
    //{
    //    //GUI.enabled = false; //해당 방식은 다른게 클릭이 진행됨

    //    // 오버레이 창 표시
    //    Handles.BeginGUI();
    //    Rect rect = new Rect(0, 0, Screen.width, Screen.height);
    //    GUI.Box(rect, "Please wait, task is running...", EditorHelper.CreateLabelStyle(new object[] 
    //    { 20, TextAnchor.MiddleCenter }));
    //    Handles.EndGUI();

    //    Event Current = Event.current;
    //    if (Current != null) Current.Use();
    //}
    #endregion
}

#endregion

#region Build Support Reference (빌드시 반드시 포함 될 사항들)

public class PreBuildProcessor : IPreprocessBuildWithReport
{
    // IPreprocessBuildWithReport 인터페이스의 실행 우선순위
    public int callbackOrder => 0;

    // 빌드 전 로직 (빌드 시작 전에 호출)
    public void OnPreprocessBuild(BuildReport report)
    {
        #region 빌드 떄 함께 처리해서 보낼것에 대한 설명 (반드시 아래 사항을 필독해서 적용할 수 있도록 한다
        //아래에 두 조건을 적절한곳에 사용하여 빌드 직전 변경해야될 사항을 적절히 사용하여 해당 콜백 함수에 대입하여 
        //사용할 수 있도록 한다
        //1.바로 적용후 빌드하는 방법이 존재할 수 있고 (CS 파일을 직접 건들기 때문에 위험성이 높음)
        //2.빌드 전에 Json으로 File.Create() 진행하고 빌드후에 런타임에서 해당 제이슨을 받아서 실행하거나
        //(내지는 여타 다른 확장자 파일을 빌드때 추가해야할때 해당 방식으로 진행) 방법이 존재한다 (실용성이 좀 떨어짐)
        #region 참고 예시
        // 파일 갯수를 확인할 폴더 경로 (예: Assets/Resources/MyFolder)
        //string folderPath = Path.Combine(Application.dataPath, "Resources/MyFolder");

        //if (Directory.Exists(folderPath))
        //{
        //    // 해당 폴더 내 파일 개수를 확인
        //    string[] files = Directory.GetFiles(folderPath);
        //    int fileCount = files.Length;

        //    // 파일 개수를 처리하거나, 스크립트에 반영하는 로직
        //    Debug.Log($"Number of files in MyFolder: {fileCount}");

        //    // 예: 특정 스크립트에 파일 개수를 반영
        //    string scriptPath = Path.Combine(Application.dataPath, "Scripts/FileCount.cs");
        //    UpdateScriptWithFileCount(scriptPath, fileCount);
        //}
        //else Debug.LogError("Folder does not exist.");
        #endregion
        #endregion

        //EditorHelper.UpdateScriptWithFileCount<int>("private readonly string[] m_ResourcesFilesPath",
        //ReqriedEditorApplyPath.m_PlayerSystemPrefabScirptReferencePath[ReqriedEditorApplyPath.PathType.ScriptFile], 
        //EditorHelper.GetFilesCountByFolder(ReqriedEditorApplyPath.m_PlayerSystemPrefabScirptReferencePath[ReqriedEditorApplyPath.PathType.ReferenceFile]));

        
    }
}

#region Editor MenuItem 형식으로 빌드 후에 적용하는 것에 대한 예시 (현재는 사용안함)
//public class CustomBuildScript
//{
//    [MenuItem("Build/Custom Build")]
//    public static void BuildGame()
//    {
//        // 빌드 전에 파일 개수를 확인하는 로직
//        string folderPath = Path.Combine(Application.dataPath, "Resources/MyFolder");
//        if (Directory.Exists(folderPath))
//        {
//            string[] files = Directory.GetFiles(folderPath);
//            Debug.Log($"Number of files in MyFolder: {files.Length}");
//        }

//        // 빌드 시작
//        BuildPipeline.BuildPlayer(EditorBuildSettings.scenes, "Builds/MyGame.exe", BuildTarget.StandaloneWindows64, BuildOptions.None);

//        Debug.Log("Custom build completed.");
//    }
//}

#endregion

#endregion


#region 참고할것이 전부 끝난다면 삭제 요망
public class Test_EditorWindowByXCELToTextAsset : EditorWindow
{
    [SerializeField] private List<MapInfo> GetMapInfo = new List<MapInfo>();
    Vector2 ScrollPos;
    private SerializedObject GetEditorSObj;
    private SerializedProperty GetEditorSPP;
    private bool showList = false;
    private void OnEnable()
    {
        //GetMapInfo.Add(new MapInfo() { s_ItemType = ItemType.MapType });
        GetEditorSObj = new SerializedObject(this);
        GetEditorSPP = GetEditorSObj.FindProperty("GetMapInfo");
    }
    

    void OnGUI()
    {
        //GetEditorSObj.Update();
        ScrollPos = EditorGUILayout.BeginScrollView(ScrollPos);
        GUILayout.Label("Convert xcel To TextAsset", EditorStyles.boldLabel); //Text 생성
        //EditorUtility.DisplayCancelableProgressBar()
        showList = EditorGUILayout.Foldout(showList, "Show MapInfo");
        if (showList)
        {
            //ScrollPos = EditorGUILayout.BeginScrollView(ScrollPos);
            if (GUILayout.Button("Execute Sample Functions"))
                Debug.Log("안녕?");
            if (GUILayout.Button("LocalTableToTextAssets"))
            {
                //해당 함수를 넣어주면 됨
                Debug.Log("엑셀 테이블 Text Asset 변환 완료!!");
            }
            
            EditorGUILayout.PropertyField(GetEditorSPP, true);
            //EditorGUILayout.EndScrollView();
        }

        //GetMapInfo.ToList().ForEach(x =>
        //{
        //    x.s_SpriteArray.ToList().ForEach(y =>
        //    {
        //        GUILayout.BeginHorizontal();
                
        //        Rect SubRect = GUILayoutUtility.GetRect(100, 100);
        //        EditorGUI.DrawPreviewTexture(SubRect, y.texture);
        //        EditorGUILayout.LabelField(y.name);
        //        GUILayout.EndHorizontal();
        //    });
        //});

        EditorGUILayout.EndScrollView();
        //GetEditorSObj.ApplyModifiedProperties();
    }
}

#endregion
