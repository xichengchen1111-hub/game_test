"""Hospital records (Y01-Y04): player-facing text and paper textures.

Design: docs/research/mystery-evidence-design.md; contract: docs/mvp-evidence.json (hospital_1931).
Each record says which clock wrote its times, so two honest records of the same walk can be
compared. No record names a culprit, a clock error or a corrected time.

Writes game/Assets/Resources/HospitalEvidence.json and game/Assets/Art/Hospital/Papers/*.png
(house typography: TownSans on aged paper; the child's drawing is drawn front and back).
"""
import json
import math
import random
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[2]
ART = ROOT / 'game/Assets/Art/Hospital/Papers'
FONT = ROOT / 'game/Assets/Art/Fonts/TownSans.ttf'

RECORDS = [
    dict(id='Y01', title='探视登记簿 · 一月二十日', source='门厅问讯台 · 门房保管的登记簿（家属可看本家条目）',
         body='一九三一年一月二十日　礼拜二\n儿童病房非探视日。家属凭护士长特许条或陪护证进入。\n\n'
              '床位：儿童病房 C-017\n'
              '14:10 进　母亲　（特许条第三号）\n'
              '15:35 出　母亲　往缴费窗口\n'
              '16:20 进　父亲　（陪护证）\n'
              '16:55 出　父亲　借看车站时刻表\n\n'
              '本簿时刻按门房怀表填写。门房的表每日早晨在车站对时。'),
    dict(id='Y02', title='缴费收据存根 · 第〇四一二号', source='事务室缴费窗口 · 收据存根簿（医院留存联）',
         body='收款日期：一九三一年一月二十日\n收款时刻：下午四时零七分\n'
              '住院号：C-017（儿童病房）\n'
              '款项：护理及膳食费，预付至四月三十日\n'
              '金额：四十元整（现款）\n'
              '交款人：母亲（亲笔签名）　经手：事务员\n\n'
              '备注：交款人嘱咐，家里若有变动，请院里先照顾孩子。\n\n'
              '本室收款时刻照室内挂钟填写；挂钟每日正午随钟楼报时校准。'),
    dict(id='Y03', title='孩子的画', source='儿童病房 · 靠院子第三床的床上小桌',
         body='正面：一座房子、三个人和一辆小汽车。中间的人手里提着一只画满红圈的篮子。太阳还没有涂色。\n\n'
              '背面，铅笔，孩子的字：\n一月二十日　礼拜二　下午\n妈妈来了，带了一篮苹果。\n妈妈说下回带金色的蜡笔来，给太阳涂。'),
    dict(id='Y04', title='病房护士的证言', source='儿童病房 · 病房护士（谈话记录）',
         body='“礼拜二下午她来过。那天本来不让探视，护士长给了特许。她坐了一个多钟头，把苹果削成小块喂孩子。'
              '走的时候说要去缴费窗口。后来听事务室说，她把护理费一直付到了四月底——从前她总是拖着。她走时没说去哪。”'),
]


def sheet(record):
    big, small = ImageFont.truetype(str(FONT), 36), ImageFont.truetype(str(FONT), 25)
    im = Image.new('RGB', (1024, 768), (214, 204, 176))
    d = ImageDraw.Draw(im)
    rnd = random.Random(record['id'])
    for _ in range(900):                                   # faint paper fibre
        x, y = rnd.randrange(1024), rnd.randrange(768)
        c = rnd.randrange(196, 222)
        d.point((x, y), fill=(c, c - 10, c - 36))
    d.rectangle((35, 35, 989, 733), outline=(133, 113, 85), width=2)
    d.text((70, 60), record['title'], font=big, fill=(44, 47, 41))
    d.line((70, 120, 955, 120), fill=(95, 96, 79), width=2)
    y = 150
    for para in record['body'].split('\n'):
        if not para:
            y += 14
            continue
        while para:
            line, para = para[:32], para[32:]
            d.text((70, y), line, font=small, fill=(54, 53, 44))
            y += 35
    return im


def crayon(d, points, colour, width, rnd, passes=3):
    for _ in range(passes):
        jitter = [(x + rnd.uniform(-2, 2), y + rnd.uniform(-2, 2)) for x, y in points]
        d.line(jitter, fill=colour, width=width, joint='curve')


