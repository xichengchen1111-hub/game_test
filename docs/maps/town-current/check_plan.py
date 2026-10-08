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
# deduplicate
errors=list({json.dumps(e,ensure_ascii=False):e for e in errors}.values())
for rd in D['roads']:
 if rd['lane'] and abs(rd['width']-(rd['lane']+2*rd['walk']+2*rd['strip']))>.01:errors.append(['road sum',rd['id']])
report=dict(status='PASS' if not errors else 'NEEDS_REPAIR',properties=len(B),namedPlaces=len([b for b in B if b['id'].startswith('B')]),trees=len(D['trees']),checks=['parcel boundaries and non-overlap','building parts do not overlap','doors lie on actual footprint edges','path centerlines outside building interiors at 0.25m intervals','road width components'],errors=errors,limitations=['Design plan only; not a building regulation audit','No Unity changes or 3D sightline validation','Path width clearances at gates, furniture and vegetation still require detailed review','All previous route times invalidated by relocation'])
(R/'checks.json').write_text(json.dumps(report,ensure_ascii=False,indent=2));print(json.dumps(report,ensure_ascii=False,indent=2))
