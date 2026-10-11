# Agent 持久记忆：错日证词

### PUBLISH-05：用户授权推送最新人物、绿化与售票员对话（2026-10-11）

- 用户确认售票员对话后明确要求“把最新版本push到github上”。CLI fetch确认main=origin/main=e079779，0/0，无新增远端提交和合并需求。发布范围为此前完成但未推送的NPC-04/05/06/07、GREENERY-01/02：Tripo送奶少年／坐姿修表钟表匠／中年售票员、对应动作和灯光／统一UI与六选项、7树形与22种植物及连续花境、共用生成器重建的场景／预制件／导航／模型、可编辑Blender／原始Tripo来源、文档／技能和精简实机证据。保留原证物及时间规则、角色／地图约束和正常存档。
- 发布前检查：远程一致、diff --check通过、待提交文件无100MiB超限和凭据模式；排除新增Milkboy.blend1自动恢复副本，并为三个人物源目录补齐恢复版本忽略规则，原文件仍留本机。现有builds／Unity缓存／完整日志／原始逐帧与存档备份继续不入Git。
- 验收沿用明确的各版本记录：最新NPC-07最终56/56 EditMode，8项原生共享UI／人物／票柜检查，以及日期答复换行后双分辨率／独立读档3项；此前人物和整镇绿化的较大回归见各自条目，不冒充全部在最新构建重跑。Mac原桌面应用及最终包已更新，源代码／资源的此次发布不新增游戏改动，无需再造相同构建。代码程序集9ccf0ced29248750a84c6f9b3e7673e6f978571ca5c622119a0bab501c69c6c9，资源8b113f1c575675b367e5ef10bfb58dbbaeeabb58198a2e4bcc3e7c0e42cf27cb；正常存档验收哈希adbb96bd5c40e7276ec6324e38a34bca7997f4efb2452b6d8106db3b36edf69a保留。最新证据位于output/demo/station-clerk-dialogue。
- 本条记录本次用户授权、整理范围与已执行检查；提交编号及推送完成状态以命令行Git、远端main核对和交付消息为准。

### NPC-07：售票员六选项、含糊日期记忆与统一 UI（2026-10-11）

- 用户要求先按 AGENTS 统一 UI，再新增“你好。”开场、玩家问20号是否见过和无法确定日子的回答。StationClerkDialogue.json 采用 greeting→topics 六选项：日期／情形／上车／检票／站台／告别，独立玩家问句→NPC回答→topics；可重复，不变色或标记已问。告别自然关闭。所有话题 station-clerk.* 持久保存，不修改 milkboyGreetingRevision 或已获证物。
- 记忆仅是“前几天好像见过”的面熟印象，不能确认20日、具体时间或上车；未增加证物卡／E03或E08前置。更新叙事约束与旧“无新增历史证词”说明，不能后续扩写成确定目击。主分支58字按停顿换行以避免末尾孤行。
- 复用 DemoApp.Dialogue.cs 透明底栏／两列完整选项／数字或鼠标选择／无选项右下四分之一区域推进／H与Esc，字号和最大alpha0.58保持。实际文本宽度计算选项高度，直接相同玩家节点不再在回看重复记录。角色沿用Tripo两动作，玩家发言待机、NPC回答说话。
- Unity6000.3.18f1最终EditMode56/56、叙事合同六访问顺序数据检查；原生Metal首轮8项（售票员双分辨率＋独立读档、票柜／废票桶、共享送奶UI双分辨率＋读档、钟表匠）通过，日期答复换行后只复跑受影响售票员双分辨率＋独立读档3项，全部通过。程序集9ccf0ced29248750a84c6f9b3e7673e6f978571ca5c622119a0bab501c69c6c9保持，最终resources.assets哈希8b113f1c575675b367e5ef10bfb58dbbaeeabb58198a2e4bcc3e7c0e42cf27cb，与首轮资源版本分记。
- 正常存档SHA adbb96bd5c40e7276ec6324e38a34bca7997f4efb2452b6d8106db3b36edf69a保持，与任务开始备份逐字节相同；review存档恢复。最终Mac已更新原桌面入口，旧应用在backup-before-clerk-dialogue。实机截图、完整对话稿、56项结果、版本／存档／范围说明与原速原声验收录像在output/demo/station-clerk-dialogue。为脚本驱动控制器／射线／UI，不是物理键鼠或盲测；未改地图或列车系统，未重跑全镇六顺序或完整跨镇往返。未提交／推送。

### NPC-06：Tripo 中年售票员、待机／对话动作与 Mac 接入（2026-10-11晚）

- 用户确认中年概念后要求用Tripo制作并替换车站售票员，加普通待机与对话两动作。CLI fetch确认main/origin/main=e079779、0/0，保留全部送奶少年／钟表匠／绿化等未提交工作，未提交推送。Chrome自动上传先因文件网址权限阻塞；用户明确授权并手动开启详情页的文件网址开关后上传成功。只开启扩展总开关无效；浏览器拒绝打开chrome://设置，未绕过拒绝。
- Tripo资产 `7ef008b1-0a11-4338-a813-c65ba9636e73`：H3.1原生成1898222三角面，重拓扑9821四边形／9272顶点，Unity Humanoid Compatible自动绑定成功。普通idle15.333秒与AI文本动作Clerk Talking to Passenger4.958秒均由Tripo导出；24800→24655积分（65＋40＋20＋20）。白底T形参考由内置image_gen转换，提示词与原始FBX/ZIP/8K基色／4K PBR保留在assets/source/station-clerk/tripo。
- prepare_tripo_station_clerk.py保留自动绑定和上身动作，统一下半身站姿、封闭循环接缝，加Jaw和局部嘴唇权重及内部口腔。首个平面口腔外露形成黑块，改为完全在头内的椭球后重新检查；ModelRoot承担身高归一化，避免原对象动画覆盖比例。最终69骨骼／9408顶点，两材质，打包源StationClerk.blend、Unity2048贴图与FBX。独立重新打开源逐三帧采样：脚漂移0、鞋底约0、首尾骨矩阵一致、Jaw最大6.846度且不牵动衣服。复现入口import_tripo_station_clerk.py与verify_tripo_station_clerk.py。
- StationClerkAsset在主镇StationTown隐藏旧Module_TicketClerk、保留旧源，原柜台后局部(-8,.12,1.9)站立朝向顾客，未移到站台门口或增添历史目击。原检票／登车说明分成两个StationClerkDialogue节点，复用统一底部界面，E／H／Esc保持，E03/E08自由查阅。StationClerkPerformance用Generic保留动作，另验证保存有效Humanoid Avatar；台词期间Talk，发言结束／回看／退出Idle，显式暂停冻结，无根运动。嘴部为通用开合，没有配音或逐音素同步。
- 最终Unity6000.3.18f1／macOS Metal／Apple M5 Pro，同一Assembly SHA256 `684e799e1f045116ea8aeef726697fa8c67842401b077cd5800d4aa240a170e4`：54/54 EditMode，六项原生全部通过：clerk1080(45.86s)、独立clerk-save(9.32s)、clerk720(45.34s)、ticket-office(38.47s)、milkboy(38.25s)、watchmaker(67.38s)。普通3m E、回看、关闭恢复、世界时间继续、暂停、两动作与嘴部通过，实际根／脚漂移0、1080 Jaw变化3.465度。已目视柜台交谈、侧面全身／脚底、面部与晴／雨／夜光照。属脚本驱动真实控制器／射线和受控特写，不是物理输入、盲测、Intel或低配验收。
- 车站复验首轮驱动误带rail-gallery，完成31个图库视点但未执行ticket-office；纠正为record-ticket-office/office-only后通过，未降低游戏断言。初轮不同程序集证据保持独立。最终截图、22.65秒待机交谈＋4.83秒嘴部原速原声录像在output/demo/station-clerk；完整日志／逐帧源在忽略的builds/macos-current/verification-station-clerk-20261011。
- 正常存档前后SHA256均 `adbb96bd5c40e7276ec6324e38a34bca7997f4efb2452b6d8106db3b36edf69a`（用户已继续游玩，不能回退旧95c7记录），review文件恢复。原唯一新版.app及桌面链接保持；Universal arm64/x86_64、严格深度签名通过；旧应用备份backup-before-station-clerk，Mac包及约49.7MB模型源包已保存。本轮未重跑全镇六顺序／完整铁路往返、未修改其系统；历史各版本证据独立。

### GREENERY-02：树木材质、七种树形与连续路边花境（2026-10-11晚）

- 用户给出两张花境照片，要求树材质/种类和画质提升、全镇路边花丛布置。CLI fetch确认main/origin/main=e079779、0/0；保留送奶少年/Tripo钟表匠等已有改动，未提交推送。范围主镇Town，邻镇车站街区未重布。
- tools/build_greenery_textures.py原创2048×1024树皮/白桦斑纹/叶脉/花瓣与法线图集；build_greenery.py输出7树形、3灌木、3草、7花、2观赏草，共22模型/51LOD。独立叶片冠替代旧球块，原灰绿/橄榄绿、暖褐与奶白紫粉花保持。Blender4.5.9 reopen验证51网格、UV/材质/尺寸/两张打包图；source-verification.json。新白桦源实际约9.2m，Unity按既有树冠r归一化高度2.7r，源尺度上限由旧9m更新为10m，不改变地图树高。
- plan_greenery.py保留217树位/树冠范围/原.13m树干碰撞；21507小植物、32段路边花境、约8477.58平方米花带、56239.23平方米草地。低白花前缘、紫粉中层、高灌木/草后层；窄树带低花，路口/门口断开，所有原道路/站台轨道/车马院/观察走廊留空。奶车在B04 gate+(-2,-.9)，在建筑排除区外；原预览发现5组穿车轮，最终后置过滤独立矩形[-147.1,10.8,-143.9,13.4]，避免重排其他花境，并加原生位置检查。
- GreeneryAuthoring同源生成Town/历史窗景，3147个16m实例批次，2档小植物/3档树；显式动态环境SH继续保留。图集法线/8倍各向异性、近档草25m/花灌45m、剔除65m/125m。BuildSlice默认1080p/内部绘制1.10，原SMAA/4K四级阴影和所有天气/曝光/光池不变。第一预览花带地面141MB，简化轮廓到15MB，模型贴图23MB；不复制实例顶点。
- 最终Unity6000.3.18f1/Metal/M5 Pro，同一Assembly `fa358da16bebcdb4f01491d629a9fae3807b42bad39c7c951e19d58a045d28ea`，PlayerData `94f8308233dfb752c5b0cd40e075d89ebcb9dddda88309110c262a9d7aa3117e`，8项原生通过：greenery1080/720、full-map六室外顺序(556.64s)、full-map-save、walnut-shop/save、milkboy-tripo、watchmaker-tripo。原217碰撞/11路扫查/实际奶店往返/指定视线/奶车净空/阴晴夜雨/角色交互保持。短时不录屏1080p平均16.67ms、P95 16.67ms；60帧上限，本机5秒视点样本不代表最低帧率、低配/Intel或盲测。
- 最终37.89秒原速原声实录/15张截图/精简证据在 output/demo/greenery-refinement；可编辑源与Mac zip在builds/macos-current。桌面唯一本地入口已更新；签名/压缩完整性通过。正常存档仍95c7a0cb4798ae63f4515648f1f2aa386a807c6f86ad9c8c33f014828f175c3a，review文件恢复；旧源/应用完整备份。此前12倍故事时间窗口失败结论保留。

### GREENERY-01：本镇绿化模型、布置与Mac实录（2026-10-11）

- 用户要求花丛、草地和树木覆盖整个地图，以少量相似但不同模型复用、保持原画风。本轮CLI fetch确认main/origin/main均e079779、0/0，保留NPC-04及待完成NPC-05工作，未提交推送。范围是主镇Town，邻镇车站街区未重布绿化。
- 原创11种模型：3树形、2灌木、3草簇、3花丛，树3档LOD/其余2档，共25网格。源 tools/build_greenery.py → assets/source/greenery/GreeneryKit.blend 与 Unity Art/Greenery。调色板已打包；Blender4.5.9重新打开验证25网格、UV/材质/尺寸，花叶朝上法线最小z=.697。首版树冠过尖、草地有条纹、花叶法线反向造成近景全黑，已改为宽短切向叶/丰满树冠、平滑随机草地色斑、向上法线，并显式传入动态环境光。
- tools/plan_greenery.py依据未改的第四版地图布置；217树位/树冠半径及原.13m树干碰撞不动。6014组小植物（4877草、308灌木、829花）、56239.23平方米草地、290地表块；留空道路/路径/门口/设施/轨道站台/车马院及指定视线。地图20角色17路线/6顺序数据检查通过；所有坐标与导出物字节未改，仅verification跨平台换行哈希刷新。compiled TownPlan哈希为原CRLF版afe327…，当前LF原文94f288…，生成器核对二者只允许换行差异并逐项比较树位。
- GreeneryAuthoring由同源生成Town及历史窗景；小植物按16m网格、资产分1543批次共享FBX，不保存每组重复顶点。曾试合并网格造成约1.1GB，已移除该任务的中间合并资产；最终地表生成资源5.7MB、模型/贴图约3.8MB。GreeneryInstances缓存矩阵、视锥/距离剔除和两档细节，并提供当前ambientProbe；保留现有天气/时间/雾/光池及阴影灯预算。
- 最终Mac Unity6000.3.18f1/Metal/M5 Pro，同一Assembly SHA256 d3413290deb2040d171a8d9959c63dd07fb13e268fba7d88c4f6b6d67598b85f，7项全部通过：greenery1080/720、full-map、full-map-save、walnut-shop、walnut-save、milkboy-tripo。完整地图556.47秒走通6种室外顺序；原树碰撞/11道路净宽/门口往返/指定视线/阴晴夜雨、两店交互与读档正常。已目视主街/住宅/车站/果园/牛奶店/广场、花叶近景、指定钟面视线及6段实录。程序控制真实角色，不是物理键鼠、盲测或低配/Intel实测。
- 约29.77秒游戏原声原速实录、截图与精简acceptance/build/save校验在 output/demo/greenery/；完整日志/逐帧源在忽略的 builds/macos-current/verification-greenery-20261011。Mac新版原入口、桌面链接保持；通用包含arm64/x86_64，签名及压缩包完整性通过。正常存档SHA256仍95c7a0cb4798ae63f4515648f1f2aa386a807c6f86ad9c8c33f014828f175c3a，review存档恢复。此前12倍剧情时间窗口未达成的事实保持。


### NPC-05：Tripo 钟表匠、三段坐姿修表与 Mac 实录（2026-10-11）

- 用户要求用 Tripo 重做钟表匠，坐着低头修表，多段克制动作轮换，替换现有角色。此前扩展文件上传阻塞后，用户手动上传并生成；本轮 Chrome 已能正常访问新资产，未修改权限。CLI fetch确认main/origin/main均e079779，0/0；保留未提交的送奶少年与绿化改动，未提交推送。
- Tripo资产373ab2a6-bf3d-43a4-9c4a-a20d63e69697：白发、眼镜/目镜、皮围裙；原生成1970956三角面，减面后10060面/9505顶点。生成后余额24840，40积分减面后24800。Humanoid自动绑定先提示不适合，再返回406；余额未减少20绑定积分。随后下载FBX/原8K基色与4K PBR，保存在assets/source/watchmaker/tripo/original；不得声称Tripo成功绑定或生成动作。
- tools/build_tripo_watchmaker.py在Blender4.5.9本地制作56骨骼、独立围裙与手部权重、手持怀表/螺丝刀，三段RepairScrew8秒、InspectMovement9秒、AdjustParts8秒，30fps。第一次围裙内腿混权出现长三角面，已改前片/裤腿平滑权重，并修正隐藏裤面与围裙的穿插；手指骨根据实际网格调整。工具尖端跟随表盘工作点。可编辑Watchmaker.blend已打包4张原纹理；Unity使用2048贴图。生成器与PIL纹理转换有tools/import_tripo_watchmaker.py复现入口。
- tools/verify_tripo_watchmaker.py重新打开源并采253个动作样本：衣服/围裙最大手指骨权重0，髋移动0，鞋底约0，坐垫支持面约0.392m（原椅面0.400m，约8mm软垫重叠）。工具与表盘标记源最大距离7.7mm（微调动作允许轻移）；修螺丝约2.14mm、检查机芯约0.028mm。Unity AvatarBuilder验证并保存有效Humanoid Avatar；实际三动作使用Generic播放保持接触，非自动重定向后效果保证。
- WatchmakerAsset.Create转入TripoWatchmakerAsset，原Meshy和CreateLegacy保留；WatchmakerPerformance洗牌轮换、相邻不重复、0.6秒混合、根运动关闭。普通对话继续修表，显式暂停冻结。原局部(2,0,2.7)、0.4m椅面、1.72m休止身高、共享ClockShop/Town、E07/原交谈/门窗保持。新髋约0.535m，禁止复用旧交叉腿0.284m偏移。
- 原生Mac Unity6000.3.18f1/Metal/Apple M5 Pro，同一最终Assembly SHA256 9c752f02f7806d4833ad5bcc6cf04a0426a8f560fe6cbae6d5bc352dcb4c4c76：6项通过，watchmaker1080(67.54s)/720(67.01s)、walnut-shop(48.87s)、walnut-save独立进程(4.81s)、milkboy(37.73s)、greenery(42.74s)。人物连续采样48秒看到3状态/6次完成，无相邻重复；根/髋不漂移、头始终低、对话射线/阅读继续/暂停冻结通过。实机工具标记最大距约11.5mm，落在直径62mm机芯内。
- 初轮低头检测误用了高于Head原点的鼻梁标记，已将headfront与头骨原点同高后重测；不是减小阈值或提高头部姿势。侧面机位原位于墙/陈设内出现黑画面，已移到可见的左侧低机位；最终手部、围裙/椅面、鞋底、背面及原交谈均已目视，影片抽帧也已查看。
- 证据/原速原声48秒全动作和约8秒手部近景在output/demo/watchmaker-tripo/；完整日志/原始逐帧源在忽略的builds/macos-current/verification-watchmaker-tripo-20261011。实际是脚本驱动真实控制器、普通交互射线与受控特写，不是物理键鼠、盲测、Intel/低配实测。本輪未重跑全地图6访问顺序；上一GREENERY-01的不同程序集证据保持独立。
- Mac唯一新版入口与桌面链接保持，更新前绿化app/普通存档备份到builds/macos-current/backup-before-watchmaker-integration。正常存档SHA256仍95c7a0cb4798ae63f4515648f1f2aa386a807c6f86ad9c8c33f014828f175c3a，review存档均恢复。深度签名校验通过；最终Mac/模型压缩包及摘要见交付verification文件。本轮仅修表，没有新增钟表匠口型/配音，三段动作的主观自然度仍可按用户反馈调整。

最后核对：2026-10-11。最新NPC-05已在Mac同一程序集完成6项验收并交付；GREENERY-01与NPC-04保留。以下NPC-04记录属于上一版本：main 已同步 e079779，Tripo v5 送奶少年、两段抬头动作、柜台前位置与店内补光已在 Mac 最终同程序集完成 9 项验收并交付；未提交推送。当前时间倍率仍为 12 倍、完整一天现实 2 小时。远端铁路/汽车/夜景/白天等既有发布结果见 PUBLISH-03/04；不能把历史平台验收写成本轮重跑。

### ART-04：成年路人基础设计图（2026-10-10，待用户Meshy生成）