def drawing_front():
    """A child's crayon picture matching the one on the overbed table."""
    im = Image.new('RGB', (1024, 768), (236, 230, 212))
    d = ImageDraw.Draw(im)
    rnd = random.Random(20)
    crayon(d, [(40, 640), (984, 650)], (96, 150, 70), 10, rnd)                       # grass
    crayon(d, [(130, 620), (130, 380), (390, 380), (390, 620), (130, 620)], (190, 70, 60), 9, rnd)   # house
    crayon(d, [(110, 390), (260, 250), (410, 390)], (150, 60, 50), 9, rnd)
    crayon(d, [(220, 620), (220, 520), (290, 520), (290, 620)], (110, 80, 60), 7, rnd)
    crayon(d, [(160, 430), (205, 430), (205, 475), (160, 475), (160, 430)], (70, 110, 190), 6, rnd)
    for cx, h, colour in ((520, 190, (60, 60, 70)), (620, 170, (170, 60, 90)), (705, 110, (200, 120, 60))):   # three people
        top = 620 - h
        d.ellipse((cx - 22, top - 44, cx + 22, top), outline=colour, width=6)
        crayon(d, [(cx, top), (cx, top + h * .55)], colour, 6, rnd)
        crayon(d, [(cx, top + h * .55), (cx - 22, 620)], colour, 6, rnd)
        crayon(d, [(cx, top + h * .55), (cx + 22, 620)], colour, 6, rnd)
        crayon(d, [(cx - 34, top + 40), (cx + 34, top + 40)], colour, 6, rnd)
    crayon(d, [(640, 470), (680, 470), (672, 510), (648, 510), (640, 470)], (140, 100, 50), 5, rnd)   # basket
    for ax, ay in ((650, 462), (664, 458), (676, 464)):
        d.ellipse((ax - 9, ay - 9, ax + 9, ay + 9), outline=(200, 40, 40), width=5)
    crayon(d, [(780, 600), (780, 545), (830, 520), (930, 520), (960, 560), (960, 600), (780, 600)], (60, 80, 140), 8, rnd)   # car
    for wx in (815, 925):
        d.ellipse((wx - 22, 585, wx + 22, 629), outline=(40, 40, 40), width=7)
    d.ellipse((800, 70, 930, 200), outline=(120, 110, 90), width=4)                   # the uncoloured sun
    for a in range(0, 360, 40):
        r0, r1 = 75, 105
        d.line((865 + r0 * math.cos(math.radians(a)), 135 + r0 * math.sin(math.radians(a)),
                865 + r1 * math.cos(math.radians(a)), 135 + r1 * math.sin(math.radians(a))), fill=(120, 110, 90), width=4)
    return im


def drawing_back():
    im = Image.new('RGB', (1024, 768), (232, 226, 206))
    d = ImageDraw.Draw(im)
    rnd = random.Random(21)
    font = ImageFont.truetype(str(FONT), 46)
    lines = ['一月二十日　礼拜二　下午', '妈妈来了，带了一篮苹果。', '妈妈说下回带金色的蜡笔来，', '给太阳涂。']
    y = 170
    for line in lines:
        x = 110
        for ch in line:                                     # a child's uneven pencil hand
            d.text((x + rnd.uniform(-3, 3), y + rnd.uniform(-5, 5)), ch, font=font, fill=(80, 82, 92))
            x += 50 + rnd.uniform(-4, 6)
        y += 110
    return im


def main():
    ART.mkdir(parents=True, exist_ok=True)
    for record in RECORDS:
        if record['id'] in ('Y01', 'Y02'):
            sheet(record).save(ART / (record['id'] + '.png'))
    drawing_front().save(ART / 'Y03.png')
    drawing_back().save(ART / 'Y03-back.png')
    out = ROOT / 'game/Assets/Resources/HospitalEvidence.json'
    out.write_text(json.dumps({'entries': RECORDS}, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
    print('HOSPITAL_EVIDENCE', len(RECORDS), sorted(p.name for p in ART.glob('*.png')))


if __name__ == '__main__':
    main()
