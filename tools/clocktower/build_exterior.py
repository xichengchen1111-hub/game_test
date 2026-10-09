"""Reference-guided, editable clock-tower exterior. Blender 4.5.9 LTS.

This is authored geometry reconstructed from the approved image, not a Meshy
service result or neural image-to-mesh inference. Unity supplies the shared
interior, physical walls, moving door, exact clock faces/hands and history text.
"""
import bpy, bmesh, json, math, random, shutil, hashlib, sys, re
from pathlib import Path
from mathutils import Vector
import numpy as np

REPO=Path(__file__).resolve().parents[2]
# The shared C# layout is authoritative; preserve physical units and avoid a
# scale transform stretching the accepted door, masonry and decorative kit.
_layout=(REPO/'game/Assets/Scripts/ClockTowerInteriorLayout.cs').read_text()
def layout_number(name,expected):
    match=re.search(r'const\s+(?:float|int)\s+'+name+r'\s*=\s*([0-9.+-]+)f?\s*;',_layout)
    if not match:raise RuntimeError('Shared layout needs numeric constant '+name)
    value=float(match.group(1))
    if abs(value-expected)>.0001:raise RuntimeError('Unexpected shared layout '+name+': '+str(value))
    return value
HEIGHT=layout_number('ExteriorHeight',20.0)
GEAR_FLOOR=layout_number('MachineFloor',15.0)
GROUND=layout_number('GroundFloor',.12)
MIDDLE_FLOOR=layout_number('MiddleFloor',8.530435)
WALL_TOP=layout_number('InnerWallHeight',18.3)
CLOCK_Z=layout_number('DialHeight',16.72)
DIAL_DIAMETER=layout_number('DialDiameter',2.2)
RING_INNER=DIAL_DIAMETER/2+.005
SRC=REPO/'assets/source/clocktower/exterior'
OUT=REPO/'game/Assets/Art/ClockTower1931'
TEX=SRC/'textures'; PRE=SRC/'previews'; VERIFY=SRC/'verification'
for p in (SRC,OUT,TEX,PRE,VERIFY):p.mkdir(parents=True,exist_ok=True)
random.seed(1816)
bpy.ops.wm.read_factory_settings(use_empty=True)
scene=bpy.context.scene;scene.unit_settings.system='METRIC';scene.unit_settings.scale_length=1
asset=bpy.data.collections.new('ClockTower1931_Export');scene.collection.children.link(asset)
review=bpy.data.collections.new('Review_Only_UnityOverlays');scene.collection.children.link(review)
parts={}; materials={};tex_manifest={}

def png(name,arr):
    h,w=arr.shape[:2];im=bpy.data.images.new(name,width=w,height=h,alpha=True)
    rgba=np.ones((h,w,4),dtype=np.float32);rgba[:,:,:3]=arr
    im.pixels.foreach_set(rgba.ravel());im.filepath_raw=str(TEX/name);im.file_format='PNG';im.save()
    shutil.copy2(TEX/name,OUT/name)
    return im

def texture_pair(prefix,kind):
    n=1024; yy,xx=np.mgrid[0:n,0:n].astype(np.float32);rng=np.random.default_rng(1816+len(prefix))
    noise=rng.random((n,n)).astype(np.float32)-.5
    large=.3*np.sin(xx*.017+np.sin(yy*.03))+.3*np.sin(yy*.011+xx*.004)
    if kind=='brick':
        # 1.44 m square tile, four running-bond bricks x ten courses.
        row=np.floor(yy/(n/10)); by=np.mod(yy,n/10)/(n/10)
        bx=np.mod(xx+(row%2)*n/8,n/4)/(n/4)
        edge=np.minimum(np.minimum(bx,1-bx)*.36,np.minimum(by,1-by)*.144)
        relief=np.clip((edge-.002)/.009,0,1)
        brickvar=np.sin(np.floor(bx+xx/(n/4)+row*.5)*37.11+row*17.42)*.012
        col=np.stack([.79+brickvar+.012*large+.007*noise,.745+brickvar+.012*large+.007*noise,.652+brickvar+.011*large+.006*noise],2)
        col*= (.92+.08*relief[:,:,None]);height=.70*relief+.006*noise+.015*large
        strength=3.0
    elif kind=='stone':
        cloud=.018*large+.009*noise
        col=np.stack([.84+cloud,.791+cloud,.691+cloud],2)
        height=.01*noise+.025*large;strength=1.2
    else:
        grain=np.sin(xx*.058+np.sin(yy*.023)*4)+.3*np.sin(xx*.19+yy*.02)
        variation=.020*large+.012*grain+.012*noise
        col=np.stack([.215+variation,.249+variation,.282+variation],2)
        height=.02*noise+.05*grain;strength=1.4
    png(prefix+'-BaseColor.png',np.clip(col,0,1))
    gy,gx=np.gradient(height);normal=np.stack([-gx*strength,-gy*strength,np.ones_like(gx)],2)
    normal/=np.linalg.norm(normal,axis=2,keepdims=True);png(prefix+'-Normal.png',normal*.5+.5)
    tex_manifest[prefix]={'base_color':prefix+'-BaseColor.png','normal':prefix+'-Normal.png','tile_metres':1.44 if kind=='brick' else 1.0}

