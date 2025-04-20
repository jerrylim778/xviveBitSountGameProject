using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Commons;
using Commons.Helpers;

public class SettingManager : MonoBehaviour, I_CheckInit
{
    public enum SettingParsingDataType
    {
        InitType,
        ResetType,
        ExistingReferenceType //기존 데이터 파싱 타입에 해당
    }

    private SettingCategoryInfoBase[] m_OGSavedSettingInfos, m_ListenSettingInfos;
    private string[] m_SettingDataStrOfTypes = null;
    private bool m_isInit = false;
    

    public void Initlization()
    {
        if (m_isInit) return;
        m_OGSavedSettingInfos = new SettingCategoryInfoBase[1];
        m_SettingDataStrOfTypes = Helper.GetEnumByStringArray<SettingCatrcoryType>(true);
        for(int i = 0; i < m_OGSavedSettingInfos.Length; i++)
        {
            m_OGSavedSettingInfos[i] = 
            NewSettingInfoOfType(m_SettingDataStrOfTypes[i].StringToEnum<SettingCatrcoryType>(), SettingParsingDataType.InitType);
        }
        m_ListenSettingInfos = m_OGSavedSettingInfos;
        m_isInit = true;
    }
    public bool ISInitlization() => m_isInit;

    #region Setting Data Main System Public Functions

    #region Setting Info Reset Reference
    public void AllResetJsonData(SettingCatrcoryType _currState = SettingCatrcoryType.None)
    {
        if(_currState != SettingCatrcoryType.None)
        {
            int FIDX = m_OGSavedSettingInfos.ToList().FindIndex(x => x.s_CategoryGetType == _currState);
            m_OGSavedSettingInfos[FIDX] = 
            NewSettingInfoOfType(_currState, SettingParsingDataType.ResetType);
            m_ListenSettingInfos[FIDX] = m_OGSavedSettingInfos[FIDX];
            LocalSettingJsonData(SettingJsonType.Save);
            return;
        }

        int CountIDX = 0; m_OGSavedSettingInfos.ToList().ForEach(x =>
        {
            m_OGSavedSettingInfos[CountIDX] = NewSettingInfoOfType(m_SettingDataStrOfTypes[CountIDX].
            StringToEnum<SettingCatrcoryType>(), SettingParsingDataType.ResetType);
            x.SettingValueApplyInGame(out bool _isCategorySettingApply);
            CountIDX++;
        });

        m_ListenSettingInfos = m_OGSavedSettingInfos;
        LocalSettingJsonData(SettingJsonType.AllReset);
    }
    #endregion

    //오직 로컬에 해당되기 때문에 이후에는 서버제이슨데이터 + PlayerPrefs(레지트리 저장로드)로 변경할 수 있도록 수정 요망..
    //또한 로드되는 시점은 로컬 또한 마찬가지로 서버의 데이터를 전부 파싱하고 그 이후에 진행되는 메카니즘으로 진행되어야한다
    public void LocalSettingJsonData(SettingJsonType _currState)
    {
        if (_currState == SettingJsonType.Save) m_OGSavedSettingInfos = m_ListenSettingInfos;

        int CountIDX = 0; m_OGSavedSettingInfos.ToList().ForEach(x =>
        {
            SettingCatrcoryType GetType = m_SettingDataStrOfTypes[CountIDX].StringToEnum<SettingCatrcoryType>();
            if (_currState == SettingJsonType.Save || _currState == SettingJsonType.AllReset)
            {
                string SavedJson = JsonUtility.ToJson(NewSettingInfoOfType(GetType, _currState == SettingJsonType.AllReset?
                SettingParsingDataType.ResetType : SettingParsingDataType.ExistingReferenceType), true);
                PlatformManager.SetPlatformSaveData(PlatformManager.ParsingPathType.LocalJson, x.GetType().Name, SavedJson);
            }
            else if(_currState == SettingJsonType.Load)
            {
                if (PlatformManager.GetPlatformLoadData(
                PlatformManager.ParsingPathType.LocalJson, x.GetType().Name, true, out object _OutReference))
                m_OGSavedSettingInfos[CountIDX] = (SettingCategoryInfoBase)
                JsonUtility.FromJson(_OutReference.ToString(), GetTypeByAllCategory(GetType));
                else m_OGSavedSettingInfos[CountIDX] = NewSettingInfoOfType(GetType, SettingParsingDataType.ResetType);
            }
            CountIDX++;
        });

        if (_currState == SettingJsonType.Load) m_ListenSettingInfos = m_OGSavedSettingInfos;

    }

