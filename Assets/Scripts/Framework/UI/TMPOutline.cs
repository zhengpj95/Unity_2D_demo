using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// TMP文字描边组件
/// 自动缓存材质，避免重复创建Material
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(TMP_Text))]
[ExecuteAlways]
public class TMPOutline : MonoBehaviour
{
  [Header("描边颜色")]
  [SerializeField]
  private Color outlineColor = Color.black;

  [Header("描边宽度")]
  [Range(0, 0.5f)]
  [SerializeField]
  private float outlineWidth = 0.2f;

  private TMP_Text tmpText;
  private Material originMaterial;
#if UNITY_EDITOR
  private Material previewMaterial;
#endif

  private void Awake()
  {
    tmpText = GetComponent<TMP_Text>();
  }

  private void OnEnable()
  {
    if (tmpText == null)
      tmpText = GetComponent<TMP_Text>();

#if UNITY_EDITOR
    if (!Application.isPlaying)
    {
      ApplyEditorPreview();
      return;
    }
#endif

    ApplyOutline();
  }

  private void OnDisable()
  {
#if UNITY_EDITOR
    ClearEditorPreview();
#endif
  }

#if UNITY_EDITOR
  private void OnValidate()
  {
    if (tmpText == null)
      tmpText = GetComponent<TMP_Text>();

    if (Application.isPlaying)
      ApplyOutline();
    else if (isActiveAndEnabled)
      ApplyEditorPreview();
  }

  /// <summary>
  /// 使用不保存到场景的临时材质提供编辑器实时预览。
  /// </summary>
  private void ApplyEditorPreview()
  {
    if (tmpText == null)
      return;

    if (previewMaterial == null)
    {
      originMaterial = GetSourceMaterial();
      if (originMaterial == null)
        return;

      previewMaterial = new Material(originMaterial)
      {
        name = $"{originMaterial.name}_OutlinePreview",
        hideFlags = HideFlags.HideAndDontSave
      };
    }

    previewMaterial.SetColor(ShaderUtilities.ID_OutlineColor, outlineColor);
    previewMaterial.SetFloat(ShaderUtilities.ID_OutlineWidth, outlineWidth);
    tmpText.fontSharedMaterial = previewMaterial;
    tmpText.SetMaterialDirty();
  }

  private void ClearEditorPreview()
  {
    if (previewMaterial == null)
      return;

    if (tmpText != null && tmpText.fontSharedMaterial == previewMaterial)
    {
      tmpText.fontSharedMaterial = originMaterial;
      tmpText.SetMaterialDirty();
    }

    DestroyImmediate(previewMaterial);
    previewMaterial = null;
    originMaterial = null;
  }
#endif


  /// <summary>
  /// 应用描边
  /// </summary>
  private void ApplyOutline()
  {
    if (tmpText == null)
      return;

    if (originMaterial == null)
      originMaterial = GetSourceMaterial();

    if (originMaterial == null)
    {
      Debug.LogError("[TMPOutline] 无法应用描边：TMP_Text 没有可用的字体材质。", this);
      return;
    }

    Material material = TMPOutlineMaterialCache.Get(
      originMaterial,
      outlineColor,
      outlineWidth
    );

    tmpText.fontSharedMaterial = material;
    tmpText.SetMaterialDirty();
  }

  /// <summary>
  /// 获取有效材质。编辑器临时材质在进入播放模式时可能已被销毁，
  /// 此时回退到字体资源的默认材质。
  /// </summary>
  private Material GetSourceMaterial()
  {
    Material material = tmpText.fontSharedMaterial;
    if (material != null)
      return material;

    return tmpText.font != null ? tmpText.font.material : null;
  }

  public void SetOutlineColor(Color color)
  {
    outlineColor = color;
    ApplyOutline();
  }


  public void SetOutlineWidth(float width)
  {
    outlineWidth = Mathf.Clamp01(width);
    ApplyOutline();
  }
}


/// <summary>
/// TMP描边材质缓存
/// </summary>
public static class TMPOutlineMaterialCache
{
  private struct Key
  {
    public int materialId;
    public Color color;
    public float width;

    public override int GetHashCode()
    {
      unchecked
      {
        int hash = materialId;
        hash = hash * 31 + color.GetHashCode();
        hash = hash * 31 + width.GetHashCode();
        return hash;
      }
    }

    public override bool Equals(object obj)
    {
      if (!(obj is Key))
        return false;
      Key other = (Key)obj;
      return materialId == other.materialId
             && color.Equals(other.color)
             && width.Equals(other.width);
    }
  }

  private static Dictionary<Key, Material> cache = new Dictionary<Key, Material>();

  public static Material Get(Material source, Color color, float width)
  {
    if (source == null)
      return null;

    Key key = new Key()
    {
      materialId = source.GetInstanceID(),
      color = color,
      width = width
    };

    if (cache.TryGetValue(key, out Material material))
    {
      return material;
    }

    material = new Material(source);
    material.name = $"{source.name}_Outline";
    material.SetColor(ShaderUtilities.ID_OutlineColor, color);
    material.SetFloat(ShaderUtilities.ID_OutlineWidth, width);

    cache.Add(key, material);
    return material;
  }

  /// <summary>
  /// 清理缓存
  /// </summary>
  public static void Clear()
  {
    foreach (var item in cache)
    {
      Object.Destroy(item.Value);
    }
    cache.Clear();
  }
}
