from pathlib import Path
import json, math, csv, random, html
R=Path(__file__).resolve().parent
random.seed(31)
B=[];Trees=[];Paths=[];Details=[]
# World: east X; north Z. All plan dimensions are design proposals in metres.
def rect(x,z,w,d):return [round(x,3),round(z,3),round(w,3),round(d,3)]
def box(a,b,c,d):return rect((a+b)/2,(c+d)/2,b-a,d-c)
num=0
names={'B01':'钟楼','B02':'钟表铺','B03':'邮局','B04':'牛奶站','B05':'邻居住宅','B06':'酒馆','B07':'修车铺','B08':'旅馆','B09':'当铺','B10':'警署','B11':'医院','B12':'主角住宅','B13':'棋社','B14':'城南店铺','B15':'教堂','B16':'妻子父亲家','B17':'车站'}
def plot(a,b,c,d,side='S',kind='shop',bid=None,name=None,variant=0):
 global num
 if kind=='terrace' and b-a>=12 and bid is None:
  mid=(a+b)/2;plot(a,mid,c,d,side,kind,variant=variant);return plot(mid,b,c,d,side,kind,variant=variant+1)
 num+=1;bid=bid or f'H{num:02}'
 x=(a+b)/2;pw=b-a;pd=d-c
 front={'shop':.7,'terrace':2,'semi':4,'garden':5,'civic':2,'work':3}[kind]
 bw=pw if kind in ['shop','terrace'] else pw-4
 if kind=='semi':
  bw=pw-4;x+=2 if a in [-94,-17] else -2
 if kind=='garden':bw=min(16 if bid=='B12' else 13,pw-6)
 bw=round(bw,2);dep=min((10+variant%3) if kind in ['garden','semi','terrace'] else (11+variant%4),pd-12)
 # The finished shop's local facade faces south: preserve its authored dimensions.
 if bid=='B02':bw=7.4;dep=8.2
 if kind=='civic':bw=pw-6;dep=13
 if kind=='work':bw=pw-7;dep=12
 z=(c+front+dep/2) if side=='S' else (d-front-dep/2)
 parts=[dict(role='主体',rect=rect(x,z,bw,dep))]
 # Asymmetric kitchen/workshop wing, with no overlap with main footprint.
 wing=kind not in ['semi'] and variant%3!=0 and bid!='B02'
 if wing:
  wd=min(7,pd-front-dep-5);ww=round(max(3,bw*.33),2)
  wx=x+(bw-ww)/2*(1 if variant%2 else -1);wz=z+(dep+wd)/2*(1 if side=='S' else -1)
  parts.append(dict(role='后翼／厨房' if kind!='work' else '修理间',rect=rect(wx,wz,ww,wd)))
 if kind=='work' and bid=='B04':
  parts.append(dict(role='牛奶冷藏间',rect=rect(a+5,c+5,6,8)))
 door=[round(x,2),round(z+(-dep/2 if side=='S' else dep/2),2)]
 gate=[x,c if side=='S' else d];backgate=[b-1.2 if kind in ['garden','semi','terrace'] and variant%2==0 else a+1.2,d if side=='S' else c]
 rearz=d-1.2 if side=='S' else c+1.2
 yard='后院菜畦＋储物棚' if kind in ['terrace','semi','garden'] else '后勤硬院＋小花园'
 typ={'shop':'沿街商住＋后院','terrace':'共墙连排住宅','semi':'双拼住宅','garden':'退界花园住宅','civic':'公共建筑＋独立院地','work':'作坊／生产院落'}[kind]
 if wing:typ+=' · L形后翼'
 floors=1 if bid=='B02' or kind=='work' else 2
 windows=[]
 for off in [-bw*.3,bw*.3]:windows.append([round(x+off,2),door[1],1.4,'首层前窗'])
 rearDoor=[round(x+(bw*.18*(1 if variant%2==0 else -1) if wing else 0),3),round(z+(dep/2 if side=='S' else -dep/2),3)]
 bld=dict(id=bid,name=name or names.get(bid,f'住户 {bid}'),kind=kind,type=typ,plot=box(a,b,c,d),parts=parts,front=side,storeys=floors,door=door,rearDoor=rearDoor,gate=gate,backgate=backgate,windows=windows,frontSetback=front,yard=yard,status='既有剧情地点；位置与宅地为本轮提案' if bid in names else '生活背景补充；不新增线索',note='前门接人行道；后院有独立后勤入口。')
 if bid=='B02':bld['note']='沿用完成模型的南向门面与7.4×8.2m占地；门口保持(-35,27.7)，主体中心据此校准。'
 B.append(bld)
 Paths.append(dict(owner=bid,points=[door,gate],width=1.4,kind='front'))
 # side service path on garden plots, or through house to private rear garden on terraces.
 Paths.append(dict(owner=bid,points=[backgate,[backgate[0],rearz],[x,rearz],rearDoor],width=1.0,kind='rear'))
 if kind in ['garden','semi','terrace']:
  # Vegetables at side opposite rear wing.
  vx=a+2 if (variant%2) else b-2
  vz=d-4 if side=='S' else c+4
  for k in range(3):Details.append(dict(kind='bed',owner=bid,rect=rect(vx,vz+k*.8*(1 if side=='N' else -1),2.3,.5)))
  sx=b-2 if variant%2 else a+2
  sz=d-2 if side=='S' else c+2
  Details.append(dict(kind='shed',owner=bid,rect=rect(sx,sz,2.8,2.4)))
  if pw>15:Trees.append(dict(x=round(a+pw*.7,2),z=round(d-6 if side=='S' else c+6,2),r=2.2,kind='院树',owner=bid))
 else:
  Details.append(dict(kind='service',owner=bid,rect=rect(b-3,d-3 if side=='S' else c+3,min(4,pw-2),2)))
 return bld