for p,k in [('IvoryBrick','brick'),('CreamStone','stone'),('Slate','slate')]:texture_pair(p,k)

def material(name,color,rough=.8,metal=0,tex=None):
    m=bpy.data.materials.new(name);m.use_nodes=True;m.diffuse_color=(*color,1)
    p=m.node_tree.nodes.get('Principled BSDF');p.inputs['Base Color'].default_value=(*color,1);p.inputs['Roughness'].default_value=rough;p.inputs['Metallic'].default_value=metal
    if tex:
        nd=m.node_tree.nodes.new('ShaderNodeTexImage');nd.image=bpy.data.images.load(str(TEX/(tex+'-BaseColor.png')),check_existing=True)
        m.node_tree.links.new(nd.outputs['Color'],p.inputs['Base Color'])
        nm=m.node_tree.nodes.new('ShaderNodeTexImage');nm.image=bpy.data.images.load(str(TEX/(tex+'-Normal.png')),check_existing=True);nm.image.colorspace_settings.name='Non-Color'
        normal=m.node_tree.nodes.new('ShaderNodeNormalMap');normal.inputs['Strength'].default_value=.70 if tex=='IvoryBrick' else .35
        m.node_tree.links.new(nm.outputs['Color'],normal.inputs['Color']);m.node_tree.links.new(normal.outputs['Normal'],p.inputs['Normal'])
    materials[name]=m;return m
brick=material('Tower_IvoryBrick',(.79,.745,.652),.85,tex='IvoryBrick')
stone=material('Tower_CreamStone',(.84,.791,.691),.75,tex='CreamStone')
slate=material('Tower_Slate',(.215,.249,.282),.72,tex='Slate')
lead=material('Tower_Lead',(.185,.20,.208),.58,.42)
oak=material('Tower_DarkOak',(.115,.083,.052),.8)
recess=material('Tower_Recess',(.032,.029,.025),.96)
bronze=material('Tower_Bronze',(.26,.207,.126),.46,.65)

def active(o):
    bpy.ops.object.select_all(action='DESELECT');o.select_set(True);bpy.context.view_layer.objects.active=o
def move(o,c=asset):
    for old in list(o.users_collection):old.objects.unlink(o)
    c.objects.link(o)
def reg(o,name,mat):
    o.name=name;move(o);o.data.materials.append(mat);parts.setdefault(mat.name,[]).append(o);return o
def uv(o,scale=1):
    layer=o.data.uv_layers.active or o.data.uv_layers.new(name='UVMap')
    for p in o.data.polygons:
        n=o.matrix_world.to_3x3()@p.normal;axis=max(range(3),key=lambda i:abs(n[i]))
        for li in p.loop_indices:
            v=o.matrix_world@o.data.vertices[o.data.loops[li].vertex_index].co
            a,b=(v.x,v.y) if axis==2 else ((v.x,v.z) if axis==1 else (v.y,v.z))
            layer.data[li].uv=(a/scale,b/scale)
def bevel(o,w=.006,segments=2):
    mod=o.modifiers.new('Crafted rounded arris','BEVEL');mod.width=w;mod.segments=segments
    mod=o.modifiers.new('Weighted stone normals','WEIGHTED_NORMAL');mod.keep_sharp=True
def box(name,loc,size,mat,bev=.006):
    bpy.ops.mesh.primitive_cube_add(size=1,location=loc);o=bpy.context.object;o.dimensions=size
    active(o);bpy.ops.object.transform_apply(location=False,rotation=False,scale=True);reg(o,name,mat);uv(o,1.44 if mat==brick else 1)
    if bev:bevel(o,min(bev,min(size)*.2))
    return o
