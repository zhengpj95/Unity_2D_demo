using System.Collections;
using System.Collections.Generic;
using UnityEngine;
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
  private const string LogTag = "[SocketMgr]";
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
    {
      Debug.Log($"{LogTag} Connect ignored because the socket is already connected. Url={_url}");
      return;
    }

    if (State == SocketState.Connecting)
    {
      Debug.Log($"{LogTag} Connect ignored because the socket is connecting. Url={_url}");
      return;
    }

    _url = url;
    State = SocketState.Connecting;
    Debug.Log($"{LogTag} Connecting. Url={_url}");
    _socket = new WebSocket(_url);
    RegisterEvents();

    try
    {
      await _socket.Connect();
      Debug.Log($"{LogTag} Connect task finished. State={_socket.State}, Url={_url}");
    }
    catch (Exception e)
    {
      State = SocketState.Error;
      Debug.LogError($"{LogTag} Connect failed. Url={_url}, Error={e}");
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
    Debug.Log($"{LogTag} WebSocket opened. Url={_url}");
    OnConnected?.Invoke();
  }

  private void HandleMessage(byte[] data)
  {
    Debug.Log($"{LogTag} Received raw data. Bytes={data?.Length ?? 0}, Url={_url}");
    OnMessage?.Invoke(data);
  }

  private void HandleError(string error)
  {
    State = SocketState.Error;
    Debug.LogError($"{LogTag} WebSocket error. Url={_url}, Error={error}");
    OnError?.Invoke(error);
  }

  private void HandleClose(WebSocketCloseCode code)
  {
    State = SocketState.Closed;
    Debug.LogWarning($"{LogTag} WebSocket closed. Url={_url}, Code={code}");
    OnClosed?.Invoke(code);
  }

  /// <summary>
  /// 发送原始数据并返回传输层结果；true 仅表示数据已交给底层 WebSocket，不表示服务端已处理。
  /// </summary>
  public async Task<bool> Send(byte[] data)
  {
    if (!IsConnected)
    {
      Debug.LogWarning($"{LogTag} Send rejected because the socket is not connected. State={State}, Bytes={data?.Length ?? 0}");
      OnError?.Invoke("WebSocket is not connected.");
      return false;
    }

    try
    {
      Debug.Log($"{LogTag} Sending raw data. Bytes={data?.Length ?? 0}, Url={_url}");
      await _socket.Send(data);
      Debug.Log($"{LogTag} Raw data submitted to WebSocket. Bytes={data?.Length ?? 0}, Url={_url}");
      return true;
    }
    catch (Exception e)
    {
      Debug.LogError($"{LogTag} Send failed. Bytes={data?.Length ?? 0}, Url={_url}, Error={e}");
      OnError?.Invoke(e.Message);
      return false;
    }
  }

  public async Task Close()
  {
    if (_socket == null)
    {
      Debug.Log($"{LogTag} Close ignored because no socket exists.");
      return;
    }

    if (_socket.State == WebSocketState.Closed)
    {
      Debug.Log($"{LogTag} Close ignored because the socket is already closed. Url={_url}");
      return;
    }

    State = SocketState.Closing;
    Debug.Log($"{LogTag} Closing socket. Url={_url}, State={_socket.State}");

    try
    {
      await _socket.Close();
      Debug.Log($"{LogTag} Close task finished. Url={_url}, State={_socket.State}");
    }
    catch (Exception e)
    {
      Debug.LogError($"{LogTag} Close failed. Url={_url}, Error={e}");
      OnError?.Invoke(e.Message);
    }
  }

  public void Dispose()
  {
    Debug.Log($"{LogTag} Disposing socket manager. Url={_url}, State={State}");
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
