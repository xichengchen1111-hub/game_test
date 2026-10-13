# 第一幕：律师送院与声音设置

延续现有监所与会见室；律师走到警署门外提出探望孩子，可答应或暂缓。答应后从汽车右前门上车，律师沿既有道路送到 B11 医院门口，下车安慰道别后驾车离开。医院仍使用现有外观，人物与动作仍为临时版本。

游戏默认静音；Esc → 声音设置可开关、调总音量并保存。所有自动检测强制静音，不改玩家偏好。

最终 Mac 双架构播放器在 Apple M5 Pro / Unity6000.3.18f1 / Metal 完成六项720p原生检查：第一幕及律师实际出门、读档、完整送院、到院读档、途中退出、途中重入。车辆行驶时密集检查实际车体车道范围及实体交叉；完整送院从放行检查点开始，监所部分单独走完。日志对应验收 JSON 中准确程序集。此前1080p完整流程及54项EditMode记录单独保留，不混称最终程序集重跑。

程序调用真实控制器、普通第一命中E射线和共用交谈/音量处理；不是物理键鼠、盲测、Windows/Intel实机或可听音效验收。正常 slice-save.json 字节在每次检测前后不变。

- [门外邀请](opening-ride-01-invitation.jpg)
- [上车](opening-ride-02-boarding.jpg)
- [途中街景](opening-ride-03b-middle-street.jpg)
- [律师驾驶](opening-ride-03c-driver.jpg)
- [医院道别](opening-ride-04-goodbye.jpg)
- [驾车离开](opening-ride-05-departure.jpg)
- [静音与音量设置](opening-ride-audio-settings.jpg)

原始PNG及日志在忽略的 builds/opening/ride-final720-*，旧检测在ride03-*。前三次未获得结束标记的检测不计通过。最终试玩入口为 builds/opening/final/错日证词-第一幕.app；同目录有重开第一幕入口和说明。

旧音效检测边界：PropReview 原来要求主输出>0，RainReview要求非零最终混音，与当前用户要求的检测静音冲突；本轮未运行这些可听混音检查，后续需先改为静音的后处理采样方案，不得临时打开扬声器来满足旧断言。
