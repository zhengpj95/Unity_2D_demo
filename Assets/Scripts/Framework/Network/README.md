# Network 模块

本目录封装 Unity 客户端的 WebSocket 连接、Packet/Protobuf 编解码和消息分发。传输层不依赖 UI；业务协议由 Proxy/Module 接收后再更新状态或派发业务事件。

`GameMgr.EnableSocketConnection` 当前为开发期代码开关且处于启用状态，客户端启动时会主动连接 `ws://localhost:3000`；本地服务端未启动时会进入既有的失败与重连流程。

## 数据流

```text
业务 Module / Proxy
       │ Send / Request / RegisterHandler
       ▼
   NetworkMgr
       ├─ ProtoMgr：IMessage ↔ byte[]
       ├─ PacketCodec：uint cmd + body ↔ packet
       └─ MessageDispatcher：cmd → 强类型 Handler
       ▼
    SocketMgr
       ▼
 NativeWebSocket / Server
```

收到消息时按相反方向执行：`SocketMgr.OnMessage → 长度校验 → PacketCodec.Decode → cmd 注册校验 → ProtoMgr.Decode → 完成等待中的 Request → MessageDispatcher.Dispatch`。任一阶段失败只丢弃当前帧，不主动断开连接；日志记录阶段、cmd（可获取时）和长度，不记录协议正文。

## 文件职责

| 文件 | 当前职责 |
| --- | --- |
| `SocketMgr.cs` | 包装 NativeWebSocket 的连接、发送、关闭、状态与底层事件；`Dispose` 解绑事件并释放 Socket |
| `NetworkMgr.cs` | 对外连接入口、重连状态机、Proto 消息收发、Request 超时、Handler 注册与业务连接事件 |
| `PacketCodec.cs` | 以小端序编码/解码 `4 字节 uint cmd + body` |
| `ProtoMgr.cs` | 保存 `cmd → MessageParser` 映射并编解码 `IMessage` |
| `ProtoRegister.cs` | 当前协议 Parser 的集中注册入口 |
| `MessageDispatcher.cs` | 保存每个 cmd 的唯一强类型业务 Handler 并执行分发 |

协议源文件是 `Assets/Configs/message.proto`，生成代码是 `Assets/Scripts/Define/Proto/Message.cs`，协议号位于 `MessageID.cs`。修改 `.proto` 后必须重新生成代码，并检查命令号与 `ProtoRegister` 一致；不要手工修改生成代码来替代协议源变更。

## 对外能力

### 连接

- `Connect(string url)`：合并同地址的并发连接任务；切换地址前应先 `Close()`。
- `ConnectionState`：类型为 `NetworkConnectionState`。
- `Connected`、`Disconnected`、`ConnectionStateChanged`：连接生命周期通知。
- `ConnectionFailed`：重连次数耗尽后携带原因与尝试次数；当前由 `MiscModule` 决定通用提示和场景处理。
- `Close()`：标记主动关闭、取消重连和等待请求，并等待底层 WebSocket Close 握手。
- `Dispose()`：只解绑事件并释放本地引用，不自行等待网络关闭；正常退出应先 `Close()` 再 `Dispose()`。

首次连接失败和已建立连接后的断线都会进入指数退避重连。等待时间为基础延迟的指数增长，并受 `MaxReconnectDelaySeconds` 和 `ReconnectJitterRatio` 限制；`MaxReconnectAttempts < 0` 表示不限次数。

### 发送与请求

```csharp
NetworkSendResult result = await NetworkMgr.Instance.Send(cmd, message);

NetworkRequestResult<TResponse> response = await NetworkMgr.Instance.Request<TRequest, TResponse>(
  requestCmd,
  request,
  responseCmd,
  timeoutSeconds);
```

- `Send` 不用异常表示常规连接状态，调用方应检查 `NetworkSendResult`。
- `Request` 按 `responseCmd` 等待一次响应并支持超时/取消结果。
- 同一个 `responseCmd` 只允许一个等待请求；项目约定一个响应协议由唯一职责方处理，不支持用相同响应号并发关联多请求。
- `MaxIncomingPacketBytes` 限制单个入站帧的总长度（包含 4 字节 cmd），默认 `1 MiB`，且不能小于包头长度。

### Handler

业务 Proxy 优先通过 `BaseProxy.RegisterHandler<T>` 注册，以便模块释放时自动注销。直接调用 NetworkMgr 时使用：

```csharp
NetworkMgr.Instance.RegisterHandler<s2c_user_login>(MessageId.S2C_USER_LOGIN, OnLogin);
NetworkMgr.Instance.UnregisterHandler(MessageId.S2C_USER_LOGIN);
```

每个 cmd 只能有一个业务 Handler。网络回调不得直接操作 UI，应先更新 Proxy/Module 状态或派发事件。

服务端通用错误协议 `S2C_ERROR` 由常驻 `MiscProxy` 注册处理，并通过 `MISC_OPEN_ALERT` 事件进入通用提示弹窗；Network 层只负责解码和分发。

## 当前已实现

- NativeWebSocket 连接、关闭与状态转换。
- 首次失败/断线后的指数退避重连、上限、抖动与主动取消。
- `uint` cmd 的 Packet 编解码。
- Protobuf Parser 集中注册、编解码与强类型消息分发。
- 可检查的发送结果和按 responseCmd 等待的单次请求。
- 发送与接收边界分别输出“发送协议”和“接收协议”中文日志，并包含 cmd、消息类型、包体长度、总包长度和发送结果；不输出完整协议内容。
- 收包会隔离非法长度、未知 cmd、Protobuf 解码失败和业务 Handler 异常；失败帧被丢弃，连接可继续处理后续消息。
- Network 与 UI/Scene 解耦，连接失败交由业务层决定表现。
- `GameMgr.OnApplicationQuit` 在停止 Editor Play Mode 或退出 Player 时等待 `Close()`，完成后再 `Dispose()`；强制终止进程时不保证握手完成。

## 已知限制与优先级

1. **P1：连接取消与地址切换。** 同地址并发连接已合并，主动关闭可取消重连；连接中的取消令牌和受控地址切换仍未提供。
2. **P2：心跳与平台验证。** 尚无应用层心跳/超时检测；需要按目标平台验证 NativeWebSocket 消息队列与主线程要求。
3. **P2：可观测性和自动化测试。** 尚无连接/重连/收发量指标，也没有 PacketCodec、ProtoMgr、Dispatcher 或重连状态机的正式测试程序集。
4. **发布配置。** 服务地址仍写在 `GameMgr`，尚无开发/测试/正式环境配置。

新增网络能力时同步检查 `Docs/Architecture.md`；改变协议时同步源 `.proto`、生成代码、命令号和注册表。
