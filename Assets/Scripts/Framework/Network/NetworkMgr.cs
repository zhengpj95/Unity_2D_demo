using UnityEngine;
using System;
using System.Threading.Tasks;
using Google.Protobuf;
using System.Collections.Generic;

/// <summary>
/// 网络连接状态。状态只描述客户端连接流程，不承载具体业务登录或鉴权状态。
/// </summary>
public enum NetworkConnectionState
{
  Disconnected,
  Connecting,
  Connected,
  Reconnecting,
  Failed,
  Closing
}

/// <summary>一次消息发送到传输层后的结果；Sent 仅表示已交给 WebSocket，不表示服务端已处理或响应。</summary>
public enum NetworkSendResult
{
  Sent,
  NotConnected,
  Reconnecting,
  EncodeFailed,
  TransportFailed
}

/// <summary>请求响应 API 的完成状态。</summary>
public enum NetworkRequestStatus
{
  Succeeded,
  SendFailed,
  TimedOut,
  Cancelled,
  ResponseTypeMismatch
}

/// <summary>连接重试耗尽时提供给业务层的失败信息。</summary>
public readonly struct NetworkConnectionFailure
{
  public string Reason { get; }
  public int AttemptCount { get; }

  internal NetworkConnectionFailure(string reason, int attemptCount)
  {
    Reason = reason;
    AttemptCount = attemptCount;
  }
}

/// <summary>一次带超时的请求结果。</summary>
public readonly struct NetworkRequestResult<TResponse> where TResponse : IMessage
{
  public NetworkRequestStatus Status { get; }
  public NetworkSendResult SendResult { get; }
  public TResponse Response { get; }

  internal NetworkRequestResult(NetworkRequestStatus status, NetworkSendResult sendResult, TResponse response = default)
  {
    Status = status;
    SendResult = sendResult;
    Response = response;
  }
}

/// <summary>
/// 网络连接与消息分发管理器。
/// Dispatcher 在整个 NetworkMgr 生命周期内复用，断线重连只替换 Socket，不会丢失协议回调。
/// </summary>
public class NetworkMgr : Singleton<NetworkMgr>
{
  private sealed class PendingRequest
  {
    public readonly TaskCompletionSource<IMessage> Completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
  }

  private SocketMgr _socketMgr;
  private MessageDispatcher _dispatcher;
  private readonly List<Action> _pendingRegistrations = new();
  private readonly Dictionary<uint, int> _commandVersions = new();
  private readonly Dictionary<uint, PendingRequest> _pendingRequests = new();
  private string _url;
  private Task<bool> _connectTask;
  private Task _reconnectTask;
  private bool _manualClose;
  private bool _hasEstablishedConnection;
  private string _lastConnectionError;

  /// <summary>是否已连接。</summary>
  public bool IsConnected => _socketMgr != null && _socketMgr.IsConnected;

  /// <summary>当前网络连接状态，供业务层决定加载、提示或降级策略。</summary>
  public NetworkConnectionState ConnectionState { get; private set; } = NetworkConnectionState.Disconnected;

  /// <summary>自动重连最大尝试次数；小于 0 表示持续重连。</summary>
  public int MaxReconnectAttempts { get; set; } = 3;

  /// <summary>两次重连尝试之间的间隔（秒）。</summary>
  public float ReconnectDelaySeconds { get; set; } = 2f;

  public event Action Connected;
  public event Action Disconnected;
  /// <summary>连接状态变化通知。首次连接失败、重连与重连耗尽都会触发。</summary>
  public event Action<NetworkConnectionState> ConnectionStateChanged;
  /// <summary>重连耗尽通知；由业务层决定提示内容、场景跳转或离线降级。</summary>
  public event Action<NetworkConnectionFailure> ConnectionFailed;

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

    if (_connectTask != null && !_connectTask.IsCompleted)
    {
      if (!string.Equals(_url, url, StringComparison.Ordinal))
        throw new InvalidOperationException("Cannot change the server URL while a connection is in progress. Close the current connection first.");

      return _connectTask;
    }

    if (_reconnectTask != null && !_reconnectTask.IsCompleted)
    {
      if (!string.Equals(_url, url, StringComparison.Ordinal))
        throw new InvalidOperationException("Cannot change the server URL while reconnecting. Close the current connection first.");

      return _reconnectTask;
    }

    if (IsConnected && !string.Equals(_url, url, StringComparison.Ordinal))
      throw new InvalidOperationException("Cannot change the server URL while connected. Close the current connection first.");

    bool isNewConnection = !string.Equals(_url, url, StringComparison.Ordinal) || _manualClose;
    if (isNewConnection)
    {
      _hasEstablishedConnection = false;
    }

    _url = url;
    _manualClose = false;
    _lastConnectionError = null;
    if (IsConnected)
    {
      SetConnectionState(NetworkConnectionState.Connected);
      return Task.CompletedTask;
    }

