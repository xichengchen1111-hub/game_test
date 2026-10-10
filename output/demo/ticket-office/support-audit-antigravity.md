# 车站售票处与车票检视物理支撑审计报告 (Ticket Counter & Support Audit)

**审计范围**: 青川车站 B 方案（铁与玻璃长廊）售票柜台、售票室家具陈设、车票/工具/废票桶几何与检票交互
**代码基准**: `tools/rail/build_rail_assets.py`、`tools/rail/rail_detail_kit.py`、`tools/rail/rail_detail_refinements.py`、`game/Assets/Editor/BuildSlice.Tickets.cs`、`game/Assets/Editor/BuildSlice.Rail.cs`
**审计性质**: 只读物理支撑与射线碰撞审计（无代码修改、不编造历史事实与时刻表）
**基准高程与坐标约定**:
- 地面顶面 (Floor Top Datum): **$Y = 0.100\text{ m}$** (`HallFloor` 顶面)
- 柜台台面 (Counter Top Datum): **$Y = 1.150\text{ m}$** (`TicketCounterTop` 顶面)
- 门头/挂牌高程 (Header Datum): **$Y = 2.700\text{ m}$** (`SignBacking_Tickets` / `售 票 处` 挂牌中心)
- 售票员基准 (Clerk Position): **$(-8.000, 1.640, 1.900)\text{ m}$**

---

## 1. 核心构件物理悬空与重叠间距计算表 (Exact Calculated Gaps & Intersections)

