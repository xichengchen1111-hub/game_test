# 1931 牛奶店

按用户参考图制作的木质牛奶店：黑白奶牛花纹木墙、实体木门、屋顶大型奶牛形木招牌及立体 `milk` 字样。没有现代玻璃门。背面、侧面与柜台陈设是制作时补全的设计，不是照片中可直接确认的结构。模型为参照图片构建的可编辑几何，奶牛招牌纹理由图像生成工具制作；没有调用 Meshy，也不声称复制其神经网络。

## 编辑与重建

- `MilkShop1931.blend`：Blender 4.5 LTS 可编辑源文件，使用的纹理已打包。`MilkShop1931_Asset` 是导出模型；`Review_Only` 中的摄影棚灯光、地面和相机不用于游戏。
- `textures/`、`cow-outline.json`：材质来源和招牌外轮廓。`reference-1931.png`、`sign-generation-prompt.txt` 保留设计依据；用户原始店铺照片只作为参考，没有当作整面建筑贴图。
- `tools/build_model.py`：用 Blender 后台运行可重新生成 FBX、GLB 和预览，输出到本目录的 `exports/`、`previews/`、`verification/`。`tools/make_textures.py` 依赖 Pillow、NumPy、SciPy，重建木纹与招牌轮廓；现有贴图可直接使用。
- 游戏资源与稳定 GUID 位于 `game/Assets/Art/MilkShop1931/`。更新模型时替换原 FBX 并保留 `.meta`；Unity Prefab 已设好 URP 材质及 12 个简化碰撞体。保持 Unity Y 向上、门口朝 -Z，不要直接改 FBX 根方向或重新生成全部 GUID。

原模型占地 7.2 × 5.8 米，52,388 三角形、12 个可见网格、17 种材质；场景统一缩放到 0.9。两扇门的独立轴心保留，当前左门开启，右门关闭。门扇为静态布置，尚无玩家开关门交互。

## 当前游戏接入

`game/Assets/Editor/MilkShopAsset.cs` 是放置来源，由 `BuildSlice.Town` 调用。根位置 `(11.45, -0.04, -7.35)`、Y 朝向 90°、比例 0.9；店面朝街，在当前送奶男孩身后略偏南。保留男孩和奶车点位。完整可见边界到最近既有建筑的水平间隔约 0.7845 米，真实角色已穿过旁巷和店门往返，并触发原 E04 对话。

此轮只接入当前可玩小镇切片；没有迁移第四版全镇地图。牛奶店内部与外观在同一空间、共用模型，无独立加载场景及新剧情。钟表铺窗外使用同一小镇几何的变换副本。

两侧窗洞里的 `OldWindow.mat` 在游戏中使用 `WrongDate/ClearGlass`，由 `MilkShopAsset.Create` 保持透明设置；木窗框使用独立木材。重新导出模型后不要把窗片恢复成不透明 URP Lit。玩家能透过玻璃看到实际室外、从外面看到实际柜台；既有侧墙碰撞阻止穿窗。北侧窗外是相邻钟表铺外墙，南侧是室外空地，景物由真实位置决定。`--review --fresh --review-milkshop-windows` 可复验两窗的内外观察、左右移动、碰撞及店门往返，当前 Mac 实机证据在 `output/demo/milkshop/windows/`。

Mac 构建使用 `BuildSlice.BuildFreshMac`；可见游戏运行 `--review --fresh --review-milkshop --capture-dir <绝对目录>` 可复验男孩对话、进出店门及旁巷行走。实际证据在 `output/demo/milkshop/`，概览截图使用受控相机，行走和交互使用实际角色控制器与射线。该验收不是手动键鼠盲测，也不是完整剧情验收。
