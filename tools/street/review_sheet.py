"""Review sheets of the street cast for the user's approval (output/demo/street-life/cast/).

One row per figure, from the renders the importer made of the prepared source
(builds/cast-review/<Role>/): idle from the front, side and back, a walking step from the side and
a conversation gesture from the front, the same layout as the hospital cast's sheets. The dog has no
clips (StreetDog moves it in the game), so its row shows the rest pose from four sides.

    python3 tools/street/review_sheet.py
"""
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[2]
REVIEW = ROOT / 'builds/cast-review'
OUT = ROOT / 'output/demo/street-life/cast'
CELL = (270, 360); LABEL = 160
SHEETS = {
    'children-and-dog.jpg': ['StreetBoy', 'StreetGirl', 'DogWalker'],
    'adults.jpg': ['Businessman', 'YoungWoman', 'Labourer'],
    'old-and-poor.jpg': ['OldGentleman', 'Housewife', 'Transient'],
}
FRAMES = ['01_Idle-front-000', '01_Idle-side-000', '01_Idle-back-000', '02_Walk-side-025', '03_Talk-front-050']
TITLES = {'StreetBoy': '男孩 1.30 m', 'StreetGirl': '女孩 1.24 m', 'DogWalker': '遛狗的妇人 1.63 m',
          'Businessman': '商人 1.78 m', 'YoungWoman': '年轻女职员 1.65 m', 'Labourer': '送货工人 1.75 m',
          'OldGentleman': '老先生 1.70 m', 'Housewife': '买东西的主妇 1.60 m', 'Transient': '流浪汉（乞讨）1.73 m',
          'Terrier': '㹴犬 肩高 0.39 m'}


def font(size):
    for path in ('/System/Library/Fonts/PingFang.ttc', '/System/Library/Fonts/STHeiti Medium.ttc',
                 '/System/Library/Fonts/Hiragino Sans GB.ttc'):
        if Path(path).exists():
            return ImageFont.truetype(path, size)
    return ImageFont.load_default()


def label(sheet, top, height, role):
    draw = ImageDraw.Draw(sheet)
    draw.rectangle([0, top, LABEL - 1, top + height - 1], fill=(28, 28, 28))
    draw.text((8, top + 8), role, fill=(235, 235, 235), font=font(14))
    draw.text((8, top + 30), TITLES.get(role, ''), fill=(200, 200, 200), font=font(14))


def human_sheet(name, roles):
    sheet = Image.new('RGB', (LABEL + CELL[0] * len(FRAMES), CELL[1] * len(roles)), (28, 28, 28))
    for row, role in enumerate(roles):
        label(sheet, row * CELL[1], CELL[1], role)
        for col, frame in enumerate(FRAMES):
            image = Image.open(REVIEW / role / (frame + '.jpg')).convert('RGB').resize(CELL, Image.Resampling.LANCZOS)
            sheet.paste(image, (LABEL + col * CELL[0], row * CELL[1]))
    sheet.save(OUT / name, quality=88)


def dog_sheet():
    cell = (338, 254); views = ['Rest-side', 'Rest-three-quarter', 'Rest-front', 'Rest-back']
    sheet = Image.new('RGB', (LABEL + cell[0] * len(views), cell[1]), (28, 28, 28))
    label(sheet, 0, cell[1], 'Terrier')
    for col, view in enumerate(views):
        image = Image.open(REVIEW / 'Terrier' / (view + '.jpg')).convert('RGB').resize(cell, Image.Resampling.LANCZOS)
        sheet.paste(image, (LABEL + col * cell[0], 0))
    sheet.save(OUT / 'terrier.jpg', quality=88)


if __name__ == '__main__':
    OUT.mkdir(parents=True, exist_ok=True)
    for name, roles in SHEETS.items():
        human_sheet(name, roles)
    dog_sheet()
    print('STREET_CAST_SHEETS', sorted(p.name for p in OUT.glob('*.jpg')))
