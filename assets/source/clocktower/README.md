# 青川古钟塔素材状态

用户认可的外观是 `reference-approved-v3.png`：浅色均匀涂装、可见砖砌纹理、维护良好、墙装铭牌、紧凑古老欧式钟楼。

`reference-meshy.png` 为该图的建模参考版本（透明背景、无钟针、铭牌留白）；生成提示保存在 `meshy-reference-prompt.txt`。这两个文件保留历史命名，仅是本地建模参考，不表示存在 Meshy 三维模型。钟针与中文铭牌使用游戏内受控几何/排版。原批准图、建模参考图及哈希都保留在本目录。

## 当前修改：外挑钟室与可调机芯（2026-10-09，已交付）

用户最新参考为 `reference-timber-gable.jpg`。保留20米塔身、原入口、三个平台与旋梯；钟盘由2.2米放大至3.4米，钟心16.7米。顶部改4.8米宽外挑木构钟室，配托架、铜木钟框、木纹与四面山墙瓦顶；机房内净宽相应增至4.25米，墙顶18.5米。该造型是本地参考图建模，没有使用Meshy服务或神经图生推理。

共享布局同时驱动外壳、真实墙体、机房地板、钟盘位置、蛛网锚点与有效存档范围。`ClockTowerAdjustment` 使现有机架可被普通E射线触发；`DemoApp.TowerAdjustment.cs` 实现 −60/−30/+30/+60分钟和Reset五控件。所有塔钟读取持久偏移，怀表不受影响；Reset清零玩家调整，回到默认+30分钟。面板不显示绝对数字时间、默认钟差或谜底，不授予证物。当前源/回导和新运行结果分别记录于外壳verification及 `output/demo/clocktower-adjustment/`，七项实机检查均通过；当时源模型217163三角面、7组材质，门洞30条射线及FBX回导通过。已更新原桌面应用并正常打开，存档哈希不变。

## 上次交付：钟楼内装表面与积灰（2026-10-09）

在已交付 20 米版本上细化内部，不改变塔高、台阶与平台位置、门洞和行走碰撞。`ClockTowerInteriorSurfaces.cs` 已接入原内装生成：自制 512 像素颜色/法线纹理表现内墙石灰石砖与底层磨损石板，内墙衬面只负责显示、保留真实门洞；木质阶梯与平台保留原底材，按高度分为 8 档尘材质。低处薄、断续，上部与机房更厚，已在本轮实机目视对比下、中、上三段的过渡。

同一串 87 个脚印继续使用原位置、左右次序和鞋型，仅调整尘层反差、印记底色与破边：越上越清楚，数量与密度不增加。脚印应像压散/擦开尘层后露出木材，不做纯黑图章；清晰度不代表来访身份、时间或新旧。

`ClockTowerAtmosphere.cs` 已接入内装构建，在顶部三处墙角及上段三处扶手外侧制作 6 处有结构锚点的不规则灰白半透明蛛网；机房局部漂浮尘粒上限 32 粒，尺寸小、低密度、缓慢移动。两类装饰没有碰撞，不横封门口、楼梯净空或扶手行走侧；最终锚点使用共用内墙位置，丝端轻入衬面1mm；实机已查看墙角网和浮尘。

本轮已完成正式Mac构建、四项针对性实机检查和桌面更新，正常启动后存档哈希不变。最终脚印颜色/破边微调后，1080p与720p已重新跑完整上楼和气氛检查；初轮机房重启下楼及中层存档结果对应未变的几何/保存代码。本轮证据目录 `output/demo/clocktower-interior/`，不覆盖 `output/demo/clocktower-20m/` 的既有通过记录。Q 怀表、时间比例、塔钟半小时偏差、稳定证物 ID 与隐藏真相边界保持不变。

