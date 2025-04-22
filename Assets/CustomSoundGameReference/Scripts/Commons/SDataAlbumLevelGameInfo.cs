using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using Commons.Helpers;


[CreateAssetMenu(fileName = nameof(SDataAlbumLevelGameInfo),
menuName = "DefultScriptableObject/" + nameof(SDataAlbumLevelGameInfo), order = int.MaxValue)]
public class SDataAlbumLevelGameInfo : ScriptableObject
{
    #region ODIN_OutPut Item List
    private IEnumerable<ValueDropdownItem<AlbumInfo>> GetClassLevelListAsDropdown()// 드롭다운에 표시할 리스트를 반환하는 함수
    {
        foreach (var item in m_DSAlbumLevelInfos)
            yield return new ValueDropdownItem<AlbumInfo>($"{item.s_ItemIDX} (Value: {item.s_ItemName})", item);
    }

    [Button("ODIN_AddDefultSoundLevelInfo")]
    private void ODIN_AddDefultSoundLevelInfo()
    => m_DSAlbumLevelInfos.HODIN_AddItemByList<AlbumInfo>();
    #endregion

    //[SerializeField] private AudioClipSpectrumExtractor m_DScriptableBitData;
    [SerializeField, InlineProperty, HideLabel, ValueDropdown(nameof(GetClassLevelListAsDropdown))] private List<AlbumInfo> m_DSAlbumLevelInfos = new();
    public List<AlbumInfo> pp_DSAlbumLevelInfos => m_DSAlbumLevelInfos;
}
