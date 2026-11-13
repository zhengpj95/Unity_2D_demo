/// <summary>Misc 模块的界面类型。</summary>
public enum MiscViewType
{
  AlertTips = 1,
  Confirm = 2,
}

/// <summary>Survivor 模块的界面类型。</summary>
public enum SurvivorViewType
{
  Main = 1,
  SkillSelect = 2,
  GameOver = 3,
  // 追加在末尾，避免改变已有 ViewType 的枚举值和 Presenter 缓存语义。
  Home = 4,
}

/// <summary>Guide 模块的界面类型。</summary>
public enum GuideViewType
{
  GuidePanel = 1,
}
