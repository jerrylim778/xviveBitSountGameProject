using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Threading.Tasks;
using Commons.Helpers;

namespace Commons.Editor.EditorCommons.Helpers
{
    public static class EditorHelper
    {
        #region Editor Support By (Main Thread [메인 리소스를 사용한다는 의미]) Reference 
        //(주로 유니티에서 메인으로 사용하는 메인스레드 Helper에 해당됨)
        public static Texture2D GetSpriteToTexShowEditor(Sprite _GetSpr)
        {
            if (_GetSpr == null) return null;
            if (_GetSpr.rect.width != _GetSpr.texture.width)
            {
                Texture2D NewTex = new Texture2D((int)_GetSpr.rect.width, (int)_GetSpr.rect.height);
                Color[] ApplyNewColors = _GetSpr.texture.GetPixels((int)_GetSpr.textureRect.x, (int)_GetSpr.textureRect.y,
                (int)_GetSpr.textureRect.width, (int)_GetSpr.textureRect.height);
                NewTex.SetPixels(ApplyNewColors);
                NewTex.Apply();
                return NewTex;
            }

            return _GetSpr.texture;
        }

        #endregion

        #region Editor All(EditorGUILayout, GUILayoutUntility, GUILayout) GUI Helper Sub System Private Functions

        public static GUIContent CreateGUIContent<T>(object _GetAny = null, string _SubStr = "") where T : class
        {
            T GetAsset = _GetAny as T;
            switch (GetAsset)
            {
                case System.Tuple<string, string> GetValue_1:
                    return new GUIContent(GetValue_1.Item1, GetValue_1.Item2);
                case Texture2D GetValue_2:
                    return new GUIContent(GetValue_2 != null ? GetValue_2 : AssetDatabase.LoadAssetAtPath<Texture2D>(_SubStr));
            }

            return null;
        }

        #region GUILayout

        #region Text => Label Reference
        public static GUIStyle CreateLabelStyle(params object[] _SetAnyArray)
        {
            int FIDX = -1; FIDX = _SetAnyArray.ToList().FindIndex(x => x is GUIStyle);
            GUIStyle NewInstanceStyle = FIDX == - 1? new GUIStyle(GUI.skin.label) : _SetAnyArray[FIDX] as GUIStyle;
            _SetAnyArray.ToList().ForEach(x => SetLabelStyle(NewInstanceStyle, x));
            return NewInstanceStyle;
        }

        public static void SetLabelStyle(this GUIStyle _SetGUIs, object _SetAny)
        {
            switch (_SetAny)
            {
                case int GetValue:
                    _SetGUIs.fontSize = GetValue;
                    break;
                case TextAnchor GetValue_2:
                    _SetGUIs.alignment = GetValue_2;
                    break;
                case FontStyle GetValue_3:
                    _SetGUIs.fontStyle = GetValue_3;
                    break;
                case System.Tuple<float, float> GetValue_4:
                    _SetGUIs.fixedWidth = GetValue_4.Item1;
                    _SetGUIs.fixedHeight = GetValue_4.Item2;
                    break;
            }
        }
        #endregion

        #region Button Reference

        #region 버튼 사이즈와 관련한 추가적인 정보
        //만일 크기에 따라 GUI사이즈를 변경하고 싶다면 아래와 같이 진행하는걸 고려해라
        //(물론 본 OnGUI()함수에서 정의해야지 의미가 발생한다
        //float windowWidth = position.width;
        //float windowHeight = position.height;

        //// 윈도우 크기에 따라 레이블과 버튼의 크기를 계산합니다.
        //buttonWidth = windowWidth* 0.8f;
        //buttonHeight = windowHeight* 0.2f;
        //labelHeight = windowHeight* 0.1f;
        #endregion
        public static void CreateEditorBtn(string _BtnTxt, GUIStyle _GetStyle = null,
        System.Action _BtnClickEvent = null, float _Width = 338, float _Height = 32)
        {
            bool isComplate = GUILayout.Button(_BtnTxt, _GetStyle ?? GUI.skin.button,
            GUILayout.Width(_Width), GUILayout.Height(_Height),
            GUILayout.ExpandWidth(true)/*, GUILayout.ExpandHeight(true)*/);
            //|| string.IsNullOrEmpty(_BtnTxt);
            if (isComplate) _BtnClickEvent?.Invoke();
        }

