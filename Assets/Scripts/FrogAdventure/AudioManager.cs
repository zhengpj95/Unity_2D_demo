using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Audio;

public static class SoundType
{
  public const string BgmMusic1 = "356-8-bit-chiptune-game-music-357518";
  public const string BgmMusic2 = "mobile-casual-video-game-music-158301";

  public const string GameStart = "game-start-6104";
  public const string GameOver = "game-over-arcade-6435";
  public const string Collect = "collect-points-190037";
  public const string Jump = "cartoon-jump-6462";
}

public class AudioManager : MonoBehaviour
{
  [Header("默认背景音乐")] 
  public AudioClip defaultBgm;

  [Header("音量设置")] 
  [Range(0f, 1f)] public float bgmVolume = 1f;
  [Range(0f, 1f)] public float sfxVolume = 1f;
  private float[] _volumeCache = { 0f, 0f };

  // 背景音乐
  private AudioSource _bgm;

  // 正在播放的音效，包含循环音效
  private readonly List<AudioSource> _sfxList = new();

  // 正在播放的循环音效
  private readonly Dictionary<string, AudioSource> _loopSfxList = new();

  // 音效对象池
  private readonly Queue<AudioSource> _sfxPool = new();
  private const int InitialSfxPoolSize = 5;

  protected void Awake()
  {
    DontDestroyOnLoad(gameObject);

    _bgm = gameObject.GetComponent<AudioSource>();
    if (_bgm == null)
    {
      _bgm = gameObject.AddComponent<AudioSource>();
    }

    _bgm.loop = true;
    _bgm.playOnAwake = false;
    _bgm.volume = bgmVolume;

    // 初始化音效对象池
    for (var i = 0; i < InitialSfxPoolSize; i++)
    {
      CreateSfxSource();
    }

    // 初始背景音乐不为空，则开始播放
    if (defaultBgm != null)
    {
      PlayBGM(defaultBgm);
    }
  }

  private void Update()
  {
    // 最笨的方法控制音量变化
    if (!Mathf.Approximately(_volumeCache[0], bgmVolume))
    {
      _bgm.volume = bgmVolume;
      _volumeCache[0] = bgmVolume;
    }

    if (!Mathf.Approximately(_volumeCache[1], sfxVolume))
    {
      foreach (var sfx in _sfxList)
      {
        sfx.volume = sfxVolume;
      }
      _volumeCache[1] = sfxVolume;
    }
}

  // 暂停背景音乐和所有音效
  public void Stop()
  {
    StopBGM();
    StopAllSfx();
  }

  #region === BGM ===

  public void PlayBGM(string soundType)
  {
    var clip = Resources.Load<AudioClip>("Sound/" + soundType);
    PlayBGM(clip);
  }

  private void PlayBGM(AudioClip clip)
  {
    if (clip == null) return;
    if (_bgm.clip == clip && _bgm.isPlaying) return;

    StopAllCoroutines();
    StartCoroutine(FadeInBGM(clip));
  }

  private void StopBGM(float fadeTime = 1f)
  {
    StopAllCoroutines();
    StartCoroutine(FadeOutBGM(fadeTime));
  }

  private IEnumerator FadeInBGM(AudioClip clip, float fadeTime = 1f)
  {
    _bgm.clip = clip;
    _bgm.volume = 0f;
    _bgm.Play();

    float t = 0f;
    while (t < fadeTime)
    {
      t += Time.deltaTime;
      _bgm.volume = Mathf.Lerp(0f, bgmVolume, t / fadeTime);
      yield return null;
    }

    _bgm.volume = bgmVolume;
  }

  private IEnumerator FadeOutBGM(float fadeTime = 1f)
  {
    float startVol = _bgm.volume;
    float t = 0f;
    while (t < fadeTime)
    {
      t += Time.deltaTime;
      _bgm.volume = Mathf.Lerp(startVol, 0f, t / fadeTime);
      yield return null;
    }

    _bgm.Stop();
    _bgm.clip = null;
    _bgm.volume = bgmVolume;
  }

  #endregion =====================

  #region === SFX ===

  public void PlaySfx(string soundType, bool loop = false)
  {
    var clip = Resources.Load<AudioClip>("Sound/" + soundType);
    PlaySfx(clip, soundType, loop);
  }

  // 停止播放循环音效
  public void StopSfx(string soundType)
  {
    if (_loopSfxList.TryGetValue(soundType, out AudioSource src))
    {
      src.Stop();
      _loopSfxList.Remove(soundType);
      _sfxList.Remove(src);
      ReleaseSfxSource(src);
    }
  }

  // sfx对象池
  private AudioSource CreateSfxSource()
  {
    var obj = new GameObject("SFX_AudioSource");
    obj.transform.SetParent(transform);
    var src = obj.AddComponent<AudioSource>();
    src.playOnAwake = false;
    _sfxPool.Enqueue(src);
    return src;
  }

  // 分配sfx
  private AudioSource AllocSfxSource()
  {
    return _sfxPool.Count > 0 ? _sfxPool.Dequeue() : CreateSfxSource();
  }

  // 放回对象池
  private void ReleaseSfxSource(AudioSource src)
  {
    src.Stop();
    src.clip = null;
    src.loop = false;
    src.volume = sfxVolume;
    _sfxPool.Enqueue(src);
  }

  // ReSharper disable Unity.PerformanceAnalysis
  private void PlaySfx(AudioClip clip, string soundType, bool loop = false)
  {
    if (!clip) return;
    if (loop && _loopSfxList.ContainsKey(soundType)) return; // 如果已经有同类型的循环音效在播放，直接返回

    var sfx = AllocSfxSource();
    sfx.clip = clip;
    sfx.loop = loop;
    sfx.volume = sfxVolume;
    sfx.Play();
    _sfxList.Add(sfx); // 正在播放的音效

    // 如果不是循环音效，则自动销毁
    if (!loop)
    {
      StartCoroutine(DestroyAfterPlay(sfx));
    }
    else
    {
      _loopSfxList.Add(soundType, sfx);
    }
  }

  private IEnumerator DestroyAfterPlay(AudioSource sfx)
  {
    yield return new WaitWhile(() => sfx.isPlaying);
    _sfxList.Remove(sfx);
    ReleaseSfxSource(sfx);
  }

  // 停止播放全部音效
  private void StopAllSfx()
  {
    // 循环音效
    foreach (var (key, src) in _loopSfxList)
    {
      src.Stop();
      _loopSfxList.Remove(key);
      _sfxList.Remove(src);
      ReleaseSfxSource(src);
    }

    _loopSfxList.Clear();

    // 全部音效
    foreach (var sfx in _sfxList.ToList())
    {
      if (sfx == null) continue;
      sfx.Stop();
      _sfxList.Remove(sfx);
      ReleaseSfxSource(sfx);
    }

    _sfxList.Clear();
  }

  #endregion =====================
}