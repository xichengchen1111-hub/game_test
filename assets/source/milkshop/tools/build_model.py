"""Rebuild the approved 1931 milk-shop concept as editable metre-scale geometry.
Blender 4.5 LTS --background --python-exit-code 1 --python tools/build_model.py
"""
import bpy, bmesh, math, json, random
from pathlib import Path
from mathutils import Vector
from mathutils.geometry import tessellate_polygon
ROOT=Path(__file__).resolve().parents[1]
TEX=ROOT/'textures'; EXP=ROOT/'exports'; PRE=ROOT/'previews'
for folder in (EXP,PRE,ROOT/'verification'):folder.mkdir(parents=True,exist_ok=True)
random.seed(1931)
bpy.ops.wm.read_factory_settings(use_empty=True)
scene=bpy.context.scene;scene.unit_settings.system='METRIC';scene.unit_settings.scale_length=1
asset=bpy.data.collections.new('MilkShop1931_Asset');scene.collection.children.link(asset)
review=bpy.data.collections.new('Review_Only');scene.collection.children.link(review)
groups={};materials={}

def active(o):
    bpy.ops.object.select_all(action='DESELECT');o.select_set(True);bpy.context.view_layer.objects.active=o
def move(o,collection=asset):
    for c in list(o.users_collection):c.objects.unlink(o)
    collection.objects.link(o)
def mat(name,color,rough=.8,metal=0,texture=None,normal=False):
    m=bpy.data.materials.new(name);m.use_nodes=True;m.diffuse_color=(*color,1)
    p=m.node_tree.nodes.get('Principled BSDF');p.inputs['Base Color'].default_value=(*color,1);p.inputs['Roughness'].default_value=rough;p.inputs['Metallic'].default_value=metal
    if texture:
        n=m.node_tree.nodes.new('ShaderNodeTexImage');n.image=bpy.data.images.load(str(TEX/texture));m.node_tree.links.new(n.outputs['Color'],p.inputs['Base Color'])
    if normal:
        n=m.node_tree.nodes.new('ShaderNodeTexImage');n.image=bpy.data.images.load(str(TEX/'wood-normal.png'),check_existing=True);n.image.colorspace_settings.name='Non-Color'
        nm=m.node_tree.nodes.new('ShaderNodeNormalMap');nm.inputs['Strength'].default_value=.32
        m.node_tree.links.new(n.outputs['Color'],nm.inputs['Color']);m.node_tree.links.new(nm.outputs['Normal'],p.inputs['Normal'])
    materials[name]=m;return m
cow=mat('CowPaintedTimber',(.8,.8,.7),texture='cow-timber-basecolor.png',normal=True)
pale=mat('PalePaintedTimber',(.7,.8,.7),texture='pale-timber-basecolor.png',normal=True)
wood=mat('NaturalTimber',(.5,.3,.13),texture='timber-basecolor.png',normal=True)
cream=mat('CreamTimber',(.8,.8,.7),texture='cream-timber-basecolor.png',normal=True)
iron=mat('ForgedIron',(.035,.045,.05),.5,.7)
roofmat=mat('WeatheredRoof',(.065,.085,.10),.78,.25)
metal=mat('GalvanizedMilkChurn',(.4,.45,.46),.48,.72)
ceramic=mat('MilkCeramic',(.82,.78,.65),.32)
dark=mat('BottleLip',(.48,.54,.50),.35)
glass=mat('OldWindow',(.13,.22,.23),.23,.1)
leaves=mat('Leaves',(.12,.23,.10),.86)
soil=mat('Soil',(.055,.038,.024),1)
flower_mats=[mat('FlowerLilac',(.38,.16,.52)),mat('FlowerPink',(.76,.27,.44)),mat('FlowerIvory',(.89,.83,.60))]
signmat=mat('CowSignPaint',(.8,.8,.8),texture='cow-sign.png')
bulb=mat('LampBulb',(.95,.72,.32),.35)
bs=bulb.node_tree.nodes.get('Principled BSDF');bs.inputs['Emission Color'].default_value=(1,.58,.20,1);bs.inputs['Emission Strength'].default_value=.6

def register(o,name,m,group):
    o.name=name;move(o);o.data.materials.append(m);groups.setdefault(group,[]).append(o);return o
