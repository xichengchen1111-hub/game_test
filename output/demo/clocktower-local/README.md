# 本地钟塔内装与时间系统验证

2026-10-09，基于 a7c3191；Unity 6000.3.18f1 / Mac Metal。外观为内部检查用临时灰壳。用户选择暂不上传 Meshy，尚无 Meshy 模型、未完成正式外观替换。

本地入口：`builds/clocktower-review/钟塔-内部验证.app`，压缩包：`builds/clocktower-review/钟塔-本地内装与时间-Mac.zip`（x86_64 + arm64，签名及ZIP完整性检查通过）。测试专用应用标识为 `com.qingchuanstudio.wrongdatetest.towerreview`；正式桌面游戏与正常存档未替换。

实际通过：7秒推进10游戏分钟、界面暂停/Q继续、四面实际指针恒快30分钟、时间独立进程读档；闭门阻挡/两侧开关/防夹；从街道实走32级旋梯的39个路径点、推挤实体护栏、走近转动机芯、楼上存档恢复后逐级下楼。

最终补齐铭牌汉字后，1280×720与1920×1080画面复验通过。`tower-door-open.png`为墙装铭牌与真实门洞，`tower-dust-and-footprints.png`为楼梯积灰鞋印，`tower-machine-room.png`为可接近机芯，`tower-watch-comparison*.png`展示同屏动态校时。`results.json`记录5项专项通过。完整运行日志在构建目录的verification下。

全部为可见播放器中程序驱动实际角色/射线的验证；不是物理键鼠测试或陌生玩家盲测。完整MVP、第四版全镇迁移、昼夜排班与Meshy外壳接入尚未完成。静态地图检查仅因证据合同新增钟塔设定而更新合同哈希，未改地图几何。
