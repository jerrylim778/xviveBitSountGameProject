using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Commons.Editor.EditorCommons.Helpers;
using Commons.Helpers.Attribute;

#region PropertyReadOnlyDrawer Reference
[CustomPropertyDrawer(typeof(Lim_InspectorReadOnly))]
public class PropertyReadOnlyDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        GUI.enabled = false;  // 읽기 전용으로 만들기 위해 GUI 비활성화
        //if (property.isArray && property.propertyType != SerializedPropertyType.String)
        //DrawArray(position, property, label); // 배열의 길이 부분을 그리지 않도록 별도의 처리
        //else EditorGUI.PropertyField(position, property, label, true); // 기본 프로퍼티 필드를 그린다
        EditorGUI.PropertyField(position, property, label);
        GUI.enabled = true;  // 원래 상태로 복원
    }

    #region 배열의 길이도 막기 위한 방식중 하나 (적용이 안되어 현재는 사용하지 않음)
    private void DrawArray(Rect position, SerializedProperty property, GUIContent label)
    {
        // 배열 필드를 기본 방식으로 그린다
        EditorGUI.PropertyField(position, property, label, true);

        // 배열의 각 요소를 읽기 전용으로 그린다
        for (int i = 0; i < property.arraySize; i++)
        {
            SerializedProperty element = property.GetArrayElementAtIndex(i);
            Rect elementPosition = new Rect(position.x, position.y + 
            EditorGUIUtility.singleLineHeight * (i + 1), position.width, EditorGUIUtility.singleLineHeight);
            EditorGUI.PropertyField(elementPosition, element, GUIContent.none, true);
        }
    }

    // 기본 높이를 사용하되, 배열의 경우 각 요소의 높이도 포함시킨다
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    => property.isArray && property.propertyType != SerializedPropertyType.String?
    EditorGUI.GetPropertyHeight(property, label, true) + property.arraySize * EditorGUIUtility.singleLineHeight :
    EditorGUI.GetPropertyHeight(property, label, true);
    #endregion
}
#endregion

#region SpritePriview Reference
[CustomPropertyDrawer(typeof(Lim_SpritePriviewAttribute))]
public class SpritePriviewDrower : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        //속성타입이 Sprite가 아니거나 다른것일경우 표시후 리턴
        if (property.propertyType != SerializedPropertyType.ObjectReference ||
        property.objectReferenceValue == null || !(property.objectReferenceValue is Sprite))
        {
            EditorGUI.PropertyField(position, property, label);
            return;
        }
        Lim_SpritePriviewAttribute GetAttribute = attribute as Lim_SpritePriviewAttribute;
        Sprite GetOutPutSpr = property.objectReferenceValue as Sprite;
        Rect labelRect = new Rect(position.x, position.y + GetAttribute.spp_Hight, position.width, EditorGUIUtility.singleLineHeight);
        EditorGUI.PropertyField(labelRect, property, label);
        //Rect labelRect_2 = new Rect(position.x, position.y, EditorGUIUtility.labelWidth, position.height);
        //EditorGUI.LabelField(labelRect_2, label); //그냥 표시만 원할경우 선택

        Texture2D m_ApplyTex = GetOutPutSpr.texture;
        Rect spriteRect = new Rect(position.x + EditorGUIUtility.labelWidth,  // Sprite 렌더링을 위한 Rect 설정
        position.y + EditorGUIUtility.singleLineHeight - 18, GetAttribute.spp_Wigth, GetAttribute.spp_Hight);
        EditorGUI.DrawPreviewTexture(spriteRect, m_ApplyTex); // Sprite 크기 조절 및 렌더링
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label) //속성 높이 설정
    {
        Lim_SpritePriviewAttribute SetNewAttribute = attribute as Lim_SpritePriviewAttribute;
        //기본 높이 + 추가 높이(Sprite 미리보기를 위한 공간)
        return EditorGUIUtility.singleLineHeight + (property.objectReferenceValue is Sprite ? SetNewAttribute.spp_Hight : 0);
        //return Mathf.Max(EditorGUIUtility.singleLineHeight ,SetNewAttribute.m_Hight); //사진만 출력하고 싶을때 사용
    }
}
#endregion

#region FoldoutDrawer Reference
// Foldout을 위한 Rect를 만듭니다.
//EditorGUILayout.FoldOut()함수와는 프러퍼티 정의 특성상 정확한 Rect를 삽입하고 않하고의 차이다
//따라서 아래 EditorGUI.Foldout()함수가 해당 정의에서 적절한 방법이다
[CustomPropertyDrawer(typeof(Lim_FoldoutAttribute))]
public class FoldoutDrawer : PropertyDrawer
{
    private bool m_isFoldOut = false;

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        Lim_FoldoutAttribute FoldOutAttribute = attribute as Lim_FoldoutAttribute;

        Rect foldoutRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
        m_isFoldOut = EditorGUI.Foldout(foldoutRect, m_isFoldOut, FoldOutAttribute.ssp_SetStr, FoldOutAttribute.ssp_isReadOnlyToogle);