- 用户要求先试一个成年路人做三个变体，本轮只先制作基础人物设计图，由用户拿去Meshy生成。参考现有送奶少年和钟表匠的真实Blender全身预览，使用内置image_gen制作约35岁成年男性：棕色毛呢外套、米白衬衫、暗橄榄领带、宽松长裤、棕色系带皮鞋；不戴帽/眼镜、不携包，以便后续变体。采用单人物近正面全身、低A姿势、浅灰背景，无多视图拼版/标签/背景道具。
- 原图、完整提示词及设计/目视核对记录保存于docs/concepts/pedestrian/round-01/。1930年男士套装参考LACMA馆藏197076，只作邻近年代依据，不指定故事国家或宣称历史服装复刻。已目视脸/头发/衣料、双手/鞋与全身构图；仅概念图，不证明Meshy网格、可拆服装、绑定或动画通过。尚未制作三个模型变体，未修改Unity/既有两人物、未提交推送。CLI fetch确认main与origin/main 0/0，无新增远端提交，本地开发改动保留。

### ART-02：车站五案概念设计（2026-10-10，用户已选B，暂不实施）

- 用户要求结合完整背景、细节和已完成建筑，先给五个完全不同的车站设计，可较大，重点氛围。本轮CLI fetch确认main/origin/main同ea1f197，无新增提交，原工作区干净。
- 已阅读原DOCX相关段落、当前MVP/E03/E08、第四版B17/角色审计及三栋成品资料和实际截图。内置image_gen生成A山墙老站、B铁与玻璃长廊、C象牙白新站、D石院回廊、E红砖货运站；每张含外观、候车厅和去顶空间示意。五张最终PNG、完整提示词、依据与局限在docs/concepts/station/round-01/。E定向删除现代绿色出口牌和小墙钟后复核。
- 用户随后明确选择B「铁与玻璃长廊」，要求先别开始做；增加可交互、会启动的列车计划，玩家上车坐下后黑屏转场到下一个城镇。其余四案保留为历史备选。尺寸/扩建仅提案，未修改地图、Unity、Blender、班次/天气事实或证据合同。B棚透视、各图窗数与家具摆位尚非严格投影一致性；后续须用共享布局深化并按改图规则复验，不能将图当模型或实机通过。现有车站宅地71×31米与主厅30×12米不是用户本轮禁止改大的限制。
- Antigravity只在scratch整理背景及草案，返回SUCCESS；主代理核对并拒绝自拟纸页事件/印章/孔形、确定无乘车结论和错误17:35站内锚点。实际图、最终判断和检查由主代理完成。未运行游戏验收，未提交推送。

### ART-03：B车站配套列车设计（2026-10-10，概念未实施）

- 用户追加「设计列车」，保持上一轮先不制作的要求。内置image_gen独立生成两张设计板：墨绿与黄铜列车外观/登车口/编组；车厢/去顶布局/靠窗坐姿与登车→坐下→发车→黑屏→抵达示意。PNG、提示词和详细计划在docs/concepts/train/round-01/。
- 建议原创1931支线蒸汽列车：2-6-0机车＋煤水车＋2客厢＋短行李车。墨绿/象牙白外观、黑铁与少量黄铜、胡桃木内饰、深绿织物座位、中央通道和两端门厅；B站棚作为实际设计参考。英国博物馆1926机车和1910开放客厢仅验证年代结构先例，不指定故事国家、真实铁路公司或历史班次。
- 交互提案：站停时实体登车门可开关，玩家步入车厢，可在出发前离开；靠窗出发座位E「坐下并出发」，就座后关门/汽笛/连杆转动与起步/窗外视差，黑屏切下一城镇，建议在同座位恢复并允许起身下车。用户确认的是上车/坐下/列车启动/黑屏跨镇；指定座位、各阶段秒数、到站恢复方式和返回方式均是待评审提案。
- 两张已目视：五单元编组、门厅/暖灯/窗景及流程可辨认。不同视图窗数、座位数量、台阶与平台高度仍待共享布局精确核对，不能从图声称碰撞/轮轨/内外一致性通过。坐姿转场不自行编造案件历史车程或证明案发日原主乘过车。下一城镇空间与往返剧情未设计。未修改模型/Unity/地图/证据合同，未运行游戏或提交推送；当前任务规模小，未再次委派Antigravity。


历史推送记录：NPC-02随 `9fc7aec`、UI-01／ROAD-01／ROAD-02随 `66fbd4c`、NPC-03按2026-10-08授权上传。历史Windows基线 `e594c3501aec1532f3ee3a656c1c0cd6c0bc94cc`，用户确认Mac可运行见 PLATFORM-01；后续实际上传以Git历史和远端为准。

## 进入项目时必须执行

1. 确认当前目录和 Git 状态。工作区的有效仓库是 `game_test/`，远端为 `https://github.com/xichengchen1111-hub/game_test.git`；`detective-demo/` 只是旧参考，未经用户要求不要修改。不要依赖本机绝对路径寻找项目。
2. 阅读仓库根目录 `AGENTS.md`、本文、[当前 MVP](mvp-demo.md)，再按任务阅读 `skills/` 下对应技能。用户最新指令优先；旧企划不能覆盖后来明确的玩法和画风约定。
3. 先辨认已实现与计划：第四版410×277米全镇外景已接入，钟楼、钟表铺、牛奶屋为3个完成建筑，其余67处保留占位；仍属案件玩法切片。完整案件、车站/住宅室内、全部人物历史行程与完整推理报告闭环尚未完成。
4. 修改场景前找到生成源。`BuildSlice.Build` 会重新生成 Prefab 和场景；不能只在生成后的 `.unity` 文件里补一个位置，下一次构建会覆盖它。先检查是否存在手工修改，保留稳定 `.meta` GUID。
5. 修复后按本文复验受影响的行为，记录真实输出及限制，并更新相应条目。编译通过、概念图或旧截图都不能证明新版本可以玩。
6. **Git 操作统一用命令行**：2026-10-08 用户明确要求后续不再打开 GitHub Desktop。每个新任务先 `git fetch origin` 核对远端；保留本地工作后再同步，只有用户授权才提交或推送。此条取代此前通过 Desktop 同步的约定。

## 已确认的长期约束

| 事项 | 必须遵守 |
| --- | --- |
| 游戏空间 | 第一人称、完整可行走的三维空间。最新 A 钟表铺在 Town 同一建筑内实体门直接进出，无黑屏转场；其他未制作室内仍保留原计划。横版、固定机位和静态图片不能代替游戏空间。 |
| 画风与成本 | 环境保留简化低模、宽色块、灰蓝/紫灰暗部和暖黄室内光，1931 年道具保持时代一致。送奶少年以用户 2026-10-11 指定 Tripo v5 为准，双手抓肩带待机、带嘴部动作交谈，两者基本抬头看人；原资源保留。钟表铺老板采用用户指定的 Tripo 新外观，本地绑定三段坐着低头修表动作，保留原木椅与旧 Meshy 源。 |
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


### TOWER-06：外挑山墙钟室、大钟盘与五档调时（2026-10-09）

- **用户要求**：地面读钟困难，四面钟盘加大，顶部按所附 Timber Gable 图外挑；齿轮可交互，−30/+30分钟、−1/+1小时和Reset五项，Reset回到当前怀表+30分钟。原上一次push已将TOWER-05及之前内容推至main的0535556；本次未获新的提交/推送要求，不自动推送这些后续改动。
- **实现**：共享 `ClockTowerInteriorLayout` 保留20米、3.6米塔身与原三层高程；四面钟盘2.2→3.4米、钟心16.7米、钟面距轴2.46米，外挑钟室宽4.8米、内净4.25米、墙顶18.5米。`build_exterior.py` 根据用户图制作木柱、托架、木纹、铜木钟框与四向山墙瓦顶；源图为 `reference-timber-gable.jpg`，本地建模，没有外部上传或神经推理。内墙、机房地板、顶板、蛛网锚点和上层有效存档范围同步扩大；92旋梯段、101行走点、87鞋印与尘粒限制保持。机芯移至共享X=-1.72，留出侧面通路。
- **操作/保存**：`ClockTowerAdjustment` 复用机架真实碰撞体和普通E射线。`DemoApp.TowerAdjustment.cs` 五个鼠标/数字键控件共用处理函数，Esc关闭；面板不显示绝对时间或默认钟差，不授予证物。`towerAdjustmentMinutes` 独立存档，旧档缺字段为0；四面 `ClockDial` 使用当前怀表时钟+默认30+玩家调整，调整不改变怀表累计时间或历史证言，Reset仅将玩家调整清零。跨整日正负累加后仍按钟面循环。
- **故障与修复**：新木纹法线首次导入会刷新实例，可能丢失先前的FBX朝向修正；材质/贴图预处理必须在实例化及朝向校正之前。不要删掉四面钟与外挑尺寸检查来绕过失败。最初保留机芯旧X位置挤住加宽通道，真实控制器复验发现并修复为靠新墙布置；保留走入外挑部分再返回的额外检查点。JSON保存的double存在极小小数舍入，读档测试须像既有时间检查一样使用1e-9容差，不以浮点完全相等误报；内存中调时期间怀表不变仍用精确检查。
- **实际结果**：最终源模型回开/FBX回导通过，7渲染组、217163三角面、30门洞射线、20米边界和3.41米钟框净径；18项布局静态约束通过。正式Mac构建 `BUILDING_LAYOUT_PASS` / `SLICE_BUILD_PASS`；七项实际播放器检查通过：time、time-relaunch、tower-1080、tower-relaunch、tower-720、middle-save、shop。全部五控件、连续正负跨日、四面针角、怀表/证物不变、偏移落盘、+90分钟差的新进程恢复、Reset回+30、上下101点、真实扩大机房通路、退出恢复行走均通过；720p/1080p画面已查看。时间两项在最终仅修改验收小数比较前通过，其游戏代码未改。保留表面8档、87脚印和稀疏动尘，未增加HUD。
- **交付**：`output/demo/clocktower-adjustment/` 保存精简截图、结果、独立公平性源码审查与构建清单；完整日志位于忽略的 `builds/clocktower-adjustment/`。Mac通用二进制arm64+x86_64、严格签名和90.4MiB ZIP完整性通过，ZIP SHA256 `56ca305162c8a9814deb80c647c5eadba626ffaf003a0201c6bdeb1306134ee0`。原应用备份至 `backup-before-clocktower-adjustment-20261009-175138`，桌面原入口已更新并通过原生窗口观察确认正常标题菜单；SAVE_LOCATION仍是原正常路径，正常存档启动前后哈希一致，review档恢复。
- **边界**：调钟后的读数不能证明此前或案发时的钟差，原E01/E04/E05推理条件不变。程序驱动真实控制器/射线与共用UI处理函数，非物理键鼠/陌生玩家盲测，未做Intel实机或完整MVP六顺序。保持地面地图/门洞和其他NPC状态，不能把这次上层扩大记作第四版全镇地图迁移。

### TOWER-07：真实磨砂钟盘、透明调时面板与机械快转锁定（2026-10-09）

- 用户要求对照四张实景截图：UI更透明，内部可看乳白模糊外景，内外钟针实时同步；调时齿轮快转几圈后恢复慢速，期间不能再次操作。本轮fetch确认远端无新提交，保留TOWER-06本地未提交内容，未提交/推送。
- `ClockTowerGlazing`、外壳构建器与内衬共同使用共享尺寸生成四个3.4米真圆孔，取代旧实墙/遮挡面；保留玻璃防跌体。立体罗马数字、环和支撑条两侧共用，背面自然镜像。玻璃用URP当前相机opaque/depth采样做乳白散射与9点模糊，静态参考图片不参与运行时钟盘；四个ClockDial的实际针同时从内外可见。近距离背面针为浅灰透光影，远处街景轮廓淡化，外面针保持清晰。
- `DemoApp.TowerAdjustment`底色alpha0.38/按钮0.27，文字阴影；3秒平滑加速/匀速/减速，约3圈以上主轮转动，负向调时反向。针偏移按同一速度曲线积分插值，忙碌期间锁全部按钮和数字键，收起重开不能绕过，Esc仍可退出。暂停/其他检查冻结，回机芯继续；普通机芯查看不冻结齿轮。目标调整量先落盘，半途中退出重启恢复完整目标，怀表时间独立。
- 真实源模型回开/FBX回导、36钟洞/30门洞射线通过，7组/221323三角面/20米；Mac构建通过。time、time-relaunch、tower-1080、tower-relaunch、tower-720、shop、middle-save七项全部退出0。专项断言实际主轮累计转角>720度、峰速>100倍、结束恢复1倍、帧间四面针角同源、忙时五操作拒绝、暂停与重开不绕过；五控件及正负跨日、独立重启Reset和101点上下楼通过。720p/1080p已目视：磨砂后实针影、淡外景、立体背字和透明按钮文字可读。
- 独立公平性源码审查在新证据目录；不显示数字钟差、不改怀表/历史证词、不授予证物，玩家调钟后的读数不证明之前偏差。程序驱动真实控制器/射线/共用处理函数，不声称物理键鼠、Intel实机或盲测。未迁移完整第四版地图。
- 交付证据 `output/demo/clocktower-glass/`，完整日志/脚本 `builds/clocktower-glass/`。通用架构/严格签名/ZIP通过，ZIP SHA256 `69de0c96bc06a7f88c41ebebf2208ebb2c9a5a2ffea3f7e3dace794e53d24ad8`；原应用备份 `/Users/wulizhi/Documents/GitHub/game_test/builds/macos-current/backup-before-clocktower-glass-20261009-183832`，正式存档哈希 `35f27eca266062b74d8bb62ae9923214617167839af82e289f456df4f65738c9`，部署时保持字节一致。正常入口启动确认另记于下条。
- 正常入口启动后：原生CUA窗口确认标题菜单及用户原机房位置；normal-launch.log的SAVE_LOCATION指向原slice-save.json，启动前后上述SHA256相同。未点击开始游戏改变正式进度。地图合同重算/复验与剧情数据校验通过（数据验收不冒充玩法验收）。

### TOWER-08：加强材质与画质（2026-10-09）

- 用户要求材质质感更强、提高画质。本轮fetch无远端新提交，保留此前本地改动，不提交推送。按Unity/Blender/实机技能读取当前资料；本次仅Unity材质/UV和渲染，不修改外部模型几何，因此不重复Blender源模型回导。
- 新 `ClockTowerMaterialFinish`：1024px橡木/加工金属base、normal及metallic/smoothness map，铜轮/铁架米制投影UV与切线；内墙/底层石板512→1024且保留米制砖尺寸、加强凹凸，外壳已有normal强度0.6→0.95。木踏面使用独立明亮灰膜木纹，法线随高度0.60→0.18衰减，避免深木色吃掉上层鞋印；同87脚印、8档灰尘数量/位置不变。磨砂玻璃加微细颗粒，不改外景采样/实体针。
- 机房新增256px局部反射探针，仅OnAwake渲染并分帧，范围4.4×3.8×4.4；既有机房维护灯启用一盏低分辨率软阴影。全局SMAA High取代FXAA，主光2048→4096/四级阴影，强制各向异性过滤，Bloom0.25→0.18、轻微对比5与饱和-6。未承诺真正光追或电影级GI。
- 收尾修正：URP忽略Light.shadowResolution时新增点光默认High挤占店铺阴影图集，改为显式AdditionalLightsShadowResolutionTierLow（256px），不再自动降级。旧店铺视差验收从名义目标横移导致真实轨迹含向窗运动，右窗投影抵消仅0.000749；改从实际落点沿相机右向真实横移0.4米，保留0.001视差阈值、增加>0.2米真实移动断言并在渲染完成后采样，不改窗体/通路。最终阴影限额和验收路径修订后复跑shop、middle-save、tower-1080；时间/重启/720沿用本轮相同材质和游戏逻辑的前一候选构建结果。
- 最终构建、七项实际播放器检查（时间/重启、两档完整上楼、机房重启Reset下楼、钟表铺、中层存档）通过；截图目视对比材质与脚印/玻璃/界面。`RenderedFrameReview`仅review参数启用，启动3秒后记录全部帧（包含抓图停顿/UI），不是无负载基准或长时间性能保证。实际数据见新证据frame-timing.json。独立源码公平性检查确认未改剧情/调时/碰撞/HUD。
- 证据 `output/demo/clocktower-quality/`；日志/脚本 `builds/clocktower-quality/`。Mac通用构建、签名/ZIP检查通过。原应用备份 /Users/wulizhi/Documents/GitHub/game_test/builds/macos-current/backup-before-clocktower-quality-20261009-190125，部署时正常存档SHA256 35f27eca266062b74d8bb62ae9923214617167839af82e289f456df4f65738c9 不变。正常窗口启动结果另记下条。
- 正常启动日志确认原slice-save.json路径，启动前后正常档SHA256不变，无运行异常。原生CUA观察时Mac已锁屏且自动解锁失败，未目视本轮正常菜单；不得把review截图当成正常菜单已确认。应用已通过原入口启动，需用户自行解锁查看。剧情合同与地图哈希复验通过。

- 用户随后明确授权推送本轮TOWER-06至TOWER-08的累计改动到GitHub；提交包含游戏源码、模型/材质、共享场景、文档与精简实机证据。Mac应用/压缩包、构建缓存及正式存档继续留在本地。推送前fetch确认origin/main无新增提交，沿用上述已完成实机结果，不重复无变更测试。
### MILKSHOP-03：牛奶屋灯光与柜台送奶订单设计第一轮（2026-10-09，待选择）

- **用户范围**：现有牛奶屋没有好看的室内灯、氛围不足且无可交互内容，要求按照剧本先出设计图；补充柜台放订单，玩家可查看几月几号送了哪一家店。本轮没有授权直接按某张图替换模型或接入新证物。
- **基线与依据**：main/origin/main同为0535556，无新增远端提交。查看实际牛奶屋内外及透明窗截图、建模源；读原修订稿地点C、现行MVP、E04/E05/E06和送奶对白。保留黑白木墙、后柜台、奶瓶架、单层与真实两侧窗，新增灯具和柜台阅读区。原稿空奶罐/派送记录可作设计依据，挂钟不能擅自增加新的准确校时源。
- **交付**：`docs/concepts/milk-shop/round-01/`内A奶白暖灯、B墨绿工坊、C铜灯暮色三张PNG，每张包含室内、灯具、柜台交互和纸面订单查看示意。内置image_gen独立三次生成；C一次定向去掉未确认的国家标签，保留青川镇。提示词在generation-prompts.json，完整交互提案/剧情边界/修图提示在design-plan.json。已人工查看全部图；建议A，等待用户选择。
- **订单约定与边界**：主物件在柜台前沿，以日期页和月份索引翻查配送日期、送达店家、数量、确认状态；只有实际送达确认才写已送，预订与送达不能混淆。历史条目不随当前游戏日期改写。图中19日钟表铺2瓶/20日旅馆3瓶明确是视觉样例，不是新正史。未移动E05送奶车上的原簿，未改妻子家前后两户独立签收，也未新增精确到达时刻或推理答案。若后续合并到E05，须先明确原件/副本来源并同步合同。
- **后续与限制**：可选次要互动为阅读灯开关、看瓶签/空罐、按服务铃；不自动增加NPC、线索卡或解锁条件。生成图窗比例和各视角陈设仍有差异，建模以既有7.2×5.8m及原门窗/柜台尺寸校准。低配目标拟用少量真实灯光配发光/烘焙，不以图中灯具数量承诺性能。仅新增概念资料和记忆，未改Unity/模型/证物合同，未运行新玩法或性能验收，未提交推送。