        public static void CreateEditorBtn(GUIContent _GetContent, GUIStyle _GetStyle = null,
        System.Action _BtnClickEvent = null, float _Width = 338, float _Height = 32)
        {
            bool isComplate = GUILayout.Button(_GetContent, _GetStyle ?? GUI.skin.button,
            GUILayout.Width(_Width), GUILayout.Height(_Height),
            GUILayout.ExpandWidth(true)/*, GUILayout.ExpandHeight(true)*/);
            //|| _GetContent != null;
            if (isComplate) _BtnClickEvent?.Invoke();
        }
        #endregion

        #endregion

        #endregion

        #region Editor Async Process Reference

        #region 단순 Thread.Sleep()을 사용한 ProcessBar (현재는 사용안함 참고용 후에 삭제 요망...)
        //단순 Thread.Sleep()을 사용하게 될 경우 유니티 메인 스래드 블러킹을 발생하여 Editor가 정상작동하지 않게 될 수 있다
        //따라서 비동기 함수나 코루틴을 사용해야한다
        public static void EditorShowOutProcessBar(
        int _WaitThridTime, string _MainOuPutProcessStr, string _ProcessingStr, out bool _isComplate)
        {
            // 진행 바 초기화
            _isComplate = false;
            float progress = 0f;
            string progressInfo = $"Starting Process {_ProcessingStr}...";

            // 진행 바 표시
            EditorUtility.DisplayProgressBar($"Processing {_MainOuPutProcessStr}", progressInfo, progress);

            // 예시로 10단계의 처리 과정을 시뮬레이션 (처리되는 과정은 다른곳에서 진행하다가 돌아올 수 있도록 한다)
            for (int i = 0; i <= 10; i++)
            {
                // 각 단계의 처리 시간을 시뮬레이션하기 위해 임의의 대기 시간을 설정
                // 해당 구간에 처리할 것을 지정하면 되겠다
                // 블러킹 때문에 메인스래드 사용에 문제가 존재할 수 있다는것을 명심하고 사용하자
                System.Threading.Thread.Sleep(_WaitThridTime * 100); // 0.5초 대기 (실제로는 시간을 적절히 조정해야 함)

                // 진행률 업데이트
                progress = (float)i / 10f;
                progressInfo = $"Step {i}/10";

                // 진행 바 업데이트
                EditorUtility.DisplayProgressBar("Processing", progressInfo, progress);

                // 취소 버튼을 눌렀는지 체크
                if (EditorUtility.DisplayCancelableProgressBar("Processing", progressInfo, progress))
                {
                    // 취소 버튼을 눌렀을 경우 처리 중단
                    EditorUtility.ClearProgressBar();
                    Debug.Log("Process Canceled.");
                    _isComplate = true;
                    return;
                }
            }
            _isComplate = true;
        }
        #endregion