| 构件对象 | 资产定义位置 | 几何范围 (X, Y, Z) 米 | 预定支撑面 / 参照面 | 计算间距 / 嵌入量 | 物理状态与缺陷定性 |
| :--- | :--- | :--- | :--- | :--- | :--- |
| **信件鸽巢柜体整体**<br>`TicketPigeonholeBack`<br>`Sides / Edges` | `build_rail_assets.py`<br>L568-571 | $X\in[-10.20,-5.80]$<br>$Y\in[1.398, 3.003]$<br>$Z\in[2.325, 2.678]$ | 地面顶面<br>$Y = 0.100\text{ m}$ | **$+1.298\text{ m}$<br>($+1298\text{ mm}$)** | **严重悬空**：整组 4.4m 宽鸽巢柜底面在 $Y=1.398\text{ m}$，下方至地面悬空 1.3 米，无矮柜、立柱、桌腿或墙体支撑（距后墙 3.32m）。 |
| **鸽巢内部隔板**<br>`PigeonDivider_0..7` | `rail_detail_kit.py`<br>L326-329 | $X = -9.65\dots-6.36$<br>$Y\in[1.475, 2.925]$<br>$Z\in[2.330, 2.630]$ | 柜底边框顶面<br>$Y = 1.443\text{ m}$ | **$+0.0325\text{ m}$<br>($+32.5\text{ mm}$)** | **悬空与穿透**：隔板底端未接触柜底；顶端距柜顶差 $32.5\text{ mm}$；与 4 层连续水平隔板在 32 处交点发生实体网格重叠穿透。 |
| **文件卷宗 0**<br>`PigeonDocBundle_0` | `rail_detail_kit.py`<br>L333 | $(-9.40, 1.80, 2.50)$<br>底面 $Y = 1.740\text{ m}$ | 搁板 0 顶面<br>$Y = 1.711\text{ m}$ | **$+0.029\text{ m}$<br>($+29\text{ mm}$)** | **悬空**：卷宗悬空飘浮在搁板上方 29 毫米处。 |
| **文件卷宗 1**<br>`PigeonDocBundle_1` | `rail_detail_kit.py`<br>L334 | $(-8.93, 1.80, 2.50)$<br>底面 $Y = 1.760\text{ m}$ | 搁板 0 顶面<br>$Y = 1.711\text{ m}$ | **$+0.049\text{ m}$<br>($+49\text{ mm}$)** | **悬空**：卷宗悬空飘浮在搁板上方 49 毫米处。 |
| **文件卷宗 2**<br>`PigeonDocBundle_2` | `rail_detail_kit.py`<br>L335 | $(-7.99, 2.08, 2.50)$<br>底面 $Y = 2.010\text{ m}$ | 搁板 1 顶面<br>$Y = 1.991\text{ m}$ | **$+0.019\text{ m}$<br>($+19\text{ mm}$)** | **悬空**：卷宗悬空飘浮在搁板上方 19 毫米处。 |
| **文件卷宗 3**<br>`PigeonDocBundle_3` | `rail_detail_kit.py`<br>L336 | $(-7.52, 2.08, 2.50)$<br>底面 $Y = 2.000\text{ m}$ | 搁板 1 顶面<br>$Y = 1.991\text{ m}$ | **$+0.009\text{ m}$<br>($+9\text{ mm}$)** | **悬空**：卷宗悬空飘浮在搁板上方 9 毫米处。 |
| **文件卷宗 4**<br>`PigeonDocBundle_4` | `rail_detail_kit.py`<br>L337 | $(-7.05, 2.36, 2.50)$<br>底面 $Y = 2.310\text{ m}$ | 搁板 2 顶面<br>$Y = 2.271\text{ m}$ | **$+0.039\text{ m}$<br>($+39\text{ mm}$)** | **悬空**：卷宗悬空飘浮在搁板上方 39 毫米处。 |
| **文件卷宗 5**<br>`PigeonDocBundle_5` | `rail_detail_kit.py`<br>L338 | $(-6.58, 2.36, 2.50)$<br>底面 $Y = 2.285\text{ m}$ | 搁板 2 顶面<br>$Y = 2.271\text{ m}$ | **$+0.014\text{ m}$<br>($+14\text{ mm}$)** | **悬空**：卷宗悬空飘浮在搁板上方 14 毫米处。 |
| **文件卷宗 6**<br>`PigeonDocBundle_6` | `rail_detail_kit.py`<br>L339 | $(-8.46, 1.52, 2.50)$<br>底面 $Y = 1.450\text{ m}$ | 柜底框顶面<br>$Y = 1.4425\text{ m}$ | **$+0.0075\text{ m}$<br>($+7.5\text{ mm}$)** | **悬空**：卷宗悬空飘浮在柜底板上方 7.5 毫米处。 |
| **手持检票钳视觉模型**<br>`PunchFixedHandle` 等 | `rail_detail_refinements.py`<br>L23-27 | $(-6.8\dots-6.55, 1.2, 0.64)$<br>最低面 $Y = 1.188\text{ m}$ | 柜台顶面<br>$Y = 1.150\text{ m}$ | **$+0.038\text{ m}$<br>($+38\text{ mm}$)** | **悬空**：检票钳视觉整体飘浮在柜台上方 38 毫米，无台面托架或支点接触。 |
| **手持检票钳碰撞体**<br>`Group("手持检票钳")` | `BuildSlice.Rail.cs`<br>L223-224 | 中心 $(-6.68, 1.235, 0.64)$<br>底面 $Y = 1.2025\text{ m}$ | 柜台顶面<br>$Y = 1.150\text{ m}$ | **$+0.0525\text{ m}$<br>($+52.5\text{ mm}$)** | **碰撞体悬空脱位**：BoxCollider 底端高于台面 52.5mm，且高于视觉模型底面 14.5mm。 |
| **台铃 (双模型重叠)**<br>`TicketBell` vs<br>`CashierBellBase/Dome` | `build_rail_assets.py` L567<br>`rail_detail_kit.py` L366-371 | 中心 $(-7.30, Y, 0.85)$<br>高程 $Y\in[1.146, 1.228]$ | 柜台顶面<br>$Y = 1.150\text{ m}$ | 环悬空 **$+5\text{ mm}$**<br>座嵌入 **$-4\text{ mm}$** | **同位严重穿插重叠**：旧版圆环 `TicketBell` 与新版台铃 `CashierBell` 在同一坐标并存，圆环切入铃体；底座嵌入柜台 4mm。 |
| **墨水盘底座**<br>`InkstandTray` | `rail_detail_kit.py`<br>L374-375 | 中心 $(-8.50, 1.155, 0.85)$<br>底面 $Y = 1.147\text{ m}$ | 柜台顶面<br>$Y = 1.150\text{ m}$ | **$-0.003\text{ m}$<br>($-3\text{ mm}$)** | **嵌入**：木制墨水盘底面嵌入柜台顶面 3 毫米。 |
| **账本 (双模型重叠)**<br>`TicketLedger` vs<br>`SafeLedgerCover` | `build_rail_assets.py` L566<br>`rail_detail_kit.py` L388-398 | 中心 $(-8.00, 1.16, 1.15\dots1.20)$<br>高程 $Y\in[1.135, 1.185]$ | 柜台顶面<br>$Y = 1.150\text{ m}$ | 嵌入 **$-15\text{ mm}$**<br>及 **$-12.5\text{ mm}$** | **同位严重穿插重叠**：绿色旧账本与红色新账本在售票员面前近乎 100% 重叠交错；且两账本底面均沉入柜台台面 12.5~15mm。 |
| **废票桶内 8 张已验废票**<br>`Punched discarded ticket 1` | `BuildSlice.Tickets.cs`<br>L36-37 | 桶中心 $(-11.30, 0, -0.80)$<br>票 1: $(0, 0.780, -0.17)$ | 铁丝废票桶开口铜圈<br>$Y = 0.780\text{ m}$ | **$0.000\text{ m}$** | **接触合理**：斜搭于废票桶口上缘铜圈。 |
| **废票桶内 8 张已验废票**<br>`Punched discarded ticket 2..8` | `BuildSlice.Tickets.cs`<br>L36-37 | 票 2 高程 $Y = 0.579\text{ m}$<br>票 8 高程 $Y = 0.693\text{ m}$ | 桶内底铁板<br>$Y = 0.110\text{ m}$ | 票 2 悬空 **$+0.469\text{ m}$**<br>票 8 悬空 **$+0.583\text{ m}$** | **严重悬空透视**：废票桶为 18 根细铁丝构成的镂空网篮，票 2 至票 8 整体飘浮在半空中，下方至桶底有 **46.9 厘米的完全空白空气**，从外侧直视破绽明显。 |
| **售票处挂牌及过梁**<br>`SignBacking_Tickets`<br>`RailSurfaceText("售 票 处")` | `build_rail_assets.py` L697<br>`BuildSlice.Rail.cs` L200 | 中心 $(-8.00, 2.70, 1.00)$<br>$Y\in[2.50, 2.90]$, $Z=1.00$ | 售票窗口铜栅栏顶端<br>$Y = 2.250\text{ m}$ | **$+0.250\text{ m}$<br>($+250\text{ mm}$)** | **空中悬浮**：挂牌底端距窗口铜栅栏顶端空隙 25cm；挂牌上方至屋顶桁架有 3.0 米挑空，无任何过梁、隔墙、门头板或横档连接。 |

