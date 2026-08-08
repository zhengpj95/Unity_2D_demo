using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// TMP文字描边组件
/// 自动缓存材质，避免重复创建Material
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(TMP_Text))]
public class TMPOutline : MonoBehaviour
{
  [Header("描边颜色")]
  [SerializeField]
  private Color outlineColor = Color.black;

  [Header("描边宽度")]
  [Range(0, 0.2f)]
  [SerializeField]
  private float outlineWidth = 0.1f;

  private TMP_Text tmpText;
  private Material originMaterial;

  private void Awake()
  {
    tmpText = GetComponent<TMP_Text>();
    ApplyOutline();
  }


#if UNITY_EDITOR
  private void OnValidate()
  {
    if (tmpText == null)
      tmpText = GetComponent<TMP_Text>();

    if (Application.isPlaying)
    {
      ApplyOutline();
    }
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
    {
      originMaterial = tmpText.fontMaterial;
    }

    Material material = TMPOutlineMaterialCache.Get(
      originMaterial,
      outlineColor,
      outlineWidth
    );

    tmpText.fontMaterial = material;
    tmpText.SetMaterialDirty();
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