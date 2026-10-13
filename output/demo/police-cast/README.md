# 警署人物制作进度（2026-10-13，第一轮未接纳）

用户已指出值班警员的卡通画风和手臂穿腰包问题。下方旧图仅保留制作记录；值班警员与看守均已拒收，其他三人待统一画风复核。不要继续把这批模型直接导入游戏。新版二维参考和生成提示词在 `assets/source/police-cast/revision-02/`，用户已确认沿用新版参考画风，但尚不代表三维穿模已修复。用户确认允许后，新版图片上传重试仍被浏览器权限检查拒绝，三维重做尚未开始。看守也已按用户要求修订为欧美人物参考；五人新增真正行走／对话动作要求，旧的只有idle+派生talk方案已不满足。

五个独立 Tripo 人物已经生成、减面、Unity 人形绑定，并预览 15.33 秒 idle。这里的 JPG 是 Tripo 网页预览，不是游戏截图。原包下载被浏览器权限检查拒绝，用户明确允许后重试仍被拒；未采用替代通道。当前游戏仍为上一轮可玩的第一幕版本，尚未替换新人物。

**五人的本地模型和可玩版本仍未交付真正的 Walk／Talk。** 已发现的旧 `DutyOfficer-Tripo-idle.zip` 只有 idle；后续五人网页已应用 Walk，新的 `Calm Conversation Gestures` 身体交谈动作也已生成（4.96 秒，20 积分），其各角色应用／导出状态见制作记录。网页动作、导出选择与本地导入是不同完成条件，不能只凭网页会动就记为游戏中已具备动作。

对这个 DutyOfficer 旧包的实际检查发现，没有下颌／嘴唇骨骼或 shape keys；只有身体交谈手势不能证明嘴部会开合。该结论仅针对已检查的旧包。新模型到位后需逐人核查；若缺失，按各自脸型补局部下颌／唇缝和通用说话曲线，并检查完整动作及过渡。既有 24 项程序检查只验证动作切换等机制，不验证五人真实 Walk／Talk、嘴部或穿模；目前也没有语音同步验收。

- [律师](lawyer-tripo.jpg)：瘦高、无帽、三件套西装。
- [看守](guard-tripo-idle.jpg)：壮实体型、方脸、无胡须。
- [值班警员](duty-tripo-idle.jpg)：较矮偏胖、灰胡须、年长面孔。
- [巡警](patrol-tripo-idle.jpg)：高挑女性、短发、长裤。
- [接待女警](reception-tripo-idle.jpg)：较矮女性、圆脸、裙装。
- [下载交接窗口](download-handoff.jpg)：FBX / Blender、8K、骨骼、1段idle、原地动画已选；历史交接图，此候选现已拒收。

本地通用导入工具、角色装配和静音验收代码已准备；Unity6000.3.18f1两次编译退出0。实际新资产导入、可编辑Blender生成、坐姿校准和游戏内验收均未进行，不能记为已完成。美术与完整动作检查通过、并明确录入 acceptance.json 后才能启用。新的合格源包到齐后用 tools/cast/import_period_character.py 逐个转换，然后 OpeningAuthoring.BuildMacFinal，执行 --review-police-cast、完整开场、送院及读档回归。全部检测 AudioListener.volume=0。正常存档与已有可玩应用未修改，未提交推送。

角色ID与面数见 assets/source/police-cast/production-status.json；年代取舍和站位见 docs/police-cast.md。Tripo后台不同行为可能同步切换所选资产，后续串行处理，并在最终Export前核对人物及原包内容。
