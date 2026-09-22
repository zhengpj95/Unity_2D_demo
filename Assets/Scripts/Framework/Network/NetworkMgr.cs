using UnityEngine;
using System;
using System.Threading;
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
  private const string LogTag = "[NetworkMgr]";
  private const int DefaultMaxIncomingPacketBytes = 1024 * 1024;

  private sealed class PendingRequest
  {
    public readonly TaskCompletionSource<IMessage> Completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
  }

  private SocketMgr _socketMgr;
  private MessageDispatcher _dispatcher;
  private readonly List<Action> _pendingRegistrations = new();
  private readonly Dictionary<uint, int> _commandVersions = new();
  private readonly Dictionary<uint, PendingRequest> _pendingRequests = new();
  private readonly System.Random _reconnectRandom = new();
  private readonly object _reconnectRandomLock = new();
  private string _url;
  private Task<bool> _connectTask;
  private Task _reconnectTask;
  private CancellationTokenSource _reconnectCancellation;
  private bool _manualClose;
  private bool _hasEstablishedConnection;
  private string _lastConnectionError;
  private int _maxIncomingPacketBytes = DefaultMaxIncomingPacketBytes;

  /// <summary>是否已连接。</summary>
  public bool IsConnected => _socketMgr != null && _socketMgr.IsConnected;

  /// <summary>
  /// 单个入站 WebSocket 二进制帧允许的最大总字节数，包含 4 字节 cmd 包头。
  /// 默认 1 MiB；限制异常大包可避免继续复制包体和执行 Protobuf 解析。
  /// </summary>
  public int MaxIncomingPacketBytes
  {
    get => _maxIncomingPacketBytes;
    set
    {
      if (value < PacketCodec.HeaderSize)
      {
        throw new ArgumentOutOfRangeException(
            nameof(value),
            value,
            $"Maximum incoming packet size must be at least {PacketCodec.HeaderSize} bytes.");
      }

      _maxIncomingPacketBytes = value;
    }
  }

  /// <summary>当前网络连接状态，供业务层决定加载、提示或降级策略。</summary>
  public NetworkConnectionState ConnectionState { get; private set; } = NetworkConnectionState.Disconnected;

  /// <summary>自动重连最大尝试次数；小于 0 表示持续重连。</summary>
  public int MaxReconnectAttempts { get; set; } = 3;

  /// <summary>两次重连尝试之间的间隔（秒）。</summary>
  public float ReconnectDelaySeconds { get; set; } = 2f;

  /// <summary>重连退避的最大等待秒数；小于等于零表示不限制上限。</summary>
  public float MaxReconnectDelaySeconds { get; set; } = 30f;

  /// <summary>重连等待时间的随机抖动比例，取值会被限制在 0 到 1 之间。</summary>
  public float ReconnectJitterRatio { get; set; } = 0.2f;

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
      Debug.Log($"{LogTag} Handler registration queued. Cmd={cmd}, Type={typeof(T).Name}");
      _pendingRegistrations.Add(() =>
      {
        if (IsCurrentCommandVersion(cmd, commandVersion)) _dispatcher.Register(cmd, handler);
      });
      return;
    }

    _dispatcher.Register(cmd, handler);
    Debug.Log($"{LogTag} Handler registered. Cmd={cmd}, Type={typeof(T).Name}");
  }

  public bool UnregisterHandler(uint cmd)
  {
    GetNextCommandVersion(cmd);
    bool result = _dispatcher != null && _dispatcher.Unregister(cmd);
    Debug.Log($"{LogTag} Handler unregistered. Cmd={cmd}, Result={result}");
    return result;
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

    Debug.Log($"{LogTag} Connect requested. Url={url}, State={ConnectionState}, IsConnected={IsConnected}");

    if (_connectTask != null && !_connectTask.IsCompleted)
    {
      if (!string.Equals(_url, url, StringComparison.Ordinal))
        throw new InvalidOperationException("Cannot change the server URL while a connection is in progress. Close the current connection first.");

      Debug.Log($"{LogTag} Returning the existing connect task. Url={url}");
      return _connectTask;
    }

    if (_reconnectTask != null && !_reconnectTask.IsCompleted)
    {
      if (!string.Equals(_url, url, StringComparison.Ordinal))
        throw new InvalidOperationException("Cannot change the server URL while reconnecting. Close the current connection first.");

      Debug.Log($"{LogTag} Returning the existing reconnect task. Url={url}");
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
      Debug.Log($"{LogTag} Connect completed immediately because the socket is already connected. Url={url}");
      SetConnectionState(NetworkConnectionState.Connected);
      return Task.CompletedTask;
    }

    SetConnectionState(NetworkConnectionState.Connecting);
    _connectTask = ConnectSocketAsync();
    return _connectTask;
  }

  private async Task<bool> ConnectSocketAsync(CancellationToken cancellationToken = default)
  {
    cancellationToken.ThrowIfCancellationRequested();

    if (IsConnected)
    {
      Debug.Log($"{LogTag} Socket creation skipped because it is already connected. Url={_url}");
      return true;
    }

    Debug.Log($"{LogTag} Preparing socket connection. Url={_url}");
    ProtoRegister.RegisterAll();
    _dispatcher ??= new MessageDispatcher();
    FlushPendingRegistrations();

    await CloseAndDisposeCurrentSocketAsync();
    cancellationToken.ThrowIfCancellationRequested();
    if (_manualClose || string.IsNullOrWhiteSpace(_url))
    {
      Debug.LogWarning($"{LogTag} Socket creation cancelled. ManualClose={_manualClose}, Url={_url}");
      return false;
    }

    SocketMgr socket = new SocketMgr();
    _socketMgr = socket;
    socket.OnMessage += ReceiveMessage;
    socket.OnConnected += () => HandleSocketConnected(socket);
    socket.OnClosed += code => HandleSocketClosed(socket, code);
    socket.OnError += error => HandleSocketError(socket, error);

    Debug.Log($"{LogTag} Socket created and callbacks registered. Url={_url}");
    await socket.Connect(_url);
    cancellationToken.ThrowIfCancellationRequested();
    bool connected = ReferenceEquals(socket, _socketMgr) && socket.IsConnected;
    Debug.Log($"{LogTag} Socket connection attempt finished. Url={_url}, Connected={connected}, SocketState={socket.State}");
    return connected;
  }

  private void HandleSocketConnected(SocketMgr socket)
  {
    if (!ReferenceEquals(socket, _socketMgr)) return;
    Debug.Log($"{LogTag} Connected. Url={_url}");
    _hasEstablishedConnection = true;
    _lastConnectionError = null;
    SetConnectionState(NetworkConnectionState.Connected);
    Connected?.Invoke();
  }

  private void HandleSocketClosed(SocketMgr socket, NativeWebSocket.WebSocketCloseCode code)
  {
    if (!ReferenceEquals(socket, _socketMgr)) return;
    Debug.LogWarning($"{LogTag} Connection closed. Url={_url}, Code={code}, ManualClose={_manualClose}");

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
    Debug.LogWarning($"{LogTag} Socket error received. Url={_url}, Error={error}");
    _lastConnectionError = error;
    ScheduleReconnect();
  }

  private void ScheduleReconnect()
  {
    if (_manualClose || string.IsNullOrWhiteSpace(_url) || IsConnected)
    {
      Debug.Log($"{LogTag} Reconnect skipped. ManualClose={_manualClose}, Url={_url}, IsConnected={IsConnected}");
      return;
    }

    if (_reconnectTask != null && !_reconnectTask.IsCompleted)
    {
      Debug.Log($"{LogTag} Reconnect is already scheduled. Url={_url}");
      return;
    }

    Debug.Log($"{LogTag} Reconnect scheduled. Url={_url}, BaseDelaySeconds={ReconnectDelaySeconds}, MaxDelaySeconds={MaxReconnectDelaySeconds}, JitterRatio={ReconnectJitterRatio}, MaxAttempts={MaxReconnectAttempts}");
    SetConnectionState(NetworkConnectionState.Reconnecting);
    CancellationTokenSource reconnectCancellation = new();
    _reconnectCancellation = reconnectCancellation;
    _reconnectTask = ReconnectLoopAsync(reconnectCancellation);
  }

  /// <summary>按指数退避和随机抖动执行重连；关闭或释放时可通过取消令牌立刻停止等待。</summary>
  private async Task ReconnectLoopAsync(CancellationTokenSource reconnectCancellation)
  {
    int attempt = 0;
    CancellationToken cancellationToken = reconnectCancellation.Token;
    try
    {
      while (!_manualClose && !IsConnected && (MaxReconnectAttempts < 0 || attempt < MaxReconnectAttempts))
      {
        attempt++;
        float delaySeconds = CalculateReconnectDelaySeconds(attempt);
        Debug.Log($"{LogTag} Reconnect waiting. Url={_url}, Attempt={attempt}, DelaySeconds={delaySeconds:F2}, MaxAttempts={MaxReconnectAttempts}");
        if (delaySeconds > 0f)
          await Task.Delay(TimeSpan.FromSeconds(delaySeconds), cancellationToken);

        cancellationToken.ThrowIfCancellationRequested();
        if (_manualClose || IsConnected) break;

        Debug.Log($"{LogTag} Reconnecting. Url={_url}, Attempt={attempt}, MaxAttempts={MaxReconnectAttempts}");
        if (await ConnectSocketAsync(cancellationToken)) return;
      }

      if (!IsConnected && !_manualClose)
      {
        Debug.LogError($"{LogTag} Reconnect attempts exhausted. Url={_url}, AttemptCount={attempt}, LastError={_lastConnectionError}");
        SetConnectionState(NetworkConnectionState.Failed);
        ConnectionFailed?.Invoke(new NetworkConnectionFailure(_lastConnectionError, attempt));
      }
    }
    catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
    {
      Debug.Log($"{LogTag} Reconnect cancelled. Url={_url}, AttemptCount={attempt}");
    }
    finally
    {
      if (ReferenceEquals(_reconnectCancellation, reconnectCancellation))
        _reconnectCancellation = null;

      reconnectCancellation.Dispose();
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

      NetworkSendResult unavailableResult = ConnectionState == NetworkConnectionState.Reconnecting
        ? NetworkSendResult.Reconnecting
        : NetworkSendResult.NotConnected;
      Debug.LogWarning(
          $"{LogTag} 发送协议失败：Cmd={cmd}, " +
          $"MessageType={message?.GetType().Name ?? typeof(T).Name}, Result={unavailableResult}, State={ConnectionState}");
      return unavailableResult;
    }

    try
    {
      byte[] body = ProtoMgr.Encode(message);
      byte[] packet = PacketCodec.Encode(cmd, body);
      SocketMgr socket = _socketMgr;
      if (socket == null)
      {
        Debug.LogWarning(
            $"{LogTag} 发送协议失败：Cmd={cmd}, " +
            $"MessageType={message?.GetType().Name ?? typeof(T).Name}, BodyBytes={body.Length}, " +
            $"PacketBytes={packet.Length}, Result={NetworkSendResult.NotConnected}, Reason=Socket manager is unavailable.");
        return NetworkSendResult.NotConnected;
      }

      Debug.Log(
          $"{LogTag} 发送协议：Cmd={cmd}, " +
          $"MessageType={message.GetType().Name}, BodyBytes={body.Length}, PacketBytes={packet.Length}");
      NetworkSendResult result = await socket.Send(packet) ? NetworkSendResult.Sent : NetworkSendResult.TransportFailed;
      Debug.Log(
          $"{LogTag} 发送协议完成：Cmd={cmd}, " +
          $"MessageType={message.GetType().Name}, BodyBytes={body.Length}, PacketBytes={packet.Length}, Result={result}");
      return result;
    }
    catch (Exception exception)
    {
      Debug.LogError(
          $"{LogTag} 发送协议异常：Cmd={cmd}, " +
          $"MessageType={message?.GetType().Name ?? typeof(T).Name}, Error={exception}");
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
    Debug.Log($"{LogTag} Request started. RequestCmd={requestCmd}, ResponseCmd={responseCmd}, TimeoutSeconds={timeoutSeconds}");

    NetworkSendResult sendResult = await Send(requestCmd, request);
    if (sendResult != NetworkSendResult.Sent)
    {
      RemovePendingRequest(responseCmd, pendingRequest);
      Debug.LogWarning($"{LogTag} Request ended because sending failed. RequestCmd={requestCmd}, ResponseCmd={responseCmd}, SendResult={sendResult}");
      return new NetworkRequestResult<TResponse>(NetworkRequestStatus.SendFailed, sendResult);
    }

    Task timeoutTask = Task.Delay(TimeSpan.FromSeconds(timeoutSeconds));
    Task completedTask = await Task.WhenAny(pendingRequest.Completion.Task, timeoutTask);
    if (completedTask == timeoutTask)
    {
      RemovePendingRequest(responseCmd, pendingRequest);
      Debug.LogWarning($"{LogTag} Request timed out. RequestCmd={requestCmd}, ResponseCmd={responseCmd}, TimeoutSeconds={timeoutSeconds}");
      return new NetworkRequestResult<TResponse>(NetworkRequestStatus.TimedOut, sendResult);
    }

    try
    {
      IMessage response = await pendingRequest.Completion.Task;
      if (response is TResponse typedResponse)
      {
        Debug.Log($"{LogTag} Request completed. RequestCmd={requestCmd}, ResponseCmd={responseCmd}, ResponseType={response.GetType().Name}");
        return new NetworkRequestResult<TResponse>(NetworkRequestStatus.Succeeded, sendResult, typedResponse);
      }

      Debug.LogError($"{LogTag} Request response type mismatch. RequestCmd={requestCmd}, ResponseCmd={responseCmd}, ActualType={response?.GetType().Name}, ExpectedType={typeof(TResponse).Name}");
      return new NetworkRequestResult<TResponse>(NetworkRequestStatus.ResponseTypeMismatch, sendResult);
    }
    catch (TaskCanceledException)
    {
      Debug.LogWarning($"{LogTag} Request cancelled. RequestCmd={requestCmd}, ResponseCmd={responseCmd}");
      return new NetworkRequestResult<TResponse>(NetworkRequestStatus.Cancelled, sendResult);
    }
  }

  public void ReceiveMessage(byte[] data)
  {
    int packetBytes = data?.Length ?? 0;

    if (data == null)
    {
      LogReceiveFailure("LengthValidation", packetBytes, null, 0, "Packet is null.");
      return;
    }

    if (packetBytes < PacketCodec.HeaderSize)
    {
      LogReceiveFailure(
          "LengthValidation",
          packetBytes,
          null,
          0,
          $"Packet is shorter than the {PacketCodec.HeaderSize}-byte header.");
      return;
    }

    if (packetBytes > MaxIncomingPacketBytes)
    {
      LogReceiveFailure(
          "LengthValidation",
          packetBytes,
          null,
          packetBytes - PacketCodec.HeaderSize,
          $"Packet exceeds the {MaxIncomingPacketBytes}-byte limit.");
      return;
    }

    if (_dispatcher == null)
    {
      LogReceiveFailure(
          "DispatcherAvailability",
          packetBytes,
          null,
          packetBytes - PacketCodec.HeaderSize,
          "Dispatcher is not initialized.");
      return;
    }

    Packet packet;
    try
    {
      packet = PacketCodec.Decode(data);
    }
    catch (Exception exception)
    {
      LogReceiveFailure(
          "PacketDecode",
          packetBytes,
          null,
          packetBytes - PacketCodec.HeaderSize,
          "Packet header decode failed.",
          exception);
      return;
    }

    if (!ProtoMgr.Contains(packet.Cmd))
    {
      LogReceiveFailure(
          "ProtocolLookup",
          packetBytes,
          packet.Cmd,
          packet.Body.Length,
          "Unknown command.");
      return;
    }

    IMessage message;
    try
    {
      message = ProtoMgr.Decode(packet.Cmd, packet.Body);
    }
    catch (Exception exception)
    {
      LogReceiveFailure(
          "ProtoDecode",
          packetBytes,
          packet.Cmd,
          packet.Body.Length,
          "Protobuf decode failed.",
          exception);
      return;
    }

    Debug.Log(
        $"{LogTag} 接收协议：Cmd={packet.Cmd}, " +
        $"MessageType={message.GetType().Name}, BodyBytes={packet.Body.Length}, PacketBytes={packetBytes}");
    CompletePendingRequest(packet.Cmd, message);

    try
    {
      _dispatcher.Dispatch(packet.Cmd, message);
    }
    catch (Exception exception)
    {
      LogReceiveFailure(
          "HandlerDispatch",
          packetBytes,
          packet.Cmd,
          packet.Body.Length,
          "Message handler threw an exception.",
          exception);
      return;
    }

  }

  /// <summary>统一记录收包失败上下文，不记录消息正文，避免异常包泄露业务敏感数据。</summary>
  private static void LogReceiveFailure(
      string stage,
      int packetBytes,
      uint? cmd,
      int bodyBytes,
      string reason,
      Exception exception = null)
  {
    string commandText = cmd.HasValue ? cmd.Value.ToString() : "Unknown";
    string log =
        $"{LogTag} Incoming packet dropped. Stage={stage}, Cmd={commandText}, " +
        $"PacketBytes={packetBytes}, BodyBytes={bodyBytes}, Reason={reason}";

    if (exception == null)
    {
      Debug.LogWarning(log);
      return;
    }

    Debug.LogError($"{log}, Exception={exception}");
  }

  public async Task Close()
  {
    Debug.Log($"{LogTag} Manual close requested. Url={_url}, State={ConnectionState}");
    _manualClose = true;
    CancelReconnect();
    _url = null;
    _hasEstablishedConnection = false;
    SetConnectionState(NetworkConnectionState.Closing);
    CancelPendingRequests();
    await CloseAndDisposeCurrentSocketAsync();
    SetConnectionState(NetworkConnectionState.Disconnected);
    Debug.Log($"{LogTag} Manual close completed. State={ConnectionState}");
  }

  public void Dispose()
  {
    Debug.Log($"{LogTag} Disposing network manager. Url={_url}, State={ConnectionState}");
    _manualClose = true;
    CancelReconnect();
    _url = null;
    _hasEstablishedConnection = false;
    CancelPendingRequests();
    _socketMgr?.Dispose();
    _socketMgr = null;
    _connectTask = null;
    _reconnectTask = null;
    SetConnectionState(NetworkConnectionState.Disconnected);
  }

  /// <summary>计算本次重连前的等待时间：基础间隔按 2 的幂增长，达到上限后保持，并叠加随机抖动。</summary>
  private float CalculateReconnectDelaySeconds(int attempt)
  {
    float baseDelay = Mathf.Max(0f, ReconnectDelaySeconds);
    int exponent = Mathf.Clamp(attempt - 1, 0, 30);
    float exponentialDelay = baseDelay * Mathf.Pow(2f, exponent);
    float maxDelay = Mathf.Max(0f, MaxReconnectDelaySeconds);
    float cappedDelay = maxDelay > 0f ? Mathf.Min(exponentialDelay, maxDelay) : exponentialDelay;
    float jitterRange = cappedDelay * Mathf.Clamp01(ReconnectJitterRatio);
    if (jitterRange <= 0f)
      return cappedDelay;

    double randomValue;
    lock (_reconnectRandomLock)
      randomValue = _reconnectRandom.NextDouble() * 2d - 1d;

    return Mathf.Max(0f, cappedDelay + (float)(randomValue * jitterRange));
  }

  /// <summary>取消正在等待的重连循环；不会影响已经建立的连接。</summary>
  private void CancelReconnect()
  {
    if (_reconnectCancellation == null || _reconnectCancellation.IsCancellationRequested)
      return;

    Debug.Log($"{LogTag} Cancelling reconnect task. Url={_url}");
    _reconnectCancellation.Cancel();
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
    Debug.Log($"{LogTag} Completing pending request. ResponseCmd={responseCmd}, ResponseType={response?.GetType().Name}");
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
    if (_pendingRequests.Count > 0)
      Debug.LogWarning($"{LogTag} Cancelling pending requests. Count={_pendingRequests.Count}");

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
    {
      Debug.Log($"{LogTag} No active socket needs to be closed.");
      return;
    }

    Debug.Log($"{LogTag} Closing and disposing the current socket. Url={_url}, SocketState={socket.State}");
    _socketMgr = null;
    try
    {
      await socket.Close();
    }
    finally
    {
      socket.Dispose();
      Debug.Log($"{LogTag} Current socket disposed. Url={_url}");
    }
  }

  /// <summary>
  /// 更新连接状态；相同状态不重复通知，避免 Socket 的错误与关闭回调产生重复 UI 刷新。
  /// </summary>
  private void SetConnectionState(NetworkConnectionState state)
  {
    if (ConnectionState == state)
      return;

    NetworkConnectionState previousState = ConnectionState;
    ConnectionState = state;
    Debug.Log($"{LogTag} Connection state changed. {previousState} -> {state}, Url={_url}");
    ConnectionStateChanged?.Invoke(state);
  }
}