def uv_world(o,scale=7.2,vertical=False):
    uv=o.data.uv_layers.active or o.data.uv_layers.new(name='UVMap')
    for p in o.data.polygons:
        n=o.matrix_world.to_3x3()@p.normal;axis=max(range(3),key=lambda i:abs(n[i]))
        for li in p.loop_indices:
            v=o.matrix_world@o.data.vertices[o.data.loops[li].vertex_index].co
            a,b=(v.x,v.y) if axis==2 else ((v.x,v.z) if axis==1 else (v.y,v.z))
            if vertical:a,b=b,a
            uv.data[li].uv=(a/scale+.5,b/5)
def bevel(o,width=.009):
    mod=o.modifiers.new('Soft timber edges','BEVEL');mod.width=width;mod.segments=1
    mod=o.modifiers.new('Weighted corner normals','WEIGHTED_NORMAL');mod.keep_sharp=True;mod.weight=40
def box(name,loc,dims,m,group='Shell',bevelled=True,vertical=False):
    bpy.ops.mesh.primitive_cube_add(size=1,location=loc);o=bpy.context.object;o.dimensions=dims;active(o);bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    register(o,name,m,group);uv_world(o,vertical=vertical)
    if bevelled:bevel(o,min(.010,min(dims)*.2))
    return o
def beam(name,a,b,width,depth,m,group='Trim'):
    a,b=Vector(a),Vector(b);o=box(name,(a+b)/2,(width,depth,(b-a).length),m,group)
    o.rotation_euler=(b-a).to_track_quat('Z','Y').to_euler();return o
def cyl(name,loc,r,depth,m,group='Props',vertices=16):
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices,radius=r,depth=depth,location=loc);o=bpy.context.object
    register(o,name,m,group);uv_world(o);bevel(o,.004)
    for p in o.data.polygons:p.use_smooth=len(p.vertices)==4
    return o
def lathe(name,profile,loc,m,group='Props',segments=20):
    vs=[];fs=[]
    for z,r in profile:
        for j in range(segments):
            t=j*2*math.pi/segments;vs.append((loc[0]+r*math.cos(t),loc[1]+r*math.sin(t),loc[2]+z))
    for i in range(len(profile)-1):
        for j in range(segments):
            a=i*segments+j;b=i*segments+(j+1)%segments;c=b+segments;d=a+segments;fs.append((a,b,c,d))
    fs.extend([tuple(reversed(range(segments))),tuple(range((len(profile)-1)*segments,len(profile)*segments))])
    mesh=bpy.data.meshes.new(name);mesh.from_pydata(vs,[],fs);mesh.update();o=bpy.data.objects.new(name,mesh);asset.objects.link(o);o.data.materials.append(m);groups.setdefault(group,[]).append(o);uv_world(o)
    for p in mesh.polygons:p.use_smooth=len(p.vertices)==4
    return o
def torus(name,loc,major,minor,m,rot=(0,0,0),group='Props'):
    bpy.ops.mesh.primitive_torus_add(major_segments=16,minor_segments=6,location=loc,major_radius=major,minor_radius=minor,rotation=rot)
    return register(bpy.context.object,name,m,group)

# Footprint and real openings. The back and sides are authored interpretations.
W,D=7.2,5.8;front=-D/2;back=D/2;eave=3.18;ridge=4.93;floor=.18;doorW=2.5;doorTop=2.75
box('Foundation',(0,0,.07),(W,D,.14),wood)
for k in range(24):box('FloorBoard',(0,-2.8+k*.244,.15),(7.0,.236,.09),wood,'Interior')
for row in range(14):
    z=.29+row*.213;h=.206
    for sy in [-1,1]:
        y=sy*D/2
        spans=[(-3.6,-doorW/2),(doorW/2,3.6)] if sy<0 and z-h/2<doorTop else [(-3.6,3.6)]
        for a,b in spans:box('FrontBoards' if sy<0 else 'BackBoards',((a+b)/2,y,z),(b-a,.16,h),cow)
    for sx in [-1,1]:
        spans=[(-2.9,-.60),(.80,2.9)] if 1.08<z<2.41 else [(-2.9,2.9)]
        for a,b in spans:box('SideBoards',(sx*3.6,(a+b)/2,z),(.16,b-a,h),cow)