---

## 2. 建筑与结构支撑修复方案 (Architectural & Support Repairs)

1. **售票室后侧鸽巢柜体 (Pigeonhole Unit) 落地与承重结构修复**:
   - **增设基座矮柜/桌腿**: 在鸽巢下方 ($X\in[-10.20, -5.80]$, $Z\in[2.30, 2.68]$, $Y\in[0.100, 1.400]$) 构建配套胡桃木售票后勤文件矮柜，或在两端及中央增设立柱桌腿落地于 $Y=0.100$。
   - **隔板交错槽口处理**: 鸽巢内部 4 层水平搁板与 8 块垂直隔板深度同为 $0.30\text{ m}$，必须在相交处开槽配合，垂直隔板高度应修正为 $1.515\text{ m}$，使其顶底面分别与外框的 $Y=1.4425$ 及 $Y=2.9575$ 严密贴合（消除目前上下各 $32.5\text{ mm}$ 的脱节）。
   - **卷宗高程对齐**: 将 7 处卷宗中心高程下移 $7.5\sim49\text{ mm}$，底面与对应搁板顶面严格贴合。

2. **柜台工作台面器物接触面修正与冗余清除**:
   - **删除重复资产**: 彻底移除 `build_rail_assets.py` 中遗留的旧版圆环 `TicketBell` 与绿色旧账本 `TicketLedger`，仅保留 `rail_detail_kit.py` 的精致台铃组与带角铁封面的账本组。
   - **台面标高标定**: 柜台台面基准为 $Y=1.150\text{ m}$。台铃底座 `CashierBellBase` 上移 $4\text{ mm}$（底面调至 $1.150$）；墨水盘底座 `InkstandTray` 上移 $3\text{ mm}$（底面调至 $1.150$）；账本整体上移 $12.5\text{ mm}$（底面调至 $1.150$）。
   - **检票钳支承**: 检票钳视觉模型下移 $38\text{ mm}$ 使下握柄平放于台面（或在台面新增 1931 年黄铜检票钳置物座/皮垫），并同步校准 Unity 的 `BoxCollider` 中心与尺寸。

3. **镂空废票桶内部填充与废票物理沉降**:
   - 镂空铁丝桶外表极易透视，票堆不可悬浮于桶半腰。
   - **修复选项 A (垫底纸堆)**: 在桶内底层构建废旧纸屑/车票碎屑聚集体模型 ($Y\in[0.110, 0.560]$)，作为票 2..8 的物理支撑底座。
   - **修复选项 B (自然沉底)**: 将票 2..8 的下落起始高程下移至桶内铁底板 ($Y=0.110\text{ m}$)，呈扇形叠放于桶底。

4. **售票窗口门头过梁与隔板建筑构造补全 (Header & Transom Bulkhead)**:
   - 现售票窗口铜栅栏顶部高程为 $Y=2.250\text{ m}$，门头挂牌位于 $Y=2.700\text{ m}$。
   - 需在 $Z=1.000\text{ m}$ 处补全经典 1931 售票隔断门头结构：两侧设立胡桃木包柱 ($X=-10.55$ 与 $X=-5.45$)，栅栏上方构建木制过梁板与横楣壁板 ($Y\in[2.250, 2.950]$)，承载“售 票 处”匾额，消除挂牌在空中孤立悬浮的断层感。