def mesh(name,verts,faces,mat,bev=0):
    me=bpy.data.meshes.new(name);me.from_pydata(verts,[],faces);me.update();o=bpy.data.objects.new(name,me);asset.objects.link(o);me.materials.append(mat);parts.setdefault(mat.name,[]).append(o)
    bm=bmesh.new();bm.from_mesh(me);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(me);bm.free();uv(o,1.44 if mat==brick else 1)
    if bev:bevel(o,bev)
    return o
def pnt(side,u,z,d):
    a=side*math.pi/2;return (u*math.cos(a)+d*math.sin(a),u*math.sin(a)-d*math.cos(a),z)
def fbox(name,side,u,z,d,w,h,depth,mat,bev=.006):
    o=box(name,pnt(side,u,z,d),(w,depth,h),mat,bev);o.rotation_euler.z=side*math.pi/2;uv(o,1.44 if mat==brick else 1);return o
def path(name,points,r,mat):
    cu=bpy.data.curves.new(name,'CURVE');cu.dimensions='3D';cu.resolution_u=1;cu.bevel_resolution=1;cu.bevel_depth=r
    sp=cu.splines.new('POLY');sp.points.add(len(points)-1)
    for p,co in zip(sp.points,points):p.co=(*co,1)
    o=bpy.data.objects.new(name,cu);asset.objects.link(o);cu.materials.append(mat);active(o);bpy.ops.object.convert(target='MESH');parts.setdefault(mat.name,[]).append(o);uv(o);return o
def beam(name,a,b,w,depth,mat):
    a,b=Vector(a),Vector(b);o=box(name,(a+b)*.5,(w,depth,(b-a).length),mat,.004);o.rotation_euler=(b-a).to_track_quat('Z','Y').to_euler();return o
def ring(name,side,u,z,profile,mat,segments=96):
    vs=[pnt(side,u+r*math.cos(i*2*math.pi/segments),z+r*math.sin(i*2*math.pi/segments),d) for r,d in profile for i in range(segments)]
    fs=[]
    for j in range(len(profile)):
        for i in range(segments):fs.append((j*segments+i,j*segments+(i+1)%segments,((j+1)%len(profile))*segments+(i+1)%segments,((j+1)%len(profile))*segments+i))
    return mesh(name,vs,fs,mat)
def arch(name,side,u,z,rx,rz,thick,d,depth,mat,start=0,end=math.pi,segments=16):
    vs=[]
    for dd in [d-depth/2,d+depth/2]:
        for rr in [0,thick]:
            for i in range(segments+1):
                t=start+(end-start)*i/segments;vs.append(pnt(side,u+(rx+rr)*math.cos(t),z+(rz+rr)*math.sin(t),dd))
    n=segments+1;fs=[]
    for i in range(segments):fs.extend([(i,i+1,n+i+1,n+i),(2*n+i,3*n+i,3*n+i+1,2*n+i+1),(i,2*n+i,2*n+i+1,i+1),(n+i,n+i+1,3*n+i+1,3*n+i)])
    fs.extend([(0,n,3*n,2*n),(n-1,2*n-1,4*n-1,3*n-1)])
    return mesh(name,vs,fs,mat,.003)
def leaf(name,side,u,z,d,length,width,angle=0):
    # Low relief acanthus leaf, convex centre and fluted vein, closed backing.
    vs=[]
    for j in range(9):
        t=j/8;w=math.sin(math.pi*t)**.75*width*.5
        for x in [-w,0,w]:
            zz=(t-.5)*length;uu=x*math.cos(angle)-zz*math.sin(angle);zz=x*math.sin(angle)+zz*math.cos(angle)
            vs.append(pnt(side,u+uu,z+zz,d+(.024*math.sin(math.pi*t) if x==0 else .004)))
    fs=[]
    for j in range(8):
        for k in range(2):fs.append((j*3+k,j*3+k+1,(j+1)*3+k+1,(j+1)*3+k))
    o=mesh(name,vs,fs,stone);sol=o.modifiers.new('Relief backing','SOLIDIFY');sol.thickness=.006
    return o
def rosette(side,u,z,d,r=.11):
    for k in range(8):
        t=k*math.pi/4;leaf('Carved rosette petal',side,u+math.sin(t)*r*.47,z+math.cos(t)*r*.47,d,r*.9,r*.40,-t)
    ring('Rosette boss',side,u,z,[(r*.18,d),(r*.20,d+.035),(r*.11,d+.05),(.002,d+.051)],stone,20)

# The three levels correspond to the real ground, intermediate and machine
# platforms. Repetition adds real masonry courses, never stretched brick UVs.
def cornice_band(side,z):
    for dz,w,h,d in [(-.08,3.76,.09,1.80),(.005,3.84,.08,1.81),(.06,3.73,.045,1.80)]:
        fbox('Floor-level carved string course',side,0,z+dz,d,w,h,.10,stone,.008)

