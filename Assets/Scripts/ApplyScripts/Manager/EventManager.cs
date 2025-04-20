using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Commons.Helpers;

#region U 2.0 Object Pettern
public enum EventListenType
{
    
    #region 이전 적용했던 것들 적용했던것들 => 이후 삭제 요망
    MFLController_ReStart,
    MFLController_Continue,//계속하기는 region MatchFactory관련 EndPopUp의 적용 설명 참조
    TestMFLGameRequiredPopUp_TestReStartGameBtn,
    TestMFLGameRequiredPopUp_ReStartGame,
    TestMFLGameRequiredPopUp_PauseGame,
    TestMFLGameRequiredPopUp_ItemModuleUpdate,
    //TestMFLGameRequiredPopUp_ItemModuleLoadAllCount, 이후에 배열별로 추가할것이 인게임내에 들어갈 수 있음
    //InGameUsed_HashSaved_ComplateGameStart,
    //ShapesManager_RestartGame,
    //ShapesManager_BonusType,
    //ShapesManager_ItemSizeScale,
    //ShapesManager_ItemVerticalCount,
    //ShapesManager_ItemHorizontalCount,
    //ShapesManager_ItemExplosionRandom,
    //GUIController_ThreeMatchGameStart,
    //GUIController_OutPutScore,
    //GUIController_BonusGage
    #endregion
}

public interface I_UseEventData
{
    public EventListenType[] I_DivisionTypes();

    public EventManager I_MainEventManager();

    public void I_OrderEventDefine(EventListenType _GetType, I_EventData _GetInter = null);
}


public interface I_EventData
{
    //Do Not Define......
}

public class EventManager
{
    private readonly Dictionary<EventListenType, System.Action<EventListenType, I_EventData>> m_DicEventMap = new();
    //private readonly Dictionary<EventListenType, object> m_DicEventSavedMap = new(); //[보류]

    public void Subscribe(EventListenType _GetType, System.Action<EventListenType, I_EventData> _GetAction)
    {
        if (!m_DicEventMap.ContainsKey(_GetType))
        {
            m_DicEventMap[_GetType] = _GetAction;
            return;
        }
        m_DicEventMap[_GetType] += _GetAction;
    }

    #region 메모리 추가 없이 등록하는 방법 [보류]
    //public void Subscribe<T>(EventListenType _GetType, T _SavedTemp,
    //System.Action<EventListenType, I_EventData> _GetAction) where T : struct
    //{
    //    if (!m_DicEventMap.ContainsKey(_GetType))
    //    {
    //        m_DicEventMap[_GetType] = _GetAction;
    //        m_DicEventSavedMap[_GetType] = _SavedTemp;
    //        return;
    //    }
    //    switch (_SavedTemp)
    //    {
    //        case int _GetIntValue:
    //            int GetOGIValue = (int)m_DicEventSavedMap[_GetType];
    //            GetOGIValue += _GetIntValue;
    //            m_DicEventSavedMap[_GetType] = GetOGIValue;
    //            break;
    //        case float _GetFloatValue:
    //            float GetOGFValue = (float)m_DicEventSavedMap[_GetType];
    //            GetOGFValue += _GetFloatValue;
    //            m_DicEventSavedMap[_GetType] = GetOGFValue;
    //            break;
    //    }
    //}
    #endregion

    public void Unsubscribe(EventListenType _GetType, System.Action<EventListenType, I_EventData> _GetAction)
    {
        if (!m_DicEventMap.ContainsKey(_GetType)) return;
        m_DicEventMap[_GetType] -= _GetAction;
    }

    public void Notify(EventListenType _GetType, I_EventData _GetInterfaceData = null)
    {
        if (m_DicEventMap.TryGetValue(_GetType, out var _GetCallBack)) _GetCallBack?.Invoke(_GetType, _GetInterfaceData);
    }

    public int GetSubscribeCount(EventListenType _GetType)
    {
        if (!m_DicEventMap.ContainsKey(_GetType)) return -1;
        return m_DicEventMap[_GetType].GetInvocationList().Length;
    }

    public EventListenType[] CheckAllEventTypeByTemp<T>()
    {
        string[] ArrayTypes = Helper.GetEnumByStringArray<EventListenType>(true).ToList().FindAll(x => x.Contains(typeof(T).Name)).ToArray();
        EventListenType[] ReturnValues = new EventListenType[ArrayTypes.Length];
        int CountIDX = 0; ArrayTypes.HForEach(x =>
        {
            ReturnValues[CountIDX] = Helper.StringToEnum<EventListenType>(x);
            CountIDX++;
        });
        return ReturnValues;
    }
}

#endregion