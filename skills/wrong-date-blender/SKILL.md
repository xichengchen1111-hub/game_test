---
name: wrong-date-blender
description: Produce editable modular low-poly buildings, props and simplified NPCs for Wrong Date Testimony using Blender and its Python CLI. Use for this game's 3D asset creation and Unity import checks.
---

# Wrong Date Testimony low-poly assets

Locate the active repo and read `docs/mvp-demo.md`. Inspect the existing three `docs/concepts/*-low-poly.png` images before establishing proportions or a new visual variant. The images are concept targets, not a promise of complex geometry.

## Asset choices

- Keep broad flat color, gray-blue/purple shadows and warm window/interior light. Reuse a small wall, door, window, floor, desk and cabinet kit across scenes. Prefer silhouette, palette and authored surface decoration over extra geometry.
- Most simplified NPCs share a body and, when animation is introduced, one basic rig. The milk-delivery child uses the user's latest supplied `Meshy_AI_The_Little_Messenger_biped.zip` (2026-10-08); preserve its mesh, face, clothing, textures, weights and six supplied actions. Its looping short-breath/look-around idle supersedes the previous Toon Boy blond/blue-eye/strap-gripping requirements. Keep E04 and the clockmaker independent. Start with idle and talk motion; do not add facial rigs, hair simulation or elaborate locomotion to meet this MVP.
- Preserve 1931 prop context while carrying over the reference images' lived-in warmth. Modern neon signs and electronics from the reference do not automatically belong in the setting.
- The shop owner uses the user's `Meshy_AI_The_Watchmaker_s_Focu_biped.zip` (2026-10-08), preserving its supplied model and three actions. Only `Sit_Cross_Legged` loops in the slice, with root motion disabled and a fitted wooden chair. Keep `assets/source/watchmaker/` editable. Measure actual pelvis support and soles when fitting the chair; the current 1.72m rest height uses a 0.40m seat. Derive both shop instances from the shared prefab, preserve conversation/E07, and inspect a whole sitting cycle plus side/back contact in Unity. Do not use whole-body bobbing, standing transitions, or the previous primitive owner.
- Dates, clock hands, ticket holes and text must be precise. Author typography and clock geometry deterministically; ImageGen can supply surface decoration and style variants but its generated text cannot be a factual clue asset.
- Use meter-scale conventions, useful pivots (door hinge, prop base), explicit material assignments and consistent export transforms. Keep editable `.blend` sources and named collections separate from FBX output. Use simple Unity colliders rather than exporting every visual mesh as a convex collider.

## Scripted production and verification

Check the actual Blender executable version; the current Mac has Blender 4.5.9 LTS; do not assume a previous host's version. Prefer `blender.exe --background --python-exit-code 1 --python asset_script.py -- [task arguments]` for deterministic asset generation. Set the Python exit code before the script so exceptions produce a failing process result. Verify current operators with the installed `--help`, `bpy` API or official Blender docs before adding a pipeline dependency.

Read task parameters instead of hardcoding an unrelated project's object names. Write only the intended generated collection/assets; preserve existing source objects and user edits. Re-running a generator must not duplicate the kit or alter exported names unexpectedly.

After generating or changing assets, open the resulting `.blend` programmatically to inspect dimensions, origins, material assignments and object counts, and make rendered review captures. Then verify Unity import size, materials, door clearance and player-height views; a successful FBX export is not sufficient. Keep screenshots and concept art clearly distinguished.

Deliver editable sources, imports and the smallest useful asset manifest. Use visual review to assess the palette and simplicity rather than treating polygon counts as proof of style.
