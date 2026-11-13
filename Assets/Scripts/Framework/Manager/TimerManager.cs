using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Timer
{
  public uint Id;

  /// <summary>时间计时器的循环间隔，单位为毫秒。</summary>
  public float DurationMilliseconds;
  /// <summary>时间计时器的剩余时长，单位为毫秒。</summary>
  public float RemainingMilliseconds;

  // 帧计时器字段
  public int InitialFrames;   // 初始帧数（用于帧计时器，loop 重置用）
  public int RemainingFrames; // 剩余帧数（用于帧计时器）
  public bool UseFrames;      // 是否为帧计时器

  public bool Loop;  // 是否循环
  public bool IsActive; // 计时器是否激活
  public bool Paused; // 计时器是否暂停
  public Action Callback; // 计时器结束时的回调函数
}

public class TimerManager : Singleton<TimerManager>
{
  private readonly List<Timer> timers = new List<Timer>();
  private uint _idSeed;
  private bool _globalPaused;

  public void OnUpdate()
  {
    if (_globalPaused)
    {
      return;
    }
    float deltaMilliseconds = Time.deltaTime * 1000f;
    for (int i = timers.Count - 1; i >= 0; i--)
    {
      // 更新每个计时器
      Timer timer = timers[i];
      if (timer.IsActive && !timer.Paused)
      {
        if (timer.UseFrames)
        {
          // 帧计时器：每帧减少一次
          timer.RemainingFrames--;

          if (timer.RemainingFrames <= 0)
          {
            try
            {
              timer.Callback?.Invoke();
            }
            catch (Exception e)
            {
              Debug.LogError($"Timer Callback Error: {e}");
            }

            if (timer.Loop)
            {
              // 重置为初始帧数
              timer.RemainingFrames = timer.InitialFrames;
            }
            else
            {
              // 移除非循环计时器
              timers.RemoveAt(i);
            }
          }
        }
        else
        {
          // 时间计时器
          timer.RemainingMilliseconds -= deltaMilliseconds;

          if (timer.RemainingMilliseconds <= 0f)
          {
            try
            {
              // 计时器结束，执行回调
              timer.Callback?.Invoke();
            }
            catch (Exception e)
            {
              Debug.LogError($"Timer Callback Error: {e}");
            }

            if (timer.Loop)
            {
              // 重置计时器
              timer.RemainingMilliseconds = timer.DurationMilliseconds;
            }
            else
            {
              // 移除非循环计时器
              timers.RemoveAt(i);
            }
          }
        }
      }
    }
  }

  private uint CreateTimer(float durationMilliseconds, Action callback, bool loop = false)
  {
    Timer timer = new Timer
    {
      Id = ++_idSeed,
      DurationMilliseconds = durationMilliseconds,
      RemainingMilliseconds = durationMilliseconds,
      Loop = loop,
      IsActive = true,
      Paused = false,
      Callback = callback
    };
    timers.Add(timer);
    return timer.Id;
  }

  /// <summary>
  /// 添加一个基于游戏时间的计时器，延迟以毫秒计；可选择循环触发。
  /// </summary>
  /// <param name="durationMilliseconds">触发间隔，单位为毫秒。</param>
  /// <param name="callback">计时到期时执行的回调。</param>
  /// <param name="loop">是否在每次触发后重新计时。</param>
  /// <returns>新计时器 ID，可用于暂停、恢复或取消。</returns>
  public uint AddTimer(float durationMilliseconds, Action callback, bool loop = false)
  {
    return CreateTimer(durationMilliseconds, callback, loop);
  }

  /// <summary>
  /// 添加一个基于帧的计时器，指定帧数后触发一次回调（或循环）。
  /// 每帧在 Update() 中都会将剩余帧数减少 1。
  /// </summary>
  /// <param name="frames">触发间隔的帧数。</param>
  /// <param name="callback">计时到期时执行的回调。</param>
  /// <param name="loop">是否在每次触发后重新计时。</param>
  /// <returns>新计时器 ID，可用于暂停、恢复或取消。</returns>
  public uint AddFrameTimer(int frames, Action callback, bool loop = false)
  {
    return CreateFrameTimer(frames, callback, loop);
  }

  private uint CreateFrameTimer(int frames, Action callback, bool loop = false)
  {
    if (frames <= 0) frames = 1;
    Timer timer = new Timer
    {
      Id = ++_idSeed,
      InitialFrames = frames,
      RemainingFrames = frames,
      UseFrames = true,
      Loop = loop,
      IsActive = true,
      Paused = false,
      Callback = callback
    };
    timers.Add(timer);
    return timer.Id;
  }

  public void Cancel(uint id)
  {
    for (int i = timers.Count - 1; i >= 0; i--)
    {
      if (timers[i].Id == id)
      {
        timers.RemoveAt(i);
        break;
      }
    }
  }

  public void Pause(uint id)
  {
    var t = timers.Find(timer => timer.Id == id);
    if (t != null)
    {
      t.Paused = true;
    }
  }

  public void Resume(uint id)
  {
    var t = timers.Find(timer => timer.Id == id);
    if (t != null)
    {
      t.Paused = false;
    }
  }

  public void PauseAll()
  {
    _globalPaused = true;
  }

  public void ResumeAll()
  {
    _globalPaused = false;
  }

  public void ClearAll()
  {
    timers.Clear();
  }
}
