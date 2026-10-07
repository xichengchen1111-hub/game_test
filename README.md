# 错日证词

第一人称 3D 悬疑游戏。玩家自由探索小镇；室内是独立的完整三维场景，通过门口淡黑转场。同行者只偶尔随口提醒。

## 10% 视觉试玩

Unity 项目在 `game/`，本地 Windows 游戏在 `builds/windows/TownDemo.exe`，双击运行。此批完成黄昏街区、钟楼、钟表铺室内、送奶少年和钟表匠、怀表、物件检查、调查簿及自动保存；完整案件和另外两处室内留在后续 MVP 批次。

Mac 运行状态：用户于 2026-10-07 实机确认试玩可以运行；本机也已生成并启动 Apple Silicon 兼容的 `TownDemo.app` 到标题画面。当前具体构建复验范围见 [Agent 记忆](docs/agent-memory.md)。

WASD 行走，鼠标自由观察，Shift 快走，E 互动，Q 查看怀表，Tab 调查簿，Esc 收起或暂停。靠近右侧钟表铺的门，按 E 进入。所有建筑、家具和人物使用三维几何体与碰撞；招牌与纸面使用可读贴图。

钟表铺内外共用 `game/Assets/Generated/ClockShop.prefab` 和 `ClockShopLayout.asset`：外轮廓 7.4 × 8.2 米，单层房间，3.3 米墙高，中央门两侧各有一扇 2.1 × 1.75 米真窗。门窗的位置、高度、朝向，以及屋顶、家具和人物模型都一致；从街上透过窗户能看到同一套室内模型。外景只保留进门互动，证物与对白在独立室内进行。

窗外直接复用小镇的三维建筑、钟楼、路面和街灯；墙体留出真实窗洞，窗框有厚度，玻璃仅有轻微透明色。玩家走近或横向移动时，窗框和远处街景产生不同视差，街钟也继续使用同一游戏时钟。街景坐标变换由同一布局推导，移除重复的本栋建筑；窗外副本只负责显示，不复制证物交互或碰撞。独立室内仍通过淡黑转场加载。

以后所有可进入建筑都必须保持内外一致。修改共用布局/Prefab 后再生成两个场景；不能在外面添加没有对应室内空间的窗户或楼层。构建前的 `BuildingLayoutChecks` 比较实际场景中的门窗和每个模型的几何、材质及变换，并验证故意错位、缺失和新增的窗户会被拒绝。

`output/demo/` 保存实机截图和行走视频。验证走实际 CharacterController 与射线交互，包括进出房间、记录 E07、静默同行者和新进程读档；这是程序控制的实际运行检查，另需持续真人试玩。当前试玩不自动解题，不限制玩家探索顺序。

窗景专项验证使用 `--review --fresh --review-window --capture-dir "绝对输出目录"`，输出 `layout-exterior.png`、`layout-interior.png`、左右窗近景和 `window-results.json`，检查内外窗数、实际横移视差、两扇窗洞通透、人物不能穿窗，以及之后正常查看证物和返回街道。此次一致性验收保存在 `output/demo/layout/`，含构建检查和新进程读档结果。

运行时附加 `--window-preview` 可直接站在钟表铺内看窗景，仍可自由移动和出门；这个展示入口使用独立存档，不覆盖正常试玩进度。

窗框嵌入窗洞，内侧与墙面留有 5 厘米错位，并突出外侧墙面 3 厘米，边框和横档接缝不再重叠，避免移动或转头时出现共面争抢（z-fighting）。室内两盏带阴影的点光源明确采用 512 像素的阴影面，十二个面可放入 2048 像素图集，消除自动降分辨率提示。

运动闪烁复验使用 `--review --fresh --review-lighting --capture-dir "绝对输出目录"`，通过实际角色控制器往返移动、相机转动和步行起伏捕捉原生画面，记录固定墙面位置的颜色。诊断参数 `--lighting-no-main`、`--lighting-no-local`、`--lighting-no-ao` 仅在该验证模式生效。`tools/check_lighting.py` 对比修复前后的采样；`output/demo/lighting/comparison.json` 和移动视频保存验证结果。验收使用完整的正常灯光与阴影。

附加 `--lighting-window-right` 以镜像移动路线单独复验第二扇窗，两扇窗的原生采样保存在 `output/demo/layout/lighting/`。`--window-preview` 展示入口面向完整的双窗和中央门。

店铺棚架共用生成函数，柱子、横梁和棚顶有实际连接，连接高度从棚底推导。长椅放在车站与旅馆之间的人行道外缘，朝向广场，脚底与路面齐平，旁边保留约 0.95 米通道。窗景专项验收也检查三处棚架的六根柱子，并通过角色控制器走过长椅旁的通道，实机截图与结果在 `output/demo/street/`。

`--street-preview` 可直接站在钟表铺对面查看棚架，随后自由走动；它使用独立展示存档。

重新构建：安装 Unity **6000.3.18f1**，Windows 运行 `tools/BuildDemo.ps1`；Apple Silicon Mac 运行 `tools/BuildDemoMac.sh`，输出到 `builds/macos-arm64/TownDemo.app`。Blender 源模型在 `assets/source/town-kit.blend`；生成脚本在 `tools/make_kit.py`。Noto Sans SC 字体子集附带 OFL 许可。生成的游戏包、Unity 缓存和日志不纳入 Git。

送奶少年已替换为金发蓝眼儿童模型，使用浅色送奶制服、圆帽、皮挎包和双手握肩带的姿态。可编辑文件在 `assets/source/milkboy/Milkboy.blend`，Unity 导入位于 `game/Assets/Art/Milkboy/`；Blender 4.5.9 LTS 运行 `tools/build_milkboy.py` 可重新生成。基础模型为 silentsheri 的 [Toon Boy](https://sketchfab.com/3d-models/toon-boy-bffaf3a444e64ed0aa012e0cf43cf8c3)，遵循 [CC BY 4.0 署名说明](assets/source/milkboy/LICENSE.txt)。

本轮 Mac 构建和 E04 对话检查结果保存在 [output/demo/milkboy](output/demo/milkboy)。其中 `unity-*` 是实际游戏画面，`model-*` 是 Blender 建模预览；特写采用受控机位，不能替代完整自由探索验收。

Windows 试玩版为本机 Unity 6.3 的原生关闭问题加入退出兼容处理：先保存，再结束自身进程。编辑器保持默认退出行为；升级引擎后需复核并移除这个临时处理。

## 文件

- `错日证词_修订稿.pdf`：阅读与审阅版。
- `错日证词_修订稿.docx`：可继续编辑的源文档。

本项目为尚在开发中的虚构悬疑游戏企划。

## 实现企划

- [Agent 持久记忆](docs/agent-memory.md)：后续 agent 必读，记录已确认约束、已修复问题、复验方法和未完成事项；入口规则见 `AGENTS.md`。
- [自由探索 MVP 方案](docs/mvp-demo.md)：场景、线索、随口提醒、阶段报告、开发批次和验收标准。
- [第一人称 3D 图文简版](docs/first-person-3d-plan.md)：保留三张低模概念图，明确自由探索与制作范围。
- [三页图文 PDF](output/pdf/first-person-3d-plan.pdf)：可直接审阅的图文版。
- [开发工具与技能安装](docs/development-tools.md)：四个项目技能已制作并安装，可从仓库复装。

上方试玩是约 10% 的视觉与操作切片；链接中的完整 MVP 仍是后续制作目标。概念图与 `output/demo/` 中的实机画面分开保存。
