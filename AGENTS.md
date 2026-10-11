# Active game and design

- At the start of every session, read `docs/agent-memory.md` before planning or editing, then read the current MVP and relevant skill. It records accepted decisions, incident causes, fixes, reproduction commands, verification evidence and unfinished work. Preserve its regression rules. After a relevant change, update the affected entry with the actual result and evidence; never turn an untested plan into a completed fact. The human user's newest instruction still takes precedence.
- This repository is the active `game_test` project. Follow the human user's newest instructions before any document or skill.
- Use command-line Git for all repository operations, including fetch, pull, commit and push; do not open GitHub Desktop (user instruction, 2026-10-08). At the start of each new task, fetch `origin/main` and summarize the commits and notable file changes since the local `main` before starting the requested work. If `main` is behind and the working tree is clean, fast-forward it. If local changes exist, preserve them before syncing and reapply them afterward; never discard user work, and report any conflict before overwriting files. Do not commit or push the sync or local work unless the user asks.
- `docs/mvp-demo.md` defines the current first-person, freely explored MVP. The original Chinese DOCX/PDF supplies story background; older third-person or fixed-route wording does not override the current MVP.
- In the complete MVP all three interiors are initially accessible. The current user-requested 10% visual slice implements the town and clock shop; its shop is freely accessible from the beginning. The companion makes optional ambient remarks; its dialogue, occupation and location never gate clues, doors or deductions.
- Concepts in `docs/concepts/` are generated style references. The playable Unity project is in `game/`; actual runtime captures and the walking clip are in `output/demo/`, and the local Windows player is in `builds/windows/` (ignored by Git).
- Skills are versioned in `skills/` and installed with `tools/install_project_skills.py`. Use `wrong-date-narrative` for chronology/evidence, `wrong-date-unity` for scenes and code, `wrong-date-blender` for assets, and `wrong-date-playtest` for runtime verification. Read the corresponding `SKILL.md` before applying it.
- `docs/mvp-evidence.json` is a development contract, not player-facing UI text. Keep private deductions out of the material viewer and preserve stable evidence IDs.
- Do not claim a playable build from concepts, static data checks or compilation. Gameplay acceptance needs runtime evidence and a separate fairness review.
- Every building with a playable interior must use one shared layout and building prefab for its town exterior and independent interior. Keep footprint, storeys, ceiling/roof, doors and windows consistent in count, position, dimensions and orientation. Do not add decorative exterior windows or a storey without corresponding physical interior space. Street views and visible room furnishings must use the same geometry and coordinate mapping. Change the common source, never patch the two scenes independently. `BuildingLayoutChecks` must reject missing, extra or displaced openings and differing geometry before building; also inspect real exterior/interior views and exercise both sides of the doorway.
- Do not include `codex` in names created for branches, commits, pull requests, worktrees, tasks or artifacts.

## 当前时间、移动与环境规则（用户确认，2026-10-10）

以下是当前运行版本的基准，优先于本文及记忆中较早的速度、暂停、店外站位和灯光记录。详细实现见 `docs/mvp-demo.md`、`docs/map-system.md`；实测与修复经过见 `docs/agent-memory.md` 的 TIME-01、MOVE-01、WORLD-04 及相邻天气条目。后续修改这些系统时同步更新本节、MVP 与相关记忆，不能把历史数值重新当成现行要求。

### 统一时间与人物速度

