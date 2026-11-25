# Assets/Editor 编辑器工具

本目录存放项目自有的 Unity Editor 工具，按职责分为 `CodeGeneration`、`UI`、`Sprites`、`SceneAndDemo`、`Config` 五个子目录。所有脚本都只在编辑器程序集编译：Unity 对路径中任意层级的 `Editor` 目录都按 Editor 程序集处理，当前项目在 `Assets/Editor` 下没有 asmdef，因此子目录划分不影响编译。

运行时组件不得放入本目录；带 `#if UNITY_EDITOR` 条件编译段的运行时组件（如 `VirtualList`、`UIGray`、`UILauncher`）必须留在 `Assets/Scripts`，否则构建中组件整体不可用。

## 目录总览

| 目录              | 职责                                                     |
| ----------------- | -------------------------------------------------------- |
| `CodeGeneration/` | 读取项目配置并生成常量代码，集中管理生成路径             |
| `UI/`             | Inspector 绘制，以及 UI 创建与 View/Presenter 绑定工作流 |
| `Sprites/`        | 贴图 Sprite 切割与 Project 窗口辅助                      |
| `SceneAndDemo/`   | 场景启动与演示场景生成                                   |
| `Config/`         | ScriptableObject 配置的创建与编辑                        |

## CodeGeneration/

代码生成工具的共享路径配置和输出工具。生成目标统一由 `CodeGenerationPaths` 指定，调整输出目录只需改这一处。

| 文件                          | 类型 / 命名空间                                | 作用与入口                                                                                                                                                                                                          |
| ----------------------------- | ---------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `CodeGenerationPaths.cs`      | `CodeGenerationPaths`，全局命名空间            | 定义三个输出目录（`Assets/Scripts/Define/Editor`、`Assets/Scripts/Define/UI`、`Assets/Scripts/Modules/Presenters`）和四个生成文件路径，提供 `EnsureDirectoryExists`。被下列三个生成器与 `UI/UIAutoBinder.cs` 引用。 |
| `XLuaCodeGenerationConfig.cs` | `XLuaCodeGenerationConfig`，全局命名空间       | 通过 `[CSObjectWrapEditor.GenPath]` 指定 xLua 包装代码的生成目录。这是项目侧配置，依赖 `Assets/ThirdParty/xLua`；第三方 Editor 文件不属于本目录。                                                                   |
| `SceneUINameGenerator.cs`     | `EditorTools.SceneUINameGenerator`             | 菜单 `Tools/03 SceneUIName Generator`：按 Build Settings 中启用的场景生成 `SceneUIName.cs`。                                                                                                                        |
| `TagLayerGenerator.cs`        | `EditorTools.TagLayerGenerator : EditorWindow` | 菜单 `Tools/02 TagLayer Generator`：读取项目 Tag 与 Layer，生成 `GameTag.cs` 和 `GameLayer.cs`。                                                                                                                    |
| `SortingLayerGenerator.cs`    | `EditorTools.SortingLayerGenerator`            | 菜单 `Tools/04 Generate Sorting Layers`：读取 Sorting Layers，生成带辅助方法的 `GameSortingLayers.cs`。                                                                                                             |

## UI/

UI 编辑期辅助工具，既包含只读 Inspector 扩展，也包含通过菜单创建 UI 对象、生成代码的流程工具。两者放在同一目录，是因为都直接依赖 Framework 的 UI 类型。

