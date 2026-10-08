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

## 送奶男孩造型与制作约定

2026-10-08 用户最新指定 `Meshy_AI_The_Little_Messenger_biped.zip` 替换送奶少年。当前资源以这个压缩包中的模型、服装、面部、贴图及骨骼为准，不能再为了下表旧造型而改回 Toon Boy 或强行重做金发、蓝眼、握肩带。只替换外观及待机动画，保留 E04 证言、碰撞和现有切片点位；这不是第四版地图迁移。

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

## 当前地图坐标与改图必验（2026-10-08）

- 后续所有场景、建筑、道路、人行道、院落、门窗、树木、角色点位和剧情动线制作，统一依据 [`docs/maps/town-current/plan-data.json`](docs/maps/town-current/plan-data.json)。先读该目录 README 与 `actor-audit.json`。钟楼中心为 (0,0)，单位米，东 +X、北 +Z；建筑轮廓、宅地与门口是不同数据，不能互相代替。当前第四版取代先前各版（包括其他目录同名“第三版”）、旧截图和Unity旧点位，后者只作历史参考。用户最新修改优先，需同步更新基准。
- 坐标数据由同目录 `build_plan.py` 与 `story_layout.py` 生成。改源文件后运行仓库根目录 `python3 tools/check_town_map.py --write`，再运行 `python3 tools/check_town_map.py`。必须同步提交JSON、SVG、HTML、CSV、Tiled和检查结果，不许只移动截图、场景或单份导出数据；检查失败必须修复，不能降低阈值来凑剧情。
- 每次地图修改都要重新跑全部角色的路线核对：公共路网与对应宅地出入口的连通、建筑/宅地重叠、门位、穿楼、道路宽度构成、距离和时长、送奶签收窗口、邻居观察窗与返家入口，以及全部6种自由调查访问顺序。打开 `index.html` 和 `actors.html` 检查改动区域，逐角色/路线播放，并检查17:53—17:58同屏。可用 `tools/check_town_map_ui.cjs` 复验网页交互（需Playwright），检查报告须标明局限，不能沿用旧截图或旧通过结果。
- 必须区分真实事件、人物陈述、未实施计划、设计提案与未知。已确认：19日真实乘列车去另一镇问路；20日挪用19日的跨镇行程。另一镇不制图，禁止在本镇补“问路点/坏灯点”或把本镇B14/B15当外镇证人场景；跨镇距离、时刻表、返程未知时保持未知。电话和棋社所属城镇待确认，不播放成既定历史路线。20日未验票不能否定19日乘车。
- 核对必须包含每个有姓名或叙事作用的角色；没有路线依据的角色记录“未知/定点/镇外”，不得为凑完整性编造通勤、不在场证明或连续目击。地图可达性不证明路线确实发生。更新证据合同后另跑 `python3 skills/wrong-date-narrative/scripts/validate_story.py docs/mvp-evidence.json`。
- 当前仅为平面设计基准，未迁移Unity。实际接入后还须测试内外共用坐标、碰撞、门窗、钟面可读性、完整3D视线、车辆占道/转弯与实机行走；平面中心线、静态图和数据PASS不能代替这些验收。记录未解决的剧情时间空白，不宣称全部角色已完全合理。
