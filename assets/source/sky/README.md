# 小镇天空与天气源

`tools/sky/build_weather_noise.py` 以固定种子 1931 生成 256px 无缝 RGB 噪声：R 为多尺度云团，G 为低频形变，B 为周期 Worley 云缘。源 PNG 和 Unity PNG 同步生成；没有下载或复制游戏天空纹理。

Unity 以 linear / readable / Repeat / Trilinear / mipmaps / Uncompressed 导入。CPU 只读取太阳／月亮方向的三个样本，使云遮阳与场景日照协调；GPU 分两层移动云并绘制云边透光、暖色低空、月面和稳定星点。`DuskSky.shader` 不做体积射线步进。

`TownWeather` 使用已有累计游戏分钟，12 段晴／多云／阴／雨／雾循环，每段 210 游戏分钟，前 55% 保持、后 45% 平滑渐变。雨在云量足够后出现。暂停、查看材料和读档沿用游戏时钟；没有离线天气推进、额外墙钟或新增剧情事实。

`WeatherEffects` 的单个动态网格最多 256 根雨丝、1024 顶点、512 三角形。半数分布在玩家近处，半数覆盖远处；雨柱锚定世界坐标并在范围边缘渐隐，保持玩家移动时的视差。垂直落雨相位也取自累计游戏时间。屋顶遮蔽检查雨丝的两边和倾斜尾端。`WeatherShelter` 由共用建筑生成器明确绑定，不改变模型或碰撞，不做每滴物理查询。楼体保持静止，后续移动或改建需重新生成遮蔽体。柜台灯的人工开关意愿仍优先；室内自然光量受天气影响，街灯按太阳昼夜关闭。

审美与制作参考：[Guerrilla 的云形、动画和时段光照公开资料](https://www.guerrilla-games.com/read/the-real-time-volumetric-cloudscapes-of-horizon-zero-dawn)。这里采用适合当前小镇与低配目标的分层天空近似；该资料不构成本项目的性能验证。

运行验证和实际画面见 `output/demo/weather/README.md`。
