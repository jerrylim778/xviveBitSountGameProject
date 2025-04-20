using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Commons;

public class SettingCategory_Gameplay : SettingCategoryBase
{
    [SerializeField] private SettingCatecoryGamePlayInfo Listen_LastSaveGP = null;

    public override void ApplyPopUpOutPut()
    {
        //적용후 팝업에 보내질때에
        SettingCatecoryGamePlayInfo GetData = (SettingCatecoryGamePlayInfo)
        Appinstance.Instance.ms_SettingManager.ReciveSettingInfo(SettingCatrcoryType.Gameplay);
        Listen_LastSaveGP = GetData;
        m_GetButtonByCategory[0].s_UIEventAction =
        base.ApplyUIEvnetTypeByStrValue<Dropdown>(m_GetButtonByCategory[0].s_UIEventAction, GetData.s_TranslateStr);
    }

    public override void ApplyPopUpSendInfoData()
    {
        //1.먼저 저장후에
        //2.다시 모든 데이터를 토대로 적용에 들어간다
        Listen_LastSaveGP.s_TranslateStr
        = base.ApplyStrValueByUIEvnetType<Dropdown>(m_GetButtonByCategory[0].s_UIEventAction);

        Appinstance.Instance.ms_SettingManager.SendSettingInfo(Listen_LastSaveGP);
    }
}

[System.Serializable]
public class SettingCatecoryGamePlayInfo : SettingCategoryInfoBase
{
    public string s_TranslateStr;

    public SettingCatecoryGamePlayInfo(bool _isResetData = false)
    {
        s_CategoryGetType = SettingCatrcoryType.Gameplay;
        if (_isResetData) 
        {
            s_TranslateStr = "(KR) 한국어";
        }
    }

    public override void SettingValueApplyInGame(out bool _isCategorySettingApply)
    {
        //관련된 변역 메니져를 만든이후에 적용때 전부 적용할 수 있도록 수정
        Appinstance.Instance.ms_TranslateManager?.ChangeTraslate(s_TranslateStr);
        _isCategorySettingApply = true;
    }
}