# Main street: continuous frontage on both sides; narrow rear plots.
for a,b,bid,n,v in [(-159,-144,None,'杂货商住',1),(-144,-126,'B07',None,2),(-114,-96,None,'裁缝商住',1),(-96,-77,'B06',None,2),(-77,-64,None,'面包房商住',0),(-64,-53,'B09',None,1),(-53,-41,None,'普通商住',2),(-41,-29,'B02',None,0),(-29,-17,None,'文具商住',1),(-17,1,'B03',None,2),(1,24,'B10',None,1),(36,51,None,'沿街住宅',2),(51,64,None,'修鞋铺商住',0),(64,79,None,'普通商住',1),(79,93,None,'布店商住',2),(93,108,None,'普通商住',0),(108,129,None,'转角商住',2)]:
 plot(a,b,27,66,'S','shop',bid,n,v)
# 2026-10-12 user-approved D「内院回廊警署」(docs/concepts/police-station/round-01, selected 2026-10-11):
# 23x12m two-storey front range, two 5.5x12m single-storey rear wings, open 12m court with 1.5m loggias.
# Exact rooms/openings live in tools/police/police_layout.py, which asserts these parts, doors and windows.
pb=next(b for b in B if b['id']=='B10')
pb['parts']=[dict(role='两层前楼',rect=box(1,24,27.7,39.7)),dict(role='一层西翼',rect=box(1,6.5,39.7,51.7)),dict(role='一层东翼',rect=box(18.5,24,39.7,51.7))]
pb['storeys']=2;pb['type']='U形前楼＋两翼回廊内院 · 警署';pb['yard']='回廊内院＋北侧后勤硬院'
pb['status']='已建成可进入：D内院回廊警署（2026-10-12）'
pb['note']='前楼两层、两翼一层；南正门(12.5,27.7)、横厅后门通内院(8.36,39.7)，内院向北经后院门(2.2,66)到北后巷。'
pb['windows']=[[x,27.7,1.2,'首层前窗','S'] for x in (3.4,6.2,9.0,16.0,18.8,21.6)]+[[x,27.7,1.1,'二层前窗','S'] for x in (3.4,6.2,10.3,12.5,14.7,18.8,21.6)]+\
 [[24,30.9,1.2,'候问室侧窗','E'],[24,38.2,1.0,'楼梯平台窗','E'],[24,30.9,1.1,'二层休息室侧窗','E'],[24,41.7,1.2,'巡警值班侧窗','E'],[24,45.5,1.0,'物证室高窗','E']]+\
 [[7.0,39.7,.7,'誊录室朝廊窗','N'],[10.35,39.7,.7,'盥洗高窗','N'],[13.4,39.7,1.2,'值班室朝院窗','N'],[15.8,39.7,1.2,'值班室朝院窗','N']]+\
 [[x,39.7,1.1,'二层档案朝院窗','N'] for x in (9.6,12.0,14.4)]+[[17.9,39.7,.7,'二层盥洗朝院窗','N']]+\
 [[6.5,43.5,1.4,'资料分馆朝廊窗','E'],[6.5,49.5,1.0,'档案库朝廊窗','E'],[18.5,42.5,1.2,'巡警值班朝院窗','W'],[3.75,51.7,1.2,'档案库北高窗','N'],[21.25,51.7,.8,'旧羁押室北高窗','N']]
