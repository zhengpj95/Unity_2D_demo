using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>展示 GuideRunner 当前步骤并把按钮交互转交给 GuideModule。</summary>
public sealed class GuideView : MonoBehaviour
{
  [SerializeField] private GameObject panel;
  [SerializeField] private TMP_Text titleText;
  [SerializeField] private TMP_Text contentText;
  [SerializeField] private TMP_Text stepText;
  [SerializeField] private Button nextButton;
  [SerializeField] private TMP_Text nextButtonText;

  private GuideModule _guideModule;
  private GuideRunner _runner;
  private bool _referencesValid;

  private void Awake()
  {
    _referencesValid = ValidateReferences();
    if (!_referencesValid)
      return;

    panel.SetActive(false);
    nextButton.onClick.AddListener(OnNextClicked);
  }

  /// <summary>绑定引导模块并根据其当前状态恢复界面。</summary>
  /// <param name="module">由 ModuleManager 管理且已初始化的 GuideModule。</param>
  public void Bind(GuideModule module)
  {
    if (module == null)
      throw new ArgumentNullException(nameof(module));
    if (!_referencesValid)
      throw new InvalidOperationException("[GuideView] Required UI references are missing.");
    if (module.Runner == null)
      throw new InvalidOperationException("[GuideView] GuideModule must be initialized before binding.");

    Unbind();
    _guideModule = module;
    _runner = module.Runner;
    _runner.OnStepChanged += OnStepChanged;
    _runner.OnStateChanged += OnStateChanged;

    if ((_runner.State == GuideState.Running || _runner.State == GuideState.Paused)
        && _runner.CurrentStep != null)
    {
      OnStepChanged(_runner.CurrentStep, _runner.CurrentIndex, _runner.Config.Steps.Count);
    }

    OnStateChanged(_runner.State);
  }

  /// <summary>解除步骤与状态事件订阅，并清除模块引用。</summary>
  public void Unbind()
  {
    if (_runner != null)
    {
      _runner.OnStepChanged -= OnStepChanged;
      _runner.OnStateChanged -= OnStateChanged;
    }

    _runner = null;
    _guideModule = null;
    if (panel != null)
      panel.SetActive(false);
  }

  private void OnDestroy()
  {
    Unbind();
    if (nextButton != null)
      nextButton.onClick.RemoveListener(OnNextClicked);
  }

  private bool ValidateReferences()
  {
    if (panel != null
        && titleText != null
        && contentText != null
        && stepText != null
        && nextButton != null
        && nextButtonText != null)
    {
      return true;
    }

    Debug.LogError("[GuideView] Assign panel, title/content/step text, next button, and button text in the Inspector.", this);
    return false;
  }

  private void OnStepChanged(GuideStepData step, int index, int total)
  {
    if (step == null || index < 0 || total <= index)
    {
      Debug.LogError("[GuideView] Received invalid step data from GuideRunner.", this);
      return;
    }

    titleText.text = step.Title ?? string.Empty;
    contentText.text = step.Content ?? string.Empty;
    stepText.text = $"{index + 1} / {total}";
    nextButtonText.text = index == total - 1 ? "完成" : "下一步";
    panel.SetActive(true);
  }

  private void OnStateChanged(GuideState state)
  {
    bool isActive = state == GuideState.Running || state == GuideState.Paused;
    panel.SetActive(isActive);
    nextButton.interactable = state == GuideState.Running;
  }

  private void OnNextClicked()
  {
    _guideModule?.Next();
  }
}
