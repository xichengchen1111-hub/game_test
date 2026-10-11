# 中年售票员 · 游戏接入验收

已替换主镇青川车站原售票员，保留原柜台位置和检票／登车说明。角色为Tripo生成的墨绿制服中年男性；两个动作是普通站立待机与交谈手势／嘴部开合，结束、回看或发言结束回到待机。现有E交互与底部对话界面保持。

Mac唯一入口：`builds/macos-current/错日证词-新版.app`，桌面原链接仍指向它。Mac压缩包为`builds/macos-current/错日证词-Mac-车站售票员版.zip`；本目录的`售票员-Tripo-模型与动作源文件.zip`包含可编辑打包Blender源、Unity FBX/贴图、原始Tripo导出、制作脚本、参考和提示词。

- `01-售票员待机与对话.mp4`：22.65秒真实游戏待机、交谈与恢复，原速原声。
- `02-售票员嘴部动作近景.mp4`：4.83秒受控面部近景，通用开合，没有配音或逐音素同步。
- `clerk-talk-inspection-rule.png`：普通玩家视角和底部交谈界面。
- `clerk-side-full-body.png`：柜台后全身、手／衣服／腰包与鞋底。
- `clerk-face-*.png`：晴昼、雨昼与夜间的角色材质／灯光。

最终Unity6000.3.18f1、macOS Metal、Apple M5 Pro；Assembly SHA256为`684e799e1f045116ea8aeef726697fa8c67842401b077cd5800d4aa240a170e4`。54项EditMode全部通过；六项同程序集原生检查全部通过：1080p／720p售票员、独立售票员读档、检票处交互、送奶少年与钟表匠回归。普通3m E射线、两动作、嘴部、回看、退出恢复、对话时间继续与显式暂停已检查；角色根和脚部漂移均0。

来源为Tripo H3.1生成、重拓扑和成功的Unity兼容人形绑定，普通idle与文本生成的Clerk Talking to Passenger。局部固定下半身／循环接缝及Jaw口型在Blender制作；详细来源见`assets/source/station-clerk/tripo/README.md`。源验证与实机验收互相独立，不能把照片或骨骼数量当玩法通过。

本轮是程序驱动真实控制器、正常射线和共享对话处理函数；面部／全身特写为受控机位。没有物理键鼠、陌生玩家盲测、Intel／低配验收或全镇／完整铁路往返重跑的声明。正常存档SHA256前后相同，review存档恢复，旧应用完整备份。完整日志、逐帧素材和首轮驱动参数诊断保留在忽略的`builds/macos-current/verification-station-clerk-20261011/`。