for a,b,n,v in [(-114,-100,'转角住宅',1),(-100,-87,'沿街住宅',2),(-87,-73,'沿街住宅',0),(-73,-60,'沿街住宅',1),(-60,-47,'沿街住宅',2),(-47,-33,'沿街住宅',0),(-33,-18,'沿街住宅',2),(36,51,'转角商住',1),(51,65,'沿街住宅',2),(65,79,'沿街住宅',0),(79,94,'沿街住宅',1),(94,110,'沿街住宅',2),(110,129,'转角住宅',0)]:plot(a,b,-27,13,'N','shop',name=n,variant=v)
# Western working edge, dairy yard directly opens to west lane.
b=plot(-160,-127,-27,13,'N','work','B04',variant=1)
# Residential frontage facing south, backyard to through lane.
for a,b,bid,k,v in [(-114,-94,None,'garden',2),(-94,-76,None,'semi',0),(-76,-58,None,'semi',0),(-58,-37,'B05','garden',1),(-37,-12,'B12','garden',2),(-12,9,None,'garden',1),(9,24,None,'terrace',0),(36,52,None,'terrace',1),(52,68,None,'terrace',2)]:plot(a,b,-68,-34,'S',k,bid,variant=v)
# Hospital wings and courtyard occupy one intentional institutional site.
h=plot(73,129,-68,-34,'S','civic','B11',variant=0)
h['parts']=[dict(role='门诊楼',rect=rect(101,-59,42,12)),dict(role='西病房翼',rect=rect(84,-44,8,18)),dict(role='东病房翼',rect=rect(118,-44,8,18))];h['door']=[101,-65];h['rearDoor']=[101,-53];h['windows']=[[90,-65,2,'诊室窗'],[112,-65,2,'诊室窗']];h['type']='U形医院 · 内向康复庭院';h['note']='三翼围合院子；病房朝内院；侧面保留后勤通道。';Paths[-2]['points']=[[101,-65],[101,-68]];h['backgate']=[101,-34];Paths[-1]['points']=[[101,-34],[101,-53]]
# South residential band: rows / paired / different house footprints.
for a,b,bid,k,v in [(-114,-102,None,'terrace',0),(-102,-90,None,'terrace',1),(-90,-65,'B16','garden',2),(-65,-53,None,'terrace',1),(-53,-41,None,'terrace',0),(-41,-29,None,'terrace',2),(-29,-17,None,'terrace',1),(-17,3,None,'semi',0),(3,24,None,'semi',0)]:plot(a,b,-113,-82,'N',k,bid,variant=v)
# church cruciform footprint + walled garden
ch=plot(36,80,-113,-82,'N','civic','B15',variant=0);ch['parts']=[dict(role='中殿',rect=rect(58,-98,9,24)),dict(role='西耳堂',rect=rect(50.5,-101,6,7)),dict(role='东耳堂',rect=rect(65.5,-101,6,7)),dict(role='门廊',rect=rect(58,-84.5,5,3))];ch['door']=[58,-83];ch['rearDoor']=[58,-110];ch['type']='十字形教堂＋围墙花园';ch['note']='教堂侧园保留公共树荫与座椅；不是无用途空地。';ch['windows']=[[53.5,-94,1.4,'侧窗'],[62.5,-94,1.4,'侧窗']];Paths[-2]['points']=[[58,-83],[58,-82]]
plot(83,105,-113,-82,'N','garden','B13',variant=1)
plot(105,129,-113,-82,'N','shop','B14',variant=2)
# village edge cottages with front garden / rear vegetable plot
for a,b,c,d,v in [(144,165,-113,-82,1),(165,187,-113,-82,2),(187,216,-113,-82,0),(-160,-128,-68,-34,2),(-160,-128,-113,-82,1)]:plot(a,b,c,d,'N' if d==-82 else 'S','garden',variant=v)
# Inn courtyard opening toward station street, under carriage passage planned as open yard.
inb=plot(144,185,-27,13,'N','civic','B08',variant=0);inb['parts']=[dict(role='客房西翼',rect=rect(149,-8,10,30)),dict(role='客房东翼',rect=rect(180,-8,10,30)),dict(role='餐厅后翼',rect=rect(164.5,-21,21,4))];inb['door']=[154,5];inb['rearDoor']=[175,-19];inb['type']='U形旅馆＋前庭车马院';inb['note']='旅馆开口朝车站路；中央庭院用于上下客，后部餐厅与两翼客房分区。';Paths[-2]['points']=[[154,5],[164.5,5],[164.5,13]];inb['windows']=[[144,0,1.8,'客房窗'],[185,0,1.8,'客房窗']]
# Station, separate front forecourt / rear platform.
st=plot(153,224,30,61,'S','civic','B17',variant=0);st['parts']=[dict(role='候车厅',rect=rect(188,46,30,12)),dict(role='行李房',rect=rect(169,46,8,8)),dict(role='值班室',rect=rect(207,46,8,8))];st['door']=[188,40];st['rearDoor']=[188,52];st['storeys']=1;st['type']='B铁与玻璃候车厅＋铁路雨棚';st['note']='完成的B方案：主厅30×12m、侧翼各8×8m；68×22m铁玻璃雨棚位于既有铁路用地，站台顶0.42m，铁轨中心Z73。无新增历史班次或人物行程。';st['windows']=[[188+x,z,2.2,'候车厅窗',side] for z,side in [(40,'S'),(52,'N')] for x in [-12,-8,-4,4,8,12]]+[[x,46+z,1.4,'侧翼窗',side] for x,side in [(165,'W'),(211,'E')] for z in [-2,2]]+[[x,z,1.4,'侧翼窗',side] for x in [169,207] for z,side in [(42,'S'),(50,'N')]]+[[188,z,1.64,'圆形玫瑰窗',side] for z,side in [(40,'S'),(52,'N')]];st['railCanopy']=[188,66,68,22];st['platformTop']=.42;st['trackZ']=73;Paths[-2]['points']=[[188,40],[188,30]];Paths[-1]['points']=[[188,52],[188,61]]
B.append(dict(id='B01',name='钟楼',kind='civic',type='广场独立地标',plot=rect(0,0,30,27),parts=[dict(role='钟楼基座',rect=rect(0,0,6,6))],front='S',storeys=1,door=[0,-3],rearDoor=None,gate=[0,-13.5],backgate=None,windows=[],frontSetback=10.5,yard='公共铺石广场',status='剧情地点；位置为提案',note='广场是明确的公共停留空间；通往商业街及南侧住宅巷。'))
roads=[dict(id='R01',name='车站路',points=[[-166,20],[224,20]],width=12,lane=6,walk=2,strip=1),dict(id='R02',name='住宅街',points=[[-165,-75],[224,-75]],width=12,lane=5,walk=2,strip=1.5),dict(id='R03',name='南街',points=[[-163,-122],[222,-122]],width=10,lane=5,walk=1.5,strip=1),dict(id='R04',name='西巷',points=[[-120,-139],[-120,96]],width=10,lane=4,walk=2,strip=1),dict(id='R05',name='中街',points=[[30,-140],[30,96]],width=10,lane=4,walk=2,strip=1),dict(id='R06',name='站西路',points=[[135,-140],[135,71]],width=10,lane=4,walk=2,strip=1),dict(id='A01',name='北后巷',points=[[-164,69],[134,69]],width=3,lane=0,walk=3,strip=0),dict(id='A02',name='住宅后巷',points=[[-164,-30],[135,-30]],width=3,lane=0,walk=3,strip=0),dict(id='A03',name='南园巷',points=[[-163,-117],[223,-117]],width=3,lane=0,walk=3,strip=0),dict(id='A04',name='广场步巷',points=[[0,-13.5],[0,-30]],width=3,lane=0,walk=3,strip=0),dict(id='A05',name='北园路',points=[[-165,91],[135,91]],width=6,lane=3,walk=1.5,strip=0)]
# Roadside trees in planting strips, not in clear walking bands. Gaps at intersections.
for road in roads[:3]:
 z=road['points'][0][1];offset=road['lane']/2+road['strip']/2
 for x in range(-156,220,14):
  if min(abs(x-v) for v in [-120,30,135])<8:continue
  if road['id']=='R01' and -17<x<25:continue
  for sign in [-1,1]:Trees.append(dict(x=x,z=z+sign*offset,r=2.3+(x%3)*.22,kind='行道树',owner=road['id']))
