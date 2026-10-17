"""Original 1930 US street dressing, derived from the unchanged metre map.
Stop bars and STOP signs stand on the stop lines the town's traffic uses (StreetNet's rule, worked out here).
Run with numpy/Pillow/shapely>=2.1. No downloaded artwork; no map or evidence edits.
"""
from pathlib import Path
import sys,json,hashlib,math,re
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
# Stop lines where the town's cars stop (StreetNet.SetBackLines, STREET-03): a metre behind the footway
# line people cross on, then set back until a car waiting at or behind its line is ClearPlan clear of every
# other approach's car body sweeping through the corner (on the 4 m north-south streets cars turning in
# swing over the middle). Same rule, with the car size and margins read from StreetNet/AmbientCar, so the
# paint stays where the cars stop; StreetNet still works its lines out from the map itself.
def cs(file,name):return float(re.search(r'\b'+name+r'=([\d.]+)f',(R/'game/Assets/Scripts'/file).read_text()).group(1))
FRONT,HL,HW,BACK,APART=[cs('AmbientCar.cs',n) for n in ['Front','HalfLength','HalfWidth','CentreBack','Apart']]
CLEAR,STEP,OUT=[cs('StreetNet.cs',n) for n in ['ClearPlan','SweepStep','SweepOut']]
def body_gap(a,fa,b,fb):
 # AmbientCar.BoxGap: clearance of two car bodies (box centre CentreBack behind the origin; unit headings).
 dx=b[0]-fb[0]*BACK-a[0]+fa[0]*BACK;dz=b[1]-fb[1]*BACK-a[1]+fa[1]*BACK
 same=abs(fa[0]*fb[0]+fa[1]*fb[1]);cross=abs(fa[0]*fb[1]-fa[1]*fb[0])
 return max(abs(dx*fa[0]+dz*fa[1])-HL-HL*same-HW*cross,abs(dx*fa[1]-dz*fa[0])-HW-HL*cross-HW*same,
  abs(dx*fb[0]+dz*fb[1])-HL-HL*same-HW*cross,abs(dx*fb[1]-dz*fb[0])-HW-HL*cross-HW*same)
class Lane:
 def point(l,s):s=min(max(s,0),l.length);return (l.start[0]+l.dir[0]*s,l.start[1]+l.dir[1]*s)
class Turn:
 def __init__(t,a,b,j,p1):
  t.a,t.b,t.j,t.p=a,b,j,(a.end,p1,b.start);t.table=[0];q=a.end
  for i in range(1,17):r=t.bezier(i/16);t.table.append(t.table[-1]+math.dist(q,r));q=r
  t.length=t.table[-1]
 def bezier(t,u):p0,p1,p2=t.p;return tuple((1-u)**2*p0[k]+2*(1-u)*u*p1[k]+u*u*p2[k] for k in (0,1))
 def param(t,s):
  s=min(max(s,0),t.length)
  for i in range(1,17):
   if t.table[i]>=s:return (i-1+(s-t.table[i-1])/max(1e-5,t.table[i]-t.table[i-1]))/16
  return 1
 def pose(t,s):
  u=t.param(s);p0,p1,p2=t.p;d=[2*(1-u)*(p1[k]-p0[k])+2*u*(p2[k]-p1[k]) for k in (0,1)]
  if d[0]**2+d[1]**2<=1e-6:d=[p2[k]-p0[k] for k in (0,1)]
  n=math.hypot(*d);return t.bezier(u),(d[0]/n,d[1]/n)
 def sweep(t):
  # The body from its line, round the corner and 2.5 m out along the street beyond (StreetNet.BuildSweeps).
  a,b=t.a,t.b;t.poses=[];s=max(0,a.length-FRONT)
  while s<a.length:t.poses.append((a.point(s),a.dir));s+=STEP
  n=math.ceil(t.length/STEP);t.poses+=[t.pose(t.length*i/n) for i in range(n+1)];s=STEP
  while s<=OUT:t.poses.append((b.point(s),b.dir));s+=STEP
 # Bodies whose origins are more than Apart apart are well clear (the same shortcut as StreetNet).
 def clearance(t,o,f):return min([body_gap(q,fq,o,f) for q,fq in t.poses if (q[0]-o[0])**2+(q[1]-o[1])**2<=APART*APART],default=math.inf)
