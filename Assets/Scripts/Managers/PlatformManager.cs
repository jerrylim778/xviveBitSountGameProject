using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEngine;
using Commons.Helpers;

public static class PlatformManager
{
    public enum PlatformType
    {
        None = 0,
        Editor,
        PC_Window,
        Android,
        IOS,
        WebGL
    }

    public enum ParsingPathType
    {
        None = 0,
        LocalJson,
        LocalBundle,
    }

    #region 플렛폼 관리자 설명
    //플렛폼 마다 내지는 에디터마다 다르게 실행되어야 할 모든 동작들을 설명함
    //따라서 각 사용한 모든건 원래의 이름을 비슷하게 지어야 한다
    //또한 해당 관리자에서 나온 함수는 제일 먼저 처리되어야 한다
    //1.파일 시스템 및 경로 관리
    //파일시스템은 빌드전에 경로로써 있었던 Resouces폴더가 각 플렛폼의 앱데이터 폴더로 전환되지 않는다
    //따라서 이미 생성한 스크립터블 오브젝트와 관련된 사항은 에셋번드을 통해 로컬로 캐시파일 형태로 선 저장 후에
    //가져오는 방식을 활용하고 그 이외에 데이터는 없을때는 해당 구간에 저장하는 방식만 채택해서 사용할 수 있도록 한다
    //참고로 스크립터블 오브젝트는 동적으로 생성하는게 불가능하면 오르지 생성된것만 사용할 수 있다
    //2.플렛폼마다 다르게 처리해야될 로직을 해당 관리자에서 진행
    #endregion
    private static bool m_isInitPlatform = false;
    private static bool m_isBlockLocalPath = false;
    private static PlatformType m_PlatformType = PlatformType.None;
    private static string m_PlatformDataPath = string.Empty, m_KeyDirectoryName = string.Empty;
    private static Dictionary<ParsingPathType, System.Tuple<string, string>> m_DicParsingPathByType;

    #region Platform Initialization
    public static PlatformType InitPlatform()
    {
        if (m_isInitPlatform)
        {
            Debug.LogError("이미 플렛폼을 초기화 했습니다 \n PlatformManager is Allready Initlazation.");
            return m_PlatformType;
        }
        #region 플렛폼별 상태
#if UNITY_EDITOR
        m_PlatformType = PlatformType.Editor;
#elif UNITY_STANDALONE_WIN
        m_PlatformType = PlatformType.Window;
#elif UNITY_ANDROID
        m_PlatformType = PlatformType.Android;
#elif UNITY_IOS
        m_PlatformType = PlatformType.IOS;
#elif UNITY_WEBGL
        m_PlatformType = PlatformType.WebGL;
#endif
        #endregion

        switch (m_PlatformType)
        {
            case PlatformType.Editor:
                m_PlatformDataPath = Application.dataPath; //변경할것
                m_KeyDirectoryName = "Assets";
                break;
            case PlatformType.PC_Window:
                m_PlatformDataPath = Application.persistentDataPath;
                m_KeyDirectoryName = Application.productName;
                break;
            case PlatformType.Android:
                m_PlatformDataPath = Application.persistentDataPath;
                m_KeyDirectoryName = "files";//$"com.{Application.productName}.{Application.companyName}"
                break;
            case PlatformType.IOS:
                //m_dataPath = Application.temporaryCachePath; //캐시인지 파일인지 명확히 확인후 기입할것
                m_PlatformDataPath = Application.persistentDataPath; 
                m_KeyDirectoryName = "Documents";
                break;
            case PlatformType.WebGL:
                //Caching.ready 해당 구조를 비동기로 기다려서 캐싱할 번들이 있는것을 기다려야 한다
                Debug.LogWarning($"{nameof(PlatformType.WebGL)}플렛폼은 캐싱으로 로컬지정 패싱 상태 입니다.!");
                m_PlatformDataPath = string.Empty;
                m_KeyDirectoryName = string.Empty;
                m_isBlockLocalPath = true;
                break;
        }
        m_DicParsingPathByType = new Dictionary<ParsingPathType, System.Tuple<string, string>>()
        {
            { ParsingPathType.LocalJson, new System.Tuple<string, string>(".json", m_PlatformType == PlatformType.Editor?
            $"Editor/Lim_{m_PlatformType}Platform_BySaveLoad/LocalJsonPath" : $"Lim_{m_PlatformType}PlatformBySaveLoad/LocalJsonPath") },

            { ParsingPathType.LocalBundle, new System.Tuple<string, string>(string.Empty, m_PlatformType == PlatformType.Editor? //다시 변경할것
            $"Editor/Lim_{m_PlatformType}Platform_BySaveLoad/LocalBundlePath" : $"Lim_{m_PlatformType}PlatformBySaveLoad/LocalBundlePath") },
            //번들은 확장자가 따로 존재하지 않는다
            //{ ParsingPathType.LocalBundle, new System.Tuple<string, string>(string.Empty, $"Lim_{m_PlatformType}PlatformBySaveLoad/LocalBundlePath") }
        };

        m_isInitPlatform = true;
        return m_PlatformType;
    }