- 唯一时间源是 `GameClock` 保存的累计游戏分钟。怀表、塔钟、昼夜和天气共用它，不各自计时。新开局准确怀表为 **17:55**；塔钟默认快 **30 游戏分钟**，另保存玩家调时偏移，调塔钟不改变怀表或天气。
- **现实 1 秒 = 游戏 12 秒**，即 **现实 50 秒 = 游戏 10 分钟**，完整 24 小时昼夜为 **现实 2 小时（7200 秒）**。实现常量为 `GameMinutesPerRealSecond = 12.0 / 60.0`。这是用户最新要求，取代此前 4 倍／6 小时；旧“7 秒／10 分钟”及 4 倍校准只属于历史版本。读档保留累计分钟，不清零、不按新倍率重算、不补算离线时间；每 15 秒活跃现实时间自动保存。
- 查看材料、对话及回看、调查簿、送奶记录、钟楼调时、怀表和手持地图时，时间、昼夜、天气和雨声继续推进。阅读类模态界面只锁定移动／视角；手持地图仍可走跑。只有显式暂停、开场和场景加载冻结正常游玩时间与天气；隔离验收的定时定天气功能不得进入普通模式。
- `PlayerMotor.WalkSpeed = 2.5f`、`SprintSpeed = 6f`，单位为米／现实秒。按住左或右 **Shift** 持续奔跑，无体力限制；斜向输入归一化，不额外加速。保持真实 CharacterController、碰撞和楼梯约束；接地向下速度为 `-Mathf.Max(2f, speed)`，离地重力 18m/s²，避免全速下楼悬空，不能用放宽穿墙／离阶阈值解决。
- 剧情时间仍为约 **17:35 离家、17:40 车站露面、17:55 返家**，不得为迁就跑速改证言。此前 **4 倍**构建的候选公共道路实测去程 **335.43m／55.91 现实秒／3.728 游戏分钟**，后巷返程 **298.44m／49.75 秒／3.317 分钟**；该版约 17:38:44 到站、17:43:59 返家是历史校准结果，不能用于当前 12 倍。当前 Windows／12 倍、同一候选路线实测（程序驱动真实控制器、含录屏）：去程 **337.36m／57.527 现实秒／11.509 游戏分钟**，返程 **301.15m／50.202 秒／10.043 分钟**；17:35 出发约 **17:46:31** 到站，假设短停 10 现实秒后约 **17:58:36** 返家，两个剧情时间窗口均未满足。最新倍率优先，保留证言时刻与人物速度，重新实测后独立报告剧情窗口可行性，不将未赶上的时间窗口冒充通过。停留是测试假设；测量端点是住宅与车站室外可走门前，不含车站室内耗时；B车站已有室内，住宅室内仍未完成。可达性不证明历史路线发生，地图最短导航路线也不能冒充剧情路线。
- 调整速度、倍率、路网或门口后，重新测同一路线的现实耗时和怀表耗时，并检查全速直行／斜行、持图奔跑、门碰撞、钟楼上下阶梯及读档。旧 4 倍测量与原速实录在 `output/demo/sprint-calibration/`；当前 12 倍验证记录在 `output/demo/time-2h/`。

### 每天重复的天气表与灰雾氛围

`TownWeather` 按准确怀表重复以下 **15 段、24 小时**日程，不另开计时器或随机替换。每段开始的 **5 游戏分钟**平滑过渡（当前倍率为 25 现实秒）；云运动按累计时间连续，午夜不跳回起点。新开局 17:55 已是傍晚阵雨。06:00 日出／18:00 日落为既有美术轨道，不是历史天文测量；当前游玩天气不改材料中的历史天气事实。

| 怀表时间 | 天气 |
| --- | --- |
| 00:00–05:30 | 阴夜 |
| 05:30–06:20 | 清晨阵雨 |
| 06:20–09:10 | 阴天 |
| 09:10–09:40 | 短暂放晴 |
| 09:40–11:10 | 阴天 |
| 11:10–11:50 | 阵雨 |
| 11:50–14:20 | 阴天 |
| 14:20–14:50 | 短暂放晴 |
| 14:50–16:40 | 阴天 |
| 16:40–17:30 | 午后阵雨 |
| 17:30–17:50 | 阴天，短暂停雨 |
| 17:50–18:20 | 傍晚阵雨 |
| 18:20–21:10 | 阴夜 |
| 21:10–21:50 | 夜间阵雨 |
| 21:50–24:00 | 阴夜 |

