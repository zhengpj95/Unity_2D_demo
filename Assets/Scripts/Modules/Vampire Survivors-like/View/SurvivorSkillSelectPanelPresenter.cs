using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VampireSurvivorsLike;

/// <summary>技能弹窗的运行时参数；Presenter 只将选择结果回传给 GameplayController。</summary>
public sealed class SurvivorSkillSelectArgs
{
  /// <summary>本次弹窗要展示的动态升级候选。</summary>
  public UpgradeConfig[] Options { get; }
  /// <summary>玩家选择后回传给 GameplayController 的回调。</summary>
  public Action<UpgradeConfig> OnSelected { get; }

  /// <summary>创建一次升级弹窗的显示数据和选择回调。</summary>
  public SurvivorSkillSelectArgs(UpgradeConfig[] options, Action<UpgradeConfig> onSelected)
  {
    Options = options;
    OnSelected = onSelected;
  }
}

public class SurvivorSkillSelectPanelPresenter : BasePresenter
{
  private const float CountdownDuration = 10f;

  public override UILayerIndex Layer => UILayerIndex.Model;
  public override string PrefabPath => "Prefabs/SurvivorSkillSelectPanel";

  private SurvivorSkillSelectPanelView _view;
  private float _remainingTime;
  private UpgradeConfig[] _options;
  private Action<UpgradeConfig> _onSelected;

  public override void OnInit(UIView view)
  {
    base.OnInit(view);
    _view = view as SurvivorSkillSelectPanelView;

    AddClickListener(_view?.skill0, () => SelectSkill(0));
    AddClickListener(_view?.skill1, () => SelectSkill(1));
    AddClickListener(_view?.skill2, () => SelectSkill(2));
  }

  public override void OnOpen(object args = null)
  {
    base.OnOpen(args);

    // 每次打开都替换候选，避免连续升级沿用上一轮数据。
    SurvivorSkillSelectArgs selectArgs = args as SurvivorSkillSelectArgs;
    _options = selectArgs?.Options;
    _onSelected = selectArgs?.OnSelected;
    if (_onSelected == null)
      Debug.LogWarning("[SurvivorSkillSelectPanelPresenter] Missing selection callback.");

    _remainingTime = CountdownDuration;
    NeedUpdate = true;
    UpdateSkillItems();
    UpdateCountdownText();
  }

  public override void OnClose()
  {
    NeedUpdate = false;
    _options = null;
    _onSelected = null;
    base.OnClose();
  }

  public override void Update()
  {
    if (_remainingTime > 0f)
    {
      _remainingTime -= Time.unscaledDeltaTime;
      UpdateCountdownText();
      return;
    }

    SelectSkill(0);
  }

  private void SelectSkill(int skillIndex)
  {
    if (_options == null || skillIndex < 0 || skillIndex >= _options.Length || _options[skillIndex] == null)
    {
      Debug.LogWarning($"[SurvivorSkillSelectPanelPresenter] Invalid skill index: {skillIndex}");
      return;
    }

    // HidePresenter 会同步调用 OnClose 并清空 _options，必须先缓存本次选择的对象。
    UpgradeConfig selectedUpgrade = _options[skillIndex];
    Action<UpgradeConfig> onSelected = _onSelected;
    UIManager.Instance.HidePresenter(this);
    onSelected?.Invoke(selectedUpgrade);
  }

  private void UpdateSkillItems()
  {
    if (_view == null || _options == null) return;

    for (int i = 0; i < 3; i++)
    {
      Transform skillItem = _view.transform.Find($"SkillItem{i}");
      if (skillItem == null) continue;

      // 候选不足 3 个时隐藏多余卡片，不能用无效配置填充。
      UpgradeConfig upgrade = i < _options.Length ? _options[i] : null;
      skillItem.gameObject.SetActive(upgrade != null);
      if (upgrade == null) continue;

      Image icon = skillItem.Find("Image")?.GetComponent<Image>();
      if (icon != null)
      {
        icon.sprite = upgrade.Icon;
        icon.enabled = upgrade.Icon != null;
      }

      TMP_Text nameText = skillItem.Find("Text")?.GetComponent<TMP_Text>();
      if (nameText != null)
      {
        string description = upgrade.Description;
        nameText.text = string.IsNullOrWhiteSpace(description)
          ? upgrade.GetDisplayTitle()
          : $"{upgrade.GetDisplayTitle()}\n{description}";
      }
    }
  }

  private void UpdateCountdownText()
  {
    if (_view?.timerText != null)
      _view.timerText.text = $"倒计时关闭：{Mathf.Max(0f, _remainingTime):F0}秒";
  }
}