def ventilation(side,base,width=.67,height=.87):
    rx=width/2;zspring=base+height;centre=base+height/2
    fbox('Closed ventilation back',side,0,centre,1.825,width,height,.036,recess,.003)
    arch('Closed vent rounded backing',side,0,zspring,.001,.001,rx,1.827,.032,recess,segments=20)
    for row in range(math.ceil((height+rx)/.112)):
        z=base+.065+row*.112;half=rx-.02 if z<=zspring else math.sqrt(max(0,(rx-.02)**2-(z-zspring)**2))
        if half>.025:
            ob=fbox('Solid angled oak louvre',side,0,z,1.862,half*2,.065,.094,oak,.004);ob.rotation_euler.rotate_axis('X',math.radians(-20))
            fbox('Louvre catching edge',side,0,z+.027,1.91,half*2,.012,.014,lead,.002)
    for u in [-(rx+.043),rx+.043]:fbox('Louvre dressed jamb',side,u,centre,1.857,.115,height,.13,stone,.008)
    arch('Louvre arch surround',side,0,zspring,rx-.012,rx-.012,.124,1.86,.14,stone,segments=24)
    for i in range(7):
        a=i*math.pi/7+.012;b=(i+1)*math.pi/7-.012
        arch('Louvre arch voussoir',side,0,zspring,rx-.010,rx-.010,.126,1.870,.15,stone,a,b,4)
    fbox('Louvre sill',side,0,base-.035,1.88,width+.31,.115,.25,stone,.008)
    fbox('Vent arch keystone',side,0,zspring+rx+.08,1.914,.14,.21,.15,stone,.009)