- 保留厚云、暗云底、灰冷低饱和氛围和较低能见度，白天太阳柔暗，不能恢复刺眼晴日作为常态。当前阴天雾量 `.74`、雨天 `.90`；雨天自然光系数 `.38`、直射光 `.04`、雨量 `1`。
- **白天光影（2026-10-11）**：厚云时同一盏天体光额外保留 `SkyKey = .24×太阳高度系数×云量系数×(1−.4×雨)` 的近中性冷白弱主光与淡阴影（地平线和夜间为 0），日环境光顶亮、墙面中等、地面暖暗；天空着色器在云层后画淡白日轮与光晕，方向与光照一致，雨中更淡。SSAO 强度 .95／半径 .55／直接光 .30；Town 体积分离色调接近中性（阴影 .495,.505,.53／高光 .54,.515,.49）、对比 13、曝光 .28。验收时测平均饱和度与蓝偏，保持与改前同级（阴天 b−r 约 +.04～+.06），不得变成蓝青色调。`--review --fresh --review-day-look` 为白天固定机位。
- **雨湿**：`TownWetness` 只读天气日程，湿度=过去 36 游戏分钟内雨量的衰减最大值（隔离 review 预设天气时只用当前雨量），按材料降低基色并提高光滑度，组件销毁时恢复共享材质；不另开计时器、不存档。列表在 `BuildSlice.TownWetnessDriver`（街面、路缘、院落、占位立面／灰泥／石板瓦、钟楼 7 材质）。
- 雨是世界空间固定 **256 条**较粗长雨线；地面水花为最多 **96 落点、5376 顶点**的复用网格，含短促溅珠和扩散水圈，贴真实地面。屋顶下不生成落雨／地面水花。地面碰撞查询缓存，每帧最多 4 次；保留屋顶的显式标注与 16m 空间索引，不改成逐雨滴物理对象或逐帧遍历全部屋顶射线。

### 连续雨声与开关门声学

- `RainAmbience` 使用原创程序生成的三条无静音接缝立体声循环：**雨幕 24 秒、砸地 29 秒、屋顶闷雨 31 秒**。必须听见落地雨滴层；不得恢复旧六秒片段首尾归零造成的周期性断续，也不能每帧或每次打开界面重新播放。
- 室外／室内约 **0.625 秒**渐变。室外雨幕低通 16000Hz；门全部关严的室内雨幕约 1100Hz、砸地层 750Hz，并增加 950Hz 屋顶层，形成隔着墙屋听外面下雨的感觉。
- **挡雨与隔声分别判断**：在屋顶下保持干燥，但同一房屋任意一扇外门实际打开（`OpenFraction > .001f`）就保持室外雨声；所有外门关严后才切室内闷雨。其他建筑开门不能影响当前房间。共享生成器明确绑定牛奶店双门、钟表铺前后门及钟楼门，不凭目标开关状态或“人在室内”直接消音。
- 阅读／对话不打断雨声；显式暂停使用 Pause／UnPause 保留循环位置。音频验收检查跨接缝连续性、砸地层、同屋各外门、其他房屋门的隔离及暂停恢复；滤波效果检查最终 AudioListener 混音，不能用滤波前声源取样替代。现有雨景与开门听雨实录在 `output/demo/fog-heavy-rain/`、`output/demo/dairy-doors-lighting/`。

### 感光路灯与手动关灯优先

