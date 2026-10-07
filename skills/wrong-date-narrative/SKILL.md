---
name: wrong-date-narrative
description: Design and check Wrong Date Testimony evidence, chronology, NPC knowledge, and freely explored investigation progression. Use for this game's story data and deduction changes.
---

# Wrong Date Testimony narrative

Locate the active `game_test` repository from the task workspace. Read its `AGENTS.md`, `docs/mvp-demo.md` and relevant original story passages before changing a plot fact. The first-person, free exploration and optional companion decisions in the user's latest instructions take precedence over older proposals.

## Evidence and progression

- Maintain `docs/mvp-evidence.json` as a development contract. Preserve stable IDs when editing wording. Record the actual source, what it establishes, and its limits; the player material viewer must not display developer-only facts or deduction labels automatically.
- Three interiors are available from the start. Core evidence remains obtainable without a companion remark, a particular visit sequence or a mandatory earlier conversation. NPC follow-up questions derive from the current evidence set, not visit counters.
- The MVP investigates a time contradiction and a date contradiction. Unpunched tickets only weaken a travel claim. Do not promote weak alibi evidence into proof of murder or add the full moral endings to this MVP.
- Distinguish past events, reported times and present observations. A current fast street clock does not by itself prove it was fast during the murder; dated delivery signatures supply the independent check. A patrol original can establish the date without forcing an additional visit merely to collect corroboration.
- Objective montage fragments are true events in an initially unreliable order. They help replay and compare observations but are not police evidence; core factual deductions must also have traceable records or testimony. Do not add an unseen confession or a new final shot to provide the answer.
- Keep the companion's natural remarks optional and restrained. Its occupation never creates a gameplay approval gate. Family pressure explains motive without excusing murder; preserve the wife's human details early.

## Check and deliver

Run `scripts/validate_story.py` from this skill with the repository's `docs/mvp-evidence.json`. It checks references, canonical relative dates, the clock arithmetic, hint independence and all six interior visit orders at the data level. Read a failing condition before changing a fact to make it pass.

Then review each core deduction manually: can an attentive player find and read its material, understand the inference, and reject an alternative explanation? Static graph reachability cannot establish this. When a change introduces another valid support set, represent that relationship rather than requiring every corroborating item.

Deliver the updated source data and a concise explanation of what each changed clue supports. Keep unapproved full-chapter revisions identified as proposals. Report data validation separately from human puzzle fairness and runtime verification.
