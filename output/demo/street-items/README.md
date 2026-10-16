# 1931 街道物件：实测（2026-10-16）

- 构建：Windows／Unity 6000.3.18f1／D3D11 `BuildSlice.BuildStaging` → `SLICE_BUILD_PASS`；构建期 `STREET_ITEMS_AUTHORING_PASS`（41 处、10 种、232954 三角面；位置、朝向、占地、贴地、无灯无交互、材质子网格、与其他碰撞不重叠），见 `authoring-checks.json`。地图 `check_town_map.py` MAP_CHECK_PASS。
- 原生 `--review --fresh --review-street-items`（`tools/ReviewStreetItems.ps1`）1080p 与 720p 均 `STREET_ITEMS_REVIEW_PASS`：54 条人行道净宽扫掠、10 种实体推挤（擦鞋摊 0.2 m 踏步可踩、平台不可进）、4 段真实奔跑行走、火警灯罩与路灯同一感光曲线（晴天正午 0、夜间 1.0、有发光）、光池预算不变、未授予证物。关键帧在 `review-1080/`（JPG）。
- ZFightScan：精确共面 0、运动件 0；物件网格在生成源头无共面，城镇修补不再改动任何物件。回归（同一最终构建）：`--review-full-map` PASS（六种访问顺序）、`--review-period-streets` PASS；`ReviewCar` PASS（前一版构建）。
- 限制：程序驱动、静音，非物理键鼠或盲测。
