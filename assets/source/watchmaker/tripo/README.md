# Tripo 钟表匠源文件

Tripo 资产：373ab2a6-bf3d-43a4-9c4a-a20d63e69697。用户手动上传参考并生成1970956三角面原模型；Tripo优化为10060面/9505顶点。原下载在 original/，白发/目镜/围裙造型来自生成模型。参考、提示词和网站截图保留。

Tripo自动绑定未成功；tools/build_tripo_watchmaker.py本地制作56骨骼、坐姿围裙/手部权重、怀表和螺丝刀、三动作。Watchmaker.blend含打包原纹理与三段可编辑动画。Unity FBX及2048纹理在game/Assets/Art/WatchmakerTripo/，还有有效的WatchmakerHumanoid.asset。当前精确接触动作通过Generic播放；需要Humanoid重定向时导入器可使用骨骼映射，重新检查工具与手部接触。

复现：python3 tools/import_tripo_watchmaker.py --archive <原始Tripo导出.zip> --blender <Blender可执行文件>，随后Unity BuildSlice.BuildFreshMac。该入口组合本轮已执行的提取/纹理转换/建模/源检查步骤。tools/verify_tripo_watchmaker.py独立重新打开源检查三个动作。

原Meshy模型与旧椅子源保留。Tripo导出未附授权文件，不继承旧角色许可。实机结果在output/demo/watchmaker-tripo/。