### MILKSHOP-04：A 奶白暖灯实施与两侧窗边闪烁修复（2026-10-09）

- **最新授权**：用户选择 A，只改内部细节；明确沿用图中 1 月 19 日钟表铺 2 瓶、1 月 20 日旅馆 3 瓶作为日常背景，均已送。制作中补充两侧窗边仍闪烁，已确认是牛奶屋而非钟表铺。此条更新 MILKSHOP-03 的待选择及样例未确认状态；不恢复此前已放弃的本地钟楼工作。
- **同步基线**：本轮开始 CLI fetch 后 main/origin/main 同为 0535556，0/0，无新远端改动；已有概念资料和记忆改动保留。未提交或推送。
- **可编辑模型**：独立 `assets/source/milkshop/interior-a/MilkShopInteriorA.blend` / `game/Assets/Art/MilkShopInteriorA/`，10 可见网格、7,208 三角面、8 材质，明确 UV/米制标记和书、灯、铃原点。两盏奶白搪瓷吊灯、铜颈台灯、绿布面订单簿与木矮架、柜台铃、瓶签、盆栽/文具、服务板及薄垫；吊灯固定木条跨接原屋架，长吊线连到实际底座，没有悬空安装盘。参考原屋集合仅在 .blend，排除 FBX。保存后重新打开并检查、渲染，实际本机 Blender 5.2.0 LTS。
- **接入与交互**：`MilkShopInteriorAsset` 在原 `MilkShopAsset.Create` 校验后加入，原体量/点位/柜台/奶瓶/秤/货架/门洞及通道保持；灯光 3 盏无实时阴影，5 个简单交互碰撞。`MilkDeliveryOrders.json` 是固定历史数据，查看不授予 E05、不迁移送奶车证物、不添加地址/具体时刻或推理答案。`DemoApp.MilkOrders.cs` 日期/月索引、上下页界限、Esc关闭，加入 Blocked 同时暂停移动、视角和游戏时钟；选中日期和阅读灯关闭保存在兼容的新增字段/原 openFixtures 列表。柜台铃只播放自制短音，不召唤 NPC。补进屋地点显示。
- **窗边原因与修复**：原立框内侧恰好与木墙窗洞切面共面，中央横/竖档也有同深度重叠。`assets/source/milkshop/tools/window_joinery.py` 统一生成修复窗框；覆盖切面 5cm、墙两面各突出 7cm，横档两段对接竖档，8 片玻璃各只有单层双面 quad，保留 ClearGlass 与原真实墙孔和碰撞。`tools/milkshop/fix_window_joinery.py` 只替换原 Windows，再保存/重开，原其余 11 可见网格及全部碰撞代理几何哈希逐一相同，证据 `assets/source/milkshop/verification/window-joinery-repair.json`。主 .blend/FBX 因修窗更新，不能继续宣称整文件哈希未变。原 52,388→52,396 三角面；备份位于被忽略的 `builds/review/milk-interior-a/before-window-fix/`。
- **构建与验收**：Unity 6000.3.18f1 / URP 17.0.1 / Windows D3D11。最终 `unity-build-window-fix.log` 有 SLICE_BUILD_PASS、BUILDING_LAYOUT_PASS、MILK_INTERIOR_IMPORT_PASS。最后双分辨率 1080p 22.53s、720p 22.02s、独立奶屋读档 4.74s 均退出0/PASS；5正常3m交互射线、日期/数量/边界、阅读移动/时间冻结、台灯/铃、无额外证物、两窗物理阻挡和无转场进出通过。当前点位/尺度保持，最小建筑间隔 .8844974m。
- **动态与保存**：两窗各真实横向移动约 .94m、13 帧已逐帧查看，普通灯光/阴影/AO下未再见原窗边争抢；两段10Hz回放和原始指标在 `output/demo/milkshop-interior-a/`，采样不是游戏 FPS。跟踪窗框亮度范围约 .031（含视角高光/木纹变化），没有旧指标比较，不能称全场/所有显卡彻底不闪。独立进程恢复室内位置、灯关、1月19页并正常打开/走出；专用 review 档与正常档隔离，正常档 SHA256 前后均 FDE6BCE28F10294A989DC5052A88111F08855E624BC756BEFD3A1BB326B58839。
- **共用回归与限制**：A内饰构建的 time、time-relaunch、shop 三项均通过，随后最终只改奶屋窗网格，已重跑受影响奶屋三项和动态窗检查。保持塔钟比 Q 表快30分钟、连续7s/10min和原钟表屋物理门/窗/往返。详细证据 `output/demo/milkshop-interior-a/README.md`，日志/原生动态帧在 `builds/review/milk-interior-a/`。agy仅起草纸面UI，最终已简化并移除草稿的额外地址/晨间派送描述，由主代理复核。程序驱动真实播放器/控制器和按钮处理函数，不是物理键鼠、盲测或真实低配性能验收；Mac应用未在Windows重建。共享生成产物已配套更新；还原了重建引入的无关钟楼材质/法线末位浮点差异，保留当前原钟楼。

### DAYNIGHT-01：共用时钟的昼夜、按采光熄灯与夜间可读性（2026-10-09）

- **用户要求与同步**：现有时间加入昼夜；白天关闭路灯，室内够亮时关灯，检查光影。任务开始命令行 fetch origin/main 成功，HEAD/origin/main 均 0535556，无新增提交。保留牛奶屋 A、窗框修复及所有既有未提交资产，没有提交推送。
- **实现**：`DayNightCycle` 只读准确怀表 `DemoApp.Minutes`，保留初始17:55、7秒/10分钟、累计存档、无离线推进与塔钟+30分钟。连续天空/日月/三色环境光/雾，单盏方向光在地平线零强度附近改方向。美术日出/落轨道06:00/18:00，不是冬季当地天文测量。`DuskSky` 增日/月圆盘和固定星点，不新增全天候第二盏阴影方向光。
- **灯具规则**：生成源 `DayNightLighting` 显式绑定 `DayNightLamp` 光源和发光材质。6路灯+2店外壁灯及背景暖窗白天熄灭；牛奶屋3灯、钟表铺前堂2灯有足够采光时关闭。维修台、后部照明和钟楼5盏维护灯按较低自然采光系数保留；第6盏塔内冷填光是独立自然窗口角色。采光系数依窗洞/房间深度校准，不是实时lux或遮挡射线，只用自然亮度避免灯光自身导致开关反馈。连续曲线没有反复阈值触发。使用逐材质slot属性块，亮灯与灯泡发光一致；未变化的表面不反复写入，暂停时不反复更新环境。
- **手动与保存**：牛奶屋 `MilkShopFixture` 通过 `scheduledLamp.SetRequested` 控制原 off ID。手动停用优先，入夜不反开；逻辑允许亮灯不代表白天实际开灯。订单所选日期、历史两条记录、证物、门状态保持。昼夜不新增独立计时存档字段。
- **边界修正**：Town additive加载时 Awake 可能读到Systems的天空；因此在共用生成器序列化skyTemplate、运行时克隆，并在active-scene改变后恢复拥有的材质和sun引用。测试截图只冻结review时钟，不能用暂停菜单遮住画面。初轮夜塔偏暗，调整夜间基础环境光；四个塔钟加微弱纹理加权夜间补亮，保留圆形alpha裁切，无黄色方块／发光矩形、无新增光源。牛奶屋壁面的大黑块核对为原奶牛花纹贴图，未以“旧阴影”改掉。
- **最终实机**：Unity6000.3.18f1、URP17.0.1、Windows D3D11/RTX5060Laptop。最终 `unity-build.log` BUILDING_LAYOUT_PASS/SLICE_BUILD_PASS，230400043 bytes；`output/demo/day-night/acceptance.json` 11项全部PASS：昼夜1080p42.89s/720p42.55s/独立读档2.46s、牛奶屋21.83s/读档4.60s、时钟19.56s/读档3.57s、钟楼85.00s/午夜钟楼84.96s/上层读档下楼54.99s、钟表铺54.15s。本轮同一最终构建，非只编译或沿用旧照片。附托管程序集SHA256与实际分辨率状态记录。
- **光影与窗口**：完整1440分钟曲线与实际时钟跨日出/日落/午夜、真实UI暂停、全部灯光/发光一致、昼间熄灯/暗房保灯/手动关闭不反开、新进程恢复都通过；正常AO及全部阴影保持。阴影预算仍单方向灯+2铺内点光，奶屋/钟楼阴影灯未新增，不做逐帧GI/反射探针/房间射线。两分辨率北窗正午、南窗深夜均真实横移约.94m、13帧10Hz逐帧查看，窗框亮度范围.017–.019（含木纹/高光变化），没有观察到新窗边争抢；不是游戏FPS或全硬件不闪保证。夜钟楼实际走满101路点，87鞋印、砖纹、机房和蛛网保持可读。
- **交付与范围**：Windows原入口 `builds/windows/TownDemo.exe` 重建，比较图、原生PNG、MP4、状态和夜塔画面在 `output/demo/day-night/`；原始窗边采样帧及完整回归截图本地保留、Git忽略。正式save前后SHA256仍 FDE6BCE28F10294A989DC5052A88111F08855E624BC756BEFD3A1BB326B58839。程序驱动实际控制器/普通3m射线及UI处理器，不是物理键鼠或盲测；没有低配硬件帧率基准、Mac重建或全MVP六路线。现有低配优先约定保留，不从宿主功能测试宣称低配60fps。
- **回归入口**：`--review --fresh --review-day-night` 两分辨率，随后独立进程 `--review --verify-day-night-save`；正常牛奶屋/time/shop/tower模式，加 `--review --fresh --review-clocktower --night-illumination`。未来新增房屋在共享源绑定采光/灯泡，不能扫描任意名字每帧或让各灯独立计时。尤其保留天空additive初始化、手动优先、暂停同步和夜间关键钟面可读性。


## WEATHER-01｜分层天空、天气渐变与屋内遮雨（2026-10-09）

用户在昼夜系统后要求精美天空、云和合理天气，并指定参考优秀游戏的氛围。开工 fetch origin/main 无新增提交，保留全部牛奶屋／昼夜未提交工作，未恢复被弃用的本地钟楼 stash、未改第四版地图或模型／剧情事实。

实现：`TownWeather` 从同一准确怀表的累计游戏分钟决定晴／多云／阴／雨／雾，12 段×210 游戏分钟，前55%保持、后45%Smooth渐变。雨门控在覆盖率65%到100%渐增，无电脑墙钟、离线推进或额外天气存档。`DuskSky.shader` 保留原 GUID/名字，使用生成的256px linear/readable/Repeat/trilinear/mipped RGB 纹理（分形团块、形变、Worley云缘）与两层钟控相位，加入云底／云边透光、太阳方向暖低空、星点、月面月晕；`DayNightCycle` CPU只采太阳／月亮方向的3个同密度样本，使同一方向光的强度／阴影柔和度随云遮蔽变化。继续采用唯一星体方向光与既有两盏阴影店灯，无新GI、反射或体积射线步进。

`WeatherEffects` 单网格256雨丝／1024顶点／512三角形，128近处、128远处，柱列固定世界XZ，范围边缘渐隐，竖直相位也来自累计时钟。`WeatherAuthoring` 在共享屋顶／屋檐源绑定 `WeatherShelter`，缓存静止变换，整根雨丝（两边及倾斜尾端）查询最高遮蔽面，无逐滴物理／射线。室内雨声减小，查看材料或暂停时云、天气、雨与雨声同步暂停；`Silent` 是陪伴者语音关闭而非全部环境音静音。灯控制区分天文学Daylight（室外）与天气自然光（室内／窗光），手动Off意愿始终保留。

重要修复：首轮只查询雨丝中心导致屋檐边缘尾端漏雨，真实屋顶顶点断言抓到后扩为完整范围；浮动路面截止改到Town地面；细化云缘并压低厚云局部反差、加入完整阴雨云幕，消除明显蓝洞和强太阳；雨覆盖门控0.65→0.90过陡，0.25游戏分钟最大变化0.00855，扩展到0.65→1.00后最大0.00688，保留<0.008检查；柱列跟镜头滑动改为世界锚定与真实控制器视差断言。隐藏原生播放器得到全黑帧，未作为验收；最终原生预览正常渲染。

最终 Windows 构建230694075 bytes；Assembly-CSharp SHA256 `ff0c6e14d487508817f91eaf54d209defc7fff9c08ab5f6be6dd9796e9b48fa8`。同一最终程序集12项均通过：weather-1080 21.99s；weather-720 21.84s；weather-relaunch 2.38s；day 42.44s；day-relaunch 2.39s；milk 21.81s；time 19.44s；tower-night 85.02s；shop 54.11s；milk-relaunch 4.79s；time-relaunch 3.55s；tower-relaunch 54.94s。天气720p/1080p都覆盖5种天气、日出／日落／月亮、自动实时时钟变化、方向光一致、全部可见雨顶点屋顶约束、世界雨视差、晴天室内关灯／雨天补灯、白天路灯Off、普通订单UI冻结和独立进程天气相位恢复。正常AO与阴影，钟楼夜间完整真实阶梯／读档下降、钟表铺门口往返、原灯和订单交互均复验；没有新增阴影灯、Shader错误或atlas缩放警告。

`output/demo/weather/sky-weather-comparison.jpg` 为最终6帧原图缩小加文字，不改色；两分辨率两侧雨窗各13帧、约0.938m真实横移，四个10Hz采样MP4和motion.json在对应目录，亮度范围0.02985–0.03226，逐帧未观察到新增突闪，范围含贴图／镜面变化，不能称为全部硬件无闪烁。`acceptance.json`12项与Save隔离证明，README记录复现与范围；当前设备RTX5060Laptop/D3D11，不是低配FPS或Mac验证，也不是物理键鼠盲测。普通slice-save.json SHA256 `FDE6BCE28F10294A989DC5052A88111F08855E624BC756BEFD3A1BB326B58839` 与修改前完全一致。

Antigravity在本地scratch做只读架构初稿；首调用读权限未授权，按用户AGENTS规定在scratch重试--dangerously-skip-permissions成功。主代理核对采纳地面高度，驳回忽略ValidElapsed保护的负值越界、误解Silent为全部音频以及忽略塔内WindowDaylight绑定的建议，实际实现／最终判断／Unity渲染由主代理完成。源纹理generator与技能已同步。没有commit/push。后续改密度参数需同步CPU和GPU的相同云量函数；改建筑或移动屋顶需更新遮蔽体，不能靠降低Alpha或关阴影隐藏穿雨。

### MAP-03：第四版全镇外景接入、全部成品重排与兼容存档（2026-10-09）

- **授权与同步**：用户要求pull合并最新，按现有地图重排全部成品并扩展街道等元素，未制作部分保持占位。原HEAD0535556，本地牛奶屋A／昼夜／天气全部用 `full-map-sync-preserved-local-assets` stash保存（SHA `b6e242cf79335f20ea1e71d2db2632f6a6229fe7`），ff-only拉取08ff02a后apply并解决冲突；保留远端真实磨砂钟盘、扩室／调时／机房256阴影灯及本地工作，未恢复被用户弃用的本地旧塔。保存stash仍保留，未commit/push。
- **坐标来源**：`docs/maps/town-current/plan-data.json`，东+X北+Z、塔0,0，边界X[-170,240]／Z[-155,122]。B02保持7.4×8.2m，中心(-35,31.8)、南门(-35,27.7)；B04生产宅地中原比例奶屋约(-144.094,-.04,7.21)、yaw180、.9scale，门(-143.5,10)。原树偏到净人行道，改到正确绿带并避开路径，217树；全部JSON/SVG/HTML/CSV/Tiled源导出同步。
- **共用生成**：`tools/build_full_town.py`输出JsonUtility平数组TownPlan、原创不透明立面和树卡片。70宅地／3成品／67占位；大宅地用院落和附属占位体块包围奶屋，不拉伸成品。道路／人行道／路缘／院落先做互不叠铺的多边形，再48m分块，147块／1618三角面，有实体地面碰撞、建筑留孔、米制UV。屋顶资产名包含宅地ID，修正原模板朝内面序；否则重名覆盖宽度和背面剔除会让多个屋顶变形／消失。
- **布置与预算**：老板、送奶少年、奶车、8长椅、15盆栽、招牌和原交互迁入新点位；少年实际模型正面是root−Z，yaw180朝北街，复验从head-front骨骼算接近点并核验dot>.9，不能假设root.forward。123沿街灯＋4广场灯，室外最多12近处点光，远灯泡保持发光；全图3局部阴影点灯＋唯一星体方向光。所有屋顶绑定雨遮蔽16m静态空间哈希，与穷举结果对照；雨预算256不变。主相机far360，总览相机700且临时关雾仅核对布局。
- **保存与验收**：worldRevision3转换旧塔／店／奶屋位置，旧街道安全回广场，缺字段旧JSON读0；保留证物、时钟、灯关、日期、对话与门。整图原生1080p真实行走810.19s，6种车站／住宅／钟表铺**室外**顺序不在各路段内传送，11道路双侧净人行道3条胶囊扫查／实际行走、指定厨房／东门／塔钟通视、日间熄灯／夜间12光源、雨中奶屋进出及新进程室内恢复通过。
- **数据／范围**：地图70宅地20角色17路线／网页390px和17:53–58同期视图0错误；本镇去站返家639.15m、8.88分钟，11.12分钟尚未分配；签收两户纯走2.86分钟，仍不自动填入停留事实。剧情合同10EID／D02D03／6顺序设计通过。车站／住宅室内、NPC历史行程与煤车转弯未实现，不能把代理射线扩大成全视点目击事实，也不能称6个完整案件游玩。
- **最终交付**：WindowsUnity6000.3.18f1／URP17.0.1／RTX5060LaptopD3D11，Unity报告296391485字节／目录全部文件296586408字节（不是exe单文件），最终程序集 `382C16AC99B26F0455BE57FF585EE2C9E97B1CA8FDA39A33656FB9584662CC40`。22项各自PASS／时长／日志和程序集哈希见 `output/demo/full-map/acceptance.json`；检查分4阶段，最后仅奶屋门框受影响5项在最终程序集复验，未改变检查保留早期确切哈希。正常存档SHA256仍 `FDE6BCE28F10294A989DC5052A88111F08855E624BC756BEFD3A1BB326B58839`。程序驱动真实控制器／普通射线，非键鼠盲测、Mac或低配FPS；结构预算不能证明低配60fps。
- **验收脚本问题**：第一次整图恢复检查走出后自动存屋外，再重复期待屋内会失败；重新用实际原生奶屋验收保存屋内检查点，原样复制到整图专用路径再由独立进程恢复／走出，来源哈希见indoor-checkpoint-provenance，未改正常档。PowerShell重建结果时未枚举JSON数组漏掉6条已PASS记录，已从完整快照恢复并逐一核验现有日志；原失败日志留存，不伪称通过。Antigravity只在scratch做只读坐标初审；主代理复核并拒绝编号连续、固定雾距离／未测draw-call结论。

### TOWER-09：机房地板／墙顶共面闪烁定点修复（2026-10-09）

