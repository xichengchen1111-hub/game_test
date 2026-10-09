"""Extend the existing OFL TownSans subset without dropping any previously supported glyph.
Usage: python3 tools/clocktower/expand_ui_font.py path/to/NotoSansSC-variable.ttf
Official source: https://github.com/google/fonts/tree/main/ofl/notosanssc
The existing game/Assets/Art/Fonts/OFL.txt remains the font license.
"""
from pathlib import Path
import sys,hashlib,json
from fontTools.ttLib import TTFont
from fontTools.varLib.instancer import instantiateVariableFont
from fontTools import subset
root=Path(__file__).resolve().parents[2]
source=Path(sys.argv[1]);target=root/'game/Assets/Art/Fonts/TownSans.ttf'
existing=TTFont(target);codepoints=set(existing.getBestCmap());existing.close()
for folder,patterns in [(root/'game/Assets',['*.cs','*.json']),(root/'tools',['*.py'])]:
 for pattern in patterns:
  for item in folder.rglob(pattern):codepoints.update(map(ord,item.read_text(encoding='utf-8')))
font=instantiateVariableFont(TTFont(source),{'wght':400},inplace=True)
options=subset.Options();options.name_IDs=['*'];options.name_legacy=True
worker=subset.Subsetter(options=options);worker.populate(unicodes=codepoints);worker.subset(font)
for record in font['name'].names:
 if record.nameID in (1,4,6):record.string=('TownSans-Regular' if record.nameID==6 else 'TownSans').encode(record.getEncoding())
font.save(target)
check=TTFont(target).getBestCmap()
required='青川古钟塔建成于1816年钟表大师埃利亚斯霍夫曼主持建造百余年钟声守候小镇晨昏'
missing=''.join(sorted({c for c in required if ord(c) not in check}))
assert not missing,'Missing required plaque glyphs: '+missing
manifest={'fontSource':'https://raw.githubusercontent.com/google/fonts/main/ofl/notosanssc/NotoSansSC%5Bwght%5D.ttf','sourceSha256':hashlib.sha256(source.read_bytes()).hexdigest(),'subsetSha256':hashlib.sha256(target.read_bytes()).hexdigest(),'weight':400,'retainedPreviousGlyphs':True,'subsetGlyphCount':len(check),'requiredPlaqueGlyphsPresent':True,'license':'game/Assets/Art/Fonts/OFL.txt'}
(root/'assets/source/clocktower/font-manifest.json').write_text(json.dumps(manifest,indent=2)+'\n')
print(json.dumps(manifest))
