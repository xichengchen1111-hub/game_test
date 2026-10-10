"""Encode timestamped native frames with their captured game mix, without retiming."""
from pathlib import Path
import json, subprocess, sys
import imageio_ffmpeg
ffmpeg=imageio_ffmpeg.get_ffmpeg_exe()
source=Path(sys.argv[1]).resolve();dest=Path(sys.argv[2]).resolve();dest.mkdir(parents=True,exist_ok=True)
names={'current-home-station':'01-从家全速跑到车站','current-station-home':'02-沿近路全速返家','01-rain-and-splashes':'01-大雨与落地水花','02-enter-room-rain':'02-走进室内听雨','03-grey-day-fog':'03-灰雾白天','01-milk-doors-rain':'01-牛奶店开关门与雨声','02-weather-street-lamps':'02-雨天路灯与晴天对比'}
results=[]
for folder in sorted(source.iterdir()):
 if folder.name not in names or not (folder/'capture.json').exists():continue
 manifest=json.loads((folder/'capture.json').read_text());frames=manifest['frames'];assert len(frames)>40 and manifest['samples']>0
 # The first displayed frame holds from capture start; subsequent durations use real wall-clock times.
 listing=[]
 for i,frame in enumerate(frames):
  at=0 if i==0 else frame['time'];end=frames[i+1]['time'] if i+1<len(frames) else manifest['duration']
  listing += ["file '"+frame['file']+"'",f'duration {max(.001,end-at):.9f}']
 listing += ["file '"+frames[-1]['file']+"'"]
 (folder/'frames.ffconcat').write_text('\n'.join(listing)+'\n')
 target=dest/(names[folder.name]+'.mp4')
 command=[ffmpeg,'-hide_banner','-loglevel','error','-y','-f','concat','-safe','0','-i',str(folder/'frames.ffconcat'),'-itsoffset',str(max(0,manifest['audioStart'])),'-i',str(folder/'game-audio.wav'),'-t',str(manifest['duration']),'-c:v','libx264','-crf','20','-preset','fast','-pix_fmt','yuv420p','-r','30','-c:a','aac','-b:a','192k','-movflags','+faststart',str(target)]
 subprocess.run(command,check=True)
 results.append({'file':target.name,'source':str(folder),'seconds':manifest['duration'],'nativeFrames':len(frames),'gameAudioChannels':manifest['channels'],'gameAudioSampleRate':manifest['rate'],'audioStartSeconds':manifest['audioStart']})
 print(target.name,round(manifest['duration'],2),flush=True)
(dest/'recording-provenance.json').write_text(json.dumps(results,ensure_ascii=False,indent=2))
if len(results)==3:
 # A short viewing reel. All motion and sounds remain at their recorded speed.
 inputs=[]
 for r in results:inputs+=['-i',str(dest/r['file'])]
 graph='[0:v]trim=duration=12,setpts=PTS-STARTPTS[v0];[0:a]atrim=duration=12,asetpts=PTS-STARTPTS[a0];[v0][a0][1:v][1:a][2:v][2:a]concat=n=3:v=1:a=1[v][a]'
 subprocess.run([ffmpeg,'-hide_banner','-loglevel','error','-y',*inputs,'-filter_complex',graph,'-map','[v]','-map','[a]','-c:v','libx264','-crf','20','-preset','fast','-pix_fmt','yuv420p','-c:a','aac','-b:a','192k','-movflags','+faststart',str(dest/'天气效果实录.mp4')],check=True)

if len(results)==2 and results[0]['file'].startswith('01-牛奶店'):
 inputs=[]
 for r in results:inputs+=['-i',str(dest/r['file'])]
 subprocess.run([ffmpeg,'-hide_banner','-loglevel','error','-y',*inputs,'-filter_complex','[0:v][0:a][1:v][1:a]concat=n=2:v=1:a=1[v][a]','-map','[v]','-map','[a]','-c:v','libx264','-crf','20','-preset','fast','-pix_fmt','yuv420p','-c:a','aac','-b:a','192k','-movflags','+faststart',str(dest/'牛奶店与雨天路灯实录.mp4')],check=True)
