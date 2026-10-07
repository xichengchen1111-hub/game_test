"""Create the initial Unity slice from the installed official URP template."""
from pathlib import Path
import json
import tarfile
import urllib.request
from fontTools.ttLib import TTFont
from fontTools.varLib.instancer import instantiateVariableFont
from fontTools import subset

ROOT = Path(__file__).resolve().parents[1]
GAME = ROOT / 'game'
TEMPLATE = Path('C:/Program Files/Unity/Hub/Editor/6000.3.18f1/Editor/Data/Resources/PackageManager/ProjectTemplates/com.unity.template.3d-cross-platform-17.0.14.tgz')

def main():
    GAME.mkdir(exist_ok=True)
    prefix = 'package/ProjectData~/'
    with tarfile.open(TEMPLATE) as archive:
        for member in archive.getmembers():
            if not member.isfile() or not member.name.startswith(prefix):
                continue
            relative = member.name[len(prefix):]
            if not relative.startswith(('ProjectSettings/', 'Assets/Settings/')):
                continue
            target = (GAME / relative).resolve()
            if not target.is_relative_to(GAME.resolve()):
                raise ValueError('Invalid template path')
            if not target.exists():
                target.parent.mkdir(parents=True, exist_ok=True)
                target.write_bytes(archive.extractfile(member).read())
    version = GAME / 'ProjectSettings/ProjectVersion.txt'
    if not version.exists():
        version.write_text('m_EditorVersion: 6000.3.18f1\n', encoding='utf-8')
    packages = GAME / 'Packages'
    packages.mkdir(exist_ok=True)
    manifest = packages / 'manifest.json'
    if not manifest.exists():
        manifest.write_text(json.dumps({'dependencies': {
            'com.unity.render-pipelines.universal': '17.0.1',
            'com.unity.test-framework': '1.4.2',
            'com.unity.modules.audio': '1.0.0',
            'com.unity.modules.imgui': '1.0.0',
            'com.unity.modules.jsonserialize': '1.0.0',
            'com.unity.modules.physics': '1.0.0',
            'com.unity.modules.imageconversion': '1.0.0',
            'com.unity.modules.screencapture': '1.0.0',
            'com.unity.modules.ui': '1.0.0',
        }}, indent=2) + '\n', encoding='utf-8')
    fonts = GAME / 'Assets/Art/Fonts'
    fonts.mkdir(parents=True, exist_ok=True)
    font_path = fonts / 'TownSans.ttf'
    if not font_path.exists():
        print('Downloading open-source Noto Sans SC font and OFL license', flush=True)
        url = 'https://raw.githubusercontent.com/google/fonts/main/ofl/notosanssc/'
        raw = urllib.request.urlopen(url + 'NotoSansSC%5Bwght%5D.ttf', timeout=90).read()
        (fonts / 'OFL.txt').write_bytes(urllib.request.urlopen(url + 'OFL.txt', timeout=30).read())
        import io
        font = instantiateVariableFont(TTFont(io.BytesIO(raw)), {'wght': 400}, inplace=True)
        chars = ''.join(p.read_text(encoding='utf-8') for p in (GAME / 'Assets').rglob('*.cs'))
        chars += Path(__file__).with_name('make_slice_textures.py').read_text(encoding='utf-8')
        chars += ''.join(chr(c) for c in range(32,127)) + '钟表铺车站青川客栈杂货钟表修理室南区壹贰叁肆伍陆柒捌玖零年月日通知修复记录店营业1931中午十二时青川铁路镇务已完工签记白布灯柱拆下'
        options = subset.Options()
        options.name_IDs = ['*']
        options.name_legacy = True
        worker = subset.Subsetter(options=options)
        worker.populate(text=chars)
        worker.subset(font)
        for record in font['name'].names:
            if record.nameID in (1,4,6):
                replacement = 'TownSans' if record.nameID != 6 else 'TownSans-Regular'
                record.string = replacement.encode(record.getEncoding())
        font.save(font_path)
        print(f'Authored font subset: {font_path.stat().st_size} bytes', flush=True)
    settings=GAME / 'ProjectSettings/ProjectSettings.asset'
    if settings.exists():
        text=settings.read_text(encoding='utf-8')
        import re
        text=re.sub(r'(?m)^  activeInputHandler: \d+', '  activeInputHandler: 0', text)
        settings.write_text(text,encoding='utf-8')
    print('Unity URP project prepared:', GAME)

if __name__ == '__main__':
    main()
