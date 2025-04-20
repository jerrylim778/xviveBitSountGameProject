using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PhycisOutPutDefultAI : PhycisOutPutBase
{
    public override void Initlization(NewPlayerCharacterController _getMainController)
    {
        m_isActionPhycisBase = false;
        base.SubInitlization(_getMainController.gameObject);
        m_CharacterController = _getMainController;
        m_InputInfo = _getMainController.pp_InputController.mspp_InputValues;
        m_AnimValuesInfo = _getMainController.pp_AnimationController.m_AnimValuesInfo;
        m_RuntimeAnimNow = _getMainController.pp_AnimationController.pp_MainAnimator;
        CalculationPhycisList.Add(new Direction_Phycis(this));
        CalculationPhycisList.Add(new Rotate_Phycis(this));
        CalculationPhycisList.Add(new Ground_Phycis(this));
        CalculationPhycisList.Add(new AIMove_Phycis(this));
        CalculationPhycisList.ForEach(x => { if (x.m_isMustCheckSubInit) x.SubInitlization(); });
    }

    public override void Initlization(NewCharacterController _getMainController)
    {
        throw new System.NotImplementedException();
    }



    public override void ActionPhycis()
    {
        m_isActionPhycisBase = true;
    }


    #region 유니티 이벤트 함수관련 상속들

    private void MoveActionValue(Vector3 _deltaPos, Quaternion _deltaRot)
    {
        FixedTime += Time.deltaTime;
        m_FixedUpdatePosition += _deltaPos;
        m_FixedUpdateRotation *= _deltaRot;
    }

    protected override void ProcessOnDestroy()
    {
        base.ProcessOnDestroy();
        CalculationPhycisList.ForEach(x => x.OnDestroyByPhycisHelper());
        CalculationPhycisList.Clear();
    }

    protected override void ProcessAnimationMove()
    {
        base.ProcessAnimationMove();
        CalculationPhycisList[1].UpdateAnimationMove(m_CharacterController.pp_AnimationController);
        //CalculationPhycisList[1].UpdateAnimationMove(m_CharacterController.pp_AnimationController);
        MoveActionValue(m_RuntimeAnimNow.deltaPosition, m_RuntimeAnimNow.deltaRotation);
    }

    protected override void ProcessUpdate()
    {
        base.ProcessUpdate();

        CalculationPhycisList.ForEach(x => x.UpdatePhycis());
    }

    protected override void ProcessFixedUpdate()
    {
        base.ProcessFixedUpdate();

        CalculationPhycisList.ForEach(x => x.FixedUpdatePycis());
    }

    protected override void ProcessCollisionEnterEvent(GameObject _GetObj)
    {
        base.ProcessCollisionEnterEvent(_GetObj);

        CalculationPhycisList.ForEach(x => x.CollisionEnterEventByPhycisHelper(_GetObj));
    }
    #endregion
}
