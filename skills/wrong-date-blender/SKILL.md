---
name: wrong-date-blender
description: Produce editable modular low-poly buildings, props and simplified NPCs for Wrong Date Testimony using Blender and its Python CLI. Use for this game's 3D asset creation and Unity import checks.
---

# Wrong Date Testimony low-poly assets

Locate the active repo and read `docs/mvp-demo.md`. Inspect the existing three `docs/concepts/*-low-poly.png` images before establishing proportions or a new visual variant. The images are concept targets, not a promise of complex geometry.

## Asset choices

- Keep broad flat color, gray-blue/purple shadows and warm window/interior light. Reuse a small wall, door, window, floor, desk and cabinet kit across scenes. Prefer silhouette, palette and authored surface decoration over extra geometry.
- Simplified faceless NPCs share a body and, when animation is introduced, one basic rig. Distinguish them through period-appropriate clothing and a small prop. Start with idle and talk motion; do not add facial rigs, hair simulation or elaborate locomotion to meet this MVP.
- Preserve 1931 prop context while carrying over the reference images' lived-in warmth. Modern neon signs and electronics from the reference do not automatically belong in the setting.
- Dates, clock hands, ticket holes and text must be precise. Author typography and clock geometry deterministically; ImageGen can supply surface decoration and style variants but its generated text cannot be a factual clue asset.
- Use meter-scale conventions, useful pivots (door hinge, prop base), explicit material assignments and consistent export transforms. Keep editable `.blend` sources and named collections separate from FBX output. Use simple Unity colliders rather than exporting every visual mesh as a convex collider.

## Scripted production and verification

Check the actual Blender executable version; the present machine has Blender 5.2.0 LTS. Prefer `blender.exe --background --python-exit-code 1 --python asset_script.py -- [task arguments]` for deterministic asset generation. Set the Python exit code before the script so exceptions produce a failing process result. Verify current operators with the installed `--help`, `bpy` API or official Blender docs before adding a pipeline dependency.

Read task parameters instead of hardcoding an unrelated project's object names. Write only the intended generated collection/assets; preserve existing source objects and user edits. Re-running a generator must not duplicate the kit or alter exported names unexpectedly.

After generating or changing assets, open the resulting `.blend` programmatically to inspect dimensions, origins, material assignments and object counts, and make rendered review captures. Then verify Unity import size, materials, door clearance and player-height views; a successful FBX export is not sufficient. Keep screenshots and concept art clearly distinguished.

Deliver editable sources, imports and the smallest useful asset manifest. Use visual review to assess the palette and simplicity rather than treating polygon counts as proof of style.
