---
name: wrong-date-unity
description: Implement Wrong Date Testimony's first-person Unity MVP, independent interior scenes, inspectable evidence, dialogue, reports and persistence. Use for this game's Unity code or scene work.
---

# Wrong Date Testimony Unity

Locate the active game repository; read its `AGENTS.md` and `docs/mvp-demo.md`. Inspect `game/ProjectSettings/ProjectVersion.txt` and `game/Packages/manifest.json` when a project exists. The initial target is installed Unity `6000.3.18f1` with URP; use the actual project version once created, and preserve its metadata and package lock.

## Implementation invariants

- Use `game/` for the Unity project. Keep a persistent systems scene and load the town or one independent interior; all three interior doors work from the start.
- For every enterable building, derive the exterior and independent interior from one layout and building prefab. Match footprint, storeys, ceiling/roof and every door/window's count, position, size and orientation; visible furnishings and views must also correspond. Never add a fake exterior floor/window or edit the two scene shells separately. The slice uses `BuildingLayout`, `ClockShopLayout.asset` and `ClockShop.prefab`; derive window street coordinates from the layout. Run `BuildingLayoutChecks` before the player build, preserving its rejection of moved, missing and extra windows. Keep window frames offset from wall returns to avoid coplanar flicker.
- Route door transitions through one service: prevent duplicate interaction, disable movement, fade out, persist state, load the destination, place the player at a named spawn point, then fade in and restore control. Preserve the specific exterior doorway on return. Restore a saved interior and position on restart when the save was made there.
- Store clues by immutable string ID matching `docs/mvp-evidence.json`; never use scene instance IDs as saved identity. Global knowledge, submitted associations, notes, report status and seen optional barks survive scene unloading and disk save/load.
- Derive NPC questions from the acquired evidence set every time a conversation opens. Starting with a later clue must work. Companion remarks can read context but cannot grant evidence, mark deductions solved, open doors or hold the player in a mandatory dialogue.
- Bind E interaction, Q watch, Tab journal and Esc close consistently. Inspection and dialogue suspend movement/look and manage cursor focus; closing or loading restores it. Render dates and ticket edges legibly in the evidence viewer, with repeatable access to the original material.
- Compute town-clock and watch readings from one shared clock plus the town-clock offset. Present readings for the player to compare; do not label the false time or auto-complete a deduction on observation.
- Keep developer evidence facts and deduction labels out of automatic player UI. Reports require a player-selected assertion with supporting cards. Having all cards is not an automatic conclusion, and an incomplete report leaves exploration available.

## Production and checks

Prefer deterministic Editor scripts to assemble reusable prefabs, collider primitives and scene data. Scope regeneration to generated assets and preserve hand-authored changes and stable `.meta` GUIDs. Use baked or simple light setups and flat URP materials; reserve readable authored textures for clue-critical details.

Use Unity's documented CLI with `-projectPath`, `-batchmode`, `-executeMethod` and an explicit `-logFile` when appropriate. An executed method must be static in an Editor assembly/folder. Do not launch a second editor on the same open project; inspect the running process and available project workflow first. Use `-nographics` for compatible data/compile operations, never as evidence that lighting or screenshots were verified. Run tests through the actual installed test framework; do not invent package versions or an unavailable editor-control API.

For changes to transitions, knowledge or reports, run focused EditMode/PlayMode coverage and a real rendered interaction pass. Follow `wrong-date-playtest` for acceptance. A compiled Editor script, a build exit code or a concept image alone does not establish a playable game. Report the artifact, logs and actual runtime evidence separately.

Primary CLI reference: https://docs.unity3d.com/6000.3/Documentation/Manual/EditorCommandLineArguments.html
