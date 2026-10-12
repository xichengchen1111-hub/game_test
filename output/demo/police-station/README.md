# 警署（B10 · D 内院回廊警署）第一版

2026-10-12 按用户已选 D 方案（`docs/concepts/police-station/round-01/`）制作的第一版可进入警署。街景外观与室内是**同一个预制件**，没有第二套室内场景；门窗洞口、楼层、房间、家具、灯和交互点都来自同一份布局合同。

## 有什么

- **形体**：23×12 m 两层前楼（屋脊 8.8 m）＋西、东两翼各 5.5×12 m 一层，12 m 露天内院和两侧 1.5 m 回廊。南正门在 (12.5, 27.7)，横厅后门通内院 (8.36, 39.7)，内院向北经后院到北后巷。与地图 `plan-data.json` B10 的三块轮廓、两处门和 31 个外窗逐一核对。
- **首层**：接待门厅（报告小桌、查阅须知、长凳）、案卷阅览室（公开材料桌、对照桌、分类柜三抽屉、书架、两盏绿罩阅读灯）、问询室、候问侧席、贯通横厅、值班接待窗口（值班警员）、誊录室、盥洗间、双跑楼梯。
- **两翼**：资料分馆、装订档案库；巡警值班室、物证室（封存物证柜、获准照片板）、旧羁押室（推开的铁栅门、空床、搪瓷盆、高窗）。
- **二层**：负责人办公室（查阅许可存根）、调查办公／誊录、值班休息室、档案储存、盥洗／储藏。
- **功能**：14 扇实体门（正门双扇、横厅后门、两翼五门、六处室内门）可 E 开关、有碰撞、防夹和存档；分类柜三抽屉可拉开读索引卡；5 盏阅读灯可手动关，手动关闭存档且优先于昼夜／天气；12 处可坐（含内院两张长椅）；25 份纸面材料（E01 两份副本、E08 一份副本按稳定 ID 去重）；值班警员六个话题，可直接递交阶段报告；阶段报告（门口小桌、警员、调查簿按钮均可打开）；雨：前楼和每间翼房各自按门判断闷雨，回廊有顶遮雨但不闷声，内院露天。
- **灯**：33 盏室内灯只照“警署室内”渲染层（Light Layer 4），不会穿墙照亮街道；6 盏外墙／回廊灯走感光与 12 处光池。无新增阴影灯。

## 来源与重新生成

| 步骤 | 文件 |
| --- | --- |
| 布局合同 | `tools/police/police_layout.py` → `assets/source/police/police-layout.json`、`game/Assets/Resources/PoliceLayout.json` |
| 文本 | `tools/police/write_police_texts.py` → `PoliceDocuments.json`、`PoliceReport.json`、`PoliceOfficerDialogue.json` |
| 纹理／音效 | `tools/police/make_police_assets.py`（纯标准库，本轮在 Mac mini 上 8 核运行）→ `game/Assets/Art/Police/Textures`、`Sounds` |
| 模型 | `tools/police/build_police_station.py`（Blender）→ `assets/source/police/PoliceStation.blend/.fbx` |
| 值班警员 | `tools/police/make_officer_texture.py`（售票员模型改藏青制服的占位） |
| Unity | `game/Assets/Editor/BuildSlice.Police.cs`、`PoliceChecks.cs` |

一次重建：`tools/BuildPolice.ps1`，然后 `BuildSlice.BuildStaging`（或 `Build`）。

## 验收

- 编辑器检查（`authoring-checks.json`）：52 个洞口全部是真洞，沿墙 13735 个探针没有多余或错位的洞；门、抽屉、灯、座位、雨棚与合同一致。
- 原生实机：`tools/ReviewPolice.ps1 -Mode police` 与 `-Mode verify`（独立 `police-review-save.json`，不碰正常存档），1920×1080 与 1280×720 各一次，结果和截图在 `review-1080/`、`review-720/`。
- 回归：最终构建上 police 两档各一对、full-map＋独立读档 PASS（`regression/summary-final.txt`）；前一构建上站务员、牛奶店门、地图导航、钟表铺、昼夜五对 PASS（`regression/summary.txt`，其中 full-map 首轮因旧断言写死 4 处成品失败，已修正）。完整日志与帧在忽略的 `builds/police/regression-full/`。
- 正常 Windows 入口 `builds/windows/TownDemo.exe` 已用同一程序集重建（Assembly-CSharp SHA256 前缀 891D83C9E3F7EC78），未运行正常存档。
- 同日随后的共面闪烁修复（`output/demo/zfight/`）又重建了正常入口（Assembly-CSharp 前缀 8E9873A2F092978C），警署 1080／720 与读档在新构建上复验通过。

## 已知局限

- 值班警员暂用已认可的售票员 Tripo 模型改藏青制服，脸相同；需要专属人物时请用 Tripo 生成后替换。
- 档案职员按设计“缺席不锁证据”，本版不设人物，目录在誊录桌上。
- 阶段报告只核对材料是否对得上。E05 派送簿、E06、E09、E10 还没有可取得的游戏内来源，所以本版只能完成“问路日期”这一组核心判断，不能进入“需要重新调查”的收束。
- 案卷摘要正文按修订稿第一节与证据合同排版，尚待用户确认措辞。
- 自动驱动的真实控制器与互动射线，不是物理键鼠、陌生玩家盲测、Mac 或低配帧率验收。
