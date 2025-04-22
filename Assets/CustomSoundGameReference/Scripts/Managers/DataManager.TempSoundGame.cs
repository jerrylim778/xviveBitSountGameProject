using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using Commons.Helpers;

public sealed partial class DataManager : MonoBehaviour
{
    [Space(20f)]

    [Header("Character Reference Control")]
    [SerializeField] private bool test_isShowTempSoundGameInfo;

    [Header("===TempCharacterDatas===")]
    [SerializeField, ShowIf(nameof(ODINToogleSoundGameReference))] private SoundGameSavedData s_TempSoundGameSavedData;
    [SerializeField, ShowIf(nameof(ODINToogleSoundGameReference))] private SDataAlbumLevelGameInfo s_DSDataAlbumInfo;

    public SDataAlbumLevelGameInfo spp_DSDataAlbumInfo => s_DSDataAlbumInfo;
    public SoundGameSavedData spp_TempSoundGameSavedData => s_TempSoundGameSavedData;

    private bool ODINToogleSoundGameReference() => test_isAppliedTemporyAll && test_isShowTempSoundGameInfo;

    public AlbumInfo TempReciveSoundGameMainInfo(int _ReciveIDX)
    => s_DSDataAlbumInfo.pp_DSAlbumLevelInfos.Find(x => x.s_ItemIDX == _ReciveIDX);

    //원래는 DataManager<클래스>의 로딩 파싱때 적용해야 한다
    public SoundGameMyInfo TempInitSoundGameMyInfo()
    {
        s_TempSoundGameSavedData.s_DBSavedIDX = "TestLoginKey";
        m_InitMyinfo = new SoundGameMyInfo(Appinstance.Instance, "TestLoginKey");
        return m_InitMyinfo as SoundGameMyInfo;
    }
    
}
