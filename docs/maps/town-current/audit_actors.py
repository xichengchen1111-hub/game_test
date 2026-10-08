from pathlib import Path
import json,math,heapq,itertools,csv
R=Path(__file__).resolve().parent;D=json.loads((R/'plan-data.json').read_text());B={b['id']:b for b in D['buildings']};roads=D['roads'];G={};ends={};Rts=[];Actors=[]
def key(p):return tuple(round(v,4) for v in p)
def dist(a,b):return math.dist(a,b)
def project(p,a,b):
 dx,dz=b[0]-a[0],b[1]-a[1];t=max(0,min(1,((p[0]-a[0])*dx+(p[1]-a[1])*dz)/(dx*dx+dz*dz)));return[a[0]+dx*t,a[1]+dz*t]
def closest(p):return min((dist(p,project(p,*r['points'])),i,project(p,*r['points'])) for i,r in enumerate(roads))
nodes=[[key(p) for p in r['points']] for r in roads]
for i,a in enumerate(roads):
 for j,b in enumerate(roads):
  for p in a['points']:
   q=project(p,*b['points'])
   if dist(p,q)<.001:nodes[j].append(key(q))
  aa,ab=a['points'];ba,bb=b['points']
  if aa[1]==ab[1] and ba[0]==bb[0]:
   p=[ba[0],aa[1]]
   if dist(project(p,aa,ab),p)<.001 and dist(project(p,ba,bb),p)<.001:nodes[i].append(key(p));nodes[j].append(key(p))
def endpoint(name,ps):
 _,i,q=closest(ps[-1]);nodes[i].append(key(q));ends[name]=ps+[q] if dist(ps[-1],q)>.001 else ps
for b in B.values():
 for kind in ['front','rear','shortcut']:
  p=next((p for p in D['paths'] if p['owner']==b['id'] and p['kind']==kind),None)
  if p:endpoint(b['id']+':'+kind,p['points'] if kind=='front' else list(reversed(p['points'])))
endpoint('B01:front',[[0,-3],[6,-3],[6,20]])
for f in D['facilities']:endpoint(f['id'],[f['point']])
endpoint('chess-junction',[[94,-75]])
def add(a,b):
 a,b=key(a),key(b)
 if a!=b:G.setdefault(a,{})[b]=dist(a,b);G.setdefault(b,{})[a]=dist(a,b)
for i,ns in enumerate(nodes):
 ns=sorted(set(ns),key=lambda p:dist(roads[i]['points'][0],p))
 for p,q in zip(ns,ns[1:]):add(p,q)
def public(a,b):
 a,b=key(a),key(b);best={a:0};prev={};q=[(0,a)]
 while q:
  cost,u=heapq.heappop(q)
  if cost!=best[u]:continue
  if u==b:break
  for v,l in G[u].items():
   c=cost+l
   if c<best.get(v,1e9):best[v]=c;prev[v]=u;heapq.heappush(q,(c,v))
 if b not in best:raise ValueError((a,b))
 ps=[b]
 while ps[-1]!=a:ps.append(prev[ps[-1]])
 return list(reversed(ps))
def make(id,title,a=None,b=None,points=None,basis='路径为推演',when='',note=''):
 if points is None:points=ends[a]+public(ends[a][-1],ends[b][-1])[1:]+list(reversed(ends[b]))[1:]
 clean=[]
 for p in points:
  if not clean or dist(p,clean[-1])>.001:clean.append(p)
 length=sum(dist(p,q) for p,q in zip(clean,clean[1:]));r=dict(id=id,title=title,points=clean,metres=round(length,2),minutes=round(length/72,2),basis=basis,when=when,note=note);Rts.append(r);return r