| 文件                                 | 类型 / 命名空间                                                  | 作用与入口                                                                                                                                                                                                                                                                                                          |
| ------------------------------------ | ---------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `RectTransformInspectorEx.cs`        | `EditorTools.RectTransformInspectorEx : Editor`                  | `[CustomEditor(typeof(RectTransform))]`：保留原生 `RectTransformEditor` 绘制结果，并附加只读的尺寸、锚点、位置调试面板；通过反射访问 `UnityEditor.RectTransformEditor`。                                                                                                                                            |
| `RectTransformPresetEditor.cs`       | `EditorTools.RectTransformPresetEditor : Editor`                 | 为 `RectTransform` 提供 3×3 锚点和拉伸预设按钮，可保持当前尺寸与位置。`[CanEditMultipleObjects]` 生效，但 `[CustomEditor(typeof(RectTransform))]` 当前被注释；恢复启用会与 `RectTransformInspectorEx` 冲突。                                                                                                        |
| `VirtualListEditor.cs`               | `EditorTools.VirtualListEditor : ScrollRectEditor`               | `[CustomEditor(typeof(VirtualList))]`：在原生 ScrollRect Inspector 上补充虚拟列表模板、布局方向、间距和重复参数。                                                                                                                                                                                                   |
| `VariableHeightVirtualListEditor.cs` | `EditorTools.VariableHeightVirtualListEditor : ScrollRectEditor` | `[CustomEditor(typeof(VariableHeightVirtualList))]`：补充异高列表的模板集合、估算高度、缓冲区和滚动参数。                                                                                                                                                                                                           |
| `UIAutoBinder.cs`                    | `UIAutoBinder : Editor`，全局命名空间                            | 菜单 `GameObject/UI 工具/生成 UI View 与 Presenter 并自动绑定`、`GameObject/UI 工具/仅生成 UI View`：按 Prefab 节点命名生成 View/Presenter，并在脚本重编译后通过 `[DidReloadScripts]` 把字段绑定回 Prefab。识别 `VirtualList`、`ListTab`、`UIButton`、`TMP_Text` 等组件，生成代码引用 `UIView` 与 `BasePresenter`。 |
| `UICreateObjectMenu.cs`              | `UICreateObjectMenu`，全局命名空间                               | 菜单 `Assets/Create/Prefab Template`、`GameObject/UI/Virtual List`、`GameObject/UI/Variable Height Virtual List`：创建 UI Prefab 模板和两种虚拟列表层级。依赖 Framework 的 `VirtualList`、`VariableHeightVirtualList`。                                                                                             |

## Sprites/

贴图切割工作流。`ProjectSpriteHelper` 通过 `EditorWindow.GetWindow<SpriteBatchSlicerEditor>()` 打开切割窗口，两者必须同批维护。

| 文件                         | 类型 / 命名空间                                                     | 作用与入口                                                                                                                                                                                                 |
| ---------------------------- | ------------------------------------------------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `ProjectSpriteHelper.cs`     | `EditorTools.ProjectSpriteHelper`、`EditorTools.TextureContextMenu` | `[InitializeOnLoad]`：在 Project 窗口显示贴图内 Sprite 数量角标；右键菜单 `Assets/精灵切割/打开精灵切割器` 及 `快速切割4×4网格`、`快速切割3×3网格`、`快速切割2×2网格`。使用 U2D Sprite Data Provider API。 |
| `SpriteBatchSlicerEditor.cs` | `EditorTools.SpriteBatchSlicerEditor : EditorWindow`                | 菜单 `Tools/精灵批量切割器`：提供单图切割、目录批量切割、项目精灵查看三个页签；使用 `SpriteDataProviderFactories`，并通过反射兼容旧版 API。                                                                |

## SceneAndDemo/

场景启动与演示场景生成工具。

| 文件                  | 类型 / 命名空间                | 作用与入口                                                                                                                                                                                                                   |
| --------------------- | ------------------------------ | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `SceneEditorUtils.cs` | `EditorTools.SceneEditorUtils` | 菜单 `Tools/01 Start Game`：保存当前场景后打开 `Assets/Scenes/Launcher.unity` 并进入播放模式。                                                                                                                               |
| `GuideDemoSetup.cs`   | `GuideDemoSetup`，全局命名空间 | 菜单 `Tools/Guide/Create Guide Demo Scene`：读取 Guide 示例配置，生成包含相机、UI 层级、按钮和引导光标的 `GuideDemo.unity` 场景。依赖 `GuideConfig`、`GuideDemoBootstrap`、`GuideCursor`、`GameMgr`、`UILauncher` 与 TMPro。 |