- 自然亮度为 **`Daylight * Weather.naturalLight`**，不包含灯自身，避免灯亮后反过来关灯。室外路灯／已绑定外墙灯及发光表面：**亮度 ≤ .42 全亮，≥ .60 熄灭，中间平滑渐变**；这是归一化美术亮度，不是照度计 lux 值。
- 雨天白昼足够暗时也亮灯（正午雨天 `.38`，放晴 `1`），不能恢复“白天一律关路灯”。灯泡发光与实际照明一致，保留最多 **12 处最近室外点光池**，远处仍能看到灯泡发光；保持单一天体光与 3 处局部阴影灯预算。
- 室内按各自采光与用途调光，深处维修／钟楼必需照明保留。**玩家手动关闭优先**，牛奶店阅读灯关掉后，昼夜、天气变化和重新读档均不得自动打开。
- **钟楼地标夜景（2026-10-11）**：四面背光乳白钟盘＋每面一盏钟面溢光、广场四根既有灯柱上的 4 只投光器、塔门两盏壁灯，全部无阴影、走同一感光曲线。`DayNightLamp.sharesStreetPoolBudget=false` 的钟楼投光与钟面溢光不占 12 处路灯光池、远处保持亮；塔门壁灯仍计入光池。投光与溢光只作用于 Light Layer 1（`TowerFacadeLayer`，钟楼外壳 FBX），壁灯只作用于 Light Layer 2（`TowerLanternLayer`，外壳、木门、铭牌、门前路面块）；不得改回 Default 层，否则无阴影灯会穿墙照亮塔内旋梯。生成源 `BuildSlice.TowerNight.cs`；验收 `--review --fresh --review-tower-night` 固定机位，并跑 `--review-clocktower --night-illumination` 与独立读档。

### 牛奶店、纸地图与取放音效

- 送奶少年现在站在**牛奶店内靠柜台**，店局部 `(2.35, .18, .85)`，世界约 `(-146.209, .122, 6.445)`，面向局部 -Z 顾客过道；保留 Meshy 原模型、服装、`.88` 比例、待机、对话及 E04，账本和左右窗通道可达。这里是当前探索站位，不修改历史送奶路线；旧“街外／人行道”描述只供追溯。
- 牛奶店原双门可分别按 E 开关，有实体碰撞、扫掠防夹与保存状态；稳定 ID 为 `milk-left-door`／`milk-right-door`，首次左开右关、向外 85°。默认开启只初始化一次，玩家关门后读档不强制重开；防夹查询保留物理同步及父级比例换算。内外与窗景来自同一 `MilkShopAsset.Create`，不分别补丁场景。
- **M 掏出／收起纸质地图，暂时没有手**；低头看图、抬头继续走跑。按住 Alt 释放光标后，纸面右键标记并显示可行近路，再右键点同一标记取消；松开 Alt 恢复观察。重要建筑保留各自较大的独特图标与当前已有地点配置；导航基于真实可走区域，不画穿楼直线。
- 地图打开时不显示底部“低头看地图”等帮助小字，也不恢复底部操作面板；纸上图例和目的地距离保留。阅读暂时藏图不算真正收起。
- Q 怀表取出／收起分别有金属与衣料短声，M 地图展开／折叠分别有纸声；Esc 收图及道具互相收起时响应相应动作，重复同一状态不重播，阅读临时隐藏不反复播放。四条原创资源由 `tools/build_prop_sounds.py` 生成在 `game/Assets/Resources/PropSounds/`。

### 验证与交付边界

- 当前 TIME-01 两小时昼夜版在 Windows／Unity6000.3.18f1／D3D11／RTX5060Laptop完成同一程序集11项计时、完整路线、双分辨率昼夜／天气及独立读档检查；另用旧4倍播放器实际生成存档验证新倍率原值载入。精简证据在 `output/demo/time-2h/`，正常存档原值保留；剧情窗口实测未满足，不能写成全部剧情可行性通过。完整一天的7200秒是时钟输入检查，不是连续运行两小时。下面MOVE-01／WORLD-04为历史Mac验收。
- 最新 MOVE-01 在 Unity 6000.3.18f1、Apple M5 Pro／macOS Metal 的同一交付程序集完成 14 项运行检查；前一轮 WORLD-04 完成门／声学／灯控等 22 项。不得把不同版本的通过记录混成全部在最新版重跑。精简测量与录像在 `output/demo/`，完整日志、程序集摘要和正常存档前后校验在忽略的 `builds/macos-current/verification-*/`；现有记录不是物理键鼠、陌生玩家盲测、Intel 实机或低配帧率验收。
- 验收用独立 review 存档，不能重置或覆盖用户正常进度。构建包、原始逐帧录像、缓存和临时日志继续不入 Git。Mac 沿用下文唯一新版入口及原桌面链接；仅更新规则文档时不必重建已验证应用。