        #region 비동기 함수(Task, awiw, async)를 사용하여 정의한 Editor ProcessBar
        #region 비동기 사용시 주의할점과 그밖에 관련 설명
        //비동기 함수내에서는 메인스래드에서 (즉 유니티의 기본 제공 클래스)에서 나온 모든 정보를 사용할 수 없다
        //따로 관련된 함수를 정의하여 해당 함수내에서 유니티의 함수를 사용할 수 없도록 정의해야된다
        //마찬가지로 코루틴사용과 해당 비동기 사용중 이점이 높은걸 사용할것
        //중요한건 async 키워드 내에서는 어떤 유니티의 메인 스래드를 사용할 수 없다는것을 알아야됨
        #endregion
        public static async Task ShowProgressBarAsync(System.Func<string>[] _GetCallBacks, string title, string info, int delay)
        {
            bool isCancel = false;

            if (_GetCallBacks == null || _GetCallBacks.Length <= 0)
            {
                UpdateProgressBar(title, info, 0);
                await Task.Delay(delay);
                EditorUtility.ClearProgressBar();
                return;
            }
            try
            {
                EditorApplication.update += EditorUpdate; // 메인 스레드에서 업데이트 하기 위해 등록
                int StepCount = _GetCallBacks.Length;
                UpdateProgressBar(title, info, 0);// ProgressBar 표시

                for (int i = 1; i <= StepCount; i++)
                {
                    if (isCancel) return;
                    string SubStepNotice = _GetCallBacks[i - 1]?.Invoke();
                    await Task.Delay(delay);
                    float progress = (float)i / StepCount;
                    UpdateProgressBar(title, $"{SubStepNotice + info} ({i}/{StepCount})", progress); // ProgressBar 업데이트

                    if (EditorUtility.DisplayCancelableProgressBar("Processing", info, progress))
                    {
                        isCancel = true;
                        EditorApplication.update -= EditorUpdate;
                        EditorUtility.ClearProgressBar();
                        Debug.Log("Process Canceled.");
                    }
                }
            }
            catch (System.Exception ex)
            {
                if (!isCancel) // ProgressBar 닫기
                {
                    EditorApplication.update -= EditorUpdate;
                    EditorUtility.ClearProgressBar();
                    Debug.LogError(ex.Message);
                }
            }
            finally
            {
                if (!isCancel) // ProgressBar 닫기
                {
                    EditorApplication.update -= EditorUpdate;
                    EditorUtility.ClearProgressBar();
                }
            }
        }

        #region Async ProcessBar SubHelpers Private Functions

        //private static bool CheckCancelByProcessBar(string _SubInfo, float _CurrProgress)
        //{
        //    if (EditorUtility.DisplayCancelableProgressBar("Processing", _SubInfo, _CurrProgress))
        //    {
        //        EditorApplication.update -= EditorUpdate;
        //        EditorUtility.ClearProgressBar();
        //        Debug.Log("Process Canceled.");
        //        return true;
        //    }
        //    return false;
        //}

        private static void UpdateProgressBar(string title, string info, float progress)
        => EditorApplication.delayCall += () => EditorUtility.DisplayProgressBar(title, info, progress);
        private static void EditorUpdate()
        {
        } // 여기에 필요한 다른 업데이트 코드가 있을 경우 추가 가능
        private static async Task WaitUntilAsync(System.Func<bool> _UntilBoolean)
        {
            if (!_UntilBoolean()) await Task.Delay(100);
        }
        #endregion
        #endregion

        #endregion

        #region Editor System IO Directory System Reference

        public static bool IsFolderEmpty(string _FilePath)
        {
            System.IO.FileInfo GetFile = new System.IO.FileInfo(_FilePath);
            return GetFile.Length <= 3;
        }

        public enum FileInfoOutPutType { PullPath, PullPathRemoveExtension, FileNameAddExtension, FileNameRemoveExtension, OnlyOutPutExtension }

