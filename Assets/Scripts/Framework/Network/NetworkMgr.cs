using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Threading.Tasks;
using Google.Protobuf;
using System.Collections.Generic;

/// <summary>
/// 网络连接与消息分发管理器。
/// Dispatcher 在整个 NetworkMgr 生命周期内复用，断线重连只替换 Socket，不会丢失协议回调。
/// </summary>
public class NetworkMgr : Singleton<NetworkMgr>
{
  private SocketMgr _socketMgr;
  private MessageDispatcher _dispatcher;
  private readonly List<Action> _pendingRegistrations = new();
  private readonly Dictionary<uint, int> _commandVersions = new();
  private string _url;
  private Task _reconnectTask;
  private bool _manualClose;
  private bool _connectionFailurePromptShown;

  /// <summary>是否已连接。</summary>
  public bool IsConnected => _socketMgr != null && _socketMgr.IsConnected;

  /// <summary>自动重连最大尝试次数；小于 0 表示持续重连。</summary>
  public int MaxReconnectAttempts { get; set; } = 3;

  /// <summary>两次重连尝试之间的间隔（秒）。</summary>
  public float ReconnectDelaySeconds { get; set; } = 2f;

  public event Action Connected;
  public event Action Disconnected;

  private void FlushPendingRegistrations()
  {
    if (_dispatcher == null) return;

    foreach (Action registration in _pendingRegistrations) registration();
    _pendingRegistrations.Clear();
  }

  public void RegisterHandler<T>(uint cmd, Action<T> handler) where T : IMessage<T>
  {
    if (handler == null) throw new ArgumentNullException(nameof(handler));

    int commandVersion = GetNextCommandVersion(cmd);
    if (_dispatcher == null)
    {
      _pendingRegistrations.Add(() =>
      {
        if (IsCurrentCommandVersion(cmd, commandVersion)) _dispatcher.Register(cmd, handler);
      });
      return;
    }

    _dispatcher.Register(cmd, handler);
  }

  public bool UnregisterHandler(uint cmd)
  {
    GetNextCommandVersion(cmd);
    return _dispatcher != null && _dispatcher.Unregister(cmd);
  }

  /// <summary>
  /// 连接服务器。重复调用不会创建重复连接；后续断线会自动重连。
  /// </summary>
  public Task Connect(string url)
  {
    if (string.IsNullOrWhiteSpace(url))
    {
      throw new ArgumentException("URL cannot be empty.", nameof(url));
    }

    _url = url;
    _manualClose = false;
    _connectionFailurePromptShown = false;
    if (IsConnected)
    {
      return Task.CompletedTask;
    }

    return ConnectSocketAsync(true);
  }

  private async Task<bool> ConnectSocketAsync(bool scheduleReconnectOnFailure)
  {
    if (IsConnected) return true;

    ProtoRegister.RegisterAll();
    _dispatcher ??= new MessageDispatcher();
    FlushPendingRegistrations();

    SocketMgr oldSocket = _socketMgr;
    if (oldSocket != null)
    {
      oldSocket.Dispose();
    }

    SocketMgr socket = new SocketMgr();
    _socketMgr = socket;
    socket.OnMessage += ReceiveMessage;
    socket.OnConnected += () => HandleSocketConnected(socket);
    socket.OnClosed += code => HandleSocketClosed(socket, code);
    socket.OnError += error => HandleSocketError(socket, error);

    await socket.Connect(_url);
    bool connected = ReferenceEquals(socket, _socketMgr) && socket.IsConnected;
    if (!connected && scheduleReconnectOnFailure) ScheduleReconnect();
    return connected;
  }

  private void HandleSocketConnected(SocketMgr socket)
  {
    if (!ReferenceEquals(socket, _socketMgr)) return;
    Debug.Log("[NetworkMgr] Connected.");
    _connectionFailurePromptShown = false;
    Connected?.Invoke();
  }

