"""Compile deterministic planting from the unchanged metre map. Requires shapely/pillow."""
from pathlib import Path
import json,math,random,hashlib,collections
from shapely.geometry import box,Point,LineString
from shapely.ops import unary_union,triangulate
from shapely.prepared import prep
from PIL import Image
import numpy as np

R=Path(__file__).resolve().parents[1];S=R/'docs/maps/town-current/plan-data.json'
p=json.loads(S.read_text());town=json.loads((R/'game/Assets/Resources/TownPlan.json').read_text());rng=random.Random(19311011)
raw=S.read_bytes().replace(b'\r\n',b'\n')
assert town['sourceSha256'] in {hashlib.sha256(raw).hexdigest(),hashlib.sha256(raw.replace(b'\n',b'\r\n')).hexdigest()}, 'Compiled TownPlan does not match current map'
assert town['trees']==p['trees'], 'Compiled tree coordinates differ'
out=R/'assets/source/greenery';out.mkdir(parents=True,exist_ok=True)
def rect(r):x,z,w,d=r;return box(x-w/2,z-d/2,x+w/2,z+d/2)
def corridor(points,w):return LineString(points).buffer(w/2,cap_style=2,join_style=2)
buildings=unary_union([rect(q['rect']) for b in p['buildings'] for q in b['parts']])
road=unary_union([corridor(r['points'],r['width']+.6) for r in p['roads']])
paths=unary_union([corridor(q['points'],q['width']+.8) for q in p['paths']])
entrances=unary_union([Point(*b[k]).buffer(1.65) for b in p['buildings'] for k in ['door','rearDoor','gate'] if b.get(k)])
# The actual station roof/platform/rail yard extends beyond the historic plot.
protected=[box(143,30,231,95),rect(town['milkRect']).buffer(1.7),box(143,-26,184,18)]
protected += [rect(d['rect']).buffer(.3) for d in p['details'] if d['kind'] in ['service','shed']]
protected += [rect(d['rect']).buffer(.3) for d in p['land'] if d['kind'] in ['rail','court','workyard']]
protected += [Point(*f['point']).buffer(max(f['width'],f['depth'])/2+.8) for f in p['facilities']]
protected += [Point(x,z).buffer(1.7) for x,z in [(-10,8),(10,8),(-10,-8),(10,-8),(194,33),(205,33),(42,-108),(74,-107)]]
protected += [LineString([s['a'],s['b']]).buffer(1.1) for s in json.loads((R/'docs/maps/town-current/actor-audit.json').read_text())['sights']]
reserved=unary_union(protected+[buildings.buffer(.55),paths,entrances])
bounds=p['bounds'];extent=box(bounds[0]+2,bounds[2]+2,bounds[1]-2,bounds[3]-2)
lawns=extent.difference(unary_union([road,box(-13.5,-14,13.5,14.5),reserved]))
# Four low ornamental beds at plaza corners, clear of benches, lamps, and doors.
plazaBeds=unary_union([box(x-1.4,z-.5,x+1.4,z+.5) for x in [-7.4,7.4] for z in [-10.7,10.7]]).difference(reserved)
treeBeds=[]
for t in p['trees']:
 if t['kind']=='行道树':
  b=Point(t['x'],t['z']).buffer(.45,resolution=10)
  if not reserved.intersects(b):treeBeds.append(b)
streetBeds=unary_union(treeBeds)
ground=[]
def surface(poly,height,kind):
 if poly.is_empty:return
 x0,z0,x1,z1=poly.bounds
 for cx in range(math.floor(x0/24),math.floor(x1/24)+1):
  for cz in range(math.floor(z0/24),math.floor(z1/24)+1):
   chunk=poly.intersection(box(cx*24,cz*24,(cx+1)*24,(cz+1)*24));v=[]
   for tri in triangulate(chunk):
    if tri.area<1e-7 or not chunk.covers(tri):continue
    for x,z in reversed(list(tri.exterior.coords)[:3]):v.extend([round(x,4),height,round(z,4)])
   if v:ground.append(dict(name=f'{kind}-{cx}-{cz}',kind=kind,vertices=v))