for s in range(4):
    if s==0:
        for u in [-1.175,1.175]:fbox('Limewashed entrance pier',s,u,WALL_TOP/2,1.798,1.25,WALL_TOP,.032,brick,0)
        fbox('Limewashed entrance lintel',s,0,(WALL_TOP+2.27)/2,1.798,1.10,WALL_TOP-2.27,.032,brick,0)
    else:fbox('Limewashed facade',s,0,WALL_TOP/2,1.798,3.6,WALL_TOP,.032,brick,0)
    intervals=[(-1.94,-.57),(.57,1.94)] if s==0 else [(-1.94,1.94)]
    for lo,hi in intervals:
        for z,h,d,dep in [(.12,.24,1.86,.15),(.335,.19,1.846,.12),(.465,.07,1.84,.15),(.535,.07,1.81,.12),(.584,.028,1.83,.13)]:
            fbox('Profiled plinth',s,(lo+hi)/2,z,d,hi-lo,h,dep,stone,.008)
        n=max(1,round((hi-lo)/.49))
        for i in range(1,n):fbox('Fine plinth joint',s,lo+(hi-lo)*i/n,.28,1.928,.008,.34,.004,brick,.001)
    for u in [-1.615,1.615]:
        for row in range(math.ceil((GEAR_FLOOR-.64)/.248)):
            z=.64+row*.248;width=.37 if row%2==0 else .285
            fbox('Dressed corner quoin',s,u,z,1.824,width,.242,.080,stone,.008)
    # Only two full perimeter belts: each aligns with an actual walkable deck.
    cornice_band(s,MIDDLE_FLOOR)
    cornice_band(s,GEAR_FLOOR)
    # Human-scale entry hood belongs to the door alone, not a fourth storey.
    if s==0:
        for z,w,h,d in [(2.98,1.77,.09,1.80),(3.065,1.83,.08,1.81),(3.12,1.72,.045,1.80)]:
            fbox('Entrance local stone hood',s,0,z,d,w,h,.10,stone,.008)
    # Closed ventilation panels serve the continuous stair shaft; no glass,
    # invented room, or additional floor is implied by these masonry details.
    ventilation(s,5.10,.72,1.30)
    ventilation(s,11.15,.72,1.30)
    # Recessed stone-framed panels give the tall piers a vertical rhythm.
    for base,top in [(3.78,7.83),(9.19,14.32)]:
        for u in [-.79,.79]:fbox('Ventilation panel fine vertical bead',s,u,(base+top)/2,1.823,.032,top-base,.030,stone,.003)
        for z in [base,top]:fbox('Ventilation panel fine horizontal bead',s,0,z,1.823,1.61,.032,.030,stone,.003)
        rosette(s,0,top-.32,1.858,.105)
    # Enlarged four-face clock chamber at the third level.
    for u in [-1.59,1.59]:
        for row in range(math.ceil((WALL_TOP-GEAR_FLOOR)/.266)):
            fbox('Clock chamber corner ashlar',s,u,GEAR_FLOOR+.14+row*.266,1.83,.40,.258,.092,stone,.010)
    panel_h=2.89
    for u in [-1.415,1.415]:fbox('Clock panel raised edge',s,u,CLOCK_Z,1.827,.044,panel_h,.048,stone,.005)
    for z in [CLOCK_Z-panel_h/2,CLOCK_Z+panel_h/2]:fbox('Clock panel raised edge',s,0,z,1.827,2.87,.040,.048,stone,.005)
    ring('Clock carved limestone surround',s,0,CLOCK_Z,[(r+.30,d) for r,d in [(.805,1.818),(.805,1.909),(.825,1.935),(.866,1.940),(.888,1.916),(.909,1.925),(.931,1.903),(.96,1.899),(.981,1.865),(.98,1.818)]],stone,112)
    ring('Clock slender bronze bead',s,0,CLOCK_Z,[(r+.30,d) for r,d in [(.814,1.925),(.818,1.944),(.830,1.944),(.834,1.925)]],bronze,112)
    for u in [-1.16,1.16]:
        for z in [CLOCK_Z-1.16,CLOCK_Z+1.16]:rosette(s,u,z,1.857,.10)
    for sign in [-1,1]:
        for zsgn in [-1,1]:
            leaf('Clock spandrel foliage',s,sign*1.22,CLOCK_Z+zsgn*1.01,1.856,.18,.082,sign*zsgn*.55)
            leaf('Clock spandrel foliage',s,sign*1.01,CLOCK_Z+zsgn*1.22,1.856,.18,.082,sign*zsgn*1.01)
    for row in range(9):
        u=-1.49+row*.373;fbox('Eave modillion',s,u,WALL_TOP-.08,1.855,.15,.19,.22,stone,.014)
        fbox('Modillion capital',s,u,WALL_TOP,1.872,.20,.055,.23,stone,.005)
    for u in [-1.62,1.62]:
        fbox('Acanthus capital block',s,u,WALL_TOP-.31,1.873,.38,.43,.16,stone,.018)
        for dx,angle in [(-.10,-.25),(0,0),(.10,.25)]:leaf('Acanthus capital leaf',s,u+dx,WALL_TOP-.29,1.962,.32,.15,angle)
        for sign in [-1,1]:
            pts=[]
            for j in range(26):
                a=j/25*math.pi*2;r=.066*(1-j/32);pts.append(pnt(s,u+sign*.115+sign*r*math.cos(a),WALL_TOP-.23+r*math.sin(a),1.980))
            path('Carved volute',pts,.013,stone)
    for dz,h,w,d,depth in [(.005,.10,3.89,1.82,.17),(.09,.065,4.02,1.86,.21),(.149,.05,4.10,1.90,.21)]:
        fbox('Crowning moulding',s,0,WALL_TOP+dz,d,w,h,depth,stone,.009)

# Door opening: square live leaf below an arched stationary tympanum.
for u in [-.642,.642]:
    for j in range(9):fbox('Entrance dressed jamb',0,u,.245+j*.235,1.851,.176,.228,.155,stone,.008)
    fbox('Entrance jamb base',0,u,.20,1.862,.213,.16,.20,stone,.008)
arch('Entrance shallow arch',0,0,2.276,.55,.30,.19,1.87,.17,stone,segments=28)
for j in range(9):arch('Entrance arch voussoir',0,0,2.276,.551,.301,.19,1.886,.17,stone,j*math.pi/9+.006,(j+1)*math.pi/9-.006,4)
# Wooden transom lies entirely above the clear 2.27 m door head.
verts=[pnt(0,-.55,2.271,1.836),pnt(0,.55,2.271,1.836)]
for j in range(1,20):
    a=j*math.pi/20;verts.append(pnt(0,.55*math.cos(a),2.276+.30*math.sin(a),1.836))
mesh('Stationary oak tympanum above door',verts,[tuple(range(len(verts)))],oak)
fbox('Entrance arch keystone',0,0,2.73,1.943,.18,.29,.17,stone,.012)
leaf('Entrance keystone leaf',0,0,2.75,2.037,.22,.12)

