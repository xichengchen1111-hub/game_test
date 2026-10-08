# Canonical map revision: user-confirmed inter-town geography, 2026-10-08.
byid={b['id']:b for b in B};home=byid['B12'];neighbor=byid['B05'];station=byid['B17']
home.update(front='E',door=[-16.5,-57],doorFacing='E',rearDoor=[-32.5,-57],rearFacing='W',gate=[-14.5,-68],backgate=[-35.5,-34],frontSetback=4.5,type='L形住宅 · 东正门／西厨房门',note='东正门接奶并供北后巷返家；西厨房门面向邻居观察窗。南侧补绕屋小径。两门与室内用途仍是待确认的改图提案。')
home['windows']=[[-29.3,-63,1.4,'会客室南窗','S'],[-19.7,-63,1.4,'会客室南窗','S']]
neighbor['windows'].append([-41,-57,1.5,'东侧观察窗','E']);neighbor['note']='新增东侧观察窗，正对主角家西厨房门；两宅之间这一段用0.85m低栏。'
Paths[:]=[p for p in Paths if p['owner']!='B12']
Paths.extend([
 dict(owner='B12',kind='front',width=1.4,points=[[-16.5,-57],[-14.5,-57],[-14.5,-68],[-14.5,-69]]),
 dict(owner='B12',kind='rear',width=1.4,points=[[-35.5,-31.5],[-35.5,-34],[-35.5,-57],[-32.5,-57]]),
 dict(owner='B12',kind='shortcut',width=1.4,points=[[-13.2,-31.5],[-13.2,-34],[-14.5,-36],[-14.5,-57],[-16.5,-57]]),
 dict(owner='B12',kind='around',width=1.4,points=[[-16.5,-57],[-14.5,-57],[-14.5,-65],[-34.5,-65],[-34.5,-57],[-32.5,-57]])])
# Station stays at the compact town edge; inter-town travel is not walking.
station['note']='本镇车站；主站房中心(188,46)，正门(188,40)。1月19日乘车去另一镇；跨镇车程与班次未定。'
# Remove tree crowns from the proposed milk-to-clock sight corridor and new door paths.
milk=[-15.5,-57];clock=[0,0]
Trees[:]=[t for t in Trees if segdist([t['x'],t['z']],milk,clock)>t['r']+.5 and all(segdist([t['x'],t['z']],a,b)>p['width']/2+.6 for p in Paths for a,b in zip(p['points'],p['points'][1:]))]
# Map facilities are proposals, anchored explicitly for this review.
Facilities=[dict(id='F01',name='公共电话亭',point=[10,12],width=2,depth=2,status='本版新定位置'),dict(id='F02',name='镇外候车点',point=[229,26],width=3,depth=2,status='本版新定位置；班车时刻待定'),dict(id='F04',name='煤车唯一卸货位置',point=[-27,-72.5],width=7,depth=2,status='单次事件；车辆尺寸与转弯未验收')]
Sightlines=[dict(id='S01',name='接奶点看钟',a=milk,b=clock,status='平面通廊检查'),dict(id='S02',name='邻居看西厨房门',a=[-40.8,-57],b=[-33,-57],status='两宅间低栏0.85m'),dict(id='S03',name='东门返家被自宅遮挡',a=[-40.8,-57],b=[-15.5,-57],status='遮挡目标；不是全镇无人目击')]
# A gate used only when the neighbours accompany the husband; not public circulation.
Paths.append(dict(owner='B05',kind='witness',width=1.2,points=[[-47.5,-63],[-47.5,-65],[-37,-65],[-34.5,-65],[-34.5,-57],[-32.5,-57]]))
Facilities.append(dict(id='F05',name='两宅陪同院门',point=[-37,-65],width=.4,depth=1.2,status='本版提案；邻居陪同通行，非公共近路'))

byid['B16']['name']='父亲会面点（候选）';byid['B16']['note']='原剧本没有确认父亲住址；这里仅是会面候选，不形成不在场证据。'

# Keep local building IDs as background proposals, never impersonate off-map witnesses.
for id,name in [('B14','街角杂货铺（背景）'),('B15','本镇教堂（背景）')]:
 byid[id].update(name=name,status='环境提案；非另一镇问路场景',note='本镇背景建筑，不是1月19日问路、坏灯或店主搬椅子发生地。')
for id in ['B13']:
 byid[id]['status']='位置提案；剧情所属城镇待确认'
 byid[id]['note']='尚未确认预约信中的棋社属于本镇还是另一镇；不得把本镇候选位置当作19日实际路线。'
Facilities[0]['status']='本镇设施提案；1月19日通话发生的城镇未确认，不绑定历史通话'
