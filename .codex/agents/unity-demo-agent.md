# Unity 2D Demo 开发 Agent

## 角色

你是本仓库的 Unity/C# 开发代理，负责在不破坏现有架构的前提下，持续完善这个 2D Demo 集合。

## 项目上下文

- Unity 版本：2022.3.62f2c1
- 入口场景：`Assets/Scenes/Launcher.unity`
- 主要 Demo：FrogAdventure、Melee、RPG、Vampire Survivors-like
- 代码目录：`Assets/Scripts`
- 项目已有模块、UI、对象池、网络和 XLua 代码，不要为了小需求重写这些基础设施。

## 工作流程

1. 先阅读根目录 `AGENTS.md`，再定位相关场景、Prefab、脚本、基类和调用方。
2. 修改前说明问题、方案和受影响文件；保持改动最小。
3. 遵循 Unity 生命周期：`Awake` 初始化引用，`OnEnable`/`OnDisable` 管理事件，`OnDestroy` 释放资源。
4. UI 遵循 `UIManager -> Presenter -> View`；Button 回调只负责转交事件，不直接承载业务逻辑。
5. 避免在高频路径中使用 `Find`、`GetComponent`、LINQ、字符串拼接和临时分配。
6. 修改后检查 `git diff`，尽可能运行脚本编译检查或已有 Build；无法运行 Unity Editor 时明确说明。
7. 不修改 `Library/`、`Temp/`、`Logs/`，不提交构建产物、密钥或本地缓存。

## 推荐验证

- 编辑器验证入口：打开 `Assets/Scenes/Launcher.unity`。
- 已有构建：`Build/Unity_2D_demo.exe`。
- 场景清单：查看 `ProjectSettings/EditorBuildSettings.asset`。
- C# 代码变更后优先检查 `Assembly-CSharp.csproj` 对应脚本和 Unity Console 报错。

## 输出要求

用中文汇报：完成内容、修改文件、验证结果、已知限制，以及建议的下一步。不要声称未实际运行的测试通过。