- 用户在全镇迁移验收中确认最上层机房地板／墙面闪烁。机房平台和窄井壁上盖都在15m，进口外壳也有该高度的盖面。`ClockTowerSurfaceChecks.Repair`按真实平台三角形裁剪覆盖，去除完全被平台覆盖且朝上的24个渲染三角面（外壳12／内壁12），拒绝部分覆盖；侧墙、地板、UV／材质、源FBX与全部碰撞引用保持。初次审计150面混入从上方被背面剔除的支脚／栏杆底面，不将其当作可见争抢面删除。
- 每次共享构建产生独立SurfaceMeshes，`tower-surface-separation.json`报告24移除／0剩余／collisionUnchanged。未通过关AO／阴影或深度偏置遮掩；保留远端08ff02a的磨砂表盘与机房光。
- `--record-tower-surfaces`真实平台横移约.94m，720p正常黄昏／1080p夜间各拍地板和墙13帧，原生路线126.75s／126.59s，独立进程上下楼61.86s／61.73s均PASS，包括五档调时与控制锁定。EB0B05C53E1F7A46F3F1C771993A046CAA87BCEE2EEF0B1A192AB598A8431F14阶段程序集已核对；随后奶屋专修不涉及塔。10Hz视频非游戏FPS，逐帧未见原共面争抢，不称全硬件不闪。

### MILKSHOP-05：屋内看出的门框上角共面闪烁（2026-10-09）

- 用户追加小问题并明确“屋内看出来的上角”。原门楣内面与前墙内面都为front+.08，立柱止于2.90m又与横梁2.73–2.91m穿插。共用 `assets/source/milkshop/tools/door_joinery.py`让内面突出墙8cm，保留外侧面／门洞宽度，立柱顶2.727m、横梁底2.730m，以3mm缝对接。
- 一次性 `tools/milkshop/fix_door_joinery.py`从原Trim识别并只替换3块门框；其他Trim面／UV／材质签名、所有非Trim网格、窗框与全部碰撞哈希相同，保存重开后核对。原备份在 `builds/review/full-map/before-milk-door-fix/`；证明 `assets/source/milkshop/verification/door-joinery-repair.json`，新blend SHA256 `22748874fb6ee053ef48be01159298c618f5f3584331c6cc98b1a9d8a6929234`。FBX／blend因定点修复而变化，MILKSHOP-04历史非窗Trim不变证明不能继续指代最终模型；门扇／窗及门洞碰撞未改变。
- 最终构建明确SLICE_BUILD_PASS／MILK_INTERIOR_IMPORT_PASS／CLOCKTOWER_SURFACE_SEPARATION_PASS，新增 `--record-milk-doorframe` 为真实横移约.94m、左右上角各13帧。720p27.55s含两侧窗移动复验／1080p22.76s，奶屋独立读档5.90s／5.48s，整图新屋内检查点独立恢复5.39s全部PASS，最终程序集同MAP-03。
- 普通AO／阴影、材质纹理保持，源受控Blender渲染与Unity画面分开；四个门角序列逐帧看未再见原共面闪烁，包含黑白奶牛纹的墙面不是异常阴影。10段奶窗／门角／塔顶完整画面采样视频与接触表在full-map；亮度包含木纹／光影变化，没有采用万能无闪烁阈值。原5交互、普通射线、订单两日期／数量、阅读灯／铃／时间冻结和开关门持续有效。正常存档保持，无提交推送。

### SYNC-01：用户授权上传全镇与累计环境工作（2026-10-09）

用户在上述交付后明确“push吧”。提交前CLI fetch确认main/origin仍08ff02a、0/0；提交包含牛奶屋A／透明窗和门框修复、昼夜／天气、第四版地图及全部成品重排、钟楼顶面修复、兼容存档、生成产物和技能／文档。保留22项已完成检查及每项确切版本，不因只改Git排除和文档而重复无变更实机运行。Git只保留精简JSON／总览／视频／接触表和选定原生PNG，其余逐帧截图、构建缓存、日志和正式存档在本机保留；不删除或上传正常save，恢复stash保留。实际提交哈希与远端上传结果以Git历史及本次最终回复为准，不能把准备中的记录当作推送成功凭据。


### PLATFORM-04：ea1f197 的 Mac 全镇构建与26项复验（2026-10-10）

- 用户要求 pull 最新、总结 Change 并构建 Mac。CLI fetch 后 main 从 aed8b28 fast-forward 到 ea1f1979962700d5d0955730a021e9d226e02962，共4提交：性能研究、20米可进入钟楼/持续时间、磨砂钟盘/五档调时/材质、完整地图/奶屋A/昼夜/天气/存档及表面修复。本地PLATFORM-03先stash再恢复；追加冲突确认其内容已完整存在远端，保留全部新记录，stash仍留存。未提交或推送。
- 本机Unity6000.3.18f1执行BuildSlice.BuildFreshMac，构建316150827字节，通过FULL_MAP_AUTHORING_PASS、BUILDING_LAYOUT_PASS、CLOCKTOWER_SURFACE_SEPARATION_PASS等。实际Info.plist为com.qingchuanstudio.wrongdatetest.currentmac，x86_64+arm64；codesign深度严格校验通过。13份构建生成差异备份于builds/macos-current/generated-backup-ea1f197-20261010后恢复HEAD，不改游戏源码。测试程序集SHA256为7e5fa09d1903fb9993879e9c8c8fa8d1f9334f7096224b2e86da291f17dae270。
- Apple M5 Pro/Metal可见原生玩家26项均PASS：720p/1080p各自钟楼上下楼/五档调时/真实磨砂玻璃/运动帧与独立楼上读档、奶屋订单/灯/铃/窗/门角横移及独立读档、天气及独立读档、昼夜及独立读档、少年全分支及独立读档；另测共享时间与独立读档、钟表铺门窗/E07/坐姿/抽屉/展柜与店内读档、全镇70宅地/11道路/4迁移/6种室外访问顺序/雨中奶屋与独立恢复。整图原生运行810.28s，独立恢复4.27s。结果与确切日志/程序集哈希在builds/macos-current/verification-ea1f197-20261010/results.json。主要界面与钟楼/奶屋13帧运动接触表已目视核对；未把取样称为FPS或全面无闪烁证明。原生部分JSON verification字符串继承上游Windows文案，本轮实际平台以Player Metal日志和外层results.json为准。
- 正常slice-save.json SHA256前后为1af71d6dbd87b74462ba54b59939256070387d7265619c742600479ee6dda776；全部测试档备份并恢复。正常无review启动日志确认同一存档路径，旧街道(0.93,.03,-4.56)在内存迁往新广场，未覆盖磁盘正常进度。窗口读取工具响应异常缓慢；不能把仅AX窗口名说成已完成正常手动全流程。
- 现有桌面同名符号链接继续经~/Documents/GitHub/game_test/builds/macos-current/错日证词-新版.app进入新版；该固定入口现链接到本工作区已验证app。直接复制到Documents时文件提供者会重建MainMenu.nib的FinderInfo，使codesign严格校验失败；只清理FinderInfo/ResourceFork无法稳定保留，因此采用本地已验证app链接，未移除安全隔离属性。旧应用及失败的待安装副本保留于backup-before-ea1f197-20261010。deployment-ea1f197.json记录实际位置。
- 安装包错日证词-Mac-ea1f197.zip约106MiB，ZIP及临时目录解包后codesign/程序集/双架构均通过，摘要与Change说明同目录。地图原始tools/check_town_map.py报告verification.json过期；临时重算16份导出字节一致、几何/叙事断言通过，TownPlan来源对应Windows CRLF的原地图；build_plan.py与checkerHash另有过期摘要，见map-platform-audit.json。未修改地图或降低断言。
- 属于程序驱动真实CharacterController/射线与共用UI处理器验证，不是物理键鼠或陌生玩家盲测；未做Intel实机或低配帧率验收。67处占位房屋、车站/住宅室内和完整案件闭环保持上游未完成状态。


### MAP-04：游戏内坐标地图／目的地／步行导航（2026-10-10）

- 用户要求 M 打开地图、右键设置普通坐标图标／再右键取消、最近步行路线，以及钟塔／奶点／裁缝／住宅／警署等大图标。CLI fetch 后 main/origin/main 均 ea1f197，无新提交；保留先前 PLATFORM-04 记忆。按现有建筑配置：H03 已是裁缝商住，无新增或移动建筑。参考 GTA PC 官方手册／GTA Online 指南、Nintendo Zelda 官方手册及 Ubisoft Valhalla 访谈；来源与操作详见 docs/map-system.md。
- DemoApp.TownMap 接入 M／Esc，地图阻断人物／视角／钟表时间，恢复既有光标模式；IMGUI 全屏坐标底图、玩家朝向、滚轮光标中心缩放、左键拖动、16 地点图例／悬停／放大名称／占位状态。使用自产奶瓶、剪刀、盾牌、钟塔等图形，无外部美术包。右键新坐标替换旧标记，图标全部范围及取消按钮可删除；建筑目标选实际门外。B04 绘实际 milkRect 和附属体，大生产院落不冒充实体建筑。
- 内置 com.unity.modules.ai 1.0.0；共用场景构建从1008个实际碰撞来源烘焙 Resources/TownNavigation.asset，人物1.78m高／0.30m半径／0.24m台阶／0.075m体素。TownNavigation 用完整 NavMeshPath，显示步行距离；可走草地、院落，门需手动开启。坐标贴近4m内地面，禁止垂直吸屋顶，不可达显示无路线。近路基于这份离散导航网格，非连续数学全局最短证明。塔内路线同平面投影，未加楼层切换。未更改空间／证据／人物／已有房间。
- 首轮检查发现图钉上半部分点击遗漏，修正按实际46px图标范围命中；截图发现 IMGUI 裁剪组旋转长线造成漂移，改为世界坐标透明路线图层及缓存朝向图形，再验证原生画面投影像素。首轮失败日志及第三轮显示偏移截图保留于 attempt1/attempt3，不把当时的画面称为通过。
- Unity6000.3.18f1 BuildSlice.BuildFreshMac 最终317423115字节，Apple M5 Pro Metal 10/10原生检查PASS：720p／1080p地图与分别重启读档4项；共享时间／对话／牛奶订单及各自读档6项。每种分辨率验证全部16入口的完整路线与普通实体扫掠、三个可玩房间到室外路径、M／Esc共用处理器和人物／视角／时钟冻结、标记全图标取消／坐标转换／北向／缩放／侧栏隔离／不可达点，实际CharacterController走完旅馆绕行42.4994m，截图路线投影3个无遮挡采样点，重开按新位置重算，独立进程保留标记和原证据／对话／灯／调时／订单日期。最终程序集SHA256 38cb80a885cd9b58d3ef8b5a84f5cc5ddf7b681d610dcf3068cf1b50d6005417，详见 builds/macos-current/verification-map-20261010-final/acceptance.json；程序输入处理器与实际控制器验证，非物理键鼠、玩家盲测或 Intel实机。截图已核对，包括overview／hotel-detour／zoom-home-route。
- 所有测试独立map-review-save.json等专用档，正常slice-save前后SHA256 f6fe46ad784581284413ca329c3f47a36df098b92f81b0df017a2f4eb876e8cc，其他review档已还原。正常档在前次任务后已发生更新（来源未确认），本轮以开始测试的当前版本作比较，不回退历史档。13份无关生成序列化差异已备份至 map-generated-backup-20261010并恢复，新增导航资源／模块／源码保留。
- 正式Mac app及独立解包安装包均codesign deep strict通过，双架构x86_64+arm64，继续同一bundle/save路径和既有桌面→Documents固定入口→本地验证app链接。上版备份before-map-system-20261010。错日证词-Mac-地图版.zip约107MiB，SHA256 8ba47c897c0c87cb0e08a0a692a341f80ab4164a1ea2dd89d6f0d7a48baedf74；zip与摘要同时复制至固定GitHub目录，map-deployment.json记录位置。未提交或推送。


### MAP-05：MC式手持纸地图，持图继续行走（2026-10-10）

- 用户认可原地图功能，但要求纸质画风和 M 掏出实物地图，低头读图、抬头继续走路，类似 MC。CLI fetch 确认 main/origin/main仍ea1f197、没有新提交；完整保留上一轮未提交的地图／导航及记忆，旧app和本轮覆盖源码备份 before-held-paper-map-20261010。新要求取代MAP-04的全屏／暂停方式；地图坐标、建筑、证据、人物和正常进度不变。
- `HeldTownMap` 实际身体前方纸张、简化袖口／手掌／弯指，M用约0.22s升降姿态掏出／收起，不自动转头。眼睛独立俯仰：低头看完整纸图，平视时纸图留在下方；真控制器行走伴随轻微晃动。私有30层URP Overlay相机，只清深度且不加世界灯，近墙不会把纸／手遮进墙内。`HeldMapInk`资源材质使自定义Shader保留进正式Mac包。纸纹／折痕／卷边／棕色轮廓及建筑排线、印刷地名替代数字面板，16重要图形保留；地名错行，钟楼地名移到图形上方避免出生点箭头遮挡。字体沿用TownSans并随动态图集重建UV。纸上不印键盘操作，HUD两行不再重叠。
- 持图从Blocked移除，游戏时钟、WASD及鼠标观察继续；Q怀表互相收起。按住Alt释放鼠标，纸面右键设置／取消，松开恢复视角；Alt不阻断行走／时间。射线命中实际印刷平面后换算米制坐标，保留纸上滚轮缩放／左拖动；纸外不标记，坐标图钉上下都可取消，建筑导航到入口。行走约0.8m后最多每秒重算，路线用轻量纸面线条网格、箭头逐帧更新，不每帧重画整图。调查簿／对话／订单等照旧暂停并藏纸，退出恢复；Esc优先遵循当前模态，正常持图时收起。
- 初次编译遇URP clearDepth只读属性，核对本地17.0.1源码m_ClearDepth默认真，删除错误赋值后构建通过。首轮实机显示正确纸张／手及连续步行，却揭示点击回算偏差：屏幕投影在z=-.001印刷层，旧射线平面却在z=0；改用同一印刷平面，不放宽坐标误差门槛。失败日志和先前画面保留verification-held-paper-20261010-attempt1。后续修正提示重影／住宅文字相碰，最后单移钟楼标注。没有把失败或旧版本截图称为最终验收。
- 本机Unity6000.3.18f1最终 BuildFreshMac 317441419字节，最终程序集SHA256 79421888d355e2fccf5227736b2af49285e5fd3348f4aadd0d0af0a79d4ea2d6。Apple M5 Pro Metal原生720p／1080p地图各20.16s／19.65s、独立进程保存恢复3.84s／3.79s共4项最终PASS；包括M不改变视角／不暂停，俯仰显露纸图的投影变化、真实控制器低头和抬头持图行走／时钟推进、Alt编辑非暂停、透视坐标往返、16入口的完整路线及实体扫掠、纸外不落标、图钉上部／尖端取消、纸图缩放／拖动、三成品房间到室外、旅馆42.4994m绕行实走及持图动态更新、原生路线墨色10个投影采样、调查簿隐藏纸并暂停后恢复、Esc收起及新进程恢复原图钉／证物／对话／灯／调时／订单。截图looking-down／looking-ahead／home-route已目视核对；程序驱动共用处理器与控制器，不等于物理键鼠、盲测或Intel设备验收。
- 兼容性6项（时间／对话／奶屋订单及各自独立读档）在仅移动钟楼地名前的程序集 95e8816484567876f24431fe4e2df1290610b448f49d9a674ae8f1e5fc913f51 全PASS，具体18–20s运行／3–5s恢复；末次仅改印字位置后按影响范围重跑上述4地图项，没有伪称6项在最终SHA重跑。详细确切版本、日志、画面和范围在 verification-held-paper-20261010-label-final/acceptance.json，其他通过／失败阶段原样保留。
- 正常slice-save前后SHA256 ea7cdd2b3b7f3a93205101a31201e9c252acd17abcd25dfb82ba1b9fa7f4bd63；全部专用review档备份并恢复，既有地图标记兼容。13份构建序列化噪声备份held-paper-generated-backup-20261010并恢复HEAD，只留意图内代码／材质／Shader／模块／导航资源／文档；未碰synced sources。
- 新Mac应用和独立解包ZIP均codesign deep strict通过，继续com.qingchuanstudio.wrongdatetest.currentmac及原桌面→Documents固定入口→本地验证app链接。错日证词-Mac-手持纸地图版.zip为112143205字节，SHA256 360f7eea78c66bc76bb17c3ad3d0a8a15014515ba786e48a4a227dc738c65b8e；ZIP／摘要复制固定GitHub构建目录，held-paper-deployment.json记实际位置。旧全屏版本保留备份。未提交／推送。MVP与docs/map-system.md已按当前持图规则修订。


### MAP-06：暂不显示地图手部（2026-10-10）

- 用户要求“暂时不需要有手”。CLI fetch 后仍ea1f197，无新提交，保留全部未提交工作；删除HeldTownMap的MakeHands调用和仅供手／手臂／袖口的生成方法，纸张姿态／掏出收起／轻微晃动／M与Alt输入／路线与存档逻辑保持。当前地图说明同步注明不显示手部；旧带手应用、源码、ZIP与部署记录保留before-no-map-hands-20261010。没有改NPC的手部。
- Unity6000.3.18f1 BuildFreshMac通过317440891字节，最终程序集01cbfbb33659ed9b775a35c7d332f67f74739652c8a48de68a8878d40c0769c4。720p／1080p地图与各自独立读档4项检查均PASS（21.32s／3.82s／19.58s／3.91s）；原生home-route截图目视无手、手臂、袖口，地图与标记清楚。无新增低影响镜像测试。
- 初轮测试脚本结束的正常存档SHA保护断言失败：正常档从ea7cdd…变22d003…，仅位置／yaw／累计时间改变，其他字段相同。四份review日志均用map-review-save.json；默认正常Player.log确实另有slice-save.json启动、修改时间与正常档17:47:45一致，来源不是这四个review。保留新正常进度，不恢复旧档；独立正常进程已退出后以新基线重跑720p地图19.77s及读档4.07s，两项PASS且正常SHA前后均22d003c7f5e723923cff33e1e6fd578f48ce37f859c36aeb4ab74d9837f58897。原失败保留，详细来源／确切哈希见verification-paper-no-hands-20261010/acceptance.json，不将第一次checksum错误说成通过。其他review档恢复。
- 原入口Mac app和独立ZIP解包codesign deep strict通过；仍沿用原桌面链接与bundle/save路径。更新同名错日证词-Mac-手持纸地图版.zip为112146061字节，SHA256 51337eb4c1ae2ea51d14a323a07b1c03d86cf586c52f6395bcab9767c93567a9，已复制固定GitHub构建目录。13份生成序列化噪声备份paper-no-hands-generated-backup-20261010并恢复；正常存档未回退，未提交／推送。程序实机检查非物理键鼠或盲测。


## WEATHER-02｜每日阵雨表循环与阅读／对话不停表（2026-10-10）