# True triangular gables built as horizontal clipped boards.
for sy in [-1,1]:
    for row in range(9):
        bottom=eave+row*.205;top=min(ridge,bottom+.198)
        if top<=bottom:continue
        xb=3.6*(ridge-bottom)/(ridge-eave);xt=3.6*(ridge-top)/(ridge-eave)
        vs=[(-xb,sy*2.90-.08,bottom),(xb,sy*2.90-.08,bottom),(xt,sy*2.90-.08,top),(-xt,sy*2.90-.08,top),(-xb,sy*2.90+.08,bottom),(xb,sy*2.90+.08,bottom),(xt,sy*2.90+.08,top),(-xt,sy*2.90+.08,top)]
        fs=[(0,3,2,1),(4,5,6,7),(0,1,5,4),(1,2,6,5),(2,3,7,6),(3,0,4,7)]
        mesh=bpy.data.meshes.new('GableBoards');mesh.from_pydata(vs,[],fs);mesh.update();o=bpy.data.objects.new('GableBoard',mesh);asset.objects.link(o);o.data.materials.append(cow);groups.setdefault('Shell',[]).append(o);uv_world(o);bevel(o)
for x in [-3.63,3.63]:
    for y in [front,back]:box('CornerPost',(x,y,1.72),(.19,.24,3.15),pale,'Trim')
for x in [-1.31,1.31]:box('DoorJamb',(x,front-.06,1.53),(.16,.27,2.74),pale,'Trim')
box('DoorLintel',(0,front-.06,2.82),(2.80,.28,.18),pale,'Trim')
box('Threshold',(0,front-.20,.16),(2.75,.65,.12),wood,'Trim')
box('EntryStep',(0,front-.62,.06),(3.1,.55,.12),wood,'Trim')
# Window holes and inset muntins, visible consistently from inside/outside.
for sx in [-1,1]:
    for y in [-.67,.87]:box('WindowFrame',(sx*3.69,y,1.74),(.22,.14,1.40),pale,'Windows')
    for z in [1.04,2.44]:box('WindowFrame',(sx*3.69,.10,z),(.25,1.66,.14),pale,'Windows')
    box('WindowSill',(sx*3.72,.10,1.01),(.38,1.78,.11),pale,'Windows')
    for y in [-.29,.49]:
        for z in [1.40,2.08]:box('SmallOldGlassPane',(sx*3.615,y,z),(.024,.67,.58),glass,'Windows',False)
    box('WindowMullion',(sx*3.71,.1,1.74),(.14,.055,1.29),pale,'Windows')
    box('WindowCrossbar',(sx*3.71,.1,1.74),(.14,1.40,.055),pale,'Windows')
# Wooden double doors, assembled then parented to hinge origins.
door_groups=[]
for side in ['Left','Right']:
    left=side=='Left';a,b=(-1.235,-.025) if left else (.025,1.235);name='Door_'+side;door_groups.append(name)
    for j in range(6):box('DoorPlank',((a+b)/2+(j-2.5)*(b-a)/6,front-.10,1.455),((b-a)/6-.008,.105,2.55),pale,name,vertical=True)
    for z in [.44,2.47]:box('DoorRail',((a+b)/2,front-.179,z),(b-a-.05,.065,.145),pale,name)
    beam('DoorDiagonal',(a+.08,front-.18,.50),(b-.08,front-.18,2.42),.12,.063,pale,name)
    for z in [.48,2.4]:
        hx=a+.3 if left else b-.3
        box('IronStrapHinge',(hx,front-.226,z),(.55,.045,.065),iron,name)
        for xx in [hx-.21,hx,hx+.21]:
            rivet=cyl('HingeRivet',(xx,front-.253,z),.021,.024,iron,name,8);rivet.rotation_euler[0]=math.pi/2
    hx=b-.14 if left else a+.14
    box('LatchPlate',(hx,front-.234,1.42),(.065,.04,.22),iron,name)
    torus('DoorPull',(hx,front-.286,1.40),.064,.012,iron,(math.pi/2,0,0),name)
