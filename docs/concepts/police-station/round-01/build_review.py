"""Build an offline police-station concept review from editable design data."""
import html
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parent
DATA = json.loads((ROOT / 'design-plan.json').read_text(encoding='utf-8-sig'))
COLORS = {'public': '#d5c49b', 'staff': '#aebaa9', 'archive': '#aebfc5',
          'holding': '#c3aead', 'service': '#c1c1b8', 'stairs': '#bfaa80'}
ESC = html.escape


def inside(rect, shell):
    x, y, w, d = rect
    a, b, c, e = shell
    return x >= a-0.001 and y >= b-0.001 and x+w <= a+c+0.001 and y+d <= b+e+0.001


def overlap(a, b):
    return min(a[0]+a[2], b[0]+b[2])-max(a[0], b[0]) > .001 and min(a[1]+a[3], b[1]+b[3])-max(a[1], b[1]) > .001


def validate():
    results = []
    for s in DATA['schemes']:
        for p in s['parts']:
            assert inside(p['rect'], [0, -.7, 23, 39]), (s['id'], 'plot', p)
        for level in (0, 1):
            f = s[f'floor{level}']
            shells = [p['rect'] for p in s['parts'] if p['storeys'] > level]
            for r in f['rooms']:
                assert any(inside(r['rect'], p) for p in shells), (s['id'], level, r['id'], 'outside floor')
            for i, r in enumerate(f['rooms']):
                for t in f['rooms'][i+1:]:
                    assert not overlap(r['rect'], t['rect']), (s['id'], level, r['id'], t['id'], 'overlap')
                for t in f['circulation']:
                    assert not overlap(r['rect'], t['rect']), (s['id'], level, r['id'], 'circulation overlap')
            stair = next(r['rect'] for r in f['rooms'] if r['id'] == 'stairs')
            if level == 0:
                lower = stair
            else:
                assert lower == stair, (s['id'], 'stairs differ')
        assert s['frontDoor'] == [11.5, 0] and s['rearDoor'] == [7.36, 12]
        results.append({'scheme': s['id'], 'within_23x39_plot': True,
                        'room_rectangles_do_not_overlap': True, 'stairs_align': True,
                        'enclosed_ground_area_m2': s['groundArea'],
                        'enclosed_floor_area_m2': s['grossArea']})
    return results


def text_lines(label, max_chars):
    return [label[i:i+max_chars] for i in range(0, len(label), max_chars)]


