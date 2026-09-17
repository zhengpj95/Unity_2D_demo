/// <summary>
/// 项目 EventBus 事件名集中定义。
/// 常量名称使用“业务前缀 + 事件语义”的大写格式，并通过自增数字生成运行期唯一事件 ID。
/// </summary>
public static class EventDefine
{
  // EventBus 事件仅在当前进程内使用；从 1000 起按声明顺序自动分配，新增事件必须追加在末尾以保持当前编号顺序。
  private static int _nextId = 1000;

  private static int NextId()
  {
    return _nextId++;
  }

  /// <summary>请求打开通用提示弹窗，数据类型为 AlertTipsPanelArgs。</summary>
  public static readonly int MISC_OPEN_ALERT = NextId();

  /// <summary>水果分数发生变化。</summary>
  public static readonly int FROG_SCORE_CHANGED = NextId();

  /// <summary>当前关卡发生变化。</summary>
  public static readonly int FROG_LEVEL_CHANGED = NextId();

  /// <summary>玩家生命值发生变化。</summary>
  public static readonly int FROG_HEALTH_CHANGED = NextId();

  /// <summary>请求复活玩家。</summary>
  public static readonly int FROG_PLAYER_REVIVE = NextId();

  /// <summary>玩家生命值显示需要刷新。</summary>
  public static readonly int RPG_PLAYER_HEALTH_CHANGED = NextId();

  /// <summary>玩家死亡，数据类型为 bool，表示 GameOver 面板是否显示。</summary>
  public static readonly int RPG_GAME_OVER = NextId();

  /// <summary>登录 Command 的测试事件，数据类型为 string。</summary>
  public static readonly int TEST_LOGIN_COMMAND = NextId();
}