- **用户确认**：先给出全天阴天为主、五轮阵雨及两次09:10–09:40／14:20–14:50短暂放晴的时间表；用户接受并要求每天一直重复，同时查看材料和对话也推进时间。CLI fetch origin/main 无新增提交，保持ea1f197和全部纸地图未提交工作；没有commit/push。
- **实现与操作**：TownWeather.Evaluate仍只读累计ElapsedGameMinutes，先换算准确怀表的日内时刻再按15个时段取样，每1440游戏分钟重复，时段开始用5游戏分钟Smooth渐变；午夜云的累计运动相位不清零。完整表已写入mvp-demo.md，新开局17:55处于17:50–18:20傍晚阵雨，仍06:00日出／18:00日落的美术轨道及7秒／10分钟。无独立墙钟、随机天气或天气存档。
- **计时规则**：GameTimePaused与Blocked分离。证物、调查簿、订单、对话、回看和机芯调时面板锁移动／视角，但怀表、共享塔钟、昼夜、云、雨、雨声持续推进；显式暂停、开场和加载冻结。钟楼齿轮正常查看时也继续运转，调时只改变原塔钟偏移，Reset恢复怀表+30，怀表按正常速度前进。退出无离线补算。旧累计时间／证物／门／灯／对话不重置。
- **实际验收与一次旧检查失败**：首构建Assembly-CSharp SHA256 306601bfeff87f7b6739cf1105c63795e49ae76def035d943b95a89fa4d3bdd9，原生Mac/Metal20项中19PASS；独立钟楼读档里的旧断言要求Reset期间Elapsed不变，因此按旧停表规则失败。修正为检验怀表增量等于实际活跃秒×原速、四面塔钟恢复+30；另纠正牛奶屋结果中旧timeAndMovementPaused字段。仅两个review文件变化，全部生产脚本SHA保持不变。最终程序集SHA256 2c1b91e752a63d3ced297b721ff15a4707c18d376e08fb50a952ddf4a1b3c4df 复跑牛奶屋／独立读档／完整夜间钟楼／独立机房Reset及101点下楼4项全PASS。其余16项保留首构建的确切哈希；跨两构建的20种路径都通过，不能写成最终哈希20项全部重跑。
- **覆盖与画面**：天气／昼夜720p和1080p各含独立读档；三天逐.05游戏分钟扫过全部边界、同怀表时刻每日一致、15段中点匹配、初始阵雨、真实钟控变化、屋顶遮雨、世界雨视差／两窗、路灯与室内补灯、手动Off；阅读时云／雨／音持续、显式暂停冻结。时间检查包含材料／调查簿／对白／回看原速与移动锁定、连续表针、共享塔钟、自动存档及独立恢复；另复验少年全分支／读档、无手纸图／导航／读档、钟表铺／店内恢复。09:25晴天、13:05阴天、17:55雨景和阅读／回看原生画面已目视核对。结果在builds/macos-current/verification-daily-weather-20261010/acceptance.json与*-final/results.json。程序驱动真实玩家／射线／共用处理器，不是物理键鼠、盲测或低配FPS。
- **保存与公平性**：两轮正常slice-save.json前后SHA均22d003c7f5e723923cff33e1e6fd578f48ce37f859c36aeb4ab74d9837f58897；专用测试存档及time/weather/day证明文件全部恢复。计时使用没有改证物／历史纸面记录，也没有新限时问答或时间门控线索；这项核对不代替陌生玩家推理盲测。
- **构建交付**：最终build-daily-weather-final-20261010.log SLICE_BUILD_PASS 317445435 bytes。当前唯一推荐错日证词-新版.app和桌面原双层symlink已更新；Universal x86_64+arm64，实际测试Apple M5 Pro/Metal。新错日证词-Mac-阵雨循环版.zip 112154617 bytes，SHA256 4d09202078813412e9e0ed370dcfc765728afc540f1e2186fd7a0a95ce841d25；源app与独立解包均通过codesign deep strict，ZIP／校验另拷至Documents原构建目录。before-daily-weather-20261010保存原app／源文件／状态；每次build只备份并恢复开工前干净的13个无关生成序列化变化，保留全部既有地图资产和手工工作。部署元数据daily-weather-deployment.json。


## PRESENTATION-01｜怀表／地图取放音效、去地图提示条与厚云（2026-10-10）

- **用户要求与同步**：给打开／关闭怀表和地图加音效，删除地图下方“低头看地图”一类小字，加厚云、让环境更压抑。CLI fetch origin/main 无新增提交，仍ea1f197；保留全部地图／天气未提交工作，未commit/push，未碰synced sources。
- **音效实现**：tools/build_prop_sounds.py生成四段原创程序合成PCM44100Hz单声道WAV：MapUnfold0.46s、MapFold0.38s、WatchTakeOut0.30s、WatchPutAway0.28s，纸纤维摩擦／折叠与金属扣响／口袋衣料两种质感，没有第三方录音。PropSoundAuthoring在构建确保PCM/DecompressOnLoad；TownSound一次加载，两个独立2D短音源分别响应纸图和怀表，音量0.52／0.48。SetWatchOpen和SetMapOpen统一正常Q/M/Esc及互相收起；相同状态不重播，临时阅读藏纸／恢复不算收起。订单实际收起怀表也走统一处理。
- **地图画面**：移除原DrawHeldMapHint整块底板及两行WASD／低头／Alt／M操作字，改为只画右上角目的地／距离。纸上的地名／方位／米制说明保留，无手。地图输入、导航与存档不变。
- **云与光线**：晴天coverage0.28，多云0.66，阴天0.99；阴天darkness0.62、自然光0.57、直射0.13，雨天darkness0.78／自然0.38／直射0.04。DuskSky加深云体阴影及高层云幕，保留两层纹理、同密度光方向采样／云遮阳和单方向光；DayNightCycle降低阴天白昼环境光饱和度与强度。夜间基础环境光／深处必要灯不压黑，仍能辨认上层脚印。没有改每日15段时刻、5分钟渐变、7秒／10分钟、阅读不停表／显式暂停／无离线推进及既有手动关灯规则。
- **实际验证**：Unity6000.3.18f1 BuildFreshMac317578707 bytes；最终程序集SHA256 4497d5ac61e0d1aa5d10862a5413a05c41928ab15b65089573d453066834c5ac，同一构建Apple M5 Pro/Metal18/18原生通过。720p/1080p各音效、地图／独立恢复、天气／独立恢复、昼夜／独立恢复共14项，另共享时间／恢复2项及完整夜间钟楼121.26s／楼上读档Reset与101点下楼62.49s2项。四声均测到正在播放的AudioSource非零输出，峰值／RMS／声源／采样率和导出的实际PCM在props-*；互相收起两声源和阅读不重播也通过。底部无提示纸图、13:05厚阴云和夜间上下层台阶／上层脚印原生画面已目视核对。程序驱动真实控制器与输入处理器，不是物理键鼠、扬声器录音、盲测、Intel设备或低配FPS验证。
- **保存与公平性**：本轮开始的正常存档SHA为d7822c49e083753fa4550cfbb384fbb8dbb4ee7dd0f2fcdd1a517fcb547d1a69，结束相同；它在上一轮之后已经更新，本轮保留当前进度，不回退以前22d003…版本。专用测试档及time/weather/day证明文件已恢复。环境加暗不改证物内容、历史天气事实、路网或调查权限，夜间上层脚印／台阶及材料UI仍可见；该核对不替代盲测。18项确切结果见builds/macos-current/verification-props-heavy-clouds-20261010/acceptance.json。
- **交付与备份**：原app／受影响源码保存before-prop-sounds-heavy-clouds-20261010；13份开工前干净的生成序列化差异先备份再恢复，新增音频与GUID保留。当前唯一推荐错日证词-新版.app与原桌面双层链接已更新；源app和独立ZIP解包codesign deep strict通过。错日证词-Mac-厚云音效版.zip 112268986 bytes，SHA256 2746a7c7c878f88577cd33f3ef67e71fbcd95fbd06cb4d18116d10917eb5a382，ZIP/摘要已另复制Documents原构建目录；部署信息props-heavy-clouds-deployment.json。正常bundle/save身份不变。


## WEATHER-03｜灰雾、重雨落地水花、连续雨声与室内听雨（2026-10-10）

- **用户要求与同步**：降低能见度、灰色大雾、降低白天太阳亮度，修正断续雨声，增大雨势，补充雨滴落地声音和可见溅水、室内听室外雨的效果，并录片段展示。CLI fetch确认main/origin/main仍ea1f197、无新增提交；保留全部既有未提交地图／时间／道具工作，没有commit/push。
- **灰雾和日光**：保留15段日程及5分钟渐变，阴天mist0.74、雨天0.90；雾密度为昼夜原基数加mist×0.057，白天阴天约0.05068、雨天约0.0598，灰色雾与近地天空相接。晴天直射系数0.66、太阳基础强度0.9，天空太阳光斑从2.1降至0.75并受雾削弱。所有状态仍以累计怀表时间为准；路灯／手动关灯规则不变。
- **雨线与落地**：固定256条世界雨线，雨量1、宽度／长度／速度提高；新增RainSplashes一张固定96落点、5376顶点网格，短促溅珠与扩散水圈按保存时间运动。世界地面接触使用缓存碰撞查询，每帧最多4次，屋顶覆盖地面不生成水花；不创建物理雨粒，不改建筑／路网／证物。实际画面雨点、水圈、灰雾街道和室内窗外雨已目视核对。
- **雨声修复**：旧六秒噪声循环首尾各40ms归零，是周期性变小的明确来源。RainAmbience改为24/29/31秒三条原创立体声循环，循环噪声滤波从尾段预热，击水尾音跨接缝折回，无静音端点。雨幕＋落地声＋屋顶闷雨，屋顶遮蔽约0.625秒渐变，雨幕低通16000→1100Hz，地面层→750Hz，室内增加950Hz屋顶层；有明确Pause/UnPause状态并恢复意外音频重启。阅读继续播放，显式暂停停雨声且能恢复。
- **音频实测与失败追溯**：首轮读取AudioSource.GetOutputData时，室内频谱断言失败；本播放器该取样点在声源滤波之前。改为最终AudioListener混音，保留声源对照，生产音频代码没有改动或放宽阈值。最终室外／室内高频差分能量比约0.29461／0.00640，而源取样仍约0.30906／0.30491；最终混音RMS约0.082／0.032。连续跨24秒循环的声源窗口RMS最低0.07227、最高0.09066，无归零，暂停恢复与室外回切通过。原始失败仍在verification-fog-heavy-rain-20261010；中间成功版本bd84f6c…与最后只改灰雾取景角度的验证均保留。
- **验收版本范围**：Unity6000.3.18f1、Apple M5 Pro/Metal。12项天气／独立恢复720p和1080p、昼夜／独立恢复两档、牛奶屋／恢复、夜间钟楼101点上楼／恢复下楼在程序集1bd3795151606daa6f7f5bb22a4b05703dabb716d6edf4ea15cf9af463d62a1a全部PASS。最终雨声／水花／室内进出／暂停／灰雾录制68.04s在交付程序集43516aabfe7826da7fa8d935e8fa5c4b6db772d7a3a04711d9f5b2dde3953a31 PASS；生产天气／灯光／音效／水花源码SHA与前12项一致，只有验收取样和取景变化。不把13项写成全部在最终程序集重跑。汇总和确切日志在verification-fog-rain-street-video-20261010/acceptance.json。钟楼上下层、暖灯下上层脚印和奶屋材料可读性已目视检查；非物理键鼠、盲测、Intel实机或低配帧率验证。
- **用户录像**：output/demo/fog-heavy-rain内有30.04秒大雨水花、19.18秒实际控制器进屋／看窗／出屋、10.04秒13:05普通日程灰雾街景，及约41秒合辑。前两段隔离review暂定Rain预设以完整比较声音；第三段使用普通日程。真实1280×720帧和48000Hz立体声AudioListener混音按真实时间戳同步；约20Hz录屏采样编码30fps，不代表游戏FPS，无调色／变速／后配雨声。三个录音均非零且无削波；来源摘要recording-provenance.json。HTML观看页与源截图同目录。
- **保存与交付**：各轮正常slice-save前后SHA均2985efb10e5a07269451ad56da0359d296d44c0f7cb4bb17d8d24d450fc19ffa，专用review档及证明文件恢复。该当前存档在前任务后已更新，未回退到旧进度。原app／源文件备份before-fog-heavy-rain-20261010；每轮13份开工前干净的无关生成序列化差异备份后恢复。最终BuildFreshMac317595607 bytes，桌面原双层链接继续指向当前app；旧正常进程未强行退出，重开才加载新版。Universal x86_64+arm64，源app与独立解包均codesign deep strict通过；错日证词-Mac-灰雾重雨版.zip 112278330 bytes，SHA256 ac2c286f61f41166ea925771dec411e3b0456f5373e1e32c71d352930c36d23b，ZIP和摘要复制至Documents原构建目录。部署元数据fog-rain-deployment.json。


## WORLD-04｜柜台旁送奶少年、双扇实体门、开门雨声与感光路灯（2026-10-10）

- **用户要求与同步**：送奶少年移到牛奶店内靠柜台；房屋外门打开时仍听室外雨声；牛奶店门可开关；路灯按自然亮度自动开关、雨天也亮。CLI fetch main/origin/main仍ea1f197，无新增提交，保留此前全部未提交工作，未commit/push。用户本次要求取代旧“少年朝街站在店外”和“白天路灯始终关闭”约定。
- **人物与门**：共享MilkShopAsset把原Meshy少年放在店局部(2.35,.18,.85)，世界约(-146.209,.122,6.445)，朝局部-Z顾客区，原0.88比例、模型/衣着/待机/对话/E04保留。账本与左右窗通道真实控制器通过。复用原Door_Left/Right网格及BoxCollider，各有E开关、实体阻挡、扫掠防夹；门从旧固定115度改成向外85度，以便从室内看见并点中门扇。稳定ID为milk-left-door/milk-right-door，首次左开右关；initializedFixtures迁移一次，关上默认打开的左门后读档不会重新开启。ShopMechanism同步最新物理位置后做扫掠，并按父级比例计算叶片盒，牛奶店0.9比例也准确。
- **雨声与灯**：WeatherAuthoring明确绑定牛奶店两扇门、钟表铺前后门和钟楼门；WeatherShelter.MufflesRain按实际OpenFraction判断，同屋任一外门有开度便用室外雨声，全部关严才渐变为低通/屋顶闷雨。另一栋屋的门不影响当前房间；雨线/落地水花仍使用独立屋顶遮蔽。路灯与外墙/窗暖光读取Daylight×Weather.naturalLight，不采样灯自身；<=.42全亮，>=.60关闭，中间Smooth渐变。正午雨天.38，放晴1；不改15段循环表、怀表速度、阅读不停表、室内手动Off或最多12处路灯光池预算。
- **诊断与真实修正**：首轮门沿用115度，室内射线被门框挡住而失败，调整到85度后两面可操作。第二轮防夹检查在门前停止容差边缘失败；加最新物理同步，并把验收停点移到实际关闭门叶平面(local z=-3.08，原-2.90)，保留真实控制器、不扩大容差。诊断轮与最终轮防夹均通过，失败记录保留于verification-dairy-doors-first/access-20261010。新.42-.60灯曲线每.05游戏分钟最大变化独立计算为.005644，连续性测试据此将外灯上限设.006，其他角色仍.005；未改变实际灯曲线来迎合旧阈值。
- **最终实机验收**：Unity6000.3.18f1，交付程序集SHA256 d13665e44854926f1a4682dcb1600bde3fcb154e873f94e30d96403fc2b8ae5c，Apple M5 Pro/Metal同一程序集22/22通过：新门/声学/少年/灯控及独立恢复2；720p和1080p天气/恢复、昼夜/恢复8；少年完整对话/恢复2；牛奶屋账本/两窗/灯与恢复2；手持纸地图16地标路线/恢复2；钟表铺前后门/抽屉/证物/恢复2；夜间钟楼上楼/调时及独立读档下楼2；全镇11道路扫掠、70房产、3成品建筑、6种外部调查访问顺序、屋顶索引和4旧坐标迁移/恢复2。全镇实际行走812.88秒，保持正常4.1m/s；延长验收驱动等待以容纳长路线，未加速玩家/物理或改断言。精确记录acceptance.json；非物理键鼠、盲测、Intel设备或低配FPS测试。
- **声音/视频与目视**：最终混音左门开/双门关/右门开高频差分能量比约.29650/.00633/.29519，低通约16000/1100/16000Hz，房内屋顶仍干燥；其他建筑前/后门均验证。output/demo/dairy-doors-lighting提供29.46秒牛奶店双门与进出听雨、7.43秒雨天亮灯/晴天熄灯以及约37秒合辑，1280×720实际帧、48000Hz立体声游戏混音、按真实时间戳原速编码，无后配雨声；为直接比较使用隔离正午天气预设，正常游戏日程不改。已目视核对少年柜台位置、门、雨/晴路灯、两窗、夜间台阶及机房。视频解码通过、录音非零且无削波，来源及摘要在recording-provenance.json。
- **地图与公平性**：角色当前探索点更新在地图README与MilkShopPlacement报告；宅地/门洞/道路、历史actor路线与证物内容不动。现有平面数据20角色/17路线/6顺序几何与叙事合同验证通过；完整check_town_map仍因旧verification.json元数据未同步失败，已核对该文件与开工备份相同。没有把此旧元数据失败写成整体检查通过，也没有为了消除它重导历史地图。FullMapReview内部描述残留Windows旧文案，本轮实际平台以Player.log和驱动记录macOS Metal为准。
- **保存、生成物及交付**：正常slice-save前后SHA均2985efb10e5a07269451ad56da0359d296d44c0f7cb4bb17d8d24d450fc19ffa；专用review档/证明恢复，旧正常进程没有强制退出。before-milkboy-doors-lighting-20261010备份原app/源码/文档/差异；9份开工前干净的无关序列化噪声先备份再恢复，保留4份包含本次门绑定/人物点位的有效ClockShop/ClockTower prefab及Town/ClockShop场景更新，避免退回实际场景行为。BuildFreshMac317607775 bytes，原桌面双层链接继续指向当前错日证词-新版.app，退出旧进程重开才加载新版。Universal x86_64+arm64，源app和ZIP独立解包deep strict签名通过；错日证词-Mac-牛奶店门与感光路灯版.zip 112269334字节，SHA256 f4383998cf220eb796dc27c4dec34676f82d24172c406a32e24d2674bdf82bc4，ZIP/校验复制Documents原构建目录；dairy-doors-deployment.json记录身份与摘要。


## MOVE-01｜Shift 跑速与剧情时间共同校准（2026-10-10）

