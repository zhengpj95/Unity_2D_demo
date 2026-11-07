/// <summary>Misc 模块的界面类型。</summary>
public enum MiscViewType
{
  AlertTips,
  Confirm,
}

/// <summary>Survivor 模块的界面类型。</summary>
public enum SurvivorViewType
{
  Main,
  SkillSelect,
  GameOver,
  // 追加在末尾，避免改变已有 ViewType 的枚举值和 Presenter 缓存语义。
  Home,
}
