# 五人行走动作：网页制作与导出交接

2026-10-13。用户指出此前五人没有走路动画，检查属实。原值班警员 ZIP 仅有 `idle.001`；先前24项检查是动作切换机制测试，不能证明模型文件具备 Walk。

本目录记录五个**第一轮、外观仍拒收或暂停接入**的 Tripo 模型，各自应用预设 `walk` 后的2.33秒网页播放，以及导出明确选中 `idle`、`walk` 两段。不是新版外观、不是本地动画文件、不是游戏验收。

五个已保留页面、模型ID及导出设置见 [handoff.json](handoff.json)。每个页面均为 FBX / Blender / 2k / Export Skeleton 开启 / 动画2段 / Stay in Place关闭，以便本地测量原动作实际位移后再制作原地循环。看守暂停时出现的 Export Current Frame 已关闭。

律师正常点击 Export 后产生转换45919afd-dde9-4379-9102-1a9be248067e，但下载跳转被Chrome以 ERR_BLOCKED_BY_CLIENT 阻止。本轮无本地新动画包，未尝试绕过；其余四人只准备好导出，没有继续触发受阻下载。需用户手动完成下载。已确认的新版外观参考上传也仍未完成，旧外观不会因为本次动作预览而被放行。

- [律师导出](lawyer-export.jpg)
- [看守行走姿态](custody-guard-walk.jpg) / [看守导出](custody-guard-export.jpg)
- [值班警员导出](duty-officer-export.jpg)
- [巡警导出](patrol-officer-export.jpg)
- [接待女警导出](reception-officer-export.jpg)

截图是网页状态证据，部分镜头/面板遮挡人物，不用于足底或完整穿模验收。仍需下载后检查真实曲线、循环、步幅、手臂/腰包/裙摆、完整过渡，以及静音Unity播放。对话动作、车内坐姿动作与新外观三维制作仍未完成。没有修改当前试玩包、用户存档或声音设置。
