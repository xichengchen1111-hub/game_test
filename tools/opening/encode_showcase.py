"""Encode a ShowcaseRecorder capture of the first act into presentation videos.

    python3 tools/opening/encode_showcase.py <capture folder> <destination folder>

The capture folder is what `--review --fresh --showcase-opening` writes: numbered JPEG frames
with wall-clock times, the listener mix as float WAV and named marks in capture.json. Two files
come out: the whole act at recorded speed, and a shorter cut made of whole passages chosen by
time. Nothing is retimed, regraded, cropped or captioned; the game mix is only raised back from
the quiet listener volume used while recording.
"""
import bisect
import json
import subprocess
import sys
from pathlib import Path

import imageio_ffmpeg
import numpy as np

FFMPEG = imageio_ffmpeg.get_ffmpeg_exe()

# (start, end, what the passage shows) in seconds of the capture, relative to a named mark.
# First act revision 2 (2026-10-15): cell cinematic, property room, seated lawyer, the lift.
HIGHLIGHT = [
    ('title', .4, 'title', 3.8, '标题画面：一桩到你那个年代都没破的命案'),
    ('awake', -6.0, 'awake', 2.6, '铁床上睁眼，模糊的天花板，头好痛'),
    ('look', 0.0, 'look', 8.0, '坐起来环顾：石墙铁栏、老式吊灯'),
    ('realize', -1.0, 'knock', 1.4, '“我……真的穿越了？”方明博士的装置竟然是真的——被敲门打断'),
    ('knock', 1.4, 'knock', 4.4, '狱警拳头砸门：“喂！醒了没有？”'),
    ('unlock', -.6, 'released', 4.0, '钥匙开锁、手推开门：“算你走运……”'),
    ('door-police-evidence-door', -1.5, 'door-police-evidence-door', 3.0, '狱警握住门把手推开保管室门'),
    ('cage', -.8, 'cage', 3.6, '开柜锁，拉开柜门'),
    ('carry', -1.2, 'carry', 2.4, '伸手提起公文包'),
    ('place', -.6, 'place', 2.0, '放到桌上'),
    ('briefcase', -.5, 'briefcase', 3.0, '按 E 拿回公文包'),
    ('watch', 0.0, 'watch', 3.4, '东西回来了：取出怀表'),
    ('pack', -.1, 'pack', 4.6, '按 B 打开公文包：怀表、地图、车票、调查簿和钱夹都在包里'),
    ('door-police-court-door', -1.2, 'door-police-court-door', 2.6, '推开横厅后门'),
    ('lawyer', 0.0, 'lawyer', 7.0, '律师：“你出来了……脸色这么差。”绕到椅子侧面坐下'),
    ('lawyer', 9.0, 'lawyer', 16.0, '坐到对面，律师先问你这几天怎么样'),
    ('newspaper', .5, 'newspaper-text', 3.5, '报纸递到手里，展开正文'),
    ('child', -.3, 'child', 7.0, '问起孩子：“我已经给医院捎了话”'),
    ('follow-out', .5, 'follow-out', 7.0, '律师起身离开，跟着走出会见室'),
    ('offer', 1.5, 'offer', 6.4, '律师提出送你去医院'),
    ('boarding', -1.0, 'boarding', 7.5, '上车：开门、低头坐进副驾驶，回头看车门'),
    ('driving', 21.2, 'driving', 27.2, '路口减速转弯'),
    ('goodbye', -3.0, 'goodbye', 5.5, '到医院下车，律师从驾驶座转头看着你道别'),
    ('departure', .3, 'departure', 5.0, '目送汽车离开'),
]


def run(command):
    subprocess.run(command, check=True)


