from pathlib import Path
import json
R=Path(__file__).resolve().parent
A=json.loads((R/'actor-audit.json').read_text()); S=A['summary']
svg=(R/'town-plan.svg').read_text().replace('id="town-svg"','id="actor-map"')
rows=''.join(f'<tr><td>{a["name"]}</td><td>{a["status"]}</td><td>{a["conclusion"]}</td><td>{a["unknown"]}<small>{a["source"]}</small></td></tr>' for a in A['actors'])
page=f'''<!doctype html><html lang="zh-CN"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>青川镇 · 跨镇纠正与角色核对 V4</title><link rel="stylesheet" href="style.css"><link rel="stylesheet" href="actors.css"><body>
<header><div><span class="eyebrow">CHARACTER × TIME × SPACE / 04</span><h1>两天的行程，不能画成同一条路</h1><p>{len(A['actors'])}组角色 · {len(A['routes'])}段本镇路线 · 另一镇仅保留交通连接，不绘制地图</p></div><a href="index.html">查看本镇平面图 ↗</a></header>
<main><section class="report"><div class="changes"><p><b>1月19日 · 实际行程</b><br>本镇车站 → 乘列车 → 另一镇 → 17:20问路。真实乘车已经用户确认；发车、车程和返程时刻未定。</p><p><b>1月20日 · 陈述与实际分开</b><br>他把19日的跨镇问路挪进20日陈述。案发日本镇去站露面、返家、送奶等另外核对，不播放成当天实际跨镇。</p><p><b>另一镇 · 本轮不制图</b><br>问路点、坏灯、店主与相关巡警事件从本镇撤出。另一镇的店铺与教堂不对应本镇B14／B15。</p><p><b>证据 · 分清发生地和查阅地</b><br>本镇可提供另一镇巡警原簿及修灯通知的副本。20日未验车票不否定19日真实乘车，也不能独自证明20日未乘任何交通。</p></div></section>
<div class="actor-toolbar"><div><button id="town">完整本镇</button><button id="home">住宅关键关系</button><button id="together">同屏：20日17:53—17:58</button></div><span>作者审阅 · 含剧情真相 · 可达不等于不在场证明</span></div>
<div class="actor-layout"><div><div class="actor-map">{svg}</div><div class="legend"><span>━━ 当前角色路线</span><span>┄┄ 未实施计划</span><span>┄ 接奶看钟／邻居观察窗</span></div><div id="scene-caption" class="verdict"></div></div><aside><label class="search">角色<select id="actor"></select></label><div id="actor-info"></div><div id="route-buttons"></div><div id="route-info"></div><div class="controls"><button id="play">播放选中路线</button><button id="rewind">重置</button><input id="scrub" type="range" min="0" max="1000" value="0"><div id="clock"></div></div></aside></div>
<section class="report"><h2>本镇重新演算</h2><p>车站恢复东缘主站房中心 (188,46)，正门 (188,40)。不再用人为加长道路解释跨镇乘车。</p>
<table><thead><tr><th>片段</th><th>本版结果</th><th>结论与限制</th></tr></thead><tbody>
<tr><td>20日17:35离家、约17:55返家</td><td>本镇往返 {S['roundTripMetres']}m；纯步行 {S['roundTripWalkMinutes']}分钟；20分钟中尚有 {S['unallocatedWithin20']}分钟未分配</td><td>距离允许返家，但不能把全部余量写成真实车站停留；去站与返家中途时间还须作者具体安排。</td></tr>
<tr><td>妻子接奶／两户签收</td><td>前户→主角家→后户纯步行 {S['milkWalkMinutes']}分钟；10分钟窗口剩余 {S['milkServiceBudgetWithin10']}分钟</td><td>余量需包含签收、送奶与其他停留；17:56接奶只是动画先后示例，前后户位置是提案。</td></tr>
<tr><td>18:15厨房门→候车点→18:35旅馆</td><td>纯步行 {S['eveningToInnWalkMinutes']}分钟；未分配 {S['eveningWaitBudget']}分钟</td><td>仅测本镇可达性，不认定实际全程步行或等车时长。18:35后至20:40仍是行程空白。</td></tr>
<tr><td>20:40试门、请邻居</td><td>绕屋 {S['homeAroundMetres']}m；跨宅陪同路径25m</td><td>两门与院门是空间提案；试锁、交谈、室内时间没有模拟。</td></tr>
<tr><td>自由探索三处室内</td><td>6种顺序的纯室外步行 {S['investigationGameMinutes'][0]}–{S['investigationGameMinutes'][1]}分钟（2.5m/s）</td><td>不规定访问顺序；阅读、调查、进屋和重复跑图另计，不代表实机完成时长。</td></tr></tbody></table>
<h2>逐角色核对</h2><p>只标证言地点的角色没有虚构通勤。另一镇角色不显示本镇锚点，也不播放假路线。</p><table><thead><tr><th>角色</th><th>状态</th><th>能说明什么</th><th>仍缺什么／出处</th></tr></thead><tbody>{rows}</tbody></table>
<h2>当前结论：本镇平面可用作制作基准，剧情仍有待定段</h2><p>两宅门窗关系、步行连通与送奶窗口已做平面核对。另一镇时刻表、19日电话和棋社所属城镇、丈夫20日晚间空白、情夫异地旅行时间仍未知。没有绘制另一镇，也没有凭空补完这些事实。</p><p>本轮未做Unity验收、完整室内路线、视线高度与钟面可读性测试、车辆转弯与占道检查；中心线路径不等于全宽通道已经通过。</p>
<p><a href="actor-audit.json">核对数据</a> · <a href="actor-routes.csv">路线距离</a> · <a href="town-plan.svg">本镇坐标图</a> · <a href="home-relations.svg">两宅详图</a> · <a href="checks.json">几何检查</a></p></section></main><script src="plan-data.js"></script><script src="actor-audit.js"></script><script src="actors.js"></script></body></html>'''
(R/'actors.html').write_text(page)
(R/'home-relations.svg').write_text((R/'town-plan.svg').read_text().replace('viewBox="0 0 2250 1630"','viewBox="625 730 430 430"'))