surface(lawns,-.011,'Lawn');surface(plazaBeds,.115,'Soil');surface(streetBeds,.115,'Soil')

items=[];counts=collections.Counter()
def add(asset,x,z,y,scale,owner):
 items.append(dict(asset=asset,x=round(x,4),z=round(z,4),y=y,scale=round(scale,4),yaw=round(rng.uniform(0,360),2),owner=owner));counts[asset]+=1
prepared=prep(lawns);inside=prep(lawns.buffer(-.65));allTrees=[Point(t['x'],t['z']) for t in p['trees']]
treesArea=prep(unary_union([t.buffer(.30) for t in allTrees]))
# Low broken-up ground cover, not a uniform grid or tall grass in paved streets.
for x in range(bounds[0]+3,bounds[1]-3,2):
 for z in range(bounds[2]+3,bounds[3]-3,2):
  px=x+rng.uniform(-.9,.9);pz=z+rng.uniform(-.9,.9);pt=Point(px,pz)
  if not inside.contains(pt) or treesArea.contains(pt):continue
  density=.22+.30*(.5+.5*math.sin(px*.19+math.sin(pz*.09)))
  if rng.random()<density:add('Grass'+str(rng.randint(1,3)),px,pz,-.009,rng.uniform(.72,1.28),'ground cover')

# Continuous verge beds: preserve the FULL pedestrian and driving widths. The
# narrow existing tree strip takes low planting; outer gardens take three layers.
borderReports=[];bedAreas=[]
roadShapes={r['id']:corridor(r['points'],r['width']+1.0) for r in p['roads']}
def plant_ok(x,z,radius,area):
 pt=Point(x,z)
 return area.covers(pt.buffer(radius,resolution=5)) and not treesArea.intersects(pt.buffer(radius*.42))
def ribbon(r,side,outer=False):
 a=np.array(r['points'][0],dtype=float);b=np.array(r['points'][-1],dtype=float);d=b-a;length=np.linalg.norm(d);d/=length;n=np.array([-d[1],d[0]])*side
 line=LineString(r['points']);isAlley=r['lane']==0
 if outer:
  # Width varies in broad 8-15m sweeps so beds do not read as a planted grid.
  width=2.65 if r['id']!='A05' else 3.6
  band=line.buffer(r['width']/2+.45+width,cap_style=2).difference(line.buffer(r['width']/2+.45,cap_style=2)).intersection(lawns)
  sidepoly=LineString([a+n*(r['width']/2+.45+width/2),b+n*(r['width']/2+.45+width/2)]).buffer(width/2,cap_style=2)
  area=band.intersection(sidepoly)
 else:
  if r['strip']<.8:return
  center=r['lane']/2+r['strip']/2;insidewidth=r['strip']-.16
  area=LineString([a+n*center,b+n*center]).buffer(insidewidth/2,cap_style=2).difference(reserved)
  area=area.difference(unary_union([sh for rid,sh in roadShapes.items() if rid!=r['id']]))
 area=area.difference(unary_union([t.buffer(.23) for t in allTrees]))
 if area.is_empty:return
 before=len(items);centers=[]
 if outer:
  for distance in np.arange(1.2,length-1.2,.66 if not isAlley else .77):
   sweep=.5+.5*math.sin(distance*.29+side*1.7)
   for row,depth in enumerate([.40,1.12,1.89,2.66]):
    if depth>1.8+sweep*(1.6 if r['id']=='A05' else .9):continue
    point=a+d*(distance+rng.uniform(-.13,.13))+n*(r['width']/2+.45+depth+rng.uniform(-.09,.09));x,z=point
    phase=int(distance/7+side*2+int(r['id'][1:]))%6
    if row==0:asset=['Flowers1','Flowers6','Flowers1','Flowers7','Flowers6','Flowers1'][phase];scale=rng.uniform(.76,.92)
    elif row==1:asset=['Flowers2','Flowers3','Flowers2','Flowers5','Flowers3','Flowers2'][phase];scale=rng.uniform(.80,1.03)
    else:asset=['Shrub2','Ornamental1','Flowers4','Shrub1','Ornamental2','Flowers5'][phase];scale=rng.uniform(.78,1.03)
    if isAlley and row>1 and rng.random()<.4:continue
    if not plant_ok(x,z,.46*scale,area):continue
    add(asset,x,z,-.004,scale,r['name']+' outer flower border');centers.append(Point(x,z).buffer(.50*scale,resolution=6))
 else:
  for distance in np.arange(1.0,length-1,.60):
   point=a+d*(distance+rng.uniform(-.08,.08))+n*(r['lane']/2+r['strip']/2);x,z=point;phase=int(distance/5+side*2)%6
   asset=['Flowers1','Flowers2','Flowers3','Flowers6','Flowers2','Flowers7'][phase];scale=.72 if r['strip']>1.2 else .60
   if not plant_ok(x,z,.46*scale,area):continue
   add(asset,x,z,.117,scale,r['name']+' tree-strip border');centers.append(Point(x,z).buffer(.47*scale,resolution=6))
 if centers:
  footprint=unary_union(centers).buffer(.16,resolution=2).buffer(-.06,resolution=2).simplify(.10,preserve_topology=True).intersection(area)
  surface(footprint,-.006 if outer else .115,'Border-'+r['id']+str(side)+('-outer' if outer else '-strip'))
  bedAreas.append(footprint);borderReports.append({'road':r['id'],'side':side,'type':'layered outer garden' if outer else 'low existing tree strip','plants':len(items)-before,'area':round(footprint.area,2)})
