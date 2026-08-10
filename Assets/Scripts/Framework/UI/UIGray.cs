using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using Scene = UnityEngine.SceneManagement.Scene;
#endif

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
    // 进入 Play 前，ExitingEditMode 已还原编辑器灰度预览（见上方编辑器区），
    // 此时 graphics 材质是真正的原始材质，可直接捕获。
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

#if UNITY_EDITOR
  /// <summary>
  /// 编辑模式预览生命周期管理。
  ///
  /// 核心问题：本组件是 [ExecuteAlways]，Inspector 勾选/取消 isGray 时会通过 OnValidate 直接改场景里 graphic 的材质。
  /// 如果带着预览进 Play，Awake 捕获到的"原始材质"其实是灰度材质，之后取消勾选永远还原不了（灰度 == 灰度，Restore 变成 no-op）。
  ///
  /// 解决：
  ///   1. ExitingEditMode：进入 Play 前还原所有实例的灰度预览，保证 Awake 捕获到真实原始材质；
  ///   2. EnteredEditMode：退出 Play 后，序列化值回滚到进入前的快照，按 isGray/isDisable 重新应用灰度预览，
  ///      保持 Inspector 勾选与场景显示一致。
  ///
  /// 注意：Play 模式中修改的 isGray 会被 Unity 在退出时丢弃，因此退出后 isGray 就是进入前的状态，
  /// 直接用它恢复预览即可，无需额外记录运行时状态。
  /// </summary>
  static UIGray()
  {
    EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    EditorSceneManager.sceneSaving += OnSceneSaving;
  }

  /// <summary>
  /// 场景保存前：还原所有实例的灰度预览。
  ///
  /// 防止退出 Play 后重新应用的灰度预览被 Ctrl+S 写进场景文件，
  /// 否则下次打开场景时，灰度材质会变成"原始材质"，取消勾选又无法还原（同样的根因）。
  /// 保存完成后通过 delayCall 重新应用预览，避免破坏用户看到的勾选状态。
  /// </summary>
  private static void OnSceneSaving(Scene scene, string path)
  {
    RestoreAllInstances();
    EditorApplication.delayCall += ApplyGrayToCheckedInstances;
  }

  private static void OnPlayModeStateChanged(PlayModeStateChange state)
  {
    switch (state)
    {
      case PlayModeStateChange.ExitingEditMode:
        RestoreAllInstances();
        break;

      case PlayModeStateChange.EnteredEditMode:
        ApplyGrayToCheckedInstances();
        break;
    }
  }

  /// <summary>
  /// 进入 Play 前：还原所有已初始化的 UIGray 实例，让场景序列化快照持有原始材质。
  /// 未初始化（graphics == null）的实例没有应用过预览，无需处理。
  /// </summary>
  private static void RestoreAllInstances()
  {
    foreach (var instance in FindObjectsOfType<UIGray>())
    {
      if (instance == null || instance.graphics == null)
        continue;
      instance.RestoreAll();
    }
  }

  /// <summary>
  /// 退出 Play 后：对勾选了 isGray/isDisable 的实例重新应用灰度预览。
  /// Refresh 内部会先 Initialize（捕获原始材质），再应用灰度，幂等安全。
  /// </summary>
  private static void ApplyGrayToCheckedInstances()
  {
    foreach (var instance in FindObjectsOfType<UIGray>())
    {
      if (instance == null)
        continue;
      if (instance.isGray || instance.isDisable)
        instance.Refresh();
    }
  }

  /// <summary>
  /// 一键恢复被污染的灰度材质。
  ///
  /// 用于修复修复前的历史版本已写入场景的灰度材质（原材质已被灰度材质覆盖，无法自动还原）。
  /// 判定依据：material 使用的 Shader 是 "UI/UIGrayscale"，则必为 UIGray 写入的污染。
  ///   - TMP_Text：原始材质从字体资源恢复（tmp.font.material，即字体默认材质）；
  ///   - Image/RawImage：重置为 null（Unity UI 默认材质）。
  ///
  /// 注意：
  ///   - 若你的项目有自定义材质恰好也使用 UI/UIGrayscale Shader，会被一并重置，请慎用；
  ///   - 若 TMP 原本使用了自定义共享材质，此处只能回退到字体默认材质，无法完全复原。
  /// </summary>
  [MenuItem("Tools/UIGray/恢复被污染的灰度材质")]
  private static void RecoverPollutedMaterials()
  {
    int recovered = 0;

    foreach (var instance in FindObjectsOfType<UIGray>())
    {
      if (instance == null)
        continue;

      instance.graphics = instance.GetComponentsInChildren<Graphic>(true);
      instance.originalMaterials.Clear();
      instance.originalTMPColors.Clear();

      foreach (var graphic in instance.graphics)
      {
        if (graphic == null)
          continue;

        Material current = instance.GetCurrentMaterial(graphic);
        if (current == null || current.shader == null || current.shader.name != "UI/UIGrayscale")
        {
          // 非灰度材质：视为原始材质，重新捕获。
          instance.originalMaterials[graphic] = current;
          continue;
        }

        // 灰度污染：恢复原始材质。
        if (graphic is TMP_Text tmp)
        {
          Material original = tmp.font != null ? tmp.font.material : null;
          instance.originalMaterials[graphic] = original;
          if (original != null && !ReferenceEquals(tmp.fontSharedMaterial, original))
          {
            tmp.fontSharedMaterial = original;
            EditorUtility.SetDirty(tmp);
          }
        }
        else
        {
          instance.originalMaterials[graphic] = null;
          graphic.material = null; // null => Unity UI 默认材质
          EditorUtility.SetDirty(graphic);
        }
        recovered++;
      }

      // 恢复后取消勾选，避免再次应用灰度。
      instance.isGray = false;
      instance.isDisable = false;
      instance.graphics = null;
      instance.Initialize();

      EditorUtility.SetDirty(instance);
    }

    Debug.Log($"UIGray: 恢复完成，修复了 {recovered} 个被污染的 Graphic。");
  }
#endif

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

    graphic.raycastTarget = !isDisable && originalRaycast;
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
