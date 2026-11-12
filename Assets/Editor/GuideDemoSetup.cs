using System;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>创建可直接运行的 GuideDemo 场景和样例配置。</summary>
public static class GuideDemoSetup
{
  private const string ConfigPath = "Assets/Configs/Guide/Guide_001.asset";
  private const string ScenePath = "Assets/Scenes/GuideDemo.unity";

  [MenuItem("Tools/Guide/Create Guide Demo Scene")]
  public static void CreateDemoScene()
  {
    GuideConfig config = AssetDatabase.LoadAssetAtPath<GuideConfig>(ConfigPath);
    if (config == null)
    {
      Debug.LogError($"[GuideDemoSetup] Sample config is missing: {ConfigPath}");
      return;
    }

    TMP_FontAsset font = TMP_Settings.defaultFontAsset;
    if (font == null)
      throw new InvalidOperationException("[GuideDemoSetup] TextMeshPro default font asset is missing.");

    Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
    new GameObject("GameMgr").AddComponent<GameMgr>();
    CreateMainCamera();
    CreateGuideCanvas(font, config);

    if (!EditorSceneManager.SaveScene(scene, ScenePath))
      throw new InvalidOperationException($"[GuideDemoSetup] Failed to save scene: {ScenePath}");

    AssetDatabase.SaveAssets();
    AssetDatabase.Refresh();
    Debug.Log($"[GuideDemoSetup] Created runnable sample scene: {ScenePath}");
  }

  private static void CreateMainCamera()
  {
    GameObject cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
    cameraObject.tag = "MainCamera";
    cameraObject.transform.position = new Vector3(0f, 0f, -10f);

    Camera camera = cameraObject.GetComponent<Camera>();
    camera.orthographic = true;
    camera.orthographicSize = 5f;
    camera.clearFlags = CameraClearFlags.SolidColor;
    camera.backgroundColor = new Color(0.192f, 0.302f, 0.475f, 0f);
  }

  private static void CreateGuideCanvas(TMP_FontAsset font, GuideConfig config)
  {
    GameObject canvasObject = new GameObject(
      "UIRoot",
      typeof(RectTransform),
      typeof(Canvas),
      typeof(CanvasScaler),
      typeof(GraphicRaycaster),
      typeof(UILauncher),
      typeof(GuideDemoBootstrap));

    Canvas canvas = canvasObject.GetComponent<Canvas>();
    canvas.renderMode = RenderMode.ScreenSpaceOverlay;

    CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
    scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
    scaler.referenceResolution = new Vector2(1280f, 720f);
    scaler.matchWidthOrHeight = 0.5f;

    Transform mainLayer = CreateLayer("MainLayer", canvasObject.transform);
    Transform windowLayer = CreateLayer("WindowLayer", canvasObject.transform);
    Transform modelLayer = CreateLayer("ModelLayer", canvasObject.transform);
    Transform tipLayer = CreateLayer("TipLayer", canvasObject.transform);

    GameObject buttonObject = CreateRectObject("NextButton", mainLayer);
    SetRect(buttonObject.GetComponent<RectTransform>(), new Vector2(0.5f, 0.15f), new Vector2(0.5f, 0.15f), Vector2.zero, new Vector2(180f, 58f));
    Image buttonImage = buttonObject.AddComponent<Image>();
    buttonImage.color = new Color(0.16f, 0.48f, 0.78f, 1f);
    Button button = buttonObject.AddComponent<Button>();
    button.targetGraphic = buttonImage;
    GuideCursor cursor = buttonObject.AddComponent<GuideCursor>();
    cursor.SetTargetId("NextButton");

    TextMeshProUGUI buttonText = CreateText("ButtonText", buttonObject.transform, font, 22f, TextAlignmentOptions.Center);
    SetRect(buttonText.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
    buttonText.text = "继续";
    buttonText.raycastTarget = false;

    UILauncher launcher = canvasObject.GetComponent<UILauncher>();
    SetObjectReference(launcher, "_mainLayer", mainLayer);
    SetObjectReference(launcher, "_windowLayer", windowLayer);
    SetObjectReference(launcher, "_modelLayer", modelLayer);
    SetObjectReference(launcher, "_tipLayer", tipLayer);

    GuideDemoBootstrap bootstrap = canvasObject.GetComponent<GuideDemoBootstrap>();
    SetObjectReference(bootstrap, "guideConfig", config);
  }

  private static Transform CreateLayer(string layerName, Transform parent)
  {
    GameObject layer = CreateRectObject(layerName, parent);
    SetRect(layer.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
    return layer.transform;
  }

  private static TextMeshProUGUI CreateText(
    string objectName,
    Transform parent,
    TMP_FontAsset font,
    float fontSize,
    TextAlignmentOptions alignment)
  {
    GameObject textObject = CreateRectObject(objectName, parent);
    TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
    text.font = font;
    text.fontSize = fontSize;
    text.color = Color.white;
    text.alignment = alignment;
    text.raycastTarget = false;
    return text;
  }

  private static GameObject CreateRectObject(string objectName, Transform parent)
  {
    GameObject child = new GameObject(objectName, typeof(RectTransform));
    child.transform.SetParent(parent, false);
    return child;
  }

  private static void SetRect(
    RectTransform rect,
    Vector2 anchorMin,
    Vector2 anchorMax,
    Vector2 anchoredPosition,
    Vector2 sizeDelta,
    Vector2? pivot = null)
  {
    rect.anchorMin = anchorMin;
    rect.anchorMax = anchorMax;
    rect.pivot = pivot ?? new Vector2(0.5f, 0.5f);
    rect.anchoredPosition = anchoredPosition;
    rect.sizeDelta = sizeDelta;
  }

  private static void SetObjectReference(UnityEngine.Object target, string propertyName, UnityEngine.Object value)
  {
    SerializedObject serializedObject = new SerializedObject(target);
    SerializedProperty property = serializedObject.FindProperty(propertyName);
    if (property == null)
      throw new InvalidOperationException($"[GuideDemoSetup] Serialized field not found: {target.GetType().Name}.{propertyName}");

    property.objectReferenceValue = value;
    serializedObject.ApplyModifiedPropertiesWithoutUndo();
  }
}
