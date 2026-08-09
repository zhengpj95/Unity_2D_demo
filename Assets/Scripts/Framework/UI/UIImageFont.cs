using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;

[AddComponentMenu("UI/UIImageFont")]
[RequireComponent(typeof(RectTransform))]
public class UIImageFont : MonoBehaviour
{
  public enum TextAlignment
  {
    Left,
    Center,
    Right
  }

  [Header("Bitmap Font")]
  [SerializeField] private Texture2D fontTexture;
  [SerializeField] private TextAsset fontFile;

  [Header("Text")]
  [SerializeField] private string text = "1234567890";

  [Header("Layout")]
  [SerializeField] private TextAlignment alignment = TextAlignment.Center;
  [SerializeField] private float scale = 1f;
  [SerializeField] private float letterSpacing = 0f;

  [Header("Color")]
  [SerializeField] private Color color = Color.white;

  private RectTransform _rectTransform;

  private BitmapFontData _fontData;

  private readonly List<Image> _images = new List<Image>();

  private void Awake()
  {
    _rectTransform = GetComponent<RectTransform>();
    BuildFont();
  }

  private void OnEnable()
  {
    Refresh();
  }

  // #if UNITY_EDITOR
  //   private void OnValidate()
  //   {
  //     if (!Application.isPlaying)
  //     {
  //       return;
  //     }

  //     UnityEditor.EditorApplication.delayCall += DelayedRefresh;
  //     Refresh();
  //   }

  //   private void DelayedRefresh()
  //   {
  //     if (!Application.isPlaying || this == null)
  //     {
  //       return;
  //     }
  //     Refresh();
  //   }
  // #endif

  /// <summary>
  /// 设置文字
  /// </summary>
  public void SetText(string value)
  {
    if (text == value)
    {
      return;
    }

    text = value;
    Refresh();
  }

  /// <summary>
  /// 获取当前文字
  /// </summary>
  public string GetText()
  {
    return text;
  }

  /// <summary>
  /// 设置颜色
  /// </summary>
  public void SetColor(Color value)
  {
    color = value;

    for (int i = 0; i < _images.Count; i++)
    {
      if (_images[i] != null)
      {
        _images[i].color = color;
      }
    }
  }

  /// <summary>
  /// 设置缩放
  /// </summary>
  public void SetScale(float value)
  {
    scale = Mathf.Max(0.01f, value);
    Refresh();
  }

  /// <summary>
  /// 设置字间距
  /// </summary>
  public void SetLetterSpacing(float value)
  {
    letterSpacing = value;
    Refresh();
  }

  /// <summary>
  /// 设置对齐方式
  /// </summary>
  public void SetAlignment(TextAlignment value)
  {
    alignment = value;
    Refresh();
  }

  private void BuildFont()
  {
    if (fontTexture == null || fontFile == null)
    {
      _fontData = null;
      return;
    }

    _fontData = BitmapFontParser.Parse(fontFile.text, fontTexture);

    if (_fontData == null)
    {
      Debug.LogError(
          $"[UIImageFont] Failed to parse bitmap font: {fontFile.name}",
          this);
    }
  }

  private void Refresh()
  {
    if (_fontData == null)
    {
      BuildFont();
    }

    if (_fontData == null)
    {
      return;
    }

    BuildText();
  }

  private void BuildText()
  {
    int charCount = text == null ? 0 : text.Length;

    EnsureImageCount(charCount);

    float totalWidth = CalculateTotalWidth();

    float currentX;

    switch (alignment)
    {
      case TextAlignment.Left:
        currentX = 0f;
        break;

      case TextAlignment.Center:
        currentX = -totalWidth * 0.5f;
        break;

      case TextAlignment.Right:
        currentX = -totalWidth;
        break;

      default:
        currentX = 0f;
        break;
    }

    int imageIndex = 0;

    for (int i = 0; i < charCount; i++)
    {
      char character = text[i];

      BitmapCharData charData;

      if (!_fontData.TryGetChar(character, out charData))
      {
        continue;
      }

      Image image = _images[imageIndex++];

      image.gameObject.SetActive(true);
      image.color = color;

      RectTransform imageRect = image.rectTransform;

      float width = charData.width * scale;
      float height = charData.height * scale;

      float x = currentX + charData.xOffset * scale;

      float y = -charData.yOffset * scale;

      imageRect.anchoredPosition = new Vector2(x, y);
      imageRect.sizeDelta = new Vector2(width, height);

      image.sprite = charData.sprite;

      currentX +=
          (charData.xAdvance + letterSpacing) * scale;
    }

    // 隐藏多余 Image
    for (int i = imageIndex; i < _images.Count; i++)
    {
      if (_images[i] != null)
      {
        _images[i].gameObject.SetActive(false);
      }
    }
  }

  private float CalculateTotalWidth()
  {
    if (string.IsNullOrEmpty(text))
    {
      return 0f;
    }

    float width = 0f;

    bool first = true;

    for (int i = 0; i < text.Length; i++)
    {
      BitmapCharData charData;

      if (!_fontData.TryGetChar(text[i], out charData))
      {
        continue;
      }

      if (!first)
      {
        width += letterSpacing * scale;
      }

      width += charData.xAdvance * scale;

      first = false;
    }

    if (width < 0f)
    {
      width = 0f;
    }

    return width;
  }

