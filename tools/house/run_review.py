import subprocess,pathlib,json,time,hashlib,sys
r=pathlib.Path(__file__).resolve().parents[2];base=r/'builds/house-review';capture=base/(sys.argv[1] if len(sys.argv)>1 else 'first1080');capture.mkdir(exist_ok=True)
app=base/'住宅验收.app/Contents/MacOS/错日证词 · 暮色小镇';args=[str(app),'--review','--fresh','--review-house','--capture-dir',str(capture),'-screen-width','1920','-screen-height','1080','-screen-fullscreen','0','-logFile',str(capture/'player.log')]
if '--reload' in sys.argv:args.remove('--fresh');args[args.index('--review-house')]='--verify-house-save'
if '--storage' in sys.argv:args.append('--storage-only')
if '--evidence' in sys.argv:args.append('--evidence-only')
if '--placement' in sys.argv:args.append('--placement-only')
if '--stairs' in sys.argv:args.append('--stairs-only')
if '--baseline' in sys.argv:args.append('--stairs-baseline')
if '--720' in sys.argv:args[args.index('1920')]='1280';args[args.index('1080')]='720'
save=pathlib.Path.home()/'Library/Application Support/com.qingchuanstudio.wrongdatetest.currentmac/slice-save.json'
orig=save.read_bytes() if save.exists() else None
started=time.monotonic();p=subprocess.Popen(args);print('player_pid',p.pid,flush=True)
while p.poll() is None:
 time.sleep(2)
 log=(capture/'player.log').read_text(errors='replace') if (capture/'player.log').exists() else ''
 if time.monotonic()-started>600: p.terminate();raise RuntimeError('Review timed out')
 if 'HOME_REVIEW_FAIL' in log or 'HOME_SAVE_FAIL' in log or 'Exception:' in log:
  print(log[-5000:],flush=True);p.terminate();break
code=p.wait(timeout=30)
unchanged=(save.read_bytes() if save.exists() else None)==orig
expected='HOME_SAVE_PASS' if '--reload' in sys.argv else 'HOME_STORAGE_REVIEW_PASS' if '--storage' in sys.argv else 'EVIDENCE_REVIEW_PASS' if '--evidence' in sys.argv else 'HOME_PLACEMENT_VIEWS_PASS' if '--placement' in sys.argv else 'HOME_STAIRS_REVIEW_PASS' if '--stairs' in sys.argv else 'HOME_REVIEW_PASS'
passed=expected in (capture/'player.log').read_text(errors='replace')
result={'exit':code,'protocolPassed':passed,'expectedResult':expected,'normalSaveUnchanged':unchanged,'capture':str(capture),'normalSaveSHA256':hashlib.sha256(orig).hexdigest() if orig else None};(capture/'process.json').write_text(json.dumps(result,indent=2));print(json.dumps(result),flush=True)
if code!=0 or not unchanged or not passed:raise SystemExit(1)
