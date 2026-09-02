using TMPro;
using UnityEngine;

public class UI_Damage : MonoBehaviour
{
  [Header("Animation")]
  [Min(0.01f)]
  [SerializeField] private float _duration = 0.8f;
  [Min(0f)]
  [SerializeField] private float _minHorizontalDistance = 80f;
  [Min(0f)]
  [SerializeField] private float _maxHorizontalDistance = 140f;
  [Min(0f)]
  [SerializeField] private float _minVerticalDistance = 100f;
  [Min(0f)]
  [SerializeField] private float _maxVerticalDistance = 180f;

  private RectTransform _rectTransform;
  private TMP_Text _text;
  private Vector2 _startPosition;
  private Vector2 _targetPosition;
  private Color _textColor;
  private float _elapsedTime;

  private void Awake()
  {
    _rectTransform = GetComponent<RectTransform>();
    _text = GetComponent<TMP_Text>();
  }

  private void Start()
  {
    _startPosition = _rectTransform.anchoredPosition;

    float horizontalDirection = Random.value < 0.5f ? -1f : 1f;
    float horizontalDistance = Random.Range(_minHorizontalDistance, _maxHorizontalDistance);
    float verticalDistance = Random.Range(_minVerticalDistance, _maxVerticalDistance);
    _targetPosition = _startPosition + new Vector2(horizontalDirection * horizontalDistance, verticalDistance);

    if (_text != null)
    {
      _textColor = _text.color;
    }
  }

  private void Update()
  {
    _elapsedTime += Time.deltaTime;
    float progress = Mathf.Clamp01(_elapsedTime / _duration);
    float easedProgress = 1f - (1f - progress) * (1f - progress);
    _rectTransform.anchoredPosition = Vector2.LerpUnclamped(_startPosition, _targetPosition, easedProgress);

    if (_text != null)
    {
      Color color = _textColor;
      color.a = Mathf.Lerp(_textColor.a, 0f, Mathf.InverseLerp(0.6f, 1f, progress));
      _text.color = color;
    }

    if (progress >= 1f)
    {
      Destroy(gameObject);
    }
  }

  public void SetDamageText(int damageAmount)
  {
    if (_text != null)
    {
      _text.text = damageAmount.ToString();
    }
  }
}