资料与适用范围见 `interior-reference-research.json`：尘层参考 [National Trust](https://www.nationaltrust.org.uk/our-cause/history-heritage/tackling-dust-in-historic-houses)和 [English Heritage 托管论文](https://www.english-heritage.org.uk/siteassets/home/learn/conservation/collections-advice--guidance/icom14dust.pdf)；蛛网形态与锚点参考 [Natural History Museum 网形说明](https://www.nhm.ac.uk/discover/spider-webs.html)及[蛛丝说明](https://www.nhm.ac.uk/discover/what-are-spider-webs-made-of.html)；[OPB 联合车站钟楼实景](https://www.opb.org/article/2024/03/11/time-marches-forward-at-portland-historic-union-station-on-daylight-saving/)为砖墙、木梯与维修照明提供视觉参考。外部照片未复制到游戏贴图。上层少清理是本塔美术设定，不能把文献解释为灰尘普遍随高度增加，或据尘层推断脚印年月。

## 已交付基线：约 20 米三层钟楼（2026-10-09）

用户最新接受现有游戏钟楼作修改基础，要求塔高约 20 米、具备三层体量。本轮继续修改现有本地参考图重建的 Blender 几何与纹理，不以图生模型推理作为增高前提。此前图片条件形状与纹理生成的研究仍属独立技术记录；本轮没有执行神经图生 3D，也不将用户对现有钟楼的接受写成该技术已经完成。

源文件和四张 Blender 预览见 `exterior/`，生成脚本为 `tools/clocktower/build_exterior.py`，Unity 资源为 `game/Assets/Art/ClockTower1931/`。新 20 米外壳已完成，7 组网格共 230159 个三角面，静态回导尺寸检查与正式 Mac 构建通过；中层、机房、钟心及塔顶标记与共享内装匹配。源码布局 18 项静态检查通过。本轮 12 项可见播放器检查全部通过：时间与时间重启、1080p/720p 钟楼、楼上读档与下楼、原钟表铺、四个旧梯阶次与旧机房的五项迁移，以及中层平台存档恢复。原始日志和结果在 `builds/clocktower-20m/verification/`，精简证据目录为 `output/demo/clocktower-20m/`。最终仅验收相机视线目标调整后重新构建，`middle-save` 复跑通过并目视核对中层旋梯画面；几何及控制器未变。20 米版已备份旧应用后更新桌面正式入口，正常打开至标题菜单，启动前后正常存档哈希一致。

此前 9.5 米版本已通过六项正式播放器检查，固定桌面入口备份后更新，正常存档保留；历史结果和截图见 `output/demo/clocktower/`。这些结果只对应旧尺寸；20 米塔另有上述本轮结果，不沿用旧结果作为新布局的验证。旧灰壳验证包同样仅作历史记录。

历史制作方式仍保持如实记录：已交付钟楼是参考图脚本重建，并非图片条件模型推理。早期曾误将需求理解为指定 Meshy 网站，参考图未上传、任务未提交、额度未消耗，没有 Meshy 导出或许可可记录。本次不登录或上传 Meshy，也不通过其他平台变相上传。

## 已有可编辑实现与验证范围

- `game/Assets/Editor/BuildSlice.ClockTower.cs`：同一建筑、实体木门、四面动态钟、墙装历史牌的场景生成入口；已通过 `ClockTowerAsset.cs` 接入本地精制外壳。
- `game/Assets/Editor/ClockTowerInterior.cs`：生成螺旋梯、灰尘与鞋印、传统齿轮机芯；全部为项目自产可编辑网格。
- `game/Assets/Editor/ClockTowerInteriorSurfaces.cs`：本轮新增内墙/地面砖石纹理和按高度分配的尘材质与脚印材质。
- `game/Assets/Editor/ClockTowerAtmosphere.cs`：本轮新增 6 处蛛网与最多 32 粒上层浮尘，无装饰碰撞。
- `game/Assets/Scripts/ClockTowerInteriorLayout.cs`：共用楼梯、平台和内净空尺寸，以及新布局的 101 个脚底行走检查点。
- `tools/clocktower/build_lettering.py`：可读中文铭牌与无针罗马数字钟面。
- `BuildSlice.BuildClockTowerPreviewMac`：上一轮内部验证入口，隔离应用标识 `com.qingchuanstudio.wrongdatetest.towerreview`。该包的临时灰壳只用于验证内部、碰撞与时间，不能当作最新精制外壳已经通过的证据。

新外壳总高 20 米，塔身外宽 3.6 米、内净宽 3.05 米。地面高程 0.12 米，中层平台 8.530435 米，机房平台 15 米；内墙至 18.3 米，钟心高 16.72 米，钟面直径 2.2 米。南门净宽 1.1 米、净高 2.15 米，门与砖纹维持人的尺度，不能把旧模型和贴图整体拉长。

旋梯共 92 个路径段：52 个下段台阶、5 个同高的平台段（共 90°）、35 个上段台阶。中层平台提供实际停留点，灰尘和 87 个清晰鞋印按新梯重新生成，机芯随平台移至 15 米。外壳和内装统一读取 `ClockTowerInteriorLayout.cs`；101 个脚底路径点用于真实控制器行走验证，不能用传送代替。

旧 9.5 米塔内部的楼梯存档须按新布局版本映射到相应阶次安全点，旧机房位置迁移到 15 米机房，保留证物、门状态、累计时间与对白进度。本轮独立进程已验证旧梯第 1／12／20／31 级和旧机房迁移，角色能在新安全点站立并移动，进度保留；中层平台存档恢复及机房读档后逐级下楼也分别通过。Q 怀表持续计时、现实 7 秒推进游戏 10 分钟、四面塔钟快 30 分钟以及无时间 HUD 的规则保持不变。完整第四版全镇地图未在此任务迁移。

## 结构参考与叙事边界

机芯参考 Chris McKay / Central Council of Church Bell Ringers 的 [Turret Clock Handbook](https://cccbr.org.uk/wp-content/uploads/2018/01/Turret-Clock-handbook.pdf)，采用传统铁架、轮轴、铜齿轮、卷筒与摆锤的可辨识组合，是游戏中的简化机构而非精确机械复原。动画是视觉模拟，不是另一套校时源。

钟楼建于 1816 年、埃利亚斯·霍夫曼主持建造，是该虚构小镇的背景。鞋印由原主之前调钟留下属于开发隐藏真相；玩家看到的痕迹不自动证明身份、日期或调钟动作。E06 仅补强当前双钟差值，历史钟差仍需原有证据交叉核对。E01–E10 的稳定 ID 与既有推理依赖保持不变。

验收范围：上述行走与交互由脚本驱动真实控制器和射线，不是物理键鼠或陌生玩家盲测。12 项结果属于本轮已测试构建；最终只调整验收相机目标后重新构建并复跑中层存档检查。桌面部署、启动和存档哈希结果见本轮精简证据内的 `build-manifest.json`；未在 Intel 实机或进行长时间性能验收。

## 当前磨砂钟盘与调时动画修订

四面外壳、实体墙和内衬同步开直径3.4米的圆孔。`ClockTowerGlazing.cs` 生成实时磨砂玻璃、实体罗马数字/环/撑条及防跌碰撞；`FrostedClockGlass.shader` 对当前真实外景做乳白混色和模糊。内外共用ClockDial及实体钟针，背面自然镜像；没有内部表面静态图片。每次调整经过3秒驱动曲线，三轮按原齿比多圈加速/降速；五控件与键盘处理共用busy锁，收起重开也不能打断。调时UI底色alpha0.38。

当前源模型221323三角面/7材质组，36条钟洞与30条门洞射线、回开/FBX回导通过；七项最终播放器验证及720p/1080p截图在 `output/demo/clocktower-glass/`。原桌面入口已备份更新，正常存档保持。Blender外观预览仍使用仅用于构图的历史钟面叠图，实际Unity玻璃和立体字/针以实机证据为准。

## 材质增强

`ClockTowerMaterialFinish.cs` 生成1024px木纹与金属颜色/法线/金属度光滑度图，校正机架与齿轮的米制平面UV，不改碰撞或几何。内墙石板1024px并增强凹凸，保留上层积灰和同串鞋印。机房一次性256px局部反射与一盏软阴影灯配合；全局SMAA、主光阴影与色调增强。实际效果和帧耗时见 `output/demo/clocktower-quality/`；外部Blender模型未改，不把旧Blender预览算作本轮材质效果。