- **授权与依据**：用户要求检验原主从家跑到车站的速度是否符合剧情，明确答复“一起校准跑速和时间流速，让剧情时间可复现”。CLI fetch main/origin/main仍ea1f197，无新提交，保留全部原有工作，未commit/push。读取原修订DOCX约17:35离家/17:55返家及mvp-evidence中17:40车站露面，不能只看20分钟往返。旧地图639.15m/1.2m/s演算没有计入游戏表7秒/10分钟的加速。
- **实现**：PlayerMotor定义WalkSpeed2.5/SprintSpeed6，左右Shift均持续奔跑，无体力限制；普通输入和专项review共享归一化，斜向不额外加速。原scenic测试的慢速analogue保留，MapWalk改用同一Sprint常量。GameClock改4/60游戏分钟每现实秒，现实150秒=10游戏分钟、昼夜6现实小时；是用户认可的全局调整，不改17:55新开局、保存的累计分钟、+30分钟塔差、剧情日期或证物。昼夜/15段阵雨表继续同一时钟；阅读/对话走时、显式暂停停表、不计离线时间。底栏改“Shift 奔跑”。旧7秒/10分钟记录只描述旧版。
- **实测**：最终剧情候选去程335.43m/55.91s/3.7280游戏分钟；后巷返程298.44m/49.75s/3.3167分钟。17:35出发，17:38:44到站；等17:40再停10现实秒，17:43:59返家，剩11.02游戏分钟。未穿楼/中途传送/暂停或加速测试物理，时钟全程活跃。旧4.1速度同路实测81.78s+72.77s；旧表折算约220.79游戏分钟（明确是换算）。地图最短路线另实走，不冒充历史公共道路。门口代理退离实体墙1m/1.3m，与先前可走验证点一致；住宅/车站室内未做，等待时长仅测试余量。
- **诊断/修复**：首轮街道/地图/门/上楼通过，但全速下梯在第8点离台阶超0.32m，源于grounded固定-1m/s低于6m/s下坡的垂直下降速度。改grounded向下接触速度为-max(2,speed)，离地后仍18m/s²重力，保留实体碰撞、阶高和0.32m高度断言，未放宽测试。专项25.92s再次全速上下101点通过，最终完整路线再次通过。初次失败保留verification-sprint-clock-20261010；修复专项verification-sprint-grounding-20261010；交付结果verification-sprint-clock-release-20261010。
- **验证**：Unity6000.3.18f1，Apple M5 Pro/Metal；最终同一程序集b7c8a8810ccfd26fdf1f5a89a013b17c6c396b2b54ed1b7b56a392e1b97e6c90的14/14通过。路线/独立存档2，时钟连续针/边界/阅读/自动保存/跨进程2，16地标纸图/碰撞近路/恢复2，1080p天气及恢复2，720p昼夜及恢复2，牛奶屋账本/灯/窗及恢复2，1080p夜间钟楼/调时/恢复下楼2。专项真实控制器全速直行/斜行、持图、closed/open门和楼梯也通过。各计时断言改对照实际现实耗时×共享常量；没有为慢表跳过阅读或边界检查。10证物/6设计顺序静态合同及候选路线源比对通过，未改地图所以未重新宣称6整图实机顺序。非物理键鼠/盲测/Intel实机或低配FPS验收。检查点、源摘要、保存隔离和交付摘要在acceptance.json。
- **保存与交付**：正常slice-save SHA前后均2985efb10e5a07269451ad56da0359d296d44c0f7cb4bb17d8d24d450fc19ffa；review存档/证明均恢复，未强退旧正常进程。before-sprint-clock-20261010备份源码/文档/差异/app/正常档。13份生成噪声先备份再恢复到开工状态，其中4份早已dirty的prefab/scene从开工patch精确还原，保留此前牛奶店门/少年改动。Universal x86_64+arm64，同一桌面双层链接指向当前app；退出旧进程重开加载。源app与ZIP独立解包deep strict签名通过，压缩包错日证词-Mac-跑速与剧情时间校准版.zip，SHA 3b5cb99318588026bf679428fa18961f08c272d5099f8982f10d42d4fb2c7c5d。output/demo/sprint-calibration有新程序集去程/返程原速实录、原生怀表截图及测量，不使用旧候选版录像冒充最终版。


## RULES-01｜现行玩法规则归档与提交授权（2026-10-10）

- **用户要求**：将当前时间、人物速度、天气／雨声及路灯写进 agent 规则并 push。本轮已明确授权提交与推送；此前条目的“未 push”仍是各轮当时的历史状态。开工 CLI fetch 核对 main 与 origin/main 均为 ea1f197，无需合并远端改动。
- **文档**：根 AGENTS.md 新增现行基准：4 倍时间、2.5／6m/s 走跑、阅读不停表、15 段循环阵雨与灰雾、三层无间断雨声、同屋开门保留室外声、.42／.60 感光灯阈值与手动关灯优先；同时记录柜台少年、可关双门、无手纸图和取放音效。补充测试／存档边界，并将旧“原切片站位”改为明确历史记录。当前 MVP、叙事合同和源码参数一致。
- **提交前核对**：当前 11 份 MOVE-01 源码／路线摘要与最终 14 项实机验收时完全一致；读取既有 acceptance.json 确认各项通过，本轮没有改玩法代码或重新运行实机。叙事静态合同检查 10 证物／6 设计顺序通过，JSON 解析与差异格式检查通过；不能将其称为新一轮游玩或公平性验收。
- **范围**：提交本地累计完成的实现、共享生成资产、规则文档、精简测量与用户演示片段；builds、应用／ZIP、缓存、正常存档及原始逐帧数据保持忽略。提交文件无超过 100MiB 的单文件，已检查常见凭据模式。仅整理现行规则，不重建已通过验收的 Mac 应用；确切提交与推送状态以 Git 为准。
### RAIL-01：B站、墨绿黄铜蒸汽列车与双向跨镇（2026-10-10）

- 用户在选定B铁玻璃车站、可落座启动并黑屏跨镇的列车设计后明确批准完整实现。本轮基于ea1f197，未提交推送；保留此前概念图/设计笔记。B17位于(188,0,46)，30×12m主厅、8×8m两翼、68×22m圆弧玻璃雨棚，主/后门仍(188,40)/(188,52)。地图4成品/70宅地，SVG越界旧轨道纠正，路线剧情不变。
- Antigravity仅在隔离scratch起草Blender模型。主代理修正实心窗背板、FBX轴下轮组父级/枢轴、平顶/雨遮蔽、缺失脚本、共享坡道网格覆盖、穿墙字牌和动态字体图集。最终源可编辑且共用内外/碰撞几何；Unity车轮/连杆独立，车厢窗真实开孔、上客侧八门、两客车和连接过道可走。真实封闭拱顶与椭圆雨遮蔽对齐，移动屋顶重新索引，保持雨256/阴影预算。
- RailTrain停靠→E座位→关门/鸣笛/蒸汽/轮组/短位移→渐黑→OtherTown同座位，E起身并可返程。进度/共享时钟保留，转场暂停时钟。发车前存停靠座位，移动/加载Save不覆盖，到站提交新场景和座位；独立进程支持两镇与发车安全检查点恢复。邻镇仅120×110m车站街区/背景外景店屋，未实现完整第二镇/证人/历史路线/班次。E03/E08自由阅读遵循原合同，副本明确出处。
- 最终1080p完整往返、720p到邻镇、两种到站独立重载、发车安全检查点独立重载、EditMode8/8、原有时间/重载、奶屋/重载全部PASS，证据与当前程序集哈希见output/demo/rail/acceptance.json及README。发车检查点取实际运行保存并原样重载，非强杀进程测试；原生脚本驱动真实控制器/普通射线，非物理键鼠/盲测/FPS。地图freshness/网页PASS，未重跑历史六室外顺序。
- 正常存档前后SHA256保持FDE6BCE28F10294A989DC5052A88111F08855E624BC756BEFD3A1BB326B58839。源生成器tools/rail/build_rail_assets.py，BuildRail.ps1/ReviewRail.ps1为入口。refresh_metadata.py只重写源尺寸说明不改几何，最后该文档修正没有重复构建或复测无变更程序集。

### RAIL-02：车站／列车细化与实际已验票孔（2026-10-10）

- 用户试玩后要求按照B站与列车设计补细节、质感，明确废票桶内必须打孔、主角随身票必须无孔。本轮仍基于ea1f197、未提交推送。Antigravity在隔离scratch起草细节套件，本地修正实际拱洞、票格遮挡、行李架占窗、告示朝向、桶高及长椅遮挡；剧情判断、票资产、Unity集成与最终验收由主代理完成。
- StationB补圆拱真实开口、石材分缝／墙脚／檐口、入口铁玻璃小雨棚、铁架连接、墙灯、木护墙／柜面／开放票格、细化长椅／告示、废票桶／检票钳和行李推车。TrainSet补木座框扶手／织物压边、开放行李架与包箱、顶肋与黄铜灯、窗锁、锅炉管／箍／铆钉、板簧与制动件。内外和碰撞仍共用几何，行李架升至窗上方以保持全部窗洞及通道；九套原创512px基础／法线／金属光滑图，石墙和地坪统一投影UV，源打包图像，灯位置与真实灯具对齐。
- E02按用户确认改为主角随身，**T**进入原阅读界面，Jan20原件完全无孔；跨镇／重载不改变原件。E03移至获准查看的废票桶，八张全部为三个实际穿透孔；废票日期未确认而留空，放大及调查簿有对应透明孔。ticket_art.py统一110×65mm纸、半径3.6mm三孔的JSON几何与票面；Unity网格与TicketProps.blend共用坐标，源保存重开及网格射线验证。旧条目复看更新来源／物理描述，稳定ID及进度保留；不否定19日真实乘车、不增加自动推理或历史班次。
- 字体发现“程券妥善”等缺字，从既有OFL缓存补齐；770→822字形，旧字形全部保留，TownSans SHA256 db97bb4f1a8d8fc6aaebb9a7775ea14856d346de1b1be897d3a2f9012c541721。票面校对后重生成，最终两档原生画面已目视票孔／无孔／中文与模型材质。
- 最终资产将废票桶向左移0.7m避开长椅，本镇两档完整往返／两档座位独立重载／实际发车安全检查点独立重载、EditMode12/12通过。邻镇独立重载、时间／奶屋及各自重载在仅桶位置不同的前一候选构建通过，逐项确切版本保留，不伪称全部最终资产重测。共17项记录在output/demo/rail-polish/acceptance.json；最终程序集9d70849d7eae3b802aed7c2360637a459f8c29dbd232df2bb00c05ec369a0a18，全目录fingerprint b8179364c4ea5419c17bbe465324c80615d54100587cbccf14df3b0e970435d9。Unity报告381912633字节、整个目录382107556字节，非exe单文件。剧情10ID/D02D03及地图freshness通过，未重跑六室外顺序；脚本驱动真实控制器／普通E射线与UI函数，非物理键鼠、盲测或低配FPS。
- 正常档基线／测试后SHA256 E1B5BC65F2C5779B98F9F65B1570B999D83E8F9251CFC50C431C1A428350ECC4；与RAIL-01不同是用户期间手动试玩，未回滚其进度。正常位置Town(191.291,0.12,44.400)、未落座，位于候车厅。所有测试使用独立review存档；测试后启动正常新版供用户查看，点击“开始漫步”恢复现有位置。邻镇仍仅车站街区，未实现完整第二镇案件。

### RAIL-03：可落座家具、车厢旋钮灯与环境查看（2026-10-10）

- 用户要求增加可交互items、椅子等。本轮fetch后main/origin仍ea1f197，保留此前车站／列车全部未提交工作，未提交推送。工作由主代理完成，没有Antigravity或子代理参与。
- `FurnitureSeat`与`DemoApp.Furniture.cs`加入59处普通休息位：8街边长椅、4候车长椅上的8处坐位、两客车43处座位。普通3m E射线坐下、E起身，坐姿降低眼高并保持转头；Q／T／Tab和暂停沿用原入口，世界时钟继续而阅读暂停。原首节南侧指定发车座位保持独立RailSeat／“坐下并出发”，不把所有休息位改成发车触发。
- 起身检查真实地面和站立胶囊，按主出口／邻近出口寻找安全空间；堵住后保持落座并提示等待。稳定furnitureSeatId与安全过道位置保存，独立进程恢复两镇普通座位或候车厅长椅；缺失或不可用座位安全回站姿。移动锁定和RailSeated分离，起身后原发车／往返流程正常。
- 两客车前门厅南侧增加可编辑黄铜旋钮，源在TrainSet.blend／rail_detail_kit.py。每个`CoachLampSwitch`只控制该节既有4盏DayNightLamp，没有增加实时阴影灯；手动关灯存入原openFixtures，重载／跨镇保持，开灯继续尊重昼夜采光。8处环境查看是旅客／候车／行李告示、行李秤、推车旅行箱、架上卷包／皮革箱／红箱，只读背景物件，无新增证物、历史班次或拾取背包系统。E02完整无孔、E03废票真实通孔保持。
- 互动HUD改为实际动作：椅子“坐下休息”、旋钮“打开／关闭车厢灯”，默认查看标题避免重复“查看”；保留旧门、机关、奶屋和对话提示。字体822→840字形，全部旧cmap保留，TownSans SHA256 eb7081879ecfaa942ae47daf6fbb262a34ba7e7e6fff59f9ff6abcfb07c10766。模型源保存重开、原窗洞／通道／雨顶和地图／剧情设计合同复验通过。
- `output/demo/rail-items/acceptance.json`共14项记录。提示修正前的720／1080逐个59座位、8查看、2开关、怀表／原票／阅读／堵出口，新进程车厅读档后跨镇、邻镇普通坐姿读档、候车长椅读档、原列车往返及发车读档均PASS。最后只改提示，最终1080全部新交互重复通过，EditMode16/16（原12＋真实站立空间4项）通过；没有把早期版本结果冒充最终二进制。首次验证走线直穿行李秤实体柱而失败，修改验收路线绕行后通过，保留碰撞及本机attempt-01失败记录。
- 最终Windows Unity6000.3.18f1／URP17／RTX5060Laptop D3D11，Unity报告382178753字节，目录382373676字节；程序集312320字节／SHA256 908e1c4a08c0d1053a56a050d7833f49514bc3d8072fc347a8aae622387639b6，全目录fingerprint 69f8d1a85c107de448917fa21d47209534951aca242213825d48b90a38093a42。提示修正前fingerprint 8aa1f0ec0fbdef7509a8f42f11ce940ad976278eeb8ec607d4668f6b6bc07216，完整版本证据分别保留。
- 旧正常播放器先正常退出，保存其最新进度作为本轮基线SHA256 451B35112DBFCF8C95CBAE42E7719FFB481A2643197C429FE51E826B1C1B5D3F，所有验收使用隔离items／rail档，结束后正常档同哈希。未回滚此前用户试玩进度；正常存档仍是Town候车厅未落座。`tools/ReviewItems.ps1`、`docs/rail-implementation.md`和rail-items/README记录复现／范围。验收是原生渲染、真实控制器、普通射线和共享处理函数；逐座位阶段在清晰过道放置玩家，不是完整59座位连续步行、物理键鼠、盲测、低配FPS或全案件六顺序。
- 最后从原入口以1600×900窗口启动正常新版PID35856，窗口标题“错日证词 · 暮色小镇”、Responding=true，日志确认正常save路径且无异常，正常档仍同哈希；启动证明normal-launch.json。用户点击“开始漫步”恢复既有候车厅位置，没有用验收存档替换正常进度。

### RAIL-04：世界字牌越界与浮空修复（2026-10-10）

- 用户指出很多文字不在范围内或浮空，例如“出站”。开工CLI fetch仍ea1f197／无远端新提交，保留此前所有未提交工作，没有提交／推送，主代理独立完成。本轮针对车站／邻镇车站／客车的21处世界标识，没有宣称全镇所有招牌完成审计。
- 原RailText使用独立坐标／字号，部分无底牌，中文站名压进圆窗及雨棚、候车厅方向在门洞内、邻镇出站位置／朝向不对应实际出口。`BuildSlice.RailSigns.cs`绑定导入网格的实际命中面，从连通共面三角形取边界／法向／中心；字体按内框真实字形等比缩放、居中，贴面2mm。新挂载父级须抵消FBX实际100倍单位缩放，不能直接把米制字面挂到导入renderer下。生成校验在站台牌首次抓到继承放大，修正后所有字牌通过，未放宽阈值。
- 入口中文牌附在雨棚前沿，英文／售票／吊挂站台牌沿用现有实体。候车厅方向移至后门实心门楣并补绿漆黄铜边牌；站台地面提示有真实立柱与脚座；废票桶有小标签。三张告示按实际纸面，客车编号／线路按原实心立柱／顶带的保留区域贴合。邻镇出站牌在厅内朝南街区出口的实心门楣，外站名贴同门楣外面，没有增改门窗或道路。
- `RailWorldSign`在font.textureRebuilt后重新适配；文字材质Cull Back，避免从牌背看到反字。编辑源来自共用Unity生成器，现有Blender牌面／车漆网格未改。实际字形四角必须留在内框且直接射中对应实体表面，边距／2mm间距均检查；不能靠ZTest Always、关闭遮挡或临时拖动场景副本掩盖问题。
- 最终同一二进制5项通过：1080与720各21牌的边界／表面距离／字号64→80→64及完整两次跨镇；59座位／8查看／2开关家具回归；独立进程返程座位恢复、原票及进度保留、正常下车；EditMode16/16。逐牌机位在玩家高度布置，原往返使用真实控制器／普通3m E射线；非物理输入／盲测／低配FPS。1080全部21原生画面与关键斜视已检查，接触表只缩小拼接加标题。证据`output/demo/rail-signs/acceptance.json`与README，`ReviewRail.ps1 -Signs`可复现。
- Windows Unity6000.3.18f1／URP17／RTX5060Laptop D3D11，Unity报告382301897字节，目录382496820字节；全目录fingerprint 21726508510cbac15e94384a14d4b2f5470029f8669e412e39369524f4666573，具体文件哈希在acceptance。正常播放器正常退出后保存的最新档基线／验收后均3A8D86417DC6EDF120F20DB5529C28D0D31A570335E3E452461714A19E2DE265，Town(191.291,0.12,44.400)、未落座，仍候车厅；没有回滚此前试玩，全部检查用独立review档。无新历史事实、证物或时刻表，完整无孔随身票／真实打孔废票继续通过。
- 最终程序集SHA256 032E6F79A23F4B4D09874214CC0C0DCE1C2F429C378D29E7CAC2544827CD2DD8。检查后以原入口1600×900窗口启动正常新版PID7968，Responding=true、窗口标题正确、日志确认正常save路径且无异常，存档仍同哈希；normal-launch.json保存启动证明，未用review档替换用户进度。


### SYNC-01：7f01ddb与本地铁路工作的合并（2026-10-10）

- 用户要求pull最新并与本地工作merge。CLI fetch确认远端新增一个提交7f01ddb（纸地图/道具声/每日天气/连续雨声/牛奶店门与人物/感光灯/6m/s与4倍时钟）。先保存378个本地文件的哈希清单，stash --include-untracked得9a8cfe83c9c716c2df9bd418b772900364d687da，再pull --ff-only至7f01ddb并apply，恢复stash留存；未提交/推送，main与origin/main 0/0。
- 出现11个冲突，覆盖前已向用户报告。共用DemoApp、PlayerMotor、WeatherShelter、BuildSlice逐处保留两边字段/处理器，文档历史记录合并；4份生成场景/预制件用有效上游输入后，由合并后的共用生成器完整重建。保留本地Station/Train/OtherTown、票孔、家具与字牌，并保留上游Map/PropSounds/导航/雨声/门声学/感光灯/新速度与读图阅读不停表规则。当前成品4栋、70宅地，导航1202碰撞来源重烘焙。79原二进制资产字节一致、101新资产GUID保留，无原文件缺失。
- 衔接：青川地图可以坐着看，发车真实收图/退出Alt，保存标记跨镇保留；邻镇无自己的纸图，因此M不显示青川的错误路线，返镇和重载可再取图/导航。T车票收怀表使用共用取放音效。阅读锁人物但不停表，列车转场仍冻结。铁路/家具验收按共享4倍常量检查计时；铁路拍照的残余dayNightReviewFrozen导致首轮失败，明确解除后两分辨率通过。天气验收旧机位将实体窗框取样点推出视口；仅把视线移到原窗面与原取样点中间，保留实体、运动/采样阈值与断言，首轮失败原样保留。
- WindowsUnity6000.3.18f1/URP17/D3D11/RTX5060Laptop最终SLICE_BUILD_PASS报告383831201字节、目录384026124。最终程序集5c563c233738f4abdba5d65a382cdc3408752f8f067179219c1704b05bb865aa；16/16最终EditMode及地图freshness通过。20项原生检查分别保留确切版本：5a9cae4b的12项铁路双分辨率往返/重载、家具/重载、地图/重载、道具声、奔跑安全、时间/重载；末次仅改窗框验收机位后5c563c23的8项天气双分辨率/重载、昼夜双分辨率/重载、牛奶店门声学灯控/重载。没有伪称12项在最后二进制重跑。全部21字牌画面、关键纸图/车票/座椅/天气画面已目视检查，证据output/demo/latest-merge/acceptance.json与README；完整原帧/日志/备份/脚本在忽略的builds/review/latest-merge。
- 原正常PID7968为构建而正常退出，正常档备份及测试前后SHA均3a8d86417dc6edf120f20db5529c28d0d31a570335e3e452461714a19e2de265，仍Town候车厅191.291/.12/44.400、未落座；未回滚试玩或替换review档。原存在的测试档备份后恢复，旧Windows包也保留windows-before。程序驱动真实控制器/3m E/共用处理器不是物理键鼠、盲测、低配FPS或完整案件；未重复候选故事全路线计时，原远端测量仍按其确切Mac版本记录。
- 合并验收后已从原Windows入口以1600×900重新启动正常最终版PID40276，Responding=true、窗口标题正确，日志确认slice-save.json且无异常；正常档启动后仍同SHA。不是把review档当试玩进度。


