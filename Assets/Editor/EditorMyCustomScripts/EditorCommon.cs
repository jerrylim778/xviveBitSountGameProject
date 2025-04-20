using System.Collections.Generic;
using System.Collections;
using UnityEngine;


namespace Commons.Editor.EditorCommons
{

    public static class ReqriedEditorApplyPath
    {
        public enum PathType { ScriptFile, ReferenceFile}

        //public static readonly string m_PlayerSystemPrefabPath = $"{Application.dataPath}/{nameof(Resources)}/Objects/DefultObject/Systems/CharacterSystems";

        public static Dictionary<PathType, string> m_PlayerSystemPrefabScirptReferencePath = new Dictionary<PathType, string>()
        {
            { PathType.ScriptFile, ""},
            { PathType.ReferenceFile, $"{Application.dataPath}/{nameof(Resources)}/Objects/DefultObject/Systems/CharacterSystems"}
            //new KeyValuePair<PathType, string>(PathType.ScriptFile, ""),
            //new KeyValuePair<PathType, string>(PathType.ScriptFile, ""),
        };
        
    }


}
