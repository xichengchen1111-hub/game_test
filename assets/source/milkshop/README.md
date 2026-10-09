# 1931 牛奶店

按用户参考图制作的木质牛奶店：黑白奶牛花纹木墙、实体木门、屋顶大型奶牛形木招牌及立体 `milk` 字样。没有现代玻璃门。背面、侧面与柜台陈设是制作时补全的设计，不是照片中可直接确认的结构。模型为参照图片构建的可编辑几何，奶牛招牌纹理由图像生成工具制作；没有调用 Meshy，也不声称复制其神经网络。

## 编辑与重建

- `MilkShop1931.blend`：最初由 Blender 4.5 LTS 制作，2026-10-09 用本机 Blender 5.2 LTS 修复窗框和门框上角并保存，纹理已打包。`MilkShop1931_Asset` 是导出模型；`Review_Only` 中的摄影棚灯光、地面和相机不用于游戏。
- `textures/`、`cow-outline.json`：材质来源和招牌外轮廓。`reference-1931.png`、`sign-generation-prompt.txt` 保留设计依据；用户原始店铺照片只作为参考，没有当作整面建筑贴图。
- `tools/build_model.py`：用 Blender 后台运行可重新生成 FBX、GLB 和预览，输出到本目录的 `exports/`、`previews/`、`verification/`。`tools/make_textures.py` 依赖 Pillow、NumPy、SciPy，重建木纹与招牌轮廓；现有贴图可直接使用。
- 游戏资源与稳定 GUID 位于 `game/Assets/Art/MilkShop1931/`。更新模型时替换原 FBX 并保留 `.meta`；Unity Prefab 已设好 URP 材质及 12 个简化碰撞体。保持 Unity Y 向上、门口朝 -Z，不要直接改 FBX 根方向或重新生成全部 GUID。

原模型占地 7.2 × 5.8 米，最初52,388、修窗后52,396三角形，12 个可见网格、17 种材质；场景统一缩放到 0.9。两扇门的独立轴心保留；当前游戏已接入物理开关门、碰撞与存档。

2026-10-09 用户指出两侧窗边移动时仍闪烁，已通过 `tools/milkshop/fix_window_joinery.py` 仅替换 `Windows` 网格。原窗洞侧切面与窗框恰好共面；修复窗框覆盖切面 5cm，并在墙内外各突出 7cm。横档分成两段与中央竖档对接，玻璃每片只有一个双面四边形，避免旧玻璃薄盒重复混合。生成源 `tools/window_joinery.py` 同时供完整建模脚本和定点修复使用。其余 11 个可见网格及全部碰撞代理逐一验证几何哈希不变；房屋点位、墙体、门洞、货架和柜台不变。修复后统计见 `verification/model-stats.json`，实际移动验收见 `output/demo/milkshop-interior-a/`。

## 当前游戏接入

`game/Assets/Editor/MilkShopAsset.cs` 由共享场景生成器调用，放置来自第四版全镇 `TownPlan.milkRect`：根位置约 `(-144.094, -0.04, 7.21)`、Y 朝向180°、比例0.9，原比例房屋坐落于B04生产院落中，门朝北街。男孩和奶车按实际入口重排，E04不变。旧切片位置 `(11.45,-0.04,-7.35)`、朝向90°只用于兼容旧存档转换。

第四版410×277米全镇外景已接入，钟楼／钟表铺／牛奶屋为3个完成建筑，其余67处占位。牛奶店内部与外观在同一空间、共用模型，无独立加载场景；日常订单不新增案件证物。A内饰另见 `interior-a/`，不得把参考屋再次导出。

两侧窗洞里的 `OldWindow.mat` 在游戏中使用 `WrongDate/ClearGlass`，由 `MilkShopAsset.Create` 保持透明设置；木窗框使用独立木材。重新导出模型后不要把窗片恢复成不透明 URP Lit。玩家能双向透过玻璃看到实际场景，既有侧墙碰撞阻止穿窗；窗景来自新的生产院落和邻居占位几何，不能沿用旧切片邻屋说明。当前复验使用 `--review --fresh --review-milk-interior-a --record-milk-windows`，随后独立 `--review --verify-milk-interior-save`。

2026-10-09 用户确认屋内看出时门框上角闪烁：原门楣内面与墙内面共面，立柱与横梁穿插。共享 `tools/door_joinery.py` 保持外侧平面和门洞宽度，内侧突出墙面8cm；立柱止于2.727m、横梁底2.730m，以3mm间隙对接。一次性定点迁移 `tools/milkshop/fix_door_joinery.py` 只替换Trim中的三块门框，其他Trim面／UV／材质，以及其余网格和全部碰撞哈希不变；保存后重开核验见 `verification/door-joinery-repair.json`。这个修复随后改变了原Trim，因此前述修窗阶段的11网格不变证明只适用于该历史阶段。

Windows原生720p／1080p各检查左右上角真实横移约0.94m、各13帧，普通AO与阴影；用 `--record-milk-doorframe` 附加到A内饰复验。画面、10Hz视频与分阶段哈希见 `output/demo/full-map/`，不是游戏帧率或所有显卡无闪烁保证。

Mac 构建使用 `BuildSlice.BuildFreshMac`；可见游戏运行 `--review --fresh --review-milkshop --capture-dir <绝对目录>` 可复验男孩对话、进出店门及旁巷行走。实际证据在 `output/demo/milkshop/`，概览截图使用受控相机，行走和交互使用实际角色控制器与射线。该验收不是手动键鼠盲测，也不是完整剧情验收。
