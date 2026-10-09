"""Additive, editable A interior; never rewrites the original milk-shop shell.
Blender 5.2 --background --python-exit-code 1 --python this_file.py
Coordinates: X across counter, Y towards back, Z height, metres.
"""
import bpy, bmesh, math, json, hashlib, sys
from pathlib import Path
from mathutils import Vector

REPO=Path(__file__).resolve().parents[2]
SOURCE=REPO/'assets/source/milkshop/interior-a'
ART=REPO/'game/Assets/Art/MilkShopInteriorA'
for p in (SOURCE, SOURCE/'previews', ART):p.mkdir(parents=True,exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True)
scene=bpy.context.scene;scene.unit_settings.system='METRIC';scene.unit_settings.scale_length=1
kit=bpy.data.collections.new('A_Interior_Additions');scene.collection.children.link(kit)
review=bpy.data.collections.new('Review_Only');scene.collection.children.link(review)
groups={};mats={}
def material(name,color,metal=0,rough=.5,tex=None,glow=0):
 m=bpy.data.materials.new(name);m.use_nodes=True;m.diffuse_color=(*color,1)
 p=m.node_tree.nodes.get('Principled BSDF');p.inputs['Base Color'].default_value=(*color,1);p.inputs['Metallic'].default_value=metal;p.inputs['Roughness'].default_value=rough
 if tex:
  n=m.node_tree.nodes.new('ShaderNodeTexImage');n.image=bpy.data.images.load(str(tex),check_existing=True);m.node_tree.links.new(n.outputs['Color'],p.inputs['Base Color'])
 if glow:p.inputs['Emission Color'].default_value=(*color,1);p.inputs['Emission Strength'].default_value=glow
 mats[name]=m;return m
cream=material('Milk_Enamel',(.87,.81,.67),.12,.28)
brass=material('Milk_Brass',(.46,.28,.10),.65,.30)
iron=material('Milk_Iron',(.055,.066,.06),.35,.50)
cloth=material('Milk_Linen',(.12,.19,.15),0,.90)
wood=material('Milk_Walnut',(.47,.32,.19),0,.65,REPO/'assets/source/milkshop/textures/timber-basecolor.png')
ink=material('Milk_Ink',(.9,.8,.65),0,.82,ART/'InkAtlas.png')
bulb=material('Milk_Bulb',(1,.70,.33),0,.3,glow=1.5)
leaf=material('Milk_Leaf',(.16,.24,.08),0,.9)
def active(o):
 bpy.ops.object.select_all(action='DESELECT');o.select_set(True);bpy.context.view_layer.objects.active=o
def register(o,name,m,group):
 o.name=name
 for c in list(o.users_collection):c.objects.unlink(o)
 kit.objects.link(o);o.data.materials.append(m);groups.setdefault(group,[]).append(o)
 return o
def uv(o):
 layer=o.data.uv_layers.active or o.data.uv_layers.new(name='UVMap')
 for p in o.data.polygons:
  axis=max(range(3),key=lambda i:abs(p.normal[i]))
  for li in p.loop_indices:
   v=o.data.vertices[o.data.loops[li].vertex_index].co
   layer.data[li].uv=((v.x,v.y) if axis==2 else ((v.x,v.z) if axis==1 else (v.y,v.z)))
def box(name,loc,size,m,group,bevel=.004):
 bpy.ops.mesh.primitive_cube_add(size=1,location=loc);o=bpy.context.object;o.dimensions=size;active(o);bpy.ops.object.transform_apply(location=False,rotation=False,scale=True);register(o,name,m,group);uv(o)
 if bevel:
  mod=o.modifiers.new('Soft edges','BEVEL');mod.width=min(bevel,min(size)*.24);mod.segments=1
  mod=o.modifiers.new('Corner normals','WEIGHTED_NORMAL');mod.keep_sharp=True
 return o
