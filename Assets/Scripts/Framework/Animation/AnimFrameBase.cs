using System;
using UnityEngine;

/// <summary>
/// 序列帧动画组件基类，统一处理动作查找、计时、循环、暂停和完成回调。
/// 子类只负责把当前帧应用到对应的 Unity 显示组件。
/// </summary>
public abstract class AnimFrameBase : MonoBehaviour
{
  [SerializeField, Tooltip("当前组件可以播放的全部动画配置。")]
  private AnimFrameData[] animations = Array.Empty<AnimFrameData>();

  [SerializeField, Tooltip("启用时默认播放的动画名称；留空时使用第一组有效动画。")]
  private string defaultAnimName;

  [SerializeField, Tooltip("组件启用时是否自动从头播放默认动画。")]
  private bool playOnEnable = true;

  private AnimFrameData _currentAnim;
  private Action _onComplete;
  private float _elapsedTime;
  private int _frameIndex;
  private bool _isPlaying;
  private bool _isPaused;

  /// <summary>当前是否存在正在播放且未暂停的动画。</summary>
  public bool IsPlaying => _isPlaying && !_isPaused;

  /// <summary>当前动画名称；尚未选择动画时返回空字符串。</summary>
  public string CurrentAnimName => _currentAnim == null ? string.Empty : _currentAnim.AnimName;

  /// <summary>当前显示帧的下标；尚未选择动画时返回 -1。</summary>
  public int CurrentFrameIndex => _currentAnim == null ? -1 : _frameIndex;

  /// <summary>
  /// 检查当前组件是否配置了指定名称且至少包含一帧的动画。
  /// </summary>
  /// <param name="animName">需要检查的动画名称。</param>
  /// <returns>存在可播放动画时返回 true。</returns>
  public bool HasAnimation(string animName)
  {
    return FindAnimation(animName) != null;
  }

  /// <summary>子类播放动画时使用的每帧时间。</summary>
  protected abstract float DeltaTime { get; }

  /// <summary>解析并缓存 SpriteRenderer 或 Image 等实际显示目标。</summary>
  protected abstract void ResolveTarget();

  /// <summary>
  /// 将当前动画帧应用到实际显示目标。
  /// </summary>
  /// <param name="sprite">需要显示的 Sprite，允许为 null。</param>
  protected abstract void SetSprite(Sprite sprite);

  protected virtual void Awake()
  {
    ResolveTarget();
    PrepareDefaultFrame();
  }

  protected virtual void OnEnable()
  {
    if (playOnEnable)
    {
      PlayDefault();
      return;
    }

    PrepareDefaultFrame();
  }

  protected virtual void Update()
  {
    Tick(DeltaTime);
  }

  protected virtual void OnDisable()
  {
    // 池对象入池或界面隐藏时取消外部回调，避免下次启用后执行上一轮逻辑。
    StopInternal(false);
  }

  protected virtual void Reset()
  {
    ResolveTarget();
    PrepareDefaultFrame();
  }

  /// <summary>
  /// 按名称播放动画。再次调用会从该动画的起始帧重新播放。
  /// </summary>
  /// <param name="animName">AnimFrameData 中配置的动画名称。</param>
  /// <param name="onComplete">非循环动画完整播放后的回调；循环动画不会触发。</param>
  /// <returns>找到有效动画并开始播放时返回 true，否则返回 false。</returns>
  public bool Play(string animName, Action onComplete = null)
  {
    AnimFrameData anim = FindAnimation(animName);
    if (anim == null)
    {
      Debug.LogWarning($"[AnimFrame] Animation not found or has no frames: {animName}", this);
      return false;
    }

    StartAnimation(anim, onComplete);
    return true;
  }

  /// <summary>
  /// 播放默认动画；默认名称为空时播放第一组有效动画。
  /// </summary>
  /// <param name="onComplete">非循环动画完整播放后的回调。</param>
  /// <returns>存在有效默认动画并开始播放时返回 true，否则返回 false。</returns>
  public bool PlayDefault(Action onComplete = null)
  {
    AnimFrameData anim = FindDefaultAnimation();
    if (anim == null)
    {
      return false;
    }

    StartAnimation(anim, onComplete);
    return true;
  }

  /// <summary>暂停当前动画并保留当前帧和播放进度。</summary>
  public void Pause()
  {
    if (_isPlaying)
    {
      _isPaused = true;
    }
  }

  /// <summary>从暂停位置继续播放当前动画。</summary>
  public void Resume()
  {
    if (_isPlaying)
    {
      _isPaused = false;
    }
  }

