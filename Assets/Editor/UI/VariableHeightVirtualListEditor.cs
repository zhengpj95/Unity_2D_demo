using UnityEditor;
using UnityEditor.UI;

namespace EditorTools
{
  [CustomEditor(typeof(VariableHeightVirtualList))]
  public class VariableHeightVirtualListEditor : ScrollRectEditor
  {
    private SerializedProperty itemTemplate;
    private SerializedProperty itemTemplates;
    private SerializedProperty spacing;
    private SerializedProperty estimatedItemHeight;
    private SerializedProperty bufferHeight;
    private SerializedProperty clickThreshold;
    private SerializedProperty scrollSpeed;

    protected override void OnEnable()
    {
      base.OnEnable();
      itemTemplate = serializedObject.FindProperty("itemTemplate");
      itemTemplates = serializedObject.FindProperty("itemTemplates");
      spacing = serializedObject.FindProperty("spacing");
      estimatedItemHeight = serializedObject.FindProperty("estimatedItemHeight");
      bufferHeight = serializedObject.FindProperty("bufferHeight");
      clickThreshold = serializedObject.FindProperty("clickThreshold");
      scrollSpeed = serializedObject.FindProperty("scrollSpeed");
    }

    public override void OnInspectorGUI()
    {
      serializedObject.Update();

      EditorGUILayout.PropertyField(itemTemplate);
      EditorGUILayout.PropertyField(itemTemplates, true);
      EditorGUILayout.Space();
      EditorGUILayout.PropertyField(spacing);
      EditorGUILayout.PropertyField(estimatedItemHeight);
      EditorGUILayout.PropertyField(bufferHeight);
      EditorGUILayout.Space();
      EditorGUILayout.PropertyField(clickThreshold);
      EditorGUILayout.PropertyField(scrollSpeed);

      serializedObject.ApplyModifiedProperties();
      EditorGUILayout.Space(10);
      base.OnInspectorGUI();
    }
  }
}
