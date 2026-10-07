from pathlib import Path
from html import escape
from reportlab.pdfgen import canvas
from reportlab.pdfbase import pdfmetrics
from reportlab.pdfbase.ttfonts import TTFont
from reportlab.lib import colors
from reportlab.lib.pagesizes import A4
from reportlab.lib.styles import ParagraphStyle
from reportlab.platypus import Paragraph, Table, TableStyle
from PIL import Image
from pypdf import PdfReader

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / 'output' / 'pdf' / 'first-person-3d-plan.pdf'
ASSETS = ROOT / 'docs' / 'concepts'
FONT_DIR = Path('C:/Windows/Fonts')
pdfmetrics.registerFont(TTFont('Chinese', str(FONT_DIR / 'msyh.ttc'), subfontIndex=0))
pdfmetrics.registerFont(TTFont('ChineseBold', str(FONT_DIR / 'msyhbd.ttc'), subfontIndex=0))
INK = colors.HexColor('#20272B')
MUTED = colors.HexColor('#667176')
TEAL = colors.HexColor('#234348')
PAGES = [
    {
        'title': '错日证词 3D Demo企划',
        'intro': '第一人称悬疑调查。1931 年小镇可自由步行，进入建筑时黑屏转场，室内是完整的独立 3D 场景。玩家靠观察、核对证词和重排往事，决定是否提交真实材料。',
        'image': 'town-low-poly.png',
        'caption': '图 1 小镇探索概念图。模块建筑、简化人物与暖冷灯光形成主要画面。',
        'sections': [
            ('首个Demo范围', [
                '目标约 20 至 30 分钟：一个小镇外景、车站、钟表铺和住宅，约五名 NPC、8 至 10 件线索。先让玩家比较街钟与怀表、检查车票，再形成对不在场陈述的质疑。',
            ]),
            ('画风与制作范围', [
                '沿用参考图的简化人物和柔和色块：灰蓝、紫灰暗部搭配暖黄灯光。建筑由少量墙体、门窗模块组合；家具使用简单网格和平涂材质，装饰主要依靠贴图。',
                'NPC 共用骨架，以发型和服装区分，先做待机与交谈动作。只把票孔、钟面和日期等推理相关细节做清楚。',
            ]),
        ],
    },
    {
        'title': '室内调查与推理',
        'intro': '每间屋子可完整观察和走动。桌面、门窗、人物位置共同提供信息；重要材料支持放大与反复查看。',
        'image': 'interior-low-poly.png',
        'caption': '图 2 住宅调查概念图。少量家具与关键证物承担探索内容。',
        'sections': [
            ('交互与转场', [
                'WASD 行走，鼠标观察，E 检查与交谈，Tab 打开调查簿。门前互动 → 淡出并保存 → 加载室内 → 恢复状态 → 淡入；离开时返回同一门口。',
            ]),
        ],
        'table_title': '核心推理',
        'table': [
            ['材料', '玩家需要判断的关系'],
            ['完整车票与旅馆登记', '买票及短暂停留，能否证明整个傍晚都在外？'],
            ['街钟、怀表与送奶记录', '证词中的时刻来自哪只钟，如何交叉核实？'],
            ['问路、电话与原始记录', '日期及先后顺序是否支持当事人的说法？'],
        ],
        'after': [
            '律师只协助核实材料。对话采用固定分支，往事片段允许玩家重排；提交给警方的仍是票据、簿页和证言。',
            '完整首章再补邻居家、警署、修车铺和旅馆，并接入诚实提交与知情隐瞒两个结局。开工前统一保单年限、煤车声时序和结局中的证据状态。',
        ],
    },
    {
        'title': '调查簿与代理开发',
        'intro': '调查簿保留材料来源和玩家笔记，往事用于回看与排序。界面不自动标出凶手或真假线索。',
        'image': 'journal-low-poly.png',
        'caption': '图 3 调查簿概念图。简洁面板和材料卡片保持信息可读。',
        'table_title': '应用与自动化',
        'table': [
            ['应用', '减少人工的方式'],
            ['Unity 6.3 与 URP', 'Editor 脚本生成场景和 Prefab，自动测试、构建与截图。'],
            ['Blender 5.2', 'Python 批量制作模块与道具，共用骨架，导出 FBX。'],
            ['ImageGen', '提供风格图、纹理与装饰；模型和可读证物正文分别制作。'],
            ['编码代理、agy 与 Git', '代理实现与复核，agy 做批量草稿；Git 保存通过检查的阶段。'],
        ],
        'sections_after': [
            ('Skills outline', [
                '现有：imagegen、pdf、computer-use、skill-creator。待建四个项目技能：剧情数据校验、Unity 场景生产、Blender 资产生产、调查玩法回归。',
                '工作流：修改 → 数据校验 → 编译与测试 → 实际运行截图 → 修正。人工集中审核画风、谜题公平性和结尾体验。',
            ]),
        ],
    },
]


