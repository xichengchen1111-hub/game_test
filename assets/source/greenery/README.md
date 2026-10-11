# 小镇树木与路边花境

2026-10-11 第二版按用户提供的两张照片重新组织花境，保持小镇灰雾、低饱和与简化造型。照片只作层次和布置参考，没有裁切复用为材质。第一版与修改前应用保存在忽略的 builds/macos-current/backup-before-greenery-refinement。

## 资产

共22种原创模型、51个LOD网格：7树形（椴树、白桦、橡树、枫树、垂柳、果树、松树的风格化轮廓），3灌木，3草簇，7花丛（雏菊、紫花穗、粉色波斯菊、金黄菊、蓝紫绣球、低矮白花、玫粉小花），2观赏草。树3档，其余2档。细节由真实叶片/花瓣几何、枝干和原创2048×1024树皮/叶脉/花瓣图集与法线提供，没有旧式整树交叉卡片或不透明球状树冠。

树木保留地图原217点位、树冠半径和原碰撞。小植物共21507组，按16米分区共享网格实例；3147批次，不复制每组顶点。树皮、白桦斑纹、叶脉均为原创程序绘制，纹理已打包进可编辑GreeneryKit.blend。manifest.json记录实际尺寸/面数，source-verification.json为重新打开源后的检查。

## 布置

32段沿街/巷花境，约8477.58平方米的有土花带。狭窄既有行道树带使用较低植物；较宽的路外绿地采用低白花边缘、中层紫粉花、后层灌木/观赏草。连续色块按5–15米节奏变化，入口、路口和设施处断开，奶车周围留出独立净空；公园保持中部草坪。原草地约56239平方米，另有果园/宅地/广场的花草。铺装通行净宽、所有前后门、站台轨道、车马院和3处指定观察走廊保留。范围是主镇Town，未重布邻镇车站街区。

## 重建

1. Python（Pillow/numpy）：运行 tools/build_greenery_textures.py。
2. Blender 4.5.9：以 --background --python-exit-code 1 --python tools/build_greenery.py 生成打包源、FBX和模型展示图；随后用同样方式运行 tools/verify_greenery_source.py。
3. Python（shapely/Pillow/numpy）：运行 tools/plan_greenery.py，生成GreeneryPlan.json、草地贴图和布置报告。生成器核对同源地图；只允许既有TownPlan与源文件的CRLF/LF差异，逐项比较原树位。
4. Unity6000.3.18f1：BuildSlice.BuildFreshMac。GreeneryAuthoring从同一计划生成Town和历史窗景；不要单独编辑生成场景或网格。

图集使用法线、8倍各向异性；保留高质量SMAA/4K四级阴影，内部绘制比例1.10、默认窗口1920×1080。维持原灰雾天气、曝光和灯光数量。草簇25m/65m、花灌木45m/125m的近档/剔除距离；树为三级LOD。实例植物接收当前环境SH，禁止回退到阴影里全黑的默认实例受光。

展示图kit-preview.png是Blender资产图库，不是游戏截图。实机检查与实际交付结论见 output/demo/greenery-refinement/README.md。