def svg_plan(s):
    out = ['<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 1080 900" role="img" aria-label="' + ESC(s['name']) + ' 两层功能平面">',
           '<rect width="1080" height="900" fill="#ece6d8"/>',
           '<style>text{font-family:Microsoft YaHei,PingFang SC,Arial,sans-serif;fill:#303a37}.room{cursor:pointer}.room:hover rect{stroke:#406c65;stroke-width:3}.room text{pointer-events:none}</style>',
           f'<defs><marker id="arrow-{s["id"]}" markerWidth="6" markerHeight="6" refX="5" refY="3" orient="auto"><path d="M0 0L6 3L0 6Z" fill="#477f73"/></marker></defs>',
           f'<text x="45" y="36" font-size="24" font-weight="bold">{s["id"]}　{ESC(s["name"])}　功能分区图</text>',
           '<text x="45" y="59" font-size="13">北向上 · 单位米 · 色块为功能分区 · 门洞、墙厚与家具在选案后深化</text>']
    scale = 18
    for level, ox, oy in [(0, 65, 780), (1, 585, 320)]:
        f = s[f'floor{level}']
        def pt(x, y):
            return ox+x*scale, oy-y*scale
        def box(rect, fill, stroke='#667169', width=1, dash=''):
            x, y, w, d = rect
            px, py = pt(x, y+d)
            out.append(f'<rect x="{px:.2f}" y="{py:.2f}" width="{w*scale:.2f}" height="{d*scale:.2f}" fill="{fill}" stroke="{stroke}" stroke-width="{width}" {dash}/>')
        if level == 0:
            box([0,-.7,23,39], '#e3dfd0', '#7d857a', 1, 'stroke-dasharray="6 5"')
            out.append('<text x="270" y="107" text-anchor="middle" font-size="15">北后巷／后院门</text>')
            out.append('<text x="266" y="805" text-anchor="middle" font-size="15">南侧车站路／公共前门</text>')
            out.append('<text x="45" y="440" transform="rotate(-90 45 440)" text-anchor="middle" font-size="14">宅地深 39m</text>')
            out.append('<text x="270" y="831" text-anchor="middle" font-size="14">宅地宽 23m</text>')
            out.append('<text x="65" y="87" font-size="18" font-weight="bold">一层与院落</text>')
            out.append('<text x="266" y="250" text-anchor="middle" font-size="14">后勤院／通往北门</text>')
        else:
            out.append('<text x="585" y="87" font-size="18" font-weight="bold">二层</text>')
        for p in s['parts']:
            if p['storeys'] > level:
                box(p['rect'], '#f5f0e4', '#46534c', 3)
        for t in f['circulation']:
            box(t['rect'], '#e2dfcf', 'none', 0)
        for r in f['rooms']:
            out.append(f'<g class="room" data-scheme="{s["id"]}" data-level="{level}" data-room="{r["id"]}"><title>{ESC(r["name"]+"："+r["detail"])}</title>')
            box(r['rect'], COLORS[r['type']])
            x,y,w,d = r['rect']
            cx, cy = pt(x+w/2, y+d/2)
            lines = text_lines(r['name'], max(2, int(w*scale/14)))
            for j,line in enumerate(lines):
                out.append(f'<text x="{cx:.2f}" y="{cy+(j-(len(lines)-1)/2)*16:.2f}" text-anchor="middle" dominant-baseline="middle" font-size="13">{ESC(line)}</text>')
            # Door concept at the first directly adjoining public circulation zone.
            for t in f['circulation']:
                a,b,c,d2 = t['rect']
                lo,hi = max(x,a), min(x+w,a+c)
                left,right = max(y,b), min(y+d,b+d2)
                if hi-lo > 1.05 and (abs(y+d-b)<.01 or abs(y-(b+d2))<.01):
                    edge = y+d if abs(y+d-b)<.01 else y
                    q1,q2 = pt((lo+hi)/2-.525,edge),pt((lo+hi)/2+.525,edge)
                elif right-left > 1.05 and (abs(x+w-a)<.51 or abs(x-(a+c))<.51):
                    edge = x+w if abs(x+w-a)<.51 else x
                    q1,q2 = pt(edge,(left+right)/2-.525),pt(edge,(left+right)/2+.525)
                else:
                    continue
                out.append(f'<path d="M{q1[0]:.2f} {q1[1]:.2f}L{q2[0]:.2f} {q2[1]:.2f}" stroke="#f5f0e4" stroke-width="5"/>')
            out.append('</g>')
        if level == 0:
            outer_doors = [s['frontDoor'],s['rearDoor']]
            if s['id'] in ('A','B','C'):
                outer_doors.append([16.81,12])
            if s['id'] == 'E':
                outer_doors.append([15.65,12])
            for x,y in outer_doors:
                q1,q2=pt(x-.7,y),pt(x+.7,y)
                out.append(f'<path d="M{q1[0]} {q1[1]}L{q2[0]} {q2[1]}" stroke="#f5f0e4" stroke-width="7"/>')
            if s['id']=='D':
                for x,ys in [(5.5,[15.2,20.8]),(17.5,[14.2,17.8,21.6])]:
                    for y in ys:
                        q1,q2=pt(x,y-.525),pt(x,y+.525)
                        out.append(f'<path d="M{q1[0]} {q1[1]}L{q2[0]} {q2[1]}" stroke="#f5f0e4" stroke-width="7"/>')
        # Keep the drawn browsing path aligned with door concepts at room edges.
        for u,v in zip(f['route'],f['route'][1:]):
            for r in f['rooms']:
                x,y,w,d=r['rect']
                for axis,edges,span in [(0,[x,x+w],[y,y+d]),(1,[y,y+d],[x,x+w])]:
                    delta=v[axis]-u[axis]
                    if abs(delta)<.0001:
                        continue
                    for edge in edges:
                        t=(edge-u[axis])/delta
                        along=u[1-axis]+t*(v[1-axis]-u[1-axis])
                        if 0<t<1 and span[0]+.525<along<span[1]-.525:
                            if axis==0:
                                q1,q2=pt(edge,along-.525),pt(edge,along+.525)
                            else:
                                q1,q2=pt(along-.525,edge),pt(along+.525,edge)
                            out.append(f'<path d="M{q1[0]} {q1[1]}L{q2[0]} {q2[1]}" stroke="#f5f0e4" stroke-width="5"/>')
        route = ' '.join(f'{pt(x,y)[0]:.2f},{pt(x,y)[1]:.2f}' for x,y in f['route'])
        out.append(f'<polyline class="flow-line" points="{route}" fill="none" stroke="#477f73" stroke-width="2.5" stroke-dasharray="7 5" marker-end="url(#arrow-{s["id"]})"/>')
    out.extend([f'<text x="585" y="377" font-size="16">封闭首层占地约 {s["groundArea"]} m²</text>',
                f'<text x="585" y="403" font-size="16">两层封闭楼面约 {s["grossArea"]} m²</text>',
                '<text x="585" y="447" font-size="14">建议层高：首层 3.2m，二层 3.0m</text>',
                '<text x="585" y="473" font-size="14">虚线为浏览方向，不是强制任务路线</text>',
                '<text x="585" y="499" font-size="14">二层仅在前方主体内；后翼为一层</text>'])
    if s['id']=='D':
        out.append('<text x="585" y="530" font-size="14">两条回廊另计约 36m²，露天净区约 9×12m</text>')
    for i,(key,name) in enumerate([('public','公共查阅／接待'),('staff','办公／值班'),('archive','档案／保管'),('holding','旧羁押空间'),('service','盥洗／储藏'),('stairs','楼梯')]):
        yy=580+i*31
        out.append(f'<rect x="585" y="{yy-13}" width="20" height="20" fill="{COLORS[key]}"/><text x="617" y="{yy+2}" font-size="14">{name}</text>')
    out.append('<text x="65" y="870" font-size="13">平面是概念功能图，尚非碰撞、门窗、通道或结构验收；外观板局部透视与房位以后续共享布局为准。</text></svg>')
    return ''.join(out)


