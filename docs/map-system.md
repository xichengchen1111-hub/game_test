# 青川镇手持纸地图

## 当前操作（2026-10-10 用户修订）

M 掏出／收起地图；不切换全屏界面，不暂停人物、视角或游戏时间。纸张实际放在人物身前，随脚步轻微晃动。用原有鼠标视角低头即可看完整纸图，抬头继续观察街道和行走。Esc 在正常持图时收起地图；对话、调查簿等界面仍按自己的 Esc 规则关闭。

需要标记时按住 Alt，鼠标暂时脱离视角锁定，右键点击纸上的位置或建筑图标。右键新目的地替换旧目的地，右键坐标图标的上半部／尖端均可取消。松开 Alt 恢复观察。Alt 模式也不暂停人物与时间。滚轮缩放纸上内容，按住左键拖动；纸张本身不变成浮动窗口。点到纸外／天空不设置标记。建筑图标对应实际门外入口。

纸图使用米制第四版 TownPlan，钟楼 (0,0)，东 +X、北 +Z。棕色墨线、建筑排线、纸纹、折痕、稍微翘起的纸边和印刷地名替代上一轮的数字地图面板。所有图形及纸纹由共用代码绘制，不借用其他游戏贴图。按用户最新要求，暂不显示手、手臂或袖口。纸上不印键盘提示，持图时也不显示底部「低头看地图／Alt／M」或WASD操作提示条；右上角目的地与距离保留。M取出播放短纸张展开声，M／Esc收起播放折叠声；临时被阅读界面藏起／恢复时不重播。

## 参考与选择

用户指定 Minecraft 式的手持体验。[Minecraft 官方 Explorer Map 说明](https://www.minecraft.net/en-us/article/taking-inventory--explorer-map)作为地图物件与地点表示参考；本作使用自己的年代纸张、全镇街道与地理内容。抬头／低头是玩家原有连续视角，M 没有自动低头、暂停或传送。

上一轮参考仍适用于标记和路线：[GTA PC 官方手册](https://media-rockstargames-com.akamaized.net/rockstargames-newsite/img/manuals/en_au/GTAEFLC_G4W_MANUAL_CRED18_AUS.pdf)的右键设置目的地、左键拖动；[GTA Online 官方指南](https://www.rockstargames.com/newswire/article/k49a58878a85k7/gta-online-game-tips-putting-in-vip-work)的目的地／GPS 配合；[任天堂 Zelda 官方手册](https://www.nintendo.com/eu/media/downloads/games_8/emanuals/nintendo_3ds_2/the_legend_of_zelda_a_link_between_worlds/ElectronicManual_TheLegendOfZeldaALinkBetweenWorlds_EN.pdf)的可移除图钉；[Ubisoft Valhalla 开发者访谈](https://news.ubisoft.com/en-us/article/4tgD0C1axNIUeJrxZLqVKL/assassins-creed-valhalla-why-changes-to-storytelling-and-quests-made-sense-for-the-viking-saga)的地图支持主动探索。地图只标地理建筑，不暴露隐藏证据、推理结论或未知历史路线。

## 地点与导航

16 个重点地点保留：钟楼、牛奶站、已有 H03 裁缝商住、钟表铺、主角／邻居住宅、警署资料室、邮局、车站、医院、旅馆、修车铺、酒馆、当铺、背景杂货铺、背景教堂。图形包含钟塔／奶瓶／剪刀／房屋／盾牌等，地名直接印在纸上；住宅地名错行避免相互覆盖。B04 画实际 milkRect 与附属体，其生产院落不被画成整块房屋。不改变建筑、人物、证据或地图坐标；既有占位房屋也没有因此新增室内。

仍使用 `TownNavigationBake` 从同一 Town 碰撞体生成 NavMesh，人物高1.78m／半径0.30m／台阶0.24m／体素0.075m。运行时完整路径按均一通行成本计算，范围包括实际可走的街道、草地、院落；这是离散导航网格内的近路，不是连续几何的数学全局最短证明。普通坐标最多贴近4m内可通行地面，限制高度差避免吸到屋顶；无法到达不画伪造直线。门需手动开启，路线不自动带玩家走或传送。塔内路线仍投影到同一纸图，尚无楼层切换。

持图行走每移动约0.8m后、最多每秒重算一次路线；轻量线条网格叠印到纸上，人物箭头每帧更新，普通图钉随存档保留。已有上一轮标记无需重置存档。

纸地图目前覆盖青川 Town，包含已经制作的 B17 车站。坐在普通座位或到站座位上仍能取图；发车自动收图并退出 Alt 编辑，标记保留。邻镇暂未制作纸地图，M 不显示青川路线；返回青川后可重新取图，导航与保存的标记继续可用。

## 呈现与输入

`HeldTownMap` 建立稍微卷曲的实体纸张和地图印层；只在私有30层由 URP Overlay 相机渲染。相机随眼睛，纸张随身体，头部俯仰自然改变看到的纸张面积；深度独立，近墙时纸张不会穿到墙面后。没有新增世界灯／阴影灯，地图材质不改变原环境渲染。

用同一透视相机的屏幕射线与实际印刷平面相交，把鼠标位置反算成纸面／世界坐标。缩放以命中位置为中心，地图裁切和路线边界共用同一坐标换算。字体使用现有 TownSans；动态字体图集更新时重新计算印字 UV。纸纹／底图／图标缓存，行走仅更新箭头和线条，不每帧重画整张纹理。

持图不进入 Blocked。对话、物件检查、订单和调查簿阻断移动／视角，时间继续推进；暂停菜单同时冻结时间。上述界面暂时藏起纸张；关闭后按仍持图状态恢复。Q 怀表与手持地图互相收起，避免道具重叠。Alt 鼠标编辑模式与物件持有状态独立，收起／进入其他界面／松开 Alt 都清理编辑状态。

## 实机验证

Unity6000.3.18f1，BuildSlice.BuildFreshMac；原生玩家 `--review --fresh --review-map` 和独立进程 `--review --verify-map-save`，专用 map-review-save.json。

覆盖：M 不自动转头或暂停，抬头／低头投影面积改变，低头持图和抬头持图的真实控制器行走及时间推进，Alt 释放鼠标且不进入暂停，纸面透视点击与坐标回算，全部16入口路径与实体扫掠，图钉上半部／尖端取消，纸外不落标记，纸面缩放／拖动，三个可玩房间到室外导航，旅馆绕行真实实走与持图路线随位置更新，截图中真实路线投影墨色采样，阅读界面锁定移动但继续计时／显式暂停冻结／纸张隐藏恢复，Esc 收起，独立进程恢复图钉和其他进度。程序驱动共用处理器与控制器，不等于物理键鼠输入或陌生玩家盲测。

手持纸图的历史截图与各次日志位于 builds/macos-current/verification-held-paper-20261010-*；2026-10-10每日阵雨和阅读不停表版本重新运行地图／独立读档，结果与确切程序集哈希位于 builds/macos-current/verification-daily-weather-20261010/results.json。正常存档隔离结果以同目录save-isolation.json及agent-memory为准。上一轮全屏地图验收仅为历史，不能代替本轮持图验证。

2026-10-10取放音效／去底部提示版本重新通过720p与1080p地图和独立恢复，音效四声实际音源输出及临时阅读不重播见builds/macos-current/verification-props-heavy-clouds-20261010/acceptance.json。