  private void EnsureImageCount(int count)
  {
    while (_images.Count < count)
    {
      CreateImage();
    }
  }

  private void CreateImage()
  {
    GameObject go = new GameObject(
        "Char",
        typeof(RectTransform),
        typeof(Image));

    go.transform.SetParent(transform, false);

    RectTransform rect = go.GetComponent<RectTransform>();

    rect.anchorMin = new Vector2(0.5f, 0.5f);
    rect.anchorMax = new Vector2(0.5f, 0.5f);
    rect.pivot = new Vector2(0.5f, 0.5f);
    rect.localScale = Vector3.one;

    Image image = go.GetComponent<Image>();

    image.raycastTarget = false;
    image.preserveAspect = false;

    _images.Add(image);
  }
}


/// <summary>
/// Bitmap Font 数据
/// </summary>
[Serializable]
public class BitmapFontData
{
  public Texture2D texture;

  public int lineHeight;
  public int baseLine;

  private readonly Dictionary<char, BitmapCharData> _chars =
      new Dictionary<char, BitmapCharData>();

  public void AddChar(BitmapCharData data)
  {
    char character = (char)data.id;

    if (_chars.ContainsKey(character))
    {
      _chars[character] = data;
    }
    else
    {
      _chars.Add(character, data);
    }
  }

  public bool TryGetChar(char character, out BitmapCharData data)
  {
    return _chars.TryGetValue(character, out data);
  }
}


/// <summary>
/// Bitmap Font 单个字符
/// </summary>
[Serializable]
public class BitmapCharData
{
  public int id;

  public int x;
  public int y;

  public int width;
  public int height;

  public int xOffset;
  public int yOffset;

  public int xAdvance;

  public Sprite sprite;
}


/// <summary>
/// BMFont FNT Parser
/// </summary>
public static class BitmapFontParser
{
  private static readonly Regex KeyValueRegex =
      new Regex(
          @"(\w+)=(""[^""]*""|\S+)",
          RegexOptions.Compiled);

  public static BitmapFontData Parse(
      string content,
      Texture2D texture)
  {
    if (string.IsNullOrEmpty(content))
    {
      return null;
    }

    if (texture == null)
    {
      return null;
    }

    BitmapFontData font =
        new BitmapFontData();

    font.texture = texture;

    string[] lines =
        content.Split(
            new[] { '\r', '\n' },
            StringSplitOptions.RemoveEmptyEntries);

    for (int i = 0; i < lines.Length; i++)
    {
      string line = lines[i].Trim();

      if (line.StartsWith("common "))
      {
        Dictionary<string, string> values =
            ParseLine(line);

        font.lineHeight =
            GetInt(values, "lineHeight");

        font.baseLine =
            GetInt(values, "base");
      }
      else if (line.StartsWith("char "))
      {
        Dictionary<string, string> values =
            ParseLine(line);

        BitmapCharData charData =
            new BitmapCharData();

        charData.id =
            GetInt(values, "id");

        charData.x =
            GetInt(values, "x");

        charData.y =
            GetInt(values, "y");

        charData.width =
            GetInt(values, "width");

        charData.height =
            GetInt(values, "height");

        charData.xOffset =
            GetInt(values, "xoffset");

        charData.yOffset =
            GetInt(values, "yoffset");

        charData.xAdvance =
            GetInt(values, "xadvance");

        charData.sprite =
            CreateSprite(
                texture,
                charData);

        font.AddChar(charData);
      }
    }

    return font;
  }

  private static Sprite CreateSprite(
      Texture2D texture,
      BitmapCharData data)
  {
    if (data.width <= 0 || data.height <= 0)
    {
      return null;
    }

    // BMFont 原点是左上角
    // Unity Texture2D Rect 原点是左下角
    float y =
        texture.height -
        data.y -
        data.height;

    Rect rect =
        new Rect(
            data.x,
            y,
            data.width,
            data.height);

    return Sprite.Create(
        texture,
        rect,
        new Vector2(0.5f, 0.5f),
        100f,
        0,
        SpriteMeshType.FullRect);
  }

  private static Dictionary<string, string> ParseLine(
      string line)
  {
    Dictionary<string, string> result =
        new Dictionary<string, string>();

    MatchCollection matches =
        KeyValueRegex.Matches(line);

    for (int i = 0; i < matches.Count; i++)
    {
      string key =
          matches[i].Groups[1].Value;

      string value =
          matches[i].Groups[2].Value;

      value =
          value.Trim('"');

      result[key] = value;
    }

    return result;
  }

  private static int GetInt(
      Dictionary<string, string> values,
      string key)
  {
    string value;

    if (!values.TryGetValue(key, out value))
    {
      return 0;
    }

    int result;

    if (int.TryParse(
        value,
        NumberStyles.Integer,
        CultureInfo.InvariantCulture,
        out result))
    {
      return result;
    }

    return 0;
  }
}