        //public static System.IO.File[] GetFileCountFolder(string _FolderPath) //이후 각 확장자에 맞는 파싱을 가져와 File[] 로 반환할 수 있도록 한다
        public static string[] GetFilesByFolder(FileInfoOutPutType _GetType, bool _isRemoveMataFile, string _FolderPath) 
        {
            List<string> ReturnFilesName = new List<string>();
            List<string> GetPulPathFiles = null;
            switch (_GetType)
            {
                case FileInfoOutPutType.PullPath or FileInfoOutPutType.PullPathRemoveExtension or FileInfoOutPutType.OnlyOutPutExtension:
                    GetPulPathFiles = System.IO.Directory.GetFiles(_FolderPath).ToList();
                    if (_isRemoveMataFile) GetPulPathFiles = GetPulPathFiles.FindAll(x => Path.GetExtension(x).TrimStart('.') != "meta");
                    if(_GetType == FileInfoOutPutType.PullPath) ReturnFilesName = GetPulPathFiles;
                    if (_GetType == FileInfoOutPutType.PullPathRemoveExtension)
                        GetPulPathFiles.HForEach(x => ReturnFilesName.Add(x.HTrimEnd('.'))); 
                    if (_GetType == FileInfoOutPutType.OnlyOutPutExtension)
                        GetPulPathFiles.HForEach(x => ReturnFilesName.Add(Path.GetExtension(x).TrimStart('.')));
                    break;
                case FileInfoOutPutType.FileNameAddExtension or FileInfoOutPutType.FileNameRemoveExtension:
                    System.IO.DirectoryInfo GetInfo = new DirectoryInfo(_FolderPath);
                    GetPulPathFiles = new List<string>();
                    GetInfo.GetFiles().HForEach(x => GetPulPathFiles.Add(x.Name));
                    if (_isRemoveMataFile) GetPulPathFiles = GetPulPathFiles.FindAll(x => Path.GetExtension(x).TrimStart('.') != "meta");
                    if (_GetType == FileInfoOutPutType.FileNameRemoveExtension) GetPulPathFiles.HForEach(x => ReturnFilesName.Add(x.HTrimEnd('.')));
                    ReturnFilesName = GetPulPathFiles;
                    break;
            }
            return ReturnFilesName.ToArray();
            #region 확장자를 추출하는 여러 방법 (정규화 된 Path<클래스> 사용을 권장 (나머지는 참고용)
            //// Path 클래스를 사용하여 확장자 추출
            //string extension = Path.GetExtension(filePath);
            //// 맨 끝 확장자를 추출하는 방법
            //int dotIndex = filePath.LastIndexOf('.');
            //string extension = dotIndex >= 0 ? filePath.Substring(dotIndex + 1) : string.Empty;
            //// Split 메소드를 사용하여 확장자 추출
            //string[] parts = filePath.Split('.');
            //string extension = parts.Length > 1 ? parts[^1] : string.Empty;  // C# 8.0 이상에서 사용 가능
            //// LINQ의 Last 메소드를 사용하여 확장자 추출
            //string extension = filePath.Split('.').Last();
            //// 정규 표현식을 사용하여 확장자 추출
            //string extension = Regex.Match(filePath, @"\.(\w+)$").Groups[1].Value;
            //return ReturnFilesName.ToArray();
            #endregion
        }

        public static int GetFilesCountByFolder(string _FolderPath)
        {
            if ((!System.IO.Directory.Exists(_FolderPath)).HDebug($"{_FolderPath}이라는 폴더는 해당 경로에 없습니다.!", Helper.HDType.Error)) return -1;
            System.IO.DirectoryInfo GetInfo = new DirectoryInfo(_FolderPath);
            return GetInfo.GetFiles().Length;
        }

        public static void AllDirectionRemoved(string _FilePath)
        {
            if (!System.IO.Directory.Exists(_FilePath))
                return;
            System.IO.DirectoryInfo DirInfo = new System.IO.DirectoryInfo(_FilePath);
            DirInfo.EnumerateFiles().ToList().ForEach(x =>
            {
                try { x.Delete(); }
                catch (System.IO.IOException ex) when ((ex.HResult & 0x0000FFFF) == 32) //파일공유 규칙 위반시 값은 32로 고정이다
                {
                    Debug.LogWarning(ex.Message);
                }
            });
            DirInfo.EnumerateDirectories().ToList().ForEach(x => x.Delete());
            if (DirInfo.GetFiles().Length <= 0) System.IO.Directory.Delete(_FilePath, true);
        }

        #endregion

        #region Editor System IO File Reference

        public static string CheckPathIsNotCreate(string _GetParsingPath, bool _isLastIsObj = true)
        {
            bool isAllParsingFile = false;
            List<string> ComapreStr = new List<string>();
            if (_GetParsingPath.Contains("Assets"))
            {
                isAllParsingFile = true;
                ComapreStr.AddRange(_GetParsingPath.Split(new string[] { "Assets" }, System.StringSplitOptions.None));
            }
            else ComapreStr.Add(_GetParsingPath);
            string[] SubStr = ComapreStr[isAllParsingFile? 1 : 0].Split('/');
            int CountIDX = 0; string ApplyStr = string.Empty; //A/B => A/B/C => A/B/C/D
            foreach(string x in SubStr)
            {
                if (_isLastIsObj && CountIDX >= SubStr.Length - 1) return _GetParsingPath;
                ApplyStr += x;
                if (CountIDX != 0 && !System.IO.Directory.Exists(Application.dataPath + "/" + ApplyStr))
                Directory.CreateDirectory(Application.dataPath + ApplyStr);
                ApplyStr += "/";
                CountIDX++;
            }

            return _GetParsingPath;
        }

