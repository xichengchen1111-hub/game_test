"""Validate the MVP design contract; this is not a Unity gameplay test."""

import argparse
from datetime import datetime, timedelta
from itertools import permutations
import json
from pathlib import Path


def index_items(items, label):
    indexed = {item["id"]: item for item in items}
    if len(indexed) != len(items):
        raise ValueError(f"Duplicate {label} ID")
    return indexed


def validate(data):
    if data.get("schema_version") != 1 or not data.get("development_only"):
        raise ValueError("Expected a development-only version 1 contract")
    if data.get("scene_visit_order") != "free":
        raise ValueError("Exploration must remain free")
    scenes = index_items(data["scenes"], "scene")
    expected_scenes = {"town", "station", "clock_shop", "home"}
    if set(scenes) != expected_scenes:
        raise ValueError("Unexpected MVP scene scope")
    if not all(scene.get("initially_accessible") for scene in scenes.values()):
        raise ValueError("Every MVP scene must be initially accessible")
    if data["companion"].get("gates_progress") or data["companion"].get("role") != "optional_ambient":
        raise ValueError("Companion remarks must not gate progression")

    evidence = index_items(data["evidence"], "evidence")
    if not 8 <= len(evidence) <= 10:
        raise ValueError("MVP evidence budget is 8-10 cards")
    for item in evidence.values():
        if item["scene"] not in scenes or not item.get("source") or not item.get("fact"):
            raise ValueError(f"Missing scene, source or fact: {item['id']}")
        if not item.get("admissible"):
            raise ValueError(f"Evidence cards need a verifiable source: {item['id']}")
        for requirement in item.get("requires", []):
            if requirement not in evidence:
                raise ValueError(f"Unknown prerequisite {requirement}; hints cannot unlock evidence")

    events = index_items(data["timeline"], "event")
    at = {key: datetime.fromisoformat(event["at"]) for key, event in events.items()}
    expected_days = {
        "route_query": "1931-01-19", "appointment_call": "1931-01-19",
        "appointment_received": "1931-01-19", "lamp_repaired": "1931-01-20",
        "station_visit": "1931-01-20", "delivery_before": "1931-01-20",
        "milk_delivery": "1931-01-20", "delivery_after": "1931-01-20",
    }
    for key, day in expected_days.items():
        if key not in at or at[key].date().isoformat() != day:
            raise ValueError(f"Canonical event date mismatch: {key}")
    if not at["route_query"] < at["appointment_call"] < at["appointment_received"] < at["lamp_repaired"]:
        raise ValueError("Route, call, letter and lamp events are out of order")
    if not at["station_visit"] < at["milk_delivery"]:
        raise ValueError("Station visit must precede the milk delivery")
    if not at["delivery_before"] < at["milk_delivery"] < at["delivery_after"]:
        raise ValueError("Milk delivery must be between independent signed receipts")
    milk = events["milk_delivery"]
    if milk["clock_offset_minutes"] != 30:
        raise ValueError("Street clock offset must match the current canonical 30 minutes")
    displayed = at["milk_delivery"] + timedelta(minutes=milk["clock_offset_minutes"])
    if displayed.strftime("%H:%M") != milk["reported_clock"]:
        raise ValueError("Reported milk time does not match the clock offset")

    montage = index_items(data["montage"], "montage")
    if len(montage) > 4:
        raise ValueError("MVP montage budget is at most four fragments")
    for clip in montage.values():
        if clip["event"] not in events or clip.get("admissible") is not False:
            raise ValueError(f"Montage is not police evidence: {clip['id']}")
    deductions = index_items(data["deductions"], "deduction")
    allowed_kinds = {"alibi_contradiction", "time_contradiction", "date_contradiction", "staging_question"}
    for item in deductions.values():
        if item["conclusion_kind"] not in allowed_kinds:
            raise ValueError("MVP does not conclude murder guilt")
        if not item.get("required_evidence"):
            raise ValueError(f"Deduction has no source material: {item['id']}")
        for eid in item["required_evidence"] + item.get("supporting_evidence", []):
            if eid not in evidence:
                raise ValueError(f"Deduction uses a montage, hint or unknown material: {eid}")
    report = data["report"]
    cores = {key for key, item in deductions.items() if item.get("core")}
    if cores != set(report["core_deductions"]) or len(cores) != 2:
        raise ValueError("Report must match both MVP core deductions")
    if not all(report.get(flag) for flag in ["requires_player_submission", "allow_partial_report", "allow_continue_after_report"]):
        raise ValueError("Reports must be player chosen and allow continued exploration")
    dialogue = index_items(data.get("dialogue", []), "dialogue")
    for question in dialogue.values():
        if question["scene"] not in scenes or not set(question["requires"]) <= evidence.keys():
            raise ValueError(f"Unknown dialogue prerequisite: {question['id']}")

    route_results = []
    for order in permutations(["station", "clock_shop", "home"]):
        acquired = set()
        questions = set()
        for scene in ["town", *order, "town", *order]:
            changed = True
            while changed:
                changed = False
                for eid, item in evidence.items():
                    if item["scene"] == scene and eid not in acquired and set(item.get("requires", [])) <= acquired:
                        acquired.add(eid)
                        changed = True
            # Knowledge is monotone; no conversation counter or reminder is consumed.
            available = {qid for qid, q in dialogue.items() if set(q["requires"]) <= acquired}
            if not questions <= available:
                raise ValueError("A follow-up question disappeared as evidence grew")
            questions = available
        available_deductions = {did for did, d in deductions.items() if set(d["required_evidence"]) <= acquired}
        if not cores <= available_deductions or acquired != set(evidence):
            raise ValueError(f"Evidence/deduction softlock in visit order: {order}")
        route_results.append(" -> ".join(order))
    return {"evidence_cards": len(evidence), "core_deductions": sorted(cores), "visit_orders_checked": route_results,
            "verification_scope": "Design data only; no runtime input, visuals, saves or puzzle fairness checked"}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("contract", type=Path)
    args = parser.parse_args()
    try:
        result = validate(json.loads(args.contract.read_text(encoding="utf-8")))
    except (ValueError, KeyError, TypeError, OSError) as exc:
        parser.exit(1, f"FAIL: {exc}\n")
    print(json.dumps(result, ensure_ascii=False, indent=2))


if __name__ == "__main__":
    main()