def stop_lines():
 carriages=[]
 for r in p['roads']:
  a=r['points'];ew,ns=abs(a[1]-a[3])<.01,abs(a[0]-a[2])<.01
  if r['lane']<3.5 or not(ew or ns):continue
  k=(0,2) if ew else (1,3);carriages.append(dict(road=r,ew=ew,centre=a[1] if ew else a[0],lo=min(a[k[0]],a[k[1]]),hi=max(a[k[0]],a[k[1]]),half=r['lane']/2))
 junctions=[(n['centre'],e['centre'],e,n) for e in carriages if e['ew'] for n in carriages if not n['ew'] and e['lo']<=n['centre']<=e['hi'] and n['lo']<=e['centre']<=n['hi']]
 def offset(j,c):o=j[3] if c['ew'] else j[2];return o['half']+o['road']['strip']+o['road']['walk']/2+1.1
 lanes=[]
 for c in carriages:
  st=sorted([(c['lo'],None),(c['hi'],None)]+[(j[0] if c['ew'] else j[1],j) for j in junctions if c is j[2] or c is j[3]],key=lambda q:q[0])
  for (s0,j0),(s1,j1) in zip(st,st[1:]):
   m0=offset(j0,c) if j0 else 0;m1=offset(j1,c) if j1 else 0
   if s1-m1-(s0+m0)<4:continue
   for sign in [1,-1]:
    l=Lane();l.dir=(sign,0) if c['ew'] else (0,sign);right=(l.dir[1],-l.dir[0]);w=c['half']/2
    def P(s):return (s+right[0]*w,c['centre']+right[1]*w) if c['ew'] else (c['centre']+right[0]*w,s+right[1]*w)
    l.start,l.end=(P(s0+m0),P(s1-m1)) if sign>0 else (P(s1-m1),P(s0+m0));l.into=j1 if sign>0 else j0;l.out=j0 if sign>0 else j1
    l.length=math.dist(l.start,l.end);l.setback=0;lanes.append(l)
 def corners():
  turns=[]
  for j in junctions:
   for a in lanes:
    if a.into is not j:continue
    for b in lanes:
     if b.out is not j or a.dir[0]*b.dir[0]+a.dir[1]*b.dir[1]<-.9:continue
     mid=((a.end[0]+b.start[0])/2,(a.end[1]+b.start[1])/2);p1=mid
     if abs(a.dir[1]*b.dir[0]-a.dir[0]*b.dir[1])>=.1:
      k=(b.start[0]-a.end[0])*a.dir[0]+(b.start[1]-a.end[1])*a.dir[1];p1=(a.end[0]+a.dir[0]*k,a.end[1]+a.dir[1]*k)
      if (b.start[0]-p1[0])*b.dir[0]+(b.start[1]-p1[1])*b.dir[1]<0:p1=mid
     turns.append(Turn(a,b,j,p1))
  for t in turns:t.sweep()
  return turns
 def behind(l,t):
  # How far back from the line a car on this lane can still come within ClearPlan of the turn's sweep:
  # every quarter metre until a metre in a row is clear, every 5 cm round any within 0.15 m more (StreetNet.Behind).
  need=back=run=0
  while back<6 and run<1:
   s=l.length-FRONT-back
   if s<0:break
   caught=False
   if t.clearance(l.point(s),l.dir)<CLEAR+.15:
    fine=max(0,back-.2)
    while fine<=back+.2:
     f=l.length-FRONT-fine
     if f>=0 and t.clearance(l.point(f),l.dir)<CLEAR:need=max(need,fine+STEP);caught=True
     fine+=STEP
   run=0 if caught else run+.25;back+=.25
  return need
 turns=corners();changed=set(lanes)
 for _ in range(4):
  moved=set()
  for l in lanes:
   if l.into is None:continue
   need=min(max([behind(l,t) for t in turns if t.j is l.into and t.a is not l and t.a in changed],default=0),max(0,l.length-8))
   if need>0:l.end=l.point(l.length-need);l.length-=need;l.setback+=need;moved.add(l)
  if not moved:break
  changed=moved;turns=corners()
 return {(l.into[0],l.into[1])+l.dir:l for l in lanes if l.into}
lines=stop_lines()
def bar(jx,jz,d):
 # The bar's centre 0.1 m on the junction side of the line (as the signal's bars always were), so a car
 # stopped with its bumper just short of the line stands right behind the paint.
 l=lines[(jx,jz)+d];return round(l.end[0 if d[0] else 1]+(d[0] or d[1])*.1,3)
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
    # Right-hand traffic: northbound east verge; southbound west verge. The STOP sign stands at its bar.
    z=bar(jx,jz,(0,sign));x=jx+sign*2.5
    stops.append(dict(x=x,z=z,yaw=0 if sign==1 else 180))
    add(f'stopline-{jx}-{jz}-{sign}','paint',box(jx if sign==1 else jx-2,z-.12,jx+2 if sign==1 else jx,z+.12),.030)
# Main street short white approach centre lines, interrupted well before crossings.
for jx in [-120,30,135]:
 for sign in [-1,1]:
  a,b=sorted([jx+sign*7,jx+sign*23]);add(f'approach-{jx}-{sign}','paint',box(a,19.94,b,20.06),.030)
# Four stop bars at the signal (no STOP signs at this junction), on the lines its cars stop at.
for sign in [-1,1]:
 z=bar(30,20,(0,-sign));add(f'signalbar-ns-{sign}','paint',box(28 if sign==1 else 30,z-.12,30 if sign==1 else 32,z+.12),.030)
 x=bar(30,20,(-sign,0));add(f'signalbar-ew-{sign}','paint',box(x-.12,20 if sign==1 else 17,x+.12,23 if sign==1 else 20),.030)
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
setback=sorted(l.setback for l in lines.values() if l.setback>0)
report=dict(mapSource=p['sourceSha256'],mapUnchanged=p['sourceSha256'] in [hashlib.sha256(source.read_bytes()).hexdigest(),hashlib.sha256(source.read_bytes().replace(b'\r\n',b'\n').replace(b'\n',b'\r\n')).hexdigest()],stopSigns=len(stops),signalJunctions=1,crossingJunctions=len(crossings),drains=len(drains),kerbRisers=len(risers),surfaceMeshes=len(meshes),triangles=sum(len(s['vertices'])//9 for s in meshes),stopBars=f"{sum(m['name'].startswith(('stopline-','signalbar-')) for m in meshes)} bars on the StreetNet stop lines; {len(setback)} north-south lines set back {setback[0]:.2f}-{setback[-1]:.2f} m for turning car bodies",design='1930 urban yellow/red THROUGH STOP TRAFFIC; paired white crosswalk boundaries; no YIELD, zebra stripes, modern yellow centre lines or invented street names.')
assert report['mapUnchanged']
(R/'output/demo/streets-1930/authoring.json').write_text(json.dumps(report,indent=2));print(json.dumps(report))
