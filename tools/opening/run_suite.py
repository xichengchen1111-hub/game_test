import subprocess,sys,pathlib,json,os
r=pathlib.Path(__file__).resolve().parents[2]
modes=sys.argv[1:] or ['1080','reload','720','reload','opening-ride','opening-ride-save','opening-ride-interrupted','opening-ride-interrupted-save']
results=[]
for mode in modes:
 print('START '+mode,flush=True)
 p=subprocess.run([sys.executable,str(r/'tools/opening/run_review.py'),mode],cwd=r)
 results.append({'mode':mode,'exit':p.returncode})
 (r/'builds/opening'/(os.environ.get('OPENING_REVIEW_PREFIX','suite')+'-suite.json' if os.environ.get('OPENING_REVIEW_PREFIX') else 'suite.json')).write_text(json.dumps(results,indent=2))
 if p.returncode:sys.exit(p.returncode)
