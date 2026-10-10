"""Refresh reviewed descriptive metadata without changing or re-exporting geometry."""
import importlib.util, json, re
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
source=ROOT/'assets/source/rail'
script=(Path(__file__).with_name('build_rail_assets.py')).read_text(encoding='utf-8')
# write_layout_json is independent of Blender; use that exact final generator function.
a=script.index('def write_layout_json(');b=script.index('\ndef main():',a)
scope={'json':json}
exec(script[a:b],scope)
scope['write_layout_json'](source)
manifest=json.loads((source/'source-manifest.json').read_text(encoding='utf-8'))
manifest['provenance']=re.search(r'"provenance": "([^"]+)"',script).group(1)
(source/'source-manifest.json').write_text(json.dumps(manifest,indent=2)+'\n',encoding='utf-8')
print('RAIL_DESCRIPTIVE_METADATA_REFRESHED: meshes/FBX/blend unchanged')
