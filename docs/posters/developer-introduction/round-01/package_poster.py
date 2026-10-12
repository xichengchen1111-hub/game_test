"""Package visually reviewed PDF renders for sharing. Run after Poppler rendering."""
import hashlib
import json
from pathlib import Path
from zipfile import ZipFile, ZIP_DEFLATED

from PIL import Image, ImageDraw, ImageFont
from pypdf import PdfReader

SOURCE = Path(__file__).resolve().parent
REPO = SOURCE.parents[3]
OUT = REPO/'output/pdf/developer-poster-20261012'
FULL = OUT/'wrong-date-developer-poster-full.pdf'
SHARE = OUT/'wrong-date-developer-poster-share.pdf'


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def main():
    full, share = PdfReader(FULL), PdfReader(SHARE)
    assert len(full.pages) == 9 and len(share.pages) == 8
    assert [p.extract_text() for p in full.pages[:8]] == [p.extract_text() for p in share.pages]
    embedded = set()
    for page in full.pages:
        for font_ref in page['/Resources']['/Font'].values():
            font = font_ref.get_object()
            desc = font.get('/FontDescriptor')
            if desc:
                desc = desc.get_object()
                assert any(key in desc for key in ['/FontFile', '/FontFile2', '/FontFile3'])
                embedded.add(str(font['/BaseFont']))
    font_families = {name.split('+', 1)[1] for name in embedded}
    assert len(font_families) == 3, font_families
    sizes, renders = [], []
    for n in range(1, 10):
        p = OUT/'pages'/f'page-{n}.png'
        assert p.stat().st_mtime >= FULL.stat().st_mtime
        with Image.open(p) as im:
            sizes.append(list(im.size))
        renders.append({'page': n, 'path': str(p.relative_to(REPO)), 'sha256': digest(p)})
    assert sizes == [[1684, 2382]]*9

    public = OUT/'share-images'
    public.mkdir(exist_ok=True)
    for n in range(1, 9):
        with Image.open(OUT/'pages'/f'page-{n}.png') as im:
            im.convert('RGB').save(public/f'wrong-date-{n:02d}.jpg', quality=95, subsampling=0)
    with ZipFile(OUT/'wrong-date-game-poster-images.zip', 'w', ZIP_DEFLATED) as package:
        for p in sorted(public.glob('wrong-date-*.jpg')):
            package.write(p, p.name)
    with ZipFile(OUT/'wrong-date-game-poster-images.zip') as package:
        assert len(package.namelist()) == 8
        assert package.testzip() is None

    labels = ['游戏概览', '调查体验', '故事与人物', '证言与线索',
              '场景环境', '角色与道具美术', '声光氛围', '游戏吸引力']
    tw, th, gap, margin = 340, 481, 22, 28
    sheet = Image.new('RGB', (margin*2+tw*4+gap*3, margin*2+(th+44)*2+gap), '#142723')
    draw = ImageDraw.Draw(sheet)
    font = ImageFont.truetype('C:/Windows/Fonts/msyhbd.ttc', 18)
    for i, label in enumerate(labels):
        x = margin+(i % 4)*(tw+gap)
        y = margin+(i//4)*(th+44+gap)
        with Image.open(OUT/'pages'/f'page-{i+1}.png') as im:
            sheet.paste(im.resize((tw, th), Image.Resampling.LANCZOS), (x, y))
        draw.text((x, y+th+12), f'{i+1:02d}  {label}', fill='#dec48f', font=font)
    sheet.save(OUT/'poster-preview.jpg', quality=95, subsampling=0)

    checks = json.loads((OUT/'document-checks.json').read_text(encoding='utf-8'))
    checks.update({'visual_review': {'reviewed_pages': list(range(1, 10)),
        'render_dpi': 144, 'render_dimensions': sizes[0],
        'method': 'Poppler render and individual page inspection; revised pages 1, 5 and 8 inspected again.',
        'issues_remaining': [], 'render_sha256': renders},
        'embedded_font_subsets': sorted(embedded), 'embedded_font_families': sorted(font_families),
        'share_pages_match_full': True, 'share_image_count': 8,
        'pdf_sha256': {'full': digest(FULL), 'share': digest(SHARE)}})
    (OUT/'document-checks.json').write_text(json.dumps(checks, ensure_ascii=False, indent=2), encoding='utf-8')
    print(json.dumps({'status': 'PACKAGED_AND_VERIFIED', 'pages': [9, 8],
          'image_zip': str(OUT/'wrong-date-game-poster-images.zip')}, ensure_ascii=False))


if __name__ == '__main__':
    main()
