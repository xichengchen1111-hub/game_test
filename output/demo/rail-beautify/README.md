# 车站与列车美化 · 2026-10-11

依据剧情（1931年1月、车站是“17:40露面”与未验票原票的核心地点）和已选定的 B「铁与玻璃长廊」／「墨绿与黄铜」设计，保留低模宽色块、灰蓝暗部与暖黄灯光的画风，补足建筑做法与表面质感。所有门窗开口、过道、上车踏步、座位、证物与交互都沿用原有合同；做法和文件见 [实现说明](../../../docs/rail-implementation.md#2026-10-11-车站与列车美化保留低模宽色块画风)。

## 改前／改后

| 画面 | 主要变化 |
| --- | --- |
| [正立面黄昏](before-after/station-B-front-dusk.jpg) | 拱石＋拱心石、窗台线、拱脚线、花岗岩勒脚、隅石、门套、玫瑰窗石圈、托石檐口；方形铁艺灯笼；正面檐沟在山花处断开不挡 QINGCHUAN |
| [侧面黄昏](before-after/station-B-wide-dusk.jpg)／[白天细节](before-after/station-B-front-detail-day.jpg) | 侧翼「行李房」字牌、补齐外山墙三角与窗边透光缝、落水管、屋脊花饰、烟囱 |
| [候车厅](before-after/station-ticket-hall.jpg)／[检票处](before-after/ticket-office-left.jpg)／[夜雨](before-after/ticket-office-night-rain.jpg) | 木板天花代替黑洞屋面，人字木、檩条、牛腿与垂饰；乳白球灯从脊梁垂下，墙面烛台灯；厅内看不到穿墙的侧翼屋面 |
| [站台](before-after/train-steam-platform.jpg)／[车窗外](before-after/train-window-rain.jpg)／[发车座位](before-after/train-seated-departure.jpg) | 雨棚玻璃贴合拱面（原为阶梯状水平条），锌板下跨、格条、屋脊通风楼、放射状端屏与挂檐；石板站台、压顶石、栏杆与检票口 |
| [客车内](before-after/train-coach-02.jpg)／[登车门](before-after/train-open-boarding.jpg) | 胡桃木护墙、束带酒红窗帘、地毯、窗下小桌、头枕；象牙白窗带与墨绿下部、金线、铜扶手、端门关闭 |

## 新画面（图集）

立面与站前：[正面](gallery/front-wide.jpg)、[入口](gallery/front-entrance.jpg)、[窗拱](gallery/window-detail.jpg)、[西翼](gallery/west-wing.jpg)、[白天](gallery/day-front.jpg)、[夜雨](gallery/night-rain-front.jpg)。
雨棚与站台：[后侧连廊](gallery/rear-way.jpg)、[通廊看检票口](gallery/concourse-barrier.jpg)、[检票口近景](gallery/barrier-close.jpg)、[站台侧](gallery/barrier-platform-side.jpg)、[东望](gallery/platform-east.jpg)、[西端端屏](gallery/platform-west-endscreen.jpg)、[仰视玻璃](gallery/shed-up.jpg)、[白天雨棚](gallery/day-shed.jpg)、[夜雨站台](gallery/night-rain-platform.jpg)。
室内：[候车厅西](gallery/hall-west.jpg)、[东](gallery/hall-east.jpg)、[天花](gallery/hall-ceiling.jpg)、[火炉](gallery/hall-stove.jpg)、[白天](gallery/day-hall.jpg)、[夜晚](gallery/night-hall.jpg)、[行李房](gallery/luggage-room.jpg)、[值班室](gallery/duty-room.jpg)。
列车：[机车](gallery/loco-three-quarter.jpg)、[车轮与轨道](gallery/wheels-on-rails.jpg)、[客车外侧](gallery/coach-exterior.jpg)、[车厢](gallery/coach-aisle.jpg)、[靠窗](gallery/coach-window.jpg)、[风挡](gallery/coach-gangway.jpg)、[夜间车厢](gallery/night-coach.jpg)。

## 验收

同一程序集（`acceptance.json` 中 SHA256）的 10 项原生检查全部通过，结果 JSON 在 `native/`：

| 检查 | 内容 |
| --- | --- |
| gallery-1080 | 31 个固定机位（黄昏／白天／夜雨），并输出每盏铁路灯的输出值与实际发光值 |
| rail-1080／rail-720 | 检票处、20 块青川字牌与 11 块邻镇字牌（贴面、字号 64→80→64）、两侧车门开关与防夹、两次跨镇往返 |
| rail-reload-1080／720 | 独立进程恢复到站座位、原票无孔与证物、可下车 |
| items-1080／items-reload-1080 | 59 处休息位、8 项环境查看、2 处车厢灯开关及读档 |
| day-night-1080 | 昼夜转换中所有灯（含新增侧翼灯、站台／候车厅／立面灯）的发光与照明一致 |
| weather-1080 | 雨、屋顶遮蔽（含新增后侧连廊雨棚）、天气灯控 |
| full-map-1080 | 全镇 70 宅地、导航与 6 种室外访问顺序（新增碰撞未挡路） |

`edit-results.xml`：EditMode 32/32。首轮 31/32：窗间柱“开洞”测试只禁用了射线碰到的第一个碰撞体，而柱内侧新增了胡桃木护墙板；测试改为移除该处全部碰撞体（真实开洞），`RailChecks.ValidateTrain` 本身未改。

本轮修复的原有缺陷：侧翼外山墙缺失、侧翼窗边透光缝、侧翼屋面穿入大厅、窗拱上悬空石块与铁饰、檐口托石浮空、吊灯／站台灯吊线不接结构、雨棚玻璃阶梯化、车轮不在轨道上、行李车墙板缺口、连杆曲柄方向与半径、`RailGlow` 缺 `_EMISSION` 导致铁路灯玻璃从未发光也不受昼夜控制、`Module_Hall_Lamps` 重名导致厅灯未绑定。

## 合并后的正常入口

汽车会话完成后，以合并源重建了 `builds/windows`（Assembly-CSharp `42628084…5D29`）：EditMode 53/53，rail 1080 往返与读档、items 与读档、汽车 1080 复查与独立读档全部通过，结果在 `integrated/`。正常存档前后哈希不变。

## 边界

- 程序驱动真实 CharacterController 与普通 3m E 射线，照片机位／时间／天气经过布置；不是物理键鼠、陌生玩家盲测、Mac 或低配帧率验收。
- 验证构建在独立 worktree（`game_test-railpolish`，分支 `station-train-polish`）的 `builds/windows-staging/`；同一批源文件已复制回 `game_test`。合并后的正常入口见上节。Mac 未构建、未实测。
- 改前图取自已提交的 RAIL-02／RAIL-05 实机截图（这期间站房外观与列车几何未变）；本轮最早截的一组改前图在临时目录中丢失。
- 不新增证物、证词、班次或时刻表；站台钟与正立面钟一样读取怀表同一时钟（不含塔钟偏差），检票口只是空间设施，检票钳仍在售票柜台。