    SetConnectionState(NetworkConnectionState.Connecting);
    _connectTask = ConnectSocketAsync();
    return _connectTask;
  }

  private async Task<bool> ConnectSocketAsync()
  {
    if (IsConnected) return true;

    ProtoRegister.RegisterAll();
    _dispatcher ??= new MessageDispatcher();
    FlushPendingRegistrations();

    await CloseAndDisposeCurrentSocketAsync();
    if (_manualClose || string.IsNullOrWhiteSpace(_url))
      return false;

    SocketMgr socket = new SocketMgr();
    _socketMgr = socket;
    socket.OnMessage += ReceiveMessage;
    socket.OnConnected += () => HandleSocketConnected(socket);
    socket.OnClosed += code => HandleSocketClosed(socket, code);
    socket.OnError += error => HandleSocketError(socket, error);

    await socket.Connect(_url);
    bool connected = ReferenceEquals(socket, _socketMgr) && socket.IsConnected;
    return connected;
  }

  private void HandleSocketConnected(SocketMgr socket)
  {
    if (!ReferenceEquals(socket, _socketMgr)) return;
    Debug.Log("[NetworkMgr] Connected.");
    _hasEstablishedConnection = true;
    _lastConnectionError = null;
    SetConnectionState(NetworkConnectionState.Connected);
    Connected?.Invoke();
  }

  private void HandleSocketClosed(SocketMgr socket, NativeWebSocket.WebSocketCloseCode code)
  {
    if (!ReferenceEquals(socket, _socketMgr)) return;
    Debug.LogWarning($"[NetworkMgr] Connection closed: {code}");

    if (_manualClose)
    {
      SetConnectionState(NetworkConnectionState.Disconnected);
      return;
    }

    _lastConnectionError = $"Socket closed: {code}";
    if (_hasEstablishedConnection)
      Disconnected?.Invoke();
    ScheduleReconnect();
  }

  private void HandleSocketError(SocketMgr socket, string error)
  {
    if (!ReferenceEquals(socket, _socketMgr)) return;
    Debug.LogWarning($"[NetworkMgr] Socket error: {error}");
    _lastConnectionError = error;
    ScheduleReconnect();
  }

  private void ScheduleReconnect()
  {
    if (_manualClose || string.IsNullOrWhiteSpace(_url) || IsConnected) return;
    if (_reconnectTask != null && !_reconnectTask.IsCompleted) return;

    SetConnectionState(NetworkConnectionState.Reconnecting);
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
      if (await ConnectSocketAsync()) return;
    }

    if (!IsConnected && !_manualClose)
    {
      Debug.LogError("[NetworkMgr] Reconnect attempts exhausted.");
      SetConnectionState(NetworkConnectionState.Failed);
      ConnectionFailed?.Invoke(new NetworkConnectionFailure(_lastConnectionError, attempt));
    }
  }

  /// <summary>
  /// 发送 protobuf 消息并返回传输结果。Sent 不表示服务端已处理；需要响应与超时控制时使用 Request。
  /// </summary>
  public async Task<NetworkSendResult> Send<T>(uint cmd, T message) where T : IMessage
  {
    if (!IsConnected)
    {
      if (!_manualClose && !string.IsNullOrWhiteSpace(_url))
        ScheduleReconnect();

      return ConnectionState == NetworkConnectionState.Reconnecting
        ? NetworkSendResult.Reconnecting
        : NetworkSendResult.NotConnected;
    }

    try
    {
      byte[] body = ProtoMgr.Encode(message);
      byte[] packet = PacketCodec.Encode(cmd, body);
      SocketMgr socket = _socketMgr;
      if (socket == null)
        return NetworkSendResult.NotConnected;

      Debug.Log($"[发送协议] Cmd={cmd}, message: {message}");
      return await socket.Send(packet) ? NetworkSendResult.Sent : NetworkSendResult.TransportFailed;
    }
    catch (Exception exception)
    {
      Debug.LogError($"[NetworkMgr] Encode or send failed. Cmd={cmd}, Error={exception}");
      return NetworkSendResult.EncodeFailed;
    }
  }

  /// <summary>
  /// 发送请求并等待指定响应 cmd。同一响应协议由唯一业务职责方处理，因此同一 responseCmd 同时只允许一个等待中的请求。
  /// </summary>
  /// <typeparam name="TRequest">请求 protobuf 类型。</typeparam>
  /// <typeparam name="TResponse">响应 protobuf 类型。</typeparam>
  /// <param name="requestCmd">请求协议号。</param>
  /// <param name="request">请求内容。</param>
  /// <param name="responseCmd">预期响应协议号。</param>
  /// <param name="timeoutSeconds">等待响应的超时秒数，必须大于零。</param>
  public async Task<NetworkRequestResult<TResponse>> Request<TRequest, TResponse>(
    uint requestCmd,
    TRequest request,
    uint responseCmd,
    float timeoutSeconds)
    where TRequest : IMessage
    where TResponse : IMessage
  {
    if (timeoutSeconds <= 0f)
      throw new ArgumentOutOfRangeException(nameof(timeoutSeconds), timeoutSeconds, "Timeout must be greater than zero.");

    if (_pendingRequests.ContainsKey(responseCmd))
      throw new InvalidOperationException($"A request is already waiting for response cmd: {responseCmd}");

    PendingRequest pendingRequest = new();
    _pendingRequests.Add(responseCmd, pendingRequest);

    NetworkSendResult sendResult = await Send(requestCmd, request);
    if (sendResult != NetworkSendResult.Sent)
    {
      RemovePendingRequest(responseCmd, pendingRequest);
      return new NetworkRequestResult<TResponse>(NetworkRequestStatus.SendFailed, sendResult);
    }

    Task timeoutTask = Task.Delay(TimeSpan.FromSeconds(timeoutSeconds));
    Task completedTask = await Task.WhenAny(pendingRequest.Completion.Task, timeoutTask);
    if (completedTask == timeoutTask)
    {
      RemovePendingRequest(responseCmd, pendingRequest);
      return new NetworkRequestResult<TResponse>(NetworkRequestStatus.TimedOut, sendResult);
    }

    try
    {
      IMessage response = await pendingRequest.Completion.Task;
      if (response is TResponse typedResponse)
        return new NetworkRequestResult<TResponse>(NetworkRequestStatus.Succeeded, sendResult, typedResponse);

      return new NetworkRequestResult<TResponse>(NetworkRequestStatus.ResponseTypeMismatch, sendResult);
    }
    catch (TaskCanceledException)
    {
      return new NetworkRequestResult<TResponse>(NetworkRequestStatus.Cancelled, sendResult);
    }
  }

  public void ReceiveMessage(byte[] data)
  {
    if (_dispatcher == null) return;

    Packet packet = PacketCodec.Decode(data);
    IMessage message = ProtoMgr.Decode(packet.Cmd, packet.Body);
    Debug.Log($"[接收协议] Cmd={packet.Cmd}, message: {message}");
    CompletePendingRequest(packet.Cmd, message);
    _dispatcher.Dispatch(packet.Cmd, message);
  }

  public async Task Close()
  {
    _manualClose = true;
    _url = null;
    _hasEstablishedConnection = false;
    SetConnectionState(NetworkConnectionState.Closing);
    CancelPendingRequests();
    await CloseAndDisposeCurrentSocketAsync();
    SetConnectionState(NetworkConnectionState.Disconnected);
  }

  public void Dispose()
  {
    _manualClose = true;
    _url = null;
    _hasEstablishedConnection = false;
    CancelPendingRequests();
    _socketMgr?.Dispose();
    _socketMgr = null;
    _connectTask = null;
    _reconnectTask = null;
    SetConnectionState(NetworkConnectionState.Disconnected);
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

  /// <summary>收到响应后完成对应的等待请求；响应仍会继续分发给正常业务 Handler。</summary>
  private void CompletePendingRequest(uint responseCmd, IMessage response)
  {
    if (!_pendingRequests.TryGetValue(responseCmd, out PendingRequest pendingRequest))
      return;

    _pendingRequests.Remove(responseCmd);
    pendingRequest.Completion.TrySetResult(response);
  }

  /// <summary>仅当字典中仍是同一请求时移除，避免旧请求超时误删后续新请求。</summary>
  private void RemovePendingRequest(uint responseCmd, PendingRequest pendingRequest)
  {
    if (_pendingRequests.TryGetValue(responseCmd, out PendingRequest current)
        && ReferenceEquals(current, pendingRequest))
      _pendingRequests.Remove(responseCmd);
  }

  /// <summary>关闭或释放网络时取消所有等待中的请求，避免调用方永久等待已不可能到达的响应。</summary>
  private void CancelPendingRequests()
  {
    foreach (PendingRequest pendingRequest in _pendingRequests.Values)
      pendingRequest.Completion.TrySetCanceled();

    _pendingRequests.Clear();
  }

  /// <summary>
  /// 关闭并释放当前 Socket。先断开旧实例与管理器的引用，使关闭回调不会触发新的重连，再等待关闭完成后释放事件引用。
  /// </summary>
  private async Task CloseAndDisposeCurrentSocketAsync()
  {
    SocketMgr socket = _socketMgr;
    if (socket == null)
      return;

    _socketMgr = null;
    try
    {
      await socket.Close();
    }
    finally
    {
      socket.Dispose();
    }
  }

  /// <summary>
  /// 更新连接状态；相同状态不重复通知，避免 Socket 的错误与关闭回调产生重复 UI 刷新。
  /// </summary>
  private void SetConnectionState(NetworkConnectionState state)
  {
    if (ConnectionState == state)
      return;

    ConnectionState = state;
    ConnectionStateChanged?.Invoke(state);
  }
}