# Real four-sided pitched slate roof. Each slate is a solid overlapping plate.
roof_eave=WALL_TOP+.195;roof_peak=HEIGHT-.32;roof_half=2.065;rows=12
for s in range(4):
    mesh('Watertight roof underlay', [pnt(s,-roof_half,roof_eave,roof_half),pnt(s,roof_half,roof_eave,roof_half),(0,0,roof_peak)],[(0,1,2)],lead)
    for row in range(rows):
        t0=row/rows;t1=min(1,(row+1.08)/rows);d0=roof_half*(1-t0);d1=roof_half*(1-t1)
        z0=roof_eave+(roof_peak-roof_eave)*t0+.010;z1=roof_eave+(roof_peak-roof_eave)*t1+.010
        count=max(1,round(d0*2/.29))
        for k in range(count):
            a=-1+2*k/count;b=-1+2*(k+1)/count;gap=.008
            lo=a*d0+gap;hi=b*d0-gap;li=a*d1+gap*.45;ri=b*d1-gap*.45
            if d1<.001:li=ri=0
            vv=[pnt(s,lo,z0,d0+.013),pnt(s,hi,z0,d0+.013),pnt(s,ri,z1,d1),pnt(s,li,z1,d1)]
            vv+= [(x,y,z-.022) for x,y,z in vv]
            o=mesh('Individual overlapping slate',vv,[(0,1,2,3),(7,6,5,4),(0,4,5,1),(1,5,6,2),(2,6,7,3),(3,7,4,0)],slate,.002)
            # Different sampling of a single tile material avoids repeated stripes.
            layer=o.data.uv_layers.active;offset=random.random()*7
            for v in layer.data:v.uv.x+=offset;v.uv.y+=offset*.43
    path('Rolled lead eave gutter',[pnt(s,-roof_half-.025,roof_eave-.003,roof_half+.018),pnt(s,roof_half+.025,roof_eave-.003,roof_half+.018)],.024,lead)
    for sign in [-1,1]:
        a=Vector(pnt(s,sign*roof_half,roof_eave+.032,roof_half));b=Vector((0,0,roof_peak+.023))
        for j in range(9):
            st=a.lerp(b,j/9);en=a.lerp(b,min(1,(j+1.04)/9));beam('Lapped lead hip cap',st,en,.087,.045,lead)

def lathe(name,profile,mat,n=32):
    vs=[(r*math.cos(j*math.pi*2/n),r*math.sin(j*math.pi*2/n),z) for z,r in profile for j in range(n)]
    fs=[]
    for i in range(len(profile)-1):
        for j in range(n):fs.append((i*n+j,i*n+(j+1)%n,(i+1)*n+(j+1)%n,(i+1)*n+j))
    fs+=[tuple(reversed(range(n))),tuple(range((len(profile)-1)*n,len(profile)*n))]
    return mesh(name,vs,fs,mat)
lathe('Roof finial and ball',[(HEIGHT+z-9.50,r) for z,r in [(9.16,.13),(9.185,.14),(9.205,.10),(9.22,.07),(9.24,.038),(9.255,.046),(9.278,.075),(9.32,.093),(9.362,.075),(9.385,.040),(9.395,.025),(9.50,.001)]],lead,40)

# Freeze appearance modifiers and combine by material: only seven exported renderers.
for name,objects in parts.items():
    for o in objects:
        active(o)
        for mod in list(o.modifiers):bpy.ops.object.modifier_apply(modifier=mod.name)
    bpy.ops.object.select_all(action='DESELECT')
    for o in objects:o.select_set(True)
    bpy.context.view_layer.objects.active=objects[0];bpy.ops.object.join();o=bpy.context.object;o.name=name.replace('Tower_','Exterior_')
    bm=bmesh.new();bm.from_mesh(o.data);bmesh.ops.remove_doubles(bm,verts=list(bm.verts),dist=.000001)
    bad=[f for f in bm.faces if f.calc_area()<1e-11]
    if bad:bmesh.ops.delete(bm,geom=bad,context='FACES_ONLY')
    bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(o.data);bm.free();o.data.update()
    scene.cursor.location=(0,0,0);bpy.ops.object.origin_set(type='ORIGIN_CURSOR')
    # A tilted louvre must not leave all merged oak with a tilted bounding box.
    bpy.ops.object.transform_apply(location=False,rotation=True,scale=True)
    parts[name]=[o]

root=bpy.data.objects.new('ClockTower1931',None);asset.objects.link(root)
for o in list(asset.objects):
    if o!=root:o.parent=root
