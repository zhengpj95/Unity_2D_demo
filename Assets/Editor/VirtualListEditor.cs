using UnityEditor;
using UnityEditor.UI;
using UnityEngine;

// namespace EditorTools
// {
//   [CustomEditor(typeof(VirtualList))]
//   [CanEditMultipleObjects]
//   public class VirtualListEditor : Editor
//   {
//     public override void OnInspectorGUI()
//     {
//       serializedObject.Update();

//       DrawPropertiesExcluding(
//         serializedObject,
//         new[]
//         {
//         "m_HorizontalScrollbarVisibility",
//         "m_VerticalScrollbarVisibility",
//         "m_HorizontalScrollbarSpacing",
//         "m_VerticalScrollbarSpacing"
//         });

//       serializedObject.ApplyModifiedProperties();
//     }
//   }
// }

namespace EditorTools
{
  [CustomEditor(typeof(VirtualListEx))]
  public class VirtualListEditor : ScrollRectEditor
  {
    SerializedProperty itemTemplate;
    SerializedProperty layoutType;
    SerializedProperty spaceX;
    SerializedProperty spaceY;
    SerializedProperty repeatX;
    SerializedProperty repeatY;

    protected override void OnEnable()
    {
      base.OnEnable();
      itemTemplate = serializedObject.FindProperty("itemTemplate");
      layoutType = serializedObject.FindProperty("layoutType");
      spaceX = serializedObject.FindProperty("spaceX");
      spaceY = serializedObject.FindProperty("spaceY");
      repeatX = serializedObject.FindProperty("repeatX");
      repeatY = serializedObject.FindProperty("repeatY");
    }

    public override void OnInspectorGUI()
    {
      serializedObject.Update();
      // EditorGUILayout.LabelField("VirtualList Settings", EditorStyles.boldLabel);
      EditorGUILayout.PropertyField(itemTemplate);
      EditorGUILayout.PropertyField(layoutType);
      EditorGUILayout.PropertyField(spaceX);
      EditorGUILayout.PropertyField(spaceY);
      EditorGUILayout.PropertyField(repeatX);
      EditorGUILayout.PropertyField(repeatY);

      serializedObject.ApplyModifiedProperties();
      EditorGUILayout.Space(10);
      base.OnInspectorGUI();
    }
  }
}