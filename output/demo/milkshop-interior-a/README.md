# 牛奶屋 A 内饰：实际游戏验收

2026-10-09，Unity 6000.3.18f1 / URP 17.0.1，Windows x64 / D3D11。当前本地入口为 `builds/windows/TownDemo.exe`；Mac 应用没有在这台 Windows 上重建。概念图与 Blender 预览分别在 `docs/concepts/milk-shop/round-01/`、`assets/source/milkshop/interior-a/previews/`，本目录都是实际 Player 帧。

已验收：从小镇连续走进/走出牛奶屋，场景和建筑实例不变、没有黑屏；三盏无实时阴影灯；柜台订单、阅读灯、服务铃、奶瓶签、空奶桶全部用正常 3m 射线触发。阅读界面暂停移动/视角/游戏时间，关闭后恢复。两条记录与用户确认相符，日期上下界不能越出记录；游戏时间跨午夜仍保留纸面历史日期。全部日常物件不授予 E05 或其他案件材料。

订单已逐页检查 1080p/720p 的日期、店家、瓶数与确认文字；阅读灯开关和铃声状态通过。真实控制器走到两侧透明窗并推动至原碰撞，不能穿过；窗外街景仍使用同一 Town 几何。窗外近照使用受控相机，室内路线和交互使用真实 CharacterController。

窗边闪烁另做动态复验：两窗均在普通光照、阴影和 AO 下实际横向行走约 0.94m，分别采样 13 帧，已逐帧查看窗框和墙板接缝，未再见原先交替争抢的表面。`window-north-motion.mp4`、`window-south-motion.mp4` 是这些真实帧的 10Hz 回放，采样率不是游戏帧率。`window-motion-results.json` 保留世界坐标跟踪的窗框亮度范围，包含随视角变化的木纹/高光，不能当作所有表面完全不闪的判定或编造修复前下降百分比。原生帧在 `builds/review/milk-interior-a/window-motion-run/`。

共用状态回归也通过：连续 7秒/10分钟走时、四面塔钟比怀表快 30分钟、UI 暂停和独立读档；钟表屋物理门、透明窗、交互和连续往返通过。上述回归运行于本轮 A 内饰构建；最后仅牛奶屋 Windows 网格修复后，重新跑双分辨率奶屋交互与独立读档，未改时间或钟表屋代码。`save-isolation.json` 确认正常存档哈希相同。

独立进程重新打开存档，验证牛奶屋内位置、阅读灯关闭和上次查看的 1 月 19 日页面恢复，再用正常射线打开、关闭订单并走出门。专用 `milk-interior-review-save.json` 不替换正常 `slice-save.json`。

复现：

```powershell
builds/windows/TownDemo.exe --review --fresh --review-milk-interior-a -screen-fullscreen 0 -screen-width 1920 -screen-height 1080 --capture-dir output/demo/milkshop-interior-a/1080
builds/windows/TownDemo.exe --review --verify-milk-interior-save --capture-dir output/demo/milkshop-interior-a/relaunch
builds/windows/TownDemo.exe --review --fresh --review-milk-interior-a -screen-fullscreen 0 -screen-width 1280 -screen-height 720 --capture-dir output/demo/milkshop-interior-a/720
```

逐项断言结果见各分辨率 `milk-a-results.json`，读档结果见 `relaunch/milk-a-save-relaunch.txt`。详细构建和 Player 日志在被 Git 忽略的 `builds/review/milk-interior-a/`。A 内饰最初没有改原模型；用户随后报告两侧窗边闪烁，现已仅修复原 `Windows` 网格。其余可见网格及全部碰撞代理几何哈希与制作前相同，原尺寸和摆放范围通过生成检查。

自动检查驱动真实 Player、控制器和交互射线，页面翻阅直接调用按钮的同一处理函数。未做物理键鼠、陌生玩家盲测、Intel/Mac 或真实低配设备帧率测试。新增几何预算和三盏无阴影灯是成本约束，不能视为低配 60 帧承诺。agy 只起草了纸面 UI，最终字段、实现、模型和验收由主代理复核；草稿中的额外地址与晨间派送描述未采用。
