import json,math
from pathlib import Path
R=Path(__file__).resolve().parent;D=json.loads((R/'plan-data.json').read_text());B=D['buildings'];errors=[]
def bounds(r):x,z,w,d=r;return(x-w/2,x+w/2,z-d/2,z+d/2)
def overlap(a,b):
 a,b=bounds(a),bounds(b);return min(a[1],b[1])-max(a[0],b[0])>0.01 and min(a[3],b[3])-max(a[2],b[2])>0.01
for i,b in enumerate(B):
 pb=bounds(b['plot'])
 for p in b['parts']:
  r=bounds(p['rect'])
  if not(r[0]>=pb[0]-.01 and r[1]<=pb[1]+.01 and r[2]>=pb[2]-.01 and r[3]<=pb[3]+.01):errors.append(['outside parcel',b['id'],p])
 for other in B[i+1:]:
  if overlap(b['plot'],other['plot']):errors.append(['parcel overlap',b['id'],other['id']])
  for p in b['parts']:
   for q in other['parts']:
    if overlap(p['rect'],q['rect']):errors.append(['building overlap',b['id'],other['id']])
 for j,p in enumerate(b['parts']):
  for q in b['parts'][j+1:]:
   if overlap(p['rect'],q['rect']):errors.append(['own parts overlap',b['id']])
 for door in [b['door'],b['rearDoor']]:
  if not door:continue
  x,z=door;on=False
  for p in b['parts']:
   l,r,s,n=bounds(p['rect'])
   if (l-.01<=x<=r+.01 and min(abs(z-s),abs(z-n))<.01) or(s-.01<=z<=n+.01 and min(abs(x-l),abs(x-r))<.01):on=True
  if not on:errors.append(['door off wall',b['id'],door])
# path centerlines must not enter any building footprint (not collision clearance certification)
for p in D['paths']:
 for a,c in zip(p['points'],p['points'][1:]):
  n=max(1,math.ceil(math.dist(a,c)/.25))
  for i in range(n+1):
   x=a[0]+(c[0]-a[0])*i/n;z=a[1]+(c[1]-a[1])*i/n
   for b in B:
    for part in b['parts']:
     l,r,s,t=bounds(part['rect'])
     if l+.05<x<r-.05 and s+.05<z<t-.05:errors.append(['path crosses building',p['owner'],p['kind'],b['id']]);break
    else:continue
    break
# Street furniture: wholly inside its zone, off carriageways, clear 2m footways, paths, doors,
# facilities, the three story sightlines and the two story homes' frontages.
def seg(p,a,c):
 dx,dz=c[0]-a[0],c[1]-a[1];t=max(0,min(1,((p[0]-a[0])*dx+(p[1]-a[1])*dz)/(dx*dx+dz*dz))) if dx*dx+dz*dz else 0
 return math.hypot(p[0]-a[0]-t*dx,p[1]-a[1]-t*dz)
def corners(it):
 x,z=it['point'];w,d=(it['width'],it['depth']) if it['facing'] in 'NS' else (it['depth'],it['width'])
 return [(x+sx*w/2,z+sz*d/2) for sx in (-1,1) for sz in (-1,1)]+[(x,z)]
def foot(it):
 x,z=it['point'];w,d=(it['width'],it['depth']) if it['facing'] in 'NS' else (it['depth'],it['width']);return [x,z,w,d]
roads={r['id']:r for r in D['roads']}
def offsets(p,r):
 a,c=r['points'];dx,dz=c[0]-a[0],c[1]-a[1];L=math.hypot(dx,dz);t=((p[0]-a[0])*dx+(p[1]-a[1])*dz)/L
 return t,((p[0]-a[0])*dz-(p[1]-a[1])*dx)/L,L
items=D.get('streetItems',[])
protected=[b for b in B if b['id'] in ('B12','B05')]
for it in items:
 c=corners(it);label=[it['id'],it['kind']]
 if it['facing'] not in 'NESW':errors.append(['street item facing',*label])
 for r in D['roads']:
  for p in c:
   t,o,L=offsets(p,r)
   if -.01<t<L+.01 and abs(o)<r['width']/2-.001:
    inner=r['lane']/2 if r['lane'] else 0;band=(inner,inner+r['strip'])
    if abs(o)<inner+.01 and r['lane']:errors.append(['street item on carriageway',*label,r['id']]);break
    if not(band[0]-.01<=abs(o)<=band[1]+.01):errors.append(['street item on footway',*label,r['id']]);break
 if it['zone']=='band':
  r=roads[it['ref']]
  for p in c:
   t,o,L=offsets(p,r)
   if not(r['lane']/2-.01<=abs(o)<=r['lane']/2+r['strip']+.01):errors.append(['street item outside facilities band',*label,r['id']]);break
 for b in B:
  for part in b['parts']:
   if overlap(foot(it),part['rect']):errors.append(['street item in building',*label,b['id']])
  for key in ['door','rearDoor','gate']:
   if b.get(key) and math.dist(b[key],it['point'])<1.2+max(it['width'],it['depth'])/2:errors.append(['street item at door',*label,b['id'],key])
 for q in D['paths']:
  for a,cc in zip(q['points'],q['points'][1:]):
   if min(seg(p,a,cc) for p in c)<q['width']/2+.25:errors.append(['street item on path',*label,q['owner'],q['kind']]);break
 for f in D['facilities']:
  if overlap([*f['point'],f['width']+2,f['depth']+2],foot(it)):errors.append(['street item at facility',*label,f['id']])
 for s in D['sightlines']:
  if min(seg(p,s['a'],s['b']) for p in c)<1.5:errors.append(['street item in sightline',*label,s['id']])
 for b in protected:
  if math.dist(b['door'],it['point'])<25 or math.dist(b['gate'],it['point'])<25:errors.append(['street item at story home',*label,b['id']])
 if it['kind']=='treeguard':
  if not any(math.dist((t['x'],t['z']),it['point'])<.01 for t in D['trees']):errors.append(['tree guard without tree',*label])
 elif any(math.dist((t['x'],t['z']),it['point'])<.9+max(it['width'],it['depth'])/2 for t in D['trees']):errors.append(['street item at tree trunk',*label])
for i,it in enumerate(items):
 for other in items[i+1:]:
  if overlap(foot(it),[*other['point'][:2],*foot(other)[2:]]) or math.dist(it['point'],other['point'])<.6:errors.append(['street items overlap',it['id'],other['id']])
# deduplicate
errors=list({json.dumps(e,ensure_ascii=False):e for e in errors}.values())
for rd in D['roads']:
 if rd['lane'] and abs(rd['width']-(rd['lane']+2*rd['walk']+2*rd['strip']))>.01:errors.append(['road sum',rd['id']])
report=dict(status='PASS' if not errors else 'NEEDS_REPAIR',properties=len(B),namedPlaces=len([b for b in B if b['id'].startswith('B')]),trees=len(D['trees']),streetItems=len(items),checks=['parcel boundaries and non-overlap','building parts do not overlap','doors lie on actual footprint edges','path centerlines outside building interiors at 0.25m intervals','road width components','street items inside their zone, clear of carriageways, footways, paths, doors, facilities, sightlines and story-home frontages'],errors=errors,limitations=['Design plan only; not a building regulation audit','No Unity changes or 3D sightline validation','Path width clearances at gates, furniture and vegetation still require detailed review','All previous route times invalidated by relocation'])
(R/'checks.json').write_text(json.dumps(report,ensure_ascii=False,indent=2));print(json.dumps(report,ensure_ascii=False,indent=2))
