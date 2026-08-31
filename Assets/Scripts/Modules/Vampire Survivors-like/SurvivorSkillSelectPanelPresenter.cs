using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VampireSurvivorsLike;

/// <summary>技能弹窗的运行时参数；Presenter 只将选择结果回传给 GameplayController。</summary>
public sealed class SurvivorSkillSelectArgs
{
  public Action<WeaponSO> OnSelected { get; }

  public SurvivorSkillSelectArgs(Action<WeaponSO> onSelected)
  {
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
  private Action<WeaponSO> _onSelected;

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

    _onSelected = (args as SurvivorSkillSelectArgs)?.OnSelected;
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
    if (_view?.weapons == null || skillIndex < 0 || skillIndex >= _view.weapons.Length || _view.weapons[skillIndex] == null)
    {
      Debug.LogWarning($"[SurvivorSkillSelectPanelPresenter] Invalid skill index: {skillIndex}");
      return;
    }

    Action<WeaponSO> onSelected = _onSelected;
    UIManager.Instance.HidePresenter(this);
    onSelected?.Invoke(_view.weapons[skillIndex]);
  }

  private void UpdateSkillItems()
  {
    if (_view?.weapons == null) return;

    for (int i = 0; i < _view.weapons.Length && i < 3; i++)
    {
      WeaponSO weapon = _view.weapons[i];
      Transform skillItem = _view.transform.Find($"SkillItem{i}");
      if (skillItem == null || weapon == null) continue;

      Image icon = skillItem.Find("Image")?.GetComponent<Image>();
      if (icon != null) icon.sprite = weapon.icon;

      TMP_Text nameText = skillItem.Find("Text")?.GetComponent<TMP_Text>();
      if (nameText != null) nameText.text = weapon.weaponName;
    }
  }

  private void UpdateCountdownText()
  {
    if (_view?.timerText != null)
      _view.timerText.text = $"倒计时关闭：{Mathf.Max(0f, _remainingTime):F0}秒";
  }
}