    private static bool CheckInitPlatform()
    {
        if ((!m_isInitPlatform).HDebug(
        "아직 플렛폼 초기화를 하지 않았습니다 초기화 이후에 모든 로직을 실행할 수 있습니다..!!", Helper.HDType.Error))
            return true;

        return false;
    }
    #endregion

    #region Main System Platform Function

    public static void Exit()
    {
        if (CheckInitPlatform()) return;

        switch (m_PlatformType)
        {
            case PlatformType.Editor:
#if UNITY_EDITOR //삭제 요망
                UnityEditor.EditorApplication.isPlaying = false;
#endif
                break;
            case PlatformType.PC_Window or PlatformType.Android or 
                 PlatformType.IOS:
                Application.Quit();
                break;
            case PlatformType.WebGL: //새로 고침하는 네이티브 코드를 호출할것
                break;
        }
    }

    #endregion

    #region Main System Functions (Parsing Local Cashing Data Folderes)

    private static string GetPlatformDataPath(ParsingPathType _GetPath)
    {
        //Editor모드에서는 Asset폴더내에 존재할 수 있도록 하고
        //나머지는 모두 각 지정된 곳에서 저장 로드할 수 있도록 하자
        if (CheckInitPlatform() || m_isBlockLocalPath) return string.Empty;

        if (!m_PlatformDataPath.Contains(m_KeyDirectoryName) || _GetPath == ParsingPathType.None)
        {
            Debug.LogErrorFormat("해당되는 경로에 플렛폼별 있어야할 폴더가 존재하지 않습니다 \n" +
            "플렛폼 혹은 파싱 타입과 해당 경로가 맞는지 다시 확인해보세요. \n" +
            "The folder that should be located for each platform does not exist in the corresponding path. \n" +
            "Check again to see if the platform type and the path are correct. {0}", m_KeyDirectoryName);
            return string.Empty;
        }

        string ApplyStr = string.Empty, GetParsingPath = m_DicParsingPathByType.ToList().Find(x => x.Key == _GetPath).Value.Item2;
        string[] SubStr = GetParsingPath.Split('/');
        foreach (string x in SubStr)//A/B => A/B/C => A/B/C/D
        {
            ApplyStr += x;
            if (!System.IO.Directory.Exists(m_PlatformDataPath + "/" + ApplyStr))
                System.IO.Directory.CreateDirectory(m_PlatformDataPath + "/" + ApplyStr);
            ApplyStr += "/";
        }

        return string.Format("{0}/{1}/", m_PlatformDataPath, GetParsingPath);
    }

    public static bool GetPlatformLoadData(ParsingPathType _GetPath, string _FileName, bool _isOnlyExitOut, out object _OutReference)
    {
        _OutReference = null;
        if (CheckInitPlatform() || m_isBlockLocalPath) return false;
        string GetPlatformPath = string.Format("{0}/{1}{2}", GetPlatformDataPath(_GetPath), _FileName,
        m_DicParsingPathByType.ToList().Find(x => x.Key == _GetPath).Value.Item1);
        bool isExists = File.Exists(GetPlatformPath);
        if ((isExists && _isOnlyExitOut) || !_isOnlyExitOut)
        {
            _OutReference = _GetPath switch
            {
                ParsingPathType.LocalJson => File.ReadAllText(GetPlatformPath),
                ParsingPathType.LocalBundle => GetPlatformPath,
                _ => null
            };
        }
        return isExists;
    }