## Animation/

`AnimFrameConfig` 与 `EnemyTypeConfig` 资源的只读首帧预览扩展。

| 文件                                | 类型 / 命名空间                                         | 作用与入口                                                                                                                                                                                                                                 |
| ----------------------------------- | ------------------------------------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `AnimFrameConfigInspector.cs`       | `EditorTools.AnimFrameConfigInspector : Editor`         | `[CustomEditor(typeof(AnimFrameConfig))]`：在 Inspector 顶部显示默认 Clip（无默认则第一组有效 Clip）的首帧 Sprite 名称、来源下标和缩略图，提供“定位”按钮在 Project 中选中资源；只读，不修改配置数据。                                      |
| `EnemyTypeConfigInspector.cs`       | `EditorTools.EnemyTypeConfigInspector : Editor`         | `[CustomEditor(typeof(EnemyTypeConfig))]`：复用同一套首帧预览，展示 `AnimConfig` 引用配置的首帧 Sprite，方便直接从敌人类型配置确认外观来源；只读，不修改配置数据。                                                                       |

## Config/

ScriptableObject 配置的编辑工作流。

| 文件                    | 类型 / 命名空间                                | 作用与入口                                                                                                                                                                                               |
| ----------------------- | ---------------------------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `EnemyConfigEditor.cs`  | `EditorTools.EnemyConfigEditor : EditorWindow` | 菜单 `Tools/05 Enemy Config Editor`：在一个窗口内新建与编辑 `AnimFrameConfig`（序列帧）和 `EnemyTypeConfig`（敌人数值/碰撞体/显示），新建资源自动保存到 `Assets/Configs/Survivor/` 对应子目录。预览区随上方选中的 `AnimFrameConfig` 实时刷新：切换现有配置、新建配置或新增/修改 Clip 与帧后，下一次重绘即显示对应默认 Clip（无默认则第一组有效 Clip）的首帧；也可手动拖入 Sprite 覆盖。选中 `EnemyTypeConfig` 时预览图叠加碰撞体矩形（绿色 Vertical / 橙色 Horizontal），拖动中心改 `colliderOffset`、四角改 `colliderSize`，实时写回配置；另附诊断按钮输出预览来源与 Clip/帧状态到 Console；可指定 `EnemyCommon.prefab` 并把选中的 `EnemyTypeConfig` 实例化到当前场景。多帧播放预览尚未实现。 |

## 生成产物与边界

- 生成输出路径定义在 `CodeGeneration/CodeGenerationPaths.cs`，产物落在 `Assets/Scripts/Define/Editor`、`Assets/Scripts/Define/UI`、`Assets/Scripts/Modules/Presenters`。
- `Assets/Scripts/Define/Editor/` 下的 `GameTag.cs`、`GameLayer.cs`、`GameSortingLayers.cs`、`SceneUIName.cs` 是上述工具的输出产物，文件头注明请勿手动修改。
- `Assets/ThirdParty/xLua/**/Editor`、`Assets/ThirdParty/FastScriptReload/**/Editor` 属于第三方包，升级时可能被覆盖，不纳入本目录维护。

## 维护约定

- 新增 Editor 工具时按职责放入五个目录；存在调用关系的工具放同一目录。
- 移动脚本时必须连同同名 `.meta` 一起移动，保留 GUID，避免序列化引用丢失。
- 不要随意修改 namespace：现有文件分别位于 `EditorTools` 和全局命名空间；尤其 `CodeGenerationPaths` 是全局共享配置，添加 namespace 需同步修改四个调用方。
- `[MenuItem]` 路径与物理目录无关，调整目录时不应顺带改变菜单路径（`Tools/01` 至 `Tools/04` 保留现有顺序）。
- 若将来为子目录新增 asmdef，需要重新处理对 `CSObjectWrapEditor`、`UnityEditor.UI`、`UnityEditor.U2D.Sprites` 以及 Framework 运行时类型的程序集引用。
