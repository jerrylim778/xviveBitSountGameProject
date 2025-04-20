using UnityEngine;
using Sirenix.OdinInspector;

public class SimpleCameraController : SubModuleControllerBase
{
    #region ODIN Reference By SpriteRenderer
    public bool ISOdinToggleFalse() => m_GetMainBGSR != null;
    #endregion
    //[ShowIf(nameof(ISOdinToggleFalse))]
    [SerializeField] private float m_CalculateValue;
    [SerializeField] private SpriteRenderer m_GetMainBGSR;

    public override object[] Initlization(params object[] _ParsingParams)
    {
        if (pp_isInitComplate) return null;
        if (!m_isTestMode)
        {
            var GetSubPars = base.Initlization(_ParsingParams);
            var ParamsData = (SimpleCameraData)GetSubPars[0];
            //pp_isRequiredSubModule = ParamsData.pp_isRequiredData;
            pp_isSelfProcessUpdate = ParamsData.pp_isSelfProcessUpdate; //((MFLActionInfo)GetSubPars[0]).s_isShowProduction; //다른 값으로 채워야 한다
            m_GetMainBGSR = ParamsData.pp_MainBackGround;
        }
        pp_isRequiredSubModule = true; pp_isInitComplate = true; m_isActionProcess = true;

        //if (m_MainController is I_SubModulesCollection GetSubModule) GetSubModule.I_CheckSubModuleIsAllCanAction(this);
        pp_isSelfProcessUpdate = m_isTestMode;
        m_isActionProcess = true;
        return null;
    }


    #region 유니티 이벤트 함수

    void Start()
    {
        if (!m_isTestMode) return;
        Initlization(null);
    }

    //빌드 후 확인뒤에 무조건 선행처리 인터페이스로 먼저 해당 계산이 먼저 실시되어야됨
    public override void ProcessUpdate()
    {
        base.ProcessUpdate();

        //float ApplyRatioX = m_GetMainBGSR.bounds.size.x * Screen.height / Screen.width * m_CalculateValue;//4.037f;
        //Camera.main.orthographicSize = ApplyRatioX > 5f ? ApplyRatioX : 5f;
        //Camera.main.fieldOfView = ApplyRatioX > 62f ? ApplyRatioX : 62f;

        Vector2 SpriteSize = m_GetMainBGSR.sprite.rect.size / m_GetMainBGSR.sprite.pixelsPerUnit;
        float ApplyRatioX = SpriteSize.x * Screen.height / Screen.width * m_CalculateValue;
        float SettingViewDis = Camera.main.orthographic ? 5f : 60f;
        if (Camera.main.orthographic) Camera.main.orthographicSize = ApplyRatioX > SettingViewDis ? ApplyRatioX : SettingViewDis;
        else Camera.main.fieldOfView = ApplyRatioX > SettingViewDis ? ApplyRatioX : SettingViewDis;
    }

    #endregion
}

[System.Serializable]
public struct SimpleCameraData : ControllerBase.I_PurifiedParamsData, ControllerBase.I_PurifiedAddKey
{
    //[field: SerializeField] public bool pp_isRequiredData { get; private set; }
    [field: SerializeField] public bool pp_isSelfProcessUpdate { get; private set; }
    [field: SerializeField] public SpriteRenderer pp_MainBackGround { get; private set; }

    [field: SerializeField, PropertyOrder(int.MinValue)] public int spp_SubItemIDX { get; private set; }
    public string I_SubInfoIDX() => spp_SubItemIDX.ToString();
    public bool I_ISGetOutParams(ControllerBase _GetPPData) => _GetPPData is SimpleCameraController;
}