        //해당 방식을 진행하는 이유는 FoldOut이 참이 되었을때 나타나는 객층구조를 더 구조화할 수 있도록 하기위해 진행한다
        if (m_isFoldOut)
        {
            EditorGUI.indentLevel++;
            #region 자식 요소들을 위한 PropertyField 그리기에서 프로퍼티 창에 따른 다양한 커스텀 시도에 관한것(사용 안함)
            //position.y += EditorGUIUtility.singleLineHeight;
            //SerializedProperty iterator = property.Copy();
            //SerializedProperty endProperty = iterator.GetEndProperty();

            //while (iterator.NextVisible(true) && !SerializedProperty.EqualContents(iterator, endProperty))
            //{
            //    position.height = EditorGUI.GetPropertyHeight(iterator, true);
            //    EditorGUI.PropertyField(position, iterator, true);
            //    position.y += position.height + EditorGUIUtility.standardVerticalSpacing;
            //}
            #endregion
            EditorGUI.PropertyField(position, property, label, true);
            EditorGUI.indentLevel--;
        }
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    => !m_isFoldOut ? EditorGUIUtility.singleLineHeight :
    EditorGUI.GetPropertyHeight(property, label, true) + EditorGUIUtility.singleLineHeight;
    #region 자식 요소들을 위한 PropertyField 그리기에서 프로퍼티 창에 따른 다양한 커스텀 시도에 관한것(사용 안함)
    //{
    //    if (m_isFoldOut)
    //    {
    //        float totalHeight = EditorGUIUtility.singleLineHeight;
    //        SerializedProperty iterator = property.Copy();
    //        SerializedProperty endProperty = iterator.GetEndProperty();

    //        while (iterator.NextVisible(true) && !SerializedProperty.EqualContents(iterator, endProperty))
    //        {
    //            totalHeight += EditorGUI.GetPropertyHeight(iterator, true) + EditorGUIUtility.standardVerticalSpacing;
    //        }
    //        return totalHeight;
    //    }
    //    else return EditorGUIUtility.singleLineHeight;
    //}
    #endregion
}
#endregion

#region FoldoutDrawerArray Reference (시도 했으나 적용이 이상해 실패 => 오딘을 활용할 수 있도록 한다)

//[CustomPropertyDrawer(typeof(Lim_FoldoutArrayAttribute))]
//public class FoldoutDrawerArray : PropertyDrawer
//{
//    private bool m_isFoldOut = false;

//    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
//    {
//        Lim_FoldoutArrayAttribute foldoutAttribute = attribute as Lim_FoldoutArrayAttribute;

//        // Foldout을 위한 Rect를 만듭니다.
//        Rect foldoutRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
//        m_isFoldOut = EditorGUI.Foldout(foldoutRect, m_isFoldOut, foldoutAttribute.ssp_SetStr, foldoutAttribute.ssp_isReadOnlyToogle);

//        if (m_isFoldOut)
//        {
//            EditorGUI.indentLevel++;

//            // Foldout이 펼쳐졌을 때의 자식 요소 Rect 시작점 설정
//            position.y += EditorGUIUtility.singleLineHeight;

//            // 자식 요소들을 위한 PropertyField 그리기
//            SerializedProperty Iterator = property.Copy();
//            SerializedProperty EndProperty = Iterator.GetEndProperty();

//            Iterator.NextVisible(true); // Move to first child

//            while (Iterator.NextVisible(false) && !SerializedProperty.EqualContents(Iterator, EndProperty))
//            {
//                bool Exclude = false;
//                Lim_FoldoutArrayAttribute FoldOutAttribute = (Lim_FoldoutArrayAttribute)attribute;

//                foreach (string excludeProperty in FoldOutAttribute.ssp_ExcludeProperties)
//                {
//                    if (Iterator.name == excludeProperty)
//                    {
//                        Exclude = true;
//                        break;
//                    }
//                }

//                if (!Exclude)
//                {
//                    position.height = EditorGUI.GetPropertyHeight(Iterator, true);
//                    EditorGUI.PropertyField(position, Iterator, true);
//                    position.y += position.height + EditorGUIUtility.standardVerticalSpacing;
//                }

//            }

//            EditorGUI.indentLevel--;
//        }
//    }

//    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
//    {
//        if (m_isFoldOut)
//        {
//            float TotalHeight = EditorGUIUtility.singleLineHeight;
//            SerializedProperty Iterator = property.Copy();
//            SerializedProperty EndProperty = Iterator.GetEndProperty();

//            Iterator.NextVisible(true); // Move to first child

//            while (Iterator.NextVisible(false) && !SerializedProperty.EqualContents(Iterator, EndProperty))
//            {
//                bool Exclude = false;
//                Lim_FoldoutArrayAttribute FoldOutAttribute = (Lim_FoldoutArrayAttribute)attribute;

//                foreach (string excludeProperty in FoldOutAttribute.ssp_ExcludeProperties)
//                {
//                    if (Iterator.name == excludeProperty)
//                    {
//                        Exclude = true;
//                        break;
//                    }
//                }

//                if (!Exclude)
//                    TotalHeight += EditorGUI.GetPropertyHeight(Iterator, true) + EditorGUIUtility.standardVerticalSpacing;
//            }

//            return TotalHeight;
//        }


//        return EditorGUIUtility.singleLineHeight;
//    }
//}

#endregion

#region 사용하는 방법에 대한것 참고후 삭제 요망..
//[System.Serializable]
//public class Test_ItemInfo
//{
//    [Commons.Helpers.Attribute.Lim_SpritePriviewAttribute(100, 100)]
//    public Sprite m_GetOutPutSpr;
//    [Commons.Helpers.Attribute.Lim_SpritePriviewAttribute(100, 100)]
//    public Sprite[] m_GetOutPutSprs;
//}
#endregion
