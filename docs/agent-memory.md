# Agent 持久记忆：错日证词

最后核对：2026-10-07。实现与 Windows 程序验收基线：提交 `e594c3501aec1532f3ee3a656c1c0cd6c0bc94cc`，用户已看过并认可这一版，然后要求推送。用户随后实测确认 Mac 能运行，详见 PLATFORM-01。本文记录这次开发的决定、实际问题与修复，供其他机器、其他 agent 和后续会话接续；不得把历史验收直接当作新改动的验收。

## 进入项目时必须执行

1. 确认当前目录和 Git 状态。工作区的有效仓库是 `game_test/`，远端为 `https://github.com/xichengchen1111-hub/game_test.git`；`detective-demo/` 只是旧参考，未经用户要求不要修改。不要依赖本机绝对路径寻找项目。
2. 阅读仓库根目录 `AGENTS.md`、本文、[当前 MVP](mvp-demo.md)，再按任务阅读 `skills/` 下对应技能。用户最新指令优先；旧企划不能覆盖后来明确的玩法和画风约定。
3. 先辨认已实现与计划：当前是约 10% 的视觉/操作切片，只有小镇、钟表铺和常驻系统场景。完整案件、车站/住宅室内、完整推理报告闭环都尚未完成。
4. 修改场景前找到生成源。`BuildSlice.Build` 会重新生成 Prefab 和场景；不能只在生成后的 `.unity` 文件里补一个位置，下一次构建会覆盖它。先检查是否存在手工修改，保留稳定 `.meta` GUID。
5. 修复后按本文复验受影响的行为，记录真实输出及限制，并更新相应条目。编译通过、概念图或旧截图都不能证明新版本可以玩。

## 已确认的长期约束

| 事项 | 必须遵守 |
| --- | --- |
| 游戏空间 | 第一人称、完整可行走的三维空间。小镇是 3D 地图；进屋淡黑转场，室内是独立完整 scene。横版、固定机位和静态图片不能代替游戏空间。 |
| 画风与成本 | 环境保留简化低模、宽色块、灰蓝/紫灰暗部和暖黄室内光，1931 年道具保持时代一致。2026-10-08 用户明确提高送奶少年的精度要求：用真实儿童基础网格和骨骼，细化金发蓝眼、脸、手、帽子与制服布料；旧的“无复杂五官”要求不再适用于该角色。 |
| 探索顺序 | 玩家自由选择线路。完整 MVP 的三个室内从开局开放；当前切片的钟表铺也从开局开放。不能把企划精简误做成强制单线任务。 |
| 同行者 | 只偶尔随口提醒，不承担律师、带路、审批、解锁证物或自动解题的职责。完全静默也不影响核心调查。 |
| 推理 | 证物使用稳定 ID；开发合同 `mvp-evidence.json` 的隐藏事实与推理答案不能直接写到物件 UI。当前钟差不能单独证明案发时钟差，未验票不能单独证明没有离镇，疑点不能自动变成定罪。 |
| 建筑 | 每座可进入建筑的内外共用一份布局和一个 Prefab。占地、层数、墙高/屋顶、门窗数量/位置/尺寸/朝向、可见陈设与窗景一致；不能有对应不到真实室内空间的外窗或假二楼。 |
| 展示与验收 | 概念图放 `docs/concepts/`，实际运行证据放 `output/demo/`；不得混称。程序控制的实机验收不能宣称是陌生玩家盲测。 |
| 命名与提交 | 新建分支、提交、PR、worktree、任务、产物名称遵守根目录命名规则。提交前检查文件范围；构建包、缓存、原始逐帧数据和临时日志不入 Git。 |

## 问题记录与防复发规则

### NPC-01：送奶少年改用完整儿童网格（已接入，待用户美术反馈）