def cyl(name,loc,r,h,m,group,n=20):
 bpy.ops.mesh.primitive_cylinder_add(vertices=n,radius=r,depth=h,location=loc);o=register(bpy.context.object,name,m,group);uv(o)
 for p in o.data.polygons:p.use_smooth=len(p.vertices)==4
 return o
def lathe(name,profile,loc,m,group,n=24):
 vs=[(r*math.cos(j*math.tau/n),r*math.sin(j*math.tau/n),z) for z,r in profile for j in range(n)]
 fs=[(i*n+j,i*n+(j+1)%n,(i+1)*n+(j+1)%n,(i+1)*n+j) for i in range(len(profile)-1) for j in range(n)]
 me=bpy.data.meshes.new(name);me.from_pydata(vs,[],fs);me.update();o=bpy.data.objects.new(name,me);kit.objects.link(o);o.location=loc;register(o,name,m,group);uv(o)
 bm=bmesh.new();bm.from_mesh(me);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(me);bm.free()
 for p in me.polygons:p.use_smooth=True
 return o
def tube(name,points,r,m,group):
 cu=bpy.data.curves.new(name,'CURVE');cu.dimensions='3D';cu.resolution_u=8;cu.bevel_depth=r;cu.bevel_resolution=1
 sp=cu.splines.new('BEZIER');sp.bezier_points.add(len(points)-1)
 for p,co in zip(sp.bezier_points,points):p.co=co;p.handle_left_type='AUTO';p.handle_right_type='AUTO'
 o=bpy.data.objects.new(name,cu);kit.objects.link(o);active(o);bpy.ops.object.convert(target='MESH');register(bpy.context.object,name,m,group);uv(bpy.context.object)
 return bpy.context.object
def patch(name,loc,w,h,region,group,rotation=(0,0,0)):
 # Atlas regions in PIL top-left pixels. Plane front is local +Z.
 x,y,rw,rh=region;vs=[(-w/2,-h/2,0),(w/2,-h/2,0),(w/2,h/2,0),(-w/2,h/2,0)]
 me=bpy.data.meshes.new(name);me.from_pydata(vs,[],[(0,1,2,3)]);me.update();o=bpy.data.objects.new(name,me);kit.objects.link(o);register(o,name,ink,group);o.location=loc;o.rotation_euler=rotation
 layer=me.uv_layers.new(name='UVMap')
 coords=[(x/1024,1-(y+rh)/1024),((x+rw)/1024,1-(y+rh)/1024),((x+rw)/1024,1-y/1024),(x/1024,1-y/1024)]
 for li in range(4):layer.data[li].uv=coords[li]
 return o

# Matching enamel pendants: open shade, rolled lip, brass neck, cloth flex.
for x in [-1.05,1.05]:
 g='Pendant_Left' if x<0 else 'Pendant_Right';y=.83;z=2.50
 # Original roof has rafters at planar Y=0 and 2.75, not a flat ceiling.
 # The narrow fixing timber meets both rafters; rose touches its underside.
 rafter_height=4.81-abs(x)/3.54*(4.81-3.12)
 timber_z=rafter_height-.075;mount=timber_z-.060
 box('Rafter fixing timber',(x,1.375,timber_z),(.12,2.83,.12),wood,g)
 cyl('Ceiling rose',(x,y,mount-.0125),.095,.025,brass,g)
 tube('Cloth flex',[(x,y,mount-.025),(x+.009,y,3.75),(x-.007,y,3.20),(x+.01,y,2.93),(x,y,2.74)],.009,iron,g)
 cyl('Brass socket',(x,y,2.73),.047,.14,brass,g)
 lathe('Enamel shell',[(0,.255),(.017,.258),(.05,.247),(.14,.20),(.21,.12),(.25,.059),(.27,.056)],(x,y,z),cream,g)
 lathe('Rolled dark rim',[(0,.257),(.012,.261),(.024,.257)],(x,y,z-.009),iron,g)
 lathe('Shade inner',[(0,.245),(.055,.224),(.17,.151),(.225,.057)],(x,y,z+.009),cream,g)
 bpy.ops.mesh.primitive_uv_sphere_add(segments=16,ring_count=8,radius=.063,location=(x,y,z+.050));register(bpy.context.object,'Warm glass bulb',bulb,g)