for name,pos in [('FrontMarker',(0,-1.8,.12)),('EastMarker',(1.8,0,.12)),('DoorHeadMarker',(0,-1.8,2.27)),('MiddleFloorMarker',(0,0,MIDDLE_FLOOR)),('GearFloorMarker',(0,0,GEAR_FLOOR)),('ClockCentreFront',(0,-1.86,CLOCK_Z)),('RoofPeakMarker',(0,0,HEIGHT))]:
    o=bpy.data.objects.new(name,None);asset.objects.link(o);o.parent=root;o.location=pos;o.empty_display_size=.1

bpy.ops.object.select_all(action='DESELECT')
for o in asset.objects:o.select_set(True)
bpy.context.view_layer.objects.active=root
bpy.ops.export_scene.fbx(filepath=str(OUT/'ClockTower1931.fbx'),use_selection=True,object_types={'MESH','EMPTY'},apply_unit_scale=True,apply_scale_options='FBX_SCALE_UNITS',axis_forward='-Z',axis_up='Y',bake_anim=False,path_mode='STRIP',embed_textures=False,use_mesh_modifiers=True)
shutil.copy2(OUT/'ClockTower1931.fbx',SRC/'ClockTower1931.fbx')
visual=[o for o in asset.objects if o.type=='MESH'];bounds=[o.matrix_world@Vector(v) for o in visual for v in o.bound_box]
tris=0
for o in visual:o.data.calc_loop_triangles();tris+=len(o.data.loop_triangles)
manifest={'asset':'ClockTower1931','construction':'Reference-guided authored editable 3D reconstruction; no external Meshy upload or neural inference.','reference':'../reference-approved-v3.png','blender_version':bpy.app.version_string,'units':'metres','blender_axes':{'right':'+X','up':'+Z','door_front':'-Y'},'fbx_export':{'axis_forward':'-Z','axis_up':'Y','apply_unit_scale':True,'scale':1},'markers_blender':{o.name:list(o.location) for o in asset.objects if o.type=='EMPTY' and o!=root},'bounds_blender_min':[min(v[i] for v in bounds) for i in range(3)],'bounds_blender_max':[max(v[i] for v in bounds) for i in range(3)],'footprint_physical_m':[3.6,3.6],'interior_clear_width_m':3.05,'height_m':HEIGHT,'clear_door':{'width':1.10,'bottom':.12,'top':2.27,'face':-1.8},'clock_centres_height':CLOCK_Z,'clock_unity_plane_distance':1.86,'ring_clear_diameter':RING_INNER*2,'dial_diameter':DIAL_DIAMETER,'gear_floor_height':GEAR_FLOOR,'middle_floor_height':MIDDLE_FLOOR,'inner_wall_top':WALL_TOP,'levels':3,'shared_layout_source':'game/Assets/Scripts/ClockTowerInteriorLayout.cs','renderers':len(visual),'triangles':tris,'material_names':list(materials),'textures':tex_manifest,'ventilation':'Eight solid-backed closed arched louvre panels, one per face on each lower shaft level; not transparent windows or extra playable rooms.','unity_supplies':['shared interior shell and all collision','animated wooden door below clear head','four deterministic clock dials and dynamic hands','wall plaque and readable history text'],'review_only':'Preview dials, hands, plaque and door illustrate the Unity overlays and are excluded from FBX.','authored_interpretations':['back and hidden side continue matching four-face clock architecture','user-requested 20m three-level revision with unchanged 3.6m width and human-scale door','three facade levels follow ground, intermediate deck and machinery deck; physical brick UV scale unchanged','vent panels closed against shared interior envelope']}
(SRC/'manifest.json').write_text(json.dumps(manifest,indent=2));(OUT/'manifest.json').write_text(json.dumps(manifest,indent=2))

# Review-only overlays use existing deterministic game textures, never generated text.
def review_mesh(o):
    for a in list(o.users_collection):a.objects.unlink(o)
    review.objects.link(o);return o
def image_material(name,path):
    m=bpy.data.materials.new(name);m.use_nodes=True;p=m.node_tree.nodes.get('Principled BSDF');p.inputs['Roughness'].default_value=.73
    nd=m.node_tree.nodes.new('ShaderNodeTexImage');nd.image=bpy.data.images.load(str(path),check_existing=True);m.node_tree.links.new(nd.outputs['Color'],p.inputs['Base Color']);return m
