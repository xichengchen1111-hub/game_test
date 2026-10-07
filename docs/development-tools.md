# MVP 技能与工具

## 项目技能

这四个技能专用于《错日证词》，源文件随仓库保存于 `skills/`，并安装到当前用户的技能目录。正常自动匹配保持开启；下一轮对话可使用 `$技能名`，若列表未刷新可重启应用。技能提供工作流，Unity 与 Blender 仍通过本机程序和脚本执行。

| 技能 | 用途 | 已提供的实际内容 |
| --- | --- | --- |
| `wrong-date-narrative` | 主时间线、证据来源、对白条件、自由探索 | 操作说明、开发线索数据及可执行静态校验器 |
| `wrong-date-unity` | 第一人称、分场景、门口转场、调查簿、报告、存档 | 对稳定 ID、许可、角色提醒、生成资产和实际验证的约束 |
| `wrong-date-blender` | 模块建筑、低模道具、简化人物 | 画风、时代、编辑源、比例、导出及 Unity 导入检查流程 |
| `wrong-date-playtest` | 自由探索与实机验收 | 6 种访问排列、静默陪伴、回访、重启读档、报告和可读性检查 |

本机沿用当前 `skill-installer` 的用户安装位置 `C:/Users/jason/.codex/skills/`。源与安装副本按 SHA-256 验证一致。复装只需在仓库运行：

```powershell
python tools/install_project_skills.py
```

已由本仓库安装且未被手工改动的技能，可以用 `--update` 更新。安装器先检查全部目标，遇到同名未知技能或本地修改会保留它们。也支持 `--dest` 明确指定另一台机器的技能目录。

线索校验命令：

```powershell
python skills/wrong-date-narrative/scripts/validate_story.py docs/mvp-evidence.json
```

这条检查仅覆盖开发数据：ID、来源、相对日期、钟差计算、提示独立性和访问顺序的可达性。没有运行 Unity，不能当成实机验收或谜题公平性通过。

## 已有技能与应用

当前会话已有 `imagegen`、`pdf`、`skill-creator` 和 `skill-installer`，无需重复安装。Windows `computer-use` 的 `sky` 接口可对实际游戏窗口截图、点击和发送按键，用来检查玩家入口与操作；当前 Mac 的统一 Computer 工具也可控制原生应用。Unity 场景与 Blender 资产优先通过各自 CLI 和脚本制作，不假定存在编辑器专用控制 API。

| 应用 | 本机确认 | 开发用途 |
| --- | --- | --- |
| Unity Editor | `6000.3.18f1`，已完成小镇/钟表铺的 10% 切片、URP 配置、Windows 构建与专项实机验收；用户已实机确认 Mac 能运行，本机另已构建并启动 universal `TownDemo.app` 到标题画面；完整 MVP 尚未完成，详见 [Agent 记忆](agent-memory.md) | Editor 脚本生成场景、测试、Windows 与 macOS 构建及实机截图 |
| Blender | 当前 Mac：`4.5.9 LTS`，CLI 已返回版本 | Python 生成低模模块，保存 `.blend`，导出 FBX |
| ImageGen | 内置工具已可用，现有三张概念图已生成 | 风格、纹理和装饰图；证物字样另行精确排版 |
| Python 与 Git | 当前环境已可用 | 线索数据校验、文档生成、技能安装和版本管理 |
| agy | 本轮检查返回 `SUCCESS`，主代理已复核 | 流程草稿和批量工作；不替代最终剧情与试玩判断 |

这次查询的官方精选技能目录没有合适的 Unity/Blender 制作项，因此补齐的是项目技能。当前方案不需要新连接账户或额外 MCP 服务；程序已经安装，也不需要再次下载游戏引擎。

## 官方依据

- [技能结构与加载方式](https://learn.chatgpt.com/docs/build-skills)：用短说明和可选脚本组织技能；新技能可自动发现，未刷新时重启。
- [Unity Editor CLI](https://docs.unity3d.com/6000.3/Documentation/Manual/EditorCommandLineArguments.html)：静态 Editor 方法、项目路径与日志参数。
- Blender CLI 以本机 Blender 的 `--help` 与版本输出为当前验证依据；实际操作符应在对应版本中再核对。

[MVP 方案](mvp-demo.md) · [三页图文简版](first-person-3d-plan.md)
