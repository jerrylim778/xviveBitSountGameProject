using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PhycisOutPutDefult : PhycisOutPutBase
{
    AnimationController m_GetMainAnimationController;

    //삭제후 아래 로직으로 진행할것
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
        CalculationPhycisList.ForEach(x => { if (x.m_isMustCheckSubInit) x.SubInitlization(); });
    }

    public override void Initlization(NewCharacterController _getMainController)
    {
        m_isActionPhycisBase = false;
        base.SubInitlization(_getMainController.gameObject);
        m_NewCharacterController = _getMainController;
        m_InputInfo = _getMainController.I_GetSubModule<BInputController>().mspp_InputValues;
        m_GetMainAnimationController = _getMainController.I_GetSubModule<AnimationController>();
        m_AnimValuesInfo = m_GetMainAnimationController.m_AnimValuesInfo;
        m_RuntimeAnimNow = m_GetMainAnimationController.pp_MainAnimator;
        CalculationPhycisList.Add(new Direction_Phycis(this));
        CalculationPhycisList.Add(new Rotate_Phycis(this));
        CalculationPhycisList.Add(new Ground_Phycis(this));
        CalculationPhycisList.ForEach(x => { if (x.m_isMustCheckSubInit) x.SubInitlization(); });
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
        CalculationPhycisList[1].UpdateAnimationMove(m_GetMainAnimationController);
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
    #endregion
}
