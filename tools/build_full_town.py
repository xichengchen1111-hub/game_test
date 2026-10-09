"""Compile authoritative map into inexpensive Unity meshes/markers. pip install shapely pillow.
Source map is never edited here. World X/Z and metres are preserved, no parcel-to-house scaling.
"""
from pathlib import Path
import json, math, hashlib, random
from shapely.geometry import box, LineString, Point
from shapely.ops import unary_union, triangulate
from PIL import Image, ImageDraw

ROOT=Path(__file__).resolve().parents[1]
SOURCE=ROOT/'docs/maps/town-current/plan-data.json'
ASSETS=ROOT/'game/Assets/Art/TownPlaceholders';ASSETS.mkdir(parents=True,exist_ok=True)
RES=ROOT/'game/Assets/Resources';RES.mkdir(exist_ok=True)
p=json.loads(SOURCE.read_text(encoding='utf-8'))
def rect(r):
 x,z,w,d=r;return box(x-w/2,z-d/2,x+w/2,z+d/2)
def corridor(points,width):return LineString(points).buffer(width/2,cap_style=2,join_style=2)

# Preserve the finished dairy at its native .9 scale; remaining B04 footprint is a yard placeholder.
milk=[-144.094,7.21,6.48,5.22]
buildings=[];holes=[]
for b in p['buildings']:
 buildings.append(dict(id=b['id'],name=b['name'],kind=b['kind'],front=b['front'],storeys=b['storeys'],
  door=b['door'],rearDoor=b['rearDoor'],gate=b['gate'],plot=b['plot'],
  doorFacing=b.get('doorFacing',b['front']),rearFacing=b.get('rearFacing',{'N':'S','S':'N','E':'W','W':'E'}[b['front']]),
  parts=[dict(role=q['role'],rect=q['rect']) for q in b['parts']],
  windows=[dict(x=w[0],z=w[1],width=w[2],side=w[4] if len(w)>4 else b['front']) for w in b['windows']]))
 if b['id']=='B04':holes.append(rect(milk))
 else:holes.extend(rect(q['rect']) for q in b['parts'])
holes=unary_union(holes)
road=unary_union([corridor(r['points'],r['lane']) for r in p['roads'] if r['lane']])
public=unary_union([corridor(r['points'],r['width']) for r in p['roads']])
paths=unary_union([corridor(q['points'],q['width']) for q in p['paths']])
plaza=box(-13,-13.5,13,14)
yards=unary_union([rect(b['plot']) for b in p['buildings'] if b['id'].startswith('B')])
# A single partition: intersections, alleys, paths and plaza never share coplanar faces.
walk=unary_union([public,paths,plaza]).difference(road).difference(holes)
kerb=road.buffer(.18,join_style=2).difference(road).intersection(public).difference(paths).difference(plaza).difference(holes)
walk=walk.difference(kerb)
yard=yards.difference(unary_union([public,paths,plaza,holes]))
surfaces=[]
def pieces(geometry,material,y):
 if geometry.is_empty:return
 x0,z0,x1,z1=geometry.bounds
 for cx in range(math.floor(x0/48),math.floor(x1/48)+1):
  for cz in range(math.floor(z0/48),math.floor(z1/48)+1):
   part=geometry.intersection(box(cx*48,cz*48,(cx+1)*48,(cz+1)*48))
   verts=[]
   for t in triangulate(part):
    if t.area<1e-7 or not part.covers(t):continue
    a=list(t.exterior.coords)[:3]
    # Shapely CCW in X/Z gives a downward normal; reverse to face sky.
    for x,z in reversed(a):verts.extend([round(x,5),y,round(z,5)])
   if verts:surfaces.append(dict(name=f'{material}-{cx}-{cz}',material=material,vertices=verts))
pieces(road.difference(holes),'road',.025)
pieces(walk,'walk',.10)
pieces(kerb,'kerb',.10)
pieces(yard,'yard',-.025)

