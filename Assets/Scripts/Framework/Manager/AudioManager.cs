using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

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
  public static AudioManager Instance { get; private set; }

  [Header("默认背景音乐")] 
  public AudioClip defaultBgm;

  [Header("音量设置")] 
  [Range(0f, 1f)] public float bgmVolume = 1f;
  [Range(0f, 1f)] public float sfxVolume = 1f;
  private float[] _volumeCache = { 0f, 0f };

  // 背景音乐
  private AudioSource _bgm;
  private AssetHandle<AudioClip> _bgmAssetHandle;
  private Coroutine _bgmCoroutine;
  private int _bgmRequestGeneration;

  // 正在播放的音效，包含循环音效
  private readonly List<AudioSource> _sfxList = new();
  private readonly Dictionary<AudioSource, AssetHandle<AudioClip>> _sfxAssetHandles = new();
  private int _sfxRequestGeneration;
  private readonly Dictionary<string, int> _sfxTypeRequestGenerations = new();

  // 正在播放的循环音效
  private readonly Dictionary<string, AudioSource> _loopSfxList = new();

  // 音效对象池
  private readonly Queue<AudioSource> _sfxPool = new();
  private const int InitialSfxPoolSize = 5;

  protected void Awake()
  {
    if (Instance != null && Instance != this)
    {
      Destroy(gameObject);
      return;
    }
    Instance = this;
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

  private void OnDestroy()
  {
    StopAllCoroutines();
    _bgmCoroutine = null;
    _bgmRequestGeneration++;
    _sfxRequestGeneration++;
    if (_bgm != null)
    {
      _bgm.Stop();
      _bgm.clip = null;
    }

    _bgmAssetHandle?.Dispose();
    _bgmAssetHandle = null;
    foreach (AssetHandle<AudioClip> handle in _sfxAssetHandles.Values)
      handle.Dispose();
    _sfxAssetHandles.Clear();
    _sfxList.Clear();
    _loopSfxList.Clear();
    _sfxPool.Clear();

    if (Instance == this)
      Instance = null;
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
    AssetHandle<AudioClip> handle = AssetLoader.Instance.LoadHandle<AudioClip>("Audio/" + soundType);
    if (handle == null)
    {
      Debug.LogWarning($"[AudioManager] BGM resource was not found: {soundType}", this);
      return;
    }

    PlayBGM(handle.Asset, handle);
  }

  /// <summary>异步加载并播放背景音乐；被替换或停止后释放其资源句柄。</summary>
  /// <param name="soundType">Audio 资源目录下的音频键。</param>
  public async Task PlayBGMAsync(string soundType)
  {
    int requestGeneration = ++_bgmRequestGeneration;
    AssetHandle<AudioClip> handle = await AssetLoader.Instance.LoadHandleAsync<AudioClip>("Audio/" + soundType);
    if (requestGeneration != _bgmRequestGeneration || this == null)
    {
      handle?.Dispose();
      return;
    }

    if (handle == null)
    {
      Debug.LogWarning($"[AudioManager] BGM resource was not found: {soundType}", this);
      return;
    }

    PlayBGM(handle.Asset, handle);
  }

  private void PlayBGM(AudioClip clip, AssetHandle<AudioClip> newHandle = null)
  {
    _bgmRequestGeneration++;
    if (clip == null)
    {
      newHandle?.Dispose();
      return;
    }
    if (_bgm.clip == clip && _bgm.isPlaying)
    {
      newHandle?.Dispose();
      return;
    }

    if (_bgmCoroutine != null)
      StopCoroutine(_bgmCoroutine);
    _bgm.clip = clip;
    _bgmAssetHandle?.Dispose();
    _bgmAssetHandle = newHandle;
    _bgmCoroutine = StartCoroutine(FadeInBGM(clip));
  }

  private void StopBGM(float fadeTime = 1f)
  {
    _bgmRequestGeneration++;
    if (_bgmCoroutine != null)
      StopCoroutine(_bgmCoroutine);
    _bgmCoroutine = StartCoroutine(FadeOutBGM(fadeTime));
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
    _bgmCoroutine = null;
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
    _bgmAssetHandle?.Dispose();
    _bgmAssetHandle = null;
    _bgmCoroutine = null;
  }

  #endregion =====================

  #region === SFX ===

  public void PlaySfx(string soundType, bool loop = false)
  {
    AssetHandle<AudioClip> handle = AssetLoader.Instance.LoadHandle<AudioClip>("Audio/" + soundType);
    if (handle == null)
    {
      Debug.LogWarning($"[AudioManager] SFX resource was not found: {soundType}", this);
      return;
    }

    PlaySfx(handle.Asset, soundType, loop, handle);
  }

  /// <summary>异步加载并播放音效；音效结束、停止或被清理时释放其资源句柄。</summary>
  /// <param name="soundType">Audio 资源目录下的音频键。</param>
  /// <param name="loop">是否循环播放。</param>
  public async Task PlaySfxAsync(string soundType, bool loop = false)
  {
    int globalRequestGeneration = _sfxRequestGeneration;
    int typeRequestGeneration = GetSfxTypeRequestGeneration(soundType);
    AssetHandle<AudioClip> handle = await AssetLoader.Instance.LoadHandleAsync<AudioClip>("Audio/" + soundType);
    if (globalRequestGeneration != _sfxRequestGeneration
        || typeRequestGeneration != GetSfxTypeRequestGeneration(soundType)
        || this == null)
    {
      handle?.Dispose();
      return;
    }

    if (handle == null)
    {
      Debug.LogWarning($"[AudioManager] SFX resource was not found: {soundType}", this);
      return;
    }

    PlaySfx(handle.Asset, soundType, loop, handle);
  }

  // 停止播放循环音效
  public void StopSfx(string soundType)
  {
    _sfxTypeRequestGenerations[soundType] = GetSfxTypeRequestGeneration(soundType) + 1;
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
    if (_sfxAssetHandles.TryGetValue(src, out AssetHandle<AudioClip> handle))
    {
      _sfxAssetHandles.Remove(src);
      handle.Dispose();
    }
    _sfxPool.Enqueue(src);
  }

  // ReSharper disable Unity.PerformanceAnalysis
  private void PlaySfx(
    AudioClip clip,
    string soundType,
    bool loop = false,
    AssetHandle<AudioClip> assetHandle = null)
  {
    if (!clip)
    {
      assetHandle?.Dispose();
      return;
    }
    if (loop && _loopSfxList.ContainsKey(soundType))
    {
      assetHandle?.Dispose();
      return;
    }

    var sfx = AllocSfxSource();
    sfx.clip = clip;
    sfx.loop = loop;
    sfx.volume = sfxVolume;
    sfx.Play();
    _sfxList.Add(sfx); // 正在播放的音效
    if (assetHandle != null)
      _sfxAssetHandles.Add(sfx, assetHandle);

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
    if (_sfxList.Remove(sfx))
      ReleaseSfxSource(sfx);
  }

  // 停止播放全部音效
  private void StopAllSfx()
  {
    _sfxRequestGeneration++;
    // 循环音效
    foreach (var (key, src) in _loopSfxList.ToArray())
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

  private int GetSfxTypeRequestGeneration(string soundType)
  {
    return _sfxTypeRequestGenerations.TryGetValue(soundType, out int generation) ? generation : 0;
  }

  #endregion =====================
}
