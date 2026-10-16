"""Reference prompts for the street cast (Tripo image generation, then image-to-model).

User, 2026-10-15: make a batch of townsfolk in Tripo — a boy, a girl, a dog walker and her dog, and
several adults dressed differently — so the street life (StreetLife) no longer borrows the hospital's
visitors. Writes one text file per figure into assets/source/street-cast/references/. Every human is
generated with the approved police duty-officer reference as the style image, so the townsfolk belong
to the same cast as the police, the hospital and the station clerk. Dress follows
docs/research/street-life-1931.md (January 1931, a small American town: nearly every adult in a hat,
flat caps for workers, fedoras and homburgs for the middle class, mid-calf skirts and small tilted hats
for women, knickers and long socks for boys; worn, oversized coats for the poor).

    python3 tools/street/cast_prompts.py
"""
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
STYLE = ("Single-character image-to-3D modeling reference for the 1931 fictional-town game Wrong Date Testimony. "
         "The supplied image of a police officer is the APPROVED rendering, material and proportion reference: treat it as a style "
         "reference only and create the person described below as a genuinely different character in different clothes. "
         "Match its restrained, slightly simplified realistic 3D game-character rendering: softly modeled facial anatomy, natural "
         "visible sclera, colored irises with pupils and shaped eyelids, true human proportions for the stated age, subtly "
         "simplified forms, matte worn cloth with visible weave and seams, soft neutral studio lighting, quiet neutral expression "
         "with closed lips. Avoid cartoon doll faces, enormous bead eyes, chibi proportions, anime, plastic toy surfaces, "
         "airbrushed fashion beauty and photographic photorealism.")
POSE = ("Pose and composition: ONE person only, directly FRONT-facing, full body, neutral symmetrical T-pose. Both arms straight "
        "out horizontally at shoulder height, elbows extended, wrists straight, open relaxed hands with five naturally shaped "
        "fingers each, palms facing the ground. Both feet flat, a little apart, knees straight. The whole head with any headwear, "
        "both fingertips and both soles fully visible with comfortable margins. Pure plain white studio background with a very "
        "faint soft grounding shadow. Hands empty. Nothing hangs from the body: no bags, baskets, umbrellas, canes, leashes, "
        "newspapers, pouches, straps or handheld props, and both flanks and hips are completely clear so the arms can be lowered. "
        "No other views, inset portraits, diagrams, scenery, text, labels, logos, borders or collage.")
