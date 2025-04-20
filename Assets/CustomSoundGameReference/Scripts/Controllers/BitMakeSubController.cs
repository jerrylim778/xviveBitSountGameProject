using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Sirenix.OdinInspector;
using Commons.Helpers;
using System;

#region 구현 메카니즘 설명
//=>줄이 넘길때 Collider를 사용하여 들어왔을때 음원이 재생될 수 있도록 하고
//모든 AudioSource는 AudioManager<클래스>의 AudioASInfo<클래스>를 걷혀서
//가져올 수 있도록 한다 (GameSFX 와 BGM등을 구분하기 위해서)

//	(1 *)단 AudioSource의 장착범위가 비정상적으로 많아진다면 우선 
//	AudioManager<클래스>와 독립적으로 AudioASInfo<클래스>를 
//	분리하여 인위적으로 배치후 역으로 있던것을 오디오 관리자에 부착하는
//	방식으로 갈 수 있도록 한다
#endregion
public class BitMakeSubController : SubModuleControllerBase
{
    //[구현 목표]
    //1.포인터가 좌에서 우로 이동
    //2.이동 최소 최대 범위 이동시 리턴
    //3.AudioASInfoModule의 집합을 Collider형태로 발동
    //4.3번까지 이상이 없다면 월드형 스크롤뷰를 만들것
    [Header("Test Control Values")]
    [SerializeField, ShowIf(nameof(m_isTestMode))] private bool m_isUsedStartEvent;

    [Header("Apply Data Values")] //말그대로 이후에 적용해야되는 로직
    [SerializeField, Range(1f, 200f)] private float m_BPMSpeed;

    [Header("Control Values")]
    [SerializeField] private float m_MStickMaxX;
    [SerializeField] private float m_MStickMinX;

    [Header("Required Values")]
    [SerializeField] private Transform m_ArrowModule;
    [SerializeField] private Transform m_ASModuleCollectionBG;
    [SerializeField] private List<ElemCategorySubCollModule> m_GetAllElemCategorySubModules = new();

    //<**적용 주의사항**>
    //데이터가 따로 존재해야지 적용가능하다
    //콜리전으로 실행할경우 실행시 타 비트 확인때
    //돌아갈 근거가 부족하다
    //따라서 데이터 형태로 가지고 있다가 해당 데이터 순데로
    //진행되어야 맞는그림이다

    //<**실행 메카니즘**>
    //정지 후 재생할때는 무조건 첫번째로
    //재생중에 중간 클릭시 재생은 계속되지만
    //타이밍에 맞게 사용자의 들어와야한다
    //코루틴으로 대기시간을 적용할 수 있도록 하면 될듯

    public override object[] Initlization(params object[] _ParsingParams)
    {
        //var PartOfParam = base.Initlization(_ParsingParams);
        //m_BPMSpeed //외부에서 Model(데이터) => Speed가 몇인지 적용해야됨
        m_GetAllElemCategorySubModules = m_ASModuleCollectionBG.ChildLinearStuctureSearch<ElemCategorySubCollModule>().ToList();
        m_GetAllElemCategorySubModules.ForEach(x => x.Initlization(this/*, 해당 구역에 적용할 Clip들을 넣으면 되겠다*/));

        m_ArrowModule.position = new Vector3(
        m_ArrowModule.position.x, m_ArrowModule.position.y, m_ArrowModule.position.z);

        m_isActionProcess = m_isTestMode;
        pp_isSelfProcessUpdate = m_isTestMode;
        if (m_isTestMode) BreakPoint(true);
        else FindParentsObjTypeByTemp<SoundGameController>().I_CheckSubModuleIsAllCanAction(this);
        return null;
    }

    public override void BreakPoint(bool _isBreak, SubModuleBreakType _BrackType = SubModuleBreakType.BreakAll, Type _GetType = null)
    {
        base.BreakPoint(_isBreak, _BrackType, _GetType);

        m_GetAllElemCategorySubModules.ForEach(x => x.SemiBreakPoint(_isBreak));
    }

    #region Main System Private Functions (**Update Reference**)

    private void UpdateMusicStickMove()
    {
        Vector3 currentPosition = m_ArrowModule.position;

        currentPosition.x += m_BPMSpeed * Time.deltaTime;
        if (currentPosition.x > m_MStickMaxX) currentPosition.x = m_MStickMinX;
        m_ArrowModule.position = currentPosition;
    }

    //데이터의 흐름에 따라 적용할 수 있는 버전
    private void UpdateDataLinerMove()
    {
        //각 리스트로 들고 있다가 해당되는 사운드를 출력할 수 있도록 한다
    }

    //레이의 기능은 이후 더 상위에 존재하는 SoundGameController<클래스>에서
    //기능을 가져와 사용해야한다
    private void UpdateRayAudioASModule()
    {
        if (Input.GetMouseButtonDown(0))
        {
            var GetWorldMousePoint = Camera.main.ScreenToWorldPoint(Input.mousePosition);

            var GetHit2D = Physics2D.Raycast(GetWorldMousePoint, Vector2.zero);

            if (GetHit2D.collider == null) return;

            if (!GetHit2D.collider.gameObject.
            TryGetComponent<AudioASModule>(out AudioASModule _GetModule)) return;

            _GetModule.SwichTurnBtn();
        }
    }
    #endregion

    #region 이후 클로드가 알려준 월드형 그리드를 참고할것
    //현재 스프라이트 기준 가로 : 0.7 세로 : 0.67 
    //if (Input.GetKeyDown(KeyCode.Space))
    //{
    //    var GetTrs =
    //    Helper.ChildLinearStuctureSearch(m_ASModuleCollectionBG.transform);
    //    GetTrs.HForEach(x =>
    //    TestGridHorizontialEvent(x.GetComponent<SpriteRenderer>(), 0.7f, 7));
    //}

    //private void TestGridHorizontialEvent(SpriteRenderer _StartSR, float _Scale, int _CountIDX)
    //{
    //    var GetElemList = new List<SpriteRenderer>();
    //    Helper.HCountForEach(0, _CountIDX - 1, _CountIDX =>
    //    {
    //        var GetInstanceSR = Instantiate(_StartSR, _StartSR.transform.position, 
    //        Quaternion.identity, m_ASModuleCollectionBG);
    //        var GetPos = GetInstanceSR.transform.position;
    //        GetPos.x = GetElemList.Count <= 0? GetPos.x + _Scale : 
    //        GetElemList[GetElemList.Count - 1].transform.position.x + _Scale;
    //        GetInstanceSR.transform.position = GetPos;
    //        GetElemList.Add(GetInstanceSR);
    //    });
    //}
    #endregion

    #region 유니티 이벤트 함수

    private void Start()
    {
        if (!m_isTestMode || !m_isUsedStartEvent) return;
        Initlization(null);
    }

    public override void ProcessUpdate()
    {
        base.ProcessUpdate();
        if (!m_isActionProcess) return;
        
        //현재 음악이 없거나 퍼즈시 멈춰야됨
        //UpdateMusicStickMove();

        UpdateRayAudioASModule();
    }
    #endregion
}