hout=make('h-out','丈夫：东正门去车站','B12:front','B17:front',when='1/20 17:35左右',basis='事件明确；街巷为推演')
hback=make('h-back','丈夫：后巷返回东正门','B17:front','B12:shortcut',when='约17:55',basis='事件明确；入口为改图提案',note='只测试邻居指定观察窗的遮挡，不等于一路无人看见。')
hstop=make('h-stop','丈夫：厨房门离家去候车点','B12:rear','F02',when='约18:15',basis='离家事件明确；厨房门对应为提案')
hinn=make('h-inn','丈夫：候车点至旅馆','F02','B08:front',when='约18:35',note='步行作为可达性测算，不认定剧本中的交通方式。')
hreturn=make('h-return','丈夫：旅馆至住宅','B08:front','B12:front',when='20:40前抵家',basis='仅测返家距离；中途行程未知',note='不能将18:35—20:40全部填成在旅馆。')
haround=make('h-around','丈夫：试正门后绕到厨房门',points=next(p['points'] for p in D['paths'] if p['owner']=='B12' and p['kind']=='around'),when='20:40前后',basis='按剧本动作补画的院内通道')
witnessPoints=next(p['points'] for p in D['paths'] if p['kind']=='witness')
make('h-neighbor','丈夫：去请邻居',points=list(reversed(witnessPoints)),when='20:40前后',basis='院门提案；室外门到门')
make('n-witness','邻居夫妇：陪同到厨房门',points=witnessPoints,when='20:40前后',basis='陪同事件明确；院门为提案')
make('h19-station','19日：住宅至本镇车站（候选起点）','B12:front','B17:front',when='1/19；发车时刻未知',basis='真实乘车已确认；从住宅出发仅提案',note='本线到站即止；之后乘列车到另一镇问路，不能把本线分钟数当作全程。')
make('w-home','妻子：医院至住宅','B11:front','B12:front',when='1/20 下午至接奶前',basis='下午离院、傍晚在家明确；中途未知')
make('w-milk','妻子：屋内至东门接奶',points=[[-21.5,-57],[-16.5,-57]],when='约17:55／示例17:56',basis='事件明确；仅示意门内接近，室内未设计',note='这条线不作为室内导航已通过的证据。')
make('w-plan','妻子：原计划次晨离镇','B12:front','B17:front',when='1/21清晨原计划',basis='未实施计划',note='她已遇害，绝不能播放成实际走过的路线。')
make('m-depot','少年：奶站至前一签收户','B04:front','H66:front',when='1/20派送开始时刻未知',basis='前户选H66为设计提案')
mprev=make('m-before','少年：前户至主角家','H66:front','B12:front',when='签收窗口17:50—18:00',basis='派送顺序明确；两户位置为提案')
mnext=make('m-after','少年：主角家至后一签收户','B12:front','H37:front',when='签收窗口17:50—18:00',basis='后户选H37为设计提案')
# Coal vehicle has exactly one occurrence; axis route only, no swept-path certification.
make('coal','煤车：沿住宅街一次卸货',points=[[-165,-75],[-27,-75],[-27,-72.5]],when='1/20 会客室／离门事件附近',basis='单次卸货明确；进场方向为提案',note='没有标定分钟；车宽、停车占道与转弯仍须校核。')
make('borrow','丈夫：借梯后返家','B02:front','B12:front',when='案发前几日；无精确时刻',basis='借梯明确；携梯路线仅候选')
# Free investigation remains a separate, non-mandatory layer.
perms=[]
for order in itertools.permutations(['B02:front','B17:front','B12:front']):
 seq=['B01:front',*order,'B01:front'];length=0
 for a,b in zip(seq,seq[1:]):
  ps=ends[a]+public(ends[a][-1],ends[b][-1])[1:]+list(reversed(ends[b]))[1:];length+=sum(dist(p,q) for p,q in zip(ps,ps[1:]))
 perms.append(dict(order=order,metres=round(length,1),minutesGame=round(length/150,2)))
# First-hit 2D obstacle checks, with heights deliberately not certified.
def blockers(a,c,ignore=()):
 hits=set()
 for b in B.values():
  if b['id'] in ignore:continue
  for part in b['parts']:
   x,z,w,h=part['rect']
   for i in range(1,501):
    t=i/501;p=[a[0]+(c[0]-a[0])*t,a[1]+(c[1]-a[1])*t]
    if x-w/2+.03<p[0]<x+w/2-.03 and z-h/2+.03<p[1]<z+h/2-.03:hits.add(b['id']);break
 return sorted(hits)
sights=[]
for s in D['sightlines']:sights.append(dict(**s,blockers=blockers(s['a'],s['b'],('B01',) if s['id']=='S01' else ())))
shortcut=next(p['points'] for p in D['paths'] if p['kind']=='shortcut');blind=[]
for a,c in zip(shortcut,shortcut[1:]):
 for i in range(11):
  t=i/10;p=[a[0]+(c[0]-a[0])*t,a[1]+(c[1]-a[1])*t];blind.append(bool(blockers([-40.8,-57],p)))
errors=[]
for r in Rts:
 if r['id']=='w-milk':continue
 for a,c in zip(r['points'],r['points'][1:]):
  if blockers(a,c):errors.append(dict(route=r['id'],blockers=blockers(a,c)))