    #endregion

    #region Sub System Public Functions
    //데이터 송신
    public void SendSettingInfo(SettingCategoryInfoBase _GetInfoBase) 
    => m_ListenSettingInfos[m_ListenSettingInfos.ToList().FindIndex(x =>
    x.s_CategoryGetType == _GetInfoBase.s_CategoryGetType)] = _GetInfoBase;
    //데이터 수신
    public SettingCategoryInfoBase ReciveSettingInfo(SettingCatrcoryType _CompareType)
    => m_ListenSettingInfos.ToList().Find(x => x.s_CategoryGetType == _CompareType);
    //외부 참조 선택
    public T ReciveSettingInfo<T>(bool _isOGSaved = false) where T : SettingCategoryInfoBase
    {
        if (_isOGSaved) return m_OGSavedSettingInfos.ToList().Find(x => x is T) as T;
        return m_ListenSettingInfos.ToList().Find(x => x is T) as T;
    }

    #endregion

    #region Sub System Public Functions (All InGame Apply Reference)
    //Setting Info 전체 인게임 적용 (저장 혹은 로드 이후 적용해야함)
    public void AllSettingValueApplyInGame(bool _isAsync = false, System.Action _EndCallBack = null)
    {
        if (_isAsync) { StartCoroutine(GetAsyncApplyData(_EndCallBack)); return; }
        m_OGSavedSettingInfos.ToList().ForEach(x => x.SettingValueApplyInGame(out bool isCategorySettingApply));
    }

    //로컬이 아닌 서버로부터의 데이터를 받았을때 변경해야됨
    public void AllSettingLoadAndApplyAnyncParsing(System.Action _EndCallBack)
    {
        LocalSettingJsonData(SettingJsonType.Load); //이후에 해당 부분을 서버 + PlayerPrefs로 변경해야됨
        StartCoroutine(GetAsyncApplyData(_EndCallBack));
    }

    IEnumerator GetAsyncApplyData(System.Action _EndCallBack = null)
    {
        int CountIDX = 0;
        while (CountIDX < m_OGSavedSettingInfos.Length)
        {
            m_OGSavedSettingInfos[CountIDX].SettingValueApplyInGame(out bool isCategorySettingApply);
            yield return new WaitUntil(() => isCategorySettingApply);
            yield return new WaitForSeconds(0.5f);
            CountIDX++;
        }
        _EndCallBack?.Invoke();
    }
    #endregion

    #region Sub System Private Functions

    private SettingCategoryInfoBase NewSettingInfoOfType(
    SettingCatrcoryType _CompareType, SettingParsingDataType _GetSMPType)
    {
        switch (_CompareType)
        {
            case SettingCatrcoryType.Gameplay :
                if (_GetSMPType == SettingParsingDataType.ExistingReferenceType)
                    return ReciveSettingInfo<SettingCatecoryGamePlayInfo>(true);
                return new SettingCatecoryGamePlayInfo(_GetSMPType == SettingParsingDataType.ResetType);
        }
        return null;
    }

    private System.Type GetTypeByAllCategory(SettingCatrcoryType _CompareType)
    {
        System.Type GetNullOnlyType = null;
        switch (_CompareType)
        {
            case SettingCatrcoryType.Gameplay:
                GetNullOnlyType = typeof(SettingCatecoryGamePlayInfo);
                break;
        }
        return GetNullOnlyType;
    }

    #endregion
}