# Roof sheets with real shallow corrugations, and wooden fascia boards.
for sx in [-1,1]:
    vs=[];fs=[];ny=100
    for j in range(ny+1):
        y=-3.20+j*6.40/ny;offset=.023*(1+math.cos(j*math.pi/2))
        for t in [0,1]:vs.append((sx*t*3.95,y,ridge+.065-t*1.92+offset))
    for j in range(ny):fs.append((j*2,j*2+1,j*2+3,j*2+2) if sx>0 else (j*2+2,j*2+3,j*2+1,j*2))
    me=bpy.data.meshes.new('RoofSheet');me.from_pydata(vs,[],fs);me.update();o=bpy.data.objects.new('RoofSheet',me);asset.objects.link(o);o.data.materials.append(roofmat);groups.setdefault('Roof',[]).append(o)
    solid=o.modifiers.new('Sheet thickness','SOLIDIFY');solid.thickness=.055;uv_world(o)
    for y in [-3.19,3.19]:beam('GableFascia',(0,y,ridge+.065),(sx*3.98,y,3.05),.19,.20,pale,'Roof')
    beam('EaveFascia',(sx*3.95,-3.20,3.045),(sx*3.95,3.20,3.045),.19,.18,pale,'Roof')
    for y in [-2.75,0,2.75]:beam('RoofRafter',(0,y,4.81),(sx*3.54,y,3.12),.14,.14,wood,'Interior')
beam('RoofRidgeCap',(0,-3.28,5.00),(0,3.28,5.00),.13,.13,roofmat,'Roof')
# Main word sign and deterministic lowercase lettering.
for i in range(4):box('MilkSignBoard',(0,front-.20,3.35+i*.205),(3.18,.15,.198),cream,'MilkSign')
font=bpy.data.curves.new('MilkLettering','FONT');font.body='milk';font.align_x='CENTER';font.align_y='CENTER';font.size=1;font.extrude=.009;font.bevel_depth=.002;font.resolution_u=5
font.font=bpy.data.fonts.load('/System/Library/Fonts/Supplemental/Georgia Bold.ttf')
o=bpy.data.objects.new('milk_Letters',font);asset.objects.link(o);o.location=(0,front-.295,3.67);o.rotation_euler=(math.pi/2,0,0);font.materials.append(iron);bpy.context.view_layer.update();o.scale=(1.10,1.10,1.10);active(o);bpy.ops.object.convert(target='MESH');groups.setdefault('MilkSign',[]).append(o)
for x in [-1.44,1.44]:
    for z in [3.30,4.01]:
        o=cyl('SignFixing',(x,front-.30,z),.032,.025,iron,'MilkSign',10);o.rotation_euler[0]=math.pi/2
# Cow contour produces an actual closed solid sign, not a rectangular sprite.
contour=json.load(open(ROOT/'cow-outline.json'));uv=contour['points_uv'];iw,ih=contour['image_size'];sw=5.15;sh=sw*ih/iw
verts2=[Vector(((u-.5)*sw,0,4.48+v*sh)) for u,v in uv];n=len(verts2)
verts=[(p.x,front+.17,p.z) for p in verts2]+[(p.x,front+.31,p.z) for p in verts2]
lookup={tuple(p):i for i,p in enumerate(verts2)};faces=[]
for tri in tessellate_polygon([verts2]):
    inds=[p if isinstance(p,int) else lookup[tuple(p)] for p in tri]
    if (verts2[inds[1]]-verts2[inds[0]]).cross(verts2[inds[2]]-verts2[inds[0]]).length<1e-7:continue
    faces.extend([tuple(reversed(inds)),tuple(i+n for i in inds)])
for i in range(n):j=(i+1)%n;faces.append((i,j,j+n,i+n))
me=bpy.data.meshes.new('CowSignSolid');me.from_pydata(verts,[],faces);me.update();o=bpy.data.objects.new('Cow_Silhouette',me);asset.objects.link(o);o.data.materials.append(signmat);o.data.materials.append(wood);groups.setdefault('CowSign',[]).append(o)
layer=me.uv_layers.new(name='UVMap')
for poly in me.polygons:
    poly.material_index=1 if len(poly.vertices)==4 else 0
    for li in poly.loop_indices:layer.data[li].uv=uv[me.loops[li].vertex_index%n]
# Ensure outward facing normals for all closed parts of the extruded silhouette.
bm=bmesh.new();bm.from_mesh(me);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(me);bm.free()
for x in [-1.55,1.55]:
    zroof=ridge-abs(x)/3.6*(ridge-eave)
    box('CowSignSupport',(x,front+.30,(zroof+5.35)/2),(.11,.14,5.35-zroof+.08),iron,'CowSign')
