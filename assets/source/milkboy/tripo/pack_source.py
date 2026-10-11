import bpy,pathlib
bpy.ops.wm.open_mainfile(filepath='/Users/wulizhi/.codex/.chatgpt-projects/g-p-6ac33302cd888191873099003d5164f4/output/milkboy-unity-v4/Milkboy_v4_Master.blend')
for im in bpy.data.images:
 p=pathlib.Path('/Users/wulizhi/.codex/.chatgpt-projects/g-p-6ac33302cd888191873099003d5164f4/output/game_test/assets/source/milkboy/tripo/textures')/pathlib.Path(im.filepath).name
 if p.exists():im.filepath=str(p);im.pack()
bpy.ops.wm.save_as_mainfile(filepath='/Users/wulizhi/.codex/.chatgpt-projects/g-p-6ac33302cd888191873099003d5164f4/output/game_test/assets/source/milkboy/tripo/Milkboy.blend')