def paragraphs(items):
    return ''.join('<p>'+ESC(v)+'</p>' for v in items)


def create_html(plans):
    selected=DATA.get('selected_scheme','A')
    nav=''.join(f'<button class="select" data-id="{s["id"]}" aria-pressed="{str(s["id"]==selected).lower()}">{s["id"]} {ESC(s["name"])}</button>' for s in DATA['schemes'])
    sections=[]
    for s,plan in zip(DATA['schemes'],plans):
        rows=[]
        for level in (0,1):
            for r in s[f'floor{level}']['rooms']:
                w,d=r['rect'][2:]
                rows.append(f'<tr><td>{"一层" if level==0 else "二层"}</td><td>{ESC(r["name"])}</td><td>{w:g}×{d:g}m</td><td>{ESC(r["detail"])}</td></tr>')
        palette=''.join(f'<span class="swatch"><i style="background:{color}"></i>{ESC(name)}</span>' for name,color in s['palette'])
        hidden='' if s['id']==selected else ' hidden'
        sections.append(f'''<article id="scheme-{s['id']}"{hidden}>
<div class="scheme-heading"><span>方案 {s['id']}</span><h2>{ESC(s['name'])}</h2><p>{ESC(s['headline'])}</p></div>
<img class="hero" src="{s['image']}" alt="{ESC(s['name'])} 外观、阅览室、两层空间与道具设计板" width="1536" height="1024">
<div class="summary"><p><b>建筑体量</b>　{ESC(s['mass'])}</p><p><b>适合的体验</b>　{ESC(s['fit'])}</p><p><b>空间叙事</b>　{ESC(s['narrative'])}</p><p><b>第一人称视线</b>　{ESC(s['view'])}</p><p><b>后院与后门</b>　{ESC(s['yard'])}</p><p><b>声音</b>　{ESC(s['sound'])}</p><p><b>取舍</b>　{ESC(s['risk'])}</p><div class="palette">{palette}</div></div>
<h3>两层布局与浏览动线</h3><p>{ESC(s['flow'])}</p>
<label class="route-control"><input type="checkbox" class="show-flow" checked>显示建议浏览方向</label>
<div class="plan-wrap">{plan}</div><p class="room-detail" aria-live="polite">点击平面色块查看房间用途。</p>
<details open><summary>每个房间的尺寸和陈设</summary><div class="table-wrap"><table><thead><tr><th>楼层</th><th>房间</th><th>净区估算</th><th>陈设与用途</th></tr></thead><tbody>{''.join(rows)}</tbody></table></div></details>
<p class="note">外观板用于选择建筑气质。中文平面给出功能组织；精确门窗、墙厚、家具净距与楼梯仍需同源建模。院落浏览可以反向进行。</p>
</article>''')
    commons=DATA['common']
    interactions=''.join(f'<tr><td>{ESC(t["object"])}</td><td>{ESC(t["action"])}</td><td>{ESC(t["saved"])}</td></tr>' for t in commons['interaction'])
    refs=''.join(f'<li><a href="{ESC(r["url"])}" target="_blank" rel="noreferrer">{ESC(r["title"])}</a><p>{ESC(r["use"])}</p></li>' for r in DATA['referenceSources'])
    source=json.dumps(DATA,ensure_ascii=False).replace('</','<'+chr(92)+'/')
    return '''<!doctype html><html lang="zh-CN"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width, initial-scale=1"><title>错日证词 警察局五种设计方案</title><style>
*{box-sizing:border-box}body{margin:0;background:#e9e5dc;color:#2e3935;font-family:"Microsoft YaHei","PingFang SC",system-ui,sans-serif;line-height:1.7}header{background:#303e39;color:#f2edde;padding:34px max(20px,calc((100vw - 1160px)/2)) 25px}h1{font-size:30px;line-height:1.3;margin:6px 0 13px}header p{max-width:940px;margin:8px 0}.eyebrow{font-size:13px;letter-spacing:.14em;color:#d3be92}main{max-width:1200px;margin:0 auto;padding:24px 20px 55px}nav{display:flex;gap:9px;flex-wrap:wrap;margin:4px 0 28px}button{font:inherit;cursor:pointer;border:1px solid #8e988c;padding:9px 13px;border-radius:4px;background:#f5f1e7;color:#34463e}button[aria-pressed=true]{background:#3e5a4c;color:#fff9e9;border-color:#3e5a4c}button:focus-visible{outline:3px solid #9c7742;outline-offset:3px}article{border-top:1px solid #adb1a2;padding-top:20px}.scheme-heading span{font-size:14px;color:#716549}.scheme-heading h2{font-size:28px;margin:0}.scheme-heading p{margin:5px 0 18px;font-size:17px}.hero{display:block;width:100%;height:auto;background:#d2c7b4;border-radius:3px}.summary{margin:20px 0 28px;columns:2;column-gap:38px}.summary p{break-inside:avoid;margin:0 0 14px}.palette{break-inside:avoid;display:flex;flex-wrap:wrap;gap:12px}.swatch{display:inline-flex;align-items:center;gap:7px;font-size:13px}.swatch i{display:inline-block;width:18px;height:18px;border-radius:50%}h3{font-size:21px;margin:29px 0 10px}p{margin:9px 0 15px}.plan-wrap{max-width:1080px;margin:10px auto}.plan-wrap svg{display:block;width:100%;height:auto}.route-control{display:block;font-size:14px}.route-control input{margin-right:8px}.room-detail{padding:12px 15px;min-height:52px;background:#dbe1d5;color:#32443b}.table-wrap{overflow-x:auto}table{width:100%;border-collapse:collapse;font-size:14px;background:#f0ede4}th{text-align:left;background:#d8ddcf;font-weight:600}td,th{padding:11px 13px;border-bottom:1px solid #c2c7b8;vertical-align:top}td:nth-child(1){white-space:nowrap}td:nth-child(3){white-space:nowrap}details{margin:17px 0;border-top:1px solid #afb5a6;padding-top:14px}summary{cursor:pointer;font-size:18px;font-weight:600;margin-bottom:13px}.note{font-size:13px;color:#5d665f}.shared{margin-top:35px;padding-top:23px;border-top:2px solid #7d8d7c}.shared h2{font-size:25px;margin:0 0 10px}.shared details p{max-width:980px}a{color:#386352}ul{padding-left:22px}.source{font-size:13px;color:#626e63}footer{margin-top:30px;font-size:13px;color:#657061}article[hidden]{display:none}@media(max-width:680px){h1{font-size:24px}.scheme-heading h2{font-size:24px}.summary{columns:1}main{padding:20px 14px 40px}header{padding:26px 18px}nav{gap:7px}button{font-size:14px;padding:8px 10px}td,th{padding:9px;font-size:13px}.plan-wrap{overflow-x:auto}.plan-wrap svg{min-width:840px}}
</style></head><body><header><div class="eyebrow">错日证词 · 一九三一 · 警署概念设计</div><h1>警察局的五种完整方案</h1><p>围绕短暂羁押、案卷查阅、摘要与原记录对照设计。五案保留公共阅览、问询、物证保管、值班、旧羁押室和后勤；核心调查自由可达。</p><p>''' + ESC(DATA['status']) + '''。用户已选 D「内院回廊警署」，暂不开始制作；其余四案保留为历史备选。</p></header><main><nav aria-label="选择建筑方案">''' + nav + '</nav>' + ''.join(sections) + '''<section class="shared"><h2>五案共用的剧情和交互设计</h2><details open><summary>案卷怎样支持玩家调查</summary>''' + paragraphs(DATA['basis']['sourceBoundaries']) + paragraphs(commons['desks']) + '''</details><details><summary>实体门 纸面 灯具和阶段报告</summary><div class="table-wrap"><table><thead><tr><th>物件</th><th>动作</th><th>状态与边界</th></tr></thead><tbody>''' + interactions + '''</tbody></table></div></details><details><summary>人物岗位与空间通行</summary>''' + paragraphs([commons['access'],commons['dimensions']]) + paragraphs([t['role']+'：'+t['at']+'。'+t['purpose']+'。'+t['limit'] for t in commons['npcSlots']]) + '''</details><details><summary>光照 雨声和模型制作</summary>''' + paragraphs([commons['environment'],commons['models'],commons['production'],'主要道具：'+'、'.join(commons['props'])+'。']) + '''</details><details><summary>后续建模的检查要求</summary>''' + paragraphs(commons['acceptance']) + '''</details><details><summary>现有地块与方案取舍</summary><p>B10 宅地为 23×39m，世界中心 X/Z=(12.5,46.5)。前门保持 (12.5,27.7)，后门保持 (8.36,39.7)，北院门 (2.2,66)。A／B／C 沿用主楼和 L 形后翼轮廓；D 改成 U 形两翼；E 缩为 18m 宽旧宅。后两项需选案后同步改图与角色路线核对。</p><p>坐标规则：功能图 x 从宅地西边 X=1 起算，y 从南立面 Z=27.7 向北。真实坐标 X=1+x、Z=27.7+y。封闭楼面面积只计房屋，不把院落计入；D 的两条回廊另列 36m²。</p></details><details><summary>图像与布局的适用范围</summary>''' + paragraphs(DATA['limitations']) + '''</details><details><summary>剧本和年代结构参考</summary><p class="source">原稿：错日证词_修订稿.docx 第1节、第4节、第5节地点K、第7节邻市客户、第9节。现行 MVP、证据合同和 B10 地块优先。</p><ul>''' + refs + '''</ul><p class="source">年代资料只用于建筑材料与空间先例；故事国家与现实司法程序不由这些资料确定。人物、保释与案件规则按虚构剧本处理。</p></details></section><footer>2026-10-11 · 内置 image_gen 生成五张图 · 完整提示词 generation-prompts.json · 可编辑布局 design-plan.json · 未制作游戏模型或提交推送</footer></main><script type="application/json" id="plan-data">''' + source + '''</script><script>
const data=JSON.parse(document.getElementById('plan-data').textContent);
document.querySelectorAll('button.select').forEach(button=>button.addEventListener('click',()=>{const id=button.dataset.id;document.querySelectorAll('article').forEach(a=>a.hidden=a.id!=='scheme-'+id);document.querySelectorAll('button.select').forEach(b=>b.setAttribute('aria-pressed',String(b===button)));}));
document.querySelectorAll('.show-flow').forEach(input=>input.addEventListener('change',()=>{input.closest('article').querySelectorAll('.flow-line').forEach(line=>line.style.display=input.checked?'':'none');}));
document.querySelectorAll('g.room').forEach(room=>room.addEventListener('click',()=>{const s=data.schemes.find(s=>s.id===room.dataset.scheme);const r=s['floor'+room.dataset.level].rooms.find(r=>r.id===room.dataset.room);room.closest('article').querySelector('.room-detail').textContent=(room.dataset.level==='0'?'一层':'二层')+' · '+r.name+' · '+r.rect[2]+'×'+r.rect[3]+'m。'+r.detail;}));
window.policeDesignReady=true;
</script></body></html>'''


if __name__ == '__main__':
    checks=validate()
    plans=[]
    for s in DATA['schemes']:
        plan=svg_plan(s)
        plans.append(plan)
        (ROOT / f'{s["id"].lower()}-floor-plan.svg').write_text(plan,encoding='utf-8')
    (ROOT / 'index.html').write_text(create_html(plans),encoding='utf-8')
    (ROOT / 'layout-checks.json').write_text(json.dumps({'status':'CONCEPT_LAYOUT_CHECK_PASS','schemes':checks,
        'scope':'Only proposed plot containment, room rectangles and matching stair footprints. Not Unity navigation, collision, structural, door/window or gameplay acceptance.'},ensure_ascii=False,indent=2),encoding='utf-8')
    print(json.dumps({'status':'CONCEPT_LAYOUT_CHECK_PASS','schemes':len(checks),'review':str(ROOT/'index.html')},ensure_ascii=False))