for r in p['roads']:
 for side in [-1,1]:ribbon(r,side,False);ribbon(r,side,True)

# Larger naturalistic sweeps in the station park and small lawn courts, always
# connected to a bordering walk and with a clear lawn centre.
for land in p['land']:
 if land['kind'] not in ['park','green']:continue
 region=rect(land['rect']).intersection(lawns);safe=region.buffer(-.55)
 x0,z0,x1,z1=region.bounds
 for x in np.arange(x0+.4,x1,.68):
  for z in np.arange(z0+.4,z1,.68):
   px=x+rng.uniform(-.14,.14);pz=z+rng.uniform(-.14,.14);pt=Point(px,pz);depth=region.boundary.distance(pt)
   if depth>2.3+.6*math.sin(px*.5) or not safe.covers(pt):continue
   phase=int((px+pz)*.11)%6
   asset=(['Flowers1','Flowers6','Flowers7'][phase%3] if depth<1.1 else ['Flowers5','Flowers2','Flowers3','Ornamental1','Shrub2','Flowers4'][phase])
   if plant_ok(px,pz,.47,region):add(asset,px,pz,-.004,rng.uniform(.80,1.03),'station garden drift')

for i,t in enumerate(p['trees']):
 if t['kind']=='行道树':continue
 for j in range(4):
  a=rng.uniform(0,math.tau);radius=rng.uniform(1.0,1.65);x=t['x']+math.cos(a)*radius;z=t['z']+math.sin(a)*radius
  if lawns.covers(Point(x,z).buffer(.65)):
   add('Shrub'+str((i+j)%3+1),x,z,-.009,rng.uniform(.7,.95),t['owner'])
   if j%2==0:add('Flowers'+str((i+j)%7+1),x+.22,z+.24,-.004,.82,t['owner'])