### 旅馆用车（用户确认，2026-10-11）

- 用户确认示意图后选定：**旅馆用车**，停在 B08 旅馆前庭车马院（约 169.5, 2.0，车头朝北）；**自动三档、真实启动**（点火开关＋起动钮，冷车拉阻风门，也可手摇）；**暗栗红车身配黑翼子板**。原创 1928–31 风格四门轿车，不复制厂牌、车型或标志。设计与边界见 `docs/concepts/car/round-01/design-notes.md`。
- 叙事边界：车归旅馆，不是主角或情夫的车，不进入证据合同、证词或推理；现在能开车不证明任何人 1 月 20 日开过车，不改变剧情时间窗口结论。不要把车移到修车铺或主角家，也不要借车补写离镇路线。
- 唯一来源：`tools/car/build_hotel_car.py` 生成 `assets/source/car/HotelCar.blend/.fbx` 与布局合同 `hotel-car-layout.json`（复制为 `game/Assets/Resources/HotelCarLayout.json`）；`BuildSlice.Car.cs` 按合同装配 `HotelCar.prefab`。改枢轴、碰撞、座位、下车点或交互点时改生成器，不手改预制体。`tools/BuildCar.ps1` 重新生成。
- 只用盒碰撞；交互目标为触发器盒，不能互相重叠（EditMode `HotelCarPrefabTests` 检查），驾驶座视线须直接命中全部车内控件。下车点在脚踏板外、离开门扇摆动范围 0.3m 以上。车不在 Town geometry 内，不参与导航烘焙。
- 驾驶中 Q／M／T 需停车拉手刹；车内阅读、对话与暂停冻结车辆物理。门窗与前风挡全关才算闷雨，任一打开即室外雨声。大灯与车顶灯从 12 处室外光池中占名额。读档熄火并拉手刹。验收用 `tools/ReviewCar.ps1` 的独立 `car-review-save.json`，不碰正常存档。

## 送奶男孩造型与制作约定

2026-10-08 用户指定 `Meshy_AI_The_Little_Messenger_biped.zip` 替换送奶少年。当前资源以这个压缩包中的模型、服装、面部、贴图及骨骼为准，不能再为了下表旧造型而改回 Toon Boy 或强行重做金发、蓝眼、握肩带。当时仅替换外观及待机动画，保留 E04 证言与碰撞；2026-10-10 当前站位已按上节移到牛奶店内柜台旁。

