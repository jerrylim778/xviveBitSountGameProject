using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Net;
using UnityEngine;
using UnityEngine.Networking;
using Commons.Helpers;
using Sirenix.OdinInspector;


public class WebNetWorkManager : MonoBehaviour
{
    #region NetManager Infos

    [System.Serializable]
    public struct UseCustomNetRequest : I_Data
    {
        public bool odin_NeedAddReference;
        public RequestURL s_RequestURLType;
        public string s_RequestURL;
        [SerializeField, ShowIf(nameof(odin_NeedAddReference))] AdditionalOperateInfo s_AdditionalNetInfo;
    }

    [System.Serializable]
    public struct AdditionalOperateInfo
    {
        [field: SerializeField] public string s_EnterNetID { get; private set; }
        [field: SerializeField] public string s_EnterNetPassword { get; private set; }

        public AdditionalOperateInfo(string _EnterID, string _EnterPassword)
        {
            s_EnterNetID = _EnterID;
            s_EnterNetPassword = _EnterPassword;
        }
    }
    #endregion

    [Header("NetManager Control")]
    [SerializeField] private bool m_UseingCustomNetInfo = false;
    [SerializeField, ShowIf(nameof(m_UseingCustomNetInfo))] private UseCustomNetRequest[] m_NetRequest;

    [Tooltip("Required Values")]
    private readonly Dictionary<RequestURL, string> m_URLs = new Dictionary<RequestURL, string>() 
    {
        //C:\
        {RequestURL.MainServerHeader, "http://192.168.50.16:8000" },
        {RequestURL.DirectParsingByServerPath, "file:///TestServerPath/" },
        {RequestURL.DirectServerDB, "" },
        {RequestURL.CahsingPath, "" }
    };

    public void Initlization()
    {
        if (m_UseingCustomNetInfo)
        {
            m_URLs.Clear();
            m_NetRequest.HForEach(x => m_URLs.Add(x.s_RequestURLType, x.s_RequestURL));
        }
    }

    //이후 각 타입은 통합하여 비교할 수 있도록 한다
    //또한 서버 연결에 실패했을경우에 대비한 콜백 또한 적용할 수 있도록 한다
    public void CallWebRequest(UsingWebAPIType _UseingWebType, RequestURL _GetURL, RestAPIType _RestType, ReciveReturnType _ReturnType,
    System.Action<object> _ComplateCallBacks = null, params object[] _AddParams)
    {
        if (_UseingWebType == UsingWebAPIType.UWRType)
            StartCoroutine(CO_RequestUnityRest(_GetURL, _RestType, _ReturnType, _ComplateCallBacks, _AddParams));
        if (_UseingWebType == UsingWebAPIType.DotNetType)
            StartCoroutine(CO_DotNetRequest(_GetURL, _RestType, _ReturnType, _ComplateCallBacks, _AddParams));
    }

    #region Main System Private Functions

    IEnumerator CO_RequestUnityRest(RequestURL _GetURL, RestAPIType _RestType, ReciveReturnType _ReturnType, System.Action<object> _ComplateCallBacks = null, params object[] _AddParams)
    {
        using (UnityWebRequest UWR = UWRequestByType(_GetURL, _RestType, FindAddParamsType<WWWForm>(_AddParams)))
        {
            yield return UWR.SendWebRequest();

            if (UWR.isDone)
            {
                if ((UWR.result != UnityWebRequest.Result.Success).
                HDebug($"예기치 못한 서버와의 결과값  {UWR.result} 입니다 \n 서버 결과 {UWR.error}", Helper.HDType.Warning))
                    yield break;

                //서버와 통신에 제약은 없으나 서버에서 로직에 다른 경고를 주는 검사
                if (!string.IsNullOrEmpty(UWR.downloadHandler.text) && 
                ParsingServerMessage(_GetURL, UWR.downloadHandler.text)) 
                    yield break;

                _ComplateCallBacks?.Invoke(_ReturnType switch
                {
                    ReciveReturnType.Text => UWR.downloadHandler.text,
                    ReciveReturnType.ByteArray => UWR.downloadHandler.data,
                    ReciveReturnType.Bundle => DownloadHandlerAssetBundle.GetContent(UWR),
                    _ => UWR.downloadHandler.text
                });
            }
        }
    }