for d in p['details']:
 if d['kind']!='bed':continue
 x,z,w,h=d['rect']
 for j in range(max(2,int(w/.42))):
  px=x+(j+.5)*w/max(2,int(w/.42))-w/2;pt=Point(px,z)
  if not reserved.intersects(pt.buffer(.24)) and not road.intersects(pt.buffer(.24)):
   add('Flowers'+str(1+((j//4+len(items))%7)),px,z,.007,.50,d['owner'])
for x in [-7.4,7.4]:
 for z in [-10.7,10.7]:
  for j in range(7):
   px=x+(j-3)*.36
   for row in [-1,1]:add(['Flowers1','Flowers2','Flowers5'][((j//3)+(row==1))%3],px,z+row*.18,.117,.66,'plaza layered border')
for b in p['buildings']:
 x,z,w,h=b['plot']
 for j in range(5):
  a=rng.uniform(0,math.tau);px=x+math.cos(a)*w*.40;pz=z+math.sin(a)*h*.40
  if lawns.covers(Point(px,pz).buffer(.8)):
   add('Shrub'+str(j%3+1),px,pz,-.009,.85,b['id'])
   add('Flowers'+str(j%7+1),px+.22,pz+.35,-.004,.80,b['id'])

# The shared FullMap generator parks the existing milk cart at gate +(-2,-.9).
# It is outside the milk building rectangle, so reserve its full wheels/handles
# plus plant overhang after placement; this does not reshuffle the other gardens.
dairy=next(b for b in p['buildings'] if b['id']=='B04')
cx,cz=dairy['gate'][0]-2,dairy['gate'][1]-.9
cartClear=box(cx-1.60,cz-1.30,cx+1.60,cz+1.30)
removedCart=[i for i in items if cartClear.intersects(Point(i['x'],i['z']))]
items=[i for i in items if not cartClear.intersects(Point(i['x'],i['z']))]
counts=collections.Counter(i['asset'] for i in items)
# The soil surface can remain continuous around this permanent cart; all visible
# foliage and flowers have a margin beyond its real 1.6 x .9m collider.
# Original, small, tileable painterly meadow texture; two world scales hide repetition.
nrng=np.random.default_rng(1931);field=np.zeros((512,512),dtype=float)
for cells,amplitude in [(4,3.5),(13,2.1),(37,1.1)]:
 noise=nrng.normal(0,1,(cells,cells));coords=np.arange(512)*cells/512
 lo=np.floor(coords).astype(int);f=coords-lo;f=f*f*(3-2*f)
 a=noise[lo[:,None]%cells,lo[None,:]%cells];b=noise[lo[:,None]%cells,(lo[None,:]+1)%cells]
 c=noise[(lo[:,None]+1)%cells,lo[None,:]%cells];d=noise[(lo[:,None]+1)%cells,(lo[None,:]+1)%cells]
 field+=amplitude*((a*(1-f)+b*f)*(1-f[:,None])+(c*(1-f)+d*f)*f[:,None])
field+=nrng.normal(0,.9,(512,512))
im=Image.fromarray(np.clip(np.stack([99+field,109+field,78+field*.7],axis=-1),0,255).astype('uint8'))
im.save(R/'game/Assets/Art/Greenery/Meadow.png')
report={'sourceSha256':hashlib.sha256(S.read_bytes()).hexdigest(),'seed':19311011,'treesPreserved':len(p['trees']),'lawnArea':round(lawns.area,2),'plantCounts':dict(counts),'instances':len(items),'surfaceChunks':len(ground),'reservedAreas':'Roads, widened paths, doors, footprints, train platforms/track, service yards, hotel car forecourt and three authored sightlines','flowerBorders':borderReports,'flowerBorderArea':round(sum(a.area for a in bedAreas),2),'treeFamilies':7,'modelVariants':22,'milkCartPlantClearance':{'center':[cx,cz],'excludedGroups':len(removedCart),'reservedRect':[cx-1.6,cz-1.3,cx+1.6,cz+1.3]},'noNewPlantColliders':True,'status':'Authoring only; native rendering and walking verification pending.'}
(R/'game/Assets/Resources/GreeneryPlan.json').write_text(json.dumps({'source':town['sourceSha256'],'modelCount':22,'plants':items,'surfaces':ground},separators=(',',':'),ensure_ascii=False))
(out/'placement-report.json').write_text(json.dumps(report,indent=2,ensure_ascii=False));print(json.dumps(report,ensure_ascii=False))