for x in [-120,30,135]:
 for z in range(-106,63,17):
  if min(abs(z-v) for v in [-122,-75,-30,20,69])<8:continue
  Trees.append(dict(x=x+2.5,z=z,r=2.1,kind='行道树',owner='纵街'))
land=[dict(name='北侧果园',rect=box(-164,-50,96,118),kind='orchard'),dict(name='北园菜地',rect=box(-44,23,96,118),kind='allotment'),dict(name='农地／镇缘',rect=box(36,130,96,118),kind='field'),dict(name='铁路用地',rect=box(145,227,63,78),kind='rail'),dict(name='候车绿地',rect=box(190,223,-27,13),kind='park'),dict(name='居民菜园',rect=box(145,216,-67,-34),kind='allotment'),dict(name='西侧果园',rect=box(-164,-127,73,86),kind='orchard'),dict(name='共享晾晒院',rect=box(-114,24,73,86),kind='court'),dict(name='修理棚与堆场',rect=box(36,129,73,86),kind='workyard'),dict(name='南部农田',rect=box(-165,224,-150,-130),kind='field')]
for space in land:
 x,z,w,d=space['rect']
 if space['kind'] in ['orchard','park']:
  for xx in range(math.ceil(x-w/2+5),math.floor(x+w/2-3),9):
   for zz in range(math.ceil(z-d/2+5),math.floor(z+d/2-2),9):Trees.append(dict(x=xx,z=zz,r=2.6,kind='果树' if space['kind']=='orchard' else '公共绿地树',owner=space['name']))
