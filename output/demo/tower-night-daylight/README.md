# 钟楼夜景、白天光影与三处修复（2026-10-11）

Windows／Unity 6000.3.18f1／D3D11，staging 播放器 `builds/windows-staging/TownDemo.exe`（正常入口 `builds/windows/` 未重建）。全部截图来自隔离存档的原生实机固定机位（`--review --fresh --review-tower-night`／`--review-day-look`），图片左“改前”、右“改后”。固定机位是摆放视角的对比图，不是游玩验收；游玩路径由下方各项 review 实际走过。

| 文件 | 内容 |
| --- | --- |
| `night-front.jpg`、`night-corner.jpg`、`night-rain-corner.jpg`、`night-street-north.jpg` | 夜间钟楼：背光乳白钟盘＋雾中光晕、木构钟室受光、广场投光、门灯 |
| `night-door.jpg` | 塔门马车式壁灯、铭牌夜间可读 |
| `night-machine-dial.jpg` | 机房内看钟盘：背光后外侧指针影仍可读 |
| `station-exit-zfight.jpg` | 车站候车厅后门外：步道与车站地坪共面闪烁带消失 |
| `car-map-seated.jpg` | 车内停车拉手刹后掏出纸地图：位于胸前，不再飘到车顶 |
| `day-*.jpg` | 白天阴天／晴天／雨／清晨：AO、顶亮天空环境光、阴天柔和主光与树影、占位房屋风化立面与石板瓦 |
| `sun-through-cloud.jpg` | 面向太阳：阴天、清晨、晴天、雨天云层后的日轮 |
| `threshold-*.jpg` | 钟楼进门地面 z-fighting 前后（贴地掠射机位） |
| `acceptance.json` | 各项原生 review 结果、耗时与程序集信息 |

限制：程序驱动的真实控制器／射线／界面处理函数，不是物理键鼠或陌生玩家盲测；未测低配帧率（新增 8 盏无阴影地标灯与更大 SSAO 半径，阴影灯数量不变）；未在 Mac 构建。
