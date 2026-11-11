using System;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>创建可直接运行的 GuideDemo 场景和样例配置。</summary>
public static class GuideDemoSetup
{
  private const string ConfigPath = "Assets/Configs/Guide/Guide_001.asset";
  private const string CursorPrefabPath = "Assets/Prefabs/Common/GuideCursor.prefab";
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

    GameObject cursorPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CursorPrefabPath);
    if (cursorPrefab == null)
    {
      Debug.LogError($"[GuideDemoSetup] GuideCursor gesture Prefab is missing: {CursorPrefabPath}");
      return;
    }

    TMP_FontAsset font = TMP_Settings.defaultFontAsset;
    if (font == null)
      throw new InvalidOperationException("[GuideDemoSetup] TextMeshPro default font asset is missing.");

    Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
    new GameObject("GameMgr").AddComponent<GameMgr>();
    CreateMainCamera();
    CreateEventSystem();
    CreateGuideCanvas(font, config, cursorPrefab);

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

  private static void CreateEventSystem()
  {
    GameObject eventSystem = new GameObject(
      "EventSystem",
      typeof(EventSystem),
      typeof(StandaloneInputModule));
    eventSystem.transform.SetAsLastSibling();
  }

  private static void CreateGuideCanvas(TMP_FontAsset font, GuideConfig config, GameObject cursorPrefab)
  {
    GameObject canvasObject = new GameObject(
      "GuideCanvas",
      typeof(RectTransform),
      typeof(Canvas),
      typeof(CanvasScaler),
      typeof(GraphicRaycaster),
      typeof(GuideDemoBootstrap));

    Canvas canvas = canvasObject.GetComponent<Canvas>();
    canvas.renderMode = RenderMode.ScreenSpaceOverlay;

    CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
    scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
    scaler.referenceResolution = new Vector2(1280f, 720f);
    scaler.matchWidthOrHeight = 0.5f;

    GameObject panel = CreateRectObject("GuidePanel", canvasObject.transform);
    SetRect(panel.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(650f, 360f));

    GameObject background = CreateRectObject("Background", panel.transform);
    SetRect(background.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
    Image backgroundImage = background.AddComponent<Image>();
    backgroundImage.color = new Color(0.07f, 0.1f, 0.15f, 0.96f);
    backgroundImage.raycastTarget = true;

    GameObject content = CreateRectObject("Content", panel.transform);
    SetRect(content.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, Vector2.zero, new Vector2(-48f, -40f));

    TextMeshProUGUI title = CreateText("TitleText", content.transform, font, 32f, TextAlignmentOptions.Center);
    SetRect(title.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -28f), new Vector2(-20f, 54f), new Vector2(0.5f, 1f));

    TextMeshProUGUI body = CreateText("ContentText", content.transform, font, 24f, TextAlignmentOptions.TopLeft);
    SetRect(body.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -100f), new Vector2(-24f, 132f), new Vector2(0.5f, 1f));
    body.enableWordWrapping = true;

    TextMeshProUGUI progress = CreateText("StepText", content.transform, font, 20f, TextAlignmentOptions.Center);
    SetRect(progress.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 82f), new Vector2(160f, 36f));

    GameObject buttonObject = CreateRectObject("NextButton", content.transform);
    SetRect(buttonObject.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 28f), new Vector2(180f, 58f));
    Image buttonImage = buttonObject.AddComponent<Image>();
    buttonImage.color = new Color(0.16f, 0.48f, 0.78f, 1f);
    Button button = buttonObject.AddComponent<Button>();
    button.targetGraphic = buttonImage;
    GuideCursor cursor = buttonObject.AddComponent<GuideCursor>();
    cursor.SetTargetId("NextButton");

    TextMeshProUGUI buttonText = CreateText("ButtonText", buttonObject.transform, font, 22f, TextAlignmentOptions.Center);
    SetRect(buttonText.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
    buttonText.text = "下一步";
    buttonText.raycastTarget = false;

    GuideView view = panel.AddComponent<GuideView>();
    SetObjectReference(view, "panel", panel);
    SetObjectReference(view, "titleText", title);
    SetObjectReference(view, "contentText", body);
    SetObjectReference(view, "stepText", progress);
    SetObjectReference(view, "cursorPrefab", cursorPrefab);

    GuideDemoBootstrap bootstrap = canvasObject.GetComponent<GuideDemoBootstrap>();
    SetObjectReference(bootstrap, "guideView", view);
    SetObjectReference(bootstrap, "guideConfig", config);
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