for b in B:
 if b['id']=='B04':b['backgate']=[-143.5,-27];b['yard']='装卸硬院＋独立冷藏间'
 if b['id']=='B08':b['yard']='朝街前庭车马院';b['frontSetback']=8;b['windows']=[[149,7,1.8,'客房前窗'],[180,7,1.8,'客房前窗']]
 if b['id']=='B11':b['yard']='三翼围合康复庭院';b['frontSetback']=3
 if b['id']=='B15':b['yard']='围墙侧园与公共树荫';b['frontSetback']=1;b['windows']=[[53.5,-94,1.4,'侧窗','W'],[62.5,-94,1.4,'侧窗','E']]
 if b['id']=='B17':b['yard']='站前候车场＋后侧站台';b['frontSetback']=10;b['backgate']=[188,61]
for p in Paths:
 if p['owner']=='B04' and p['kind']=='rear':p['points']=[[-143.5,-27],[-143.7,-2]]
# Access aprons bridge the plot boundary to the public sidewalk edge.
for p in Paths:
 if p['kind']=='front':
  b=next(b for b in B if b['id']==p['owner']);g=b['gate'];side=b['front'];p['points'].append([g[0],g[1]+(-1 if side=='S' else 1)])
# Correct exceptional doors and remove generic service pads from station platform.
inb['doorFacing']='E';inb['rearFacing']='S';inb['rearDoor']=[170,-23];inb['backgate']=[164.5,-27]
for p in Paths:
 if p['owner']=='B08' and p['kind']=='rear':p['points']=[[164.5,-27],[170,-26],[170,-23]]
 if p['owner']=='B15' and p['kind']=='rear':p['points']=[[37.2,-113],[58,-112],[58,-110]]
Details=[d for d in Details if d['owner']!='B17']
# Connect entrances to the closest public pavement edge, not just a plot boundary.
def nearest_edge(g):
 options=[]
 for rd in roads:
  a,e=rd['points'];qx=max(min(g[0],max(a[0],e[0])),min(a[0],e[0]));qz=max(min(g[1],max(a[1],e[1])),min(a[1],e[1]));dx,dz=g[0]-qx,g[1]-qz;dist=math.hypot(dx,dz)
  if dist:options.append((dist-rd['width']/2,[round(qx+dx/dist*rd['width']/2,2),round(qz+dz/dist*rd['width']/2,2)]))
 return min(options,key=lambda t:t[0])[1]
for p in Paths:
 b=next(b for b in B if b['id']==p['owner'])
 if p['kind']=='front':p['points'][-1]=nearest_edge(b['gate'])
 elif b['id']!='B17':p['points'].insert(0,nearest_edge(b['backgate']))
# Screen out planted trunks directly at access paths (>= 0.5m trunk radius + path half width).
def segdist(p,a,b):
 dx=b[0]-a[0];dz=b[1]-a[1];t=max(0,min(1,((p[0]-a[0])*dx+(p[1]-a[1])*dz)/(dx*dx+dz*dz))) if dx*dx+dz*dz else 0
 return math.hypot(p[0]-a[0]-t*dx,p[1]-a[1]-t*dz)