dialmat=image_material('REVIEW_GameDial',REPO/'game/Assets/Art/Textures/TowerDial.png')
historymat=image_material('REVIEW_GameHistory',REPO/'game/Assets/Art/Textures/TowerHistory.png')
def disc(s):
    vs=[pnt(s,0,CLOCK_Z,1.86)]+[pnt(s,DIAL_DIAMETER/2*math.cos(j*2*math.pi/112),CLOCK_Z+DIAL_DIAMETER/2*math.sin(j*2*math.pi/112),1.86) for j in range(112)]
    o=mesh('REVIEW clock face supplied by Unity',vs,[(0,j+1,(j+1)%112+1) for j in range(112)],dialmat);review_mesh(o)
    layer=o.data.uv_layers.active
    for po in o.data.polygons:
        for li in po.loop_indices:
            i=o.data.loops[li].vertex_index
            layer.data[li].uv=(.5,.5) if i==0 else (.5+.5*math.cos((i-1)*2*math.pi/112),.5+.5*math.sin((i-1)*2*math.pi/112))
    for length,angle,w,d in [(.86,60,.022,1.90),(.58,-65,.032,1.92)]:
        t=math.radians(angle);a=pnt(s,0,CLOCK_Z,d);b=pnt(s,length*math.sin(t),CLOCK_Z+length*math.cos(t),d);review_mesh(beam('REVIEW dynamic clock hand supplied by Unity',a,b,w,.01,lead))
for s in range(4):disc(s)
review_mesh(fbox('REVIEW Unity door',0,0,1.17,1.798,1.04,2.10,.09,oak,.010))
for u in [-.36,-.18,0,.18,.36]:review_mesh(fbox('REVIEW door seam',0,u,1.17,1.846,.009,1.99,.004,recess,.001))
for z in [.47,1.89]:review_mesh(fbox('REVIEW door strap',0,-.06,z,1.866,.76,.045,.025,lead,.007))
review_mesh(fbox('REVIEW Unity wall plaque',0,1.06,1.52,1.849,.67,.72,.026,bronze,.004))
o=mesh('REVIEW plaque text supplied by Unity',[pnt(0,.745,1.18,1.865),pnt(0,1.375,1.18,1.865),pnt(0,1.375,1.86,1.865),pnt(0,.745,1.86,1.865)],[(0,1,2,3)],historymat);review_mesh(o)
layer=o.data.uv_layers.active
for li,co in zip(o.data.polygons[0].loop_indices,[(0,0),(1,0),(1,1),(0,1)]):layer.data[li].uv=co
groundmat=material('REVIEW_Ground',(.25,.28,.29),.91)
review_mesh(box('Review ground',(0,0,-.11),(200,200,.2),groundmat,0))
def light(name,loc,energy,color,size,target=(0,0,10)):
    d=bpy.data.lights.new(name,'AREA');d.energy=energy;d.color=color;d.shape='DISK';d.size=size
    o=bpy.data.objects.new(name,d);review.objects.link(o);o.location=loc;o.rotation_euler=(Vector(target)-o.location).to_track_quat('-Z','Y').to_euler()
light('Large warm sky',(-12,-16,23),4500,(1,.85,.68),14)
light('Cool fill',(10,-6,21),3200,(.69,.81,1),12)
light('Roof rim',(5,9,23),3800,(1,.9,.76),10)
scene.world=bpy.data.worlds.new('Review world');scene.world.use_nodes=True;scene.world.node_tree.nodes['Background'].inputs[0].default_value=(.49,.55,.63,1);scene.world.node_tree.nodes['Background'].inputs[1].default_value=.45
cd=bpy.data.cameras.new('ReviewCamera');cam=bpy.data.objects.new('ReviewCamera',cd);review.objects.link(cam);scene.camera=cam
scene.render.engine='CYCLES';scene.cycles.samples=32;scene.cycles.use_denoising=True
scene.render.resolution_x=1100;scene.render.resolution_y=1400;scene.render.resolution_percentage=100;scene.render.image_settings.file_format='PNG';scene.view_settings.view_transform='AgX'
def camera(pos,target,lens):cam.location=pos;cam.rotation_euler=(Vector(target)-cam.location).to_track_quat('-Z','Y').to_euler();cd.lens=lens
camera((18,-29,18),(0,0,10),48)
bpy.ops.wm.save_as_mainfile(filepath=str(SRC/'ClockTower1931.blend'))
for name,pos,target,lens in [('model-front',(18,-29,18),(0,0,10),48),('model-back',(-18,29,18),(0,0,10),48),('model-detail',(5.3,-7.4,17.7),(0,0,16.8),47),('model-entrance',(3.5,-5.8,2.1),(0,-1.4,1.62),45)]:
    camera(pos,target,lens);scene.render.filepath=str(PRE/(name+'.png'));bpy.ops.render.render(write_still=True)
print('CLOCKTOWER_EXTERIOR_EXPORT_PASS',json.dumps(manifest))