# Modest 1931 counter, shelving and dairy goods.
box('ShopCounter',(0,1.05,.67),(3.6,.75,.98),wood,'Interior')
box('CounterTop',(0,1.03,1.20),(3.77,.91,.11),cream,'Interior')
for i in range(16):box('CounterSlat',(-1.72+i*.229,.656,.71),(.213,.028,.88),wood,'Interior')
for x in [-2.4,0,2.4]:box('ShelfUpright',(x,2.62,1.65),(.09,.13,2.70),wood,'Interior')
for z in [.56,1.27,1.98,2.69]:box('ShelfBoard',(0,2.51,z),(5.15,.51,.065),wood,'Interior')
def bottle(x,y,z,scale=1):
    p=[(0,.07),(.025,.085),(.28,.085),(.33,.047),(.43,.045),(.45,.055)]
    lathe('MilkBottle',[(a*scale,b*scale) for a,b in p],(x,y,z),ceramic,segments=12)
    cyl('BottleCap',(x,y,z+.458*scale),.055*scale,.018*scale,dark,vertices=12)
for z in [.60,1.31,2.02]:
    for j in range(15):
        if random.random()>.24:bottle(-2.16+j*.295,2.48,z,random.uniform(.75,1.06))
for x in [-1.2,-.97,-.74]:bottle(x,.97,1.26)
# Countertop mechanical scale with shallow bowl.
box('ScaleBase',(.84,1.03,1.30),(.50,.34,.10),iron,'Props')
cyl('ScalePedestal',(.84,1.03,1.49),.075,.32,iron)
lathe('ScaleBowl',[(0,.05),(.04,.17),(.13,.27),(.145,.28),(.145,.26),(.06,.145)],(.84,1.03,1.65),metal)
def churn(x,y,s=1):
    z=.20;p=[(0,.21),(.05,.25),(.64,.25),(.73,.19),(.79,.145),(.95,.145),(.98,.17)]
    lathe('MilkChurn',[(a*s,b*s) for a,b in p],(x,y,z),metal)
    cyl('ChurnLid',(x,y,z+1.00*s),.175*s,.04*s,metal)
    for sx in [-1,1]:torus('ChurnHandle',(x+sx*.235*s,y,z+.72*s),.081*s,.014*s,metal,(math.pi/2,0,0))
    torus('ChurnBand',(x,y,z+.14*s),.25*s,.011*s,metal)
for x,y,s in [(-2.50,-1.75,1),(-2,-1.60,.78),(2.43,.2,.91)]:churn(x,y,s)
# One old enamel gooseneck lamp above the word sign.
beam('LampBracket',(0,front-.03,4.47),(0,front-.57,4.47),.052,.052,iron,'Lamp')
beam('LampNeck',(0,front-.57,4.47),(0,front-.57,4.27),.052,.052,iron,'Lamp')
lathe('EnamelLampShade',[(0,.23),(.05,.25),(.19,.11),(.21,.06)],(0,front-.57,4.07),iron,'Lamp')
cyl('LampGlow',(0,front-.57,4.075),.135,.035,bulb,'Lamp')
for o in groups['Lamp']:o.location.z+=.22
# Barrel planters and modest low-poly flowers, kept as separate optional meshes.
for x in [-2.50,2.50]:
    y=-3.33;lathe('FlowerBarrel',[(0,.32),(.08,.38),(.50,.43),(.65,.39)],(x,y,.02),wood,'Planters',20)
    for z in [.12,.53]:torus('BarrelHoop',(x,y,z),.4,.019,iron,group='Planters')
    cyl('BarrelSoil',(x,y,.64),.375,.025,soil,'Planters',20)
    for j in range(24):
        ang=j*2.4;r=random.uniform(.03,.44);xx=x+math.cos(ang)*r;yy=y+math.sin(ang)*r;zz=random.uniform(.80,1.19)
        beam('FlowerStem',(x+(xx-x)*.6,y+(yy-y)*.6,.63),(xx,yy,zz),.014,.014,leaves,'Planters')
        for k in range(3):
            theta=ang+k*2.1;o=box('Leaf',(xx+math.cos(theta)*.08,yy+math.sin(theta)*.08,zz-.14),(.18,.07,.012),leaves,'Planters',False);o.rotation_euler=(.3,k*.35,theta)
        for k in range(5):
            theta=k*2*math.pi/5;bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=1,radius=.055,location=(xx+.047*math.cos(theta),yy+.047*math.sin(theta),zz));o=register(bpy.context.object,'Petal',flower_mats[j%3],'Planters');o.scale=(1,1,.40)