Trees=[t for t in Trees if all(segdist([t['x'],t['z']],a,b)>p['width']/2+.6 for p in Paths for a,b in zip(p['points'],p['points'][1:]))]
exec((R/'story_layout.py').read_text())
D=dict(version=4,sourceCommit='4ee07d1',storyRevision='2026-10-08 user-confirmed inter-town trip',offMapJourneys=[dict(id='J19',date='1931-01-19',origin='B17',destination='另一镇（不制图）',mode='train',actual=True,purpose='17:20在另一镇南区问不存在的东街',travelMinutes=None,departureTime=None,returnTime=None),dict(id='J20-claim',date='1931-01-20',origin='B17',destination='另一镇（声称）',mode='train',actual=False,basis='把19日行程挪进20日陈述，不是当天实际路线')],unit='m',bounds=[-170,240,-155,122],origin='钟楼中心 (0,0)',buildings=B,roads=roads,trees=Trees,paths=Paths,details=Details,land=land,facilities=Facilities,sightlines=Sightlines,notes=['1931年背景下的虚构小镇平面设计，不是测绘复原。','参考英式老镇连续街面、细长后院与后巷关系；尺寸均为本设计。','建筑形态变更未同步Unity。上一版路线距离、时间和视线结论不适用于此版。','普通商住、住宅与生产附属设施为环境补充，不新增剧情证据。'])
(R/'plan-data.json').write_text(json.dumps(D,ensure_ascii=False,indent=2));(R/'plan-data.js').write_text('window.PLAN='+json.dumps(D,ensure_ascii=False)+';')
# SVG with reusable map layers.
W,H=2250,1630;S=5;OX=105;OY=145
P=lambda x,z:(OX+(x+170)*S,OY+(122-z)*S)
def rr(r,fill,stroke='none',sw=.18,extra=''):
 x,z,w,d=r;xx,yy=P(x-w/2,z+d/2);return f'<rect x="{xx}" y="{yy}" width="{w*S}" height="{d*S}" fill="{fill}" stroke="{stroke}" stroke-width="{sw*S}" {extra}/>'
def line(ps,col,width=.25,extra=''):
 return '<path d="'+' '.join(('M' if i==0 else 'L')+f'{P(*p)[0]},{P(*p)[1]}' for i,p in enumerate(ps))+f'" fill="none" stroke="{col}" stroke-width="{width*S}" {extra}/>'
def txt(x,z,t,size=10,col='#314b47',extra=''):
 xx,yy=P(x,z);return f'<text x="{xx}" y="{yy}" fill="{col}" font-size="{size}" text-anchor="middle" {extra}>{html.escape(str(t))}</text>'
svg=[f'<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 {W} {H}" id="town-svg"><defs><pattern id="crops" width="12" height="12" patternUnits="userSpaceOnUse"><path d="M0 10L12 10" stroke="#9caa7c" stroke-width="1"/></pattern><pattern id="stone" width="14" height="10" patternUnits="userSpaceOnUse"><path d="M0 0H14M0 5H14M7 0V5M3 5V10" fill="none" stroke="#c0bba5" stroke-width=".5"/></pattern></defs><style>text{{font-family:PingFang SC,Microsoft YaHei,sans-serif}}.label{{paint-order:stroke;stroke:#f6f2e7;stroke-width:3px;stroke-linejoin:round}}.building{{cursor:pointer}}.building:hover .foot{{stroke:#bd573d;stroke-width:3px}}.selected .foot{{stroke:#b34d30;stroke-width:3px}}</style><rect width="2250" height="1630" fill="#f5f1e6"/><text x="104" y="57" fill="#244c47" font-size="34" font-weight="600">青川镇 · 宅地与街区详图</text><text x="106" y="89" fill="#67786b" font-size="16">第四版 / 跨镇日期纠正　·　连续街面、前后院、后巷和有用途的空地　·　2026.10.08</text><text x="2070" y="65" fill="#244c47" font-size="23">↑ 北 N</text>',rr(box(-170,240,-155,122),'#dfe3cc')]
svg.append('<g id="land">')
for sp in land:
 svg.append(rr(sp['rect'],'#d5dabb' if sp['kind'] not in ['court','workyard','rail'] else '#d6d0ba','#a8b18f'))
 if sp['kind'] in ['field','allotment']:svg.append(rr(sp['rect'],'url(#crops)'))
 svg.append(txt(sp['rect'][0],sp['rect'][1],sp['name'],12,extra='class="label"'))
svg.append('</g><g id="roads">')
for rd in roads:svg.append(line(rd['points'],'#c3c5aa',rd['width'],'stroke-linecap="square"'))
for rd in roads:
 if rd['lane']:svg.append(line(rd['points'],'#ded9c6',rd['lane']+2*rd['walk'],'stroke-linecap="square"'))
for rd in roads:
 if rd['lane']:svg.append(line(rd['points'],'#a0a697',rd['lane'],'stroke-linecap="square"'))
