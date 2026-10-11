"""1931 Railway Station B and Steam Train Detail Polish Kit.

Checked Blender modelling draft for an original fictional 1931 station and train.
Self-contained with math only and builder primitives:
b.box, b.beam, b.cylinder, b.ring, b.authored_mesh, b.set_module.

Coordinates are in Unity metre coordinates:
- X: East (+X) / West (-X)
- Y: Up (+Y)
- Z: North (+Z) / South (-Z)
"""

import math


# ===========================================================================
# 1. STATION B DETAILS
# ===========================================================================

def add_station_details(b):
    """Add authentic 1931 architectural and interior details to Station B."""
    print(">>> Adding Station B details...")

    # -----------------------------------------------------------------------
    # 1.1 EXTERIOR ARCHITECTURAL DETAILS (Module_Station_Detail)
    # -----------------------------------------------------------------------
    b.set_module('Module_Station_Detail')

    z_front = -6.0
    z_rear = 6.0
    wall_t = 0.24

    # Plinth Base Courses along South and North Facades
    # Sills are at y in [0.10, 1.30], extending x in [-15, -1.7] and [1.7, 15]
    # Plinths project by 0.05m beyond wall surface (offset z to avoid z-fighting)
    for side_sign, z_w in ((-1, z_front), (1, z_rear)):
        z_offset = z_w + side_sign * (wall_t / 2.0 + 0.035)  # intentional 35mm relief
        # West and East base plinths
        for px_center, p_width, name_suffix in ((-8.35, 13.3, 'West'), (8.35, 13.3, 'East')):
            # Heavy limestone plinth block: y in [0.10, 0.40]
            b.box(f'PlinthBlock_{name_suffix}_{side_sign}',
                  (px_center, 0.25, z_offset),
                  (p_width, 0.30, 0.07), 'RailGranite', bevel=0.006)
            # Chamfered plinth cap moulding: y in [0.40, 0.46]
            b.box(f'PlinthCap_{name_suffix}_{side_sign}',
                  (px_center, 0.43, z_offset - side_sign * 0.01),
                  (p_width + 0.04, 0.06, 0.05), 'RailStone', bevel=0.004)

    # Annex Plinths (West Annex x in [-23, -15], East Annex x in [15, 23])
    for annex_cx, x_outer, a_sign in ((-19.0, -23.0, -1), (19.0, 23.0, 1)):
        # End wall plinth at x_outer
        b.box(f'AnnexEndPlinth_{a_sign}', (x_outer + a_sign * 0.155, 0.25, 0.0),
              (0.07, 0.30, 8.08), 'RailGranite', bevel=0.005)
        # Side wall plinths at z = +-4.0
        for sz, s_sign in ((-4.0, -1), (4.0, 1)):
            b.box(f'AnnexSidePlinth_{a_sign}_{s_sign}',
                  (annex_cx, 0.25, sz + s_sign * 0.155),
                  (8.08, 0.30, 0.07), 'RailGranite', bevel=0.005)

    # Pier capitals, floating modillions and voussoir 'ears' were replaced (2026-10-10) by
    # wall-bearing impost, archivolt and modillion courses in rail_station_beautify.py.

    # Layered Pediment Relief (South facade x in [-3.5, 3.5], y in [6.1, 8.0], NO invented dates)
    z_ped = z_front - 0.21
    # Stepped Classical Frieze Band above pediment base
    b.box('PedimentFriezeBand', (0.0, 6.42, z_ped - 0.02),
          (6.70, 0.08, 0.06), 'RailStone', bevel=0.004)
    # Recessed Geometric Relief Panels in Tympanum (symmetric wings flanking clock)
    for p_sign in (-1, 1):
        b.box(f'PedimentReliefWing_{p_sign}',
              (p_sign * 1.80, 6.72, z_ped - 0.01),
              (2.10, 0.45, 0.04), 'RailStone', bevel=0.004)
        b.box(f'PedimentReliefInner_{p_sign}',
              (p_sign * 1.80, 6.72, z_ped - 0.025),
              (1.85, 0.32, 0.025), 'RailStone', bevel=0.002)

    # Stepped Raking Cornice along Sloped Pediment Gables
    # Left slope: from (-3.5, 6.36) to (0.0, 8.0); Right slope: from (3.5, 6.36) to (0.0, 8.0)
    for g_sign in (-1, 1):
        # Upper raking moulding
        b.beam(f'PedimentRakingUpper_{g_sign}',
               (g_sign * 3.52, 6.38, z_ped - 0.04),
               (0.0, 8.04, z_ped - 0.04),
               radius=0.045, mat='RailStone')
        # Lower raking fillet
        b.beam(f'PedimentRakingLower_{g_sign}',
               (g_sign * 3.42, 6.34, z_ped - 0.02),
               (0.0, 7.96, z_ped - 0.02),
               radius=0.030, mat='RailStone')

    # Carved Classical Urn / Acroterion Finials at Pediment Apex and Corners
    # Apex Finial at (0, 8.05)
    b.box('PedimentApexPedestal', (0.0, 8.08, z_ped - 0.03),
          (0.42, 0.12, 0.24), 'RailStone', bevel=0.004)
    b.cylinder('PedimentApexUrnBody', (0.0, 8.24, z_ped - 0.03),
               0.14, 0.22, 'RailStone', axis=(0, 1, 0), vertices=12)
    b.cylinder('PedimentApexUrnCap', (0.0, 8.38, z_ped - 0.03),
               0.08, 0.08, 'RailStone', axis=(0, 1, 0), vertices=10)
    # Corner Acroteria at x = +-3.5
    for a_sign in (-1, 1):
        b.box(f'PedimentCornerAcroterion_{a_sign}',
              (a_sign * 3.52, 6.46, z_ped - 0.03),
              (0.32, 0.18, 0.22), 'RailStone', bevel=0.004)

    # Narrow Glass & Iron Entry Porch and Scrolled Brackets
    # Doorway opening is x in [-1.7, 1.7], y in [0.10, 3.50], z = -6.0.
    # Porch canopy hangs overhead at y = 3.65 to 3.82, projecting South from z = -6.08 to z = -7.28 (depth 1.2m),
    # spanning x in [-2.0, 2.0] (width 4.0m).
    # Scrolled brackets are mounted on piers at x = +-2.10 (away from central opening x[-1.7, 1.7]!).
    # Headroom under canopy is y >= 3.65m (well above player height).
    porch_y = 3.72
    porch_z_front = -7.26
    porch_z_wall = -6.08

    # Outer Iron Canopy Perimeter Frame
    b.box('PorchFrame_Front', (0.0, porch_y, porch_z_front),
          (4.04, 0.07, 0.07), 'RailIron', bevel=0.003)
    b.box('PorchFrame_Wall', (0.0, porch_y + 0.05, porch_z_wall),
          (4.04, 0.07, 0.07), 'RailIron', bevel=0.003)
    for e_sign in (-1, 1):
        b.beam(f'PorchFrame_Edge_{e_sign}',
               (e_sign * 2.00, porch_y + 0.05, porch_z_wall),
               (e_sign * 2.00, porch_y, porch_z_front),
               radius=0.032, mat='RailIron')

    # Translucent Glass Canopy Roof Panelling (slightly pitched forward for drainage)
    b.box('PorchGlass_Roof', (0.0, porch_y + 0.03, (porch_z_wall + porch_z_front) / 2.0),
          (3.94, 0.015, abs(porch_z_wall - porch_z_front) - 0.06), 'RailGlass')

    # Slender Glazing Ribs across the Canopy
    for rib_x in (-1.33, -0.67, 0.0, 0.67, 1.33):
        b.beam(f'PorchGlazingRib_{rib_x:.2f}',
               (rib_x, porch_y + 0.05, porch_z_wall),
               (rib_x, porch_y + 0.01, porch_z_front),
               radius=0.012, mat='RailIron')

    # Cast Iron Scrolled Brackets mounted on Piers at x = +-2.10 (away from central opening)
    for b_sign in (-1, 1):
        bx = b_sign * 2.10
        # Wall Mounting Flange Plate: y in [2.70, 3.72]
        b.box(f'PorchBracketWallPlate_{b_sign}',
              (bx, 3.20, porch_z_wall + 0.01),
              (0.10, 1.00, 0.035), 'RailIron', bevel=0.003)
        # Horizontal Top Arm Beam
        b.beam(f'PorchBracketTopBeam_{b_sign}',
               (bx, porch_y, porch_z_wall),
               (bx, porch_y, porch_z_front + 0.08),
               radius=0.024, mat='RailIron')
        # Diagonal Main Compression Strut
        b.beam(f'PorchBracketStrut_{b_sign}',
               (bx, 2.75, porch_z_wall),
               (bx, porch_y - 0.02, porch_z_front + 0.12),
               radius=0.022, mat='RailIron')
        # Victorian Scrolled Iron Ring Filigree inside the bracket triangle
        b.ring(f'PorchBracketScrollMain_{b_sign}',
               (bx, 3.38, (porch_z_wall + porch_z_front) / 2.0 + 0.10),
               radius=0.22, thickness=0.013, mat='RailIron', axis=(1, 0, 0),
               segments=18, ring_segs=6)
        b.ring(f'PorchBracketScrollMinor_{b_sign}',
               (bx, 3.52, porch_z_front + 0.32),
               radius=0.11, thickness=0.010, mat='RailIron', axis=(1, 0, 0),
               segments=14, ring_segs=6)

    # Porch lanterns now hang from real glazing ribs (rail_station_beautify, facade lamp module).


    # -----------------------------------------------------------------------
    # 1.2 INTERIOR FURNISHINGS & DETAILS (Module_Station_InteriorDetail)
    # -----------------------------------------------------------------------
    b.set_module('Module_Station_InteriorDetail')

    # Inset Green Wainscoting Panelling on Interior Stone Walls
    # Interior faces at z = -5.88 (South) and z = 5.88 (North).
    # Clear central walkway x in [-2, 2]; panels placed in West wing [-14.5, -2.5] and East wing [2.5, 14.5].
    for side_sign, z_int in ((-1, -5.88), (1, 5.88)):
        z_pane = z_int - side_sign * 0.016  # 16mm inward relief
        for w_sign, (x_min, x_max) in ((-1, (-14.4, -2.6)), (1, ((2.6, 14.4)))):
            span_w = x_max - x_min
            mid_x = (x_min + x_max) / 2.0
            # Continuous Walnut Baseboard Kickplate: y in [0.10, 0.24]
            b.box(f'WainscotBaseboard_{w_sign}_{side_sign}',
                  (mid_x, 0.17, z_pane),
                  (span_w, 0.14, 0.04), 'RailWalnut', bevel=0.004)
            # Continuous Walnut Dado / Chair Rail: y in [1.16, 1.24]
            b.box(f'WainscotDadoRail_{w_sign}_{side_sign}',
                  (mid_x, 1.20, z_pane + side_sign * 0.005),
                  (span_w, 0.08, 0.05), 'RailWalnut', bevel=0.004)
            # 5 Inset Heritage Green Molded Panels in each wing (y in [0.24, 1.16])
            num_panels = 5
            panel_w = (span_w - (num_panels + 1) * 0.18) / num_panels
            for p_idx in range(num_panels):
                px = x_min + 0.18 + p_idx * (panel_w + 0.18) + panel_w / 2.0
                # Inset Green Panel
                b.box(f'WainscotGreenPanel_{w_sign}_{p_idx}_{side_sign}',
                      (px, 0.70, z_pane - side_sign * 0.006),
                      (panel_w, 0.84, 0.02), 'RailGreen', bevel=0.003)
                # Raised Walnut Bolection Frame Border
                b.box(f'WainscotFrameTop_{w_sign}_{p_idx}_{side_sign}',
                      (px, 1.12, z_pane),
                      (panel_w + 0.04, 0.04, 0.03), 'RailWalnut', bevel=0.002)
                b.box(f'WainscotFrameBottom_{w_sign}_{p_idx}_{side_sign}',
                      (px, 0.28, z_pane),
                      (panel_w + 0.04, 0.04, 0.03), 'RailWalnut', bevel=0.002)
                b.box(f'WainscotFrameLeft_{w_sign}_{p_idx}_{side_sign}',
                      (px - panel_w / 2.0, 0.70, z_pane),
                      (0.04, 0.84, 0.03), 'RailWalnut', bevel=0.002)
                b.box(f'WainscotFrameRight_{w_sign}_{p_idx}_{side_sign}',
                      (px + panel_w / 2.0, 0.70, z_pane),
                      (0.04, 0.84, 0.03), 'RailWalnut', bevel=0.002)

    # Detailed, physically supported ticket office is authored by ticket_office.py.

    # Waiting Bench Joinery Polish (Slat backs, scrolled armrests, brass caps)
    # Benches: bx_start in (-10.5, 5.5), bz in (-3.4, -1.8), length 5.0m
    for bx_start in (-10.5, 5.5):
        for bz in (-3.4, -1.8):
            back_z = bz - 0.28 if bz < -2.5 else bz + 0.28
            # Slatted walnut backrest cap moulding: y = 1.16
            b.box(f'BenchBackTopCap_{bx_start:.1f}_{bz:.1f}',
                  (bx_start + 2.5, 1.16, back_z),
                  (5.04, 0.05, 0.07), 'RailWalnut', bevel=0.003)
            # Scrolled Walnut Armrests with Polished Brass Caps
            # Placed at bench ends (bx_start + 0.1, bx_start + 4.9) and center divider (bx_start + 2.5)
            for arm_idx, arm_x in enumerate((bx_start + 0.15, bx_start + 2.5, bx_start + 4.85)):
                # Armrest upright support pillar: y in [0.42, 0.70]
                b.box(f'BenchArmSupport_{bx_start:.1f}_{bz:.1f}_{arm_idx}',
                      (arm_x, 0.56, bz),
                      (0.055, 0.28, 0.46), 'RailWalnut', bevel=0.004)
                # Polished Brass Armrest Cap: y = 0.71
                b.box(f'BenchArmBrassCap_{bx_start:.1f}_{bz:.1f}_{arm_idx}',
                      (arm_x, 0.71, bz),
                      (0.065, 0.022, 0.48), 'RailBrass', bevel=0.003)

    # Traditional Framed Notice Boards with Blank Ivory Centres (text added by parent)
    # 1. North Wall Notice Board: near (9.0, 2.70, 5.86)
    b.box('NoticeBoardNorth_Frame', (10.0, 2.70, 5.86),
          (1.80, 1.25, 0.045), 'RailWalnut', bevel=0.004)
    b.box('NoticeBoardNorth_BlankCenter', (10.0, 2.70, 5.84),
          (1.64, 1.09, 0.015), 'RailIvory')
    b.box('NoticeBoardNorth_BrassClip', (10.0, 3.28, 5.83),
          (0.80, 0.045, 0.025), 'RailBrass', bevel=0.002)

    # 2. West Connection Passage Notice Board: facing +X at (-14.86, 2.60, -3.50)
    b.box('NoticeBoardWest_Frame', (-14.86, 2.60, -3.50),
          (0.045, 1.25, 1.00), 'RailWalnut', bevel=0.004)
    b.box('NoticeBoardWest_BlankCenter', (-14.84, 2.60, -3.50),
          (0.015, 1.09, 0.86), 'RailIvory')

    # 3. East Connection Passage Notice Board: facing -X at (14.86, 2.60, -3.50)
    b.box('NoticeBoardEast_Frame', (14.86, 2.60, -3.50),
          (0.045, 1.25, 1.00), 'RailWalnut', bevel=0.004)
    b.box('NoticeBoardEast_BlankCenter', (14.84, 2.60, -3.50),
          (0.015, 1.09, 0.86), 'RailIvory')

    # Open wire waste-ticket bin with clearance from the adjacent bench end.
    # Exact specs: width 0.48m (radius 0.24m), height 0.78m (y in [0.10, 0.88]).
    # Top actually open (no lid/cap!). Dark inner bottom at 0.10m. Thin wire sides. Polished brass rim.
    # Parent will insert perforated physical tickets into it.
    bin_cx, bin_cz = -11.30, -0.80
    bin_r = 0.24
    bin_bottom_y = 0.10
    bin_top_y = 0.78

    # Dark inner bottom plate at y = 0.105m
    b.cylinder('TicketBin_InnerBottom', (bin_cx, bin_bottom_y + 0.005, bin_cz),
               bin_r - 0.008, 0.010, 'RailIron', axis=(0, 1, 0), vertices=18)
    # Bottom support rim ring
    b.ring('TicketBin_BottomRim', (bin_cx, bin_bottom_y + 0.008, bin_cz),
           radius=bin_r, thickness=0.008, mat='RailIron', axis=(0, 1, 0),
           segments=18, ring_segs=6)
    # Polished Brass Top Rim Ring at y = 0.88m (leaving the top completely open inside!)
    b.ring('TicketBin_TopBrassRim', (bin_cx, bin_top_y, bin_cz),
           radius=bin_r, thickness=0.014, mat='RailBrass', axis=(0, 1, 0),
           segments=18, ring_segs=6)
    # Intermediate Horizontal Wire Hoops
    for hoop_y in (0.26, 0.44, 0.62, 0.72):
        b.ring(f'TicketBin_WireHoop_{hoop_y:.2f}', (bin_cx, hoop_y, bin_cz),
               radius=bin_r, thickness=0.0045, mat='RailIron', axis=(0, 1, 0),
               segments=18, ring_segs=6)
    # Thin Vertical Wire Ribs (18 wires evenly spaced around circumference)
    for w_i in range(18):
        ang = w_i * math.tau / 18.0
        wx = bin_cx + bin_r * math.cos(ang)
        wz = bin_cz + bin_r * math.sin(ang)
        b.beam(f'TicketBin_WireRib_{w_i}',
               (wx, bin_bottom_y, wz), (wx, bin_top_y, wz),
               radius=0.0035, mat='RailIron', vertices=6)


    # -----------------------------------------------------------------------
    # 1.3 PLATFORM & SHED DETAILS (Module_Station_PlatformDetail)
    # -----------------------------------------------------------------------
    b.set_module('Module_Station_PlatformDetail')

    # Platform Column Base Tie Plates & Flanges (y = 0.40)
    # Columns at col_xs in [-30, -20, -10, 0, 10, 20, 30] along cz in (9, 31), skip cx=0, cz=9
    col_xs = [-30.0, -20.0, -10.0, 0.0, 10.0, 20.0, 30.0]
    for cx in col_xs:
        for cz in (9.0, 31.0):
            if cx == 0.0 and cz == 9.0:
                continue
            # Bolted Flange Tie Plate at Column Base (y = 0.41)
            b.box(f'ColTiePlate_{cx:.0f}_{cz:.0f}', (cx, 0.41, cz),
                  (0.54, 0.035, 0.54), 'RailIron', bevel=0.004)
            # Corner Hex Bolt Studs
            for bx_s in (-0.21, 0.21):
                for bz_s in (-0.21, 0.21):
                    b.cylinder(f'ColBolt_{cx:.0f}_{cz:.0f}_{bx_s:.2f}_{bz_s:.2f}',
                               (cx + bx_s, 0.435, cz + bz_s),
                               0.016, 0.025, 'RailIron', axis=(0, 1, 0), vertices=8)
            # Column Capital Knee Bracing / Scrolled Filigree Struts (y in [4.8, 5.4])
            bracket_z_dir = 1.0 if cz < 20 else -1.0
            b.beam(f'ColKneeBrace_{cx:.0f}_{cz:.0f}',
                   (cx, 4.80, cz),
                   (cx, 5.38, cz + bracket_z_dir * 1.10),
                   radius=0.032, mat='RailIron')
            b.ring(f'ColKneeScroll_{cx:.0f}_{cz:.0f}',
                   (cx, 5.10, cz + bracket_z_dir * 0.55),
                   radius=0.18, thickness=0.012, mat='RailIron', axis=(1, 0, 0),
                   segments=14, ring_segs=6)

    # Platform Truss Node Iron Gusset Plates with Rivet Clusters
    truss_xs = [-34.0] + col_xs + [34.0]
    for tx in truss_xs:
        for gz in (9.2, 30.8):
            b.box(f'TrussTieGusset_{tx:.0f}_{gz:.1f}', (tx, 5.40, gz),
                  (0.12, 0.28, 0.22), 'RailIron', bevel=0.004)
            # Rivet heads
            for ry_off in (-0.08, 0.08):
                b.cylinder(f'TrussRivet_{tx:.0f}_{gz:.1f}_{ry_off:.2f}',
                           (tx + 0.065, 5.40 + ry_off, gz),
                           0.014, 0.015, 'RailIron', axis=(1, 0, 0), vertices=8)

    # ORIGINAL Wheeled Luggage Trolley + Believable Leather Suitcases
    # Position: Platform local x = -22.0, z = 20.5 (platform surface topY = 0.42).
    # Maximum 2m extent, completely off the boarding route. Fictional original 1931 style, no real brand.
    trolley_cx = -22.0
    trolley_cz = 20.50
    trolley_bed_y = 0.58
    wheel_r = 0.11
    wheel_hub_y = 0.42 + wheel_r  # 0.53m

    # 4 Cast Iron Spoked Wheels (Axles at x = -22.50 and x = -21.50, track width 0.70m)
    wheel_xs = (-22.50, -21.50)
    wheel_zs = (trolley_cz - 0.35, trolley_cz + 0.35)
    for ax_idx, wx in enumerate(wheel_xs):
        # Transverse axle shaft
        b.beam(f'TrolleyAxleBeam_{ax_idx}',
               (wx, wheel_hub_y, trolley_cz - 0.35),
               (wx, wheel_hub_y, trolley_cz + 0.35),
               radius=0.016, mat='RailIron')
        for s_idx, wz in enumerate(wheel_zs):
            # Wheel tire ring & hub
            b.cylinder(f'TrolleyWheelHub_{ax_idx}_{s_idx}',
                       (wx, wheel_hub_y, wz),
                       0.030, 0.035, 'RailIron', axis=(0, 0, 1), vertices=10)
            b.ring(f'TrolleyWheelTire_{ax_idx}_{s_idx}',
                   (wx, wheel_hub_y, wz),
                   radius=wheel_r, thickness=0.010, mat='RailIron', axis=(0, 0, 1),
                   segments=14, ring_segs=6)
            # Wheel spokes
            for spk in range(4):
                spk_a = spk * math.pi / 4.0
                b.beam(f'TrolleySpoke_{ax_idx}_{s_idx}_{spk}',
                       (wx + wheel_r * 0.9 * math.cos(spk_a), wheel_hub_y + wheel_r * 0.9 * math.sin(spk_a), wz),
                       (wx - wheel_r * 0.9 * math.cos(spk_a), wheel_hub_y - wheel_r * 0.9 * math.sin(spk_a), wz),
                       radius=0.005, mat='RailIron', vertices=6)

    # Chassis Underframe: Iron channel beams
    for cz_side in (-0.30, 0.30):
        b.beam(f'TrolleyChassisLong_{cz_side:.2f}',
               (-22.68, trolley_bed_y - 0.03, trolley_cz + cz_side),
               (-21.32, trolley_bed_y - 0.03, trolley_cz + cz_side),
               radius=0.018, mat='RailIron')

    # Slatted Walnut Load Deck (length 1.36m, width 0.72m, thickness 0.03m)
    b.box('TrolleyLoadDeck', (trolley_cx, trolley_bed_y, trolley_cz),
          (1.36, 0.032, 0.72), 'RailWalnut', bevel=0.004)

    # Upright Iron End Gate & Curved Push Handle at x = -22.65
    gate_x = -22.65
    b.beam('TrolleyGatePostL',
           (gate_x, trolley_bed_y, trolley_cz - 0.33),
           (gate_x, trolley_bed_y + 0.54, trolley_cz - 0.33),
           radius=0.014, mat='RailIron')
    b.beam('TrolleyGatePostR',
           (gate_x, trolley_bed_y, trolley_cz + 0.33),
           (gate_x, trolley_bed_y + 0.54, trolley_cz + 0.33),
           radius=0.014, mat='RailIron')
    b.beam('TrolleyPushHandle',
           (gate_x, trolley_bed_y + 0.54, trolley_cz - 0.33),
           (gate_x, trolley_bed_y + 0.54, trolley_cz + 0.33),
           radius=0.014, mat='RailIron')
    b.beam('TrolleyGateMidRail',
           (gate_x, trolley_bed_y + 0.26, trolley_cz - 0.33),
           (gate_x, trolley_bed_y + 0.26, trolley_cz + 0.33),
           radius=0.010, mat='RailIron')

    # Believable 1931 Leather Suitcases stacked on the trolley (max 2m extent)
    # 1. Large Brown Traveling Trunk (bottom)
    b.box('LuggageTrunk_Bottom', (-21.75, trolley_bed_y + 0.14, trolley_cz),
          (0.76, 0.25, 0.48), 'RailLeather', bevel=0.005)
    # Brass corner caps on trunk
    for tc_x in (-0.37, 0.37):
        for tc_y in (-0.11, 0.11):
            for tc_z in (-0.23, 0.23):
                b.box(f'TrunkBrassCorner_{tc_x:.2f}_{tc_y:.2f}_{tc_z:.2f}',
                      (-21.75 + tc_x, trolley_bed_y + 0.14 + tc_y, trolley_cz + tc_z),
                      (0.04, 0.04, 0.04), 'RailBrass', bevel=0.001)
    # Leather binding straps around trunk
    for st_x in (-0.18, 0.18):
        b.box(f'TrunkLeatherStrap_{st_x:.2f}',
              (-21.75 + st_x, trolley_bed_y + 0.14, trolley_cz),
              (0.035, 0.255, 0.485), 'RailLeather', bevel=0.001)

    # 2. Medium Burgundy Red Leather Suitcase (stacked on top of trunk)
    b.box('Suitcase_MediumRed', (-21.75, trolley_bed_y + 0.35, trolley_cz),
          (0.58, 0.16, 0.36), 'RailRed', bevel=0.004)
    # Leather handle & brass locks
    b.box('SuitcaseRed_Handle', (-21.75, trolley_bed_y + 0.44, trolley_cz),
          (0.14, 0.035, 0.02), 'RailRed', bevel=0.002)
    for lk_x in (-0.16, 0.16):
        b.box(f'SuitcaseRed_Lock_{lk_x:.2f}',
              (-21.75 + lk_x, trolley_bed_y + 0.35, trolley_cz - 0.182),
              (0.035, 0.040, 0.015), 'RailBrass', bevel=0.001)

    # 3. Small Dark Green Leather Case standing upright against the trolley gate
    b.box('Suitcase_SmallGreen', (-22.35, trolley_bed_y + 0.24, trolley_cz),
          (0.28, 0.44, 0.50), 'RailGreen', bevel=0.004)
    b.box('SuitcaseGreen_Handle', (-22.35, trolley_bed_y + 0.47, trolley_cz),
          (0.025, 0.035, 0.14), 'RailWalnut', bevel=0.002)

    print(">>> Station B details added successfully.")