- **造型约定入口（2026-10-08）**：用户要求总结并写入 agent 文档，已在根目录 [AGENTS.md](../AGENTS.md#送奶男孩造型与制作约定) 集中记录头脸、帽子、制服、挎包、握带动作、参考图用途和近景检查要求；后续修改先读该节。当前实现已随 `f57cec1` 推送 GitHub；本次造型总结仅更新文档，未重新制作模型或运行游戏验收。
- **用户要求**：原先方块帽冠/袖子和球体手部被用户否决。改找相似儿童基础模型，完成金发蓝眼、浅色老式送奶制服、圆帽、领结、皮挎包；双手握住肩带，呈现被问责时的紧张姿态。钟表匠独立保留。
- **资源决定（2026-10-08）**：指定的 Menglow 资源需购买，用户明确放弃该方案。采用 silentsheri 的免费 `Toon Boy`（Sketchfab ID `bffaf3a444e64ed0aa012e0cf43cf8c3`），页面及下载窗口明确为 CC BY，允许修改与商用，须署名。作者原始 FBX/纹理保存在 `assets/source/milkboy/vendor/`；授权及改动说明在相邻 `LICENSE.txt` 和随游戏分发的 `Assets/StreamingAssets/ThirdPartyNotices.txt`。不要删除署名或误称完全原创人物。
- **制作实现**：当前 Mac 已安装并验证 Blender **4.5.9 LTS**。`tools/build_milkboy.py` 从完整儿童脸/手网格生成制服、软帽冠、弧形帽檐、袖口、皮包与肩带；补建 47 根骨骼及权重，使用连续弯曲变形制作握带的静态休止姿态。生成 `assets/source/milkboy/Milkboy.blend`（两张使用中的皮肤纹理已打包）和 `game/Assets/Art/Milkboy/Milkboy.fbx`。这是偏卡通的儿童角色；尚无口型、表情动画或完整走跑动画。
- **Unity 接入**：`MilkboyAsset.Create` 按材质名指定 URP 皮肤纹理、棉布织纹/法线、皮革、金发和蓝眼。依据实际帽檐方向校正导入朝向；角色根朝向 `95°`，比例 `.88`，在东侧人行道 `(6.48, .1, -4.2)`，避免旧位置脚伸出路缘。去掉会让脚底上下浮动的整体 AmbientMotion。保留 E04、对白和简单碰撞。源模型导入休止高度约 `1.5053 m`，场景缩放后约 `1.325 m`。
- **本轮纠正**：原始脸部颈底边缘收进衣领；帽檐/虹膜前向法线正确。第一次运行中虹膜被背面剔除，现已在源网格中反转绕序并重新验证。脚底落点用实际休止顶点计算，不用会膨胀的 SkinnedMeshRenderer bounds。Unity 的默认 BakeMesh 在这个 FBX 层级下产生百倍尺度，已移除该定位方法。不能恢复旧方块拼人作为精细版，不能拿骨骼数/顶点数代替美术评审。
- **实机证据**：Unity `6000.3.18f1` Mac/Metal 构建通过 `MILKBOY_IMPORT_PASS`、`BUILDING_LAYOUT_PASS`、`SLICE_BUILD_PASS`。独立运行 `--review --fresh --review-milkboy` 通过真实角色控制器走近以及 E04 射线交互；脸、手、全身和侧面截图在 `output/demo/milkboy/`。特写使用受控相机并隐藏 HUD，场景灯光/材质未改；这不是手动键鼠或陌生玩家盲测。`source-results.json` 记录重新打开可编辑源文件的检查，`runtime-results.json` 记录交互结果。
- **交付**：`builds/macos-arm64/TownDemo-Milkboy-Rebuilt.app`，桌面副本 `错日证词-送奶少年重制版.app`，独立 company name 为 `Qingchuan Milkboy Rebuilt`。旧桌面版本和正常存档保留；新包不复用旧的越界位置。用户于 2026-10-08 要求将本轮改动提交并推送至 GitHub。上传授权不等于完整玩法或美术验收通过。
- **生成范围**：Town 与 ClockShop 的窗外小镇副本都需要保留新版角色生成差异。ClockShop prefab 本体、Systems 与 Paper 的无关 ID/序列化噪音可恢复构建前内容；不要把含新窗外角色的 ClockShop 场景误当作全无关改动恢复。

### SAVE-02：创建独立 Mac 副本以排除旧位置存档

- **用户反馈**：Mac 游戏持续卡在地图边界外，希望复制一份重新下载。
- **处理**：首次复制只改了 bundle identifier；Unity 6.3 的 macOS Player 存档路径为 `~/Library/Application Support/unity.<company name>.<product name>`，所以仅改 bundle ID 并不隔离。最新副本使用独立 company name `Qingchuan Milkboy Detail`，生成独立存档目录，且构建结束会恢复项目原 company name。
- **产物与限制**：首次桌面副本 `错日证词-新副本.app` 和第二份 `错日证词-新副本-金发男孩.app` 保留；旧版 `错日证词-送奶少年精细版-最终.app` 使用 `Qingchuan Milkboy Detail`。最新模型改为 `错日证词-送奶少年重制版.app`，使用 `Qingchuan Milkboy Rebuilt`；旧副本均保留。新版本已通过自动控制的走近少年与对话，长时间自由探索的边界情况仍待用户确认。

### SPACE-01：窗外只有贴图，没有真实空间感（已修复）

- **用户反馈**：优化窗外景色，原来是贴图，要求按真实空间处理。
- **修复**：`BuildSlice.WindowStreet` 从实际 `Town.unity` 复制 `Town geometry` 的三维几何。墙体按真实窗洞分段，窗框有厚度，玻璃使用 `WrongDate/ClearGlass` 轻微透明色，当前 tint alpha 为 `0.055`。保留街钟的动态指针与同一个游戏时钟。
- **坐标关系**：`townPoint = layout.townPosition + layout.TownRotation * roomPoint`；窗景使用其逆变换。不能手工另造一条相似街景，或靠目测重新摆远处钟楼。
- **必须清理**：室内已经有共用的整栋建筑，复制街景时必须删除本栋 `ClockShop`，防止重叠表面；街景副本删掉所有 `Interactable` 和 `Collider`，不复制线索或阻挡室内玩家。实际窗洞另有 Ignore Raycast 层的安全碰撞，防止玩家穿窗而不抢证物射线。
- **验收**：真实横向行走产生窗框与远景的相对视差；靠近左右两窗能看到窗外；窗洞通透、玩家不能穿窗；查看 E07 和进出门仍然正常。最新证据：`output/demo/street/window-results.json`（2/2 窗、视差约 `0.3124`），同目录左右窗远近截图。
- **禁止回退**：不能用平面街景图或视频屏幕替代真实几何；不能重新引入自身建筑、重复证物和街景副本碰撞。

### RENDER-01：移动/转头时光影闪烁（已修复，须动态复验）

- **用户反馈**：最新录屏里一动就闪。静止截图不足以发现此问题。
- **已确认原因与处理**：窗框和窗洞侧面存在共面争抢（z-fighting）；从室外看，早期框深也与外墙共面。`BuildSlice.ClearWindow` 让框嵌入窗洞边缘 5 厘米；框深由 `wallThickness + 0.10` 推导。当前墙厚 `0.20`、框深 `0.30`，开口局部中心偏移 `0.02`，外侧突出约 3 厘米。横档、立框也避免相互重叠的同一表面。
- **另一个已处理问题**：两盏带阴影点光源需要 12 个立方体面。当前明确选择每面 512 像素，使其能装入 2048 像素阴影图集，避免自动降分辨率提示。`Point` 通过 `SerializedObject` 写 URP 的 `m_AdditionalLightsShadowResolutionTier`；不要重新使用该版本不可写的公开属性导致编译失败。
- **定位方法**：先检查重复网格、共面接缝和真实阴影预算，再按需用 `--lighting-no-main`、`--lighting-no-local`、`--lighting-no-ao` 隔离原因。这些开关只用于 review 诊断；验收必须恢复完整正常灯光、阴影和 AO，不能把关掉效果当作修复。
- **验收基线**：左窗原生运动样本 86 帧，固定窗洞侧面亮度范围由 `0.3065` 降到 `0.00392`，降幅约 98.7%；右窗 83 帧，范围 `0.00315`。右窗是新建的，没有旧版右窗对照，不能给它编造下降百分比。证据：`output/demo/layout/lighting/comparison-left.json`、`stability-right.json`，以及 `output/demo/layout/lighting-left.mp4`、`lighting-right.mp4`。
- **禁止回退**：不能只检查一扇窗或单张截图；不能用相机不动的帧代替走动/转头复验；不能声称此局部指标证明整个游戏所有光照或所有显卡都没有问题。

### SPACE-02：室外两扇窗，室内一扇窗，房间内外不一致（已修复）

- **用户要求**：今后所有房间的内外必须统一，不是只补这一扇窗。
- **唯一来源**：`game/Assets/Scripts/BuildingLayout.cs` 定义布局类型；实际钟表铺数据为 `game/Assets/Generated/ClockShopLayout.asset`，建筑为 `ClockShop.prefab`。`BuildSlice.ClockShopPrefab` 生成一次，`InstantiateShop` 在小镇与独立室内实例化同一个 Prefab。修改布局资产/生成逻辑后生成两侧，不分别修补两张 scene。
- **当前基线**：占地 `7.4 × 8.2` 米（含墙厚），单层，墙高 `3.3`、墙厚 `0.2`。正面两窗中心横坐标 `-2 / +2`，中心高 `1.95`，开口 `2.1 × 1.75`；中门 `1.4 × 2.4`。小镇位置 `(9.72, 0, 1)`、朝向 `90°`，室内出生点 `(0, 0.03, -2.2)`。这些是当前数据，不是禁止以后改尺寸；以后改动仍须统一来源并复验。
- **已清理**：删除没有真实室内对应的假上层；共用屋顶、棚架、家具和人物。从街上透过窗户能看到同一套陈设。街道地砖/人行道按实际建筑占地切除，防止穿进室内地板。外景实例只开放进门交互，证物和人物对话在独立室内进行。
- **构建门槛**：`BuildingLayoutChecks.ValidateDefinition/ValidateScenes` 比较实际加载的实例、每个 mesh/material/transform、门窗及出生点；故意移动、删除和新增窗户必须被拒绝。当前验收见 `output/demo/layout/build-layout-results.json`。
- **范围提醒**：现有校验和场景组装针对 ClockShop。新增车站/住宅时必须扩展到相应建筑、门户与测试，不能只引用“所有建筑统一”的规则就宣称新建筑已被自动检查。
- **实机验收**：查看两侧外观及透窗陈设，走两扇窗的近景与碰撞检查，实际射线进门、返回正确门口。只比较配置数字不足以证明生成结果一致。

### PROP-01：室外柱子顶与棚顶有空隙（已修复）

- **原因与修复**：棚子和柱子的独立高度没有连接约束。三处店铺统一使用 `BuildSlice.Porch`：棚顶、连接横梁和柱子在同一局部坐标生成，柱高从实际棚底推导。
- **当前关系**：棚中心高 `3.05`、厚 `0.15`，棚底 `2.975`；横梁顶 `2.985`、梁高 `0.14`；柱高 `2.855`。梁与棚、柱与梁分别有 `0.01` 米连接余量。以后改变棚高、宽度或朝向，要保留这种计算关系，不手工分别填柱高。
- **验收**：当前 3 个棚架、6 根柱子，实际 renderer bounds 检查接合位置；同时看玩家高度正面与接缝近景。证据：`output/demo/street/street-results.json`、`canopy-front.png`、`canopy-joint.png`。
- **范围提醒**：`DemoApp.ReviewStreet` 当前断言数量 `3/6`。合法新增棚架时更新预期覆盖，不能删掉检查来绕过失败。

### PROP-02：长椅位置和朝向不合理（已修复）

- **修复**：长椅从中央道路附近移到车站与旅馆之间的西侧人行道外缘，位置 `(-7.55, sidewalk.bounds.max.y, 7.5)`，面向广场 `+X`；脚底按实际路面顶面放置，不能固定一个会悬空的高度。
- **导入陷阱**：FBX 导入轴向不能凭想象决定“正面”。`FaceBenchTowardSquare` 从实际靠背网格推导朝向，再生成 `Seat facing` 标记。禁止用一个未经检查的 yaw 替代模型朝向判断。
- **验收**：当前实际碰撞通道约 `0.95` 米，检查最低 `0.8` 米；脚底与路面误差不超过 `0.015` 米。角色控制器沿 `x=-6.65, z=6.4 → 8.6` 走过长椅旁通道。证据：`output/demo/street/street-results.json`、`bench-placement.png`。
- **防复发**：摆道具要同时检查视觉位置、朝向、落地和真实碰撞通行；看起来留出路不代表角色走得过去。

### RUNTIME-01：本机 Windows 玩家退出兼容问题（临时处理仍保留）

- **历史问题**：Unity 6.3 的本机玩家在相对鼠标输入后走原生关闭流程会崩溃；单纯延后 `Application.Quit` 未解决；Mono 的 `Environment.Exit` 曾等待工作线程而无法正常结束。
- **当前实现**：`DemoApp` 在 Windows standalone、非 Editor 下拦截 `Application.wantsToQuit`；`RequestQuit → QuitAfterInput` 暂停输入、同步落盘、等 `0.25` 秒，再用 `GetCurrentProcess` 的当前进程伪句柄调用 `TerminateProcess(..., 0)`。仅结束游戏自身，不能按 PID 杀其他进程或 Unity Editor。
- **平台边界**：`kernel32.dll` 与该退出路径始终包在 `UNITY_STANDALONE_WIN && !UNITY_EDITOR` 中；Editor 和其他平台保留正常路径。不能把 Windows 兼容处理复制到 Mac。
- **防复发**：没有复现并验证新引擎版本之前，不要以“清理代码”为由删除此处理。引擎升级后，复测真实鼠标转头、Q/UI 开关、正常退出、退出码及新进程读档，确认原生退出已正常后再移除。
- **已有证据限制**：`output/demo/layout/save-relaunch.txt` 确认新进程恢复 E07、小镇与返回位置；它不等于完整 MVP 的笔记、报告、所有室内存档已测试。

### PLATFORM-01：MacBook 能否运行（用户已实测确认能运行）

- **最新用户证据**：2026-10-07，用户明确反馈“mac能跑了，我试过了”。当前状态更新为：**Mac 已由用户实机确认可以运行**。这条反馈覆盖此前“Mac 尚未验证”的记录；后续 agent 必须保留这一结果。
- **本机 Mac 构建复验**：2026-10-07，以 `e594c35` 的游戏源和本地 Mac 构建改动运行 Unity `6000.3.18f1`。`BuildDemoMac.sh` 生成 `builds/macos-arm64/TownDemo.app`；日志包含 `BUILDING_LAYOUT_PASS` 与 `SLICE_BUILD_PASS`，`lipo -archs` 显示 `arm64 x86_64`，应用实际启动到标题画面。标题画面之后的操作、转场、存档及性能未由本轮验证。构建器、说明和文档记忆改动尚未推送。
- **构建入口**：`BuildSlice.Build` 与 `tools/BuildDemo.ps1` 继续构建 Windows `TownDemo.exe`、Direct3D 11；本地新增 `BuildSlice.BuildMac` 与 `tools/BuildDemoMac.sh`，使用 macOS、Metal，产出 universal app。运行需 Unity `6000.3.18f1`；可通过 `UNITY_EDITOR` 指定编辑器路径。
- **尚未记录的用户测试细节**：用户独立实测所用的 Mac 型号/芯片、macOS 版本、包或提交、启动方式与详细范围仍未知；不能把它与本机本轮构建混为一谈。应用启动到菜单不自动证明完整玩法、帧率、门口转场或读档专项通过。
- **后续开发**：保留上述用户实测反馈与本机构建结果；提交 Mac 构建入口时仍须按目标设备复验。保留跨平台存档及非 Windows 退出路径，不能只改文件扩展名。
- **兼容依据**：本次查询的 [Unity 6.3 官方系统要求](https://docs.unity.com/en-us/engine/6000.3/manual/get-started/install-and-upgrade/getting-started-installing-unity/system-requirements) 支持 Apple Silicon 和满足要求的 x64/Metal Mac，Player 最低 macOS 12。正式制作时按实际 Unity 版本重新核对，并区分 Editor 与 Player 的要求。
- **禁止误报**：不能再写“Mac 不能跑”或“Mac 从未实测”；也不能把用户反馈说成 agent 自行完成的 Mac 验收，给未知型号承诺帧率，或把仅到标题画面的本机验证说成完整玩法验收。

### WORKFLOW-01：生成资产、版本和技能更新不能凭假设（持续规则）

- Unity 版本以 `game/ProjectSettings/ProjectVersion.txt` 为准，当前 `6000.3.18f1`。包版本以实际 lock/解析结果为准：当前 URP lock 为内置 `17.3.0`，虽然 manifest 声明 `17.0.1`。不要因此擅自降级或重建锁文件。
- `BuildSlice.Roof` 已改为更新已有 mesh 的序列化内容并保留 GUID；不能恢复“存在资产就跳过更新”的旧行为，否则代码改了屋顶而场景仍引用旧网格。
- 不同时启动第二个 Editor 打开同一项目。先检查已有 Unity 进程与项目状态；运行构建时用明确项目和日志路径。`tools/BuildDemo.ps1` 当前含 Windows 本机引擎路径，换机器先定位引擎，不能假定路径存在。
- 四个项目技能源在仓库 `skills/`，本机安装位置不随 Git 迁移。安装使用 `python tools/install_project_skills.py`，完整受管副本更新使用 `--update`。安装器拒绝同名未知文件/本地改动时，先比对源文件、安装文件和 receipt，保留差异；不得为了强行更新伪造安装校验记录。
- 本轮四个技能已安装/更新；另一台机器须自行安装。根据任务选 `wrong-date-narrative`、`wrong-date-unity`、`wrong-date-blender`、`wrong-date-playtest`，不要把“技能已安装”当作程序或平台已测试。
- `.gitattributes` 已将 `.unity/.prefab/.asset/.mat/.meta` 的空 YAML 值行尾空格排除于空白检查；这是 Unity 序列化格式。不要因此忽略 C#/Python/Markdown 的真正空白错误。

## 可复现验收入口

从仓库根目录执行。先确认已有 Unity/玩家进程，避免同项目并发构建和同一 review 存档的并发写入。以下是现有入口，不代表本记忆文件创建时又重新运行了游戏。

| 用途 | 入口与通过条件 |
| --- | --- |
| Windows 构建 | `powershell -NoProfile -File tools/BuildDemo.ps1`；退出码 0 且 `output/demo/build.log` 有 `SLICE_BUILD_PASS`，构建前必须有 `BUILDING_LAYOUT_PASS`。 |
| 建筑/窗景/棚架/长椅 | 玩家参数 `--review --fresh --review-window --capture-dir "绝对输出目录"`；日志有 `STREET_REVIEW_PASS` 和 `SLICE_REVIEW_PASS`，生成 `street-results.json`、`window-results.json`、`review-results.json` 和两侧实际截图。 |
| 左窗运动闪烁 | 玩家参数 `--review --fresh --review-lighting --capture-dir "绝对输出目录"`；日志有 `LIGHTING_REVIEW_PASS`，生成 `lighting-results.json` 和原生帧。检查输出完整后再做亮度分析及运动画面复核。 |
| 右窗运动闪烁 | 上一条再加 `--lighting-window-right`；单独输出、单独复核第二扇窗。 |
| 磁盘重启读档 | 完成正常 review 并确认玩家退出，再用 `--review --verify-save --capture-dir "另一个绝对输出目录"` 启动新进程；**不加 `--fresh`**，通过标志 `SLICE_SAVE_RELAUNCH_PASS`。当前此入口仅验证 E07、Town 和返回位置。 |
| 室内展示 | `--window-preview`，正常可行走，使用独立展示存档。 |
| 室外展示 | `--street-preview`，站在钟表铺对面，正常可行走，使用另一份独立展示存档。 |
| 剧情静态检查 | `python skills/wrong-date-narrative/scripts/validate_story.py docs/mvp-evidence.json`；只验证开发数据，不证明游戏交互和盲测公平性。 |

玩家可执行文件为 `builds/windows/TownDemo.exe`；日志可用 Unity 的 `-logFile "绝对日志路径"` 指定。review/lighting 入口使用真实角色控制器与射线，但自动驱动运动；不能称为人手键鼠或陌生玩家盲测。`review-results.json` 的 `renderedFrames: 6` 是旧字段的基础截图计数，不是整轮帧数，也不是 FPS；运动帧数看相应专项报告。

亮度对比工具的确切用法：

```powershell
python tools/check_lighting.py output/demo/lighting/before/lighting-results.json output/demo/layout/lighting/left/lighting-results.json --output "新输出目录/comparison-left.json"
```

先创建新输出目录。工具要求正常灯光、至少 20 个运动帧和有效窗洞侧面样本，修复后该表面亮度范围 `< 0.06` 且 `< 旧值 × 0.25`；还要检查视频。右窗没有历史 before，只能对完整灯光下的新采样做绝对稳定性验证，不能拿左窗假充它的旧样本。参数、门槛以后若改动，同步更新此处。

新验收写独立输出目录，不覆盖 `output/demo/lighting/before/` 等历史基线。临时输出可放已忽略的 `builds/review/`；将要提交的精简报告、截图与录屏复制到明确的 `output/demo/` 子目录，检查原始帧/日志的忽略规则后再提交。

存档由 `Application.persistentDataPath` 推导，正常试玩为 `slice-save.json`，review 为 `review-save.json`，展示为 `window-preview-save.json` / `street-preview-save.json`。不要硬编码某个人的用户目录、删用户正常存档来做自动验收，或以展示存档证明正常试玩的读档行为。

## 尚未完成，不能误报为已通过

- 完整 MVP 的车站/住宅场景、10 条证物完整交互、问法重算、片段回看与重排、玩家提交的推理报告闭环。
- 三个室内首次访问的全部 6 种排列，以及完整 MVP 的记录先于问人、回访、未完成报告继续探索等玩法验收；当前切片没有足够场景进行这组测试。
- 独立陌生玩家盲测与谜题公平性审阅；现有自动化结果只支持明确执行过的路径。
- Mac 的运行方式/被测产物记录、可复现构建入口和详细专项验收；**用户已实机确认 Mac 能运行**，不能把这一结果列为未验证。其他平台及目标机器上的 FPS/内存/首局时长实测仍待记录；不能从低模或帧采样数量推断流畅度。
- 退出兼容处理在引擎升级后的复核。它是本机 Windows 的临时处理，不是跨平台默认退出方案。
- 首章后续剧情修订：情夫不在场证明与坏结局冲突；保险年份统一为两年多；煤车只明确出现在一次离屋事件；20:40 街灯应已亮。这些不扩成当前 10% 切片的新场景。

## 后续 agent 如何维护本文

每次修复新问题或改变已确认约定，更新本文，不另建互相冲突的“最终版”记忆文件。新条目写清：用户反馈 → 已复现现象/原因 → 最终实现与代码入口 → 禁止回退的规则 → 实际复验命令/证据 → 未验证部分。保留旧问题 ID；合法改变尺寸或平台时更新当前值，并保留改变原因。

用户明确修改玩法时，先同步 `docs/mvp-demo.md`、代码/数据和相关技能，再同步本文；不能以本文冻结旧决定。程序改动后的验收另写新结果，不在历史 JSON 中把 `false` 改成 `true`。阶段完成后注明实际提交 ID/日期和被测平台，提供具体产物；没有运行就明确写“未运行”。

本文件通过根目录 `AGENTS.md` 成为每轮必读入口，通过 Git 随仓库迁移。它不能代替代码、实际复验或人工审美判断；遇到记录与代码冲突，先核实并修正记录，再继续开发。
