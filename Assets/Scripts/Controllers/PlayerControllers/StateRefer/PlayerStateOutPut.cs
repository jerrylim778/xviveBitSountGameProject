using System.Collections.Generic;
using System.Linq;
using Commons.Helpers;
using UnityEngine;

public class PlayerStateOutPut //: CharacterStateBase
{
    //public void Initlization(StateMachineController _GetAnimControllHeader, SkillDataInfo[] _SkillDataInfos)
    //{
    //    base.Initlization(_GetAnimControllHeader);
    //    #region 이후 table(CSV) => DataManager 를 통해 가져와야할 사항
    //    //<****** 전부 테이블에 의해 해당되는것 가져와서 배치할 수 있도록 수정한다
    //    //테이블에 타입이 배열로 존재하며 배열로 존재하는 타입에 따라 전부
    //    //상속받는것을 대입하면 되겠다 ******>
    //    //Interaction 또한 마찬가지다 해당 구역에서 동작할 State가 존재하지 않는다면
    //    //일반 State로 적용하되 그게 아니라면 해당 되는것 적용할 수 있도록 수정한다
    //    #endregion

    //    //m_CharacterBaseStateDic.Add(CharacterDefultStateType.StateOfAppearanceBefore, new AppearanceBeforeState(this)); //이전 연출 애니가 존재할경우 사용
    //    m_CharacterBaseStateDic.Add(CharacterDefultStateType.Idle, new IdleState(this));
    //    List<SkillDataInfo> GetNewList = _SkillDataInfos.ToList().FindAll(x => x.s_interactionAnim);
    //    GetNewList.ForEach(x => //CharacterController로 부터 데이터를 가져와 원래 있었던 값을 대입하는 방식
    //    {
    //        //m_InteractionStateDic.Add(x.s_ItemIDX, x.s_ItemIDX switch
    //        //{ 158 => new Type8State(this, x), 157 => new Type7State(this, x), _ => new CharacterSkillState(this, x) });
    //    });
    //    //ChangeState(CharacterDefultStateType.StateOfAppearanceBefore);
    //    ChangeState(pp_DefultPlayerState, true);
    //}
}