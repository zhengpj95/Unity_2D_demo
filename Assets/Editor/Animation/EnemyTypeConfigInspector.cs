using UnityEditor;
using VampireSurvivorsLike;

namespace EditorTools
{
  /// <summary>
  /// EnemyTypeConfig 的只读动画首帧预览：展示其 AnimConfig 引用的首帧 Sprite，方便快速确认敌人外观来源。
  /// 只辅助查看资源内容，不修改配置数据。
  /// </summary>
  [CustomEditor(typeof(EnemyTypeConfig))]
  public sealed class EnemyTypeConfigInspector : Editor
  {
    public override void OnInspectorGUI()
    {
      EnemyTypeConfig config = target as EnemyTypeConfig;
      AnimFramePreviewDrawer.Draw(
        config == null ? null : config.AnimConfig,
        "动画首帧预览（AnimConfig）",
        "未指定 AnimConfig，或引用的 AnimFrameConfig 中没有有效帧。");

      EditorGUILayout.Space(8f);
      DrawDefaultInspector();
    }
  }
}