for z in [72.2825,73.7175]:svg.append(line([[144,z],[232,z]],'#706e60',.16))
for x in range(145,233):svg.append(line([[x,71.75],[x,74.25]],'#857c68',.22))
svg.append(rr(rect(188,67.5,68,7.9),'#c0b69c','#8a8772'));svg.append(txt(188,67.5,'站台 68 × 7.9 m · 顶高 0.42m',10))
svg.append(rr(st['railCanopy'],'none','#41665b',.18,'stroke-dasharray="5 3"'));svg.append(txt(188,80,'B 铁玻璃雨棚 68 × 22 m',11))
svg.append('</g><g id="plots">')
for b in B:
 col='#d7d9bc' if b['kind'] in ['garden','semi','terrace'] else '#d9cdb3'
 if b['id']=='B01':col='url(#stone)'
 svg.append(rr(b['plot'],col,'#8d9680',.18))
 # yard paving inset around house; actual front paths separately shown
 if b['id']!='B01':
  x,z,w,d=b['plot'];front=b['frontSetback'];fz=z-d/2+front/2 if b['front']=='S' else z+d/2-front/2
  svg.append(rr(rect(x+w/2-front/2,z,front,d) if b['front']=='E' else rect(x,fz,w,front),'#d9d6c1'))
svg.append('</g><g id="paths">')
for p in Paths:svg.append(line(p['points'],'#ede4ca',p['width'],'stroke-linejoin="round"'))
svg.append('</g><g id="details">')
# Distinguish rear yard use: cobbled work apron, vegetable patch, clothes drying line.
for b in B:
 if b['id'] in ['B01','B08','B11','B15','B17']:continue
 x,z,w,d=b['plot'];rear=z+d/2-8 if b['front']=='S' else z-d/2+8
 if b['kind'] in ['shop','work']:
  svg.append(rr(rect(x,rear,w-3,8),'url(#stone)'))
  svg.append(rr(rect(x+1,rear-1,max(2,w*.22),3),'#b3bf91','#8d9d79',.12))
 else:
  svg.append(line([[x-2,rear],[x+2,rear]],'#898773',.12))
  for i in [-1,0,1]:svg.append(rr(rect(x+i,rear-.4,.6,.7),'#f4ead7','#b9b49c',.06))
# Northern communal yard: planted pocket and open drying court; western/eastern sheds.
for x in range(-109,21,12):
 svg.append(line([[x,78],[x+6,78]],'#9b947d',.16))
 for dx in [1,3,5]:svg.append(rr(rect(x+dx,77.4,1,1),'#ece5cd','#b2ac96',.08))
for x in [47,70,95,116]:svg.append(rr(rect(x,80,12,7),'#b4a78e','#7f8f7c',.18))

for det in Details:svg.append(rr(det['rect'],{'bed':'#9aab79','shed':'#b9ad94','service':'#bcb39d'}[det['kind']],'#89917c',.1))
# public benches, lamp columns and drainage marks
for x,z in [(-10,8),(10,8),(-10,-8),(10,-8),(194,33),(205,33),(42,-108),(74,-107)]:svg.append(rr(rect(x,z,2,.6),'#82654b'))
for rd in roads[:3]:
 z=rd['points'][0][1]
 for x in range(-150,215,24):
  if min(abs(x-t) for t in [-120,30,135])>7:
   xx,yy=P(x,z-rd['width']/2+.35);svg.append(f'<circle cx="{xx}" cy="{yy}" r="1.8" fill="#6c6956"/>')
svg.append('</g><g id="buildings">')
colors={'shop':'#b69b7a','garden':'#90a594','semi':'#9eae94','terrace':'#a5b09a','civic':'#7f9f9a','work':'#a89b84'}
for b in B:
 svg.append(f'<g class="building" data-id="{b["id"]}"><title>{b["id"]} {html.escape(b["name"])}</title>')
 for part in b['parts']:
  r=part['rect'];svg.append(rr([r[0]+.4,r[1]-.4,r[2],r[3]],'#8d937e',extra='opacity=".25"'));svg.append(rr(r,colors[b['kind']],'#637d71',.2,'class="foot"'))
  x,z,w,d=r
  svg.append(line([[x-w/2+1,z],[x+w/2-1,z]],'#e0dbbf',.15))
 svg.append('</g>')
svg.append('</g><g id="openings">')
for b in B:
 for pt,facing in [(b['door'],b.get('doorFacing',b['front'])),(b['rearDoor'],b.get('rearFacing','N' if b['front']=='S' else 'S'))]:
  if pt:svg.append(line([[pt[0]-.65,pt[1]],[pt[0]+.65,pt[1]]] if facing in ['N','S'] else [[pt[0],pt[1]-.65],[pt[0],pt[1]+.65]],'#744d32',.65))
 for pt in [b['gate'],b['backgate']]:
  if pt:svg.append(line([[pt[0]-.7,pt[1]],[pt[0]+.7,pt[1]]],'#f5f1e6',.5));svg.append(line([[pt[0]-.7,pt[1]],[pt[0]+.7,pt[1]+.7]],'#8f795b',.18))
 for win in b['windows']:
  x,z,w,n=win[:4];facing=win[4] if len(win)>4 else b['front'];svg.append(line([[x-w/2,z],[x+w/2,z]] if facing in ['N','S'] else [[x,z-w/2],[x,z+w/2]],'#416d79',.45))
