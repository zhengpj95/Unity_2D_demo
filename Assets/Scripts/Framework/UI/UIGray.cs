using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI 灰度 / 禁用组件。
///
/// 挂载到 UI 父节点后，会处理整个子节点树。
///
/// IsGray: 置灰，但仍然可以点击。
///
/// IsDisable: 置灰，并禁止 UI 交互。
///
/// Image / RawImage:
///     使用 UIGray Shader。
///
/// TMP_Text:
///     保留原 TMP Shader。
///     同时处理 TMP 材质颜色和 TMP 顶点颜色。
/// </summary>
[ExecuteAlways]
public class UIGray : MonoBehaviour
{
  [Header("只置灰，不影响交互")]
  [SerializeField]
  private bool isGray;

  [Header("置灰并禁止交互")]
  [SerializeField]
  private bool isDisable;

  private Graphic[] graphics;

  /// <summary>
  /// 原始材质。
  /// </summary>
  private readonly Dictionary<Graphic, Material> originalMaterials = new();
  /// <summary>
  /// 灰度材质。
  /// </summary>
  private readonly Dictionary<Graphic, Material> grayMaterials = new();
  /// <summary>
  /// 原始 Raycast 状态。
  /// </summary>
  private readonly Dictionary<Graphic, bool> originalRaycastTargets = new();
  /// <summary>
  /// TMP 原始颜色。
  /// </summary>
  private readonly Dictionary<TMP_Text, Color> originalTMPColors = new();
  /// <summary>
  /// Image Shader 的灰度参数。
  /// </summary>
  private static readonly int GrayAmount = Shader.PropertyToID("_GrayAmount");
  /// <summary>
  /// TMP Face Color。
  /// </summary>
  private static readonly int FaceColor = Shader.PropertyToID("_FaceColor");
  /// <summary>
  /// TMP Outline Color。
  /// </summary>
  private static readonly int OutlineColor = Shader.PropertyToID("_OutlineColor");
  /// <summary>
  /// TMP Underlay Color。
  /// </summary>
  private static readonly int UnderlayColor = Shader.PropertyToID("_UnderlayColor");

  public bool IsGray
  {
    get => isGray;
    set
    {
      if (isGray == value)
        return;
      isGray = value;
      Refresh();
    }
  }

  public bool IsDisable
  {
    get => isDisable;
    set
    {
      if (isDisable == value)
        return;
      isDisable = value;
      Refresh();
    }
  }

  private void Awake()
  {
    Initialize();
  }

  private void OnEnable()
  {
    Initialize();
    Refresh();
  }

  private void Initialize()
  {
    if (graphics != null)
      return;
    graphics = GetComponentsInChildren<Graphic>(true);
    foreach (var graphic in graphics)
    {
      if (graphic == null)
        continue;
      originalMaterials[graphic] = GetCurrentMaterial(graphic);
      originalRaycastTargets[graphic] = graphic.raycastTarget;
      /*
       * TMP 原始颜色。
       */
      if (graphic is TMP_Text tmp)
      {
        originalTMPColors[tmp] =
            tmp.color;
      }
    }
  }

  private Material GetCurrentMaterial(Graphic graphic)
  {
    if (graphic is TMP_Text tmp)
    {
      return tmp.fontSharedMaterial;
    }
    return graphic.material;
  }

  private Material GetGrayMaterial(Graphic graphic)
  {
    if (grayMaterials.TryGetValue(graphic, out var material))
    {
      return material;
    }

    if (!originalMaterials.TryGetValue(graphic, out var originalMaterial))
    {
      originalMaterial = GetCurrentMaterial(graphic);
      originalMaterials[graphic] = originalMaterial;
    }

    if (originalMaterial == null)
      return null;

    /*
     * ============================
     * TMP
     * ============================
     */
    if (graphic is TMP_Text)
    {
      material = new Material(originalMaterial);
      material.name = $"{originalMaterial.name}_Gray";
      SetupTMPGrayMaterial(material);
      grayMaterials.Add(graphic, material);
      return material;
    }

    /*
     * ============================
     * Image / RawImage
     * ============================
     */

    var shader = Shader.Find("UI/Grayscale");
    if (shader == null)
    {
      Debug.LogError("UIGray: 找不到 Shader：UI/UIGray", this);
      return null;
    }

    material = new Material(shader);
    material.name = $"{graphic.name}_Gray";

    /*
     * 保留原始纹理。
     */
    if (originalMaterial.HasProperty("_MainTex"))
    {
      material.SetTexture("_MainTex", originalMaterial.GetTexture("_MainTex"));
    }

    /*
     * 保留原始颜色。
     */
    if (originalMaterial.HasProperty("_Color"))
    {
      material.SetColor("_Color", originalMaterial.GetColor("_Color"));
    }

    material.SetFloat(GrayAmount, 1);
    grayMaterials.Add(graphic, material);
    return material;
  }