def render(folder, manifest, audio, gain, passages, target, work):
    """passages: [(start, end)] on the capture clock, played in order with hard cuts."""
    times = manifest['times']
    listing, graph, labels, total = [], [], [], 0.0
    for index, (start, end) in enumerate(passages):
        first = bisect.bisect_left(times, start)
        last = max(first + 1, bisect.bisect_left(times, end))
        last = min(last, len(times))
        begin = times[first]
        finish = times[last] if last < len(times) else manifest['duration']
        for frame in range(first, last):
            until = times[frame + 1] if frame + 1 < len(times) else manifest['duration']
            listing += [f"file '{folder / f'frame{frame:05d}.jpg'}'", f'duration {max(.001, until - times[frame]):.9f}']
        length = finish - begin
        offset = max(0.0, begin - manifest['audioStart'])
        edge = min(.05, length / 4)
        graph.append(f'[1:a]atrim=start={offset:.6f}:duration={length:.6f},asetpts=PTS-STARTPTS,'
                     f'afade=t=in:d={edge:.3f},afade=t=out:st={length - edge:.6f}:d={edge:.3f}[a{index}]')
        labels.append(f'[a{index}]')
        total += length
    listing.append(listing[-2])  # the concat demuxer needs the last file repeated to honour its duration
    script = work / (target.stem + '.ffconcat')
    script.write_text('ffconcat version 1.0\n' + '\n'.join(listing) + '\n', encoding='utf-8')
    graph.append(''.join(labels) + f'concat=n={len(labels)}:v=0:a=1,volume={gain:.4f},'
                 f'afade=t=in:d=0.4,afade=t=out:st={total - .9:.3f}:d=0.9[a]')
    # JPEG frames are full-range BT.601; deliver ordinary limited-range BT.709 so players agree on the levels.
    graph.append(f'[0:v]fps=30,scale=1920:1080:in_range=full:out_range=limited:in_color_matrix=bt601:out_color_matrix=bt709:flags=bicubic+accurate_rnd+full_chroma_int,'
                 f'format=yuv420p,fade=t=in:d=0.4,fade=t=out:st={total - .9:.3f}:d=0.9[v]')
    run([FFMPEG, '-hide_banner', '-loglevel', 'error', '-y', '-f', 'concat', '-safe', '0', '-i', str(script), '-i', str(audio),
         '-filter_complex', ';'.join(graph), '-map', '[v]', '-map', '[a]', '-t', f'{total:.3f}',
         '-c:v', 'libx264', '-crf', '18', '-preset', 'slow', '-pix_fmt', 'yuv420p',
         '-colorspace', 'bt709', '-color_primaries', 'bt709', '-color_trc', 'bt709', '-color_range', 'tv',
         '-c:a', 'aac', '-b:a', '192k', '-movflags', '+faststart', str(target)])
    return total


def main():
    folder = Path(sys.argv[1]).resolve()
    dest = Path(sys.argv[2]).resolve()
    dest.mkdir(parents=True, exist_ok=True)
    work = folder.parent
    manifest = json.loads((folder / 'capture.json').read_text())
    marks = {m['name']: m['time'] for m in manifest['marks']}
    audio = folder / 'game-audio.wav'
    mix = np.fromfile(audio, dtype='<f4', offset=44)
    peak = float(np.abs(mix).max())
    # Back to the level heard at full volume, short of clipping.
    gain = min(1 / manifest['listenerVolume'], .95 / peak) if peak > 0 else 1.0
    full_end = marks['end'] + 1.4
    full = dest / '01-开场实机完整版.mp4'
    cut = dest / '02-开场实机精华版.mp4'
    # A moment that did not happen in this run (a door already open) is left out rather than guessed.
    shown = [h for h in HIGHLIGHT if h[0] in marks and h[2] in marks]
    passages = [(marks[a] + da, marks[b] + db) for a, da, b, db, _ in shown]
    report = {
        'source': str(folder), 'nativeFrames': len(manifest['times']), 'captureSeconds': manifest['duration'],
        'size': [manifest['width'], manifest['height']], 'recordedListenerVolume': manifest['listenerVolume'],
        'audioGain': round(gain, 3), 'audioPeakAfterGain': round(peak * gain, 3), 'marks': manifest['marks'],
        'method': 'Native game window and listener mix of a scripted run with the real controller, doors, dialogue, '
                  'inspection and car ride. Recorded speed; no retiming, grading, cropping or captions. '
                  'Scripted inputs are not physical keyboard play.',
        'videos': [
            {'file': full.name, 'seconds': round(render(folder, manifest, audio, gain, [(0, full_end)], full, work), 2), 'cuts': 0},
            {'file': cut.name, 'seconds': round(render(folder, manifest, audio, gain, passages, cut, work), 2),
             'passages': [{'from': round(s, 2), 'to': round(e, 2), 'shows': h[4]} for (s, e), h in zip(passages, shown)]},
        ],
    }
    (dest / '素材来源.json').write_text(json.dumps(report, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
    for video in report['videos']:
        print(video['file'], video['seconds'])


if __name__ == '__main__':
    main()
