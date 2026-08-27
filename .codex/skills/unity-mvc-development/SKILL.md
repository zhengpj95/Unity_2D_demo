---
name: unity-mvc-development
description: Develop or refactor Unity C# business features using this project's Module/Proxy/Command MVC conventions, especially flows modeled after Assets/Scripts/TestCode/Login. Do not use for unrelated Unity art, scene-layout, or non-MVC tasks.
---

# Unity MVC 业务开发

用于本项目中新增或修改 MVC 风格业务模块。项目的实际参考实现位于 `Assets/Scripts/TestCode/Login`：`LoginModule` 注册 `LoginProxy` 与 `LoginCmd`，Proxy 负责协议处理，Command 负责业务动作。

## 开始前

- 先阅读仓库根目录 `AGENTS.md`，再阅读目标模块、`BaseModule`、`BaseProxy`、`BaseCommand` 及其调用方。
- 明确业务入口、状态数据、网络消息和 UI 需求；不要把示例 Login 的占位日志当成完整业务规则。
- 保持现有命名、目录和生命周期约定，优先扩展现有基类，不新增第三方依赖。

## 分层职责

- **Module**：模块装配与生命周期。在 `OnInit` 中注册 Proxy/Command，在 `OnRelease` 中释放资源；避免承载具体业务细节。
- **Proxy**：模块数据和协议边界。集中注册消息处理器，校验解码结果后更新状态或发布事件；不要直接操作 UI。
- **Command**：一次明确的业务动作。在 `Execute` 中编排校验、调用 Proxy/服务和结果事件；参数使用明确类型。
- **Presenter/View**：涉及 UI 时遵循 `UIManager -> Presenter -> View`。Presenter 协调 Command、Proxy 和 UI 状态，View 只处理 Unity 组件显示。

## 实现规则

1. 新模块先确定 `ModuleName`、注册的 Proxy/Command 和事件名，再实现业务代码。
2. 网络请求通过现有 Network/Proto/Dispatcher 链路；网络回调不要直接改 UI。
3. 事件订阅放在 `OnEnable`，解绑放在 `OnDisable`；异步回调在关闭/销毁时失效。
4. 不在 `Update` 中创建临时对象、拼接字符串或使用 LINQ；缓存高频依赖。
5. 修改序列化字段时保持场景/Prefab 兼容，必要时使用 `FormerlySerializedAs`。
6. 不为了 MVC 形式强行拆分简单逻辑，也不把 Module 做成全局 God class。

## 验证与汇报

- 检查 `git diff`，确认只改动任务相关文件。
- 尽可能运行 Unity 2022.3.62f2c1 的现有 Build 或 C# 编译检查；无法运行 Editor 时明确说明。
- 汇报修改的 Module、Proxy、Command、Presenter/View、协议和事件，以及验证结果和未覆盖风险。
