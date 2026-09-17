/// <summary>
/// 项目 EventBus 事件名集中定义。
/// 常量名称和事件键均使用“业务前缀 + 事件语义”的大写格式；调用方必须使用此处常量，避免散落的字符串在重构时失配。
/// </summary>
public static class EventDefine
{
  /// <summary>请求打开通用提示弹窗，数据类型为 AlertTipsPanelArgs。</summary>
  public const string MISC_OPEN_ALERT = "MISC_OPEN_ALERT";

  /// <summary>水果分数发生变化。</summary>
  public const string FROG_SCORE_CHANGED = "FROG_SCORE_CHANGED";

  /// <summary>当前关卡发生变化。</summary>
  public const string FROG_LEVEL_CHANGED = "FROG_LEVEL_CHANGED";

  /// <summary>玩家生命值发生变化。</summary>
  public const string FROG_HEALTH_CHANGED = "FROG_HEALTH_CHANGED";

  /// <summary>请求复活玩家。</summary>
  public const string FROG_PLAYER_REVIVE = "FROG_PLAYER_REVIVE";

  /// <summary>玩家生命值显示需要刷新。</summary>
  public const string RPG_PLAYER_HEALTH_CHANGED = "RPG_PLAYER_HEALTH_CHANGED";

  /// <summary>玩家死亡，数据类型为 bool，表示 GameOver 面板是否显示。</summary>
  public const string RPG_GAME_OVER = "RPG_GAME_OVER";

  /// <summary>登录 Command 的测试事件，数据类型为 string。</summary>
  public const string TEST_LOGIN_COMMAND = "TEST_LOGIN_COMMAND";
}
