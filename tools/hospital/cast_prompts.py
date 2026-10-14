"""Reference prompts for the hospital cast (Tripo image generation, then image-to-model).

Writes one text file per character into assets/source/hospital-cast/references/. Every
prompt is used together with the approved police duty-officer reference image as the style
reference (assets/source/police-cast/revision-02/duty-officer-reference.png), so the hospital
people belong to the same cast as the watchmaker, the station clerk and the police.
Clothing follows docs/research/hospital-1931.md.

    python3 tools/hospital/cast_prompts.py
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
        "faint soft grounding shadow. Hands empty. Nothing hangs from the body: no bags, pouches, straps, tools, stethoscope, "
        "keys, watch chains or handheld props, and both flanks and hips are completely clear so the arms can be lowered. "
        "No other views, inset portraits, diagrams, scenery, text, labels, logos, borders or collage.")
CAST = {
    # Revision 2 (2026-10-14, user): she should look gravely ill and exhausted, in a hospital gown,
    # so that the player pities her at first sight. Revision 1 wore a nightgown and a rose bed jacket.
    'Child': ("an 8-year-old girl, a long-stay patient in a children's hospital ward, about 1.22 metres tall, with the real "
              "proportions of an eight-year-old (about six head lengths), neither a toddler nor a teenager, slight build. Pale "
              "complexion with no colour in the cheeks, faint bluish shadows under tired grey-green eyes, small straight nose, a "
              "quiet, gentle and weary expression. Light brown hair in two short plaits, a little untidy, with a short fringe.",
              "a modest 1930s children's hospital ward outfit: a long-sleeved, high round-necked hospital gown of plain white "
              "cotton with very faint pale-blue stripes, reaching below the knees, worn over matching long white cotton pyjama "
              "trousers; plain grey wool bed socks, no shoes. No jacket, cardigan, belt, pockets or jewellery."),
    'Porter': ("a 62-year-old hospital hall porter, about 1.70 metres tall, spare and a little stooped, with a long lined face, "
               "pale blue eyes, a full grey walrus moustache, thinning grey hair and large capable hands. Kind, watchful and tired.",
               "a plain dark navy serge porter's uniform of 1931: single-breasted jacket buttoned to a low stand collar with six "
               "dull brass buttons, matching trousers, well-polished black boots, and a soft-crowned peaked cap with a narrow "
               "plain band and no badge. No belt, no epaulettes, no insignia. It must read as a hospital porter or doorman, "
               "clearly not a policeman or soldier."),
    'WardSister': ("a 42-year-old ward sister in charge of a children's ward, about 1.63 metres tall, straight-backed and "
                   "capable, medium build, with a calm oval face, hazel eyes, fine lines at the eyes and dark auburn hair pinned up "
                   "under her cap. Composed and gentle, used to being obeyed.",
                   "a hospital sister's uniform of about 1931: plain mid-blue cotton dress with long sleeves, hem at mid-calf; a "
                   "long starched white apron with a square bib pinned at the chest; stiff white collar, white cuffs and a "
                   "white belt; a white frilled muslin cap tied under the chin with a small bow; black stockings and plain "
                   "black lace-up shoes. No watch, scissors, chain or pocket contents."),
    'StaffNurse': ("a 27-year-old staff nurse, about 1.68 metres tall, slim and quick, with a narrow freckled face, blue eyes "
                   "and fair hair cut in a short wave under her cap.",
                   "a nurse's uniform of about 1931: long-sleeved cotton dress in fine blue-and-white stripes, hem at mid-calf; a "
                   "long starched white apron with a rounded bib and straps crossing at the back; stiff white collar, cuffs and "
                   "belt; a plain white 'Sister Dora' cap gathered at the back; black stockings and black lace-up shoes."),
    'Probationer': ("a 19-year-old probationer nurse, about 1.57 metres tall, short and sturdy with a round rosy face, brown "
                    "eyes and dark brown hair in a low knot. Eager and slightly anxious.",
                    "a probationer's uniform of about 1931: long-sleeved plain lilac-grey cotton dress, hem at mid-calf; a long "
                    "white apron with a square bib; white collar and cuffs; a small plain white cap pinned flat on the head; "
                    "black stockings and black lace-up shoes."),
    'Matron': ("a 56-year-old hospital matron, about 1.66 metres tall, stately and broad-shouldered, with a strong square face, "
               "grey eyes, iron-grey hair parted in the middle and drawn back. Dignified and unsentimental.",
               "a matron's dress of about 1931: plain dark navy gown with long sleeves and a hem at the lower calf, narrow white "
               "collar and cuffs, a dark petersham belt with a small plain silver buckle, and a white lace cap with two short "
               "lappets behind. No apron. Black stockings and black court shoes with a low heel."),
    'HouseSurgeon': ("a 34-year-old house surgeon, about 1.78 metres tall, lean, with a narrow intelligent face, dark eyes, "
                     "dark hair brushed back and a neat short moustache. Overworked and courteous.",
                     "a knee-length white cotton doctor's coat buttoned at the front over a grey waistcoat, white shirt with a "
                     "soft collar and a dark tie, charcoal trousers and brown leather lace-up shoes. Coat pockets flat and empty."),
    'VisitingDoctor': ("a 61-year-old country doctor, about 1.72 metres tall, portly, with a broad kindly face, small round "
                       "wire spectacles, a bald crown with white side hair and a short white beard.",
                       "a dark charcoal three-piece suit of 1931 with a wing-collared white shirt and a black tie, the jacket "
                       "worn open over the buttoned waistcoat, and black boots. No hat, no bag, no watch chain."),
    'PatientMan': ("a 68-year-old male patient, about 1.69 metres tall, thin, with a gaunt unshaven face, sunken cheeks, "
                   "watery blue eyes and sparse white hair.",
                   "blue-and-white striped flannel pyjamas under a shabby long brown wool dressing gown tied flat at the waist "
                   "with its cord tucked in, and plain felt slippers."),
    # Reworded after two refused generations: plain hospital-ward wording only.
    'PatientWoman': ("a 45-year-old woman recovering in a hospital ward, about 1.60 metres tall, of slight build, with a pale "
                     "tired kind face, dark eyes and dark hair in one long braid.",
                     "a modest ankle-length white cotton hospital gown with long sleeves and a high buttoned collar, a grey "
                     "hand-knitted cardigan buttoned over it, and plain grey felt slippers."),
    'PatientBoy': ("a 6-year-old boy patient, about 1.12 metres tall, small and wiry with the real proportions of a six-year-old, "
                   "a snub nose, brown eyes, sticking-up sandy hair and a gap in his front teeth hidden by closed lips.",
                   "striped flannel pyjamas in faded red and cream, a little too big, with the sleeves turned back once, and "
                   "grey wool bed socks. No shoes."),
    'VisitorWoman': ("a 33-year-old working-class mother visiting her son in hospital, about 1.62 metres tall, with a worried "
                     "gentle face, brown eyes and brown hair mostly hidden under her hat.",
                     "her best winter clothes of 1931: a dark green wool coat buttoned to the neck and reaching below the knee, "
                     "with a small brown velvet collar, a close-fitting brown felt cloche hat, brown knitted gloves, dark brown "
                     "wool hose and worn brown strap shoes with a low heel. No handbag or basket."),
    'VisitorMan': ("a 40-year-old labourer visiting a patient, about 1.74 metres tall, broad and weathered, with a square "
                   "stubbled jaw, grey eyes and cropped dark hair under his cap.",
                   "a grey tweed flat cap, a heavy dark brown overcoat worn open over a waistcoat and a collarless striped "
                   "shirt, a knitted maroon muffler tucked inside the coat, dark trousers and heavy black boots."),
    'VisitorGranny': ("a 72-year-old grandmother visiting a patient, about 1.52 metres tall, small and bent, with a deeply "
                      "lined face, sharp dark eyes and white hair drawn back.",
                      "old-fashioned mourning black: a long black wool dress to the ankle, a black knitted shawl pinned flat "
                      "across the shoulders, a small black bonnet-like hat and black button boots."),
}


def prompt(cid):
    who, wears = CAST[cid]
    return f"{STYLE}\nCharacter identity: {who}\nClothing: {wears}\n{POSE}\n"


if __name__ == '__main__':
    out = ROOT / 'assets/source/hospital-cast/references'
    out.mkdir(parents=True, exist_ok=True)
    for cid in CAST:
        (out / f'{cid}.txt').write_text(prompt(cid), encoding='utf-8')
    print('HOSPITAL_CAST_PROMPTS', len(CAST))