    //[**사용 보류**] 비동기사용을 해야하며 (사용한다면 향후 UniTask를 활용하여 사용할것) + WebGL에서는 닷넷 WebRequest가 되지 않는다
    IEnumerator CO_DotNetRequest(RequestURL _GetURL, RestAPIType _RestType, ReciveReturnType _ReturnType, 
    System.Action<object> _ComplateCallBacks = null, params object[] _AddParams)
    {
        var GetTaskAsync = RequestWebDevSeverAsync(_GetURL, _RestType, _AddParams);

        while (!GetTaskAsync.IsCompleted)
            yield return null;

        if((GetTaskAsync.Exception != null).HDebug(
        $"닷넷 송신 에러 : {GetTaskAsync.Exception.Message}", Helper.HDType.Error))
            yield return null;

        if(_RestType == RestAPIType.Get_UnityBundle)
        {
            //해당 Task 결과값을 Platform으로 보내서 Byte송신을 재게하거나 비동기 Task함수를 변환할것
            yield return null;
        }

        _ComplateCallBacks?.Invoke(GetTaskAsync.Result);
    }

    private async System.Threading.Tasks.Task<string> RequestWebDevSeverAsync(RequestURL _GetURL, RestAPIType _RestType, params object[] _AddParams)
    {
        var GetAddInfo = FindAddParamsType<AdditionalOperateInfo>(_AddParams);

        System.Net.WebRequest WR = System.Net.WebRequest.Create(m_URLs[_GetURL]);
        if (!GetAddInfo.Equals(default(AdditionalOperateInfo)))
        WR.Credentials =  new System.Net.NetworkCredential(GetAddInfo.s_EnterNetID, GetAddInfo.s_EnterNetPassword);
        WR.Method = _RestType switch
        {
            RestAPIType.Get or RestAPIType.Get_UnityBundle => System.Net.WebRequestMethods.Http.Get,
            RestAPIType.Post => System.Net.WebRequestMethods.Http.Post,
            _ => System.Net.WebRequestMethods.Http.Get
        };

        // stream을 플렛폼별 파일 생성기인 PlatformManager<클래스>에 정의한곳에 넘겨서 결과값을 리턴한다
        try
        {
            using (WebResponse WRes = await WR.GetResponseAsync())
            using (System.IO.StreamReader SR = new System.IO.StreamReader(WRes.GetResponseStream()))
            return await SR.ReadToEndAsync();
        }
        catch (System.Exception ex)
        {
            throw new System.NotImplementedException($"{ex.Message}");
        }
        #region 이전 작업 참고용 (이후 삭제 요망)
        //using (System.IO.FileStream CreateLocalFile = System.IO.File.Open(_callLocalPath, System.IO.FileMode.Create))
        //{
        //    using (System.IO.Stream RequestcNet = WR.GetResponse().GetResponseStream())
        //    {
        //        // stream을 전달
        //        byte[] ReciveBuffer = new byte[1024];
        //        int n = 0;
        //        while ((n = RequestcNet.Read(ReciveBuffer, 0, ReciveBuffer.Length)) > 0)
        //        {
        //            yield return null;
        //            CreateLocalFile.Write(ReciveBuffer, 0, n);
        //        }
        //    }
        //}
        #endregion
    }

    #endregion

    #region Sub System Private Functions

    private UnityWebRequest UWRequestByType(RequestURL _GetURL, RestAPIType _RestType, WWWForm _SendForm = null)
    {
        switch (_RestType)
        {
            case RestAPIType.Get:
                return UnityWebRequest.Get(m_URLs[_GetURL]);
            case RestAPIType.Get_UnityBundle:
                return UnityWebRequestAssetBundle.GetAssetBundle(m_URLs[_GetURL]);
            case RestAPIType.Post:
                return UnityWebRequest.Post(m_URLs[_GetURL], _SendForm);
        }
        return null;
    }

    private T FindAddParamsType<T>(params object[] _AddParams)
    {
        //if (_AddParams.ToList().ISFindCondition<T>(x => x is T, out T _OutPutTemp))
        string ReciveStr = typeof(T).Name;
        if (Helper.ISFindCondition(_AddParams.ToList(), x => x.GetType().Name == ReciveStr, out object _OutPutReference))
            return (T)_OutPutReference;

        return default(T);
    }

    #endregion

    #region Sub System Public Functions (Rest Support Reference)

    public WWWForm SendPostCallRest(params System.Tuple<string, string>[] _ApplyAllFields)//string _MainField, string _FindField)
    {
        WWWForm GetWForm = new WWWForm();
        _ApplyAllFields.HForEach(x => 
        GetWForm.AddField(x.Item1, x.Item2));
        return GetWForm;
    }