# Simple collider boxes hidden in Blender and converted to Unity BoxColliders.
colliders=[]
for name,loc,dim in [('Floor',(0,0,.07),(7.2,5.8,.14)),('Back',(0,2.9,1.63),(7.2,.20,3.1)),('Left',(-3.6,0,1.63),(.2,5.8,3.1)),('Right',(3.6,0,1.63),(.2,5.8,3.1)),('FrontLeft',(-2.45,front,1.63),(2.3,.20,3.1)),('FrontRight',(2.45,front,1.63),(2.3,.20,3.1)),('Lintel',(0,front,2.97),(2.6,.2,.44)),('Counter',(0,1.05,.67),(3.6,.8,1.0))]:
    o=box('COL_'+name,loc,dim,iron,'Collision',False);o.hide_render=True;o.display_type='WIRE';colliders.append(o)
# Apply modifiers and group geometry for manageable draw calls. Keep doors independent.
for group,objects in list(groups.items()):
    if group=='Collision':continue
    for o in objects:
        active(o)
        for mod in list(o.modifiers):bpy.ops.object.modifier_apply(modifier=mod.name)
    bpy.ops.object.select_all(action='DESELECT')
    for o in objects:o.select_set(True)
    bpy.context.view_layer.objects.active=objects[0];bpy.ops.object.join();o=bpy.context.object;o.name=group
    bm=bmesh.new();bm.from_mesh(o.data)
    bmesh.ops.remove_doubles(bm,verts=list(bm.verts),dist=0.000001)
    bmesh.ops.dissolve_degenerate(bm,edges=list(bm.edges),dist=0.0000001)
    bad=[f for f in bm.faces if f.calc_area()<1e-12]
    if bad:bmesh.ops.delete(bm,geom=bad,context='FACES_ONLY')
    bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces))
    bm.to_mesh(o.data);bm.free();o.data.update()
    bad_ids={p.index for p in o.data.polygons if p.area<1e-12}
    if bad_ids:
        bm=bmesh.new();bm.from_mesh(o.data);bm.faces.ensure_lookup_table()
        bmesh.ops.delete(bm,geom=[bm.faces[i] for i in bad_ids],context='FACES_ONLY')
        bm.to_mesh(o.data);bm.free();o.data.update()
    bpy.context.scene.cursor.location=(0,0,0);bpy.ops.object.origin_set(type='ORIGIN_CURSOR')
    if group in door_groups:
        bpy.context.scene.cursor.location=(-1.245 if group.endswith('Left') else 1.245,front-.05,.18);bpy.ops.object.origin_set(type='ORIGIN_CURSOR')
        o.rotation_euler.z=math.radians(-115 if group.endswith('Left') else 0)
    groups[group]=[o]
bpy.context.scene.cursor.location=(0,0,0)
root=bpy.data.objects.new('MilkShop1931',None);asset.objects.link(root)
for o in list(asset.objects):
    if o!=root:o.parent=root
for name,loc in [('FrontMarker',(0,-3.5,0)),('UpMarker',(0,0,1)),('EntryClearance',(0,-2.85,1.5)),('GroundOrigin',(0,0,0))]:
    o=bpy.data.objects.new(name,None);asset.objects.link(o);o.parent=root;o.location=loc
bpy.context.view_layer.update()
# Save editable source and exports before adding any review-only floor or lights.
for im in bpy.data.images:
    if im.source=='FILE':im.pack()
asset_objects=list(asset.objects)
bpy.ops.object.select_all(action='DESELECT')
for o in asset_objects:
    o.hide_set(False);o.select_set(True)
