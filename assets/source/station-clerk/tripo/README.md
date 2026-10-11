# 青川车站售票员 · Tripo

中年男性、墨绿制服、硬檐帽、小胡子、黄铜扣和腰包，基于用户确认的角色概念。建模参考为 `reference-tpose.png`，由内置 image_gen 从中年版本转为 T 形站姿；完整提示词在 `reference-prompt.txt`。

Tripo 资产：`7ef008b1-0a11-4338-a813-c65ba9636e73`。H3.1 原生成 1898222 三角面，经 Tripo 四边形重拓扑为 9821 面、9272 顶点，Unity Humanoid Compatible 自动绑定成功。网站原始下载、8K 基色和 4K PBR 保留在 `original/`；制作过程预览为 `tripo-animation-preview.jpg`。

两个动作来自 Tripo：普通 `idle`（15.333 秒）与文本生成的 `Clerk Talking to Passenger`（4.958 秒）。`tools/prepare_tripo_station_clerk.py` 保留自动绑定与上身动作，统一两段的下半身站姿，修整循环接缝；添加 Jaw、局部嘴唇权重和口腔内衬，只在说话时开合。没有配音或逐音素同步。可编辑 `StationClerk.blend` 打包原四张纹理，最终 69 骨骼、9408 网格顶点；Unity 使用 2048 贴图与两段 Generic 动画，另保存并验证可复用的有效 Humanoid Avatar。

复现：`python3 tools/import_tripo_station_clerk.py --archive <Tripo导出.zip> --blender <Blender可执行文件>`，再运行 `tools/verify_tripo_station_clerk.py`。之后用 Unity `BuildSlice.BuildFreshMac` 从共享生成源重建。

`StationClerkAsset.Create` 在本镇 `StationTown` 隐藏原 `Module_TicketClerk`、保留旧源，并于原柜台后局部 `(-8,.12,1.9)` 放置新人物。保持原售票员身份、说明文字和 E03/E08 自由查阅；没有移动至站台检票口或添加历史证词。`StationClerkPerformance` 在售票员台词期间切换 Talk，回看、结束或发言结束后回 Idle，显式暂停冻结。对话使用现有底部透明界面。

实际实机检查、截图和原速原声录像在 `output/demo/station-clerk/`。完整日志与逐帧源在忽略的 `builds/macos-current/verification-station-clerk-20261011/`。原正常存档和旧应用备份在 `builds/macos-current/backup-before-station-clerk/`。测试采用隔离存档；不把脚本驱动的真实控制器和交互射线称为物理键鼠或陌生玩家盲测。

Tripo 导出没有附带许可文件，来源记录不继承此前 Meshy、Toon Boy 等旧角色许可。