    #endregion

    #region Sub System Private Functions

    private bool ParsingServerMessage(RequestURL _GetURL, string _ReciveServerMessage, System.Action _EndCallBack = null)
    {
        var GetCompareType = Helper.GetEnumByArray<RecvieNetWorkMessageType>(true);
        int FIDX = -1; FIDX = GetCompareType.ToList().FindIndex(x => _ReciveServerMessage.Contains(x.ToString()));
        if (FIDX == -1) return false;

        string ReplaceStr = _ReciveServerMessage.ReplaceRemoveStrValue(@"\");
        ReciveNetworkMessageInfo? GetJsonObj = (ReciveNetworkMessageInfo)JsonUtility.FromJson(ReplaceStr, typeof(ReciveNetworkMessageInfo));
        bool isHaveMessage = GetJsonObj != null && GetJsonObj.HasValue && !string.IsNullOrEmpty(GetJsonObj.Value.s_ReciveMessage);
        if (isHaveMessage)
        {
            ReciveNetworkMessageInfo GetApplyInfo = GetJsonObj.Value;
            GetApplyInfo.s_RNMType = GetCompareType[FIDX];
            GetJsonObj = GetApplyInfo;
        }
        (isHaveMessage).HDebug($"[**{typeof(WebNetWorkManager)} => {m_URLs[_GetURL]}**] => " +
        $"[**{GetJsonObj.Value.s_RNMType}**] \n ReciveServerNoticeMessage [**{GetJsonObj.Value.s_ReciveMessage}**]",
        GetJsonObj.Value.s_RNMType switch
        {
            RecvieNetWorkMessageType.Warning => Helper.HDType.Warning,
            RecvieNetWorkMessageType.Error => Helper.HDType.Error,
            _ => Helper.HDType.Defult
        });
        return isHaveMessage;
    }

    #endregion
}

//UWR.downloadHandler.text.Contains("Warning").HDebug(UWR.downloadHandler.text, Helper.HDType.Warning))
//{
//    ReciveNetworkMessageInfo
//    yield break;
//}

#region Network Support Module Info(Data) Reference

public enum UsingWebAPIType
{
    UWRType, //유니티 웹 Request API 송신사용
    DotNetType, //닷넷 레스트 API 송신사용 
    //DotNetType_FTP, //닷넷 FTP Type
}

public enum RequestURL
{
    MainServerHeader, //PHP EngineX Apahe Node Born Spring 같은 연동된 웹 서버에 요청
    DirectParsingByServerPath, //서버내의 경로를 바로 탐색하여 직접적인 파일을 가져올때 사용 (**FTP 송신으로 뺄 수 있도록 리팩요망**)
    DirectServerDB, //직접적인 DB 접속에 사용 (서버의 과정을 클라이언트에서 바로 처리하기 위함)
    CahsingPath //따로 중간에 캐싱이된 URL로 접속 (웹 송신의 경우 쿠기 및 indexDB 기술적으로 미리 처리된 프록시 서버 등등)
}

public enum RestAPIType
{
    Get, Post, Put, Delete, Get_UnityBundle
}

public enum ReciveReturnType
{
    Text, ByteArray, Bundle
}

public enum RecvieNetWorkMessageType
{
    None = 0,
    Warning,
    Error
}

public struct ReciveNetworkMessageInfo 
{
    public RecvieNetWorkMessageType s_RNMType; 
    public string s_ReciveMessage;
    public bool s_RemoveAllServerInfo; //True일경우 모든 로컬의 번들 및 서버에서 받아들인 정보를 삭제 후 강제 종료
}

#endregion

#region Common으로 이전하거나 삭제 (현재 사용하지 않음)

public enum ProtocolType
{
    None = 0,
    WebDev,
    FTP,
}

[System.Serializable]
public struct BundleProtocal
{
    public ProtocolType Ptype;
    public int Port;
    public string ProtocolName;
    public string TypeID;
    public string TypePassword;
}

//[System.Serializable]
//public struct ConnectNetwork
//{
//    public ConnectSeverType SeverType;
//    public BundleProtocal[] ConnectProtocal;
//    public string TypeID;
//    public string TypePassword;
//    public string SeverDDNS;
//    public string SeverBundlePath;
//}

//public enum ConnectSeverType
//{
//    None = 0, //후에 로컬서버로 전환
//    NAS,
//    AWS
//}

#endregion

