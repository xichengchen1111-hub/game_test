# 胡桃木钟表屋实机验收 · 2026-10-08

先快进同步至 `cd991ce`，再按用户选定的 A / Walnut Heritage 制作。Unity 6000.3.18f1、Windows 可见原生玩家、Direct3D 11；Blender 5.2.0 LTS。构建包位于本机 `builds/windows/TownDemo.exe`，不纳入 Git。

## 当前结果

`walnut-results.json` 来自最终 1920×1080 运行中的实际断言：

- 实际 CharacterController 走进/走出前后门；关闭前门阻挡通行，前门两侧都能用普通交互射线开关；有人站在门扇转动范围时拒绝关闭。
- 进出始终同一建筑、Systems + Town 两个场景；无门口淡黑、场景替换或传送。
- 两扇橱窗内外都看真实空间；普通交互射线穿过玻璃，固定朝向横移有近远视差。窗户保留独立物理防穿过碰撞。
- 老板真实射线可交谈；锁定 Sit_Cross_Legged 完整循环，未换走跑、未启用根位移。E07 通知可查看并进入调查簿。
- 零件抽屉和展柜访问门真实射线开关；检查抽屉连接、维修台与右柜间距、工具支撑和木纹/地毯 UV。
- 门面与悬挂招牌采用圆盘实体刻度/指针，修正用户指出的黄色方底露角。

`walnut-save-relaunch.txt`：另一进程恢复店内世界位置、E07、开启前门和抽屉。`walnut-legacy-migration.txt`：实际读取单独的旧 `ClockShop` 局部坐标磁盘存档，映射到 Town 并再次保存，保留 E04/E07、送奶对白话题与版本、同行者备注。测试和展示均使用独立存档，正常 slice-save.json 未修改。

最新送奶少年底部对话 UI / 全分支 / 中断重问 / E04 时机 / 历史回看 / 条件话题和独立读档在 1280×720 回归 PASS，简报在 `regression/`。牛奶店两窗的内外视图、物理阻挡和门口往返在 1600×900 回归 PASS。`verification.json` 汇总实际日志标记、平台及最终源资产哈希；详细日志和失败迭代保留在忽略的 `builds/review/walnut-shop/`。牛奶店旧 harness 的报告字符串写死 Mac，本轮平台以实际 Windows 进程和本汇总为准，未复制该误标报告。

## 图像与源文件

`unity-*.png` 均为最终真实 Unity 游戏画面；主图 `unity-exterior.png` / `unity-interior.png`，细节 `unity-repair-area.png` / `unity-open-drawer.png` / `unity-open-vitrine.png`，窗户包含内、外和横移机位。存档与迁移图为另外的实际进程。

`model-complete-*.png` 是可编辑模型的 Blender 检查渲染，不是实机画面。可编辑源及重建顺序见 [assets/source/watch-shop](../../../assets/source/watch-shop/README.md)，老板与椅子沿用单独锁定资源，完整源没有重复导出人物。

## 范围与限制

采用程序控制移动、瞄准和共用交互处理函数；没有物理键盘验收或陌生玩家盲测。本轮只实测 Windows；新模型尚未复验 Mac，也没有重跑旧独立室内逐帧灯光采样或完整案件六顺序。未迁移第四版全镇坐标，未实现其他建筑室内或新增案件结论。用户随后授权提交推送，本批版本以 Git 历史和远端为准。
