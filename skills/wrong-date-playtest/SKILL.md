---
name: wrong-date-playtest
description: Verify Wrong Date Testimony's Unity demo with different exploration orders, silent-companion runs, persistence checks and rendered gameplay evidence. Use for this game's playable acceptance and regression checks.
---

# Wrong Date Testimony playtesting

Read the active repo's `docs/mvp-demo.md`, evidence contract and the installed Unity project's test/build configuration. Identify the actual available build or Editor before reporting that testing happened. If no game exists, only report design checks and specify that gameplay remains untested.

## Distinguish the evidence

1. Narrative/data checks establish consistent references and possible deductions, not discoverability.
2. EditMode/PlayMode tests establish code and state behavior within what they actually execute.
3. A rendered runtime pass establishes observed doors, raycasts, focus, material legibility and report interaction.
4. A blind playthrough establishes whether an unfamiliar player can reason without the author guiding them. Automation and the author knowing the answer do not substitute for this.

## Meaningful acceptance passes

- Start fresh runs for all six initial visit orders of station, clock shop and home. Vary evidence order inside a scene, including records before witnesses; revisit after acquiring a relevant card elsewhere. Assert that question availability depends on facts, not the traversal history.
- Disable all companion remarks. Core material, doors and report support must remain available. Do not count a missing hint as a failed clue pickup.
- Exercise normal runtime interactions and transitions, not only debug commands that grant cards or teleport through locked doors. If a harness bypasses inputs, separately verify actual interaction reach and visibility.
- For state-related changes, save and relaunch from an exterior doorway and an interior after acquiring material. Compare scene/position, evidence IDs, notes, selected associations, NPC questions and report status. Do not rely only on a memory-to-memory serialization test.
- Check each door's return point, repeated interaction during fade, collision clearance, inspection open/close, dialogue close and journal close. Make sure movement/look and cursor recover after UI and loading.
- Submit an incomplete or unsupported report, continue investigating, then submit supported time/date judgments. Ensure no automatic culprit reveal, no clue-count completion and no loss of progress.
- Capture real game frames at player height and evidence-viewer scale. At 1920x1080 and 1280x720, check dates, ticket edges, clock hands, dialogue, contrast and clipping. Capture at least one transition result and one rendered report interaction.

Use the available engine test hooks, scripted runtime harness and normal rendered player when appropriate. Browser CUA does not provide native Unity control. When the Windows computer-use skill's supported sky interface is available, use it for real player-window input and observation, following that skill's rules. Do not assume an editor-specific automation API exists. A screenshot-only capture still does not prove an interaction occurred. Avoid building a large general automation system for a small focused check.

## Report

Record build/editor version, exercised paths, actual assertions, screenshots and any input bypass. Measure frame timing and play duration only on a real run. Separate technical failures, inference ambiguity and visual/readability issues. Keep the six-order smoke suite for accepted milestones; after a small fix, rerun the affected case and expand only when state/progression concerns justify it.