    public static void SetPlatformSaveData(ParsingPathType _GetPath, string _FileName, object _SaveReference, System.Action _EndCallBack = null)
    {
        if (CheckInitPlatform() || m_isBlockLocalPath) return;
        string GetPlatformPath = string.Format("{0}/{1}{2}", GetPlatformDataPath(_GetPath), _FileName,
        m_DicParsingPathByType.ToList().Find(x => x.Key == _GetPath).Value.Item1);

        switch (_SaveReference)
        {
            case string IsStr: 
                //System.IO.File.WriteAllText(GetPlatformPath, IsStr); //제이슨 저장시 제대로 저장이 안되는 오류가 존재한다
                FileMode GetFileMode = File.Exists(GetPlatformPath) ? FileMode.Open : FileMode.Create;
                using (FileStream fs = new System.IO.FileStream(GetPlatformPath, GetFileMode))
                {
                    fs.Close();
                    using (StreamWriter SW = new StreamWriter(
                    GetPlatformPath, false, System.Text.Encoding.UTF8)) { SW.Write(IsStr); SW.Close(); }
                }
                break;
            case byte[] ByteArray:
                System.IO.File.WriteAllBytes(GetPlatformPath, ByteArray);
                break;
            case Stream IOStream:
                Debug.LogError($"{nameof(Stream)}의 형식은 동기로 파싱할 수 없습니다.!");
                break;
        }
    }

    //**********코루틴으로 사용하는게 아닌 코루틴으로 반환하는 함수인걸 명심하고 사용 요망********************
    public static IEnumerator SetPlatformSaveDataAsync(ParsingPathType _GetPath, string _FileName, object _SaveReference, System.Action _EndCallBack = null)
    {
        if (CheckInitPlatform() || m_isBlockLocalPath) return null;
        string GetPlatformPath = string.Format("{0}/{1}{2}", GetPlatformDataPath(_GetPath), _FileName,
        m_DicParsingPathByType.ToList().Find(x => x.Key == _GetPath).Value.Item1);

        switch (_SaveReference)
        {
            case string IsStr:
                return CO_LoadStreamAsync(GetPlatformPath, new StreamWriter(GetPlatformPath, false, System.Text.Encoding.UTF8), IsStr, _EndCallBack);
            case byte[] ByteArray:
                return CO_LoadStreamAsync(GetPlatformPath, new MemoryStream(ByteArray), _FinalEndCallBack: _EndCallBack);
            case System.IO.Stream IOStream:
                return CO_LoadStreamAsync(GetPlatformPath, IOStream, _FinalEndCallBack: _EndCallBack);
        }
        return null;
    }

    #endregion

    #region 이후 Helper에 InGame Version SystemIO 파트를 나눠서 인게임에서 IO를 사용할 수 있도록 수정 요망**

    //이후 반드시 UniTask를 적용하여 StreamWriter 또한 비동기로 반환할 수 있도록 리팩요망 ****
    static IEnumerator CO_LoadStreamAsync<T>(string _GetCallLocalPath, T _GetParsingRefObj, 
    object _SubApply = default, System.Action _FinalEndCallBack = null) where T : System.MarshalByRefObject
    {
        FileMode GetFileMode = File.Exists(_GetCallLocalPath) ? FileMode.Open : FileMode.Create;
        using (System.IO.FileStream CreateLocalFile = System.IO.File.Open(_GetCallLocalPath, GetFileMode))
        {
            switch (_GetParsingRefObj)
            {
                case StreamWriter RefSW : 
                    RefSW.Close();
                    using (StreamWriter ParsingSW = RefSW)
                    {
                        ParsingSW.Write(_SubApply.ToString());
                        ParsingSW.Close();
                    }
                    break;
                case Stream RefDS:
                    using (Stream GetStream = RefDS)
                    {
                        byte[] ReciveBuffer = new byte[1024];
                        int n = 0;
                        while ((n = GetStream.Read(ReciveBuffer, 0, ReciveBuffer.Length)) > 0)
                        {
                            yield return null;
                            CreateLocalFile.Write(ReciveBuffer, 0, n);
                        }
                    }
                    break;
            }
        }
        _FinalEndCallBack?.Invoke();
    }
    #endregion
}