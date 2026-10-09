# 天空与天气实际验收

2026-10-09，Unity 6000.3.18f1 / URP 17.0.1，Windows D3D11，RTX 5060 Laptop GPU。`sky-weather-comparison.jpg` 是六张最终播放器画面的等比缩小组合，没有改色或 AI 修图。原始 1080p/720p 帧在对应目录。

## 已实现

- 两层随同一游戏时钟移动的云：低云团、高空薄云，云边透光、云底阴影、太阳方向暖色低空、星点、月面与月晕。太阳/月亮方向与场景同一盏方向光一致；云遮阳会柔化并减弱场景直射光。
- 晴、多云、阴、雨、雾采用确定性慢变化；每段 210 游戏分钟，55% 保持、45% 渐变。雨在云量达到 65% 后渐增，充分覆盖后达到完整雨量，散云时渐停。变化不绑定电脑墙钟，退出不会离线推进。
- 256 根上限的世界坐标雨丝，近远两种范围，范围边缘渐隐、固定垂直落雨相位；玩家移动时保留视差。屋顶查询覆盖雨丝两边和倾斜尾端，屋内保持遮雨，室外雨仍可通过透明窗查看。雨声在屋内变轻，查看材料时与雨、云一同暂停。
- 路灯依太阳昼夜在白天关闭；室内自然光估计受阴雨影响，光线不足自动补灯。手动关闭的阅读灯不会被天气强行开启。原有两盏阴影吊灯、AO、门窗、钟楼可读性与 +30 分钟塔钟差值保留。

## 本次实际结果

同一最终构建共 12 项通过：天气 1080p、天气 720p、天气独立读档、昼夜、昼夜独立读档、牛奶屋、游戏时钟、钟楼夜间路线、钟表铺，以及牛奶屋／时钟／钟楼各自独立读档。明细与最终程序集 SHA256 在 `acceptance.json`；日志在本地 `builds/review/weather/`。原始正常存档 SHA256 与修改前一致，所有自动验证使用独立的 review 存档。

天气检查包含实时自动变化、太阳与灯光方向一致、雨丝世界视差、全部可见雨顶点的屋顶遮蔽、晴天室内关灯与雨天补灯、白天室外关灯、普通订单交互真正暂停天气、独立新进程恢复保存相位。没有改历史天气、E01–E10、订单两条已确认记录或调查推进。

两侧窗在每种分辨率均用实际控制器横移约 0.94m，截取 13 帧。`rain-window-north.mp4` / `rain-window-south.mp4` 是 10Hz 采样短片；这个采样率不是游戏 FPS。固定窗框探测区亮度范围约 0.030–0.032，结合全帧检查未观察到新增突闪；纹理、镜面反射与正常灯光变化会产生缓慢差异，不能据此宣称所有显卡没有闪烁。

这轮验证为正常渲染的 Windows 播放器、脚本控制的 CharacterController 和普通交互射线，时间/天气固定仅用于独立 review；不是物理键鼠盲测、Mac 测试或低配 60FPS 基准。低配目标通过分层天空、256px 噪声、一个雨网格、无新增阴影灯、无逐滴物理/GI/反射更新控制成本；实际低配帧率仍需目标机器验证。

## 发现与修正

首轮屋檐边缘测试发现只检查雨丝中心会让倾斜尾端穿进覆盖区；改为整个雨丝范围的屋顶检查。提高云团细节并压低阴雨云幕的局部反差，修正雨天明显蓝洞/强太阳的问题；光照按同一密度场遮阳。较陡的下雨门控改为覆盖率 65%–100% 的平滑区间，保持连续变化。雨从跟随摄像机的柱列改成固定世界柱列，并用真实控制器横移断言其视差。

隐藏/最小化原生播放器的首轮截图为黑色，没有纳入验收；最终截图全部来自正常游戏预览窗口。Antigravity 只读架构检查提供初稿，主代理逐项核对：采纳路面高度建议；负时间越界、`Silent` 静音和未绑定自然窗光的说法与代码不符，未照搬。最终实现与实际 Unity 图像/回归均由主代理核验。

## 重现

```powershell
python -X utf8 tools/sky/build_weather_noise.py
# 关闭本项目 Editor 后执行 BuildSlice.Build，使用 Unity 官方 batchmode CLI。
builds/windows/TownDemo.exe --review --fresh --review-weather -screen-fullscreen 0 -screen-width 1920 -screen-height 1080 --capture-dir output/demo/weather/1080
builds/windows/TownDemo.exe --review --verify-weather-save
builds/windows/TownDemo.exe --review --fresh --review-weather -screen-fullscreen 0 -screen-width 1280 -screen-height 720 --capture-dir output/demo/weather/720
# 每个可见播放器进程退出后再启动下一项。其他回归入口见项目playtest技能。
python -X utf8 tools/sky/compose_weather_review.py
```

源与设计原则见 `assets/source/sky/README.md`。本段历史环境验收先于全镇迁移；用户随后授权一起上传。Git保留JSON、对照图和动态视频，原始PNG／Player日志留在本机，重新运行上述可见播放器后可重建对照图。
