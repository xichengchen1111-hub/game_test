# Agent 持久记忆：错日证词

最后核对：2026-10-08。NPC-02 送奶少年已随 `9fc7aec` 推送，UI-01 怀表、ROAD-01 街面和 ROAD-02 色差修正已随 `66fbd4c` 推送；本轮提交包含 NPC-03 的 Meshy 坐姿钟表匠与木椅，用户于 2026-10-08 要求提交推送，实际上传版本以 Git 历史和远端为准。历史 Windows 验收基线为 `e594c3501aec1532f3ee3a656c1c0cd6c0bc94cc`，用户认可后要求推送；用户随后实测确认 Mac 能运行，详见 PLATFORM-01。本文记录决定、实际问题与修复，供其他机器和后续 agent 接续；不得把历史验收直接当作新改动的验收。

## 进入项目时必须执行

1. 确认当前目录和 Git 状态。工作区的有效仓库是 `game_test/`，远端为 `https://github.com/xichengchen1111-hub/game_test.git`；`detective-demo/` 只是旧参考，未经用户要求不要修改。不要依赖本机绝对路径寻找项目。
2. 阅读仓库根目录 `AGENTS.md`、本文、[当前 MVP](mvp-demo.md)，再按任务阅读 `skills/` 下对应技能。用户最新指令优先；旧企划不能覆盖后来明确的玩法和画风约定。
3. 先辨认已实现与计划：当前是约 10% 的视觉/操作切片，只有小镇、钟表铺和常驻系统场景。完整案件、车站/住宅室内、完整推理报告闭环都尚未完成。
4. 修改场景前找到生成源。`BuildSlice.Build` 会重新生成 Prefab 和场景；不能只在生成后的 `.unity` 文件里补一个位置，下一次构建会覆盖它。先检查是否存在手工修改，保留稳定 `.meta` GUID。
5. 修复后按本文复验受影响的行为，记录真实输出及限制，并更新相应条目。编译通过、概念图或旧截图都不能证明新版本可以玩。
6. **Git 操作统一用命令行**：2026-10-08 用户明确要求后续不再打开 GitHub Desktop。每个新任务先 `git fetch origin` 核对远端；保留本地工作后再同步，只有用户授权才提交或推送。此条取代此前通过 Desktop 同步的约定。

## 已确认的长期约束

| 事项 | 必须遵守 |
| --- | --- |
| 游戏空间 | 第一人称、完整可行走的三维空间。最新 A 钟表铺在 Town 同一建筑内实体门直接进出，无黑屏转场；其他未制作室内仍保留原计划。横版、固定机位和静态图片不能代替游戏空间。 |
| 画风与成本 | 环境保留简化低模、宽色块、灰蓝/紫灰暗部和暖黄室内光，1931 年道具保持时代一致。送奶少年、钟表铺老板以各自用户提供的 Meshy 包为准，保留完整人物及贴图；老板全程坐着并配木椅。旧人物的具体外观、握带姿态及方块造型不再约束这些新资源。 |
| 探索顺序 | 玩家自由选择线路。完整 MVP 的三个室内从开局开放；当前切片的钟表铺也从开局开放。不能把企划精简误做成强制单线任务。 |
| 同行者 | 只偶尔随口提醒，不承担律师、带路、审批、解锁证物或自动解题的职责。完全静默也不影响核心调查。 |
| 推理 | 证物使用稳定 ID；开发合同 `mvp-evidence.json` 的隐藏事实与推理答案不能直接写到物件 UI。当前钟差不能单独证明案发时钟差，未验票不能单独证明没有离镇，疑点不能自动变成定罪。 |
| 建筑 | 每座可进入建筑的内外共用一份布局和一个 Prefab。占地、层数、墙高/屋顶、门窗数量/位置/尺寸/朝向、可见陈设与窗景一致；不能有对应不到真实室内空间的外窗或假二楼。 |
| 展示与验收 | 概念图放 `docs/concepts/`，实际运行证据放 `output/demo/`；不得混称。程序控制的实机验收不能宣称是陌生玩家盲测。 |
| 命名与提交 | 新建分支、提交、PR、worktree、任务、产物名称遵守根目录命名规则。提交前检查文件范围；构建包、缓存、原始逐帧数据和临时日志不入 Git。 |

## 问题记录与防复发规则

### PERF-01：低配性能与更多可进入房屋调研

- **用户目标（2026-10-09）**：全面评估更多精致模型和可进入房屋的卡顿风险，优先低配电脑流畅；随后询问高性能电脑能否通过画质设置获得更好的画面。当前任务为调研，正式画质菜单和优化代码尚未接入。开始时命令行 fetch 成功，main 与 origin/main 均为 `aed8b2827f45525c3e04e766db1be9ed82005049`，无新增远端提交。
- **实际统计**：Unity 6000.3.18f1，清单 URP 17.0.1，但 packages-lock 和本机实际加载包为17.3.0，本轮没有升级包；Town 489 Renderer、357,546 三角面，钟表屋含老板 201 Renderer、279,923 三角面；Town 14 实时灯，其中方向光与两盏店内点灯投射阴影。没有 LODGroup、钟表屋自动 Mesh LOD、烘焙 lightmap 或 Town 遮挡剔除数据。PC 为 Forward+；不要把 RenderingMode=2 误判为 Deferred，也不要用每物体 4 灯字段解释 Forward+。
- **实际测量**：Windows/D3D11、Ultra 9 285H／RTX 5060 Laptop／约32GB，独立 release 测试包、1920×1080 可见窗口，临时启用 Frame Timing Stats、取消60帧上限。固定视角每个预热2秒、采样三段各5秒。当前店内 GPU 中位4.0054ms、整帧P95 4.4646ms、665次绘制；试验组合低档分别0.7654ms、2.7535ms、212次绘制。组合低档同时改变阴影、HDR、MSAA、SSAO、不透明颜色纹理与渲染比例，不能归因于某单项或换算为低配帧率。四次实际控制器门口往返7,385帧，最长17.3466ms，没有超过33.33ms；是脚本驱动，不是物理键鼠。没有测冷启动、多房屋容量、低配、Mac或长时间内存增长。
- **保护与证据**：正常存档SHA256前后相同，采样使用独立存档；临时Unity脚本已移出Assets保存在忽略的 `builds/performance-research/instrumentation/`，正式包、场景、ProjectSettings与画质资产未改。完整报告 `docs/performance-research-2026-10-09.md`，小型统计 `output/performance/2026-10-09/`，原始逐帧数据／日志／独立测试包在忽略的 `builds/performance-research/`，汇总工具 `tools/summarize_performance_research.py`。Antigravity只做静态排查，渲染路径等错误由主agent核对纠正；不能把其风险推断写成实测故障。调研结束时未提交；用户随后于2026-10-09明确授权提交推送报告、精简统计与汇总工具，实际远端版本以Git为准。
- **后续约束**：画质档位与房屋LOD／可见性／内存管理同时建设；低档仍保留实体门、双向透明窗、近景重要模型与证据可读性。优先控制额外实时阴影和过多提交，远处陈设简化，确有内存压力再按区域异步装卸。当前 `BuildSlice.BuildFor` 每次构建覆盖HDR／MSAA／阴影参数，正式分档必须修改源逻辑。保留门夹人保护、稳定证据ID、正常存档和锁定老板资源。建议预算是待真实低配校准的目标，不能宣称已经保证多少栋房或多少帧。

### MILKSHOP-01：1931 牛奶店接入送奶男孩身后

- **用户要求（2026-10-08）**：把此前制作的牛奶店直接放入游戏、位于当前送奶男孩身后、大小合适且不能与其他建筑碰撞。保留木质墙体、奶牛黑白花纹、大奶牛形招牌、`milk` 字样和实体木门。用户此次指向当前可玩游戏；本轮没有迁移第四版全镇图，也没有变更 B04 平面方案或人物剧情路线。
- **资源和来源**：可编辑源、贴图和重建脚本在 `assets/source/milkshop/`；Unity FBX、URP 材质及带 12 个简化碰撞体的 Prefab 在 `game/Assets/Art/MilkShop1931/`。参照图片构建几何，招牌图由图像生成工具制作，不能称为实际 Meshy 推理。左右木门可独立编辑，当前固定左开右闭；没有新增开关门交互或剧情证物。店内与外观共用一个实际模型，直接行走进入，无另建室内场景。
- **放置来源**：`MilkShopAsset.Create` 由 `BuildSlice.Town` 调用；根位置 `(11.45, -0.04, -7.35)`、Y 朝向 90°、比例 `0.9`，店面朝街。男孩、奶车及原 E04 交互保持原位。原模型占地 7.2×5.8m，场景占地 6.48×5.22m；包含招牌的高度约 7.07m。完整 renderer 水平边界与最近建筑间隔 `0.7844975m`，这是保守外包围盒间隔，不等同于全段通道净宽。构建检查既有 7 栋建筑间隔、男孩身后关系、人行道/边界、基础高度及碰撞体数，日志为 `MILKSHOP_PLACEMENT_PASS`。位置和路径标记保存在 `MilkShopPlacement`。
- **实际验收**：Unity `6000.3.18f1`、Mac/Metal，可见 3024×1898 运行。`--review --fresh --review-milkshop` 用实际 CharacterController 走近男孩并射线触发 E04、进出牛奶店、往返走通与钟表铺之间的小巷，结果为 `MILKSHOP_REVIEW_PASS`。截图和 JSON 在 `output/demo/milkshop/`；概览使用受控高位相机，其他店门/店内/男孩/小巷截图来自玩家高度。这是自动实机验证，不是手动键鼠或陌生玩家盲测。
- **既有建筑回归**：`Town` 几何继续由 `WindowStreet` 同源复制到钟表铺窗景，副本去除交互与碰撞。`BuildingLayoutChecks` 通过；`--review --fresh --review-window` 通过 `STREET_REVIEW_PASS`、`WINDOW_REVIEW_PASS`、`SLICE_REVIEW_PASS`，两窗通透/碰撞、E07、钟表铺门口往返均通过，窗景视差约 `0.35805`。精简结果与实机图在 `output/demo/milkshop/window-regression/`，27 张运动原始帧移到忽略的 `builds/milkshop-integration/window-motion/`。保留一同生成的 ClockShop Prefab 与场景，不能只恢复旧 Prefab 而破坏新 fileID 引用。
- **交付与范围**：重建当前唯一 Mac 入口 `builds/macos-current/错日证词-新版.app`，桌面同名链接指向它；Info.plist 和实际 review 日志确认应用标识/数据目录为 `com.qingchuanstudio.wrongdatetest.currentmac`，沿用 SAVE-04 防越界措施。日志在忽略的 `builds/milkshop-integration/`。本轮未构建 Windows，未验收完整全镇剧情。平面数据未改；现有 `tools/check_town_map.py` 只读检查通过 70 宅地、20 角色、17 路线和 6 顺序，不能把该数据检查说成完整三维地图已实现。
- **工作区保护**：从本机 `main=123aeeb` 的现有工作区接入；命令行 fetch 因当前 HTTPS 无可用凭据失败，不能声称核实了远端最新状态。构建前已有修改备份在 `builds/milkshop-integration/preexisting/`。保留用户原修改，本轮未提交或推送。
- **收尾核对**：牛奶店验收结束后不带 review/fresh 参数正常启动，`normal-startup.log` 确认使用 `com.qingchuanstudio.wrongdatetest.currentmac/slice-save.json`。同时检测到另一任务正在接入少年新版对话（`DemoApp.Dialogue.cs` 等），这些晚于本轮打包的改动已保留，未重建覆盖它的工作；上面的 E04 记录对应本轮已测二进制，不能沿用为新版多分支对话的验收。

