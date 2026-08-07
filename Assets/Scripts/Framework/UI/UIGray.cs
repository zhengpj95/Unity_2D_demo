using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIGray : MonoBehaviour
{
  // 静态全局材质池（保证所有置灰 UI 共享同一个材质，不破坏 Canvas Batching）
  private static Material s_UIGrayMaterial;
  private static Material s_TMPGrayMaterial;

  [Header("是否初始变灰")]
  [SerializeField] private bool _isGray = false;

  private Graphic[] _graphics;

  private void Awake()
  {
    InitMaterials();
    _graphics = GetComponentsInChildren<Graphic>(true);
  }

  private void Start()
  {
    if (_isGray)
    {
      SetGray(true);
    }
  }

  private static void InitMaterials()
  {
    if (s_UIGrayMaterial == null)
    {
      // 加载或创建普通 UI 置灰材质
      Shader uiShader = Shader.Find("UI/Grayscale");
      if (uiShader != null) s_UIGrayMaterial = new Material(uiShader);
    }

    if (s_TMPGrayMaterial == null)
    {
      // 加载或创建 TMP 置灰材质 (使用自定义的 TMP Grayscale Shader)
      Shader tmpShader = Shader.Find("TextMeshPro/Distance Field Grayscale");
      if (tmpShader != null) s_TMPGrayMaterial = new Material(tmpShader);
    }
  }

  /// <summary>
  /// 一键控制该节点及其所有子节点的置灰状态
  /// </summary>
  public void SetGray(bool isGray)
  {
    _isGray = isGray;
    if (_graphics == null) return;

    foreach (var graphic in _graphics)
    {
      if (isGray)
      {
        // 1. 处理 TMP 文本
        if (graphic is TextMeshProUGUI tmp)
        {
          // 使用 TMP 专属置灰材质（使用 fontMaterial 创建实例，不污染共享资源）
          if (s_TMPGrayMaterial != null)
            tmp.fontMaterial = s_TMPGrayMaterial;
        }
        // 2. 处理普通 Image & 原生 UI Text
        else
        {
          if (s_UIGrayMaterial != null)
          {
            graphic.material = s_UIGrayMaterial;
          }
        }
      }
      else
      {
        // 恢复默认
        if (graphic is TextMeshProUGUI tmp)
        {
          tmp.fontMaterial = null; // 恢复默认字体材质
        }
        else
        {
          graphic.material = null; // 恢复 UI 默认材质
        }
      }
    }
  }
}