        public static void RefreshAndFocusPoint(string _RefreshPath)
        {
            AssetDatabase.ImportAsset(_RefreshPath); //해당 방식은 참조가 되지 않는다
            AssetDatabase.Refresh();
            EditorUtility.FocusProjectWindow();
            Object asset = AssetDatabase.LoadAssetAtPath<Object>(_RefreshPath);
            Selection.activeObject = asset;
        }

        #endregion

        #region Excel Reference
        //엑셀 테이블 한번에 모두 파싱 관련
        public static System.Func<string>[] XLSXParsingTables()
        {
            string XLSXPath = CheckPathIsNotCreate(
            string.Format("{0}/Editor/EditorMyCustomReference/ProjectO_LocalTableInfo.xlsx", Application.dataPath));
            if (!File.Exists(XLSXPath))
            {
                Debug.LogWarningFormat("해당 파일을 찾을 수 없어서 새로 파일을 생성합니다..!!{0}", XLSXPath);
                string GetNewPath = XLSXPath.Remove(XLSXPath.LastIndexOf('x')); 
                File.Create(GetNewPath); RefreshAndFocusPoint(GetNewPath);
                return null;
            }

            Excel GetExcel = ExcelHelper.LoadExcel(XLSXPath);
            Dictionary<string, string> DICGetExcel = GetExcel.GetSheetDic();
            List<System.Func<string>> ReturnValues = new List<System.Func<string>>();
            var GetEnumer = DICGetExcel.GetEnumerator();
            while (GetEnumer.MoveNext())
            {
                System.Tuple<string, string> GetSubStrs =
                new System.Tuple<string, string>(GetEnumer.Current.Key, GetEnumer.Current.Value);
                ReturnValues.Add(() =>
                {
                    SaveReturnTextAsset(GetSubStrs.Item1, GetSubStrs.Item2);
                    return GetEnumer.Current.Key;
                });
            }
            return ReturnValues.ToArray();
        }

        //파일 생성 혹은 수정 
        private static void SaveReturnTextAsset(string _SaveFileName, string _SaveValue)
        {
            string PathStr = CheckPathIsNotCreate(
            string.Format("{0}/Resources/Datas/TextAsset/{1}.txt", Application.dataPath, _SaveFileName));
            if (!System.IO.File.Exists(PathStr))
            {
                Debug.LogWarningFormat("파일이 없어서 새로 파일을 생성합니다..!! {0}", PathStr);
                using (System.IO.FileStream fs = new System.IO.FileStream(PathStr, FileMode.Create)) fs.Close();
                RefreshAndFocusPoint(PathStr);
                SaveReturnTextAsset(_SaveFileName, _SaveValue);
                return;
            }
            using (System.IO.StreamWriter sr = new System.IO.StreamWriter(PathStr, false, System.Text.Encoding.UTF8))
                sr.WriteLine(_SaveValue);
        }
        #endregion

        #region Editor MenuItem Hierachy Create Object Reference

        #region Create GUI Reference
        public static T CreateObjectSyncTr<T>(this GameObject _GetApplyObj, MenuCommand _GetMC) where T : MonoBehaviour
        {
            Transform GetFinalParents = _GetMC.context == null ? null : (_GetMC.context as GameObject).transform;
            T ReturnValue = _GetApplyObj.AddComponent<T>();
            if (GetFinalParents == null) return ReturnValue;
            if(_GetApplyObj.GetComponent<RectTransform>() != null && GetFinalParents.GetComponent<RectTransform>() != null)
            {
                (_GetApplyObj.transform as RectTransform).anchoredPosition = Vector2.zero;
                //(GetFinalParents as RectTransform).anchoredPosition;
                return ReturnValue;
            }
            _GetApplyObj.NewObjAndMakeSyncPosRot(GetFinalParents, Helper.SyncType.NotIntsnce);
            return ReturnValue;
        }