# All named/functional roles are represented, unknown paths stay unknown.
def actor(id,name,routeids,anchors,status,conclusion,unknown,source):
 Actors.append(dict(id=id,name=name,routes=routeids,anchors=anchors,status=status,conclusion=conclusion,unknown=unknown,source=source))
actor('husband','丈夫／原主',['h19-station','borrow','h-out','h-back','h-stop','h-inn','h-return','h-around','h-neighbor'],['B12','B17','B08'],'部分成立','19日真实乘车到另一镇问路，20日挪用前一天行程；20日本镇去站返家可达，但剩余时间不等于已证实停留。','19日列车班次、车程、返程及电话和棋社所属城镇未定；20日18:35至20:40行程仍缺失。','原稿第三节；第五节E/I/M')
actor('wife','妻子',['w-home','w-milk','w-plan'],['B11','B12'],'条件成立','医院回家可达；接奶改到东门，平面上看钟不穿房。1/21离镇只标为未实施计划。','下午离院的具体时刻、中间停靠、酒馆寄包由谁执行未明确；屋内回避丈夫/少年的调度未设计。','第三节；第五节G/L/M；第六节片段1')
actor('milkboy','送奶少年',['m-depot','m-before','m-after'],['B04','H66','B12','H37'],'条件成立','用西侧H66作为前签收户、H37作为后签收户，可把主角家夹在17:50—18:00之间；接奶点留出看钟通廊。','户主与具体签收分钟未定，交付停留为预算；需进一步验证钟面读时。','当前MVP“证词时刻”；原稿第三节与第五节C')
actor('neighbors','邻居夫妇',['n-witness'],['B05','B12'],'条件成立','东窗看得到西厨房门；看不到东正门的指定返家段。跨宅院门支持陪同进屋。','不能证明其他窗户、其他邻居或在街上时均看不到；20:40散步路线没有写明，未凭空补一圈。','第五节E；第七节邻居妻子')
actor('lover','情夫／修车匠',[],['B17'],'镇外待定','1/20实际在邻市凯恩车间，不能把他画在本镇修车铺；1/21清晨等候点在本镇车站。','邻市位置、来镇交通和时刻表缺失，无法用本图证明异地往返不可能；仅保留剧本记录的证言事实。','第三节；第五节H；第七节修车匠')
actor('clockmaker','钟表匠',[],['B02','B01'],'无需新增行程','保留钟表铺、维修簿和借梯记录。没有把“中午街钟被调快”误画成钟表匠去改钟。','钟表匠当天出门维修的时间未写。','第五节D；第七节钟表匠')
actor('shopkeeper','另一镇店主',[],[],'镇外未绘制','19日问路与搬椅子发生在另一镇；本镇B14杂货铺和B15教堂只是背景建筑。','另一镇不在本轮制图范围，不计算店铺至教堂距离。','用户2026-10-08纠正；原稿第七节')
actor('patrolman','另一镇巡警',[],[],'镇外未绘制','19日17:20问路原簿属于另一镇；本镇车站仅提供获准查阅的副本。','巡逻路线及棋社路口所属位置不在本图推定。','用户纠正；原稿第三、五节K')
actor('lampworker','另一镇修灯工',[],[],'镇外未绘制','19日坏灯、20日中午修复属于另一镇同一盏灯；已删除本镇F03误标。','另一镇维修通知副本如何移交为材料来源设计，不等于工人来本镇。','用户纠正；原稿第七节')
actor('coalworker','煤车与卸货员',['coal'],['F04','B12'],'车辆待验','只布置一次卸货事件，住宅南窗可朝向这段路；未复制成多次离屋。','进入方向是提案；车辆转弯、卸货占道和确切分钟尚未验证。','第六节片段4—6；当前MVP修订约束')
actor('ticketclerk','售票员',[],['B17'],'两日分开','19日丈夫真实乘车；20日本镇车站露面和未验车票不能证明同日跨镇。','若保留原稿售票员在另一镇问路现场相遇，须另交代其出行，不能画成本镇步行串联。','用户确认；原稿第五节B')
actor('innstaff','旅馆门房／伙计',[],['B08'],'定点记录','目击地点固定在旅馆，只证明18:35后短暂出现，不能覆盖整个傍晚。','换班及目击持续时间未写。','第五节I')
actor('barkeep','酒馆老板',['keeper-church'] if False else [],['B06'],'定点记录','只标酒馆与寄存记录；不能因登记名字把情夫或妻子强行放到案发当晚现场。','谁送包、何时送、是否委托他人尚需原账或作者确认。','第五节G；第七节酒馆老板')
actor('nurse','护士／缴费人员',[],['B11'],'定点记录','医院内提供下午离院与家庭背景记录；没有规定离岗路线。','护士的通勤与院内时间表未写，不影响当前主线定位。','第五节L；第七节病房护士')
actor('father','妻子父亲',[],['B16'],'位置待定','B16仍只是会面地点候选，不把它当作剧本确认的住所。','父亲当晚的所在地和出行完全未写，不能证明其不在场。','第七节妻子父亲；原稿无地址')
actor('clients','邻市顾客甲／乙',[],[],'镇外待定','甲约18:00取车、乙19:00前付账发生在邻市车间；本镇地图不画伪造路线。','需要邻市车间、旅店和班车的独立行程表才能校核连续证言。','第七节邻市车间客户')
actor('postclerk','邮局柜员／电话记录人员',[],['B03','F01'],'定点记录','邮局与本镇电话亭为候选设施；19日通话所属城镇未确认，不播放电话亭至棋社的历史路线。','未给工作人员编通勤或巡视路线。','第五节F')
actor('pawnbroker','当铺人员',[],['B09'],'定点记录','保留账本地点；不将每笔账自动解释成妻子当时亲自到场。','取款经手人和具体时间有待账本内容确认。','第五节J')
actor('companion','同行者／旧稿律师',[],['B01'],'自由调查','只在当前调查提供可选陪伴，不进入1931年案发回溯，不作为开门或路线解锁条件。','自由探索没有固定访问顺序。','当前MVP优先于旧稿第四节')
actor('others','其他居民／棋社负责人／警方资料人员',[],['H66','H37','B13','B10'],'定点或待定','签收户和案卷来源有候选位置；棋社所属城镇待确认，其余居民不虚构同晚行程。','需新增角色出场时再补班次和移动，不以背景人物数量制造证人。','第五节及当前MVP')
summary=dict(roundTripMetres=round(hout['metres']+hback['metres'],2),roundTripWalkMinutes=round((hout['metres']+hback['metres'])/72,2),unallocatedWithin20=round(20-(hout['metres']+hback['metres'])/72,2),eveningToInnWalkMinutes=round(hstop['minutes']+hinn['minutes'],2),eveningWaitBudget=round(20-hstop['minutes']-hinn['minutes'],2),milkWalkMinutes=round(mprev['minutes']+mnext['minutes'],2),milkServiceBudgetWithin10=round(10-mprev['minutes']-mnext['minutes'],2),homeAroundMetres=haround['metres'],investigationGameMinutes=[min(p['minutesGame'] for p in perms),max(p['minutesGame'] for p in perms)])
result=dict(version=4,offMapJourneys=D['offMapJourneys'],actors=Actors,routes=Rts,summary=summary,sights=sights,finalReturnSamples=dict(total=len(blind),blocked=sum(blind),scope='指定邻居东窗至北后巷入口后的最后一段，二维建筑遮挡'),geometryErrors=errors,permutations=perms,assumptions=['1.2m/s用于剧情可达性，2.5m/s用于当前游戏纯步行预算','行走采用公共道路中心线抽象与对应宅地入口，不能穿他人宅地抄近路','室内移动、车辆转弯、人流、门锁状态和完整视锥未模拟','设施与两签收户位置为本版提案；未知行程不补成事实'],source='用户2026-10-08确认19日真实乘车去另一镇、20日挪用；原稿第三、五、六、七节；当前MVP')
(R/'actor-audit.json').write_text(json.dumps(result,ensure_ascii=False,indent=2));(R/'actor-audit.js').write_text('window.AUDIT='+json.dumps(result,ensure_ascii=False)+';')
with (R/'actor-routes.csv').open('w',encoding='utf-8-sig',newline='') as f:
 w=csv.writer(f,lineterminator="\n");w.writerow(['角色路线','时刻','米','1.2m/s分钟','依据','限制']);w.writerows([[r['title'],r['when'],r['metres'],r['minutes'],r['basis'],r['note']] for r in Rts])
print(json.dumps(dict(summary=summary,sights=sights,blind=result['finalReturnSamples'],errors=errors,roles=len(Actors),routes=len(Rts)),ensure_ascii=False,indent=2))