  private void HandleSocketClosed(SocketMgr socket, NativeWebSocket.WebSocketCloseCode code)
  {
    if (!ReferenceEquals(socket, _socketMgr)) return;
    Debug.LogWarning($"[NetworkMgr] Connection closed: {code}");
    Disconnected?.Invoke();
    ScheduleReconnect();
  }

  private void HandleSocketError(SocketMgr socket, string error)
  {
    if (!ReferenceEquals(socket, _socketMgr)) return;
    Debug.LogWarning($"[NetworkMgr] Socket error: {error}");
    ScheduleReconnect();
  }

  private void ScheduleReconnect()
  {
    if (_manualClose || string.IsNullOrWhiteSpace(_url) || IsConnected) return;
    if (_reconnectTask != null && !_reconnectTask.IsCompleted) return;

    _reconnectTask = ReconnectLoopAsync();
  }

  private async Task ReconnectLoopAsync()
  {
    int attempt = 0;
    while (!_manualClose && !IsConnected && (MaxReconnectAttempts < 0 || attempt < MaxReconnectAttempts))
    {
      attempt++;
      int delayMilliseconds = Mathf.Max(0, Mathf.RoundToInt(ReconnectDelaySeconds * 1000f));
      if (delayMilliseconds > 0) await Task.Delay(delayMilliseconds);
      if (_manualClose || IsConnected) break;

      Debug.Log($"[NetworkMgr] Reconnecting ({attempt}/{MaxReconnectAttempts})...");
      if (await ConnectSocketAsync(false)) return;
    }

    if (!IsConnected && !_manualClose)
    {
      Debug.LogError("[NetworkMgr] Reconnect attempts exhausted.");
      if (!_connectionFailurePromptShown)
      {
        _connectionFailurePromptShown = true;
        EventBus.Dispatch(UIEventDefine.MISC_OPEN_ALERT.ToString(), new AlertTipsPanelArgs("连接失败", "网络连接失败，请刷新游戏后重试。", ReloadCurrentScene));
      }
    }
  }

  /// <summary>
  /// 重连耗尽后只显示一次提示。确认按钮会重新加载当前场景，重新建立游戏状态。
  /// </summary>
  private static void ReloadCurrentScene()
  {
    UnityEngine.SceneManagement.Scene activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
    if (activeScene.buildIndex >= 0) UnityEngine.SceneManagement.SceneManager.LoadScene(activeScene.buildIndex);
  }

  public async Task Send<T>(uint cmd, T message) where T : IMessage
  {
    if (!IsConnected)
    {
      ScheduleReconnect();
      return;
    }

    byte[] body = ProtoMgr.Encode(message);
    byte[] packet = PacketCodec.Encode(cmd, body);
    Debug.Log($"[发送协议] Cmd={cmd}, message: {message}");
    await _socketMgr.Send(packet);
  }

  public void ReceiveMessage(byte[] data)
  {
    if (_dispatcher == null) return;

    Packet packet = PacketCodec.Decode(data);
    IMessage message = ProtoMgr.Decode(packet.Cmd, packet.Body);
    Debug.Log($"[接收协议] Cmd={packet.Cmd}, message: {message}");
    _dispatcher.Dispatch(packet.Cmd, message);
  }

  public async Task Close()
  {
    _manualClose = true;
    _url = null;
    if (_socketMgr != null) await _socketMgr.Close();
  }

  public void Dispose()
  {
    _manualClose = true;
    _url = null;
    _socketMgr?.Dispose();
    _socketMgr = null;
    _reconnectTask = null;
  }

  private int GetNextCommandVersion(uint cmd)
  {
    int version = _commandVersions.TryGetValue(cmd, out int currentVersion) ? currentVersion + 1 : 1;
    _commandVersions[cmd] = version;
    return version;
  }

  private bool IsCurrentCommandVersion(uint cmd, int version)
  {
    return _commandVersions.TryGetValue(cmd, out int currentVersion) && currentVersion == version;
  }
}
