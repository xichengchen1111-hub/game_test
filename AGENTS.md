# Active game and design

- This repository is the active `game_test` project. Follow the human user's newest instructions before any document or skill.
- `docs/mvp-demo.md` defines the current first-person, freely explored MVP. The original Chinese DOCX/PDF supplies story background; older third-person or fixed-route wording does not override the current MVP.
- In the complete MVP all three interiors are initially accessible. The current user-requested 10% visual slice implements the town and clock shop; its shop is freely accessible from the beginning. The companion makes optional ambient remarks; its dialogue, occupation and location never gate clues, doors or deductions.
- Concepts in `docs/concepts/` are generated style references. The playable Unity project is in `game/`; actual runtime captures and the walking clip are in `output/demo/`, and the local Windows player is in `builds/windows/` (ignored by Git).
- Skills are versioned in `skills/` and installed with `tools/install_project_skills.py`. Use `wrong-date-narrative` for chronology/evidence, `wrong-date-unity` for scenes and code, `wrong-date-blender` for assets, and `wrong-date-playtest` for runtime verification. Read the corresponding `SKILL.md` before applying it.
- `docs/mvp-evidence.json` is a development contract, not player-facing UI text. Keep private deductions out of the material viewer and preserve stable evidence IDs.
- Do not claim a playable build from concepts, static data checks or compilation. Gameplay acceptance needs runtime evidence and a separate fairness review.
- Do not include `codex` in names created for branches, commits, pull requests, worktrees, tasks or artifacts.
