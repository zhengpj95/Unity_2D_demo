using System;
using System.Threading.Tasks;
using UnityEngine;

public class SurvivorMainPresenter : BasePresenter
{
  public override string PrefabPath => "Prefabs/Survivor/View/SurvivorMain";

  private const string JoystickPrefabPath = "Prefabs/Joystick/JoystickPanel";

  private SurvivorMainView _view;
  private SurvivorModule _survivorModule;
  private SurvivorProxy _proxy;
  private Joystick _joystick;
  private AssetHandle<GameObject> _joystickHandle;
  private Action<Vector2> _joystickCallback;

  public override void OnInit(UIView view)
  {
    base.OnInit(view);
    _view = view as SurvivorMainView;
    _survivorModule = ModuleManager.Instance.GetModule<SurvivorModule>(ModuleName.Survivor);
    if (_survivorModule == null)
      Debug.LogError("[SurvivorMainPresenter] 未找到 SurvivorModule，摇杆输入无法转交 Hero。");
    else
      _proxy = _survivorModule.GetProxy<SurvivorProxy>();

    // Presenter 自行订阅数据变化，避免 Module 或 Controller 直接调用本类方法。
    On(EventDefine.SURVIVOR_MODEL_CHANGED, OnModelChanged);

    // 摇杆是独立预制体，由本 Presenter 统一加载、挂载和释放。
    _ = LoadJoystickAsync();
  }

  /// <summary>异步加载 JoystickPanel 并挂到 HUD 根节点下；Presenter 已销毁时只释放句柄。</summary>
  private async Task LoadJoystickAsync()
  {
    try
    {
      AssetHandle<GameObject> handle = await AssetLoader.Instance.LoadHandleAsync<GameObject>(JoystickPrefabPath);
      if (handle == null)
      {
        Debug.LogError("[SurvivorMainPresenter] 摇杆预制体加载失败，资源键=" + JoystickPrefabPath);
        return;
      }

      // 迟到加载保护：Presenter 销毁后 View 为 null，不能继续创建界面对象。
      if (_view == null)
      {
        handle.Dispose();
        return;
      }

      _joystickHandle = handle;
      GameObject joystickObject = UnityEngine.Object.Instantiate(handle.Asset, _view.transform, false);
      joystickObject.name = "JoystickPanel";

      // 预制体没有序列化脚本组件，运行时挂载即可获得拖拽输入，无需改动资源文件。
      _joystick = joystickObject.GetComponent<Joystick>();
      if (_joystick == null)
        _joystick = joystickObject.AddComponent<Joystick>();

      _joystickCallback = OnJoystickDirectionChanged;
      _joystick.DirectionChanged += _joystickCallback;
    }
    catch (Exception exception)
    {
      Debug.LogError($"[SurvivorMainPresenter] 加载摇杆异常，资源键={JoystickPrefabPath}，Error={exception}");
    }
  }

  /// <summary>把摇杆方向交给 SurvivorModule 的输入通道；Presenter 不直接持有或修改 Hero。</summary>
  private void OnJoystickDirectionChanged(Vector2 direction)
  {
    if (_survivorModule != null)
      _survivorModule.SetMoveInput(direction);
  }

  /// <summary>收到数据变化事件后自行刷新；Presenter 不接收 Model 参数，数据来源始终是 Proxy。</summary>
  private void OnModelChanged(EventContext context)
  {
    RefreshView();
  }

  /// <summary>从缓存恢复显示时刷新一次，避免隐藏期间发生的数据变化没有反映到界面。</summary>
  public override void OnShow()
  {
    base.OnShow();
    RefreshView();
  }

  /// <summary>界面隐藏时归中摇杆并清零输入，避免升级/结算面板期间玩家继续移动。</summary>
  public override void OnClose()
  {
    base.OnClose();
    _joystick?.ResetInput();
    if (_survivorModule != null)
      _survivorModule.SetMoveInput(Vector2.zero);
  }

  /// <summary>销毁时退订摇杆事件，并在 View 销毁后释放预制体资源租约。</summary>
  public override void OnDestroy()
  {
    if (_joystick != null && _joystickCallback != null)
      _joystick.DirectionChanged -= _joystickCallback;
    _joystickCallback = null;
    _joystick = null;

    base.OnDestroy();

    _joystickHandle?.Dispose();
    _joystickHandle = null;
    _survivorModule = null;
    _proxy = null;
  }

  /// <summary>从 Proxy 读取当前 Model 快照并刷新显示；Presenter 只在需要显示时访问数据，不持有业务数据。</summary>
  private void RefreshView()
  {
    if (_view == null || _proxy == null)
      return;

    SurvivorModel model = _proxy.Model;
    if (model == null)
      return;

    UpdateHp(model.CurrentHealth, model.MaxHealth);
    UpdateExp(model.CurrentExp, GetRequiredExp(model.Level), model.Level);
    UpdateEnemyKillCount(model.KillCount);
    UpdateInventory(model.GemCount, model.CoinCount);
  }

  private void UpdateHp(int currentHealth, int maxHealth)
  {
    if (_view.hpSlider != null)
    {
      _view.hpSlider.maxValue = maxHealth;
      _view.hpSlider.value = currentHealth;
    }

    if (_view.hpValueText != null)
      _view.hpValueText.text = $"{Mathf.Max(0, currentHealth)} / {maxHealth}";
  }

  private void UpdateExp(int currentExp, int requiredExp, int level)
  {
    if (_view.expSlider != null)
      _view.expSlider.value = requiredExp <= 0 ? 0f : Mathf.Clamp01((float)currentExp / requiredExp);

    if (_view.expLevelTxt != null)
      _view.expLevelTxt.text = $"Lv.{level}";
  }

  private void UpdateEnemyKillCount(int killCount)
  {
    if (_view.killCountText != null)
      _view.killCountText.text = killCount.ToString();
  }

  private void UpdateInventory(int gemCount, int coinCount)
  {
    if (_view.gemCountText != null)
      _view.gemCountText.text = gemCount.ToString();

    if (_view.coinCountText != null)
      _view.coinCountText.text = coinCount.ToString();
  }

  private static int GetRequiredExp(int level)
  {
    const int baseValue = 20;
    const int growth = 5;
    return baseValue * level + growth * level * level;
  }
}
