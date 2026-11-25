using UnityEditor;
using UnityEngine;

namespace EditorTools
{
  /// <summary>
  /// AnimFrameConfig 的只读首帧预览：在 Inspector 顶部显示默认 Clip（无默认则第一组有效 Clip）的第一张 Sprite。
  /// 只辅助查看资源内容，不修改配置数据。
  /// </summary>
  [CustomEditor(typeof(AnimFrameConfig))]
  public sealed class AnimFrameConfigInspector : Editor
  {
    public override void OnInspectorGUI()
    {
      AnimFramePreviewDrawer.Draw(target as AnimFrameConfig);
      EditorGUILayout.Space(8f);
      DrawDefaultInspector();
    }
  }

  /// <summary>
  /// 序列帧首帧预览的共用绘制逻辑，供直接选中 AnimFrameConfig 或通过其他配置间接查看时使用。
  /// </summary>
  internal static class AnimFramePreviewDrawer
  {
    private const float PreviewHeight = 120f;

    /// <summary>
    /// 绘制首帧资源字段、来源信息、缩略图与 Project 定位按钮。
    /// </summary>
    /// <param name="config">提供帧数据的 AnimFrameConfig，可为空。</param>
    /// <param name="title">预览区标题。</param>
    /// <param name="emptyMessage">没有有效帧时显示的提示。</param>
    internal static void Draw(
      AnimFrameConfig config,
      string title = "首帧预览",
      string emptyMessage = "没有可用首帧：请给 Clip 拖入 Sprite，或检查默认 Clip 名称是否有效。")
    {
      EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
      Sprite sprite = FindPreviewSprite(config, out string clipName, out int frameIndex);
      if (sprite == null || sprite.texture == null)
      {
        EditorGUILayout.HelpBox(emptyMessage, MessageType.None);
        return;
      }

      using (new EditorGUILayout.HorizontalScope())
      {
        using (new EditorGUI.DisabledScope(true))
        {
          EditorGUILayout.ObjectField("首帧资源", sprite, typeof(Sprite), false);
        }

        if (GUILayout.Button("定位", GUILayout.Width(56f)))
        {
          EditorGUIUtility.PingObject(sprite);
          Selection.activeObject = sprite;
        }
      }

      string source = string.IsNullOrEmpty(clipName) ? "未知 Clip" : clipName;
      EditorGUILayout.LabelField($"来源：{source} [{frameIndex}]", EditorStyles.miniLabel);

      float previewWidth = Mathf.Max(100f, EditorGUIUtility.currentViewWidth - 40f);
      Rect area = GUILayoutUtility.GetRect(previewWidth, PreviewHeight, GUILayout.ExpandWidth(true));
      EditorGUI.DrawRect(area, new Color(0.12f, 0.12f, 0.12f, 1f));
      GUI.DrawTextureWithTexCoords(GetFittedRect(area, sprite.rect.width, sprite.rect.height), sprite.texture, GetUvRect(sprite));
    }

    /// <summary>优先取默认 Clip 的第一张有效帧；默认无效时取第一组有效 Clip 的首帧。</summary>
    private static Sprite FindPreviewSprite(AnimFrameConfig config, out string clipName, out int frameIndex)
    {
      clipName = string.Empty;
      frameIndex = -1;
      if (config == null || config.Clips == null)
      {
        return null;
      }

      if (!string.IsNullOrEmpty(config.DefaultClipName))
      {
        for (int i = 0; i < config.Clips.Length; i++)
        {
          AnimFrameClip clip = config.Clips[i];
          if (clip == null || clip.AnimName != config.DefaultClipName)
          {
            continue;
          }

          Sprite sprite = FindFirstValidFrame(clip, out frameIndex);
          if (sprite != null)
          {
            clipName = clip.AnimName;
            return sprite;
          }
        }
      }

      for (int i = 0; i < config.Clips.Length; i++)
      {
        AnimFrameClip clip = config.Clips[i];
        if (clip == null)
        {
          continue;
        }

        Sprite sprite = FindFirstValidFrame(clip, out frameIndex);
        if (sprite != null)
        {
          clipName = clip.AnimName;
          return sprite;
        }
      }

      return null;
    }

    /// <summary>返回 Clip 中第一张非空 Sprite，并记录它在 frames 数组中的下标。</summary>
    private static Sprite FindFirstValidFrame(AnimFrameClip clip, out int frameIndex)
    {
      frameIndex = -1;
      for (int i = 0; i < clip.FrameCount; i++)
      {
        Sprite sprite = clip.GetFrame(i);
        if (sprite != null && sprite.texture != null)
        {
          frameIndex = i;
          return sprite;
        }
      }

      return null;
    }

    /// <summary>按 Sprite 原始宽高比缩放预览区域，避免拉伸变形。</summary>
    private static Rect GetFittedRect(Rect area, float width, float height)
    {
      if (width <= 0f || height <= 0f)
      {
        return area;
      }

      float scale = Mathf.Min(area.width / width, area.height / height) * 0.95f;
      float drawWidth = width * scale;
      float drawHeight = height * scale;
      return new Rect(area.center.x - drawWidth * 0.5f, area.center.y - drawHeight * 0.5f, drawWidth, drawHeight);
    }

    /// <summary>把 Sprite 在图集中的 rect 转换成 GUI.DrawTextureWithTexCoords 使用的 UV。</summary>
    private static Rect GetUvRect(Sprite sprite)
    {
      Texture2D texture = sprite.texture;
      Rect rect = sprite.rect;
      return new Rect(
        rect.x / texture.width,
        1f - (rect.y + rect.height) / texture.height,
        rect.width / texture.width,
        rect.height / texture.height);
    }
  }
}
