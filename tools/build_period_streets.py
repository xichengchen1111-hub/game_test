"""Original 1930 US street dressing, derived from the unchanged metre map.
Run with numpy/Pillow/shapely>=2.1. No downloaded artwork; no map or evidence edits.
"""
from pathlib import Path
import sys,json,hashlib,math
import numpy as np
from PIL import Image,ImageDraw,ImageFont,ImageFilter
sys.path.append(str(Path(__file__).resolve().parents[1]/'builds/street-tools'))
from shapely.geometry import Polygon,LineString,Point,box
from shapely.ops import unary_union
from shapely import constrained_delaunay_triangles
from make_street_textures import fbm_seamless,compute_tangent_normal,create_mask_map
R=Path(__file__).resolve().parents[1]; A=R/'game/Assets/Art/Street'; N=2048
rng=np.random.default_rng(193001)
def noise(freq,oct=3):return fbm_seamless(N,freq,freq,oct,.5,rng)
def save(name,col,height,smooth):
 Image.fromarray(np.uint8(np.clip(col,0,255))).save(A/(name+'_Base.png'))
 Image.fromarray(compute_tangent_normal(height.astype('float32'),8)).save(A/(name+'_Normal.png'))
 Image.fromarray(create_mask_map(smooth)).save(A/(name+'_Mask.png'))
# 4 m tiling, fine aggregate with broad restrained variation, no oversized crater noise.
fine=noise(180,2);broad=noise(4);mid=noise(32);value=80+fine*29+mid*12+broad*17
h=.5+fine*.25+mid*.06
save('PeriodAsphalt',np.stack([value*1.035,value*1.025,value],-1),h,np.clip(.33+mid*.18,.22,.45))
y,x=np.mgrid[:N,:N]/N*4
# 1m concrete squares: tooled joints and gently mottled trowelled panels.
d=np.minimum(np.mod(x,1),1-np.mod(x,1));d=np.minimum(d,np.minimum(np.mod(y,1),1-np.mod(y,1)))
joint=np.clip((.009-d)/.009,0,1);edge=np.clip((.026-d)/.026,0,1)
vari=rng.uniform(-4,4,(4,4))[np.minimum(y.astype(int),3),np.minimum(x.astype(int),3)]
v=171+noise(5)*15+noise(140)*10+vari-joint*44-edge*9
save('PeriodConcrete',np.stack([v*1.04,v*1.025,v*.975],-1),.55-joint*.19-edge*.025+fine*.04,np.clip(.30+edge*.12,0,1))
# Warm worn paint; an alpha-clipped mask breaks edges and tiny chips without extra geometry.
v=207+noise(6)*18+fine*12
alpha=np.where(noise(55)+fine*.4>-.13,255,0).astype('uint8')
Image.fromarray(np.dstack([v*1.04,v*1.025,v*.94,alpha]).clip(0,255).astype('uint8')).save(A/'PeriodPaint.png')
# An accurately worded 24-inch octagonal urban sign. Letterforms are an approximation.
fontpath=R/'game/Assets/Art/Fonts/TownSans.ttf'
font=ImageFont.truetype(str(fontpath),220);small=ImageFont.truetype(str(fontpath),85)
im=Image.new('RGB',(1024,1024),(183,144,58));dr=ImageDraw.Draw(im)
octagon=[(512+480*math.cos(math.pi/8+k*math.pi/4)/math.cos(math.pi/8),512+480*math.sin(math.pi/8+k*math.pi/4)/math.cos(math.pi/8)) for k in range(8)]
# Vertex extent = 480 with flat top/side bounds.
dr.polygon(octagon,fill=(181,56,37));inner=[(512+(a-512)*.959,512+(b-512)*.959) for a,b in octagon];dr.polygon(inner,fill=(207,172,78))
for text,yy,f in [('THROUGH',300,small),('STOP',507,font),('TRAFFIC',706,small)]:dr.text((512,yy),text,font=f,anchor='mm',fill=(136,38,27),stroke_width=1)
arr=np.asarray(im).astype('float32');weather=np.random.default_rng(4).normal(0,1.2,(1024,1024,1));Image.fromarray(np.uint8(np.clip(arr+weather,0,255))).save(A/'PeriodStop.png')
# Dressing surfaces are shallow non-colliding veneers over known road heights.
p=json.loads((R/'game/Assets/Resources/TownPlan.json').read_text());source=R/'docs/maps/town-current/plan-data.json'
def shape(surface):
 v=surface['vertices'];return [Polygon([(v[i+j],v[i+j+2]) for j in (0,3,6)]) for i in range(0,len(v),9)]
road=unary_union([t for s in p['surfaces'] if s['material']=='road' for t in shape(s)])
kerb=unary_union([t for s in p['surfaces'] if s['material']=='kerb' for t in shape(s)])
meshes=[]
def add(name,kind,g,y=.028):
 vertices=[]
 for tri in constrained_delaunay_triangles(g).geoms:
  if tri.area<1e-7 or not g.covers(tri):continue
  a=list(tri.exterior.coords)[:3]
  if (a[1][0]-a[0][0])*(a[2][1]-a[0][1])-(a[1][1]-a[0][1])*(a[2][0]-a[0][0])>0:a.reverse()
  for xx,zz in a:vertices += [round(xx,5),y,round(zz,5)]
 if vertices:meshes.append(dict(name=name,material=kind,vertices=vertices))
