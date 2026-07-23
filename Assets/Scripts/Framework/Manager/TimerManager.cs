using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Timer
{
  public uint Id;

  // 时间计时器字段
  public float Duration; // 总时长（用于时间计时器）
  public float ExeTime;  // 执行时间（用于时间计时器）

  // 帧计时器字段
  public int InitialFrames;   // 初始帧数（用于帧计时器，loop 重置用）
  public int RemainingFrames; // 剩余帧数（用于帧计时器）
  public bool UseFrames;      // 是否为帧计时器

  public bool Loop;  // 是否循环
  public bool IsActive; // 计时器是否激活
  public bool Paused; // 计时器是否暂停
  public Action Callback; // 计时器结束时的回调函数
}

public class TimerManager : MonoBehaviour
{
  public static TimerManager Instance { get; private set; }

  private readonly List<Timer> timers = new List<Timer>();
  private uint _idSeed;
  private bool _globalPaused;

  private void Awake()
  {
    if (Instance == null)
    {
      Instance = this;
      DontDestroyOnLoad(gameObject);
    }
    else
    {
      Destroy(gameObject);
    }
  }

  private void Update()
  {
    if (_globalPaused)
    {
      return;
    }
    float dt = Time.deltaTime;
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
          timer.ExeTime -= dt;

          if (timer.ExeTime <= 0f)
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
              timer.ExeTime = timer.Duration;
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

  private uint CreateTimer(float duration, Action callback, bool loop = false)
  {
    Timer timer = new Timer
    {
      Id = ++_idSeed,
      Duration = duration,
      ExeTime = duration,
      Loop = loop,
      IsActive = true,
      Paused = false,
      Callback = callback
    };
    timers.Add(timer);
    return timer.Id;
  }

  public uint AddTimer(float duration, Action callback, bool loop = false)
  {
    return CreateTimer(duration, callback, loop);
  }

  /// <summary>
  /// 添加一个基于帧的计时器，指定帧数后触发一次回调（或循环）。
  /// 每帧在 Update() 中都会将剩余帧数减少 1。
  /// </summary>
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
