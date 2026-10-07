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
        'title': '错日证词 自由探索MVP',
        'intro': '第一人称 3D 悬疑调查。1931 年小镇可自由步行，进屋黑屏转场，室内为独立场景。玩家自行选择地点，核对时刻和日期；陪伴角色偶尔随口说话。',
        'image': 'town-low-poly.png',
        'caption': '图 1 小镇探索概念图。模块建筑、简化人物与暖冷灯光形成主要画面。',
        'sections': [
            ('范围与自由探索', [
                '目标约 20 至 30 分钟：小镇、车站、钟表铺、住宅，五名 NPC、十个证物条目。三个室内从开始就可访问，物件和人物可重复检查，取得顺序不影响事实。',
                'Demo 收束于玩家提交有依据的日期与时刻质疑，要求重新调查；完整谋杀重建和双结局留给后续章节。阶段报告后仍可继续探索。',
            ]),
            ('画风与制作范围', [
                '简化无五官人物、宽色块、灰蓝与紫灰暗部、暖黄灯光。建筑和家具共用少量模块；NPC 先做待机与交谈。只精确制作票孔、钟面和日期，保持 1931 年道具背景。',
            ]),
        ],
    },
    {
        'title': '自由调查与随口提醒',
        'intro': '每间屋子可走动和观察。材料保留来源并支持放大；新问法随已知材料更新。陪伴角色的对白可以错过，不能解锁线索、房门或结论。',
        'image': 'interior-low-poly.png',
        'caption': '图 2 住宅调查概念图。少量家具与关键证物承担探索内容。',
        'sections': [
            ('交互与转场', [
                'WASD 行走，鼠标观察，E 互动，Q 怀表，Tab 调查簿。进门淡出、保存、加载、恢复、淡入；离开时回原门口，重启保留材料和位置。',
            ]),
        ],
        'table_title': '可以交叉开始的推理',
        'table': [
            ['材料', '玩家需要判断的关系'],
            ['送奶时刻', '18:25 来自街钟；独立签收记录把送达约束在 17:50–18:00。当前钟差只作交叉观察。'],
            ['问路日期', '巡警原簿、修灯通知和片段环境互相核实：问路属于 1 月 19 日，不能移到案发日。'],
            ['完整车票', '与已验票比较，只能削弱乘车说法，不能直接证明杀人。'],
        ],
        'after': [
            '陪伴角色最多随口说“这张票倒是保存得挺好”，不带路或公布答案。往事可回看和重排；关键事实也须存在于记录与证言中，导演镜头不能提交给警方。',
        ],
    },
    {
        'title': '调查簿与开发技能',
        'intro': '玩家自己选取要质疑的陈述并关联材料。界面不自动标出真假或凶手；纸面线索、家书和生活痕迹共同支撑调查。',
        'image': 'journal-low-poly.png',
        'caption': '图 3 调查簿布局示意，条目以 MVP 方案为准；图中旅馆暂不制作。',
        'table_title': '已制作并安装的项目技能',
        'table': [
            ['技能', '职责'],
            ['wrong-date-narrative', '日期、证据来源、推理边界与线索数据校验。'],
            ['wrong-date-unity', '第一人称、独立场景、互动、调查簿和存档。'],
            ['wrong-date-blender', '模块建筑、低模道具、简化人物与导入检查。'],
            ['wrong-date-playtest', '访问顺序、静默陪伴、状态恢复和实机验收。'],
        ],
        'sections_after': [
            ('工具与验收', [
                '本机已有 Unity 6000.3.18f1 和 Blender 5.2.0 LTS；ImageGen 与 PDF 技能直接可用。Editor 与 Python 脚本生产，agy 做草稿，主代理复核。',
                '测试三个室内的全部六种访问顺序，以及读档、回访和完全无提醒。编译与数据检查需另加实机截图和试玩；当前尚未制作 Unity 游戏。',
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
    lines.extend(['[完整 MVP 方案](mvp-demo.md) · [开发工具与安装](development-tools.md)', '', '[三页图文 PDF](../output/pdf/first-person-3d-plan.pdf) · [图片提示词](concepts/generation-notes.json)', ''])
    (ROOT / 'docs' / 'first-person-3d-plan.md').write_text('\n'.join(lines), encoding='utf-8')


def make_pdf():
    OUT.parent.mkdir(parents=True, exist_ok=True)
    c = canvas.Canvas(str(OUT), pagesize=A4, pageCompression=1)
    c.setTitle('错日证词第一人称3D自由探索MVP')
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
        c.drawString(margin, 24, '错日证词  第一人称3D自由探索MVP')
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
