using System.Threading.Tasks;
using NativeWebSocket;
using System;

public enum SocketState
{
  None,
  Connecting,
  Connected,
  Closing,
  Closed,
  Error
}

public sealed class SocketMgr : IDisposable
{
  private WebSocket _socket;
  private string _url;
  public SocketState State { get; private set; }

  /// <summary>
  /// WebSocket连接成功
  /// </summary>
  public event Action OnConnected;

  /// <summary>
  /// 收到二进制消息
  /// </summary>
  public event Action<byte[]> OnMessage;

  /// <summary>
  /// Socket错误
  /// </summary>
  public event Action<string> OnError;

  /// <summary>
  /// Socket关闭
  /// </summary>
  public event Action<WebSocketCloseCode> OnClosed;

  public bool IsConnected => _socket != null && _socket.State == WebSocketState.Open;

  public async Task Connect(string url)
  {
    if (IsConnected)
      return;

    if (State == SocketState.Connecting)
      return;

    _url = url;
    State = SocketState.Connecting;
    _socket = new WebSocket(_url);
    RegisterEvents();

    try
    {
      await _socket.Connect();
    }
    catch (Exception e)
    {
      State = SocketState.Error;
      OnError?.Invoke(e.Message);
    }
  }

  private void RegisterEvents()
  {
    _socket.OnOpen += HandleOpen;
    _socket.OnMessage += HandleMessage;
    _socket.OnError += HandleError;
    _socket.OnClose += HandleClose;
  }

  private void UnregisterEvents()
  {
    if (_socket == null)
    {
      return;
    }

    _socket.OnOpen -= HandleOpen;
    _socket.OnMessage -= HandleMessage;
    _socket.OnError -= HandleError;
    _socket.OnClose -= HandleClose;
  }

  private void HandleOpen()
  {
    State = SocketState.Connected;
    OnConnected?.Invoke();
  }

  private void HandleMessage(byte[] data)
  {
    OnMessage?.Invoke(data);
  }

  private void HandleError(string error)
  {
    State = SocketState.Error;
    OnError?.Invoke(error);
  }

  private void HandleClose(WebSocketCloseCode code)
  {
    State = SocketState.Closed;
    OnClosed?.Invoke(code);
  }

  /// <summary>
  /// 发送原始数据并返回传输层结果；true 仅表示数据已交给底层 WebSocket，不表示服务端已处理。
  /// </summary>
  public async Task<bool> Send(byte[] data)
  {
    if (!IsConnected)
    {
      OnError?.Invoke("WebSocket is not connected.");
      return false;
    }

    try
    {
      await _socket.Send(data);
      return true;
    }
    catch (Exception e)
    {
      OnError?.Invoke(e.Message);
      return false;
    }
  }

  public async Task Close()
  {
    if (_socket == null)
      return;

    if (_socket.State == WebSocketState.Closed)
      return;

    State = SocketState.Closing;

    try
    {
      await _socket.Close();
    }
    catch (Exception e)
    {
      OnError?.Invoke(e.Message);
    }
  }

  public void Dispose()
  {
    UnregisterEvents();

    OnConnected = null;
    OnMessage = null;
    OnError = null;
    OnClosed = null;

    _url = null;
    _socket = null;
    State = SocketState.None;
  }
}
