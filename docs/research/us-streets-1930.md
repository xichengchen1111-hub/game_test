# 1930—1931 美国小镇街道：考据与本镇实现

核对日期：2026-10-12。美国城市和州之间有差异；本镇未指定真实州，因此选一个前后一致的市政方案，不宣称全国每条街都如此。游戏既有1931年、街区尺度、建筑与剧情位置不变。

| 要素 | 历史依据与本镇取舍 |
| --- | --- |
| 马路、人行道 | 当时已有沥青/水泥铺面，砖石与未铺路也并存。本镇作为已有铁路、市政设施的镇中心，采用磨旧的细颗粒沥青、分格混凝土步道、石路缘和石砌排水边沟；这是设计选择，不是对某个真实城镇的复原。 |
| STOP | 1930城市手册规定八角形，黄底红字，并示范 THROUGH / STOP / TRAFFIC；本轮采用24英寸规格。1927公路体系存在黄底黑字，不能把两套规定混成唯一全国样式。 |
| GIVE WAY / YIELD | 不放。西北大学的历史记录将美国首个Yield实用路牌记在Tulsa，1950年，晚于故事年代。已有让行规则不等于已有现代让行牌。 |
| 人行横道 | 已有过街边界线。本轮采用两条平行磨损白线，避免直接套用今天的宽条斑马线或梯形标线。不是声称当时所有城市禁止任何条纹试验。 |
| 地面标记 | 中心线、停止线和方向引导已有。本轮只在主路口附近画短白中心线与停止线，不铺满全镇双黄线、不加现代车道箭头/让行三角。 |
| 红绿灯 | 三色灯至少1920年已有。主街×中街放一组悬吊四面灯，红上黄中绿下，圆透镜与铸铁罩；其余路口用停止牌。悬吊净空高于14英尺。 |
| 排水与细节 | 补分格接缝、路缘侧面、石砌边沟、铸铁箅子、牌背夹件及铆钉。具体纹理与构件为原创美术，未冒用某制造商型号。 |

## 可追溯来源

1. [1930 Manual on Street Traffic Signs, Signals and Markings，原手册](https://rosap.ntl.bts.gov/view/dot/73360)：印刷页13–14、29–34、图1。原文支持黄底红字、横道边界与停止线、三色灯、悬吊布置、净空和颜色顺序。图库目录把现代保管机构写成作者；历史出版语境是National Conference on Street and Highway Safety / Department of Commerce，不能误称1930年已有今天的US DOT。
2. [The Henry Ford：1920三色四向信号灯实物](https://www.thehenryford.org/collections/explore/artifact/227457)：William Potts，Detroit；用于确认三色信号灯在故事年代已存在。造型为原创近似，不复制馆藏品牌或宣称精确复制该实物。
3. [Northwestern：The Father of the Yield Sign](https://magazine.northwestern.edu/news/the-father-of-the-yield-sign)：1950 Tulsa首次设置Yield。
4. [Library of Congress：约1930年美国南方小巷](https://www.loc.gov/pictures/item/2009633951/)：目录明确描述人行道、街灯、消防栓。地域样本，不能代表全国。
5. [TRB百年交通控制史](https://onlinepubs.trb.org/onlinepubs/centennial/papers/AHB50-Final.pdf)：图4、5区分1927公路手册与1930城市手册，也说明早期路面标记用色尚未统一为今天的习惯。

## 实现边界与复建

`tools/build_period_streets.py`生成原创2K沥青/混凝土纹理、路牌和`PeriodStreets.json`；`StreetSurfaces.Period.cs`共用构建器生成街道设施，Town与旧室内窗景副本同源。停止线和STOP牌画在街车实际停车的线上：脚本按`StreetNet`的规则算线，南北向窄街为拐进来的车身后退约2 m（2026-10-16，记忆STREET-04）。

平面图及道路中心线/宽度/建筑/绿化位置不变。修复原编译器丢失凹多边形三角面的问题：补回人行道9994.003㎡、路缘168.840㎡、院落1633.481㎡，车行面9622㎡未变；原有面没有删除。新增逐块覆盖面积和向上绕序断言。TownPlan现统一为本机LF源散列，GreeneryPlan只更新source字段，植物与土面数组未动。16块STOP、一个信号路口、3处路口横道、108组铁箅子。路牌和杆体有18个简易碰撞，排水/标线不添加绊脚碰撞。新悬吊信号灯仅灯片发光；16块停止牌各有带罩白炽小照明，随现有自然亮度开关，借用原12盏邻近灯池，不加阴影灯。

信号取自保存的同一个游戏时钟，显式暂停冻结，读档相位可重建。58现实秒的周期是可玩的表现选择（游戏本身12倍时钟），不是声称历史信号以12倍时间运行。无NPC车流、交通罚则、新证据或历史驾车路线。手册指出低流量可用闪黄；当前本镇尚未模拟交通密度，未加入该切换。

复建：`python3 tools/build_period_streets.py`，再用Unity6000.3.18f1 `BuildSlice.BuildFreshMac`。验收入口：`--review --fresh --review-period-streets`；另做全镇可走宽度/六访问顺序与汽车回归。实际结果见`output/demo/streets-1930/README.md`，不能把这段预定流程当已通过。
