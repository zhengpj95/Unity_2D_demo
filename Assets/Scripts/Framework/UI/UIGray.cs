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
[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
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
  // 共享 TMP 灰度材质：按"原始共享材质"缓存，全局复用。
  // 注意：不随单个实例销毁，避免其它实例引用到已销毁的材质（见 GetGrayMaterial 的兜底判断）。
  private static readonly Dictionary<Material, Material> s_tmpGrayMaterials = new();
  // 共享 Image/RawImage 灰度材质：_MainTex 是 [PerRendererData]，纹理由 CanvasRenderer 按 graphic 注入。
  private static Material s_grayMaterial;

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

  private void OnDisable()
  {
    // 组件被禁用时还原 UI 状态（材质与 Raycast），与 OnEnable 对称。
    if (graphics == null)
      return;
    RestoreAll();
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
        originalTMPColors[tmp] = tmp.color;
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
      // 按"原始共享材质"缓存灰度材质，全局复用，避免重复创建。
      if (s_tmpGrayMaterials.TryGetValue(originalMaterial, out var gray))
      {
        // 兜底：缓存可能持有已被销毁的材质（如 AssetBundle 卸载）。
        if (gray == null)
        {
          s_tmpGrayMaterials.Remove(originalMaterial);
        }
        else
        {
          return gray;
        }
      }

      var grayMaterial = new Material(originalMaterial)
      {
        name = $"{originalMaterial.name}_Gray",
      };
      SetupTMPGrayMaterial(grayMaterial);
      s_tmpGrayMaterials.Add(originalMaterial, grayMaterial);
      return grayMaterial;
    }

    /*
     * ============================
     * Image / RawImage
     * ============================
     */

    // 所有 Image / RawImage 共享同一个灰度材质，不按 graphic 克隆：
    // 1. _MainTex 声明为 [PerRendererData]，纹理由 CanvasRenderer 按 graphic 注入，无需拷贝；
    // 2. 颜色 tint 走顶点色（IN.color * _Color），_Color 保持默认白色即可；
    // 3. 材质从 N 份降到 1 份，且不破坏合批。
    if (s_grayMaterial == null)
    {
      var shader = Shader.Find("UI/UIGrayscale");
      if (shader == null)
      {
        Debug.LogError("UIGray: 找不到 Shader：UI/UIGrayscale", this);
        return null;
      }
      s_grayMaterial = new Material(shader);
      s_grayMaterial.SetFloat(GrayAmount, 1);
    }

    return s_grayMaterial;
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
    // Color.grayscale 权重与 Shader 中的 0.299 / 0.587 / 0.114 一致。
    float gray = color.grayscale;
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
      // 统一用 fontSharedMaterial，避免 fontMaterial 触发材质实例化。
      if (!ReferenceEquals(tmp.fontSharedMaterial, material))
        tmp.fontSharedMaterial = material;
      ApplyTMPGray(tmp);
      return;
    }

    /*
     * Image / RawImage
     */
    if (!ReferenceEquals(graphic.material, material))
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
    Color grayColor = ToGray(originalColor);
    if (tmp.color != grayColor)
      tmp.color = grayColor;
  }

  private void Restore(Graphic graphic)
  {
    if (!originalMaterials.TryGetValue(graphic, out var originalMaterial))
    {
      return;
    }

    if (originalMaterial == null)
      return;

    /*
     * TMP
     */
    if (graphic is TMP_Text tmp)
    {
      if (!ReferenceEquals(tmp.fontSharedMaterial, originalMaterial))
        tmp.fontSharedMaterial = originalMaterial;

      if (originalTMPColors.TryGetValue(tmp, out var originalColor))
      {
        if (tmp.color != originalColor)
          tmp.color = originalColor;
      }

      return;
    }

    /*
     * Image / RawImage
     */
    if (!ReferenceEquals(graphic.material, originalMaterial))
      graphic.material = originalMaterial;
  }

  private void ApplyInteractableState(Graphic graphic)
  {
    if (!originalRaycastTargets.TryGetValue(graphic, out var originalRaycast))
    {
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
    graphics = null;
    originalMaterials.Clear();
    originalRaycastTargets.Clear();
    originalTMPColors.Clear();
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

  /// <summary>
  /// 进入运行时前重置静态缓存。
  ///
  /// 编辑器启用了 "Enter Play Mode Options (Disable Domain Reload)" 时，
  /// 静态字段会跨会话保留，需要手动清空，避免持有旧材质。
  /// </summary>
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
  private static void ResetStatics()
  {
    s_tmpGrayMaterials.Clear();
    s_grayMaterial = null;
  }
}