def make_markdown():
    lines = []
    for index, page in enumerate(PAGES):
        lines.extend([('# ' if index == 0 else '## ') + page['title'], '', page['intro'], '',
                      f"![{page['caption']}](concepts/{page['image']})", '', page['caption'], ''])
        if index == 0:
            lines.extend(['以上为低模风格概念 Demo 图，用于确定制作目标，非实机截图。', ''])
        for heading, paragraphs in page.get('sections', []):
            lines.extend(['### ' + heading, ''])
            for paragraph in paragraphs:
                lines.extend([paragraph, ''])
        if 'table' in page:
            lines.extend(['### ' + page['table_title'], ''])
            for row_index, row in enumerate(page['table']):
                lines.append('| ' + ' | '.join(row) + ' |')
                if row_index == 0:
                    lines.append('| --- | --- |')
            lines.append('')
        for paragraph in page.get('after', []):
            lines.extend([paragraph, ''])
        for heading, paragraphs in page.get('sections_after', []):
            lines.extend(['### ' + heading, ''])
            for paragraph in paragraphs:
                lines.extend([paragraph, ''])
    lines.extend(['[三页图文 PDF](../output/pdf/first-person-3d-plan.pdf) · [图片提示词](concepts/generation-notes.json)', ''])
    (ROOT / 'docs' / 'first-person-3d-plan.md').write_text('\n'.join(lines), encoding='utf-8')


def make_pdf():
    OUT.parent.mkdir(parents=True, exist_ok=True)
    c = canvas.Canvas(str(OUT), pagesize=A4, pageCompression=1)
    c.setTitle('错日证词第一人称3D Demo精简企划')
    c.setAuthor('Game team')
    width, height = A4
    margin = 44
    usable = width - margin * 2
    body = ParagraphStyle('body', fontName='Chinese', fontSize=10.5, leading=17,
                          textColor=INK, wordWrap='CJK', spaceAfter=7)
    secondary = ParagraphStyle('secondary', parent=body, fontSize=8.2, leading=12, textColor=MUTED)
    heading_style = ParagraphStyle('heading', parent=body, fontName='ChineseBold', fontSize=12.5,
                                   leading=18, textColor=colors.black)
    cell_style = ParagraphStyle('cell', parent=body, fontSize=9.3, leading=14)
    header_style = ParagraphStyle('table_header', parent=cell_style, fontName='ChineseBold', textColor=colors.white)
    for index, page in enumerate(PAGES):
        y = height - margin

        def para(text, style=body, gap=7):
            nonlocal y
            flow = Paragraph(escape(text), style)
            _, h = flow.wrap(usable, height)
            if y - h < 45:
                raise ValueError(f'Page {index + 1} overflow: {text[:30]}')
            flow.drawOn(c, margin, y - h)
            y -= h + gap

        c.setFillColor(colors.black)
        c.setFont('ChineseBold', 21 if index == 0 else 19)
        c.drawString(margin, y - 23, page['title'])
        y -= 41
        para(page['intro'], gap=11)
        with Image.open(ASSETS / page['image']) as img:
            image_height = usable * img.height / img.width
        c.drawImage(str(ASSETS / page['image']), margin, y - image_height,
                    width=usable, height=image_height, preserveAspectRatio=True, mask='auto')
        y -= image_height + 8
        para(page['caption'], secondary, gap=10)
        if index == 0:
            para('低模风格概念 Demo 图，用于确定制作目标，非实机截图。', secondary, gap=11)
        for heading, paragraphs in page.get('sections', []):
            para(heading, heading_style, gap=5)
            for paragraph in paragraphs:
                para(paragraph)
        if 'table' in page:
            para(page['table_title'], heading_style, gap=6)
            rows = [[Paragraph(escape(text), header_style if i == 0 else cell_style) for text in row]
                    for i, row in enumerate(page['table'])]
            table = Table(rows, colWidths=[135, usable - 135], hAlign='LEFT')
            table.setStyle(TableStyle([
                ('BACKGROUND', (0, 0), (-1, 0), TEAL),
                ('ROWBACKGROUNDS', (0, 1), (-1, -1), [colors.white, colors.HexColor('#F1F5F5')]),
                ('GRID', (0, 0), (-1, -1), 0.4, colors.HexColor('#D9D9D9')),
                ('VALIGN', (0, 0), (-1, -1), 'MIDDLE'),
                ('LEFTPADDING', (0, 0), (-1, -1), 9),
                ('RIGHTPADDING', (0, 0), (-1, -1), 9),
                ('TOPPADDING', (0, 0), (-1, -1), 7),
                ('BOTTOMPADDING', (0, 0), (-1, -1), 7),
            ]))
            _, h = table.wrap(usable, height)
            if y - h < 45:
                raise ValueError(f'Page {index + 1} table overflow')
            table.drawOn(c, margin, y - h)
            y -= h + 12
        for paragraph in page.get('after', []):
            para(paragraph)
        for heading, paragraphs in page.get('sections_after', []):
            para(heading, heading_style, gap=5)
            for paragraph in paragraphs:
                para(paragraph)
        c.setFont('Chinese', 8)
        c.setFillColor(MUTED)
        c.drawString(margin, 24, '错日证词  第一人称3D精简企划')
        c.drawRightString(width - margin, 24, f'{index + 1} / 3')
        c.showPage()
    c.save()
    reader = PdfReader(OUT)
    if len(reader.pages) != 3:
        raise ValueError('Expected exactly three pages')
    for index, page in enumerate(reader.pages):
        assert page['/Resources']['/XObject'], f'Page {index + 1} has no embedded image'
        assert page.extract_text().strip(), f'Page {index + 1} has no readable text'
    print(f'Created {OUT}: {len(reader.pages)} pages, 3 embedded images')


if __name__ == '__main__':
    make_pdf()
    make_markdown()