# Angled walnut reading stand and green cloth ledger on original countertop.
g='OrderBook';cx=-.14;cy=.75;base=1.255;theta=math.radians(24)
box('Stand foot',(cx,cy,base+.014),(.70,.49,.028),wood,g)
support=box('Stand back',(cx,cy+.15,base+.11),(.57,.08,.19),wood,g)
board=box('Reading board',(cx,cy,base+.119),(.68,.46,.027),wood,g);board.rotation_euler.x=theta
def on_board(x,y,z):return Vector((cx+x,cy+y*math.cos(theta)-z*math.sin(theta),base+.119+y*math.sin(theta)+z*math.cos(theta)))
cover=box('Clothbound cover',on_board(-.095,.005,.032),(.39,.365,.035),cloth,g);cover.rotation_euler.x=theta
pages=box('Ivory page edges',on_board(-.095,.005,.053),(.374,.35,.010),cream,g);pages.rotation_euler.x=theta
top=box('Front cloth cover',on_board(-.095,.005,.067),(.39,.365,.018),cloth,g);top.rotation_euler.x=theta
for yy in [-.164,.172]:
 strip=box('Brass cover corners',on_board(-.095,yy,.079),(.36,.008,.003),brass,g,0);strip.rotation_euler.x=theta
patch('Cover title',on_board(-.095,.040,.078),.235,.068,(0,0,512,150),g,(theta,0,0))
patch('Delivery slip',on_board(.221,.01,.033),.142,.365,(512,0,512,520),g,(theta,0,0))
clip=box('Brass order clip',on_board(.22,.20,.048),(.062,.027,.017),brass,g);clip.rotation_euler.x=theta
for xx in [-.30,.30]:cyl('Rest brass screw',(cx+xx,cy-.21,base+.033),.009,.006,brass,g,12)
box('Stand retaining lip',(cx,cy-.222,base+.038),(.67,.022,.049),wood,g)

# Desk lamp leaves existing mechanical scale and original three bottles in place.
g='ReadingLamp';x=.37;y=1.21
lathe('Weighted brass foot',[(0,.092),(.018,.106),(.04,.10),(.055,.054),(.07,.045)],(x,y,base),brass,g)
tube('Curved neck',[(x,y,base+.07),(x+.03,y,base+.30),(x+.075,y-.015,base+.49),(x+.01,y-.06,base+.63),(x-.16,y-.105,base+.61)],.015,brass,g)
shade=lathe('Desk enamel shade',[(0,.152),(.025,.155),(.09,.125),(.15,.056),(.17,.038)],(x-.16,y-.105,base+.43),cream,g)
lathe('Desk rolled rim',[(0,.153),(.008,.156),(.016,.153)],(x-.16,y-.105,base+.426),brass,g)
bpy.ops.mesh.primitive_uv_sphere_add(segments=16,ring_count=8,radius=.043,location=(x-.16,y-.105,base+.447));register(bpy.context.object,'ReadingBulb',bulb,'ReadingBulb')
cyl('Switch button',(x+.065,y-.035,base+.053),.019,.021,iron,'LampSwitch',12)