# Role: (height in metres, identity, clothing). Heights are what tools/cast/import_period_character.py scales to.
CAST = {
    'StreetBoy': (1.30, "a 9-year-old schoolboy of a small American town, about 1.30 metres tall, with the real proportions of a "
                  "nine-year-old (about six and a half head lengths), slim and lively, freckles, blue-grey eyes and short sandy "
                  "hair under his cap.",
                  "boys' winter clothes of 1931: a grey tweed flat cap, a short brown wool jacket buttoned over a cream knitted "
                  "sweater, grey wool knickerbockers buckled just below the knee, long dark grey wool socks to the knee and "
                  "scuffed brown leather lace-up boots. No scarf, no bag."),
    'StreetGirl': (1.24, "an 8-year-old girl of a small American town, about 1.24 metres tall, with the real proportions of an "
                   "eight-year-old (about six head lengths), neither a toddler nor a teenager, with rosy cheeks, brown eyes and "
                   "a dark brown bob with a straight fringe.",
                   "girls' winter clothes of 1931: a knee-length double-breasted red-brown wool coat with a round collar and "
                   "two rows of buttons, a navy knitted tam-o'-shanter beret, thick ribbed beige wool stockings and black "
                   "patent strap shoes. No muff, no bag."),
    'DogWalker': (1.63, "a 52-year-old woman of comfortable means, about 1.63 metres tall, upright and brisk, with a kind square "
                  "face, grey eyes and greying brown hair set in waves under her hat.",
                  "a 1931 winter outfit: a calf-length dark plum wool coat with a deep brown fur collar, wrapped and fastened "
                  "with one large button at the hip, a small dark felt hat with a narrow brim tilted forward, brown leather "
                  "gloves, dark lisle stockings and sturdy black low-heeled shoes. No handbag."),
    'Businessman': (1.78, "a 46-year-old businessman, about 1.78 metres tall, well fed, with a clean-shaven heavy-jowled face, "
                    "brown eyes and dark hair greying at the temples.",
                    "a charcoal grey felt fedora with a black band, a double-breasted mid-grey wool overcoat reaching below the "
                    "knee, buttoned, over a white shirt with a starched collar and a dark striped tie, charcoal trousers with "
                    "turn-ups and polished black oxford shoes. No briefcase, no gloves in hand."),
    'YoungWoman': (1.65, "a 24-year-old office clerk, about 1.65 metres tall, slender, with an oval face, green eyes, a little "
                   "rouge and short dark blonde hair in finger waves.",
                   "a fashionable 1931 outfit: a small dark green felt hat worn tilted over one eyebrow, a fitted burgundy wool "
                   "coat with a light fox-fur collar reaching mid-calf and belted at the natural waist, a cream silk scarf at "
                   "the neck, sheer stockings and black strap shoes with a small heel. No bag."),
    'Labourer': (1.75, "a 33-year-old delivery workman, about 1.75 metres tall, lean and strong, with a long weathered face, a "
                 "broken nose, dark stubble and dark eyes.",
                 "working clothes of 1931: a dark blue wool cap with a short leather peak, a heavy brown canvas chore coat with "
                 "a corduroy collar and patch pockets, worn open over a grey flannel shirt and braces, faded blue denim work "
                 "trousers and heavy black work boots. No gloves, no tools."),
    'OldGentleman': (1.70, "a 74-year-old retired gentleman, about 1.70 metres tall, thin and slightly stooped, with a long lined "
                     "face, a neat white moustache, pale blue eyes and white hair at the sides.",
                     "old-fashioned 1931 town clothes: a black bowler hat, a long black Chesterfield overcoat with a velvet "
                     "collar, buttoned, over a dark suit, a white wing collar and a grey silk tie, grey spats over polished "
                     "black boots. Hands empty: no cane."),
    'Housewife': (1.60, "a 38-year-old housewife out shopping, about 1.60 metres tall, of medium build, with a round friendly "
                  "face, hazel eyes and brown hair rolled at the neck.",
                  "a practical 1931 outfit: a hand-knitted grey-blue cloche hat, a brown-and-grey herringbone tweed coat reaching "
                  "below the knee, buttoned, a knitted mustard scarf crossed at the throat, thick brown wool stockings and "
                  "brown laced walking shoes. No basket."),
    'Transient': (1.73, "a 50-year-old man down on his luck, about 1.73 metres tall, gaunt, with a tired lined face, a week of "
                  "grey stubble, deep-set brown eyes and untidy greying hair under his hat.",
                  "worn-out 1931 clothes: a battered brown felt fedora with a dented crown, an oversized dark army-surplus "
                  "greatcoat with patched elbows and a missing button, a collarless faded shirt, a frayed grey muffler, baggy "
                  "patched trousers and cracked old boots tied with string. Shabby but not torn to rags."),
}
# The dog walks beside the dog walker on a leash; Tripo rigs it as a quadruped (StreetDog in Unity).
DOG = ("Single-animal image-to-3D modeling reference for the 1931 fictional-town game Wrong Date Testimony, in the same restrained, "
       "slightly simplified realistic 3D game rendering as the supplied style image: matte fur with visible clumps rather than "
       "single hairs, soft neutral studio lighting. A wire fox terrier dog of the 1930s, adult, about 39 cm tall at the shoulder, "
       "white body with tan-and-black patches on the head and saddle, wiry coat, a plain brown leather collar. "
       "Pose and composition: ONE dog only, standing square on all four legs in a neutral relaxed stance seen from the side with "
       "the head turned slightly toward the viewer, legs straight and a little apart, tail up, mouth closed. The whole animal "
       "including ears, all four paws and the tail tip fully visible with margins. Pure plain white studio background with a "
       "very faint soft grounding shadow. No leash, no people, no other views, text, labels or borders.")


def prompt(role):
    _, who, wears = CAST[role]
    return f"{STYLE}\nCharacter identity: {who}\nClothing: {wears}\n{POSE}\n"


if __name__ == '__main__':
    out = ROOT / 'assets/source/street-cast/references'
    out.mkdir(parents=True, exist_ok=True)
    for role in CAST:
        (out / f'{role}.txt').write_text(prompt(role), encoding='utf-8')
    (out / 'Terrier.txt').write_text(DOG + "\n", encoding='utf-8')
    print('STREET_CAST_PROMPTS', len(CAST) + 1)