---

## 3. 标准 3 米射线与碰撞体遮挡风险排查 (Normal 3m Ray & Collision Risks)

在 `game/Assets/Scripts/DemoApp.cs:252` 中，玩家交互判定逻辑为：
```csharp
if (!Blocked && !RailSeated && !FurnitureSeated &&
    Physics.Raycast(player.eye.transform.position, player.eye.transform.forward, out var hit, 3f))
    focus = hit.collider.GetComponentInParent<Interactable>();
```
沿玩家第一人称视线发射 3.0 米检测射线。在售票处区域存在以下严重的射线截断与碰撞体冲突隐患：

1. **售票员交谈射线被铜栅栏与静态网格完全拦截风险**:
   - 玩家站在柜台外顾客侧 ($Z\approx0.2\sim0.5\text{ m}$)，售票员位于 $Z=1.90\text{ m}$，物理间距约 $1.4\sim1.7\text{ m}$（在 3m 范围内）。
   - 但在两者之间的 $Z=1.00\text{ m}$ 处，建有售票窗口铜栅栏（`TicketGrilleFrame` 与 `TicketBar_0..6`），并在构建时被赋予了全局建筑静态 `MeshCollider`（属于 `StationB`，父级无 `Interactable`）。
   - **致命问题**: 在中央窗口 ($gx=-8.0$)，代码在整个 $0.9\text{ m}$ 开间内密集排布了 7 根垂直铜栅栏柱（包括恰好位于中央正对售票员面部的 $X=-8.0$ 栅栏柱）。当玩家对准窗口中央按 E 时，射线直接命中铜栅栏的静态 `MeshCollider`，由于其父节点没有 `Interactable`，导致交谈判定彻底失败！
   - 此外，售票员视觉模型（`Module_TicketClerk`）本身也挂载了静态 `MeshCollider`，若射线穿过栅栏空隙但先击中身体网格而非独立的 `车站售票员` BoxCollider，也会导致交互丢失。
   - **整改要求**: 中央窗口拱形取票口（$Y\in[1.23, 1.67]$）内部必须留出开阔视线窗口，移除正中遮挡视线与射线的栅栏柱；售票员的交互碰撞体必须包裹整个面部与胸腔，或将静态模型与交互体统一归属。

2. **手持检票钳的双重碰撞体竞争风险**:
   - 检票钳在 Blender 源中作为 `Module_TicketTools` 导出，在场景构建时生成了静态网格碰撞体 `MeshCollider`（归属于 `StationB`，无 `Interactable`）。
   - 同时，C# 在独立节点 `手持检票钳` 上生成了挂载 `Interactable` 的 `BoxCollider`。
   - 检票钳视觉模型底端高于台面 38mm，而 BoxCollider 底端高于台面 52.5mm。当玩家自上而下或斜向观察检票钳时，视线很容易先击中外部的静态网格碰撞体而丢失交互。
   - **整改要求**: `Module_TicketTools` 应从建筑静态碰撞体生成名单中排除，仅保留其专属的 `Interactable` 碰撞触发体。

3. **废票桶交互碰撞体高程错位与标牌点击失效**:
   - 废票桶挂牌“废票”位于标高 $Y=0.40\text{ m}$，但 Unity 赋予 `废票桶 · 已验车票` 的交互 `BoxCollider` 高程范围为 $Y\in[0.605, 0.855]\text{ m}$（厚度仅 25cm，位于桶口附近）。
   - 玩家走近低头阅读桶前“废票”标牌并按 E 时，射线照射点在 $Y\approx0.40\text{ m}$，完全落空于该 `BoxCollider` 之外，击中外围建筑铁丝静态碰撞体，导致无法触发 E03 证物查看。
   - **整改要求**: 废票桶的交互 BoxCollider 应当向下延伸覆盖至地面（$Y\in[0.10, 0.85]\text{ m}$），完整包络桶身及标牌区域。

---

## 4. 交付文件路径与结论 (Audit Artifact Location)

- **输出审计文档路径**:
  `D:\AllPersonalProjects\Chenchengame\game_test\builds\review\ticket-checkin\agy-audit\support-audit.md`
- **审计结论**:
  已全面完成售票处、鸽巢柜、7 份卷宗、手持检票钳、台铃、墨水盘、账本、8 张已验废票、门头过梁及售票员对齐检查。发现了柜体离地 1.3m 悬空、废票桶下半部 47cm 空心悬浮、卷宗与检票钳全员悬空、账本/台铃双重网格重叠互穿、门头挂牌空中悬浮以及铜栅栏阻断 3 米交互射线的确切物理根因，并给出了具体建筑学修复方案与碰撞层隔离方案。无代码修改，无虚构历史或时刻表事实。
