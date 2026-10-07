# Active game and design

- At the start of every session, read `docs/agent-memory.md` before planning or editing, then read the current MVP and relevant skill. It records accepted decisions, incident causes, fixes, reproduction commands, verification evidence and unfinished work. Preserve its regression rules. After a relevant change, update the affected entry with the actual result and evidence; never turn an untested plan into a completed fact. The human user's newest instruction still takes precedence.
- This repository is the active `game_test` project. Follow the human user's newest instructions before any document or skill.
- At the start of each new task in this repository, fetch `origin/main` through authenticated GitHub Desktop and summarize the commits and notable file changes since the local `main` before starting the requested work. If `main` is behind and the working tree is clean, fast-forward it. If local changes exist, preserve them before syncing and reapply them afterward; never discard user work, and report any conflict before overwriting files. Do not commit or push the sync or local work unless the user asks.
- `docs/mvp-demo.md` defines the current first-person, freely explored MVP. The original Chinese DOCX/PDF supplies story background; older third-person or fixed-route wording does not override the current MVP.
- In the complete MVP all three interiors are initially accessible. The current user-requested 10% visual slice implements the town and clock shop; its shop is freely accessible from the beginning. The companion makes optional ambient remarks; its dialogue, occupation and location never gate clues, doors or deductions.
- Concepts in `docs/concepts/` are generated style references. The playable Unity project is in `game/`; actual runtime captures and the walking clip are in `output/demo/`, and the local Windows player is in `builds/windows/` (ignored by Git).
- Skills are versioned in `skills/` and installed with `tools/install_project_skills.py`. Use `wrong-date-narrative` for chronology/evidence, `wrong-date-unity` for scenes and code, `wrong-date-blender` for assets, and `wrong-date-playtest` for runtime verification. Read the corresponding `SKILL.md` before applying it.
- `docs/mvp-evidence.json` is a development contract, not player-facing UI text. Keep private deductions out of the material viewer and preserve stable evidence IDs.
- Do not claim a playable build from concepts, static data checks or compilation. Gameplay acceptance needs runtime evidence and a separate fairness review.
- Every building with a playable interior must use one shared layout and building prefab for its town exterior and independent interior. Keep footprint, storeys, ceiling/roof, doors and windows consistent in count, position, dimensions and orientation. Do not add decorative exterior windows or a storey without corresponding physical interior space. Street views and visible room furnishings must use the same geometry and coordinate mapping. Change the common source, never patch the two scenes independently. `BuildingLayoutChecks` must reject missing, extra or displaced openings and differing geometry before building; also inspect real exterior/interior views and exercise both sides of the doorway.
- Do not include `codex` in names created for branches, commits, pull requests, worktrees, tasks or artifacts.

## 送奶男孩造型与制作约定

2026-10-08 根据用户要求整理；当前重制实现为提交 `f57cec1`。以下是后续制作需要延续的造型方向，用户的新要求优先。实现已接入游戏，不等于用户已认可全部美术细节。

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