### MILKSHOP-02：牛奶店玻璃不透明，无法看到外面（已修复）

- **用户反馈与原因（2026-10-08）**：用户要求按 AGENTS.md 的内外共用真实空间约定，让牛奶店窗户能看到室外。原 FBX 已有两侧真实窗洞和木窗框，但 `OldWindow.mat` 使用不透明 URP Lit，alpha=1、写入深度，原来的进店/通道验收漏掉了逐窗视觉检查。
- **最终实现**：`MilkShopAsset.Create` 维护既有共享 `OldWindow.mat` 的 GUID，改用已验证的 `WrongDate/ClearGlass`，透明队列 3000、淡色 alpha=0.055、双面、关闭深度写入。只改变这份玻璃材质，木窗框仍使用不透明木材；既有两侧墙体/窗洞、12 个碰撞体、门口、建筑位置和大小保持一致。牛奶店同场景内外与 `WindowStreet` 中的副本共同引用该材质，没有新增照片窗景或另造室外。北窗实际朝钟表铺外墙，南窗朝室外空地与边界，不能把真实邻房遮挡误判为玻璃不透明或用假街景遮盖。
- **构建防回退**：生成器断言 Windows renderer 确实引用这份透明玻璃。重新生成 FBX/场景后不能恢复旧不透明窗片；查看室外的要求适用于新增建筑，不能只引用钟表铺旧验收就认定新建筑已通过。
- **实际验收**：Unity 6000.3.18f1、Mac/Metal，`BuildFreshMac` 构建通过原建筑统一检查及放置检查。可见 1600×900 玩家以 `--review --fresh --review-milkshop-windows` 运行，`MILKSHOP_WINDOWS_REVIEW_PASS`，退出码 0。真实控制器走入店内，依次观察两窗、固定朝向横移、向窗行走确认无法穿出，再从门返回街上；外侧特写使用受控相机。已目视检查两侧内外截图，南窗可见室外地面/围墙/天空，北窗可见相邻钟表铺的实体外墙，外看内能透过玻璃看见柜台/奶瓶。图片与报告在 `output/demo/milkshop/windows/`，日志在忽略的 `builds/milkshop-windows/`。自动行走不是陌生玩家盲测。
- **回归与交付**：再次运行 `--review --fresh --review-window`，`STREET_REVIEW_PASS`、`WINDOW_REVIEW_PASS`、`SLICE_REVIEW_PASS` 且退出码 0，钟表铺两窗与门口往返、E07 仍正常，视差约 0.31247；结果在 `builds/milkshop-windows/clockshop-regression/`。更新现有 `builds/macos-current/错日证词-新版.app`，Info.plist/实际 review 日志确认原应用标识，继续使用同一桌面入口，未改变正常存档逻辑；本轮没有 Windows 构建，未提交推送。保留同时接入的少年对话和数字键修正。

### UI-01：怀表指针在缩放界面中画到表盘外（已修复）

- **用户反馈与复现**：2026-10-08，“怀表时间怎么没了”。计时数据仍存在；旧 `DemoApp.DrawHand` 在缩放过的 `GUI.matrix` 上调用 `GUIUtility.RotateAroundPivot`，旋转轴偏离表盘。1280×720 原生截图中指针出现在怀表左侧；1600×900 的缩放恰好为 1，旧版此分辨率看似正常。该绘制逻辑并非 NPC-02 新增，不能把它归因为模型替换。
- **最终实现**：指针矩形以表心为局部原点，通过 `previous * Matrix4x4.TRS(center, rotation, Vector3.one)` 合成一次外层 HUD 缩放。时、分针读取同一帧的 `Minutes`；保留街钟快 30 分钟的既有逻辑，不增加自动推理或数字答案。
- **防复发**：带缩放的 IMGUI 内绘制旋转元素，明确局部坐标和外层缩放的合成顺序；不能恢复旧的 `RotateAroundPivot` 用法，不能只验 1600×900。怀表显示/隐藏应与 Q 共用 `ToggleWatch`。
- **实际验收**：Unity 6000.3.18f1、Windows/D3D11 构建通过；1280×720、1600×900、1920×1080 各自可见玩家运行 `--review --fresh --review-watch -screen-fullscreen 0 -screen-width 宽 -screen-height 高 --capture-dir "绝对输出目录"`，日志均为 `WATCH_REVIEW_PASS`。检查时间推进、街钟共用时间和开关；`python tools/check_watch.py "输出目录/watch-results.json" --output "输出目录/pixel-results.json"` 对实际 PNG 检查两根指针与关闭画面，三档分辨率每根指针均 20/20 采样命中。历史 720p 截图对照为两根均 0/20；历史对照使用约 17:55 的现有截图，并非新跑旧二进制。
- **证据与限制**：精简证据在 `output/demo/watch/`。自动 review 调用与 Q 相同的开关函数，绕过物理键盘；原生截图已人工查看，不能称为人手键鼠盲测。可见玩家截图，禁止以隐藏窗口产生的黑帧证明通过。本轮未在 Mac 重建实测，也未重跑完整玩法；用户此前 Mac 能运行的确认仍有效。复验使用独立 review 存档，不删除正常进度。

### ROAD-01：1931 年街面、人行道与灯下氛围

