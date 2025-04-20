using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using Commons.Helpers;


[CreateAssetMenu(fileName = nameof(SoundGameClipInfo),
menuName = "DefultScriptableObject/" + nameof(SoundGameClipInfo), order = int.MaxValue)]
public class SDataSoundGameInfo : ScriptableObject
{
    #region ODIN_OutPut Item List
    private IEnumerable<ValueDropdownItem<SoundGameClipInfo>> GetClassListAsDropdown()// 드롭다운에 표시할 리스트를 반환하는 함수
    {
        foreach (var item in m_DSSoundgameClipInfos)
            yield return new ValueDropdownItem<SoundGameClipInfo>($"{item.s_ItemIDX} (Value: {item.s_ItemName})", item);
    }

    [Button("ODIN_AddDefultCharacterInfo")]
    private void ODIN_AddDefultCharacterInfo()
    => m_DSSoundgameClipInfos.HODIN_AddItemByList<SoundGameClipInfo>();
    #endregion

    //[SerializeField] private AudioClipSpectrumExtractor m_DScriptableBitData;
    [SerializeField, InlineProperty, HideLabel, ValueDropdown("GetClassListAsDropdown")] private List<SoundGameClipInfo> m_DSSoundgameClipInfos = new();
    public List<SoundGameClipInfo> pp_DSSoundgameClipInfos => m_DSSoundgameClipInfos;
}

[System.Serializable] 
public class SoundGameClipInfo : OtherAllClips
{
    public bool s_isLoop;
    [ReadOnly] public List<BitInfo> s_ReciveBitInfoByClipName = new();
}
