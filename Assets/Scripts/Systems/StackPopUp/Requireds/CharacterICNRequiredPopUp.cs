using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Sirenix.OdinInspector;
using Commons.Helpers;

public class CharacterICNRequiredPopUp : SystemBaseGUIPopUpRequired, I_PopUpPush
{
    #region 사용자가 조정하는 캐릭터 인터렉션 필수팝업에 관한 설명
    //1.오르지 CharacterMyInfo => NewCharacterController 체재로 MVC를 설정할때
    //캐릭터와 인터렉션하는 필 수 GUI 요소들을 정의한다
    //2.원래는 Controller에서 계산이 마친 데이터를 가져와 OutPut 즉 View객체의 역활만
    //진행되어야 하나 GUI를 가지고 계산해서 넘기는 작업이 존재하기에 Controller역활을 하는 인터페이스
    //상속받아 해당 구역에서 진행할 수 있도록 한다 (물론 해당 Controller는 메인 해더격인 팝업의 종속성을 강제하고 있으며)
    //3.강제 받은 각 팝업 서브 Controller객체는 요청즉시 캐릭터 필수 팝업에 할당받는다
    //View역활 또한 Controller객체에서 함께 진행되거나 데이터만 전달받고 View는 그대로 해당 팝업에 정적으로 
    //존재할지는 차후 구현의 성과에 따라 설계 및 구현할 수 있도록 한다
    #endregion
   
    public void InitCharacterConnectPopUp(NewCharacterController _GetCharacter)
    {
        base.Initlization();
    }

    public void PushOutEvent(System.Action _AddCallBack = null)
    {
        //나중에 체력이나 기타 가져가야할게 존재할시 적용할것
    }
}