- 当前管线：`tools/import_meshy_milkboy.py` → `assets/source/milkboy/meshy/Messenger.blend` 与 `game/Assets/Art/MilkboyMeshy/` → `MilkboyAsset.Create`。完整保留六段动画，默认循环 `Short_Breathe_and_Look_Around`，关闭根运动；不以角色整体上下漂浮冒充呼吸。
- 保留原始 GLB、三张原始纹理及来源摘要；压缩包没有授权文件，不得套用旧 Toon Boy 的 CC BY 许可或虚构 Meshy 授权。旧模型和原署名作为历史源保留。
- 每次替换后检查真实 Unity 全身、脸、侧面、脚底及动画，并走近触发 E04；同时重新生成 Town 与 ClockShop 的窗外副本。当前结果和平台范围见 [NPC-02](docs/agent-memory.md#npc-02meshy-送奶少年替换)。

以下为 `f57cec1` 的历史造型记录，仅用于旧版本追溯；最新用户资源选择优先。接入成功不等于用户已认可美术效果。

| 部位 | 造型要求 |
| --- | --- |
| 人物与比例 | 小镇的送奶男孩，保留明确的儿童头身比例和稚气；当前采用较细致的卡通儿童模型。提高该角色精度不要求同时重做环境或钟表匠。 |
| 头发与面部 | 金发、蓝色眼睛，保留可辨认的发束、眼窝、眼睑、虹膜、鼻口、脸颊与下颌。表情拘谨、略带不安。使用完整脸部网格，不能退回球形头上贴眼睛的做法。 |
| 帽子 | 老式送奶员帽：浅色圆润软帽冠、深色帽带、弧形帽檐、小金属帽徽。注意帽檐厚度、帽冠与头发的衔接，不使用方块帽冠。 |
| 制服 | 奶白／米白色棉布短外套，翻领、深色领结、胸袋、袖口和小金属纽扣；配同色长裤、深色系带皮鞋。布料要有细织纹、缝线、厚度及肩肘处自然褶皱，不能只给方块身体涂白色。 |
| 挎包 | 棕色小皮挎包置于腰胯侧，斜跨肩带绕过肩部并与包体连接；保留翻盖、扣件和缝线，表现皮革与棉布的质感差异。 |
| 动作与情绪 | 双手放在胸前，一高一低攥住同一根斜跨肩带；手指弯曲贴住带子，手腕接入袖口，手肘收拢、肩部略缩，表现被询问或问责时的紧张拘谨。当前是握带静态姿态，尚未制作完整表情、口型或走跑动画。 |

- 参考图用途：老式送奶员照片用于服装、帽子和配饰；穿汉服的小孩图片只用于说明脸部、身体和衣料的建模精度，不是换成汉服的要求。
- 使用完整儿童身体、手部网格及骨骼权重继续修改；不能恢复旧版方块袖子、球体手指、悬浮袖口等被用户否决的做法。指节与拇指应自然弯曲，不能靠穿入肩带或把手压成一团来假装握住。
- 在生成源修改后再导出、更新 Unity；不要只改生成后的场景。角色必须面向广场可接近的一侧，双脚落在人行道上，不用整体上下漂浮代替呼吸动作。保留送奶少年的 E04 交互，避免影响钟表匠。
- 修改后检查正面、侧面和三分之二角度，以及脸／帽／手的近景：眼睛在 Unity 中可见，帽子不穿头发，手指与肩带接触合理，袖口和手腕连接，肩肘无明显穿插、悬浮或断裂；还须在小镇实际灯光下走近并触发对话。建模预览、编译成功或顶点数量不能代替这项检查。
- 可编辑模型：`assets/source/milkboy/Milkboy.blend`；生成脚本：`tools/build_milkboy.py`；Unity 模型／材质：`game/Assets/Art/Milkboy/`；接入入口：`game/Assets/Editor/MilkboyAsset.cs` 与 `BuildSlice.Milkboy`。当前工具版本和故障记录见 [Agent 记忆的 NPC-01](docs/agent-memory.md)。
- 基础模型为 silentsheri 的 `Toon Boy`，按 CC BY 4.0 使用。保留 `assets/source/milkboy/LICENSE.txt` 和 `game/Assets/StreamingAssets/ThirdPartyNotices.txt` 中的署名、来源及修改说明。
- 视觉核对资料：`output/demo/milkboy/model-*.png` 是 Blender 预览，`unity-*.png` 是实际游戏画面；特写使用受控机位，不能称为玩家盲测。新增修改要重新核对对应画面，不把旧截图当作新版本通过的证据。

## 钟表铺老板坐姿约定（2026-10-08）

- 用户后续选定 A / WALNUT HERITAGE 概念，要求精细外观和内部、可开关实体门、透明玻璃、直接行走进出。钟表铺改为 Town 中同一座可进入建筑，废止该店黑屏转场与独立室内加载；其他未制作室内不在此次改动范围。老板仍锁定。旧 ClockShop 场景仅保留布局一致性检查/历史兼容，不作为新运行入口；旧室内存档加载时映射到 Town 世界坐标，保留证物与对话进度。

- 用户指定 `Meshy_AI_The_Watchmaker_s_Focu_biped.zip` 替换店内老板，全程坐着。保留包中的人物、服装、面部、贴图和骨骼；控制器只循环 `Sit_Cross_Legged`，不切换走跑，不用 `AmbientMotion` 整体漂浮冒充动画。三段原动作保存在可编辑源与 FBX 中。
- 生成源为 `tools/import_meshy_watchmaker.py` 与 `tools/build_watchmaker_chair.py`；源资产位于 `assets/source/watchmaker/`，Unity 接入为 `WatchmakerAsset.Create`。椅子和老板一起放进共用 `ClockShop.prefab`；最新钟表铺在 Town 同一建筑内直接进出，透窗看到同一位老板。不要分别修改两张场景。
- 椅子为简化木架软坐垫款，按实际坐姿承重点对齐；当前成人休止高度 1.72m，椅面 0.40m，髋骨到支撑面的距离约 0.284m。改比例或动作后必须重新量承重点、鞋底、椅背、柜台间距和碰撞，不直接沿用数值。
- 保留老板的既有交谈与 E07 通知，替换造型不改变调查进程。验收要看实际坐姿循环、侧面/背面落座、真实交谈射线、E07 和门口往返；来源与实际结果见记忆 NPC-03。压缩包无授权文件，不能套用旧人物许可。

## 当前地图坐标与改图必验（2026-10-08）

- 后续所有场景、建筑、道路、人行道、院落、门窗、树木、角色点位和剧情动线制作，统一依据 [`docs/maps/town-current/plan-data.json`](docs/maps/town-current/plan-data.json)。先读该目录 README 与 `actor-audit.json`。钟楼中心为 (0,0)，单位米，东 +X、北 +Z；建筑轮廓、宅地与门口是不同数据，不能互相代替。当前第四版取代先前各版（包括其他目录同名“第三版”）、旧截图和Unity旧点位，后者只作历史参考。用户最新修改优先，需同步更新基准。
- 坐标数据由同目录 `build_plan.py` 与 `story_layout.py` 生成。改源文件后运行仓库根目录 `python3 tools/check_town_map.py --write`，再运行 `python3 tools/check_town_map.py`。必须同步提交JSON、SVG、HTML、CSV、Tiled和检查结果，不许只移动截图、场景或单份导出数据；检查失败必须修复，不能降低阈值来凑剧情。
- 每次地图修改都要重新跑全部角色的路线核对：公共路网与对应宅地出入口的连通、建筑/宅地重叠、门位、穿楼、道路宽度构成、距离和时长、送奶签收窗口、邻居观察窗与返家入口，以及全部6种自由调查访问顺序。打开 `index.html` 和 `actors.html` 检查改动区域，逐角色/路线播放，并检查17:53—17:58同屏。可用 `tools/check_town_map_ui.cjs` 复验网页交互（需Playwright），检查报告须标明局限，不能沿用旧截图或旧通过结果。
- 必须区分真实事件、人物陈述、未实施计划、设计提案与未知。已确认：19日真实乘列车去另一镇问路；20日挪用19日的跨镇行程。另一镇不制图，禁止在本镇补“问路点/坏灯点”或把本镇B14/B15当外镇证人场景；跨镇距离、时刻表、返程未知时保持未知。电话和棋社所属城镇待确认，不播放成既定历史路线。20日未验票不能否定19日乘车。
- 核对必须包含每个有姓名或叙事作用的角色；没有路线依据的角色记录“未知/定点/镇外”，不得为凑完整性编造通勤、不在场证明或连续目击。地图可达性不证明路线确实发生。更新证据合同后另跑 `python3 skills/wrong-date-narrative/scripts/validate_story.py docs/mvp-evidence.json`。
- 2026-10-09 已迁入Unity全镇外景，入口见 `tools/build_full_town.py` 和 `BuildSlice.FullMap.cs`，存档 `worldRevision=3`。3处成品保留可进入空间，其余建筑和树木采用占位；改平面源后须重算、编译TownPlan并生成Unity。已验六种室外访问顺序与指定代理视线，不等于完整剧情室内、全部视点或历史人物路线通过。以后按受影响范围复验内外坐标、碰撞、门窗、钟面、3D视线与实机行走；车辆占道/转弯未实施。记录未解决的剧情时间空白，不宣称全部角色已完全合理。

## 当前 Mac 启动入口（2026-10-08）

- 用户反馈旧 `builds/macos-arm64/TownDemo.app` 存档掉出地图，要求移除该应用并以后统一使用新版。当前唯一推荐入口为 `builds/macos-current/错日证词-新版.app`，桌面同名入口链接到它。使用 `BuildSlice.BuildFreshMac` 重建，company name 固定为 `Qingchuan Fresh 20261008`，应用标识固定为 `com.qingchuanstudio.wrongdatetest.currentmac`；已通过运行日志确认新存档位于 `~/Library/Application Support/com.qingchuanstudio.wrongdatetest.currentmac/`，不能仅凭公司名宣称隔离；以后打开或更新Mac版默认使用此入口，不再打开旧TownDemo或其他历史副本。
- 已修复旧模板应用标识导致重复读取坏存档，以及旧切片缺少北侧边界的问题。保留启动位置校验、运行时掉落恢复和保存位置校验；保留证物进度。构建后必须检查实际 Info.plist 与 SAVE_LOCATION 日志，不以review新开局证明正常存档正确。改当前切片范围时同步更新位置有效范围；这些范围不是第四版完整地图。

## 全部 NPC 对话 UI 统一规范（用户确认 2026-10-08）

- 后续所有新建或修改的 NPC 对话，都按照当前送奶少年的界面与操作制作，复用 `game/Assets/Scripts/DemoApp.Dialogue.cs` 的布局、样式和输入处理。当前基准是 [UI-03](docs/agent-memory.md#ui-03缩小透明底栏右下半区推进与恢复原定开场) 与 `output/demo/dialogue/compact/` 的实际游戏截图；此前的大底栏、单列长表格及原型只作历史参考。当前已接入送奶少年，其他 NPC 尚未统一迁移，不能把规范写成全部已完成；用户后续修改优先。
- 对话靠屏幕底部，背景为低遮挡的透明暗渐变，当前最大 alpha 为 0.58；正常正文约占底部 19%，五至六个选项约占 24%。保留细金色说话人标记、米白正文和轻微文字阴影，不恢复遮住半屏的厚重面板、表格式长分隔线。以当前字体和留白为基准适配内容，在 1280×720 和 1920×1080 检查中文可读性、换行与遮挡。
- 无选项时正文横向铺开；有选项时左侧显示原话，右侧两列完整选项，按从左到右、从上到下顺序编号。选项仅用对应数字键（1、2、3……，含小键盘）或鼠标整行点击，不增加方向键选择、Enter 确认或默认选中第一项。已问与未问的文字、宽度、色彩和样式完全一致，不显示“已问过”；允许统一的悬停反馈并保留内部重复问话记录。
- 不显示大块“继续”按钮或“继续”文字。右下角小箭头仅作提示；无选项时，点击屏幕右下四分之一区域即可推进，无须点准箭头，保留 Enter／空格／E 推进。回看、退出按钮优先响应；有选项或打开回看时禁用大区域推进，选择选项的同一次点击不得再跳过下一句。
- 保留 H 对话回看、Esc 返回／结束，交谈期间暂停移动和视角，退出恢复光标与行走；回看不得穿透到底层按钮。文本与分支放在独立数据中，界面只负责显示与交互，不能提前授予证物或自动揭示谜底。改 UI 不重置正常存档、已获证物或对话进度。
- 送奶少年新版开场保持“您是住在［街名］［门牌号］的［姓氏］先生吧？”及“您太太的事……我听说了。我很抱歉。”；正式街名、门牌与姓氏未确认前保留原占位，不自行改成泛称。保留旧存档开场版本迁移及证物去重。修改后按受影响路径复验推进、选项、回看、退出和读档；自动驱动结果不能称为物理键鼠或玩家盲测。
