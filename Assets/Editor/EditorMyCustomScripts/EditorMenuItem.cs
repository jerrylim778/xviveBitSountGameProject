using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;
using Commons.Editor.EditorCommons.Helpers;

public static class EditorMenuItem
{
    #region Editor Window Reference

    [MenuItem("EditorCustomSettings/TableParsingWindow")]
    public static void TableParsingWindow()
    {
        //EditorWindow.GetWindow<Test_EditorWindowByXCELToTextAsset>();
        EditorWindow.GetWindow<EditorWindowByTableParsing>();
    }

    //[MenuItem("EditorCustomSettings/DataSet_TyconProject")]
    //public static void DataSet_TyconProject()
    //=> EditorWindow.GetWindow<TestEditorWindow>();

    #endregion

    #region Hierachy Window Reference
    //GUI => [GameObject/UI/] Reference
    [MenuItem("GameObject/UI/LimCustom Text - TextTranslate", false, int.MaxValue)]
    public static void CreateTextTranslate(MenuCommand menuCommand)
    {
        GameObject NewTMPTxt = new GameObject("Text (TextTranslate)(CustomGUI)");
        NewTMPTxt.transform.SetParent(EditorHelper.FindParentByClickPoint(menuCommand)); //항시 부모부터 배치해야됨
        NewTMPTxt.CreateObjectSyncTr<Text_Translate>(menuCommand).text = "New Text";

        Selection.activeGameObject = NewTMPTxt;
        Undo.RegisterCreatedObjectUndo(NewTMPTxt, "Create" + NewTMPTxt.name);
    }
    
    [MenuItem("GameObject/UI/LimCustom Text - TMPTranslate", false, int.MaxValue)]
    public static void CreateTMPTextTranslate(MenuCommand menuCommand)
    {
        GameObject NewTMPTxt = new GameObject("Text (TMPTranslate)(CustomGUI)");
        NewTMPTxt.transform.SetParent(EditorHelper.FindParentByClickPoint(menuCommand)); //항시 부모부터 배치해야됨
        NewTMPTxt.CreateObjectSyncTr<TMPText_Translate>(menuCommand).text = "New Text";

        Selection.activeGameObject = NewTMPTxt;
        Undo.RegisterCreatedObjectUndo(NewTMPTxt, "Create" + NewTMPTxt.name);
    }
    #endregion
}