# ===========================================================================
# 2. TRAIN SET DETAILS
# ===========================================================================

def add_train_details(b):
    """Add authentic 1931 train set details: locomotive, tender, and coaches."""
    print(">>> Adding TrainSet details...")

    # -----------------------------------------------------------------------
    # 2.1 LOCOMOTIVE (Module_Loco_Detail)
    # -----------------------------------------------------------------------
    b.set_module('Module_Loco_Detail')

    # Boiler Bands: 5 Polished Brass Rings encircling the boiler (radius 0.785m, axis along X)
    # Boiler is centered at y = 1.95, z = 0.0, from x = 16.5 to 24.2
    boiler_band_xs = [17.5, 18.8, 20.1, 21.6, 22.8]
    for idx, bx in enumerate(boiler_band_xs):
        b.ring(f'LocoBoilerBand_{idx}', (bx, 1.95, 0.0),
               radius=0.785, thickness=0.012, mat='RailBrass', axis=(1, 0, 0),
               segments=20, ring_segs=6)

    # Boiler Flank Walkplate Handrails & Stanchions (along boiler sides at z = +-0.88, y = 2.10)
    for side_name, hz in (('L', 0.88), ('R', -0.88)):
        # Longitudinal safety handrail
        b.beam(f'LocoHandrail_{side_name}',
               (16.85, 2.10, hz), (24.00, 2.10, hz),
               radius=0.014, mat='RailBrass')
        # Radial mounting stanchions connecting boiler to handrail
        for sx in (17.2, 19.4, 21.6, 23.8):
            b.beam(f'LocoHandrailStanchion_{side_name}_{sx:.1f}',
                   (sx, 2.10, hz * 0.88), (sx, 2.10, hz),
                   radius=0.010, mat='RailBrass')

    # Curved Smokebox Front Handrail across Smokebox Door (x = 24.28)
    b.beam('LocoSmokeboxHandrail_Top',
           (24.28, 2.30, -0.40), (24.28, 2.30, 0.40),
           radius=0.012, mat='RailBrass')
    b.beam('LocoSmokeboxHandrail_L',
           (24.28, 2.30, 0.40), (24.28, 1.70, 0.65),
           radius=0.012, mat='RailBrass')
    b.beam('LocoSmokeboxHandrail_R',
           (24.28, 2.30, -0.40), (24.28, 1.70, -0.65),
           radius=0.012, mat='RailBrass')

    # Cab Roof Steam Whistle & Injector Piping
    b.cylinder('LocoSteamWhistle', (16.78, 3.76, 0.22),
               0.026, 0.16, 'RailBrass', axis=(0, 1, 0), vertices=10)
    b.cylinder('LocoWhistleBase', (16.78, 3.66, 0.22),
               0.040, 0.04, 'RailBrass', axis=(0, 1, 0), vertices=10)

    # Cab Doorway Vertical Brass Grab Irons (x = 14.85, z = +-1.32)
    for gz_side, gz in (('L', 1.32), ('R', -1.32)):
        b.beam(f'LocoCabGrabIron_{gz_side}',
               (14.85, 1.15, gz), (14.85, 2.35, gz),
               radius=0.014, mat='RailBrass')

    # Mechanical Lubricator Box on Running Plate
    b.box('LocoLubricatorBox', (21.40, 1.42, -1.05),
          (0.28, 0.18, 0.16), 'RailBrass', bevel=0.003)
    b.cylinder('LocoLubricatorDriveWheel', (21.40, 1.42, -1.14),
               0.055, 0.02, 'RailIron', axis=(0, 0, 1), vertices=10)


    # -----------------------------------------------------------------------
    # 2.2 TENDER (Module_Tender_Detail)
    # -----------------------------------------------------------------------
    b.set_module('Module_Tender_Detail')

    # Authentic Irregular Coal Lumps piled on the Tender Bunker
    # Existing TenderCoalHeap is at (13.0, 2.35, 0.0), size (3.4, 0.95, 2.45).
    # Pile irregular coal lumps along the bunker crest (y in [2.65, 3.05], x in [11.6, 14.4])
    coal_heap_lumps = [
        ((12.20, 2.86, -0.45), (0.42, 0.22, 0.38)),
        ((12.70, 2.92, 0.25), (0.48, 0.24, 0.44)),
        ((13.20, 2.98, -0.20), (0.52, 0.26, 0.46)),
        ((13.80, 2.88, 0.35), (0.46, 0.22, 0.40)),
        ((12.00, 2.80, 0.50), (0.38, 0.18, 0.35)),
        ((14.10, 2.76, -0.50), (0.40, 0.20, 0.36)),
        ((12.60, 2.90, -0.70), (0.35, 0.18, 0.32)),
        ((13.50, 2.94, 0.65), (0.36, 0.20, 0.34)),
    ]
    for c_i, (c_pos, c_size) in enumerate(coal_heap_lumps):
        b.box(f'TenderCoalLump_{c_i}', c_pos, c_size, 'RailCoal', bevel=0.008)

    # Tender Tank Vertical Stiffener Straps (along tank sides at z = +-1.36)
    # Plain iron straps were replaced by lined panels (rail_train_beautify).

    # Tender Rear Backup Lantern (facing -X at x = 7.95, y = 2.40, z = 0.0)
    b.cylinder('TenderBackupLampBody', (7.95, 2.40, 0.0),
               0.14, 0.20, 'RailBrass', axis=(-1, 0, 0), vertices=14)
    b.cylinder('TenderBackupLampLens', (7.84, 2.40, 0.0),
               0.11, 0.02, 'RailGlow', axis=(-1, 0, 0), vertices=14)


    # -----------------------------------------------------------------------
    # 2.3 COACHES 1 & 2 (Module_Coach1_Detail / Module_Coach2_Detail)
    # -----------------------------------------------------------------------
    # Coach 1 center x = 0.0; Coach 2 center x = -16.0.
    coaches_spec = [
        ('Coach1', 'C1', 0.0, 'Module_Coach1_Detail'),
        ('Coach2', 'C2', -16.0, 'Module_Coach2_Detail'),
    ]

    for c_long_name, c_short, c_cx, mod_name in coaches_spec:
        b.set_module(mod_name)

        # -------------------------------------------------------------------
        # Seating Polish: Curved welt piping, inset walnut backs, brass armrests
        # -------------------------------------------------------------------
        # Seat bays at bx_off in [-4.5, -2.7, -0.9, 0.9, 2.7]
        seat_bays = [-4.5, -2.7, -0.9, 0.9, 2.7]
        for bay_idx, bx_off in enumerate(seat_bays):
            bx = c_cx + bx_off
            # For both benches: Facing +X (cushion at bx - 0.45) and Facing -X (cushion at bx + 0.45)
            # South side: z = -0.92; North side: z = 0.92
            for f_dir, cx_off, back_off, back_thick_sign in (('F', -0.45, -0.68, -1), ('R', 0.45, 0.68, 1)):
                bench_x = bx + cx_off
                back_x = bx + back_off
                for side_name, bz in (('S', -0.92), ('N', 0.92)):
                    # 1. Seat Cushion Welt Piping (Ivory cord along cushion perimeter at y = 1.53)
                    # Cushion is 0.44m wide (along X) and 0.80m deep (along Z)
                    b.beam(f'{c_short}_PipingFront_{f_dir}_{bay_idx}_{side_name}',
                           (bench_x - back_thick_sign * 0.22, 1.53, bz - 0.40),
                           (bench_x - back_thick_sign * 0.22, 1.53, bz + 0.40),
                           radius=0.007, mat='RailIvory')
                    b.beam(f'{c_short}_PipingAisle_{f_dir}_{bay_idx}_{side_name}',
                           (bench_x - 0.22, 1.53, bz + (0.40 if side_name == 'S' else -0.40)),
                           (bench_x + 0.22, 1.53, bz + (0.40 if side_name == 'S' else -0.40)),
                           radius=0.007, mat='RailIvory')

                    # 2. Inset Walnut Backrest Outer Frame & Moulding
                    # Sits immediately behind the felt back cushion at back_x
                    walnut_back_x = back_x + back_thick_sign * 0.035
                    b.box(f'{c_short}_WalnutBack_{f_dir}_{bay_idx}_{side_name}',
                          (walnut_back_x, 1.90, bz),
                          (0.025, 0.82, 0.82), 'RailWalnut', bevel=0.003)
                    # Top curved walnut headrest cap rail
                    b.box(f'{c_short}_WalnutBackCap_{f_dir}_{bay_idx}_{side_name}',
                          (walnut_back_x - back_thick_sign * 0.01, 2.32, bz),
                          (0.045, 0.05, 0.82), 'RailWalnut', bevel=0.002)

                    # 3. Aisle-Side Armrest Support & Polished Brass Armrest Cap
                    # Aisle edge is at z = -0.52 (for S) and z = 0.52 (for N).
                    # Notice: abs(z) = 0.52 > 0.40, safely OUTSIDE the 0.8m central aisle!
                    arm_z = -0.52 if side_name == 'S' else 0.52
                    # Vertical support post from floor y = 1.05 to arm height y = 1.74
                    b.box(f'{c_short}_ArmSupport_{f_dir}_{bay_idx}_{side_name}',
                          (bench_x, 1.40, arm_z),
                          (0.05, 0.65, 0.05), 'RailWalnut', bevel=0.002)
                    # Polished Brass Armrest Cap Plate at y = 1.74
                    b.box(f'{c_short}_ArmBrassCap_{f_dir}_{bay_idx}_{side_name}',
                          (bench_x, 1.74, arm_z),
                          (0.36, 0.022, 0.06), 'RailBrass', bevel=0.002)

        # Dedicated Seating Row at c_cx + 3.80
        for side_name, bz in (('S', -0.92), ('N', 0.92)):
            ded_x = c_cx + 3.80
            ded_back_x = c_cx + 3.55
            # Piping
            b.beam(f'{c_short}_DedPipingFront_{side_name}',
                   (ded_x + 0.23, 1.53, bz - 0.40),
                   (ded_x + 0.23, 1.53, bz + 0.40),
                   radius=0.007, mat='RailIvory')
            # Walnut back
            b.box(f'{c_short}_DedWalnutBack_{side_name}',
                  (ded_back_x - 0.035, 1.90, bz),
                  (0.025, 0.82, 0.82), 'RailWalnut', bevel=0.003)
            # Armrest at abs(z) = 0.52
            arm_z = -0.52 if side_name == 'S' else 0.52
            b.box(f'{c_short}_DedArmSupport_{side_name}',
                  (ded_x, 1.40, arm_z),
                  (0.05, 0.65, 0.05), 'RailWalnut', bevel=0.002)
            b.box(f'{c_short}_DedArmBrassCap_{side_name}',
                  (ded_x, 1.74, arm_z),
                  (0.36, 0.022, 0.06), 'RailBrass', bevel=0.002)

        # -------------------------------------------------------------------
        # Brass Window Sash Catches at bottom sill rail (keep y = 2.70 clear!)
        # -------------------------------------------------------------------
        # Window centers within coach: c_cx + [-4.5, -2.7, -0.9, 0.9, 2.7, 4.5]
        # Sash catch placed on lower window rail at y = 1.86, z = +-1.42 (keeps y=2.70 completely clear!)
        win_offsets = [-4.5, -2.7, -0.9, 0.9, 2.7, 4.5]
        for w_idx, w_off in enumerate(win_offsets):
            wx = c_cx + w_off
            for side_name, wz in (('S', -1.42), ('N', 1.42)):
                b.box(f'{c_short}_WinCatch_{w_idx}_{side_name}',
                      (wx, 1.86, wz),
                      (0.14, 0.025, 0.03), 'RailBrass', bevel=0.002)

        # -------------------------------------------------------------------
        # Lattice Luggage Nets & Placed Items ABOVE outer seat bays only
        # -------------------------------------------------------------------
        # Luggage racks span x in [c_cx - 4.9, c_cx + 3.2], y = 2.65, z = +-1.25.
        # Transverse crossbars spaced every 0.22m across outer seat bays only.
        for side_name, rz_wall, rz_inner in (('S', -1.41, -1.09), ('N', 1.41, 1.09)):
            # Longitudinal Front Guard Rail (tubular brass)
            b.beam(f'{c_short}_RackFrontRail_{side_name}',
                   (c_cx - 4.85, 3.23, rz_inner),
                   (c_cx + 4.9, 3.23, rz_inner),
                   radius=0.010, mat='RailBrass')
            # Repeating Transverse Crossbars
            crossbar_xs = [c_cx - 4.8 + i * 0.22 for i in range(45)]
            for cb_idx, cb_x in enumerate(crossbar_xs):
                b.beam(f'{c_short}_RackCrossbar_{side_name}_{cb_idx}',
                       (cb_x, 3.21, rz_wall),
                       (cb_x, 3.21, rz_inner),
                       radius=0.004, mat='RailBrass', vertices=6)
            # Scrolled Brass Wall Support Brackets at window posts
            for post_x in (c_cx - 3.60, c_cx - 1.80, c_cx, c_cx + 1.80):
                b.beam(f'{c_short}_RackBracket_{side_name}_{post_x:.1f}',
                       (post_x, 3.05, rz_wall),
                       (post_x, 3.21, rz_inner),
                       radius=0.009, mat='RailBrass')

        # Placed luggage items on racks (in Coach 1 only to keep variety)
        if c_short == 'C1':
            # 1. Rolled Fabric Travel Roll at (c_cx - 2.70, 3.29, -1.25)
            b.cylinder('C1_TravelRoll', (c_cx - 2.70, 3.29, -1.25),
                       0.08, 0.52, 'RailFelt', axis=(1, 0, 0), vertices=12)
            for str_x in (c_cx - 2.85, c_cx - 2.55):
                b.ring(f'C1_TravelRollStrap_{str_x:.2f}', (str_x, 3.29, -1.25),
                       radius=0.082, thickness=0.008, mat='RailWalnut', axis=(1, 0, 0),
                       segments=12, ring_segs=6)
            # 2. Small Walnut Leather Case at (c_cx + 0.90, 3.28, -1.25)
            b.box('C1_LeatherCase_Walnut', (c_cx + 0.90, 3.28, -1.25),
                  (0.48, 0.14, 0.26), 'RailLeather', bevel=0.003)
            b.box('C1_LeatherCase_WalnutLock', (c_cx + 0.90, 3.28, -1.115),
                  (0.04, 0.03, 0.015), 'RailBrass', bevel=0.001)
            # 3. Small Red Leather Case at (c_cx - 0.90, 3.28, 1.25)
            b.box('C1_LeatherCase_Red', (c_cx - 0.90, 3.28, 1.25),
                  (0.44, 0.15, 0.24), 'RailRed', bevel=0.003)
            b.box('C1_LeatherCase_RedLock', (c_cx - 0.90, 3.28, 1.125),
                  (0.04, 0.03, 0.015), 'RailBrass', bevel=0.001)

        # -------------------------------------------------------------------
        # Subtle Ivory Ceiling Ribs following original ellipse formula
        # y = 3.365 + 0.34 * sqrt(max(0, 1 - (z/1.515)^2)), z in [-1.4, 1.4].
        # Ribbon thickness 0.02. Must stay above y = 3.30 in central aisle (|z| < 0.40).
        # (At z=0: y=3.705m. Underside is at 3.685m > 3.30m!).
        # -------------------------------------------------------------------
        rib_xs = [c_cx - 5.30, c_cx - 3.60, c_cx - 1.80, c_cx, c_cx + 1.80, c_cx + 3.60, c_cx + 5.30]
        for rib_idx, rx in enumerate(rib_xs):
            # Authored mesh ribbon spanning across the curved vault
            num_z_steps = 16
            rib_pts = []
            rib_faces = []
            rib_half_w = 0.022  # ribbon width 0.044m along X
            for z_i in range(num_z_steps + 1):
                t = z_i / float(num_z_steps)
                z_val = -1.40 + t * 2.80
                z_norm = z_val / 1.515
                radicand = max(0.0, 1.0 - z_norm * z_norm)
                y_outer = 3.365 + 0.34 * math.sqrt(radicand)
                y_inner = y_outer - 0.020  # ribbon thickness 0.02m

                # 4 vertices per z-station: [outer_left, outer_right, inner_right, inner_left]
                v_base = z_i * 4
                rib_pts.extend([
                    (rx - rib_half_w, y_outer, z_val),
                    (rx + rib_half_w, y_outer, z_val),
                    (rx + rib_half_w, y_inner, z_val),
                    (rx - rib_half_w, y_inner, z_val)
                ])

            for z_i in range(num_z_steps):
                v0 = z_i * 4
                v1 = (z_i + 1) * 4
                # Underside face (facing downwards into cabin)
                rib_faces.append((v0 + 3, v0 + 2, v1 + 2, v1 + 3))
                # Left side face
                rib_faces.append((v0, v0 + 3, v1 + 3, v1))
                # Right side face
                rib_faces.append((v0 + 2, v0 + 1, v1 + 1, v1 + 2))

            b.authored_mesh(f'{c_short}_CeilingRib_{rib_idx}',
                            rib_pts, rib_faces, 'RailIvory')

        # -------------------------------------------------------------------
        # Layered Brass / Milky Pendant Trim around existing pendants
        # Existing pendants at lx in (-4.0, -1.5, 1.5, 4.0), stem y=3.38, glow y=3.25
        # Add brass ceiling rosette at y = 3.68, stepped gallery collar at y = 3.32,
        # and open brass equator hoop at y = 3.25 (keeps glass visible, maintains aisle headroom).
        # -------------------------------------------------------------------
        lamp_lxs = [-4.0, -1.5, 1.5, 4.0]
        for lx in lamp_lxs:
            lamp_x = c_cx + lx
            # Stepped Brass / Ivory Ceiling Rosette at ceiling attachment
            b.ring(f'{c_short}_LampRosette_{lx:.1f}', (lamp_x, 3.68, 0.0),
                   radius=0.18, thickness=0.012, mat='RailBrass', axis=(0, 1, 0),
                   segments=16, ring_segs=6)
            # Stepped Brass Gallery Collar above the glass shade (y = 3.32)
            b.ring(f'{c_short}_LampGallery_{lx:.1f}', (lamp_x, 3.32, 0.0),
                   radius=0.13, thickness=0.010, mat='RailBrass', axis=(0, 1, 0),
                   segments=16, ring_segs=6)
            # Open Brass Equator Hoop Ring framing the milky glass (leaving glow unobstructed)
            b.ring(f'{c_short}_LampEquatorRing_{lx:.1f}', (lamp_x, 3.25, 0.0),
                   radius=0.125, thickness=0.007, mat='RailBrass', axis=(0, 1, 0),
                   segments=16, ring_segs=6)

        # -------------------------------------------------------------------
        # Reachable original rotary light control, on the solid vestibule wall pier.
        b.set_module(f'Module_{c_short}_LightSwitch')
        b.box(f'{c_short}_SwitchPlate',(c_cx+6.55,2.16,-1.405),(.14,.19,.014),'RailBrass',bevel=.003)
        b.cylinder(f'{c_short}_SwitchBase',(c_cx+6.55,2.16,-1.391),.048,.020,'RailIron',axis=(0,0,1),vertices=16)
        knob=b.empty(f'SwitchKnob_{c_short}',(c_cx+6.55,2.16,-1.37))
        b.box(f'{c_short}_SwitchGrip',(c_cx+6.55,2.16,-1.37),(.018,.064,.016),'RailIvory',bevel=.003,parent=knob)
        b.set_module(mod_name)

        # Exterior Coach Details (Waistline trim, roof fascia, underframe brakes)
        # -------------------------------------------------------------------
        # Polished Brass Waistline Molding at y = 1.80 (skipping doorway openings at c_cx +- 5.9)
        # Sits at z = +-1.492 (42mm relief outside walls)
        for side_name, wz in (('S', -1.492), ('N', 1.492)):
            # Mid passenger section (between doors): x in [c_cx - 5.35, c_cx + 5.35]
            b.box(f'{c_short}_WaistlineMid_{side_name}', (c_cx, 1.80, wz),
                  (10.70, 0.035, 0.015), 'RailBrass', bevel=0.001)
            # Rear corner section: x in [c_cx - 7.40, c_cx - 6.45]
            b.box(f'{c_short}_WaistlineRear_{side_name}', (c_cx - 6.92, 1.80, wz),
                  (0.95, 0.035, 0.015), 'RailBrass', bevel=0.001)
            # Front corner section: x in [c_cx + 6.45, c_cx + 7.40]
            b.box(f'{c_short}_WaistlineFront_{side_name}', (c_cx + 6.92, 1.80, wz),
                  (0.95, 0.035, 0.015), 'RailBrass', bevel=0.001)

            # Roof Eaves Rain Gutter Trim at y = 3.52
            b.box(f'{c_short}_RoofGutter_{side_name}', (c_cx, 3.52, wz),
                  (14.80, 0.025, 0.018), 'RailIron', bevel=0.001)

        # Underframe Bogie Suspension Details (NOT parented to wheel empties!)
        # Bogies are centered at c_cx - 4.90 and c_cx + 4.90
        for b_idx, bx in enumerate((c_cx - 4.90, c_cx + 4.90)):
            # Leaf Spring Assemblies above axle journals (at bx +- 0.85, y = 0.55, z = +-1.08)
            for ax_off in (-0.85, 0.85):
                for sz_name, sz in (('L', 1.08), ('R', -1.08)):
                    # Semi-elliptical leaf spring bundle
                    b.box(f'{c_short}_LeafSpring_{b_idx}_{ax_off:.2f}_{sz_name}',
                          (bx + ax_off, 0.66, sz),
                          (0.42, 0.045, 0.06), 'RailIron', bevel=0.003)
                    # Axle Journal Box
                    b.box(f'{c_short}_JournalBox_{b_idx}_{ax_off:.2f}_{sz_name}',
                          (bx + ax_off, 0.58, sz),
                          (0.12, 0.12, 0.08), 'RailIron', bevel=0.003)

        # Underframe Air Brake Cylinder Reservoir (under center chassis)
        b.cylinder(f'{c_short}_BrakeCylinder', (c_cx, 0.69, 0.45),
                   0.16, 1.15, 'RailIron', axis=(1, 0, 0), vertices=12)
        b.beam(f'{c_short}_BrakePipe',
               (c_cx - 3.5, 0.835, 0.45), (c_cx + 3.5, 0.835, 0.45),
               radius=0.014, mat='RailIron')


    # -----------------------------------------------------------------------
    # 2.4 GUARD / LUGGAGE VAN (Module_Luggage_Detail)
    # -----------------------------------------------------------------------
    b.set_module('Module_Luggage_Detail')
    van_cx = -29.0

    # Exterior Vertical Iron Reinforcement Straps along Wooden Body Walls (z = +-1.49)
    van_strap_xs = [van_cx - 4.0, van_cx - 3.0, van_cx - 1.2, van_cx + 1.2, van_cx + 3.0, van_cx + 4.0]
    for side_name, wz in (('S', -1.49), ('N', 1.49)):
        for idx, vx in enumerate(van_strap_xs):
            b.box(f'LuggageStrap_{side_name}_{idx}',
                  (vx, 2.30, wz),
                  (0.05, 2.40, 0.025), 'RailIron', bevel=0.002)

        # Sliding Cargo Door Top Iron Track & Hanger Rollers at y = 3.25
        b.beam(f'LuggageDoorTrack_{side_name}',
               (van_cx - 1.2, 3.26, wz - (0.04 if side_name == 'S' else -0.04)),
               (van_cx + 1.2, 3.26, wz - (0.04 if side_name == 'S' else -0.04)),
               radius=0.016, mat='RailIron')
        for r_x in (van_cx - 0.7, van_cx + 0.7):
            b.ring(f'LuggageDoorRoller_{side_name}_{r_x:.1f}',
                   (r_x, 3.26, wz - (0.04 if side_name == 'S' else -0.04)),
                   radius=0.035, thickness=0.010, mat='RailIron', axis=(0, 0, 1),
                   segments=10, ring_segs=6)

    # Guard Lookout Ducket / Cupola Brass Window Beading (at x = van_cx + 3.0)
    for dz in (-1.58, 1.58):
        b.ring(f'GuardDucketBeading_{dz:.1f}', (-26.0, 2.50, dz),
               radius=0.38, thickness=0.010, mat='RailBrass', axis=(0, 0, 1),
               segments=16, ring_segs=6)

    # Underframe Luggage Van Brake Gear
    b.cylinder('LuggageBrakeReservoir', (van_cx, 0.70, -0.40),
               0.15, 0.95, 'RailIron', axis=(1, 0, 0), vertices=12)

    print(">>> TrainSet details added successfully.")