### TIME-01：完整一天为现实两小时（2026-10-10）

- 用户要求“把时间改成一天2h，把agent.md的也改了”。CLI fetch确认main/origin/main均7f01ddb、无新增提交；保留本地B站／列车／票孔／家具／字牌等全部工作及恢复stash，未提交／推送。GameClock改为12.0/60.0游戏分钟／现实秒：50现实秒=10游戏分钟，完整24小时=7200现实秒。同步AGENTS.md、MVP、证据合同、地图说明与Unity／Playtest技能，项目托管安装副本更新。走跑速度、初始17:55、塔钟+30、共享昼夜天气、暂停／阅读及保存累计分钟规则保留；天气5游戏分钟渐变现25现实秒，不改历史证言和天气事实。
- 时间验收更新旧150秒断言及跨小时／午夜输入，并在实际播放器中检查7200秒输入增加1440分钟、同一钟面相位、旧累计125.75不重算及暂停不推进。真实运行7.001113现实秒／1.400223游戏分钟，非两小时连续运行。新程序集3122f247a339761aa5a6087cc2b1073ed00dc64051f0866682af6b995c7342f1，WindowsUnity6000.3.18f1／D3D11／RTX5060Laptop，构建383833109字节／目录384028032。11项同程序集原生检查通过：时间／重载、完整跑速路线／重载、天气1080p／720p／重载、昼夜1080p／720p／重载、旧倍率存档载入。未重跑未修改的铁路／家具美术或EditMode几何测试。
- 另用备份旧4倍播放器5c563c23实际生成旧倍率时间检查存档，再由新播放器载入：累计1.147246857733分钟原值恢复、独立进程、不补离线时间，证物／对白保留。这一旧版生成检查不混入11项新版本计数。天气读档外围汇总误期待WEATHER_SAVE_PASS，实际原生退出0／WEATHER_SAVE_RELAUNCH_PASS；从原日志纠正汇总并保留初始结果，未改断言／重用消耗后的检查点。
- 同一候选路线当前实测：6m/s去程337.36m／57.527现实秒／11.509游戏分钟，后巷返程301.15m／50.202秒／10.043分钟；17:35离家约17:46:31到站，假设停10现实秒后约17:58:36返家，17:40到站与17:55返家两个窗口均未满足。剧情可行性独立报storyWindowFeasible=false，不再因旧4倍预算硬断言中止其余物理／计时检查，不能把技术通过写成剧情通过。保留证言、人物速度和路网；后续剧情时间预算仍需校准。端点在室外门前、不含B站室内，住宅未实现；候选／地图近路不证明历史行程。全速直行／斜行、持图、阅读锁移动不停表、闭门碰撞／开门通行、钟楼101点上下楼和重启相位通过。
- 正常玩家PID40276正常退出后保存的最新档SHA7570031ca37f344b3f3168d9108a90202c03e2b347fa04c4b93e2a40b0fdc540为基线，验收后相同，4条材料和92.615826累计分钟保留，未回滚此前试玩；原review档恢复。旧包与正常档备份、原始日志／帧／旧4倍存档来源在忽略的builds/review/time-2h/，精简记录output/demo/time-2h/acceptance.json与README。已目视原生双钟、720p雨景、1080p黎明；非物理键鼠／盲测／低配／Mac或完整案件验收。
- 最终已从原入口1600×900正常打开PID58152，窗口“错日证词 · 暮色小镇”、Responding=true，日志确认slice-save.json且无异常，启动后正常档仍同SHA；未使用review或fresh覆盖进度。


### PUBLISH-01：用户授权上传当前游戏工作（2026-10-10）

- 用户在TIME-01完成后明确要求“直接push吧”。发布前CLI fetch确认main与origin/main同7f01ddb、0/0、没有新的远端提交，保留9a8cfe83恢复stash。发布范围为当前B站／蒸汽列车跨镇往返、实体票孔、可交互家具、贴面字牌、与上游合并后的场景／生成源，以及最新12倍／两小时昼夜；附可编辑Blender资产、项目设计资料（含已完成的成年路人基础概念）和精简验证记录。
- 发布前diff --check通过，新增可跟踪文件无100MB超限文件，构建包、Unity缓存、原始逐帧录像、日志与正常／review存档备份继续忽略。运行证据沿用各自明确的程序集：TIME-01同一新版本11项，加旧4倍源存档生成检查；此前铁路、票孔、家具、字牌与合并验收不冒充在两小时版本全部重跑。剧情窗口未满足的结果继续公开记录，不更改证言以掩盖预算问题。
- 本条记录用户发布授权、文件范围与已执行的发布前检查；最终提交编号和推送是否完成以命令行Git结果、远端main及交付消息为准，不把等待推送的状态写成已上传事实。

### RAIL-05：检票处落地细化、废票填充与实体车门（2026-10-10）

- **用户要求与基线**：检票处过于简陋、存在浮空物体，要求完整精细化；追加废票下面空洞和“打开车门”无反应。开工CLI fetch确认main/origin/main同ca395d0、工作区干净，保留恢复stash。已有两小时昼夜、证物合同、路网和人物进度保留。此前“直接push”已用于上一轮发布，本轮新工作未提交／推送。
- **支撑与造型**：旧悬空票格底部约1.298m，七本票册离支撑面约9–49mm，检票钳约38mm，并有重复账本／台铃。现由tools/rail/ticket_office.py统一生成落地木柜／绿色嵌板、黄铜脚杆及落地支脚、三个开放底部拱形服务口、黄铜窗格、立柱／门头／檐线。背柜有落地下柜、工作台、抽屉拉手、搁板／分格及真正落在搁板上的票册；收票盘、台铃、账本、墨水盘、检票钳按支撑面高度放置。两盏绿罩工作灯含底座／杆／灯罩／灯泡，暖光接入既有昼夜系统。原简化售票员未替换。构建根据实际导入Renderer边界验证39处接触和8处落地；50个支撑检查部件保留独立名称。
- **废票**：原票下方约.469m空洞现从底板.11m填至上层纸堆.696m，并加层叠碎纸／揉皱纸折；八张分散票分别由纸折支撑。早期平圆纸堆质感不足，最终换成破碎叠纸与分散票。废票保持每张三个实际孔洞、透空材质与原票面；主角T查看的E02仍无孔。桶标牌／检票钳碰撞触发范围覆盖实际模型，不额外授予证词或推理答案。
- **门无反应根因与修复**：旧逻辑反复调用全部打开，平台侧默认已开、另一侧旋转角为零，HUD又固定显示标题。现在八扇门独立开关，平台侧／另一侧均向外转100度，E提示随目标显示打开／关闭；每扇门一个真实叶片BoxCollider随门轴运动，修正FBX百倍导入比例。门叶与真实CharacterController胶囊占位检测使用旋转矩形距离，触碰时退回打开；首次ComputePenetration方案未通过实际占位检查，改用胶囊／OBB计算后两镇复验通过。发车从各门实际开度关闭，显式暂停冻结普通门动画。停靠初始化平台侧开、另一侧关，手动门状态目前不单独保存，不能宣称读档还原各门最后开度。
- **同源与资产保护**：BuildRail.ps1新增-StationOnly，仅导出StationB及支撑合同；完整生成器同样引用新票房。StationB最终134个源网格，editable blend／FBX／Unity资源／TicketOfficeGeometry.json一并更新。重开Blender5.2LTS，比较检票区域外207175个面（多重集合含重复计数）、材质、UV均相同：位置精度.0001m、UV精度.00001；列车blend与FBX哈希不变。该比较不代表无限精度位元相同。公共构建生成的钟表铺／钟楼Prefab与场景须一起保留，单独恢复旧Prefab会破坏fileID引用。
- **最终验收**：Unity6000.3.18f1 Windows/D3D11最终SLICE_BUILD_PASS，392380225 bytes，Assembly-CSharp SHA256 7c2f4539d265a12676cb663ee45a5364e459db7ed4eb01d2bd9d0493a1cd8475。32/32 EditMode通过，含浮空票册／检票钳／招牌／支柱及缺失纸堆故障注入、八叶实际闭／开门射线和胶囊占位。最后加入工作灯后，同一最终程序集六项原生检查全部通过：1080p／720p各完整铁路往返与独立进程到站读档，共四项；1080p家具／物品与独立读档共两项。真实控制器绕行票房、普通3m E命中售票员／钳／桶标牌；21字牌14本站＋7邻镇贴面与字号64→80→64；两镇两侧车门开关、平台占位防夹、两次完整跨镇；59休息位、8环境查看、2车灯开关。早期验收绕行点碰到桶的实际铁圈，改绕行路线，未减弱碰撞。最终白天／夜雨暖灯、背柜落地、检票钳、桶内穿孔／填充、关门／重开画面均已目视核对。
- **保存与范围**：本轮开始正常玩家PID40568退出后的最新存档SHA5850e1e6f99c4248eb04460f13b20f3857a23cedfca51451b6c5283e5409d1d6，最终六项验收后相同，Town／2次车程／累计164.2588185278949游戏分钟保留；没有回退TIME-01旧92.615826分钟档。检查使用隔离rail-review／items-review档，原件备份在builds/review/ticket-checkin/before。这是程序驱动的真实控制器、E射线及原生渲染，照片机位／时间／天气经过布置，非物理键鼠、盲测、Mac、低配或完整六顺序案件验收；TIME-01剧情窗口未满足结论保留。
- **协作与证据**：Antigravity在scratch只读审计旧模型支撑／射线遮挡，第一次读文件被拒、第二次成功；主代理按源码／源模型／实机核对，最终造型实现验收在本地完成，不把其所有推测写成通过。精简PNG／acceptance／EditMode XML／几何保存报告及说明在output/demo/ticket-office/；完整日志、失败迭代、原帧、旧资产／正常档备份在忽略的builds/review/ticket-checkin/。当前MVP记录同步更新。
- **正常交付**：两个原review档已恢复并逐字节核对，正常档未覆盖。由builds/windows/TownDemo.exe以1600×900窗口正常启动PID41540，Responding=true、窗口“错日证词 · 暮色小镇”，日志确认原slice-save.json路径且无异常；没有review／fresh参数或传送，用户从菜单继续已有进度。启动证明output/demo/ticket-office/normal-launch.json；之后正常游玩产生的新累计时间不可用开工备份回滚。

### PUBLISH-02：用户授权推送检票处与车门修复（2026-10-10）

- RAIL-05交付后，用户明确要求“push”。命令行fetch确认main与origin/main同ca395d0、0/0，无新增远端提交或文件变化，不需要合并。授权范围是本轮检票处细化、废票填充、实体车门、可编辑源及Unity同源生成资源、测试／精简实机记录和文档；恢复stash继续保留。
- 发布前diff --check、修改JSON解析、Unity新增源码／材质／合同的meta完整性通过，75个待提交文件均低于100MiB，构建包／缓存／原始日志／存档备份继续忽略。核对最终本地程序集仍为7c2f4539…，既有验收记录32项EditMode及六项同程序集原生检查全部通过；本轮仅发布，没有改玩法或重跑实机。
- 本条记录授权、发布范围和已执行的检查；最终提交编号与推送成功以命令行Git结果和远端main为准。

### RAIL-06：车站与列车美化、质感与旧缺陷修复（2026-10-11）

- **要求与基线**：用户要求“根据剧情和设计，优化美化车站和火车；保留现在的画风，可以加强质感”。开工fetch时main/origin/main 0/0；期间另一会话（旅馆用车）提交推送了RAIL-05为9cd22d4，并在同一工作区开发汽车。双方经跨会话消息约定互不改对方文件、错开Unity；本轮在worktree `D:/AllPersonalProjects/Chenchengame/game_test-railpolish`（分支station-train-polish，自9cd22d4）构建与验收，最终源文件逐字节复制回game_test。未提交／推送；未触碰builds/windows与正常存档。
- **实现**：新模块`tools/rail/rail_shapes.py`、`rail_station_beautify.py`、`rail_train_beautify.py`由`build_rail_assets.py`调用；`preview_rail.py`为Blender多机位建模预览。站房：拱石／拱心石、窗台线、拱脚线、花岗岩勒脚、隅石、门套、玫瑰窗石圈、托石檐口、屋脊花饰、两座烟囱、檐沟落水管、铁艺灯笼；候车厅木板天花、人字木与檩条、牛腿隅撑、球形吊灯与七盏墙灯、西北角火炉；后门玻璃连廊（新增WeatherShelter）；西翼行李房、东翼值班室按plan-data部件命名布置家具、天花和不投影吊灯，外加两块门楣字牌。雨棚：贴拱斜玻璃＋锌板下跨、格条、屋脊通风楼、放射端屏与挂檐、檐沟、铸铁柱线脚；新`WrongDate/ShedGlass`受光雾化玻璃。Unity：`RailMeshKit`米制UV网格、砖砌站台墙＋石板＋压顶石、后沿栏杆与检票口（双面钟读取共享时钟、双面字牌、讲台，不放检票钳）、站名牌（青川／邻镇）、消防桶、包裹、水鹤、臂板信号机；邻镇站台两盏既有路灯。列车：机车走行板／轮罩／火箱／铜帽烟囱／拱形司机室顶等，煤水车外翻沿与金线，客车象牙白窗带＋墨绿下部、金线、扶手、转向架侧架、拉杆、风挡、端门关闭，车内护墙、窗帘、地毯、小桌、头枕。20套程序化贴图与按材质光滑度。
- **修复的原有缺陷**：侧翼外山墙缺三角墙；侧翼窗边20cm透光缝；侧翼屋面／屋脊穿过主厅山墙在厅内成“人”字暗条；窗拱上悬空石块与铁饰、檐口托石浮空；厅灯／站台灯吊线不接结构；雨棚玻璃为阶梯状水平板；车轮1.9m轮距与1.435m轨距不符、轮底在地面而轨顶0.24m——现轮组整体上抬0.09m、轨道改到轮下且轨顶0.095m，车体／地板／踏步不变；行李车侧墙两端各缺0.9m；连杆曲柄0.14m且逆向——现以轴心为圆心0.30m顺时针；**RailGlow材质被保存成无`_EMISSION`关键字，所有铁路灯玻璃从未真正发光、DayNightLamp也没写入**——RailMaterial现对发光材质每次重设关键字、发光色与RealtimeEmissive，图集诊断确认全部铁路灯接管；`set_module`同名重入会生成`Module_Hall_Lamps.001`使厅灯未绑定——改为复用同名模块（客车`Module_C*_Detail.001`也随之合并）。
- **验收**：Windows Unity6000.3.18f1／D3D11，staging构建431655660字节，Assembly-CSharp SHA256 280AE6DF2F44E6307B93C840AAD6E943353430488B86943F054C97C1EDFCB602。同程序集10项原生检查通过：图集31机位（含灯具发光诊断）、rail1080／720完整往返（检票处、20+11字牌贴面与字号变化、车门防夹、两次跨镇）、两档独立读档、items 59座位／8查看／2灯开关及读档、day-night、weather、full-map（6种访问顺序）。EditMode 32/32；首轮31/32为窗间柱开洞测试只禁用首个碰撞体，而柱内新增护墙板——测试改为禁用该处全部碰撞体，ValidateTrain未改。期间修过：QINGCHUAN被新檐沟仰视遮挡（正面檐沟在山花处断开）、讲台变量名冲突、窗帘过宽、站台灯强度3.5→4.5（范围12，数量与阴影预算不变）。
- **存档与证据**：正常slice-save.json最后写入2026-10-10 23:36:14（早于本轮所有复查），SHA256 DA36CC066E9532BF0C0A6958221DB2BF03824FA20903187B7B1BE1E5625901F4；全部复查用隔离review存档。精简证据`output/demo/rail-beautify/`（改前改后对比、30张图集、native结果JSON、EditMode XML、acceptance.json）。改前图取自已提交RAIL-02／RAIL-05截图；本轮最早截的一组改前图在scratch中丢失。
- **回归规则**：铁路字牌现为青川20／邻镇11（RailSignsReview与ReviewRail.ps1已同步）；改发光材质须确认`_EMISSION`在保存后的.mat里存在并跑图集诊断（`--review --fresh --review-rail --rail-gallery`）；生成器中同一模块可多次`set_module`；车轮中心=半径+0.09、轨距=车轮横距1.9m，改轮组时同步Unity轨道；ShedGlass与ClearGlass一样不加碰撞体；检票口开口宽3.4m对齐坡道，复查路线沿x=0穿过。
- **后续**：汽车会话完成后已按用户要求合并重建正常入口并推送，见PUBLISH-03。本轮未做Mac构建或实测（本机Unity无Mac Build Support），Mac能否启动最新版尚未验证；非物理键鼠／盲测／低配帧率验收。

### CAR-01：旅馆用车——可驾驶的1928–31风格四门轿车（2026-10-11）

