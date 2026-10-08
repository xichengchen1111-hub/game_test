# Walnut Heritage 钟表铺

用户于 2026-10-08 选定 A 概念，随后要求完整建模、实体门直接进出、窗户透明。`reference-walnut.png` 是用户选择的概念参考；实际运行画面在 `output/demo/walnut-shop/unity-*.png`，两者不能混称。

## 可编辑源与生成顺序

- `WalnutShop.blend`：Blender 5.2.0 LTS，可编辑的 57 个视觉网格、分模块集合、独立展柜铰链和零件抽屉。原始几何及 Wood / Plaster / Rug 贴图由本仓库脚本制作，无外部模型包。纹理已打包进完整 .blend。
- `ShopVisual`：导出到 `game/Assets/Art/WalnutShop/WalnutShop.fbx` 的视觉模型。米制、显式 UV；FrontMarker / EastMarker 校准 Unity 导入方向。
- `ArchitectureReference`：按 Unity 导出的 `layout.json` 补入的可编辑墙、透明窗、门、顶板及圆形表盘参考，仅便于完整建模查看，不导出 FBX。运行中的墙体、门扇、碰撞和灯光仍由 `BuildSlice.Walnut.cs` 生成。
- `FinalReview`：Blender 摄影机和灯光。`model-complete-*.png` 是 Blender 模型检查，未放入游戏替代真实场景。
- 老板与椅子沿用 `assets/source/watchmaker/` 和 `WatchmakerAsset.Create`，不重复导出或改动锁定的 Meshy 人物。

在仓库根目录依次运行（用当前机器真实 Blender / Unity 可执行文件）：

```text
python tools/make_watch_shop_textures.py
blender --background --python-exit-code 1 --python tools/build_walnut_shop.py -- --root REPO_ABSOLUTE_PATH
Unity -batchmode -quit -projectPath REPO_ABSOLUTE_PATH/game -executeMethod BuildSlice.Build -logFile BUILD_LOG
blender --background --python-exit-code 1 --python tools/complete_watch_shop_source.py -- --root REPO_ABSOLUTE_PATH
```

先改生成源再重建；脚本会替换其生成资产，保留手工版本后再执行。完成脚本在 Unity 生成之后运行，以保证参考建筑对应实际布局。运行资源不依赖 Blender 安装。

## 交互和验收

E 开关前门、后门、展柜访问门、零件抽屉；老板交谈和 E07 通知沿用原内容。门扇有真实碰撞，扫过玩家时拒绝或暂停转动。玩家开门后自己行走进出，同一 Town 场景，无门口淡黑、加载或传送。两扇透明橱窗及门上玻璃可看真实两侧空间；橱窗保留防穿过碰撞。

当前可玩切片占地仍为 7.4 × 8.2m，沿用该切片位置；本轮没有迁移第四版全镇规划。旧 ClockShop 场景仅保留共同几何检查，启动不加载它。旧室内坐标读取后转为 Town 世界坐标，证物和对话进度保留。

实机复验命令：`--review --fresh --review-walnut-shop`，随后独立进程 `--review --verify-walnut-save`。迁移用独立 `walnut-migration-save.json` 旧格式测试文件及 `--review --verify-walnut-migration`；不修改正常 `slice-save.json`。输出详情见 `output/demo/walnut-shop/README.md`。自动移动、瞄准和共用交互函数不等于物理键盘测试或陌生玩家盲测；本轮只在 Windows D3D11 实机验证，新模型尚未复验 Mac。