- **用户要求与年代依据**：按故事背景制作准确、有氛围、好看的马路和人行道。剧本明确为 1931 年 1 月，未指定现实国家；保留虚构青川镇。采用小块花岗石车行面、较大暖灰砂岩旗石人行道和石路缘，材料构成参考 [Historic England 的历史街道实物记录](https://historicengland.org.uk/listing/the-list/list-entry/1393770)，不能据此把剧情所在地改成英国。潮湿表面是美术提案，不是新确定的案发日雨情或证据。
- **源文件与尺度**：`tools/make_street_textures.py` 生成 `game/Assets/Art/Street/` 三套 1024px 周期纹理及 manifest：车行石约 0.25×0.167m、旗石约 0.8×0.667m；Base 为 sRGB，Normal/Mask 为线性，Mask 的 R=0 金属、A=光滑度。`StreetSurfaces.cs` 生成可重建的低模 mesh，保留 mesh 资产 GUID，按世界米数铺 UV；划分围绕室内地板的街面时不能拉伸纹理或留下重叠顶面。高度图是制作源，不额外启用运行时位移。
- **实际几何**：道路套件从当前地图读取 R01 的 6m 车行面、2m 人行道、1m 设施带设计值。道路中部高 0.025m，靠沟边约 −0.020m；沟宽 0.28m，路缘宽 0.22m、顶高 0.10m，旗石顶高 0.10m。路缘有实体侧面、18mm 倒角及接缝；6 处铁沟盖有真实槽条。通往钟楼广场的石坡将车行面接到广场，角色可以跨路缘。这里是旧视觉切片内的路面套件展示，**没有迁移第四版全镇位置或改变平面图**；2m 是套件设计值，不代表已证明旧切片所有棚柱附近都有连续 2m 净空。
- **灯光与通行**：近街灯位于 x=±3.62，广场段仍为 ±5.7，脚底高 0.10m。灯池 intensity=14、range=7.2，保留冷色暮光与暖光层次，不新增带阴影点灯。街面使用三线性 mipmap、8倍各向异性过滤；128px 环境反射探针只在初次启用时捕获一次，不每帧重绘。长椅未移动；`StreetSurfaceLayout.FootwayInnerEdge` 按长椅所在广场段测量通道，不能用南部较宽 mesh 的整体 bounds 虚增净空。
- **室内外共用**：`BuildSlice.Town → StreetSurfaces.Build` 先生成实际小镇街面，再由既有 `WindowStreet` 复制同一几何并变换坐标、删除本栋副本及副本碰撞/交互。不要另做室内窗景路面。新路面在生成时按钟表铺实际地板 footprint 裁切；旧 `TrimStreetFloor` 不再是本轮街面入口。Prefab/场景构建 ID 会共同更新，不能只恢复旧 Prefab 而留下引用新 fileID 的场景。
- **真实验收**：Windows/D3D11、Unity 6000.3.18f1，最终构建通过 `STREET_SURFACE_BUILD_PASS`、`BUILDING_LAYOUT_PASS`、`SLICE_BUILD_PASS`；可见玩家以 `--review --fresh --review-roads --review-window --record` 运行，1600×900 下通过 `ROAD_REVIEW_PASS`、`STREET_REVIEW_PASS`、`WINDOW_REVIEW_PASS`、`SLICE_REVIEW_PASS`。真实控制器跨路缘并走两侧人行道，长椅旁约 0.95m 通道实际走过；两窗/碰撞/E07/店门往返正常，窗景视差约 0.31266。原生截图和精简报告在 `output/demo/roads/`；道路运动样本由 179 张原生帧按约 5fps 编码，另有 50 张窗景运动帧留在忽略的构建目录。采样频率不是游戏 FPS；不是物理键鼠或陌生玩家盲测，也没有证明全场所有光照均无闪烁。当前怀表截图已复核两根指针可见。
- **平面验证的 Windows 限制**：原 `tools/check_town_map.py` 在默认 GBK 下先发生解码错误，设 `PYTHONUTF8=1` 后仍因 CSV 换行和原始字节哈希报告过期。未覆盖原地图；在独立临时副本执行原检查与重新生成，70宅地、20角色、17路线、6顺序及剧情断言通过，所有导出内容归一化换行后相同。详见 `map-compatibility.json`；不能将此写成原仓库 CLI 原始字节检查通过。后续需单独处理检查器的跨平台换行/哈希可移植性。
- **协作与范围**：Antigravity 在 scratch 起草纹理脚本；首轮因工具权限未产出，隔离目录重试后返回 SUCCESS，主 agent 阅读代码、查看纹理、减弱缝色并接入实机。可编辑来源为 Python/C# 生成源与命名 mesh 资产；本轮不新增复杂建模。未重新构建 Mac，保留用户此前 Mac 能运行的确认；完整全镇/完整 MVP、连续净宽与目标设备性能仍不在本次验收范围。

### ROAD-02：人行道出现笔直的明显色差（已修复）

- **用户反馈与原因**：ROAD-01 首轮截图中，设施带沿边界整条变暗。`StreetSurfaces.Build` 曾给设施带另建 `StreetServiceFlags`，BaseColor 约为相邻旗石的 0.8 倍，光滑度也不同；这是材质分区造成的生硬色带，不能解释成自然光影。
- **修复与防复发**：设施带与步行面共用同一个 `StreetFlags` Material，保持相同世界米数 UV、法线与光滑度。设施带是空间分配，不靠给相邻连续石板整条调暗来表达。自然灯光和单块石材变化保留；以后分段/裁切铺装仍需检查接缝两侧的实际材质与 UV，不能仅看各自颜色配置。
- **最新证据**：`output/demo/roads/unified-paving/` 为当前画面，父目录 ROAD-01 截图/视频保留为色带修正前记录。Windows/D3D11、1600×900 重新构建并运行 `--review --fresh --review-roads`，通过 `ROAD_REVIEW_PASS` 和 `SLICE_REVIEW_PASS`；查看了路面总览、路缘近景及人行道，色带消除，路缘通行与店门往返正常。生成后的 Town/ClockShop 均不再引用旧深色 Material；前一轮已通过两窗回归，此次没有重新做两窗运动专项。自动输入，不是人手盲测；未重建 Mac。

### NPC-03：钟表铺老板使用 Meshy 坐姿动画并配木椅

- **用户要求与来源**：2026-10-08，指定 `Meshy_AI_The_Watchmaker_s_Focu_biped.zip` 作为表店老板，全程坐着并配椅子。包 SHA256 为 `4ffd02fc63a719095c2c9220f39f82878ea883a7faacf6d4a879706832691e06`；原坐姿 GLB、原始纹理及三动作来源摘要在 `assets/source/watchmaker/meshy/`。无授权文件，不能继承旧 Toon Boy 或其他模型许可。
- **可重建管线**：Blender 5.2.0 LTS，`tools/import_meshy_watchmaker.py --archive 包路径 --root 仓库` 保留一套 16,276 顶点、37 骨骼、三张纹理和坐/走/跑三动作；`Watchmaker.blend` 纹理打包且相对路径可移植。坐姿源为 30fps 下 0–286.25 帧，检查 296 个时间样本，另对 10 个含精确首尾的时间点量测实际支持面。`source-packed-results.json` 确认重新打开后每张打包纹理与原 PNG 字节相同；URP RGB=原 B 金属度、A=1−原 G 粗糙度，逐像素误差为 0。原始粗糙度图为 4096px，Unity 导入上限 2048，不删除原始源图。
- **接入与坐姿**：`WatchmakerAsset.Create` 取代 `BuildSlice.ClockShopPrefab` 中旧 `Model("Clockmaker")`；保留已有交谈和 E07。休止成人高度归一化为 1.72m；只播放 `WatchmakerSeated.controller` 的单一 `Seated` 状态，引用 `Sit_Cross_Legged`，Unity 长约 9.533 秒，循环、关闭根运动、无状态跳转，生成 Prefab 前采样坐姿，禁止站姿闪现和 `AmbientMotion` 漂浮。走跑保留为资源，不进入本切片行为。点位仍为旧视觉切片室内 `(0.65,0,2.65)`，这不是第四版全镇迁移。
- **椅子与贴合**：`tools/build_watchmaker_chair.py` 生成可编辑 `assets/source/watchmaker/chair/WatchmakerChair.blend` 和 Unity FBX；简化木架、软坐垫、四腿及横撑，20 部件、480 顶点。椅面高 0.40m，髋骨比支持面高约 0.284m。10 个源网格样本变换后支持面高约 0.399–0.403m，低侧鞋底高约 0.019–0.020m；实际侧/背画面已查看。这是所选源坐姿的采样贴合，不是通用衣料碰撞系统；改动作、比例后重新量，不能把骨骼高度当成鞋底或臀部表面。
- **朝向故障与防复发**：静态椅子 FBX 和动画角色导入轴不同，最初靠背挡在前面；随后直接 `FromToRotation` 处理完全相反方向，产生绕 X 轴翻转，导致椅面变为负高度。最终根据实际 `Back top rail` 与 `ChairSeat` 的位置求水平朝向，使用 `SignedAngle(..., Vector3.up)` 加 `AngleAxis(..., Vector3.up)`，只绕 Y 旋转。构建和运行都要求椅面高度正确、靠背在老板身后；不能恢复猜 yaw 或无旋转轴约束的 180° 对齐。失败截图与日志仅留在忽略的 `builds/review/watchmaker/`，交付证据均来自修正后的运行。
- **室内外统一**：老板和椅子都属于共用 `ClockShop.prefab`，Town 与独立 ClockShop 同步生成。`BuildingLayoutChecks.CompareGeometry` 在原静态网格核对之外，新增 SkinnedMeshRenderer 的网格、材质、变换、控制器与根运动核对；保留移动/缺失/多余窗户的拒绝检查。不能只恢复旧 Prefab 而留下引用新 fileID 的场景。
- **实际验收**：Unity 6000.3.18f1、Windows/D3D11、1600×900。构建通过 `WATCHMAKER_IMPORT_PASS`、`BUILDING_LAYOUT_PASS`、`SLICE_BUILD_PASS`；可见玩家以 `--review --fresh --review-window --review-watchmaker --record-watchmaker` 运行，通过 `WATCHMAKER_REVIEW_PASS`、`STREET_REVIEW_PASS`、`WINDOW_REVIEW_PASS`、`SLICE_REVIEW_PASS`。真实控制器进店、柜台前射线交谈、整个坐姿循环采样 593 帧（约 1.037 圈），角色根移动 0，髋骨上下范围约 3.9mm，头骨运动约 1.09°。检查两窗横移/碰撞、E07、调查簿和正确门口返回。证据在 `output/demo/watchmaker-meshy/`，`seated-loop.mp4` 由 99 张原生截图按 10fps 编码，编码率不是实测游戏 FPS。
- **交付与限制**：当前 Windows 玩家为 `builds/windows/TownDemo.exe`；`--watchmaker-preview` 直接面对老板，使用独立 `watchmaker-preview-save.json`，不改正常进度。画面是原生实机；交互为自动控制，特写为受控机位，不是手动或陌生玩家盲测。未在 Mac 重建本次资源，保留用户此前 Mac 能运行的确认；未重跑完整 MVP 六顺序或磁盘重启专项。Antigravity 在 scratch 提供导入初稿和源分析，CLI 15 分钟打印超时仅返回部分回复；主 agent 复核并修正分数帧采样，完成最终导入、椅子、接入和实机验收。用户于 2026-10-08 授权提交推送此轮，上传状态以远端 Git 记录为准。

### NPC-02：Meshy 送奶少年替换

- **用户要求与来源**：2026-10-08，指定 `Meshy_AI_The_Little_Messenger_biped.zip` 替换 milk boy。原始包 SHA256、六个 GLB 的摘要及动画范围见 `assets/source/milkboy/meshy/import-results.json`。包内没有授权文件，不能沿用旧 Toon Boy 的 CC BY 许可；旧源及署名保留。
- **当前管线**：本机 Blender **5.2.0 LTS**，`tools/import_meshy_milkboy.py` 保留一套 13,212 顶点网格、66 骨骼、三张原始纹理及全部六段动作，去掉导入器生成的 Icosphere 骨骼显示辅助物和其他五套重复人物。六段动作统一到同一骨架；GLB 秒数按导入时 30fps 换算，各动作按自身范围导出，不使用默认 1–250 帧。可编辑 `Messenger.blend` 已重新打开，确认三张纹理打包及六动作；原始短待机 GLB 同目录保存。
- **材质与接入**：`MilkboyAsset.cs` 使用 `Assets/Art/MilkboyMeshy/Messenger.fbx` 及 URP/Lit，依据 headfront 骨骼校正朝向。glTF G 粗糙度/B 金属度转为 URP RGB 金属度/A=1−粗糙度；PNG 通道逐像素核对最大误差 0。纹理导入上限 2048。当前休止网格归一化高度 1.505281m，再保持切片原有 `.88` 缩放、根点位与朝向；不是第四版地图迁移。E04、碰撞与钟表匠保持独立。
- **动画与导入故障**：项目此前未启用 `com.unity.modules.animation`，现已显式加入内置模块。Unity 对象不能以 `??` 判定缺失的 Animator，须用 `if(!animator)` 后添加；异常退出留下的空控制器需补 Base Layer。默认控制器只播放 `Short_Breathe_and_Look_Around`，循环并关闭根运动；其他五段不自动播放，不新增行走行为或以整体漂浮代替呼吸。
- **实际验收**：Windows/D3D11，Unity **6000.3.18f1**，构建通过 `MILKBOY_IMPORT_PASS`（66骨/6动作）、`BUILDING_LAYOUT_PASS`、`SLICE_BUILD_PASS`。可见玩家进程以 `--review --fresh --review-milkboy` 走真实控制器到男孩前，射线触发 E04；短待机循环、头骨运动和固定角色根检查通过，已查看全身、脸、手、侧面与前后两个待机画面。特写是受控相机，不是手动或盲测。
- **受影响回归**：重新生成 Town 及 ClockShop 窗外人物副本；可见运行 `--review --fresh --review-window` 通过棚架/长椅通路、两侧门窗、左右窗横移与碰撞、E07、返回 Town，日志为 `STREET_REVIEW_PASS`、`WINDOW_REVIEW_PASS`、`SLICE_REVIEW_PASS`。精简报告及截图在 `output/demo/milkboy-meshy/`。本轮不改变保存机制，未重跑磁盘重启专项或完整六顺序。
- **黑帧教训**：第一次用隐藏窗口运行，程序交互和动画断言通过，但截图全黑；这不是视觉验收。改用可见窗口重新运行后才保存当前实机证据，不把最初报告或黑帧复制为通过产物。
- **交付与限制**：Windows 本地 `builds/windows/TownDemo.exe`；`--milkboy-preview` 直接面对男孩，使用独立 `milkboy-preview-save.json`，不覆盖正常进度。新模型尚未在 Mac 重建实测；此前用户确认 Mac 能运行仍然有效。未做完整表情/口型、实际走跑行为或陌生玩家公平性验收。Antigravity 导入脚本草稿任务超时未交付，最终脚本及验收由主 agent 完成。用户于 2026-10-08 授权先拉取最新改动、再提交推送此版；上传状态以远端 Git 记录为准。

### NPC-01：送奶少年改用完整儿童网格（历史版本，已被 NPC-02 替换）

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

### MAP-01：第一镇区按剧本与行程重排（第三稿，未实现）

- **用户纠正（2026-10-08）**：第二稿情夫工坊过于靠近案发住宅，且擅加茶餐馆、裁缝铺等无关地点；必须按剧本地点和凶手步行时间设计。第二稿为被否决的历史草案，不能继续作为当前图。
- **依据**：读取原DOCX第三节完整时间线、第五节A–M、第七节NPC；使用 `skills/wrong-date-narrative/SKILL.md`。邻居不记得争吵具体时间、情夫案发日在邻市等用户修订优先。所有新坐标/尺寸/中间到达分钟为规划，不伪装成原稿事实。
- **当前产物**：[具名地图](concepts/town-plan-story-v3.png)、[作者路线图](concepts/town-route-review-v3.png)、[地点与时间预算](town-layout-plan.md)、[数据](town-layout-data.json)。SVG和PNG由 `tools/render_town_plan.py` 同源生成；Markdown保留独立剧情解释与生成数据表。33处建筑含17处剧情相关地点与居民A–P；删除无依据商铺，补棋社/城南店铺/破灯/候车点。教堂仅为原稿店主口供中的背景；父亲住所为会面布置建议。
- **逐栋尺寸标注（2026-10-08）**：用户认可第三稿布局后要求每栋长宽直接标在图上；基础图和作者路线图均在33栋名称下增加「东西长 × 南北宽（m）」，与原坐标数据一致，指主体外墙、不含院落；未调整地点、建筑尺寸或路程。
- **电话亭漏标修正（2026-10-08）**：用户指出电话亭缺失；原图仅把名称并入邮局，实际2×2米亭体过小且无独立标牌。现邮局单独命名，公共电话亭作为独立设施写入布局数据，位置(45,-15)、尺寸2×2米，在基础图和作者图用独立名称/尺寸/引线及定位圈标清；说明表同步拆分。保留原位置与19日路线，数量为33栋建筑加1亭。
- **坐标替代v2**：钟楼(0,0)，车站(360,70)，车站路Z=40；住宅(-40,-90)，邻居(-70,-90)，情夫修车铺(-250,75)，医院(145,-155)。情夫修车铺到住宅公开道路393米；本镇工坊不是邻市凯恩车间。
- **时间预算**：住宅去站581米，近路返家568米，合计1149米；1.2m/s+2分钟停留约17分58秒，可放入17:35—17:55。1.1m/s余约半分钟；1.0m/s不成立。18:15→候车点→18:35旅馆移动约7.2分钟。18:35—20:40缺失行踪不得编成两小时旅馆不在场。19日问路/电话/棋社独立核算，不移植至20日。
- **视线**：邻居东窗对住宅西侧门；送奶停步点(-54,-90)留至钟楼视廊。邻居未连续监看，不用视野盲区强行抹掉门口可见性；二维视线不等于3D钟面可读。禁止把东侧路命名“东街”，以免毁掉不存在东街的线索。
- **验证边界**：已生成并目视核对两张坐标图；33座建筑无互相重叠/路权冲突，所列路线无穿楼，车站路水平，检查记录见 `town-layout-checks.json`；已计算步行时间窗口；旧MVP证据合同静态检查仍通过10证物、6访问顺序。此轮没有Unity构建或实机走路，不宣称游戏内可行性已验证。原DOCX/PDF未改，待补行踪和异地交通细节已写进说明。
- **防复发**：每处剧情地点必须有源章节或明确的新规划标记；先验证时间锚点和沿路长度，再加视觉细节。尺寸更改重算所有受影响路线。基础图与作者路线图分开，路线和推理不能直接加入玩家提示。未来实现还须同时迁移路网/门窗/出生点/边界，内外共用建筑源。

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

### ART-01：钟表铺设计第一轮（已选择 A）

- **用户范围**：2026-10-08，要求先设计到满意，提供四组外观、内部与可交互物件设计图；只有老板人物模型已经锁定，其余仍可设计。未授权直接按概念图替换游戏资产。
- **交付**：`docs/concepts/watch-shop/round-01/` 内 A 胡桃木老铺、B 装饰艺术精品店、C 蓝灰匠人工坊、D 酒红古董店四张 PNG。内置 imagegen 四次生成及 D 一次修图；首轮与修图提示词、物件提案和局限一并保存。老板未重绘，只保留空工作位。
- **检查与修正**：人工查看全部设计板。D 初稿误生二层，修图后保存单层版本并缩小中央展柜。A/C 视角间仍有柜量/摆位差异；概念图不能证明尺寸、共用内外布局、老板坐姿贴合或通行已经通过。
- **后续选择**：用户选定 A / WALNUT HERITAGE，明确授权先拉取最新改动，再做精细建模、实体门无黑屏转场、透明窗内外可见，见 SHOP-01。其他三组保留为设计历史。首轮出图本身没有建模或实机验证。

### SHOP-01：A 胡桃木钟表屋建模与连续空间（2026-10-08）

- **同步与范围**：先保存本地 ART-01 / 概念图，fetch 并快进 main 从 `123aeeb` 到 `cd991ce`，再恢复本地资料，无冲突。保留最新牛奶店、送奶少年分支对话、统一底部 UI 和存档恢复处理。此次不迁移第四版全镇规划；沿用当前切片 7.4 × 8.2m 的店铺位置。用户随后明确授权将此批提交并推送，实际上传版本以 Git 历史和远端为准。
- **资源入口**：`tools/build_walnut_shop.py`、`tools/make_watch_shop_textures.py`、`tools/complete_watch_shop_source.py`；可编辑完整源在 `assets/source/watch-shop/WalnutShop.blend`，视觉 FBX / 材质在 `game/Assets/Art/WalnutShop/`。深绿门面、铜色雨棚、木护墙、橡木地板、胡桃木柜、透明展柜、三种座钟、摆钟、工作台、分格抽屉、镊子、放大镜、灯、镂空齿轮、怀表、维修簿、通知夹板、地毯及小花瓶均为原始程序建模。57 个导出视觉网格、138248 顶点；Blender 5.2.0 LTS 重开检查通过。完整源的 ArchitectureReference 来自实际 Unity layout.json，排除出 FBX，避免双层墙。
- **空间与交互入口**：`BuildSlice.Walnut.cs` 生成共用建筑，`ShopMechanism.cs` 驱动实体前后门、两侧展柜访问门和零件抽屉。Town 内直接行走，保持同一实例、同一场景，无门口淡黑/加载/传送。门扇保留碰撞，扫描玩家胶囊所在的转动空间，拒绝或暂停会撞人的开关。窗和门上玻璃透明、橱窗保留防穿过碰撞。老板仍使用锁定 Meshy 模型、木椅和 Sit_Cross_Legged，E07 原内容保留；老板原有两句交谈接入同一底部对话 UI，未新加剧情。
- **存档**：运行仅加载 Systems + Town。旧 `scene=ClockShop` 的局部位置与朝向读取后映射到 Town 世界坐标；保留已有证物、问话、开场版本和同行者备注。新增 `openFixtures` 保存目标开关状态。review / 迁移 / 展示入口各用独立测试存档，未改正常 slice-save.json。
- **实际问题与修复**：FBX 导入反射 X，最初东西标记不一致，现显式处理并校准两个标记；join 后旋转 AABB 曾高估越界，改为实际顶点范围并应用旋转。初次门射线失败命中玩家自身肩部胶囊，玩家设 Ignore Raycast，仍参与实体碰撞/门扫描。抽屉把手原被柜体和夹板遮挡，调整实体抽屉前脸、拉手与近看站位；近景又发现面板连接缝和右柜挡住放大镜支架，已延长盒体至前脸并缩短右柜。所有程序网格显式 UV，避免木纹/地毯只采一个像素。
- **用户补充修正**：用户截图指出门面主钟和吊牌钟有黄色方底超出圆盘。用真正圆盘、刻度和动态指针的 `HeritageDial` 替代旧方形不透明贴图；检查主钟与吊牌两面，无黄色方角。不能恢复旧 Quad 表盘到这些圆壳上。
- **实机证据**：Unity 6000.3.18f1 / Windows D3D11 可见原生玩家，1920×1080 最终 `--review --fresh --review-walnut-shop` PASS；实际 CharacterController 走通前后门、门关时阻挡、前门内外开关、有人时关闭被拒绝、两窗通透与固定朝向横移视差、坐姿完整循环无根位移、老板射线交谈、E07/调查簿、抽屉及展柜开关通过。新进程店内位置/E07/开门开抽屉复原，以及独立旧室内磁盘存档迁移 PASS。送奶少年完整分支和独立读档 1280×720 PASS；牛奶店两窗与门口往返回归 PASS。结果/截图在 `output/demo/walnut-shop/`，全日志及失败迭代留在忽略的 `builds/review/walnut-shop/`。
- **限制与委派**：程序控制移动、瞄准及共用处理函数，不是物理键盘验收或陌生玩家盲测。本轮没有复验 Mac，也没有重跑旧室内逐帧闪烁或完整案件六顺序测试。尝试 agy 委派 Blender 首稿；返回只有未完成的查找工具说明，没有脚本，未采纳，最终全部由本 agent 制作和检查。

## 后续 agent 如何维护本文

每次修复新问题或改变已确认约定，更新本文，不另建互相冲突的“最终版”记忆文件。新条目写清：用户反馈 → 已复现现象/原因 → 最终实现与代码入口 → 禁止回退的规则 → 实际复验命令/证据 → 未验证部分。保留旧问题 ID；合法改变尺寸或平台时更新当前值，并保留改变原因。

用户明确修改玩法时，先同步 `docs/mvp-demo.md`、代码/数据和相关技能，再同步本文；不能以本文冻结旧决定。程序改动后的验收另写新结果，不在历史 JSON 中把 `false` 改成 `true`。阶段完成后注明实际提交 ID/日期和被测平台，提供具体产物；没有运行就明确写“未运行”。

本文件通过根目录 `AGENTS.md` 成为每轮必读入口，通过 Git 随仓库迁移。它不能代替代码、实际复验或人工审美判断；遇到记录与代码冲突，先核实并修正记录，再继续开发。

### MAP-02：第四版坐标基准与跨镇日期纠正

- **用户确认**：2026-10-08，19日丈夫真实乘车去另一镇问路，20日挪用前一天行程；另一镇暂不制图，继续只做本镇平面。用户要求上传本轮地图并把坐标基准、每次改图重跑合理性写入AGENTS.md。
- **唯一当前基准**：[地图README](maps/town-current/README.md)及同目录plan-data.json。此前稀疏33建筑地图、旧第三版300m站路及旧Unity均不是后续摆放依据。
- **修复**：撤回额外300m站路，车站主站房中心(188,46)、正门(188,40)；删除本镇F03坏灯点与本地问路路线。B14/B15仅是本镇背景；B13、电话亭的历史事件归属不明，暂停历史路线。保留两宅院门、东正门、西厨房门、送奶看钟的平面提案。另一镇只记录无坐标的列车连接。
- **验证**：70宅地、209树、20角色组、17条本镇候选/实际事件路线重新计算；本镇去站返家639.15m、8.88分钟，20分钟中11.12分钟尚未分配，不能写成确凿停留。两签收户间纯步行2.86分钟；全部6种调查顺序5.86–6.18分钟（仅室外2.5m/s）。基础几何无错误，指定邻居窗对返家最后一段44/44采样被建筑遮挡；不是全镇无人目击。
- **复验**：`python3 tools/check_town_map.py`，网页交互使用`tools/check_town_map_ui.cjs`；详见README。不得直接改生成数据而遗漏源文件与其他导出物。
- **限制与后续**：19日车程/班次/返程，电话与棋社所属城镇，20日晚间离馆后行程，异地情夫交通，室内、3D视线、车道净空均未完全验证。材料副本来源渠道为提案。未更新Unity，不把旧构建当新地图验收。


### PLATFORM-02：123aeeb 的本机 Mac 构建与运行核对（2026-10-08）

- 用户要求将最新同步版本构建为 Mac 应用。基于 `123aeeb`，Unity `6000.3.18f1` 执行 `bash tools/BuildDemoMac.sh`，生成 `builds/macos-arm64/TownDemo.app`；`lipo -archs` 确认为 `x86_64 arm64` 通用应用，使用 Metal。旧 `TownDemo.app` 保留为 `TownDemo-before-123aeeb.app`，其他旧包与正常存档未删除。
- 构建通过 `MILKBOY_IMPORT_PASS`、`WATCHMAKER_IMPORT_PASS`、`STREET_SURFACE_BUILD_PASS`、`BUILDING_LAYOUT_PASS`、`SLICE_BUILD_PASS`。日志在 `builds/macos-arm64/build.log`。构建生成的五份场景/Prefab/材质差异已备份至 `builds/macos-123aeeb/generated-backup/` 并恢复构建前内容；原有未提交改动保留。
- 本机可见玩家以 `--review --fresh --review-roads --review-window --review-watchmaker` 在1600×900运行，进程退出码0；道路、街面、钟表匠、窗景与基础切片均PASS。实际控制器行走、对话射线、坐姿循环、E07、调查簿及店门往返通过；证据在 `builds/macos-123aeeb/runtime/`，已查看街景及坐姿截图。
- 单独运行 `--review --fresh --review-milkboy`，退出码0且 `MILKBOY_REVIEW_PASS`，证据在相邻 `milkboy/`，已查看全身截图。上述输入为自动驱动，不是手动或陌生玩家盲测。
- 1280×720运行 `--review --fresh --review-watch`，时间推进、共享时钟与开关检查PASS，原生 `watch-720/watch-later.png` 目视可见两根正确位置的指针；但 `tools/check_watch.py` 的固定暗色阈值检测两针均0/20，`pixel-results.json` 为false。保留失败结果，未修改阈值；Mac像素检测差异尚待单独定位，不得声称该自动检查通过。
- 本次未重跑完整MVP六顺序、磁盘重启或长时间性能验收，未在Intel实机测试。命令行远端复查因HTTPS凭据不可用失败，使用上一轮刚经认证同步的123aeeb；未提交、推送本轮记录。


### SAVE-03：最新Mac版因掉出地图改用独立新存档

2026-10-08 用户报告刚打开的TownDemo卡Bug掉悬崖，明确要求删除该应用、重新制作并以后只使用新版。新增 `BuildSlice.BuildFreshMac`，输出 `builds/macos-current/错日证词-新版.app`，company name为 `Qingchuan Fresh 20261008`；桌面同名符号链接为固定入口。旧 `builds/macos-arm64/TownDemo.app` 已移至废纸篓，旧存档及其他历史包保留。构建通过SLICE_BUILD_PASS，新应用独立review通过真实控制器行走、E07、门口往返，退出码0，证据见 `builds/macos-current/review/` 与 `review.log`。未定位越界根因，不能宣称根本修复；正常启动不带review参数，使用新的正常存档。用户已有未提交修改保留，本轮未提交推送。


### SAVE-04：纠正公司名隔离误判，修复边界与坏坐标（2026-10-08）

- 用户录屏显示所谓新版仍在空旷地图外。检查实际Info.plist发现仍为 `com.Unity-Technologies.com.unity.template.urp-blank`，正常存档实际位于同名Application Support目录，坐标(-10.6898,-7210.034,37.444)，已有E04/E07。SAVE-03所称公司名已隔离是错误判断；SAVE-02的历史路径结论不能套用于此构建。之前review使用独立测试存档，掩盖了正常启动读坏存档的问题。原坏存档保留。
- 修复：`BuildFreshMac`明确设置 `com.qingchuanstudio.wrongdatetest.currentmac` 应用标识；实际Info.plist和运行日志均确认此ID和相应新目录。`DemoApp`在载入后校验有限数值及当前切片范围，异常时仅重置位置，保留证物；LateUpdate恢复掉落者，Save不再持久化坏位置。`BuildSlice.Town`补北侧z=32边界，阻止从地面北端走出。当前小镇有效范围与旧切片墙对应，未来全镇迁移必须修改该范围。
- 验证：Unity6000.3.18f1、Mac/Metal构建通过SLICE_BUILD_PASS和BUILDING_LAYOUT_PASS。将实际旧坏存档复制到新版review-save.json，不用--fresh；运行 `--review --review-position`，日志证实读取新目录、恢复原坏坐标、模拟下落恢复、阻止存入坏坐标、保留证物数量及真实控制器撞北墙，POSITION_REVIEW_PASS且退出码0。证据在 `builds/macos-current/position-review/` 与position-review.log。
- 正常从桌面入口启动（无review参数），SAVE_LOCATION指向新版slice-save.json；运行日志记录E04、Town→ClockShop，正常存档位置(0.896,约0,0.356)位于钟表铺且保留E04。未改用户这一轮的正常进度或强制退出。原录屏首帧核对为空旷天际画面；窗口读取工具对正常窗口给出停滞菜单画面，因此不把该截图当作完整正常玩法证据，正常流程以实际日志和存档为证。
- 此轮修复不是完整全地图/长时间碰撞验收；未在Intel实机验证，未提交推送。桌面入口和应用路径保持不变。

### UI-02：送奶少年固定分支对话与底部电影字幕界面（2026-10-08）

- **用户决定**：三款交互小样中选第二款「电影字幕」，随后认可先认人/慰问、再问送奶或聊日常的短文案，并要求接入游戏。当前只改送奶少年；钟表匠和证物继续使用原查看界面。主角姓氏、正式街名与门牌未定，首版临时开场（已由UI-03取代）为「您是那家的男主人吧？平时是我给您家送奶。」再说「您太太的事……我听说了。我很抱歉。」；不显示占位符，不虚构东街或姓名。
- **实现入口**：文本/分支集中在 `game/Assets/Resources/MilkboyDialogue.json`，`DialogueScript` 验证节点与引用，`DemoApp.Dialogue.cs` 管理底部渐变、左侧原话、右侧完整选项、数字键/鼠标整行点击选项、H本次交谈回看、Esc关闭。`Inspect` 对E04进入对话；生成源 `BuildSlice.Town` 也从同份数据取证言，不能只改单张场景。交谈阻断移动和视角，隐藏怀表/环境插话，退出恢复光标与行走。回看时不绘制底层按钮，避免点击穿透。
- **剧情与保存**：先说亲手交奶、再说18:25及街钟来源，最后一行显示时才获得E04；只打招呼、日常分支或中途退出不提前获证。保留妻子提醒手套和少年说话拘谨的日常细节，不宣告钟快30分钟或案情答案。E04稳定ID、去重、原有证物保留；新字段 `milkboyGreeted` / `dialogueTopics` 兼容缺字段的旧存档，记住重逢/已问；对白可重复。E05问题每次打开按现有证物重算；本切片尚无E05实物，专项测试临时注入后移除，不能称为已制作派送簿。
- **实际验收**：Unity6000.3.18f1、Mac/Metal，`BuildSlice.BuildFreshMac` 构建通过 `BUILDING_LAYOUT_PASS` / `SLICE_BUILD_PASS`；实际Info.plist为 `com.qingchuanstudio.wrongdatetest.currentmac`，可执行文件为x86_64+arm64。最终1280×720与1920×1080分别运行 `--review --fresh --review-dialogue`，均退出0且 `DIALOGUE_REVIEW_PASS`。通过真实控制器走近/射线、问候/日常/证言/后续/告别、退出重问、证言不提前授予与不重复、回看隔离、退出恢复行走、E05条件变化。已目视核对两档原生画面；修正720p回看返回按钮换行及渐变分界。
- **读档与回归**：独立新进程 `--review --verify-dialogue-save`（不加fresh）通过 `DIALOGUE_SAVE_RELAUNCH_PASS`，恢复E04、已问、首次问候状态及后续选项。上述均使用独立 `dialogue-review-save.json`，不改正常进度。最终 `--review --fresh --review-window` 通过 `STREET_REVIEW_PASS` / `WINDOW_REVIEW_PASS` / `SLICE_REVIEW_PASS`，E07、调查簿、钟表铺往返正常。精简图与报告在 `output/demo/dialogue/`，完整构建/运行日志在忽略的 `builds/dialogue-integration/`。
- **范围与限制**：以上是可见玩家中的程序驱动验收，对话使用与UI共用的处理函数，未宣称物理键鼠或陌生玩家盲测。原生窗口工具仍把当前应用误解析到旧模板标识，无法完成物理键鼠专项；未借旧截图声称通过。未做Intel实机、完整MVP六顺序或性能验收。命令行fetch因HTTPS凭据不可用失败，继续基于此前同步的123aeeb并保留本地奶店等其他改动；未提交推送。桌面「错日证词-新版.app」继续指向唯一当前Mac入口。

- **选项操作简化（用户后续要求）**：移除方向键选择和Enter确认选项，也移除默认第一项高亮；只用对应数字键（含小键盘）或鼠标整行点击，底部提示按当前选项数显示。无选项时继续对白仍可点按钮或按Enter/空格，H/Esc保持原行为。重新构建当前Mac入口通过SLICE_BUILD_PASS，720p可见玩家以 `--review --fresh --review-dialogue` 通过DIALOGUE_REVIEW_PASS且退出0，已目视核对新提示与整行排版；源代码核对选择分支没有方向键/Enter入口。日志和原生画面在 `builds/dialogue-number-input/`；属于自动流程检查，未新增物理键鼠验收。远端fetch仍因凭据不可用失败，本轮未提交推送。

- **底栏精简（用户截图修订）**：用户要求不显示「已问过」，问过和未问的选项保持同一文字宽度、色彩和样式；仅保留所有选项通用的鼠标悬停反馈。`DrawDialogue` 不再读取已问状态来排版；内部记录和重复问话逻辑保留。无选项时原话从半宽扩展到整条底栏（只预留小箭头边距），删掉大块「继续」按钮和底部继续文字，右下角显示可点击的小下箭头；箭头以两条线绘制，避免字体缺字，并保留现有Enter/空格推进。告别最后一句也由该箭头结束，Esc仍可直接退出。
- **本次复验**：等待同项目已有奶店窗户构建结束后，再用BuildFreshMac更新同一Mac入口；BUILDING_LAYOUT_PASS、SLICE_BUILD_PASS及Info.plist应用标识核对通过。1280×720与1920×1080分别运行现有 `--review --fresh --review-dialogue`，均DIALOGUE_REVIEW_PASS、退出0，已查看完整宽栏、小箭头和重复问话后的无标记选项。新证据为 `output/demo/dialogue/wide-panel/`，完整日志在 `builds/dialogue-wide-panel/`；为自动处理函数/真实角色射线验证，不声称物理点击箭头或盲测。未改变正文/证物合同或用户存档，不重跑无关的完整MVP验收。远端fetch仍被HTTPS凭据缺失阻塞，未提交推送。

### UI-03：缩小透明底栏、右下半区推进与恢复原定开场（2026-10-08）

- **用户反馈/原因**：截图中的五行选项和高不透明渐变遮住近半屏，箭头点击范围太小；存档已标记 `milkboyGreeted` 导致一上来就说重逢对白，且此前擅自将用户指定的地址/姓氏开场替换成了泛称。用户要求更小更透明、重新设计、点右下半区推进，并恢复原句；本条取代UI-02的旧外观与临时开场约定。
- **当前外观**：`DemoApp.Dialogue.cs` 底栏最大渐变alpha由0.97降至0.58，渐变顶部延伸由160改28逻辑像素；无选项约底部19%，五/六项约24%。去掉长分隔线及大面板，名字配细金色短线、正文加细阴影；有选项时右侧两列按横向顺序编号，无选项仍横向铺开。选项只用数字或鼠标整行点击，所有选项同样式，继续不显示「已问过」。修正720p慰问句的末尾句号单独换行。
- **推进与隔离**：无选项时监听右下象限的鼠标左键抬起（GUI坐标x>=W/2,y>=H/2），箭头只绘制提示。`AdvanceDialogueAt` 排除回看/退出按钮区域、选项状态和回看状态；OnGUI先让具体按钮消费事件，并使用本帧原始选项状态，避免选择选项后同一次点击再跳过一句。Enter/空格等已有无选项推进保持；H/Esc及移动暂停/恢复保持。
- **开场修复**：按用户原文恢复「您是住在［街名］［门牌号］的［姓氏］先生吧？」与「您太太的事……我听说了。我很抱歉。」。具体地址/姓氏未提供，先保留用户给出的占位，不再擅自换句。新增存档字段 `milkboyGreetingRevision`，当前版本2；缺字段或旧版本时下次交谈从两句开场开始，显示慰问后记录版本，之后仍可直接回访。旧bool保留兼容，证物及进度不重置；后续正式更改开场需相应维护版本。
- **实际检查**：Mac/Metal、Unity6000.3.18f1当前入口BuildFreshMac通过BUILDING_LAYOUT_PASS与SLICE_BUILD_PASS。剧情合同静态验证通过。现有对话专项增加旧greeted=true/revision=0状态、保留既有E07、准确开场文字、远离箭头的右下点可推进、左下/右上不推进、底部按钮与选项不误推进等检查；720p/1080p初版全流程均PASS。最后调整左侧文本宽度后，720p重新全流程PASS，1080p独立进程不加fresh读档PASS（保留新版开场版本、E04和回访选项），目视核对最终图。均为真实角色/射线加共用处理函数的自动验收，未声称物理鼠标盲测；没有重测无关完整MVP。
- **交付与边界**：精简证据在 `output/demo/dialogue/compact/`，日志在忽略的 `builds/dialogue-compact/`。复验使用独立dialogue-review-save.json。更新正常窗口前备份已有正常存档到normal-save-before-restart.json，并只终止已核实的旧游戏进程以载入新程序；不删除正常存档。远端fetch仍因HTTPS凭据不可用失败，保留其他本地奶店改动，未提交推送。
- **统一规范（用户后续要求）**：用户要求将当前版本上传，并在根目录 `AGENTS.md` 明确今后全部 NPC 对话 UI 遵循本条当前效果。已添加“全部 NPC 对话 UI 统一规范”，涵盖低遮挡底部渐变、文字与双列选项、数字键／鼠标整行选择、已问同样式、右下象限推进及输入隔离；新增或修改对话时复用现有实现。此要求不代表钟表匠已经迁移，也不改变证物查看界面。本次整理未修改已验收的对话代码，沿用上面的最终720p流程与1080p独立读档证据。


### PLATFORM-03：aed8b28 的 Mac 通用构建与正常入口更新（2026-10-09）

- 用户要求拉取最新仓库、总结 Change 并交付 Mac 版。命令行复查 `origin/main` 与 HEAD 均为 `aed8b2827f45525c3e04e766db1be9ed82005049`；已有干净副本从 `fc72ff2` fast-forward，另克隆最新仓库至同级 `game_test/`。新增5提交涵盖 NPC、街面/怀表、分支对话与连续钟表铺。未提交或推送本轮记录。
- 在本机 Unity 6000.3.18f1 执行 `BuildSlice.BuildFreshMac`，输出 `builds/macos-current/错日证词-新版.app`；日志 `build-aed8b28.log` 中 SLICE_BUILD_PASS、BUILDING_LAYOUT_PASS、角色导入/街面/奶店位置检查通过。实际 Info.plist 标识为 `com.qingchuanstudio.wrongdatetest.currentmac`，lipo 为 x86_64 + arm64，codesign 深度严格校验通过。生成的5份场景/Prefab/材质差异已备份于 `generated-backup-aed8b28/` 并恢复 HEAD；构建前此副本无本地修改。
- 本轮实际可见 Mac/Metal 玩家9项全部退出0且对应PASS：1920×1080 Walnut店铺实体门、阻挡/防夹、同场景前后门行走、两窗视差、坐姿循环、老板/E07/调查簿/抽屉/展柜；独立店内磁盘重启；独立旧ClockShop磁盘存档迁移；1280×720及1920×1080少年完整分支；对话磁盘重启；奶店两窗及进出；少年待机/交谈；720p怀表共享时间检查。证据与各日志在 `builds/macos-current/verification-aed8b28/`，results.json含9项PASS。已目视核对本轮店铺内外/窗景、两档对白和怀表；未运行旧固定暗色阈值的怀表像素检测，不声称该检测通过。
- 已将验证后的应用复制至用户原固定入口 `~/Documents/GitHub/game_test/builds/macos-current/错日证词-新版.app`，桌面原同名符号链接保持；旧应用移至该目录 `backup-before-aed8b28-20261009/`。正常无review启动已目视到标题菜单，normal-launch.log确认新版正常slice-save.json路径；正常存档哈希未改变，测试前的review/dialogue存档备份并恢复。保持菜单供用户继续，不宣称正常手动全流程通过。
- 压缩包 `builds/macos-current/错日证词-Mac-aed8b28.zip` 约77MiB，通过unzip完整性检查，旁边有SHA-256与Changes-aed8b28.txt。真实控制器/射线及共享处理函数自动验证，不是物理键鼠或盲测；未在Intel实机、完整MVP六顺序或长时间性能复验。当前仍为10%切片，全镇第四版尚未迁移。

### TOWER-01：本地可进入钟塔与持续时间系统；Meshy 上传按用户要求暂缓（2026-10-09）

> 历史阶段记录：随后用户澄清不是指定 Meshy 网站。当前制作方式与正式交付状态以 TOWER-02 为准；这里的旧模型门槛和待完成项已被取代。

- **用户范围与最终决定**：采用已认可第三版古欧式塔楼概念，原计划用 Meshy 图生建模外壳、实体开门、灰尘旋梯与原主鞋印、可走近的传统机芯；Q怀表为随身校时工具、塔钟恒快30分钟、无数字时间HUD。Meshy网页登录后显示Free/100积分/CC BY4.0，但浏览器拒绝参考图上传；用户明确答复“暂不上传，保留本地制作结果”。图片没有上传，模型没有开始生成，没有消耗生成积分。**不得继续尝试上传、换API绕过或称程序灰壳为Meshy。只有用户后续明确恢复此步骤才继续。**
- **同步与素材**：命令行fetch并从aed8b28快进到a7c3191（仅性能研究/测量文档），保留原PLATFORM-03本地记忆追加，未提交/推送。`assets/source/clocktower/`保留第三版参考、透明无针Meshy输入、提示/哈希/字体来源。1816年/埃利亚斯·霍夫曼为虚构镇史。齿轮视觉参考CCCBR的Turret Clock Handbook，机芯是项目自产简化模型，不是机械精度复原。
- **共用生成源**：`BuildSlice.ClockTower.cs`生成同一ClockTower.prefab，Town与历史窗外副本共用；沿用切片中心(0,0,10)，3.6m外宽/3.05m内净宽/9.5m高。未迁移第四版全镇坐标。`ClockTowerInteriorLayout.cs`统一32级台阶（0.17125m步高）、5.6m机房、39个脚底路线点；`ClockTowerInterior.cs`生成实体梯面、灰尘、32个鞋印、铸铁框架/铜轮/轴/卷筒/摆锤。`ClockTowerGearMotion`三轮反向转动，仅视觉动画。机房西侧108°为可站平台，其余为挑空，不是可环绕大机房。
- **实体门与防跌**：复用ShopMechanism真实木门与扫掠防夹。最初外侧扶手仅显示几何，已加随梯上升的连续阻挡；随后实机发现最底段挡住南门进梯，已同时移除前两级36°范围外栏显示/碰撞，第三阶起连续防跌。不得扩大测试容差或传送掩盖入口问题。机房挑空边带可见扶手与阻挡。SafePosition只允许塔内部合法高处至5.9m，仍拒绝镇外高点和塔顶。
- **时间**：`GameClock.cs`/`DemoApp.Time.cs`从17:55开始，现实7秒＝游戏10分钟，以double累计、一天取模；四面ClockDial LateUpdate读取同一时间并+30。Q拿表不暂停；开场/暂停/对白/回看/调查簿/查看物件/转场冻结。每15活跃秒自动保存累计时间；旧存档缺字段从17:55开始且保留其他进度，不计算离线时间。钟表铺三面招牌改固定10:10装饰，店内陈列表保持静止，不再提供另一准确校时源。
- **文字与画面修复**：墙装铭牌与无针罗马钟面由`tools/clocktower/build_lettering.py`受控排版；不使用生成图片的文字。最初TextMesh尺寸/朝向错误已换成实际墙面贴图。随后发现原TownSans字体子集缺新增字，`expand_ui_font.py`从原Google Fonts Noto Sans SC OFL来源扩充，保留所有旧字形并覆盖铭牌；许可仍在原OFL.txt。鞋印由硬黑图标改为较接近木色的破边/碎灰痕迹。
- **构建边界**：`BuildSlice.BuildClockTowerPreviewMac`输出`builds/clocktower-review/钟塔-内部验证.app`，独立标识`com.qingchuanstudio.wrongdatetest.towerreview`，只使用自己的review存档；正式桌面入口与正常进度未更新。当前外观为明确的开发灰壳，不能当作获认可概念的最终外壳。常规交付构建在缺少`Assets/Art/ClockTowerMeshy/ClockTowerMeshy.fbx`时明确停止；实际模型仍须掏空、切门洞、校准钟面与内外轮廓、转换贴图并重新验收。
- **已实际验证**：Mac/Metal可见播放器通过`--review-time`和独立进程`--verify-time-save`；通过720p/1080p的`--review-clocktower`（真实CharacterController从街道进入、39个路径点、四面实际指针、门内外操作/扫掠拒绝、向外推挤护栏、机房齿轮转动），独立`--verify-clocktower-save`恢复楼上位置/E06/开门并逐级下楼离开。没有用teleport过门或爬梯。静态楼梯8项、叙事10证物/2判断/6设计顺序、全镇平面70建筑/20角色/17路线检查通过；平面只有证据合同哈希更新。完整记录在`builds/clocktower-review/verification/`。这些是程序驱动实机检查，不是物理键鼠或陌生玩家盲测；没有宣称完整MVP或最终Meshy替换完成。
- **最终本地交付核对**：补字后构建`build-lettering.log`成功，重新运行1080p/720p钟塔专项分别47.1s/46.6s均PASS；已目视确认完整铭牌、查看面板、同屏罗马钟面与Q表、灰尘鞋印及机芯。`output/demo/clocktower-local/`保留精简实机图、5项结果与应用清单；完整初次时间/重启与最终塔楼日志在构建目录。78MiB `钟塔-本地内装与时间-Mac.zip`通过完整性检查，通用二进制和codesign严格校验通过。源码/素材/生成场景与本地验证应用均保留，未提交推送、未替换正式桌面入口。

### TOWER-02：按牛奶店流程完成图片参考重建、正式钟楼替换与 Mac 入口更新（2026-10-09）

- **用户澄清**：要求“类似 Meshy 的图生建模方式”，沿用牛奶店按参考图制作的本地流程，不指定 Meshy 服务。不得再次上传或把本地重建称为 Meshy 推理。读取 MILKSHOP-01 与源脚本核实：参考图→Blender 几何/贴图→可编辑 .blend→FBX→Unity。命令行 fetch 核对 HEAD/origin/main 均 a7c3191，无新提交；保留全部本地改动，未提交推送。
- **正式外观**：`tools/clocktower/build_exterior.py` 生成 `assets/source/clocktower/exterior/ClockTower1931.blend` 和 `game/Assets/Art/ClockTower1931/`。米白均匀砖纹、浅石雕花钟圈/角石/檐口、实体四坡石板瓦顶、关闭拱形百叶及门框；7 材质组、151351 三角面，9.5m 高。百叶是实体关闭通风面板，不是透视假窗或额外房间。四钟面/动态针、木门、铭牌和全部内装由同一个 ClockTower.prefab 承载。
- **导入修复/回归规则**：`ClockTowerAsset.cs` 显式映射 URP 材质/法线，利用 FrontMarker/EastMarker 校准米制与南门。FBX 根含 X 轴转换，必须将 yaw 乘原 rotation，不能直接赋 yaw 清掉它；首次构建因此被尺寸标记检查阻断，修复后 build-2.log 成功。源合并百叶残留旋转已冻结，避免包围盒虚胖，最终FBX重开验证通过。构建检查导入可见几何的9条门洞射线，源模型另检30条；不得仅打碰撞洞而保留视觉实墙。正式门牌前移到 z=-1.845/-1.862 避免外墙遮盖。旧 Meshy 路径/门槛改为本地模型门槛，运行报告改 referenceExterior。
- **本轮实际验证**：`builds/clocktower-final/verification/results.json` 六项 PASS：time19.6s、time-relaunch3.3s、tower-1080 49.2s、tower-relaunch24.0s、tower-720 49.1s、shop54.0s。真实 CharacterController 从街道过门、39点上32级梯、护栏推挤、内外开门及防夹、机房齿轮动画；另一个进程恢复楼上/E06/开门并逐级下楼。四面实体针与 Q 怀表共享计时，+30分钟、7秒/10分钟、暂停、跨日、旧字段迁移及自动存档通过。720/1080完整中文铭牌、砖纹、灰尘鞋印、齿轮、双钟同屏均已目视核对。独立只读叙事公平性复核未发现时钟来源、脚印身份或证物依赖越界。
- **正式入口**：构建为通用 arm64+x86_64，codesign --deep --strict 和85.8MiB ZIP完整性通过。已更新 `~/Documents/GitHub/game_test/builds/macos-current/错日证词-新版.app`，原应用与正常存档备份在 `backup-before-clocktower-20261009-135311/`，桌面原符号链接保持。普通无review启动已通过原入口打开并目视标题菜单，新钟楼在背景可见；normal-launch.log确认SAVE_LOCATION仍是 `com.qingchuanstudio.wrongdatetest.currentmac/slice-save.json`，启动前后正常存档哈希相同。测试使用独立review文件，原review/time/tower/proof文件均恢复。
- **证据与范围**：精简结果及实机截图 `output/demo/clocktower/`；模型渲染 `assets/source/clocktower/exterior/previews/`，两者不混称。程序驱动实机，不是物理键鼠/陌生玩家盲测；未做 Intel 实机、长时间性能或完整MVP六路线运行。仍为10%切片中心(0,0,10)，没有迁移全镇第四版。合同同步后重新生成平面验证hash，不改几何，不以平面PASS替代3D验证。

### TOWER-03：再次纠正图生建模的含义（2026-10-09）

- 用户明确“不要用这个网站，我的意思是学习这个网站图生成建模的方式”。TOWER-02 把其要求解释为接受 Blender 参考图脚本重建，是助手的推断，不能继续视为用户已认可制作方法或美术结果。现有钟楼实际已构建并可玩，但不等于完成了图片条件模型推理。
- 最新边界：研究/学习同类图片→三维形状→纹理的方法，不操作 Meshy 网站、不上传、不调用服务；不得假称知晓或复刻其未公开内部模型。本轮只查阅公开论文、代码与硬件，没有下载模型、安装环境或运行推理，也没有再次覆盖游戏。
- 官方 TripoSR 为图片编码→三平面表示→网格；Hunyuan3D 提供形状与纹理两阶段。Mac 路线需区分官方CUDA默认实现和社区MLX移植。实际主机读取为 Apple M5 Pro、24GiB统一内存、约370GiB可用空间；系统Python3.12当前无torch/transformers/diffusers等。社区小型形状模型可能可行，未在本机验证；完整贴图内存与质量仍待验证，不能借用作者性能数据当作本机结果。
- 后续若执行应先隔离试验图片条件形状生成、保留输入/模型版本/原始网格，再比较外观；不要先覆盖现有已测试的门、旋梯、时间与存档。外观图不提供内部通道、门轴与机房信息，这些需按既有空间合同修整并重验。调研来源与状态见 `assets/source/clocktower/image3d-research.json`。

### TOWER-04：现有钟楼增高至 20 米、三层连续旋梯与旧存档迁移（2026-10-09）

- **用户最新决定**：接受当前游戏中的钟楼作继续修改的基础，要求约 20 米、三层体量。本轮继续已有本地资产；没有使用 Meshy 网站、上传、下载或运行图生模型，不以神经图生推理为本次高度修改的前提。命令行 fetch 成功，HEAD 与 origin/main 同为 a7c3191，没有新增远端提交；保留全部本地改动，未提交推送。
- **共用尺度**：`ClockTowerInteriorLayout` 定义总高 20m、塔身外宽 3.6m／净宽 3.05m、地面 .12m、中层 8.530435m、机房 15m、内墙 18.3m、四钟心 16.72m／直径 2.2m。Blender 生成源读取同份常量并补砖列、角石、檐口与实体关闭百叶，不把人的门或砖纹拉长。7 材质组／230159 三角面，源和 FBX 静态回导、门洞射线及各层标记通过。原 9.5m 资产备份于 `builds/clocktower-20m/previous-art/`。
- **楼梯与中层**：92 段不是 92 个升高阶；实际为下段 52 级（步高 .161739m）、同高 90° 中层的 5 段、上段 35 级（步高 .184845m），共 87 级／87 鞋印／101 脚底路线点。保留首两级的门口扶手缺口；中层栏杆两端开放，旋梯和平台来自同一 Prefab。禁止恢复“每段持续升高并在中间直接叠楼板”，那会遮住中层站立区。共享布局 18 项检查通过，整圈同点最小净高约 2.316m，保守胶囊包络净高约 2.154m，高于玩家 1.78m；不能把同点净高当作整个胶囊净高。
- **旧档迁移**：新增 `towerLayoutRevision=1`。无版本旧档在塔内旧梯高度时映射到相同角度的新梯，旧机房移到新 15m 平台；只改位置和版本，保留证物、门、对白及累计时间。正常塔外位置不变。`ContainsUpperWalkPosition` 上限同步至 15.3m，测试用非法高点移到 21m，仍拒绝塔顶和塔外悬空点，不能为新高度放开所有高处存档。
- **实机结果**：Mac/Metal 12 项 PASS，`builds/clocktower-20m/verification/results.json`：time、time-relaunch、tower-1080、tower-relaunch、tower-720、shop、旧梯第 1／12／20／31 级及旧机房五种迁移、middle-save。真实控制器走满 101 点登至机房，新进程恢复机房并逐级下楼；中层站立/往返、门口/防夹、各段扶手、87 鞋印与齿轮动画通过。四面塔钟持续比 Q 表快 30 分钟，7 秒/10 分钟规则保持。迁移保留 E06、开门、问候及加载时 123.5 秒累计时间。不是所有旧阶次穷举验证。
- **最后构建与画面**：首轮中层验收机位只拍到近墙，因此仅修改 `middleLookPoint` 至 (.55, MiddleFloor+.25, -.65)，最终 `build-review-camera.log` 构建通过并单独复跑 middle-save PASS（4.2s），已目视旋梯/平台画面。未改变几何或控制器，其他 11 项沿用同轮初始构建的有效结果。已查看双分辨率钟面／怀表、全高外观、脚印、机芯等。精简证据在 `output/demo/clocktower-20m/`，历史 9.5m 证据未覆盖。
- **正式交付**：通用 arm64+x86_64、严格签名、89.2MiB ZIP 完整性通过。最终 ZIP SHA256 为 `25f9719b998f3e3c8ed2a2c3ba37c66d70e64dd4f8a641766f8065dd08edfe22`。已更新原 `~/Documents/GitHub/game_test/builds/macos-current/错日证词-新版.app`，桌面原符号链接保持；旧应用与正常档备份至 `backup-before-clocktower-20m-20261009-142040/`。正常打开标题菜单已目视，SAVE_LOCATION 仍为原正常路径，启动前后正常档哈希一致；所有验收专用档恢复。没有修改主 checkout 的源代码。
- **范围**：程序驱动真实播放器／控制器及目视检查，不是物理键鼠或玩家盲测；未做 Intel 实机、长时间性能或完整 MVP 六路线运行。只增高现有塔，切片中心仍为 (0,0,10)，无第四版全镇迁移。合同更新后重新生成平面验证哈希，叙事及平面检查不能替代上述 3D 运行结果。

### TOWER-05：钟楼砖石内装、逐层落灰、同串脚印与上层蛛网浮尘（2026-10-09）

- **用户要求**：根据内部截图补墙地砖石纹理；底层灰尘和脚印轻淡，向上逐渐明显，脚印只更清晰而不增加；顶层墙角、上部扶手增加蛛网，接近机房有少量白色空气尘粒。命令行fetch成功，HEAD/origin/main仍a7c3191，无远端变化；保留所有已有未提交修改，没有提交推送。
- **真实参考**：National Trust历史屋积尘、English Heritage论文、NHM蛛网/蛛丝说明及OPB联合车站塔内实景。`assets/source/clocktower/interior-reference-research.json`保留链接与适用边界：越高越积灰是本塔少清扫/少碰触的艺术设定，不是自然定律；不从印记清晰度宣布身份、日期或调钟行为。外部照片没有复制成游戏贴图，没有上传或调用Meshy。
- **表面生成**：新增 `ClockTowerInteriorSurfaces.cs`，自制512px颜色/法线，约.38×.18m石灰砖、.58×.38m底层石板；内墙贴面比旧内壳前移6mm且切开原门洞，只渲染无碰撞。所有相关表面改米制UV，防止每个三角区重复整张砖图。原木梯/平台保留，8个每2m高度带的灰膜从轻到厚。6盏维护灯适度提亮并加入小暖色灯泡，未增灯数或阴影灯。
- **同串脚印**：仍为原位置、左右和鞋型的87枚，`footprintCount==87`实机断言取代旧>=70门槛。初轮顶部近似深色实心图章，经目视后改上端底色为#8A8271、alpha cutoff .43，保留孔隙与碎灰；最终低/高对比约.00746/.16292，cutoff .600625/.43。第7/47/87段同方位实机近景可见低层近乎淡去、中层渐清、顶部清楚的变化，不新增第二串脚印。
- **蛛网与浮尘**：新增 `ClockTowerAtmosphere.cs`，顶层三墙角与上段第76/84/90段扶手外侧共6处疏密不均细丝网，不横封路径且没有碰撞。只读复核发现初值1.49m墙端距1.519m贴面悬空29mm，已统一 `WallAnchor=InteriorWidth/2-.005`，丝端入墙1mm；扶手端落真实立杆。机房局部粒子12–22s寿命、1.25枚/s、上限32，微小灰白柔点缓慢漂移，水平范围约±1.332m、垂直14.39–17.286m，位于塔内。
- **构建修复**：首构建因项目未启用ParticleSystem模块报类型缺失，依据本机Unity内置包manifest加入 `com.unity.modules.particlesystem=1.0.0`；锁文件仅增此builtin模块，无外部包升级。`build-2.log`与最终 `build-final.log`均BUILDING_LAYOUT_PASS、CLOCKTOWER_ATMOSPHERE_READY、SLICE_BUILD_PASS。
- **实机证据**：首轮1080p/720p完整101点行走、独立机房读档下楼、中层存档四项全PASS；随后仅脚印材质柔化和底层机位补充，最终1080p85.4s、720p84.9s再次完整通过。上下楼/存档代码和碰撞未变，机房重启54.7s与中层3.7s沿用同轮build-2结果。检查真实门口/防夹/护栏、四面针+30分钟/Q表持续走时、87枚印记、上层网无碰撞、实际粒子两次GetParticles匹配位移；最终1080p20→21粒、20粒实际移动，未调用Emit/Simulate制造通过。已目视底层砖石、三段灰尘脚印、角落网、机芯和双钟两档画面。证据 `output/demo/clocktower-interior/`，完整日志 `builds/clocktower-atmosphere/`。
- **交付**：arm64+x86_64、严格签名、90.1MiB ZIP完整性通过，SHA256 `3186958168d7311502bdfccbb3ff3a69591b2e99d0ee9208ed9f276982b6508c`。已更新原桌面入口并正常打开菜单，可见保存位置的新砖墙；旧应用及正常档备份在 `backup-before-clocktower-interior-20261009-144731/`。SAVE_LOCATION仍为原正常路径，启动前后正常档哈希一致，所有review存档恢复。没有修改主checkout源文件。
- **范围**：18项布局静态检查仍通过，最终叙事与地图合同哈希检查另记录。只是现有钟楼表面/气氛与照明调整，无20m布局、钟表规则、证物依赖或第四版全镇迁移。脚本驱动实际玩家与目视验收，不是物理键鼠或盲测；未做Intel实机或长时间性能评估。
