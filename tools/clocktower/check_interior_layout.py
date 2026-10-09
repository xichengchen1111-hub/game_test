#!/usr/bin/env python3
"""Check the shared three-storey stair geometry; this is not a runtime walking test."""
import json
import math
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
source = ROOT / "game/Assets/Scripts/ClockTowerInteriorLayout.cs"
text = source.read_text()
constants = {
    name: float(value.rstrip("f"))
    for name, value in re.findall(r"public const (?:float|int) (\w+) = ([\d.\-]+f?);", text)
}
c = constants
count = int(c["StepCount"])
middle_step = int(c["MiddleStepCount"])
flat_count = int(c["MiddleLandingSegments"])
turn_steps = int(c["StepsPerTurn"])
open_steps = int(c["OpenHeadroomSteps"])
lower_rise = (c["MiddleFloor"] - c["GroundFloor"]) / middle_step
upper_rise = (c["MachineFloor"] - c["MiddleFloor"]) / (count - middle_step - flat_count)
angle = 360 / turn_steps


def height(progress):
    progress = max(0, min(count, progress))
    if progress <= middle_step:
        return c["GroundFloor"] + progress * lower_rise
    if progress <= middle_step + flat_count:
        return c["MiddleFloor"]
    return c["MiddleFloor"] + (progress - middle_step - flat_count) * upper_rise


tops = [height(i + 1) for i in range(count)]
rises = [height(i + 1) - height(i) for i in range(count)]
turn_clearances = [tops[i + turn_steps] - tops[i] - c["TreadThickness"] for i in range(count - turn_steps)]
middle_deck_clearance = c["MiddleFloor"] - tops[middle_step - open_steps - 1] - c["TreadThickness"]
machine_deck_clearance = c["MachineFloor"] - tops[count - open_steps - 1] - c["TreadThickness"]
walk_run = c["WalkingRadius"] * math.radians(angle)
maximum_grade = math.degrees(math.atan2(max(rises), walk_run))
clear_width = 1.35 - .32 - .05
outer_guard_margin = 1.326 * math.cos(math.radians(angle / 8)) - (c["WalkingRadius"] + .25 + .03)

# Inspect the next turn above every stair-centre capsule. Adjacent angular
# sectors can be lower than the sector directly overhead, so report both the
# vertical architectural clearance and the capsule's conservative disc envelope.
# This envelope uses the full radius at the crown, larger than the real capsule.
envelope_clearances = []
for i in range(count - turn_steps):
    centre_angle = math.radians(c["StartAngle"] + (i + .5) * angle)
    centre = (math.cos(centre_angle) * c["WalkingRadius"], math.sin(centre_angle) * c["WalkingRadius"])
    overhead = []
    for sample in range(72):
        a = sample * math.tau / 72
        x, z = centre[0] + .28 * math.cos(a), centre[1] + .28 * math.sin(a)
        direction = math.degrees(math.atan2(z, x))
        angular_slot = int(((direction - c["StartAngle"]) % 360) / angle)
        for j in range(max(0, i + turn_steps - 2), min(count, i + turn_steps + 3)):
            if j % turn_steps == angular_slot:
                overhead.append(tops[j] - tops[i] - c["TreadThickness"])
    if overhead:
        envelope_clearances.append(min(overhead))

middle_angle = c["StartAngle"] + middle_step * angle
final_angle = c["StartAngle"] + count * angle
middle_standing_angle = middle_angle + 45
middle_standing_radius = .67
middle_detour_headroom = min(tops[middle_step + turn_steps + j] - c["MiddleFloor"] - c["TreadThickness"] for j in range(flat_count))
middle_entry_headroom = tops[middle_step + turn_steps] - c["MiddleFloor"] - c["TreadThickness"]
middle_exit_headroom = tops[middle_step + flat_count + turn_steps] - tops[middle_step + flat_count] - c["TreadThickness"]
checks = {
    "exact_three_storey_heights": abs(height(middle_step) - c["MiddleFloor"]) < 1e-6 and abs(height(count) - c["MachineFloor"]) < 1e-6,
    "all_rises_below_19cm": all(-1e-6 <= rise <= .19 for rise in rises),
    "five_middle_segments_are_flat": all(abs(tops[i] - c["MiddleFloor"]) < 1e-6 for i in range(middle_step, middle_step + flat_count)),
    "middle_landing_spans_90_degrees": abs(flat_count * angle - 90) < 1e-6,
    "middle_and_upper_arrivals_align": abs((final_angle - middle_angle) % 360) < 1e-6,
    "every_turn_centre_headroom_at_least_225cm": all(v >= 2.25 for v in turn_clearances),
    "capsule_envelope_has_15cm_above_178cm_player": min(envelope_clearances) >= 1.78 + .15,
    "both_decks_have_225cm_centre_headroom": min(middle_deck_clearance, machine_deck_clearance) >= 2.25,
    "middle_standing_entry_and_exit_headroom": min(middle_detour_headroom, middle_entry_headroom, middle_exit_headroom) >= 2.25,
    "middle_detour_clears_central_collider": middle_standing_radius - .285 >= .25 + .03,
    "middle_detour_clears_both_open_edges": middle_standing_radius * math.sin(math.radians(45)) >= .25 + .03,
    "clear_stair_width_at_least_80cm": clear_width >= .8,
    "outer_guard_leaves_10cm_capsule_margin": outer_guard_margin > .10,
    "stair_inside_shared_shell": c["OuterRadius"] * 2 <= c["InteriorWidth"] - .20,
    "walking_grade_below_45_degrees": maximum_grade < 45,
    "door_clears_capsule": c["DoorWidth"] > .25 * 2 + .03 * 2 and c["DoorHeight"] > 1.78,
    "mechanism_standing_space_clears_capsule": abs(-.64 - (c["MechanismX"] + .22)) >= .25 + .03,
    "machine_room_above_player_height": c["InnerWallHeight"] - c["MachineFloor"] > 2.25,
}
assert all(checks.values()), checks
report = {
    "source": str(source.relative_to(ROOT)),
    "scope": "Static geometric constraints only. Unity compilation, rendered appearance and real CharacterController ascent/descent with a stop on the middle platform must be checked separately.",
    "constants": constants,
    "derived": {
        "rising_tread_count": count - flat_count,
        "flat_middle_segments": flat_count,
        "lower_riser_m": lower_rise,
        "upper_riser_m": upper_rise,
        "step_angle_degrees": angle,
        "walk_radius_run_m": walk_run,
        "maximum_walking_grade_degrees": maximum_grade,
        "clear_stair_width_m": clear_width,
        "minimum_turn_centre_headroom_m": min(turn_clearances),
        "minimum_capsule_disc_envelope_headroom_m": min(envelope_clearances),
        "middle_deck_centre_headroom_m": middle_deck_clearance,
        "machine_deck_centre_headroom_m": machine_deck_clearance,
        "middle_standing_headroom_m": middle_detour_headroom,
        "middle_entry_headroom_m": middle_entry_headroom,
        "middle_exit_headroom_m": middle_exit_headroom,
        "middle_standing_point": [math.cos(math.radians(middle_standing_angle)) * middle_standing_radius, c["MiddleFloor"], math.sin(math.radians(middle_standing_angle)) * middle_standing_radius],
        "platform_open_sector_degrees": open_steps * angle,
        "platform_solid_sector_degrees": 360 - open_steps * angle,
        "outer_guard_capsule_margin_m": outer_guard_margin,
    },
    "all_turn_centre_clearances_m": turn_clearances,
    "checks": checks,
}
print(json.dumps(report, indent=2, ensure_ascii=False))
