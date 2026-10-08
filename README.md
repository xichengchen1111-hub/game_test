# 错日证词

第一人称 3D 悬疑游戏。玩家自由探索小镇；最新钟表铺采用实体门，在同一三维场景内直接进出。同行者只偶尔随口提醒。

## 10% 视觉试玩

Unity 项目在 `game/`，本地 Windows 游戏在 `builds/windows/TownDemo.exe`，双击运行。此批完成黄昏街区、钟楼、钟表铺室内、送奶少年和钟表匠、怀表、物件检查、调查簿及自动保存；完整案件和另外两处室内留在后续 MVP 批次。

Mac 运行状态：用户于 2026-10-07 实机确认试玩可以运行；本机也已生成并启动 Apple Silicon 兼容的 `TownDemo.app` 到标题画面。当前具体构建复验范围见 [Agent 记忆](docs/agent-memory.md)。

WASD 行走，鼠标自由观察，Shift 快走，E 互动，Q 查看怀表，Tab 调查簿，Esc 收起或暂停。靠近右侧钟表铺的门，按 E 开门后自行走入；门也可从两侧关闭。所有建筑、家具和人物使用三维几何体与碰撞；纸面使用可读贴图。

钟表铺按用户选定的 A / Walnut Heritage 制作：深绿木饰面、铜色雨棚、胡桃木玻璃展柜、多种座钟和摆钟、橡木地板、地毯、维修工作台与工具。内外使用同一 Town 建筑和 `game/Assets/Generated/ClockShop.prefab`：7.4 × 8.2 米，单层，3.3 米墙高，中央门两侧各有一扇 2.1 × 2.2 米透明窗。前后门、展柜访问门和零件抽屉可开关；老板保持锁定坐姿模型和循环动画，沿用统一对话 UI；E07 保留。

窗户两侧看到同一 Town 的真实室内外空间，移动有真实视差，窗框有厚度、玻璃轻微着色并保留防穿过碰撞。进出钟表铺不淡黑、不加载或传送。旧 ClockShop 场景只用于历史布局对照；旧室内存档自动映射到世界坐标，保留证物与对白进度。可编辑模型、重建说明在 [assets/source/watch-shop](assets/source/watch-shop/README.md)，本轮 Windows 实机验收在 [output/demo/walnut-shop](output/demo/walnut-shop/README.md)。

以后所有可进入建筑都必须保持内外一致。修改共用布局/Prefab 后再生成两个场景；不能在外面添加没有对应室内空间的窗户或楼层。构建前的 `BuildingLayoutChecks` 比较实际场景中的门窗和每个模型的几何、材质及变换，并验证故意错位、缺失和新增的窗户会被拒绝。

`output/demo/` 保存实机截图和行走视频。验证走实际 CharacterController 与射线交互，包括进出房间、记录 E07、静默同行者和新进程读档；这是程序控制的实际运行检查，另需持续真人试玩。当前试玩不自动解题，不限制玩家探索顺序。

当前专项验证使用 `--review --fresh --review-walnut-shop --capture-dir "绝对输出目录"`，检查实体门阻挡与两侧开关、实际控制器进出、透明窗视差、老板交谈与坐姿循环、E07、抽屉和展柜。随后用 `--review --verify-walnut-save` 启动另一进程复验存档。`output/demo/layout/` 中的旧 portal/window 验收是历史记录，本轮结果见 `output/demo/walnut-shop/`。

运行时附加 `--window-preview` 可直接站在钟表铺内看窗景，仍可自由移动和出门；这个展示入口使用独立存档，不覆盖正常试玩进度。

窗框嵌入窗洞，内侧与墙面留有 5 厘米错位，并突出外侧墙面 3 厘米，边框和横档接缝不再重叠，避免移动或转头时出现共面争抢（z-fighting）。室内两盏带阴影的点光源明确采用 512 像素的阴影面，十二个面可放入 2048 像素图集，消除自动降分辨率提示。

历史独立室内的运动闪烁复验使用 `--review-lighting` 和诊断开关，相关采样、比较及移动视频保留在 `output/demo/lighting/`。旧 harness 使用室内局部坐标，不能直接当作当前 Town 中钟表铺的新灯光验收。本轮保留正常灯光与显式阴影分辨率，查看实际行走截图，未重新声称完成该项逐帧采样。

旧第二扇窗的原生灯光采样保存在 `output/demo/layout/lighting/`。当前 `--window-preview` 展示入口映射到 Town 店内，面向双窗和中央门。

其他店铺棚架保留共用生成函数；新钟表铺改为带斜撑的铜色雨棚。长椅和街面未移动，旧棚架/长椅通道检查保留在 `output/demo/street/`。

`--street-preview` 可直接站在钟表铺对面查看棚架，随后自由走动；它使用独立展示存档。

重新构建：安装 Unity **6000.3.18f1**，Windows 运行 `tools/BuildDemo.ps1`；Apple Silicon Mac 运行 `tools/BuildDemoMac.sh`，输出到 `builds/macos-arm64/TownDemo.app`。Blender 源模型在 `assets/source/town-kit.blend`；生成脚本在 `tools/make_kit.py`。Noto Sans SC 字体子集附带 OFL 许可。生成的游戏包、Unity 缓存和日志不纳入 Git。

送奶少年已按用户 2026-10-08 提供的 Meshy `The Little Messenger` 压缩包替换，保留原始模型、服装和贴图，默认循环呼吸并环顾，另外五段动画保留供后续使用。可编辑源为 `assets/source/milkboy/meshy/Messenger.blend`，Unity 导入位于 `game/Assets/Art/MilkboyMeshy/`。本轮使用 Blender **5.2.0 LTS**；运行 `blender --background --python-exit-code 1 --python tools/import_meshy_milkboy.py -- --archive "原始压缩包路径" --root "仓库路径"` 可重新导入，再按上方入口构建。旧 Toon Boy 源文件和 [CC BY 4.0 署名说明](assets/source/milkboy/LICENSE.txt) 保留用于旧版追溯；其许可不适用于新模型。

新模型的 Windows 构建、循环待机、E04 走近对话及钟表铺回归检查保存在 [output/demo/milkboy-meshy](output/demo/milkboy-meshy)。其中 `unity-*` 是实际游戏画面，`model-*` 是 Blender 建模预览；特写采用受控机位。附加 `--milkboy-preview` 可直接站在男孩前面自由试玩，使用独立展示存档。新模型尚未在 Mac 重建实测；旧版的 Mac 构建和 E04 检查仍保留在 [output/demo/milkboy](output/demo/milkboy)，用户此前确认 Mac 能运行的结论不变。

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

## 当前地图与剧情基准

[本镇第四版地图、建筑坐标与角色动线](docs/maps/town-current/README.md)：19日真实乘车去另一镇问路，20日挪用前一天行程。后续制作统一使用此目录坐标，修改地图必须按AGENTS.md重跑检查；当前尚未迁移Unity。