# Inner edge gutter follows the exact union boundary, including intersections. No topology change.
gutter=road.difference(road.buffer(-.26,join_style=2))
for cx in range(-4,5):
 for cz in range(-3,3):add(f'gutter-{cx}-{cz}','gutter',gutter.intersection(box(cx*48,cz*48,(cx+1)*48,(cz+1)*48)))
# Kerb risers supply thickness missing from the existing flat top meshes.
risers=[]
for poly in getattr(kerb,'geoms',[kerb]):
 for ring in [poly.exterior,*poly.interiors]:
  coords=list(ring.coords)
  for a,b in zip(coords,coords[1:]):
   if LineString([a,b]).distance(road.boundary)>.002:continue
   risers.append([a[0],a[1],b[0],b[1]])
# A single signalled junction; restrained parallel-boundary crossings elsewhere.
crossings=[];stops=[]
for jx in [-120,30,135]:
 for jz in [20,-75,-122]:
  half=3 if jz==20 else 2.5
  if jz==20:
   for sign in [-1,1]:
    # Walk E/W across N/S road, aligned with the existing sidewalk continuations.
    zz=jz+sign*(half+1.2)
    for edge in [-1,1]:add(f'crossing-{jx}-{jz}-ew-{sign}-{edge}','paint',box(jx-2,zz+edge*.95-.06,jx+2,zz+edge*.95+.06).intersection(road),.030)
    xx=jx+sign*3.2
    for edge in [-1,1]:add(f'crossing-{jx}-{jz}-ns-{sign}-{edge}','paint',box(xx+edge*.95-.06,jz-half,xx+edge*.95+.06,jz+half).intersection(road),.030)
   crossings.append([jx,jz])
  if (jx,jz)!=(30,20):
   for sign in [-1,1]:
    # Right-hand traffic: northbound east verge; southbound west verge.
    z=jz-sign*(6.7 if jz in [20,-75] else 5.7);x=jx+sign*2.5
    stops.append(dict(x=x,z=z,yaw=0 if sign==1 else 180))
    add(f'stopline-{jx}-{jz}-{sign}','paint',box(jx if sign==1 else jx-2,z-.12,jx+2 if sign==1 else jx,z+.12),.030)
# Main street short white approach centre lines, interrupted well before crossings.
for jx in [-120,30,135]:
 for sign in [-1,1]:
  a,b=sorted([jx+sign*7,jx+sign*23]);add(f'approach-{jx}-{sign}','paint',box(a,19.94,b,20.06),.030)
# Four stop bars at the signal (no STOP signs at this junction).
for sign in [-1,1]:
 z=20+sign*6.0;add(f'signalbar-ns-{sign}','paint',box(28 if sign==1 else 30,z-.12,30 if sign==1 else 32,z+.12),.030)
 x=30+sign*5.0;add(f'signalbar-ew-{sign}','paint',box(x-.12,20 if sign==1 else 17,x+.12,23 if sign==1 else 20),.030)
# Flat iron covers set into the gutter zone, outside travel wheel paths.
drains=[]
for r in p['roads']:
 if r['lane']<4:continue
 ax,az,bx,bz=r['points'];length=math.hypot(bx-ax,bz-az);dx=(bx-ax)/length;dz=(bz-az)/length
 for distance in np.arange(15,length-8,32):
  for side in [-1,1]:
   xx=ax+dx*distance-dz*side*(r['lane']/2-.16);zz=az+dz*distance+dx*side*(r['lane']/2-.16)
   if road.boundary.distance(Point(xx,zz))>.22:continue
   drains.append(dict(x=round(xx,3),z=round(zz,3),yaw=90 if abs(dx)>.5 else 0))
# Ensure sign stems stay in the furniture band, clear of original tree/lamp trunks.
for s in stops:
 for shift in [0,.35,.7,1.05]:
  zz=s['z']+shift*(-1 if s['yaw']==0 else 1)
  if all(math.hypot(s['x']-t['x'],zz-t['z'])>.5 for t in p['trees']+p['lamps']):s['z']=zz;break
 else:raise RuntimeError('No clear stop sign location')
data=dict(sourceSha256=p['sourceSha256'],surfaces=meshes,risers=[dict(a=q) for q in risers],stops=stops,drains=drains,crossings=crossings)
(R/'game/Assets/Resources/PeriodStreets.json').write_text(json.dumps(data,separators=(',',':')))
report=dict(mapSource=p['sourceSha256'],mapUnchanged=p['sourceSha256'] in [hashlib.sha256(source.read_bytes()).hexdigest(),hashlib.sha256(source.read_bytes().replace(b'\r\n',b'\n').replace(b'\n',b'\r\n')).hexdigest()],stopSigns=len(stops),signalJunctions=1,crossingJunctions=len(crossings),drains=len(drains),kerbRisers=len(risers),surfaceMeshes=len(meshes),triangles=sum(len(s['vertices'])//9 for s in meshes),design='1930 urban yellow/red THROUGH STOP TRAFFIC; paired white crosswalk boundaries; no YIELD, zebra stripes, modern yellow centre lines or invented street names.')
assert report['mapUnchanged']
(R/'output/demo/streets-1930/authoring.json').write_text(json.dumps(report,indent=2));print(json.dumps(report))
