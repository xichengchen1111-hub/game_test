# 《错日证词》游戏内容与美术海报

本册面向想了解游戏、考虑参与兴趣协作的开发者。介绍故事与人物、调查玩法、小镇空间、角色与道具美术、色彩及声光氛围。按用户要求，不展示实现清单、开发进度、技术缺口或任务分工。

## 内容与输出

- 八页转发版保留剧情核心悬念。
- 九页完整版在末页单列剧情真相、时间与日期矛盾及结局主题。
- 八张 JPG 图片包便于逐页转发，不包含剧透附录。
- 页面尺寸为 A3 竖版；正文与图片同版导出 PDF，字体已嵌入。

交付目录：`output/pdf/developer-poster-20261012/`。两份 PDF 使用稳定文件名 `wrong-date-developer-poster-share.pdf` 与 `wrong-date-developer-poster-full.pdf`。

## 可编辑来源

`poster-content.json` 保存文案，`poster_pages.py` 保存各页构图，`build_poster.py` 提供排版与 PDF 导出。`source-manifest.json` 记录图像来源、裁切位置和文件哈希。游戏图片沿用仓库内的实机截图，警署两幅为已选 D 方案的环境概念图，并在画面下标注“概念图”。未修改原始图片。

运行 `build_poster.py` 导出两份 PDF，再用 Poppler 按 144 DPI 渲染完整版至交付目录的 `pages/page-1.png` 至 `page-9.png`。逐页目视检查后运行 `package_poster.py`，核对 PDF 页数、正文一致性、字体嵌入、图片尺寸与 ZIP 完整性，生成八张 JPG、图片包及无剧透总览预览图。重排后必须重新渲染并检查，不把旧检查记录当作新版本验收。

联系方式未提供，海报采用项目仓库二维码作为了解入口，不代填私人联系方式或承诺待遇。