svg.append('</g><g id="trees">')
for t in Trees:
 x,y=P(t['x'],t['z']);r=t['r']*S
 svg.append(f'<circle cx="{x+2}" cy="{y+2}" r="{r}" fill="#77865f" opacity=".10"/><circle cx="{x}" cy="{y}" r="{r}" fill="#a7b585" fill-opacity=".68" stroke="#7f976c" stroke-width=".8"/><circle cx="{x-.2*r}" cy="{y-.15*r}" r="{r*.67}" fill="none" stroke="#c8d0aa" stroke-width=".6"/><circle cx="{x}" cy="{y}" r="1.6" fill="#657252"/>')
svg.append('</g><g id="grid" opacity=".35">')
for x in range(-150,241,25):svg.append(line([[x,-150],[x,120]],'#839184',.08,'stroke-dasharray="3 5"'));svg.append(txt(x,124,x,10))
for z in range(-150,121,25):svg.append(line([[-165,z],[237,z]],'#839184',.08,'stroke-dasharray="3 5"'));svg.append(txt(-174,z,z,10))
svg.append('</g><g id="labels">')
for b in B:
 x,z,w,d=b['parts'][0]['rect'];label=b['id']+' '+b['name'] if b['id'].startswith('B') else b['id']
 svg.append(txt(x,z,label,11 if b['id'].startswith('B') else 9,extra='class="label"'))
for x,z,t in [(-73,20,'车 站 路  ·  12m'),(80,20,'车 站 路'),(-78,-75,'住 宅 街  ·  12m'),(87,-75,'住 宅 街'),(-64,-122,'南 街  ·  10m'),(-71,69,'北后巷 3m'),(-70,-30,'住宅后巷 3m'),(-121,-10,'西巷'),(30,-45,'中街'),(135,-48,'站西路')]:svg.append(txt(x,z-.8,t,11,extra='class="label"'))
svg.append('</g><g id="story-notes">')
for f in Facilities:
 svg.append(rr(rect(*f['point'],f['width'],f['depth']),'#ae704e','#704b33',.2));svg.append(txt(f['point'][0],f['point'][1]+3,f['id']+' '+f['name'],11,extra='class="label"'))
for v in Sightlines:
 svg.append(line([v['a'],v['b']],'#4a8190' if v['id']!='S03' else '#af6346',.28,'stroke-dasharray="4 4"'))
svg.append(line([[-37,-61],[-37,-53]],'#a37853',.35));svg.append(txt(-36,-52,'低栏0.85m',9,extra='class="label"'))
svg.append(txt(186,105,'本镇车站 → 列车 → 另一镇（未绘制）',12,extra='class="label"'))
svg.append(txt(186,98,'1月19日真实乘车问路；20日挪用前一天陈述',10,extra='class="label"'))
svg.append('</g><g id="footer"><text x="105" y="1568" font-size="15" fill="#445f52">X 向东 / Z 向北　·　单位：米　·　B 编号为命名建筑，含候选与背景地点　·　H 编号为环境补充住宅／商住　·　细线为宅地边界，实色为建筑占地</text><text x="105" y="1600" font-size="14" fill="#75806a">设计提案，未同步 Unity；路线与案件时间须按新布局重新核对。前后院、门窗位置为平面方案，不代表已完成室内设计。</text><path d="M1770 1580h250" stroke="#294c44" stroke-width="3"/><text x="1895" y="1567" text-anchor="middle" font-size="14" fill="#294c44">50 m</text></g></svg>')
(R/'town-plan.svg').write_text(''.join(svg))
# building schedule: main footprint + actual parts area, parcel sizes separate.
with (R/'building-schedule.csv').open('w',encoding='utf-8-sig',newline='') as f:
 w=csv.writer(f,lineterminator="\n");w.writerow(['编号','名称','类型','宅地中心X','宅地中心Z','宅地宽','宅地深','宅地面积m2','建筑占地面积m2','层数','前门X','前门Z','前院退界m','状态'])
 for b in B:
  x,z,pw,pd=b['plot'];w.writerow([b['id'],b['name'],b['type'],x,z,pw,pd,pw*pd,sum(p['rect'][2]*p['rect'][3] for p in b['parts']),b['storeys'],*b['door'],b['frontSetback'],b['status']])
print(len(B),'properties;',len(Trees),'trees;',len(Paths),'paths')