  /// <summary>
  /// 设置 TMP 灰度材质。
  ///
  /// 注意：
  /// 这里只处理 TMP Material。
  /// TMP 顶点颜色在 ApplyTMPGray 中处理。
  /// </summary>
  private void SetupTMPGrayMaterial(Material material)
  {
    if (material.HasProperty(FaceColor))
    {
      Color color = material.GetColor(FaceColor);
      material.SetColor(FaceColor, ToGray(color));
    }

    if (material.HasProperty(OutlineColor))
    {
      Color color = material.GetColor(OutlineColor);

      material.SetColor(OutlineColor, ToGray(color));
    }

    if (material.HasProperty(UnderlayColor))
    {
      Color color = material.GetColor(UnderlayColor);

      material.SetColor(UnderlayColor, ToGray(color));
    }
  }

  /// <summary>
  /// RGB 转灰度。
  /// </summary>
  private static Color ToGray(Color color)
  {
    float gray =
        color.r * 0.299f +
        color.g * 0.587f +
        color.b * 0.114f;
    return new Color(gray, gray, gray, color.a);
  }

  private void Refresh()
  {
    Initialize();

    bool needGray = isGray || isDisable;


    foreach (var graphic in graphics)
    {
      if (graphic == null)
        continue;


      if (needGray)
      {
        ApplyGray(graphic);
      }
      else
      {
        Restore(graphic);
      }

      ApplyInteractableState(graphic);
    }
  }


  private void ApplyGray(Graphic graphic)
  {
    Material material = GetGrayMaterial(graphic);


    if (material == null)
      return;

    /*
     * TMP
     */
    if (graphic is TMP_Text tmp)
    {
      tmp.fontMaterial = material;
      ApplyTMPGray(tmp);
      return;
    }


    /*
     * Image / RawImage
     */
    graphic.material = material;
  }


  /// <summary>
  /// TMP 颜色置灰。
  ///
  /// 这是解决：
  ///
  /// TMP 原本是红色
  /// ↓
  /// 修改 FaceColor 后仍然红色
  ///
  /// 的关键。
  /// </summary>
  private void ApplyTMPGray(TMP_Text tmp)
  {
    if (!originalTMPColors.TryGetValue(tmp, out var originalColor))
    {
      originalColor = tmp.color;
      originalTMPColors[tmp] = originalColor;
    }
    tmp.color = ToGray(originalColor);
  }


  private void Restore(Graphic graphic)
  {
    if (!originalMaterials.TryGetValue(graphic, out var originalMaterial))
    {
      return;
    }


    /*
     * TMP
     */
    if (graphic is TMP_Text tmp)
    {
      tmp.fontSharedMaterial = originalMaterial;


      if (originalTMPColors.TryGetValue(tmp, out var originalColor))
      {
        tmp.color = originalColor;
      }


      return;
    }

    /*
     * Image / RawImage
     */
    graphic.material = originalMaterial;
  }

  private void ApplyInteractableState(Graphic graphic)
  {
    if (!originalRaycastTargets.TryGetValue(graphic, out var originalRaycast))
    {
      Debug.Log("111111111111");
      return;
    }

    graphic.raycastTarget = isDisable ? false : originalRaycast;
  }


  /// <summary>
  /// 重新扫描整个 UI 节点树。
  ///
  /// 当运行时动态增加子节点时调用。
  /// </summary>
  public void Rebuild()
  {
    RestoreAll();
    ReleaseMaterials();
    graphics = null;
    originalMaterials.Clear();
    originalRaycastTargets.Clear();
    originalTMPColors.Clear();
    grayMaterials.Clear();
    Initialize();
    Refresh();
  }


  private void RestoreAll()
  {
    if (graphics == null)
      return;


    foreach (var graphic in graphics)
    {
      if (graphic == null)
        continue;
      Restore(graphic);
      if (originalRaycastTargets.TryGetValue(graphic, out var raycastTarget))
      {
        graphic.raycastTarget = raycastTarget;
      }
    }
  }


#if UNITY_EDITOR
  private void OnValidate()
  {
    Initialize();

    Refresh();
  }
#endif


  private void OnDestroy()
  {
    ReleaseMaterials();
  }


  private void ReleaseMaterials()
  {
    foreach (var material in grayMaterials.Values)
    {
      if (material == null)
        continue;


#if UNITY_EDITOR

      if (!Application.isPlaying)
      {
        DestroyImmediate(material);
      }
      else
#endif
      {
        Destroy(material);
      }
    }

    grayMaterials.Clear();
  }
}