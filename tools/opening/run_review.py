import subprocess,pathlib,sys,time,hashlib,json,os
r=pathlib.Path(__file__).resolve().parents[2]
mode=sys.argv[1] if len(sys.argv)>1 else '1080'
cap=r/'builds/opening'/(os.environ.get('OPENING_REVIEW_PREFIX','final')+'-'+mode);cap.mkdir(parents=True,exist_ok=True)
app=next(pathlib.Path(os.environ.get('OPENING_PLAYER_APP',str(r/'builds/opening/错日证词-第一幕.app'))).joinpath('Contents/MacOS').iterdir())
pairs={'hospital':'HOSPITAL_REVIEW_PASS','hospital-save':'HOSPITAL_SAVE_PASS','yard-gate':'YARD_GATE_REVIEW_PASS','yard-gate-save':'YARD_GATE_SAVE_PASS','police-cast':'POLICE_CAST_REVIEW_PASS','opening-ride':'OPENING_RIDE_REVIEW_PASS','opening-ride-save':'OPENING_RIDE_SAVE_PASS','opening-ride-interrupted':'OPENING_RIDE_INTERRUPTED_PASS','opening-ride-interrupted-save':'OPENING_RIDE_INTERRUPTED_SAVE_PASS','map-sightlines':'MAP_SIGHTLINE_REVIEW_PASS','full-map':'FULL_MAP_REVIEW_PASS','full-map-save':'FULL_MAP_SAVE_PASS','station-clerk':'STATION_CLERK_REVIEW_PASS','station-clerk-save':'STATION_CLERK_SAVE_PASS','dairy-doors':'DAIRY_DOORS_REVIEW_PASS','dairy-doors-save':'DAIRY_DOORS_SAVE_RELAUNCH_PASS','map':'MAP_NAVIGATION_REVIEW_PASS','map-save':'MAP_NAVIGATION_SAVE_PASS','walnut-shop':'WALNUT_REVIEW_PASS','walnut-save':'WALNUT_SAVE_RELAUNCH_PASS','day-night':'DAY_NIGHT_REVIEW_PASS','day-night-save':'DAY_NIGHT_SAVE_RELAUNCH_PASS'}
flag={'reload':'--verify-opening-save','police':'--review-police','police-reload':'--verify-police-save','opening-ride-interrupted-save':'--verify-opening-ride-interrupted'}.get(mode,('--verify-'+mode if mode.endswith('-save') else '--review-'+mode) if mode in pairs else '--review-opening')
a=[str(app),'--review',flag,'--capture-dir',str(cap),'-screen-width','1280' if (mode=='720' or os.environ.get('OPENING_REVIEW_HEIGHT')=='720') else '1920','-screen-height','720' if (mode=='720' or os.environ.get('OPENING_REVIEW_HEIGHT')=='720') else '1080','-screen-fullscreen','0','-logFile',str(cap/'player.log')]
if os.environ.get('OPENING_REVIEW_BATCHMODE')=='1':a+=['-batchmode']
if 'reload' not in mode and not mode.endswith('-save'):a+=['--fresh']
save=pathlib.Path.home()/'Library/Application Support/com.qingchuanstudio.wrongdatetest.currentmac/slice-save.json'
before=save.read_bytes() if save.exists() else None
p=subprocess.Popen(a,stdout=subprocess.DEVNULL,stderr=subprocess.DEVNULL);t=time.monotonic()
while p.poll() is None:
 time.sleep(1);log=(cap/'player.log').read_text(errors='replace') if (cap/'player.log').exists() else ''
 if 'Exception:' in log or time.monotonic()-t>900:p.terminate();break
code=p.wait(timeout=30);log=(cap/'player.log').read_text(errors='replace')
marker={'reload':'OPENING_SAVE_PASS','police':'POLICE_REVIEW_PASS','police-reload':'POLICE_SAVE_PASS'}.get(mode,pairs.get(mode,'OPENING_REVIEW_PASS'))
unchanged=(save.read_bytes() if save.exists() else None)==before
result=dict(passed=marker in log and code==0 and unchanged,marker=marker,exit=code,normalSaveUnchanged=unchanged,normalSaveSHA256=hashlib.sha256(before).hexdigest() if before else None)
(cap/'process.json').write_text(json.dumps(result,indent=2));print(json.dumps(result),flush=True)
if not result['passed']:print(log[-4000:]);sys.exit(1)
