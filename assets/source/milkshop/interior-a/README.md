# A / 奶白暖灯

用户于 2026-10-09 选择 A，要求只改现有牛奶屋内部细节。`MilkShopInteriorA.blend` 是独立可编辑模块；`Reference_ExistingShop_NOT_EXPORTED` 仅供对照。建筑位置、奶牛招牌、柜台、货架、入口和真实窗洞沿用原资产。用户随后报告两侧窗边闪烁，因此另修复原 `Windows` 网格的接缝与玻璃表面；其余原网格逐一哈希确认不变，见上级 `verification/window-joinery-repair.json`。

模块包含两盏奶白搪瓷吊灯、弯铜颈阅读灯、绿色布面送奶记录簿及胡桃木矮架、黄铜服务铃、瓶签、少量柜台用品、小盆栽、服务板和薄地垫。各交互物件有独立命名和米制原点；显式 UV。10 个可见网格、7,208 三角面、8 种材质，不增加完整建筑副本或复杂碰撞。

Unity 的同一 `MilkShopAsset.Create` 接入内饰、三盏无实时阴影点光和五个简单交互碰撞。`MilkShopInteriorAsset` 用三个标记校验米制轴向、检查原建筑边界、灯光数量与预算，再由共用 Town 场景派生钟表屋窗外副本。`MilkShopInteriorA.prefab` 保存纯视觉组件；交互与灯光由生成源统一装配。

历史记录只包含用户确认的两条：1931 年 1 月 19 日钟表铺 2 瓶，1 月 20 日旅馆 3 瓶，均已送。正式数据为 `game/Assets/Resources/MilkDeliveryOrders.json`，纸面 UI 使用中文字体确定性排版；模型表面印字由 Pillow 生成。没有额外地址、时间、签收人或案件推断，不授予 E05。当前游戏时钟跨日不会重写纸面日期。

E 打开订单；月份、日期标签及上一日/下一日按钮翻阅，方向键也可翻日期；Esc 合上。阅读暂停视角、移动和游戏时间。上次阅读日期、阅读灯开关和原有存档内容分别保存；服务铃仅播放本项目生成的短音，不召唤 NPC。

重建：

```powershell
python -X utf8 tools/milkshop/create_print_assets.py
& 'C:/Program Files/Blender Foundation/Blender 5.2/blender.exe' --background --python-exit-code 1 --python tools/milkshop/build_interior_a.py
& 'C:/Program Files/Blender Foundation/Blender 5.2/blender.exe' --background --python-exit-code 1 --python tools/milkshop/verify_source.py
```

`manifest.json` 记录原模型哈希、预算与条目，`source-check.json` 是实际保存后重新打开的源检查。`previews/` 是 Blender 建模评审，不能代替游戏验收。实际 Windows 构建和验收结果另见 `output/demo/milkshop-interior-a/README.md`。新增几何和无阴影灯约束不是低配设备帧率实测。
