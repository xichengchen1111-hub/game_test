from pathlib import Path
import json,csv
R=Path(__file__).resolve().parent;d=json.loads((R/'plan-data.json').read_text())
def table(name,keys,rows):
 with (R/name).open('w',encoding='utf-8-sig',newline='') as f:
  w=csv.writer(f,lineterminator="\n");w.writerow(keys);w.writerows(rows)
table('building-parts.csv',['编号','主体或附翼','中心X','中心Z','宽m','深m'],[[b['id'],p['role'],*p['rect']] for b in d['buildings'] for p in b['parts']])
table('trees.csv',['编号','X','Z','冠幅半径m','类型','所属'],[[f'T{i+1:03}',t['x'],t['z'],t['r'],t['kind'],t['owner']] for i,t in enumerate(d['trees'])])
table('roads.csv',['编号','名称','起点X','起点Z','终点X','终点Z','总宽','车行面','单侧净人行道','单侧设施带'],[[r['id'],r['name'],*r['points'][0],*r['points'][-1],r['width'],r['lane'],r['walk'],r['strip']] for r in d['roads']])
openings=[]
for b in d['buildings']:
 for key,role in [('door','前门'),('rearDoor','后门'),('gate','前院门'),('backgate','后院门')]:
  if not b[key]:continue
  facing=b.get('doorFacing',b['front']) if key=='door' else b.get('rearFacing','N' if b['front']=='S' else 'S') if key=='rearDoor' else '待定'
  openings.append([b['id'],role,*b[key],1.3 if key in ['door','rearDoor'] else 1.4,facing])
 for w in b['windows']:openings.append([b['id'],w[3],w[0],w[1],w[2],w[4] if len(w)>4 else b['front']])
table('openings.csv',['建筑','开口','X','Z','宽m','朝向'],openings)
uid=0;layers=[]
def layer(name,objects):layers.append(dict(type='objectgroup',id=len(layers)+1,name=name,objects=objects,visible=True,opacity=1,x=0,y=0))
def obj(name,x,z,w=0,dep=0,**extra):
 global uid
 uid+=1
 return dict(id=uid,name=name,x=(x-w/2+170)*10,y=(122-z-dep/2)*10,width=w*10,height=dep*10,rotation=0,visible=True,**extra)
layer('宅地',[obj(b['id']+' '+b['name'],*b['plot']) for b in d['buildings']])
layer('建筑主体与附翼',[obj(b['id']+' '+p['role'],*p['rect']) for b in d['buildings'] for p in b['parts']])
layer('用途明确的开放用地',[obj(s['name'],*s['rect']) for s in d['land']])
layer('院落设施',[obj(s['owner']+' '+s['kind'],*s['rect']) for s in d['details']])
layer('树冠',[obj(t['kind'],t['x'],t['z'],t['r']*2,t['r']*2,ellipse=True) for t in d['trees']])
layer('门与院门',[obj(b['id']+' '+k,*b[k],point=True) for b in d['buildings'] for k in ['door','rearDoor','gate','backgate'] if b[k]])
for name,items in [('道路',d['roads']),('院内通道',d['paths'])]:
 objects=[]
 for p in items:
  x,z=p['points'][0];o=obj(p.get('name',p.get('owner','通道')),x,z,polyline=[dict(x=(a-x)*10,y=(z-b)*10) for a,b in p['points']],properties=[dict(name='width_m',type='float',value=p['width'])]);objects.append(o)
 layer(name,objects)
layer('剧情设施',[obj(f['id']+' '+f['name'],*f['point'],f['width'],f['depth']) for f in d['facilities']])
(R/'town-plan.tmj').write_text(json.dumps(dict(type='map',version='1.10',tiledversion='1.11.2',orientation='orthogonal',renderorder='right-down',width=410,height=277,tilewidth=10,tileheight=10,infinite=False,layers=layers,nextlayerid=len(layers)+1,nextobjectid=uid+1,properties=[dict(name='scale',type='string',value='10 pixels = 1 metre; X=(px/10)-170; Z=122-(py/10)')]),ensure_ascii=False,indent=2))
print('exported CSV schedules and',uid,'editable objects')