# Reception bell, pencil cup, slips and a planted pot: compact countertop detail.
g='ServiceBell';x=-.63;y=.68
lathe('Bell base',[(0,.074),(.012,.084),(.025,.082),(.033,.07)],(x,y,base),iron,g)
lathe('Bell brass dome',[(0,.068),(.024,.071),(.06,.058),(.078,.033),(.085,.013)],(x,y,base+.027),brass,g)
cyl('Bell plunger',(x,y,base+.137),.014,.037,brass,g,16)
g='CounterDetails';x=1.40;y=1.17
lathe('Glazed pot',[(0,.08),(.13,.11),(.15,.115),(.16,.115)],(x,y,base),cream,g,16)
for i in range(11):
 angle=i*2.399;tip=Vector((x+.17*math.cos(angle),y+.14*math.sin(angle),base+.21+.1*(i%3)))
 tube('Fern stem',[(x,y,base+.14),(x+.04*math.cos(angle),y+.04*math.sin(angle),base+.24),tip],.0035,leaf,g)
 me=bpy.data.meshes.new('Fern leaflet');me.from_pydata([(0,0,0),(-.024,.052,.012),(0,.135,0),(.024,.052,.012)],[],[(0,1,2,3),(3,2,1,0)]);me.update()
 o=bpy.data.objects.new('Fern leaflet',me);kit.objects.link(o);register(o,'Fern leaflet',leaf,g);o.location=tip;o.rotation_euler=(.4,angle,.4+angle);uv(o)
x=1.42;y=.81
lathe('Pencil cup',[(0,.042),(.11,.048),(.125,.048),(.125,.039),(.025,.039)],(x,y,base),cream,g,16)
for i in range(3):
 p=cyl('Wood pencil',(x-.02+i*.02,y,base+.13),.004,.24,wood,g,6);p.rotation_euler.y=-.09+i*.08
box('Order slip stack',(1.17,.68,base+.017),(.19,.145,.025),cream,g)
patch('Stack print',(1.17,.68,base+.030),.18,.13,(0,150,512,370),g)

# Readable label attached to the existing left bottle, no duplicate bottle geometry.
g='BottleTag'
box('Bottle string',(-1.20,.89,1.52),(.004,.018,.17),brass,g,0)
patch('Fresh milk tag',(-1.20,.881,1.47),.108,.146,(0,520,256,340),g,(math.pi/2,0,0))
# Small framed service board on the existing right interior wall, preserves window.
g='InteriorDecor';rot=(math.pi/2,0,-math.pi/2)
board=box('Oak service board',(3.47,1.56,2.03),(.038,.61,.89),wood,g)
patch('Chalk lettering',(3.446,1.56,2.03),.52,.79,(256,520,384,504),g,rot)
# Thin woven mat away from existing thresholds.
patch('Woven rug',(0,-1.08,.202),1.70,1.55,(640,520,384,504),g)

# Bake bevels, merge each useful part, preserve UV and naming, set useful origins.
origins={'OrderBook':(cx,cy,base),'ReadingLamp':(.37,1.21,base),'LampSwitch':(.435,1.175,base+.053),'ServiceBell':(-.63,.68,base),'ReadingBulb':(.21,1.105,base+.447)}
objects=[]
for group,parts in groups.items():
 for o in parts:
  active(o)
  for mod in list(o.modifiers):bpy.ops.object.modifier_apply(modifier=mod.name)
 bpy.ops.object.select_all(action='DESELECT')
 for o in parts:o.select_set(True)
 bpy.context.view_layer.objects.active=parts[0]
 if len(parts)>1:bpy.ops.object.join()
 o=bpy.context.object;o.name=group
 scene.cursor.location=origins.get(group,(0,0,0));bpy.ops.object.origin_set(type='ORIGIN_CURSOR');objects.append(o)
root=bpy.data.objects.new('MilkShopInteriorA',None);kit.objects.link(root)
for o in objects:o.parent=root
for name,loc in [('FrontMarker',(0,-1,0)),('RightMarker',(1,0,0)),('UpMarker',(0,0,1))]:
 o=bpy.data.objects.new(name,None);kit.objects.link(o);o.parent=root;o.location=loc
scene.cursor.location=(0,0,0)
for im in bpy.data.images:
 if im.source=='FILE':im.pack()