        public static Transform FindParentByClickPoint(MenuCommand _GetMC)//, MonoBehaviour _GetGUI)
        {
            //if(_GetGUI is UnityEngine.EventSystems.UIBehaviour)
            //{
            //    Debug.LogErrorFormat("해당 함수는 오직 GUI를 생성할때만 사용 가능합니다..!! \n" +
            //    "생성하고자 한 오브젝트 {0}", _GetGUI.name);
            //    return null;
            //}
            Canvas GetCanvas = Object.FindObjectOfType<Canvas>();
            Transform GetFinalParents = _GetMC.context == null ? null : (_GetMC.context as GameObject).transform;
            if (GetCanvas == null)
            {
                GameObject GetNewCanvas = new GameObject("Canvas");
                GetCanvas = GetNewCanvas.AddComponent<Canvas>();
                GetNewCanvas.AddComponent<UnityEngine.UI.CanvasScaler>();
                GetNewCanvas.AddComponent<UnityEngine.UI.GraphicRaycaster>();
                GetCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                GetFinalParents = GetCanvas.transform;
            }
            if (GetFinalParents.ParentNonLinearStuctureSearch<Canvas>() == null)
                GetFinalParents = GetCanvas.transform;
            return GetFinalParents;
        }
        #endregion
        #endregion

        #region Editor Build Support Reference

        //****//****//****//****//****//****//****//****//****//**** (**문자로 cs 파일의 변수를 바꿀때 주의할점**) //****//****//****//****//****//****//****//****//****//****//****//****//****//****
        //****//****//****//****//****//****//****//****//****//****//****//****//****//****//****//****//****//****//****//****//****//****//****//****
        //****//****//****//****//****//****//****//****//****//****//****//****//****//****//****//****//****//****//****//****//****//****//****//****
        //<** 제이슨에 할당하는게 아닌 바로 파일내에 기입하여 보낼 수 있는 방식 스크립트 파일에 파일 개수를 반영하는 함수
        // 오직 변수만 기입할 수 있다  (또한 변경하고자 하는 라인에서 적용할 변수값 이후로 있으면 안되며 주석 또한 같은게(없을 가능성이 높음 없어야 한다)**>
        //****//****//****//****//****//****//****//****//****//****//****//****//****//****//****//****//****//****//****//****//****//****//****//****
        //****//****//****//****//****//****//****//****//****//****//****//****//****//****//****//****//****//****//****//****//****//****//****//****
        //****//****//****//****//****//****//****//****//****//**** (**문자로 cs 파일의 변수를 바꿀때 주의할점**) //****//****//****//****//****//****//****//****//****//****//****//****//****//****
        public static void UpdateScriptWithFileCount<T>(string _VariableLine, string _ScriptPath, T _ApplyValue)
        {
            //확장자를 이용하여 CS 폴더가 아닌것은 제할 수 있도록 수정요망... ************************************************
            //_VariableLine 해당 문자열 또한 검사하여 변수의 최소 요건이 맞는지 검사하고 아니라면 Error 출력 할 수 있도록 수정요망... ************************************************
            if (!File.Exists(_ScriptPath))
            {
                Debug.LogError("Script file not found.");
                return;
            }
            // 파일을 열고 파일 개수를 반영하는 로직을 작성
            string[] GetLines = File.ReadAllLines(_ScriptPath);
            bool isApplyCheck = false;
            for (int i = 0; i < GetLines.Length; i++)
            {
                if (GetLines[i].Contains(_VariableLine)) // 예시 => "public static int fileCount" 
                {
                    GetLines[i] = $"{_VariableLine} = {_ApplyValue}"; // 예시 => $"public static int fileCount = {_ApplyValue};"                        
                    isApplyCheck = true;
                    break;
                }
            }
            if ((!isApplyCheck).HDebug($"{_ScriptPath} CS 파일에 {_VariableLine} 변수 라인이 존재 하지 않습니다.!", Helper.HDType.Error)) return;

            File.WriteAllLines(_ScriptPath, GetLines);
            Debug.Log($"Script updated with file count. {_ScriptPath} CS 파일에 {_VariableLine} 변수 라인에 {_ApplyValue} 로 변수가 변경되어 빌드합니다.!");
        }
        #endregion
    }
}