- **用户要求与决定**：要一台可运行、真实物理、可驾驶、交互点多（如开后备箱）、符合年代的车，并要求先出示意图。会话内展示侧视交互点、驾驶座视角、俯视物理布局三张示意图后，用户选：旅馆用车（B08前庭车马院，约169.5,2.0，车头朝北）、自动三档＋真实启动、暗栗红车身配黑翼子板。开工fetch：main=origin/main=9cd22d4、无远端新提交；同工作区另有车站／列车会话（RAIL-06），双方约定各改各的文件、跑Unity前互相通知、汽车会话不提交不推送。
- **叙事边界**：原作1928年原主分心驾驶致孩子重伤、情夫是修车匠（B07里间有其工作车）、20日另有煤车；所以车归旅馆，不放修车铺或主角家，不进证据合同／证词，现在能开车不证明任何人20日开过车，也不改变17:35–17:55窗口结论。
- **实现**：`tools/car/build_hotel_car.py`（Blender5.2）生成可编辑HotelCar.blend/FBX与布局合同`hotel-car-layout.json`（45个可动枢轴、42个交互点、盒碰撞、座位／下车点、车轮、灯位、挡雨顶），`BuildSlice.Car.cs`按合同装配`HotelCar.prefab`并放在Town几何组外（不进导航烘焙）。运行时`HotelCar.cs`（WheelCollider四轮、1100kg、L头四缸扭矩曲线、3.11/1.77/1.00自动档与倒档、机械鼓刹／手刹、雨天抓地降至0.72、防倾、点火＋起动钮＋冷车阻风门、摇把手摇与火花提前反弹、阅读／暂停冻结车辆物理、翻车推正、出界回车位）、`CarPart/CarControl/CarSeatPoint/CarSounds`（原创程序音：四缸、起动机、喇叭、胎噪、雨刷、门、棘轮）、`DemoApp.Car.cs`（上车／换座／下车、座位视角、司机HUD、驾驶中Q/M/T需停车拉手刹）、`WindshieldRain.shader`（雨滴与雨刷扫净扇区）。共享改动：SliceSave新增车位姿／座位／热车时间；E在车内未对准控件即下车；`MovementLocked`、列车与家具入口排除在车内；`WeatherShelter.vehicle`使车门窗／风挡打开时恢复室外雨声；`TownLightBudget.Reserved`让大灯与车顶灯占12光池名额；`TownSound`停用脚步于禁用的控制器；manifest新增`com.unity.modules.vehicles`。
- **踩过的坑**：同一文件多个MonoBehaviour存进预制体会丢脚本（拆成一类一文件）；WheelCollider静止在targetPosition，悬挂抬高应为距离×0.5（0.09m）而不是加静压；座位眼点不在转体轴上会让瞄准偏5cm；下车点须在脚踏板（x0.88）外且离开门扇0.3m以上（最终x±1.30、前z−0.36、后z−0.66），并允许小幅挪位；站立检测须忽略0.24m以下路缘；交互目标做成触发器且不能重叠；发动机罩侧板改为整片翻到顶上（270°），否则从车旁看不见发动机；箱内物品按站立视线重排。
- **验收**：Windows Unity6000.3.18f1／D3D11，staging `builds/car-staging`（433215021字节，Assembly-CSharp SHA256 42628084ea623f633fa9dd4f34b0cb9140b06bb6db0aa09d435eb4ae67d65d29，含RAIL-06源）。EditMode 53/53（含21项汽车几何／视线／重叠测试）；同一程序集原生：1280×720与1920×1080完整汽车验收、独立进程读档（恢复驾驶座与全部车况后起动开走）、`--review-items`与`--review-rail`回归，全部PASS。指标：0–30km/h 4.83s、湿路33km/h刹停8.16m、撞墙16.6km/h停在墙前、倒车2.2s 5.1m、33个不同交互点、车轮接地误差0.9mm。证据`output/demo/hotel-car/`（README、acceptance.json、结果JSON、EditMode XML、Blender预览与实机截图）。程序驱动脚本输入代替键盘，翻车由脚本摆放；非物理键鼠／盲测／Mac／低配帧率；weather／day-night／full-map全套未在此构建重跑。
- **存档与交付**：正常slice-save.json前后SHA256均为DA36CC06…01F4，全部检查用隔离car-review-save.json；车站会话的items/rail review存档先备份后原样恢复。正常入口builds/windows未重建，由车站会话在合并origin/main后统一重建；汽车会话未提交／推送。示意图中“行李架折起”“脚踏板可踩上”未实现（行李箱固定在架上；角色无跳跃、台阶0.24m）；无NPC车流、损伤或油耗。

### PUBLISH-03：铁路美化与旅馆用车合并重建并推送（2026-10-11）

- **授权与顺序**：用户要求“等汽车结束合并到最新然后push”。汽车会话报告完成并给出文件清单与提交说明后，CLI fetch确认main=origin/main=9cd22d4、无远端新提交。本会话以合并后的game_test运行`tools/BuildDemo.ps1`重建正常入口builds/windows（旧包与正常存档先备份到忽略的builds/windows-before-rail06/）。
- **集成验收**：SLICE_BUILD_PASS 433215917字节，Assembly-CSharp SHA256 42628084EA623F633FA9DD4F34B0CB9140B06BB6DB0AA09D435EB4AE67D65D29（与汽车会话car-staging相同）。EditMode 53/53（铁路32＋汽车21）。同程序集原生：rail 1080完整往返（字牌20＋11、检票处、车门、两次跨镇）与独立读档、items 59座位及读档、`tools/ReviewCar.ps1`汽车1080复查与独立进程读档均PASS；精简记录在output/demo/rail-beautify/integrated/。正常slice-save.json前后同为DA36CC066E9532BF0C0A6958221DB2BF03824FA20903187B7B1BE1E5625901F4。
- **提交范围**：分三次提交——车站／列车美化（RAIL-06源、贴图、生成网格／材质／预制件、证据）、旅馆用车（汽车会话的源、资源、测试与AGENTS规则，按其提供的说明提交）、集成（BuildSlice.cs两边入口、共享文档、重建后的场景／导航／预制件）。builds、缓存、原始日志与存档备份继续忽略。最终提交编号与推送结果以命令行Git与远端main为准。
- **未验证**：Mac未构建未实测；worktree `game_test-railpolish`在推送后清理。

### TOWER-10：钟楼夜景——背光钟盘、投光、门灯与光照层（2026-10-11）

- **用户要求**：“夜晚钟楼很黑暗不够好看，加强质感和氛围感”。开工 CLI fetch：main=origin/main=63e93a9，0/0，无远端新提交。改前实机（隔离 `look-gallery-review-save.json`，固定机位）确认：夜间塔身是一块暗板，乳白钟盘是不受光的平白圆片（像贴纸），门洞黑。
- **实现（共享生成源）**：`BuildSlice.TowerNight.cs` 在同一 ClockTower.prefab 内加：`FrostedClockGlass` 新增 `_EMISSION` 背光（暖色 1,.77,.50×1.75，中心亮/边缘暗/两盏灯热点，雾中衰减但比砖墙更能穿雾；白天未亮灯时外观不变）；每面钟室顶两盏铜罩背光灯（只作发光体）；每面钟前 1.35m 一盏无阴影溢光（9.5/5.4m）照钟框、角柱、托梁、封檐板；`WrongDate/LampHalo` 雾中光晕（动态、立方包围盒，不能静态合批，近处淡出）；塔门两侧马车式壁灯（1.5/4.6m）；广场 4 根既有灯柱上的铸铁投光器（聚光 72/32m/62°）。全部 `DayNightLamp` Exterior 感光，无新增阴影灯。
- **光照层（关键）**：无阴影灯会穿墙。首轮壁灯在 Default 层，实测塔内首层画面平均亮度 36.90→43.20（墙面暖色漏光）；改为投光/溢光只照 Light Layer 1（外壳 FBX），壁灯只照 Light Layer 2（外壳、木门、铭牌、门槛、门前 8 块路面），复测首层 36.90→36.90、中层 43.85→43.85（差值≤0.3 噪声）。投光与溢光 `sharesStreetPoolBudget=false`，不占 12 路灯光池；`FullMapReview` 的光池断言同步只数参与预算的灯。
- **钟盘可读性**：背光会冲淡机房内看到的外侧指针影，已按深度近邻系数压低内侧背光（`1-.85*nearby`），机房内仍可读针。
- **旧检查纠正**：`--review-clocktower` 的“除塔钟外不得有准确钟”在现行发布版就失败（车站检票口/站房钟按 RAIL-01/06 与 MVP 明确读取怀表时间）；已用发布版 `builds/windows` 复现确认非本轮引入，检查改为只允许 B17 车站内的准确钟。这是否符合叙事意图留给用户确认。

### RAIL-07：车站后门出站口地面 z-fighting（2026-10-11）

- **原因**：`tools/build_full_town.py` 把 plan-data 中 B17 后侧步道 (188,52)→(188,61)、宽 1m 的 walk 面（y=0.10）铺在车站预制件自带的 `StationConcourseFlags` 石板地坪（顶面同为 0.10）下面，候车厅后门外出现闪烁窄带。坡道脚 y=0.10 与地坪同高但以 7% 坡度分离，不是主因。
- **修复**：编译器将车站地坪轮廓（站点根 (188,46)，局部中心 (0,11.775)，68×11.55m）并入挖孔，重编译后只少 walk-3-1 两个三角形、院落各少 1–3 个，其余 TownPlan 数据逐项相同；`FullMapChecks` 新增“车站地坪下不得有共面街面三角形”断言。固定机位前后对比 `output/demo/tower-night-daylight/station-exit-zfight.jpg`。

### CAR-02：车内掏出纸地图飞到车顶（2026-10-11）

- **原因**：`HeldTownMap` 纸面位置写死在身体坐标 1.06–1.13m 高（按站立眼高 1.66m 设计）；车内眼高 0.72m，纸面比眼睛还高约 0.35m，贴在车顶。列车座位（1.10m）与家具座位同类问题较轻。
- **修复**：纸面加上“静止眼位−站立眼高”偏移（忽略 ±0.016m 走路晃动），任何座位都保持在胸前同一相对位置。汽车验收停车拉手刹后打开地图，断言纸面相对眼睛 (0,−0.537,0.610)（与站立相同），截图 `car-map-seated.jpg`；之后恢复原坐姿，避免改变后续手刹射线检查。

### LOOK-01：白天质感光影与可见的太阳（2026-10-11）

- **用户要求**：“帮我做白天的质感光影增强，可以学习别的游戏，目前看着很plain”，随后问“有太阳光和太阳吗？没有的话需要加一下”。答复：一直有单一天体光（06:00 日出／18:00 日落美术轨道）和天空日轮，但已确认的天气表白天以厚云为主，天空着色器在云层混合之前画太阳，云量≈1 时被完全盖住，阴天直射光≈0.02，所以看不出。保持 AGENTS“白天太阳柔暗、不恢复刺眼晴日”。
- **参照手法**：阴天街景常见做法——强环境光遮蔽与顶亮天空环境光塑形（Dishonored 一类）、雨后湿面光泽（The Last of Us／RDR2 一类）、近中性分离色调（Firewatch 一类）；只借手法，不复制资源。
- **实现**：`DayNightCycle` 厚云时给同一盏天体光加 `SkyKey`（近中性冷白、淡阴影），白天环境光顶亮（.75,.79,.85）、墙中（.52,.54,.56）、地暖暗（.31,.28,.24），夜间值不变；`DuskSky` 在云层混合后加淡白日轮与光晕（随云薄厚、雨中更淡）；`BuildSlice.DayLook.cs` 设 SSAO .95／.55m／直接光 .30、Town 体积近中性分离色调、对比 13、曝光 .28；占位房屋改用 `tools/make_placeholder_textures.py` 生成的两层立面图集＋法线（勒脚、返潮、窗套、窗台、百叶、雨痕、檐口阴影）、四种灰泥色（按 id 稳定哈希）、法线石板瓦和灰泥山墙（`Roof` 新增山墙子网格，其他调用者两槽同材质外观不变）；`TownWetness` 按天气表推算湿度（36 游戏分钟衰减），给 20 个共享材质加暗与光泽，销毁时恢复。
- **调色纠偏**：首轮分离色调过强，阴天平均饱和度 .12→.31～.37、蓝偏 b−r +.03～.05→+.10，画面发蓝青；改为近中性后阴天 b−r +.037～.059（改前 +.033～.055），饱和度 .12～.17，p5/p95 动态范围加宽。以后改调色必须量化对比，不能凭单张截图。
- **夜间与室内影响**：夜景外观亮度/色偏基本不变；塔内因 SSAO 加强暗 5～13%，旋梯与上层脚印仍可读（`interior-ao` 对比已目视）。
- **agy**：立面/石板纹理生成器曾委派 Antigravity（scratch 目录、`--dangerously-skip-permissions`），约 8 分钟无任何输出，主代理停止该进程并自行完成脚本；仓库中纹理与脚本均为主代理所写。

### TOWER-11：钟楼进门地面 z-fighting（2026-10-11）

- **原因**：`BuildSlice.ClockTower` 的入口门槛石 `Tower entrance threshold` 顶面 0.12、z −2.07…−1.37，向塔内伸入约 15cm；塔内底层石板 `Ground floor` 顶面同为 0.12、边缘在内墙面 z −1.525，两者在门内重叠共面。
- **修复**：门槛只覆盖门外台阶和墙厚，止于内墙面前 5mm（z −2.07…−1.53），碰撞同步缩短，5mm 缝远小于玩家胶囊；新增 `TowerFloorCoplanarCheck`，门槛与底层石板同高且水平重叠时构建失败（旧几何必然被拒）。塔夜景图集新增门内三张贴地掠射机位（threshold-inside／grazing／noon）用于前后对比。

### 过期验收断言（2026-10-11 核对）

- `--review-clocktower` 的“唯一准确钟”与 `--review-day-night` 的“所有点光都受昼夜调度”在现行发布版 `builds/windows` 上本就失败：前者遇到 RAIL-01/06 明确读取怀表时间的车站钟，后者遇到 CAR-01 由玩家开关的车顶灯。已在发布版复现后再收窄断言（车站 B17 内的钟、HotelCar 下的灯除外），其余条件不变；车站钟是否该给玩家另一个准确时间源，属于叙事判断，待用户确认。

### 验收与交付：TOWER-10／TOWER-11／RAIL-07／CAR-02／LOOK-01（2026-10-11）

- **最终 staging**（`builds/windows-staging`，Assembly-CSharp SHA256 `007B3004834BFC6AA02C80BB76FC774B880C0ED6BED08EED1E9AB895CF855098`）：构建 `CLOCKTOWER_FLOOR_COPLANAR_PASS`、`FULL_MAP_AUTHORING_PASS`（含车站地坪共面断言）、`BUILDING_LAYOUT_PASS`、`SLICE_BUILD_PASS`。同程序集原生 1080p：夜间钟楼完整 101 点上楼／调时 124.5s 与独立进程读档下楼 65.4s、day-night 30.5s 与读档 6.6s、全镇 11 道路／6 种访问顺序 559.1s、两套固定机位图集，全部 PASS。
- **上一 staging**（仅差门槛修剪与两条过期断言收窄）：汽车完整验收 82.4s（含车内地图断言）与读档 11.9s、铁路往返 138s、天气 23.7s 与读档 6.2s，全部 PASS；该轮 day-night 两项因车顶灯旧断言失败，发布版同样失败，收窄后在最终 staging 通过。中途一次汽车验收在撞墙后手刹射线擦过（插入的地图检查改变了坐姿朝向），已在检查后恢复原坐姿并复跑通过。
- **存档**：正常 `slice-save.json` 全程 SHA256 `BDB288E663F3AEA269F68C49C81917121B775B50A9BE2C4E641B945BEA1DD83A`（最后写入 12:39:44，早于所有复查），所有检查用各自隔离 review 存档。
- **交付**：正常入口 `builds/windows/TownDemo.exe` 已备份至忽略的 `builds/windows-before-tower-night/` 后用同源重建（Assembly-CSharp `D3C7733B…225D9E`，与 staging 不同仅因之后改了一处注释和编辑器整理），图集冒烟运行 PASS。精简证据与 `acceptance.json` 在 `output/demo/tower-night-daylight/`；原始帧与日志在忽略的 `builds/tower-night/`、`builds/day-look/`。未提交／推送；未构建 Mac；非物理键鼠／盲测／低配帧率验收。

### PUBLISH-04：用户授权推送钟楼夜景、白天光影与三处修复（2026-10-11）

- 用户在交付后明确回复“push吧”。CLI fetch：main=origin/main=63e93a9，0/0，无远端新提交，无需合并。提交范围：TOWER-10／TOWER-11／RAIL-07／CAR-02／LOOK-01 的源码、着色器、生成源与纹理、共享生成器重建的预制件／场景／导航／网格、文档与技能、`output/demo/tower-night-daylight/` 精简证据；builds、原始帧、日志与存档备份继续忽略。提交前检查无大于 20MB 文件、无凭据模式、无孤立 .meta。实际提交编号与推送结果以 Git 与远端 main 为准。


### NPC-04：Tripo 送奶少年、双手抓肩带、抬头看人与牛奶店补光（2026-10-11）

- 用户要求拉取最新、总结、构建 Mac，替换送奶少年，抓肩带紧张动作为默认待机，对话时用说话动作，移到柜台前进店可见并录游戏视频。随后明确要求调亮房间，以及待机/说话都基本抬头看玩家。用户说的是肩带，不是包盖。
- CLI 从 7f01ddb 快进到 e079779（6 提交，车站/列车/两小时昼夜、旅馆车、钟楼夜景、白天光影及修复）；再次 fetch main=origin/main，0/0。保留全部本地工作。无提交/推送。
- 来源：用户既有 Tripo v4 源、T 形参考修复版本；tools/import_tripo_milkboy.py 导入 FBX、原纹理及打包纹理 Blender 源到 assets/source/milkboy/tripo 与 Assets/Art/MilkboyTripo。tools/milkboy_raise_gaze.py 将两动画 Neck/Head 改为基本抬头（基础 14°，小幅侧看 3°，轻微倾斜 1°）。head-up-preservation.json 确认其他动画、Jaw/双手/身体曲线及网格权重不变。旧 Meshy、Toon Boy 和原始 Tripo 交付目录均保留，不虚构授权。
- Unity：有效 Humanoid，68 骨骼（Jaw），2 个原地循环动作，默认 02_Nervous_HoldShoulderStrap，E04 少年台词用 01_Talk_WithMouth；.25 秒转换，按台词长度 2.5–8 秒播放后回待机；玩家发言、历史、退出停止说话，显式暂停冻结动作。嘴部为通用下颌/嘴唇开合，没有配音或逐音素同步。MilkboyPerformance 在 LateUpdate 平滑看向前侧 6m 内玩家，转头限制 ±55°、抬头 5–38°，只调头，不改手和身体。
- 共享 MilkShopAsset 站位 (.15,.18,.15)，世界约 (-144.229,.122,7.075)，朝入口；旧位置太靠门曾仅见帽子，已移近柜台。柜台测试旧直线路径会撞上新 NPC，现通过实际顾客过道绕行，保留碰撞及普通 3m 射线，不降低断言。接近角色测试改用身体 -Z，不用随玩家转动的 headfront 作为站位方向。
- 用户指出脸黑：MilkShopInteriorAsset 将两吊灯 4.8→3、阅读灯 1.2→.75，新增两处无阴影暖中性间接补光（2.2/.7，范围4.3m，采光系数.48）。Light Layer 3 限定店内/少年，避免无阴影灯穿墙照街道；共5盏无阴影灯，沿用天气/昼夜与阅读灯手动关灯优先。晴/雨正午、傍晚与夜间实际画面脸/嘴/手清楚，未改全局曝光。
- 一次自动对话回看检查在桌面活动期间出现不稳定失败，不能据此断定玩家流程坏；现 review 模式隔离真实键鼠/GUI事件，仍调用同一普通对话处理函数；正常模式输入不变。最终720p/1080p对话含回看均通过。
- 最终同程序集 3feb8cdfe4b87aa42268b3c26618b38e5fa5bea93039854ebc667404c5dbcc84：9项 Mac/Metal 原生检查通过（人物含真实进店/两动作/看人/录像，对话720/1080及独立读档，牛奶内饰720/1080及独立读档，双门声学灯控及独立读档）。待机6秒视线偏差最大2.02°，说话采样.06°，Jaw变化7.39°，根位置不变。目视正/侧面和帧序列未见旧错误权重导致的明显衣包拉长，不能声称全部角度绝无轻微穿插。
- 交付：builds/macos-current/错日证词-新版.app 和“错日证词-Mac-送奶小孩抬头与灯光版.zip”，Universal x86_64+arm64，原/解压严格签名通过，原桌面入口仍指向它。证据与21.41秒/8.60秒两段原速实机视频在 output/demo/milkboy-tripo；原始帧/完整日志/失败轮次/原包备份在忽略的 builds/macos-current。正常存档 SHA256 前后均 95c7a0cb4798ae63f4515648f1f2aa386a807c6f86ad9c8c33f014828f175c3a，review 存档原样恢复。非物理键鼠、盲测、Intel实机或低配验收，铁路/汽车/全镇全套未在本包重跑。
