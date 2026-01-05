using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 确保挂载此脚本的物体上一定有 SpriteRenderer 组件
[RequireComponent(typeof(SpriteRenderer))]
public class ObjectFader : MonoBehaviour
{
  [Header("设置")]
  [Tooltip("淡化后的透明度 (0完全透明 - 1完全不透明)")]
  [Range(0f, 1f)]
  public float fadedAlpha = 0.6f; // 建议设置在 0.4 到 0.6 之间

  [Tooltip("淡入淡出的速度")]
  public float fadeSpeed = 5f;

  private SpriteRenderer spriteRenderer;
  private float originalAlpha;
  private Coroutine currentFadeRoutine;

  void Start()
  {
    spriteRenderer = GetComponent<SpriteRenderer>();
    originalAlpha = spriteRenderer.color.a;
  }

  void OnTriggerEnter2D(Collider2D collision)
  {
    if (collision.gameObject.CompareTag("Player"))
    {
      StartFade(fadedAlpha); // 淡出到50%透明度
    }
  }

  void OnTriggerExit2D(Collider2D collision)
  {
    if (collision.gameObject.CompareTag("Player"))
    {
      StartFade(originalAlpha); // 淡入到100%不透明
    }
  }

  // 开始淡入淡出处理
  void StartFade(float targetAlpha)
  {
    if (currentFadeRoutine != null)
    {
      StopCoroutine(currentFadeRoutine);
    }
    currentFadeRoutine = StartCoroutine(FadeRoutine(targetAlpha));
  }

  // 实际执行平滑过渡的协程
  private IEnumerator FadeRoutine(float targetAlpha)
  {
    Color currentColor = spriteRenderer.color;

    // 当当前 alpha 值与目标值差距大于 0.01 时，持续循环插值
    while (Mathf.Abs(currentColor.a - targetAlpha) > 0.01f)
    {
      // 使用 Lerp 进行平滑过渡
      currentColor.a = Mathf.Lerp(currentColor.a, targetAlpha, Time.deltaTime * fadeSpeed);
      spriteRenderer.color = currentColor;
      // 等待下一帧再继续
      yield return null;
    }

    // 确保最终值精确无误
    currentColor.a = targetAlpha;
    spriteRenderer.color = currentColor;

    currentFadeRoutine = null;
  }
}