# Candidate lanterns occupy the facilities band, never the 2m clear footway or a junction.
lamps=[]
for r in p['roads']:
 if not r['lane'] or r['strip']<=0:continue
 a,b=r['points'];dx,dz=b[0]-a[0],b[1]-a[1];length=math.hypot(dx,dz);ux,uz=dx/length,dz/length
 for t in range(12,int(length)-6,24):
  for side in [-1,1]:
   off=r['lane']/2+r['strip']*.55
   x,z=a[0]+ux*t-uz*off*side,a[1]+uz*t+ux*off*side
   if any(corridor(other['points'],other['width']+.8).contains(Point(x,z)) for other in p['roads'] if other['id']!=r['id']):continue
   if paths.distance(Point(x,z))<.6:continue
   if any(math.hypot(t['x']-x,t['z']-z)<.8 for t in p['trees']):continue
   lamps.append(dict(x=round(x,3),z=round(z,3),road=r['id']))

data=dict(version=p['version'],sourceSha256=hashlib.sha256(SOURCE.read_bytes()).hexdigest(),bounds=p['bounds'],buildings=buildings,
 roads=[dict(id=r['id'],name=r['name'],points=sum(r['points'],[]),lane=r['lane'],walk=r['walk'],strip=r['strip'],width=r['width']) for r in p['roads']],
 trees=p['trees'],land=p['land'],details=p['details'],facilities=p['facilities'],surfaces=surfaces,lamps=lamps,milkRect=milk)
(RES/'TownPlan.json').write_text(json.dumps(data,ensure_ascii=False,separators=(',',':')),encoding='utf-8')

# Original placeholder graphics, explicitly opaque. They do not pretend to show an interior.
im=Image.new('RGB',(512,512),(151,139,122));d=ImageDraw.Draw(im)
for y in range(0,512,64):
 for x in range(-64,512,128):d.rectangle((x+(y//64%2)*64,y,x+126+(y//64%2)*64,y+62),outline=(127,118,107),width=2)
for x in [75,303]:
 d.rectangle((x,88,x+134,340),fill=(65,71,69));d.rectangle((x+10,98,x+124,324),fill=(92,104,106))
 d.line((x+67,98,x+67,324),fill=(44,52,49),width=7);d.line((x+10,200,x+124,200),fill=(44,52,49),width=7)
 d.rectangle((x-10,338,x+144,351),fill=(183,170,144))
im.save(ASSETS/'Facade.png')
im=Image.new('RGBA',(256,384),(0,0,0,0));d=ImageDraw.Draw(im);rng=random.Random(1931)
d.rectangle((121,181,136,381),fill=(82,66,48,255))
for k in range(110):
 x=rng.uniform(30,226);y=rng.uniform(22,212);radius=rng.uniform(19,36)
 if ((x-128)/110)**2+((y-116)/116)**2>1:continue
 shade=rng.choice([(88,105,73,255),(100,117,78,255),(117,125,87,255),(74,93,69,255)])
 d.ellipse((x-radius,y-radius,x+radius,y+radius),fill=shade)
im.save(ASSETS/'Tree.png')
out=ROOT/'output/demo/full-map';out.mkdir(parents=True,exist_ok=True)
report=dict(source=data['sourceSha256'],buildings=len(buildings),trees=len(p['trees']),roads=len(p['roads']),
 surfaceChunks=len(surfaces),surfaceTriangles=sum(len(s['vertices'])//9 for s in surfaces),lanterns=len(lamps),
 disjointSurfaceAreaError=road.intersection(walk).area+kerb.intersection(walk).area,
 milkPlacement='Existing house retained at scale .9 inside B04 production parcel; remaining production floor is placeholder yard',
 scope='Map compilation and planar surface partition. Native Unity collision and rendering require player acceptance.')
assert report['disjointSurfaceAreaError']<1e-6
(out/'map-compile.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps(report,ensure_ascii=False))
