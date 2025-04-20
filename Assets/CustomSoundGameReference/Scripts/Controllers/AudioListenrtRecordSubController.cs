using UnityEngine;

public class AudioListenrtRecordSubController : SubModuleControllerBase
{

    public override object[] Initlization(params object[] _ParsingParams)
    {
        var PartParams = base.Initlization(_ParsingParams);

        FindParentsObjTypeByTemp<SoundGameController>().I_CheckSubModuleIsAllCanAction(this);
        return PartParams;
    }
}
