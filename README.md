# unity

自学习，自搭建框架。

## 模块分级


| 模块分级 | 模块内容                                             |
| -------- | ---------------------------------------------------- |
| 简单模块 | UI组件、活动框架、辅助类                             |
| 中等模块 | 场景管理、缓动管理、音频管理、界面管理               |
| 复杂模块 | 资源管理、网络管理、消息管理、定时器管理、对象池管理 |

## Codex Skills

项目内的 Codex Skill 保存在 `.codex/skills/`，会随 Git 提交。首次在新电脑克隆项目后，在项目根目录执行：

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\Install-CodexSkills.ps1
```

该命令会把 `unity-mvc-development` 安装到当前用户的 Codex Skills 目录。安装完成后重新打开 Codex，即可使用 `$unity-mvc-development`，或直接要求按 MVC 约定开发业务模块。

如需预览而不写入用户目录：

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\Install-CodexSkills.ps1 -WhatIf
```

