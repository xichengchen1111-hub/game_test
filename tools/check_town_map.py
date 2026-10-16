"""Regenerate the canonical 2D map and reject stale outputs or broken narrative routes."""
import argparse, hashlib, json, shutil, subprocess, sys, tempfile
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
MAP=ROOT/'docs/maps/town-current'
SOURCES=['build_plan.py','story_layout.py','check_plan.py','audit_actors.py','export_plan.py','make_viewer.py','make_actor_view.py','viewer.js','actors.js','style.css','actors.css']
GENERATED=['plan-data.json','plan-data.js','town-plan.svg','building-schedule.csv','checks.json','actor-audit.json','actor-audit.js','actor-routes.csv','building-parts.csv','trees.csv','roads.csv','openings.csv','town-plan.tmj','index.html','actors.html','home-relations.svg']
def require(ok,message):
    if not ok: raise ValueError(message)
def validate(d,a,c):
    require(c['status']=='PASS' and not c['errors'],'Parcel, door, path or road geometry failed')
    require(not a['geometryErrors'],'Actor route intersects a building')
    require(len(a['permutations'])==6,'All six free investigation orders must be checked')
    routes={r['id']:r for r in a['routes']}
    for actor in a['actors']:
        require(set(actor['routes'])<=routes.keys(),f"Missing route for {actor['id']}")
    require(len({b['id'] for b in d['buildings']})==len(d['buildings']),'Duplicate building ID')
    facilities={f['id'] for f in d['facilities']}
    items=d.get('streetItems',[])
    require(len(items)==41 and len({i['kind'] for i in items})==10,'Approved street furniture set changed (10 kinds, 41 instances)')
    require(not {'clock','newsstand','phonebooth','streetsign'}&{i['kind'] for i in items},'Street furniture must not add clocks, newspapers, phones or street names')
    require('F03' not in facilities,'Other-town broken lamp must not return to the local map')
    require(not {'h-ask','h-call','h-chess','keeper-church','patrol'} & routes.keys(),'Unconfirmed/off-map historical route restored')
    for actor in a['actors']:
        if actor['id'] in ['shopkeeper','patrolman','lampworker']:
            require(not actor['routes'] and not actor['anchors'],'Other-town witness mapped locally')
    journeys=d['offMapJourneys']
    require(journeys[0]['date']=='1931-01-19' and journeys[0]['actual'] and journeys[0]['mode']=='train','19 Jan actual train journey changed')
    require(journeys[1]['date']=='1931-01-20' and not journeys[1]['actual'],'20 Jan claim must not become actual travel')
    require(journeys[0]['travelMinutes'] is None,'Other-town travel time not established')
    require(routes['w-plan']['basis']=='未实施计划','Wife escape must stay unimplemented')
    s=a['summary']
    require(s['unallocatedWithin20']>=0,'20 Jan station round trip exceeds the 20-minute window')
    require(s['milkServiceBudgetWithin10']>=0,'Milk route exceeds independent receipt window')
    require(s['eveningWaitBudget']>=0,'Local evening leg exceeds its window')
    sights={s['id']:s for s in a['sights']}
    require(not sights['S01']['blockers'] and not sights['S02']['blockers'],'Required plan-view sight corridor blocked')
    require('B12' in sights['S03']['blockers'],'Return door no longer screened by house')
    sample=a['finalReturnSamples'];require(sample['total']>0 and sample['blocked']==sample['total'],'Final return segment exposed to specified window')
def main():
    ap=argparse.ArgumentParser(description=__doc__);ap.add_argument('--write',action='store_true',help='Update generated exports after editing sources');args=ap.parse_args()
    with tempfile.TemporaryDirectory(prefix='town-map-check-') as name:
        tmp=Path(name)
        for n in SOURCES:shutil.copy2(MAP/n,tmp/n)
        for script in ['build_plan.py','check_plan.py','audit_actors.py','export_plan.py','make_viewer.py','make_actor_view.py']:
            subprocess.run([sys.executable,'-X','utf8',str(tmp/script)],check=True,stdout=subprocess.PIPE,text=True,encoding='utf-8')
        d=json.loads((tmp/'plan-data.json').read_text());a=json.loads((tmp/'actor-audit.json').read_text());c=json.loads((tmp/'checks.json').read_text());validate(d,a,c)
        result=subprocess.run([sys.executable,'-X','utf8',str(ROOT/'skills/wrong-date-narrative/scripts/validate_story.py'),str(ROOT/'docs/mvp-evidence.json')],check=True,capture_output=True,text=True,encoding='utf-8')
        story=json.loads(result.stdout)
        report=dict(status='PASS',version=d['version'],sourceHashes={n:hashlib.sha256((MAP/n).read_bytes()).hexdigest() for n in SOURCES},contractHash=hashlib.sha256((ROOT/'docs/mvp-evidence.json').read_bytes()).hexdigest(),checkerHash=hashlib.sha256(Path(__file__).read_bytes()).hexdigest(),properties=len(d['buildings']),trees=len(d['trees']),actors=len(a['actors']),routes=len(a['routes']),summary=a['summary'],storyValidation=story,limitations=c['limitations']+['Public road centerlines, not pedestrian swept width','No other-town route or train timetable','Positive time budgets are unallocated, not proven activity'])
        (tmp/'verification.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n')
        stale=[]
        for n in GENERATED+['verification.json']:
            if args.write:shutil.copy2(tmp/n,MAP/n)
            elif not (MAP/n).exists() or (MAP/n).read_bytes()!=(tmp/n).read_bytes():stale.append(n)
        require(not stale,'Stale generated outputs; run --write and review: '+', '.join(stale))
        print(json.dumps(dict(status='MAP_CHECK_PASS',properties=report['properties'],actors=report['actors'],routes=report['routes'],summary=report['summary']),ensure_ascii=False,indent=2))
if __name__=='__main__':
    try:main()
    except (ValueError,KeyError,OSError,subprocess.CalledProcessError) as exc:sys.exit('MAP_CHECK_FAIL: '+str(exc))
