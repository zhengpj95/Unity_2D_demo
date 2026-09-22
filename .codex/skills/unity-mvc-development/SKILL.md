---
name: unity-mvc-development
description: Develop or refactor Unity C# business features that use this repository's Module, Proxy, Command, Presenter, and View conventions. Use for MVC business flows; do not use for standalone prototype scripts, scene layout, art, shaders, or other non-MVC work.
---

# Unity MVC 业务开发

为本仓库已有的 Module/MVC 体系实现或重构业务功能。Skill 只补充 MVC 特有决策；通用工程规则以根目录 `AGENTS.md` 为准。

## 读取顺序

1. 阅读 `AGENTS.md`。
2. 阅读 `Docs/Architecture.md` 和 `Assets/Scripts/Framework/MVC/README.md`。
3. 阅读目标 Module、相关 Proxy/Command/Presenter/View、MVC 基类及真实调用方。
4. 修改 Survivor 时，再阅读 `Docs/Modules/Survivor.md` 与相关专题文档。

`Assets/Scripts/TestCode/Login` 只提供最小注册示例，不代表完整业务规则；复杂流程优先参考目标模块的当前实现。

## 关键边界

- **Module** 负责装配、公开业务入口和生命周期收口；在 `OnInit` 登记 Proxy、Command、Presenter，不隐藏初始化其他模块。
- **Proxy** 持有模块数据或协议边界；协议 Handler 在 Proxy 生命周期内注册和注销，不直接操作 UI。
- **Command** 通过 `Execute(EventContext)` 编排一次明确动作，不保存长期业务状态。
- **Presenter/View** 遵循 `UIManager -> Presenter -> View`；Presenter 协调交互和展示，View 只处理 Unity 组件。
- 事件 ID 来自 `EventDefine`，监听器使用 `EventContext`。MonoBehaviour 按启用状态订阅时使用 `OnEnable`/`OnDisable`；Module、Proxy、Command、Presenter 使用 `BaseEmitter` 和各自框架释放阶段。
- Presenter 的 `PrefabPath` 是交给 `AssetLoader` 的资源键；不要在业务层新增直接的 Resources 或 Addressables 调用。
- 跨模块协作使用明确入口、稳定接口或必要事件，不用 Singleton 查找隐藏循环依赖。

## 实施与验证

- 先确认 `ModuleName`、ViewType、事件、状态真源和调用方向，再做最小改动。
- 不为套用 MVC 强拆简单原型，也不把场景瞬时状态全部塞入 Module/Model。
- 修改后检查事件/协议解绑、Presenter 关闭、异步失效、序列化兼容和跨层反向依赖。
- Unity 版本读取 `ProjectSettings/ProjectVersion.txt`。分别汇报静态检查、Unity 编译、Edit Mode、Play Mode 和 Build；未执行的项目明确说明。
- 架构边界变化时同步 `Docs/Architecture.md`，模块业务规则变化时同步对应模块文档。
