// Requires Playwright. MAP_REVIEW_BROWSER optionally chooses an installed Chromium executable.
const {chromium}=require('playwright');
const fs=require('fs'), path=require('path'), {pathToFileURL}=require('url'), assert=require('assert');
const R=path.resolve(__dirname,'../docs/maps/town-current');
(async()=>{
 const b=await chromium.launch({headless:true,...(process.env.MAP_REVIEW_BROWSER?{executablePath:process.env.MAP_REVIEW_BROWSER}:{})});
 try {
 const p=await b.newPage({viewport:{width:1600,height:1050}}), errors=[];
 p.on('pageerror',e=>errors.push(e.message));
 await p.goto(pathToFileURL(path.join(R,'actors.html')).href);await p.waitForFunction(()=>window.actorViewReady);
 const options=await p.locator('#actor option').evaluateAll(os=>os.map(o=>o.value));let count=0;
 for(const id of options){
  await p.locator('#actor').selectOption(id);
  assert(await p.locator('#actor-info h2').textContent());
  const routes=await p.locator('[data-route]').evaluateAll(es=>es.map(e=>e.dataset.route));
  for(const route of routes){await p.locator(`[data-route="${route}"]`).click();await p.locator('#scrub').fill('1000');assert(!/NaN|undefined/.test(await p.locator('#actor-map').getAttribute('viewBox')));count++;}
  if(['shopkeeper','patrolman','lampworker'].includes(id)){assert(await p.locator('#play').isDisabled());assert.equal(await p.locator('[id^="anchor-"]').count(),0);}
 }
 await p.locator('#actor').selectOption('wife');await p.locator('[data-route="w-plan"]').click();assert((await p.locator('#scene-caption').textContent()).includes('未实施'));
 await p.locator('#together').click();await p.locator('#scrub').fill('600');assert((await p.locator('#clock').textContent()).includes('17:56'));assert.equal(await p.locator('#h').getAttribute('visibility'),'hidden');
 await p.locator('#rewind').click();const before=await p.locator('#h').getAttribute('cx');await p.locator('#play').click();await p.waitForTimeout(700);assert.notEqual(before,await p.locator('#h').getAttribute('cx'));await p.locator('#play').click();
 await p.setViewportSize({width:390,height:844});assert(await p.evaluate(()=>document.documentElement.scrollWidth<=innerWidth));
 await p.goto(pathToFileURL(path.join(R,'index.html')).href);await p.locator('#detail h2').waitFor();assert((await p.locator('#detail').textContent()).includes('东正门'));
 await p.setViewportSize({width:1600,height:1050});await p.locator('[data-view="station"]').click();assert((await p.locator('#detail').textContent()).includes('(188, 40)'));
 await p.locator('#search').fill('B14');await p.locator('#results button').click();assert((await p.locator('#detail').textContent()).includes('背景'));
 await p.locator('[data-layer="trees"]').uncheck();assert.equal(await p.locator('#town-svg #trees').evaluate(e=>e.style.display),'none');
 assert.deepEqual(errors,[]);console.log(JSON.stringify({status:'MAP_UI_PASS',actors:options.length,routeSelections:count,pageErrors:errors,scope:'Web interactions only; not Unity or 3D validation'},null,2));
 } finally {await b.close();}
})().catch(e=>{console.error(e);process.exitCode=1});
