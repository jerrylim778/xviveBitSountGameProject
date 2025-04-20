using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Sirenix.OdinInspector;
using Commons.Helpers;

public class HandSubModuleController : SubModuleControllerBase
{
    //캐릭터 인터렉션 1 Hand Controller
    //해당 구역은 AnimationController<클래스>의 통제에 따르며
    //해당되는 데이터는 중간에 들어온 데이터를 다른곳으로 옮기는 
    //버퍼역활정도로 데이터를 가지고 있으며 각 상태에 따라
    //Send Reciver를 해줄 수 있도록 수정한다

    [field: SerializeField, ReadOnly] public Stack<GameObject> m_ReciverObjs { get; private set; }
    [SerializeField, ReadOnly] private Animator m_MainAnimController;
    [field: SerializeField, ReadOnly] public Transform pp_LHandTr { get; private set; } //Plan_First_90
    private readonly int m_HandLayerIDX = 1;
    private int m_MaxCarryCount = 3;//8;
    public int pp_PlayerHaveCurrCount { get => m_ReciverObjs.Count; }
    public int pp_PlayerHaveMaxCount { get => m_MaxCarryCount; }

    public override object[] Initlization(params object[] _ParsingParams)
    {
        base.Initlization(_ParsingParams);
        if(m_MainController is AnimationController _GetAniCon) m_MainAnimController = _GetAniCon.pp_MainAnimator;
        else if(m_MainController is TPSPackageSubModuleController _GetTPSPack) m_MainAnimController = _GetTPSPack.pp_MainAnimator;
        //각 데이터에 따른 손내부의 지정사항을 가져올 수 있도록 수정할것
        pp_LHandTr = Helper.ChildLinearStuctureSearch(
        m_MainAnimController.GetBoneTransform(HumanBodyBones.LeftHand)).HFind(x => x.name == "Plan_First_90");
        if (pp_LHandTr == null) pp_LHandTr = m_MainAnimController.GetBoneTransform(HumanBodyBones.LeftHand);
        m_ReciverObjs = new();
        return null;
    }
    
    public override void ExecuteChangeSubController()
    {
        base.ExecuteChangeSubController();
        m_MainAnimController.SetLayerWeight(m_HandLayerIDX, 0);
        m_ReciverObjs.ToList().ForEach(x => Destroy(x));
        m_ReciverObjs.Clear();
    }

    #region Condition Reference
    public bool CheckAll(GameObject _GetObj)
    => !CheckCountEnter(_GetObj) || !CheckCanEnter(_GetObj);

    public bool CheckCanEnter(GameObject _GetObj) => 
    m_ReciverObjs.Count <= 0 || (m_ReciverObjs.Count > 0 && m_ReciverObjs.Peek().name == _GetObj.name);

    public bool CheckCountEnter(GameObject _GetObj) =>
    m_ReciverObjs.Count <= 0 || m_ReciverObjs.Count < m_MaxCarryCount;

    public bool CheckCountISEmpty()
    => m_ReciverObjs.Count <= 0;
    #endregion

    public void ChangeMaxValue(int _GetValue) => m_MaxCarryCount = _GetValue;

    public Transform PeekStackHandTr()
    => m_ReciverObjs.Count <= 0 ? pp_LHandTr : m_ReciverObjs.Peek().transform;

    public void EnterPushHand(GameObject _GetObj, float _GetStaticPosY = 0f, VectorArrowType _ArrowType = VectorArrowType.Up)
    {
        if (_GetObj == null || m_ReciverObjs == null) return;
        if (!CheckCountEnter(_GetObj) || !CheckCanEnter(_GetObj)) return;

        ReCalculateObjPosRot(_GetObj, _GetStaticPosY, _ArrowType);
        if (m_MainAnimController.GetLayerWeight(m_HandLayerIDX) < 1)
        m_MainAnimController.SetLayerWeight(m_HandLayerIDX, 1);
        m_ReciverObjs.Push(_GetObj);
    }

    public GameObject OutPopHand(Transform _GetTr = null)
    {
        if (m_ReciverObjs.Count <= 0) return null;
        var GetObj = m_ReciverObjs.Pop();
        GetObj.transform.SetParent(_GetTr);
        if (m_ReciverObjs.Count <= 0)
        m_MainAnimController.SetLayerWeight(m_HandLayerIDX, 0);
        return GetObj;
    }

    private void ReCalculateObjPosRot(GameObject _GetObj, float _GetStaticPosY, VectorArrowType _ArrowType = VectorArrowType.Up)
    {
        _GetObj.transform.SetParent(pp_LHandTr);
        _GetObj.transform.localPosition = Vector3.zero;
        _GetObj.transform.localRotation = Quaternion.Euler(Vector3.zero);

        if (m_ReciverObjs.Count <= 0) return;
        var GetBeforeObj = m_ReciverObjs.Peek();
        Vector3 ApplyArrowSet = _ArrowType switch
        {
            VectorArrowType.Up => GetBeforeObj.transform.up,
            VectorArrowType.Down => -GetBeforeObj.transform.up,
            VectorArrowType.Left => -GetBeforeObj.transform.right,
            VectorArrowType.Right => GetBeforeObj.transform.right,
            _ => Vector3.zero
        };
        _GetObj.transform.position = GetBeforeObj.transform.position + ApplyArrowSet * _GetStaticPosY;
    }

    public void EnterInOnce(GameObject[] _GetObjs, float _GetStaticPosY = 0f, VectorArrowType _ArrowType = VectorArrowType.Up)
    => _GetObjs.HForEach(x => EnterPushHand(x, _GetStaticPosY, _ArrowType));
}