bpy.ops.object.select_all(action='DESELECT')
for o in kit.objects:o.select_set(True)
bpy.context.view_layer.objects.active=root
bpy.ops.export_scene.fbx(filepath=str(ART/'MilkShopInteriorA.fbx'),use_selection=True,object_types={'MESH','EMPTY'},apply_unit_scale=True,apply_scale_options='FBX_SCALE_UNITS',axis_forward='-Z',axis_up='Y',bake_anim=False,path_mode='STRIP',use_mesh_modifiers=True)
bounds=[o.matrix_world@Vector(v) for o in objects for v in o.bound_box]
for o in objects:o.data.calc_loop_triangles()
stats={'asset':'MilkShopInteriorA','units':'metres','blender':bpy.app.version_string,'triangles':sum(len(o.data.loop_triangles) for o in objects),'meshes':len(objects),'parts':[o.name for o in objects],'materials':list(mats),'boundsMin':[min(v[i] for v in bounds) for i in range(3)],'boundsMax':[max(v[i] for v in bounds) for i in range(3)],'originalBlendSha256':hashlib.sha256((SOURCE.parent/'MilkShop1931.blend').read_bytes()).hexdigest(),'originalFbxSha256':hashlib.sha256((REPO/'game/Assets/Art/MilkShop1931/MilkShop1931.fbx').read_bytes()).hexdigest(),'realLights':3,'newRealtimeShadows':0,'orders':[{'date':'1931-01-19','shop':'钟表铺','bottles':2,'delivered':True},{'date':'1931-01-20','shop':'旅馆','bottles':3,'delivered':True}]}
(SOURCE/'manifest.json').write_text(json.dumps(stats,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
# Append original geometry solely for a contextual modelling review. Not exported.
with bpy.data.libraries.load(str(SOURCE.parent/'MilkShop1931.blend'),link=False) as (src,dst):dst.collections=['MilkShop1931_Asset']
ref=dst.collections[0];ref.name='Reference_ExistingShop_NOT_EXPORTED';scene.collection.children.link(ref)
for o in ref.all_objects:
 if o.name.startswith('COL_'):o.hide_render=True;o.hide_set(True)
def point(name,loc,power,color):
 data=bpy.data.lights.new(name,'POINT');data.energy=power;data.color=color;data.shadow_soft_size=.18;o=bpy.data.objects.new(name,data);review.objects.link(o);o.location=loc
for x in [-1.05,1.05]:point('Review pendant',(x,.83,2.48),95,(1,.72,.43))
point('Review task',(.21,1.105,1.68),20,(1,.75,.45))
point('Review window fill',(-2.5,-1,2.5),80,(.57,.66,1))
scene.world=bpy.data.worlds.new('Dusk review');scene.world.use_nodes=True;scene.world.node_tree.nodes['Background'].inputs[0].default_value=(.25,.29,.40,1);scene.world.node_tree.nodes['Background'].inputs[1].default_value=.35
camd=bpy.data.cameras.new('ReviewCamera');cam=bpy.data.objects.new('ReviewCamera',camd);review.objects.link(cam);scene.camera=cam
scene.render.engine='CYCLES';scene.cycles.samples=32;scene.cycles.use_denoising=True;scene.render.resolution_x=1440;scene.render.resolution_y=960;scene.render.resolution_percentage=100;scene.view_settings.view_transform='AgX';scene.render.image_settings.file_format='PNG'
def camera(pos,target,lens):cam.location=pos;cam.rotation_euler=(Vector(target)-cam.location).to_track_quat('-Z','Y').to_euler();camd.lens=lens
camera((-.65,-2.3,1.87),(0,1.5,1.87),20)
bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE/'MilkShopInteriorA.blend'))
for name,pos,target,lens in [('interior-player',(-.65,-2.3,1.87),(0,1.5,1.87),20),('counter-details',(-.25,-.32,1.90),(-.14,.94,1.39),44)]:
 if '--skip-render' in sys.argv:break
 camera(pos,target,lens);scene.render.filepath=str(SOURCE/'previews'/f'{name}.png');bpy.ops.render.render(write_still=True)
print('MILK_INTERIOR_MODEL_PASS',json.dumps(stats))