bpy.context.view_layer.objects.active=root
bpy.ops.export_scene.fbx(filepath=str(EXP/'MilkShop1931.fbx'),use_selection=True,object_types={'MESH','EMPTY'},apply_unit_scale=True,apply_scale_options='FBX_SCALE_UNITS',axis_forward='-Z',axis_up='Y',bake_anim=False,path_mode='COPY',embed_textures=False,use_mesh_modifiers=True)
# GLB contains visual meshes only; Unity collision comes from the FBX proxy nodes.
for o in colliders:o.select_set(False)
bpy.ops.export_scene.gltf(filepath=str(EXP/'MilkShop1931.glb'),export_format='GLB',use_selection=True,export_apply=True,export_yup=True,export_texcoords=True,export_normals=True,export_materials='EXPORT',export_extras=True)
bounds=[o.matrix_world@Vector(c) for o in asset_objects if o.type=='MESH' and o not in colliders for c in o.bound_box]
triangles=0
for o in asset_objects:
    if o.type=='MESH' and o not in colliders:o.data.calc_loop_triangles();triangles+=len(o.data.loop_triangles)
stats={'asset':'MilkShop1931','units':'metres','construction':'reference-guided authored geometry; NOT Meshy neural inference','triangles':triangles,'visual_meshes':len([o for o in asset_objects if o.type=='MESH' and o not in colliders]),'materials':len(materials),'footprint_m':[7.2,5.8],'bounds_min':[min(v[i] for v in bounds) for i in range(3)],'bounds_max':[max(v[i] for v in bounds) for i in range(3)],'door_opening_m':[2.5,2.57],'doors':['Door_Left','Door_Right'],'cow_sign_thickness_m':.14,'back_and_sides':'authored approximation; not visible in source','map_placement':'standalone asset; town B04 footprint and layout not changed','collider_proxies':len(colliders)}
(ROOT/'verification/model-stats.json').write_text(json.dumps(stats,indent=2))
# Review scene, clearly separate from the exported asset.
for o in colliders:o.hide_render=True;o.hide_set(True)
ground=box('ReviewGround',(0,0,-.15),(200,200,.2),mat('ReviewGroundMat',(.23,.26,.25)),'ReviewGround',False);move(ground,review)
def light(name,typ,loc,energy,color,size=5):
    d=bpy.data.lights.new(name,typ);d.energy=energy;d.color=color
    if typ=='AREA':d.shape='DISK';d.size=size
    o=bpy.data.objects.new(name,d);review.objects.link(o);o.location=loc;o.rotation_euler=(Vector((0,0,2))-o.location).to_track_quat('-Z','Y').to_euler();return o
light('LargeSoftbox','AREA',(-8,-10,13),1900,(1,.85,.65),9)
light('SkyFill','AREA',(8,-4,10),1150,(.68,.79,1),9)
light('Rim','AREA',(2,7,11),1800,(1,.91,.75),7)
light('InteriorWarm','AREA',(0,0,2.8),110,(1,.65,.33),2)
scene.world=bpy.data.worlds.new('ReviewWorld');scene.world.use_nodes=True;scene.world.node_tree.nodes['Background'].inputs[0].default_value=(.40,.47,.57,1);scene.world.node_tree.nodes['Background'].inputs[1].default_value=.45
camdata=bpy.data.cameras.new('ReviewCamera');cam=bpy.data.objects.new('ReviewCamera',camdata);review.objects.link(cam);scene.camera=cam
scene.render.engine='CYCLES';scene.cycles.samples=32;scene.cycles.use_denoising=True
scene.render.resolution_x=1400;scene.render.resolution_y=1200;scene.render.resolution_percentage=100
scene.view_settings.view_transform='AgX';scene.render.image_settings.file_format='PNG'
def camera(pos,target,lens=48):cam.location=pos;cam.rotation_euler=(Vector(target)-cam.location).to_track_quat('-Z','Y').to_euler();camdata.lens=lens
camera((-11,-16,9.2),(0,0,3.65),47)
bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/'MilkShop1931.blend'))
for name,pos,target,lens in [('model-front',(-11,-16,9.2),(0,0,3.65),47),('model-back',(11,15,8),(0,0,3.7),47),('model-player',(-.9,-7.1,1.72),(0,.7,1.55),22)]:
    camera(pos,target,lens);scene.render.filepath=str(PRE/(name+'.png'));bpy.ops.render.render(write_still=True)
print('MILKSHOP_EXPORT_PASS',json.dumps(stats))