  /// <summary>
  /// 停止当前动画并取消完成回调。
  /// </summary>
  /// <param name="resetToFirstFrame">是否把画面恢复到当前动画的第一帧。</param>
  public void Stop(bool resetToFirstFrame = true)
  {
    StopInternal(resetToFirstFrame);
  }

  /// <summary>
  /// 由 Unity Update 驱动帧切换，不在高频路径创建临时集合或使用 LINQ。
  /// </summary>
  private void Tick(float deltaTime)
  {
    if (!_isPlaying || _isPaused || _currentAnim == null || deltaTime <= 0f)
    {
      return;
    }

    float frameDuration = 1f / _currentAnim.FrameRate;
    _elapsedTime += deltaTime;
    if (_elapsedTime < frameDuration)
    {
      return;
    }

    int advancedFrames = Mathf.FloorToInt(_elapsedTime / frameDuration);
    _elapsedTime -= advancedFrames * frameDuration;

    int nextFrameIndex = _frameIndex + advancedFrames;
    if (_currentAnim.Loop)
    {
      _frameIndex = nextFrameIndex % _currentAnim.FrameCount;
      ApplyCurrentFrame();
      return;
    }

    if (nextFrameIndex < _currentAnim.FrameCount)
    {
      _frameIndex = nextFrameIndex;
      ApplyCurrentFrame();
      return;
    }

    // 最后一帧保持显示，先结束播放状态再调用外部逻辑，允许回调安全地播放下一段动画或回收对象。
    _frameIndex = _currentAnim.FrameCount - 1;
    ApplyCurrentFrame();
    _isPlaying = false;
    _isPaused = false;

    Action callback = _onComplete;
    _onComplete = null;
    callback?.Invoke();
  }

  /// <summary>使用给定配置初始化本次播放状态并立即显示起始帧。</summary>
  private void StartAnimation(AnimFrameData anim, Action onComplete)
  {
    _currentAnim = anim;
    _onComplete = anim.Loop ? null : onComplete;
    _elapsedTime = 0f;
    _frameIndex = anim.RandomStartFrame && anim.FrameCount > 1
        ? UnityEngine.Random.Range(0, anim.FrameCount)
        : 0;
    _isPlaying = true;
    _isPaused = false;
    ApplyCurrentFrame();
  }

  /// <summary>停止播放并根据需要恢复第一帧。</summary>
  private void StopInternal(bool resetToFirstFrame)
  {
    _isPlaying = false;
    _isPaused = false;
    _elapsedTime = 0f;
    _onComplete = null;

    if (!resetToFirstFrame || _currentAnim == null || _currentAnim.FrameCount == 0)
    {
      return;
    }

    _frameIndex = 0;
    ApplyCurrentFrame();
  }

  /// <summary>停止状态下准备默认动画的首帧，便于编辑器和关闭自动播放时预览。</summary>
  private void PrepareDefaultFrame()
  {
    AnimFrameData anim = FindDefaultAnimation();
    if (anim == null)
    {
      return;
    }

    _currentAnim = anim;
    _frameIndex = 0;
    _elapsedTime = 0f;
    _isPlaying = false;
    _isPaused = false;
    _onComplete = null;
    ApplyCurrentFrame();
  }

  /// <summary>查找默认名称指定的动画，名称为空时返回第一组有效配置。</summary>
  private AnimFrameData FindDefaultAnimation()
  {
    if (!string.IsNullOrWhiteSpace(defaultAnimName))
    {
      return FindAnimation(defaultAnimName);
    }

    if (animations == null)
    {
      return null;
    }

    for (int i = 0; i < animations.Length; i++)
    {
      AnimFrameData anim = animations[i];
      if (anim != null && anim.FrameCount > 0)
      {
        return anim;
      }
    }

    return null;
  }

  /// <summary>按名称查找第一组有效动画配置。</summary>
  private AnimFrameData FindAnimation(string animName)
  {
    if (animations == null || string.IsNullOrWhiteSpace(animName))
    {
      return null;
    }

    for (int i = 0; i < animations.Length; i++)
    {
      AnimFrameData anim = animations[i];
      if (anim != null && anim.FrameCount > 0 &&
          string.Equals(anim.AnimName, animName, StringComparison.Ordinal))
      {
        return anim;
      }
    }

    return null;
  }

  /// <summary>把当前下标对应的 Sprite 交给具体显示组件。</summary>
  private void ApplyCurrentFrame()
  {
    if (_currentAnim == null || _currentAnim.FrameCount == 0)
    {
      return;
    }

    SetSprite(_currentAnim.GetFrame(_frameIndex));
  }
}
