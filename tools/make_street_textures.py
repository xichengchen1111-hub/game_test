#!/usr/bin/env python3
"""
make_street_textures.py

Procedural generation of standalone deterministic seamless 1931-town stone textures:
- RoadSetts: 4m tile, grey granite dressed blocks (~0.25m x 0.16m, 16x24 courses with 50% staggered running bond).
- FootwayFlags: 4m tile, warm greige sandstone flags (~0.8m x 0.6m, 5x6 courses with 50% staggered running bond).
- KerbStone: 2m tile, low-contrast grey stone grain without grid.

Outputs per texture set:
  - {Name}_Base.png: RGB sRGB diffuse albedo, broad muted colors, restrained detail, no baked lighting/paint.
  - {Name}_Normal.png: Tangent-space +Y green normal map computed via periodic height gradients.
  - {Name}_Mask.png: RGBA (R: 0, G: 255, B: 0, A: Smoothness mostly ~0.20 with damper edges up to 0.55).
  - {Name}_Height.png: Linear grayscale height/displacement field [0..255].
  - texture-manifest.json: Metadata, seeds, tile sizes in metres, channel formats, and color conventions.
"""

import argparse
import json
import math
from pathlib import Path

import numpy as np
from PIL import Image


def periodic_perlin_2d(size: int, freq_x: int, freq_y: int, rng: np.random.Generator) -> np.ndarray:
    """
    Generate seamless 2D gradient noise with exact toroidal periodic wrapping.
    freq_x and freq_y specify the number of periodic cycles across [0, 1).
    """
    freq_x = max(1, int(freq_x))
    freq_y = max(1, int(freq_y))

    # Random unit gradients on grid (freq_y, freq_x)
    angles = rng.uniform(0.0, 2.0 * math.pi, size=(freq_y, freq_x)).astype(np.float32)
    gx = np.cos(angles)
    gy = np.sin(angles)

    # Pixel center sampling coordinates
    u = (np.arange(size, dtype=np.float32) + 0.5) / float(size)
    xv, yv = np.meshgrid(u * freq_x, u * freq_y)

    x0 = np.floor(xv).astype(np.int32)
    y0 = np.floor(yv).astype(np.int32)
    fx = xv - x0
    fy = yv - y0

    x1 = (x0 + 1) % freq_x
    y1 = (y0 + 1) % freq_y
    x0 = x0 % freq_x
    y0 = y0 % freq_y

    # Quintic Hermite interpolation curve: 6t^5 - 15t^4 + 10t^3
    wx = fx * fx * fx * (fx * (fx * 6.0 - 15.0) + 10.0)
    wy = fy * fy * fy * (fy * (fy * 6.0 - 15.0) + 10.0)

    # Dot products with 4 corner gradients
    d00 = gx[y0, x0] * fx + gy[y0, x0] * fy
    d10 = gx[y0, x1] * (fx - 1.0) + gy[y0, x1] * fy
    d01 = gx[y1, x0] * fx + gy[y1, x0] * (fy - 1.0)
    d11 = gx[y1, x1] * (fx - 1.0) + gy[y1, x1] * (fy - 1.0)

    b0 = d00 + wx * (d10 - d00)
    b1 = d01 + wx * (d11 - d01)
    val = b0 + wy * (b1 - b0)
    return val


def fbm_seamless(
    size: int,
    base_fx: int,
    base_fy: int,
    octaves: int,
    persistence: float,
    rng: np.random.Generator,
) -> np.ndarray:
    """
    Sum multiple octaves of seamless periodic gradient noise.
    """
    total = np.zeros((size, size), dtype=np.float32)
    amp = 1.0
    amp_sum = 0.0
    fx = base_fx
    fy = base_fy
    for _ in range(octaves):
        total += amp * periodic_perlin_2d(size, fx, fy, rng)
        amp_sum += amp
        amp *= persistence
        fx *= 2
        fy *= 2
    return total / amp_sum


def restrained_grain(size: int, base_freq: int, octaves: int, rng: np.random.Generator) -> np.ndarray:
    """
    Restrained small stone mineral grain (not sparkly black/white noise).
    Band-limited and continuous, avoiding isolated 1-pixel spikes.
    """
    grain = fbm_seamless(size, base_freq, base_freq, octaves, 0.5, rng)
    return grain


def dist_rounded_rect(dx: np.ndarray, dy: np.ndarray, r_corner: float) -> np.ndarray:
    """
    Signed distance to boundary of a rectangle with corner rounding of radius r_corner.
    dx is distance to vertical edges, dy is distance to horizontal edges.
    """
    in_corner = (dx < r_corner) & (dy < r_corner)
    cx = r_corner - dx
    cy = r_corner - dy
    corner_dist = r_corner - np.sqrt(cx * cx + cy * cy)
    return np.where(in_corner, corner_dist, np.minimum(dx, dy))


def compute_tangent_normal(height: np.ndarray, strength: float = 6.0) -> np.ndarray:
    """
    Compute tangent-space +Y green normal map from periodic height field.
    Derivative is computed with periodic rolling, guaranteeing seamless wrapping.
    +X (tangent right) -> Red
    +Y (bitangent up)  -> Green
    +Z (normal out)    -> Blue
    Flat surface maps to (128, 128, 255).
    """
    # Central difference with periodic wrapping
    # In image arrays: row index increases downwards (-Y).
    # du: step in +X (column + 1)
    # dv: step in +Y (row - 1, since row 0 is top)
    du = (np.roll(height, -1, axis=1) - np.roll(height, 1, axis=1)) * 0.5
    dv = (np.roll(height, 1, axis=0) - np.roll(height, -1, axis=0)) * 0.5

    strength *= height.shape[0] / 1024.0
    nx = -strength * du
    ny = -strength * dv
    nz = np.ones_like(height, dtype=np.float32)

    length = np.sqrt(nx * nx + ny * ny + nz * nz)
    nx /= length
    ny /= length
    nz /= length

    r = np.clip((nx * 0.5 + 0.5) * 255.0, 0.0, 255.0).astype(np.uint8)
    g = np.clip((ny * 0.5 + 0.5) * 255.0, 0.0, 255.0).astype(np.uint8)
    b = np.clip((nz * 0.5 + 0.5) * 255.0, 0.0, 255.0).astype(np.uint8)

    normal_rgb = np.stack([r, g, b], axis=-1)
    return normal_rgb


def create_mask_map(smoothness: np.ndarray) -> np.ndarray:
    """
    Construct RGBA mask map:
    R: 0 (Metallic = 0)
    G: 255 (Ambient Occlusion = 255 / 1.0)
    B: 0 (Detail = 0)
    A: Smoothness (mostly ~0.20, damper edges up to 0.55)
    """
    h, w = smoothness.shape
    r = np.zeros((h, w), dtype=np.uint8)
    g = np.full((h, w), 255, dtype=np.uint8)
    b = np.zeros((h, w), dtype=np.uint8)
    a = np.clip(smoothness * 255.0, 0.0, 255.0).astype(np.uint8)
    return np.stack([r, g, b, a], axis=-1)


def generate_road_setts(size: int, seed: int):
    """
    RoadSetts:
    - Tile: 4.0m
    - Grey granite dressed blocks (~0.25m x 0.16m)
    - 16 columns (0.25m) x 24 courses (0.1667m), 50% staggered running bond
    - Thin natural joints, worn small bevels (not domed), broad per-stone variation,
      restrained mineral grain, no baked lighting/paint.
    """
    rng = np.random.default_rng(seed)
    tile_metres = 4.0
    nx_cols = 16
    ny_rows = 24
    wx = tile_metres / nx_cols  # 0.25m
    hy = tile_metres / ny_rows  # 0.1667m

    # Pixel coordinate grid [0, 1)
    u = (np.arange(size, dtype=np.float32) + 0.5) / float(size)
    uu, vv = np.meshgrid(u, u)

    # Seamless domain warping for natural mason dressed joint waviness
    warp_amp_uv = 0.0035 / tile_metres  # ~3.5mm warp
    warp_x = fbm_seamless(size, 4, 4, 3, 0.5, rng) * warp_amp_uv
    warp_y = fbm_seamless(size, 4, 4, 3, 0.5, rng) * warp_amp_uv

    uw = (uu + warp_x) % 1.0
    vw = (vv + warp_y) % 1.0

    # Grid mapping
    y_scaled = vw * ny_rows
    r_idx = np.floor(y_scaled).astype(np.int32) % ny_rows
    y_local = (y_scaled - np.floor(y_scaled)) * hy

    # Alternating 50% row stagger
    stagger = (r_idx % 2) * 0.5
    x_scaled = (uw * nx_cols - stagger) % nx_cols
    c_idx = np.floor(x_scaled).astype(np.int32) % nx_cols
    x_local = (x_scaled - np.floor(x_scaled)) * wx

    # Distance to stone edges
    dx = np.minimum(x_local, wx - x_local)
    dy = np.minimum(y_local, hy - y_local)

    # Rounded rectangle distance (corners rounded slightly: 12mm radius)
    r_corner = 0.012
    d_stone = dist_rounded_rect(dx, dy, r_corner)

    # Joint & Bevel specifications
    w_joint = 0.005  # half joint: 5mm (total joint ~10mm)
    w_bevel = 0.014  # bevel width: 14mm

    # Smooth worn bevel profile (smoothstep transition, NOT domed)
    t_bevel = np.clip((d_stone - w_joint) / w_bevel, 0.0, 1.0)
    blend_bevel = t_bevel * t_bevel * (3.0 - 2.0 * t_bevel)

    # Per-stone pseudorandom properties (Nx * Ny stones)
    stone_rng = np.random.default_rng(seed + 101)
    per_stone_height_offset = stone_rng.uniform(-0.022, 0.022, size=(ny_rows, nx_cols)).astype(np.float32)
    per_stone_tilt_x = stone_rng.uniform(-0.012, 0.012, size=(ny_rows, nx_cols)).astype(np.float32)
    per_stone_tilt_y = stone_rng.uniform(-0.012, 0.012, size=(ny_rows, nx_cols)).astype(np.float32)
    per_stone_lum = stone_rng.uniform(-16.0, 16.0, size=(ny_rows, nx_cols)).astype(np.float32)
    per_stone_temp = stone_rng.uniform(-1.0, 1.0, size=(ny_rows, nx_cols)).astype(np.float32)  # cool vs warm

    # Map per-stone properties to pixels
    p_h_offset = per_stone_height_offset[r_idx, c_idx]
    p_tilt_x = per_stone_tilt_x[r_idx, c_idx] * ((x_local - 0.5 * wx) / wx)
    p_tilt_y = per_stone_tilt_y[r_idx, c_idx] * ((y_local - 0.5 * hy) / hy)
    p_lum = per_stone_lum[r_idx, c_idx]
    p_temp = per_stone_temp[r_idx, c_idx]

    # Broad undulating stone surface wear & restrained grain
    broad_undulation = fbm_seamless(size, 4, 4, 3, 0.5, rng) * 0.020
    fine_grain = restrained_grain(size, 32, 3, rng) * 0.016

    # Height map construction: flat top plateau with worn bevels (not domed)
    h_joint = 0.22 + fbm_seamless(size, 8, 8, 2, 0.5, rng) * 0.02
    h_plateau = 0.85 + p_h_offset + p_tilt_x + p_tilt_y + broad_undulation
    height = np.where(d_stone < w_joint, h_joint, h_joint + (h_plateau - h_joint) * blend_bevel)
    height = np.clip(height + fine_grain * blend_bevel, 0.0, 1.0)

    # Base Color construction (Grey granite)
    # Muted palette: neutral granite base ~ [116, 116, 115]
    granite_r = 116.0 + p_lum + p_temp * 4.0
    granite_g = 116.0 + p_lum
    granite_b = 115.0 + p_lum - p_temp * 5.0

    # Mineral flecks: quartz (slightly lighter), biotite (slightly darker), restrained
    quartz_flecks = np.clip(restrained_grain(size, 64, 2, rng), 0.0, 1.0) * 12.0
    biotite_flecks = np.clip(-restrained_grain(size, 64, 2, rng), 0.0, 1.0) * 15.0
    stone_color_r = granite_r + quartz_flecks - biotite_flecks
    stone_color_g = granite_g + quartz_flecks - biotite_flecks
    stone_color_b = granite_b + quartz_flecks * 0.8 - biotite_flecks

    # Mortar joint color: dark weathered sandy joint ~ [66, 63, 58]
    mortar_grain = fbm_seamless(size, 16, 16, 2, 0.5, rng) * 6.0
    mortar_r = 79.0 + mortar_grain
    mortar_g = 77.0 + mortar_grain
    mortar_b = 73.0 + mortar_grain

    # Color blending across bevel into joint
    base_r = np.clip(mortar_r * (1.0 - blend_bevel) + stone_color_r * blend_bevel, 0.0, 255.0).astype(np.uint8)
    base_g = np.clip(mortar_g * (1.0 - blend_bevel) + stone_color_g * blend_bevel, 0.0, 255.0).astype(np.uint8)
    base_b = np.clip(mortar_b * (1.0 - blend_bevel) + stone_color_b * blend_bevel, 0.0, 255.0).astype(np.uint8)
    base_rgb = np.stack([base_r, base_g, base_b], axis=-1)

    # Smoothness & Dampness Mask
    # Mostly ~0.20 on dry stone plateau; joints and damper edges up to 0.55
    moisture_field = fbm_seamless(size, 4, 4, 3, 0.5, rng)
    m_norm = (moisture_field - moisture_field.min()) / (moisture_field.max() - moisture_field.min() + 1e-6)
    damp_factor = np.clip((w_joint + w_bevel - d_stone) / w_bevel, 0.0, 1.0)
    damp_intensity = damp_factor * (0.60 + 0.40 * m_norm)

    s_base = 0.20 + fbm_seamless(size, 16, 16, 2, 0.5, rng) * 0.012
    smoothness = np.clip(s_base + damp_intensity * (0.55 - s_base), 0.18, 0.55)

    normal_rgb = compute_tangent_normal(height, strength=6.5)
    mask_rgba = create_mask_map(smoothness)
    height_gray = np.clip(height * 255.0, 0.0, 255.0).astype(np.uint8)

    return base_rgb, normal_rgb, mask_rgba, height_gray


def generate_footway_flags(size: int, seed: int):
    """
    FootwayFlags:
    - Tile: 4.0m
    - Warm greige sandstone flags (~0.8m x 0.6m)
    - 5 columns (0.80m) x 6 courses (0.6667m), 50% staggered running bond
    - Tight natural joints, worn small bevels (not domed), broad per-flag variation,
      restrained sand grain, no baked lighting/paint.
    """
    rng = np.random.default_rng(seed)
    tile_metres = 4.0
    nx_cols = 5
    ny_rows = 6
    wx = tile_metres / nx_cols  # 0.80m
    hy = tile_metres / ny_rows  # 0.6667m

    u = (np.arange(size, dtype=np.float32) + 0.5) / float(size)
    uu, vv = np.meshgrid(u, u)

    warp_amp_uv = 0.0028 / tile_metres
    warp_x = fbm_seamless(size, 3, 3, 3, 0.5, rng) * warp_amp_uv
    warp_y = fbm_seamless(size, 3, 3, 3, 0.5, rng) * warp_amp_uv

    uw = (uu + warp_x) % 1.0
    vw = (vv + warp_y) % 1.0

    y_scaled = vw * ny_rows
    r_idx = np.floor(y_scaled).astype(np.int32) % ny_rows
    y_local = (y_scaled - np.floor(y_scaled)) * hy

    stagger = (r_idx % 2) * 0.5
    x_scaled = (uw * nx_cols - stagger) % nx_cols
    c_idx = np.floor(x_scaled).astype(np.int32) % nx_cols
    x_local = (x_scaled - np.floor(x_scaled)) * wx

    dx = np.minimum(x_local, wx - x_local)
    dy = np.minimum(y_local, hy - y_local)

    r_corner = 0.016
    d_stone = dist_rounded_rect(dx, dy, r_corner)

    w_joint = 0.004  # tight joint: 4mm half-width (total ~8mm)
    w_bevel = 0.016  # worn bevel: 16mm

    t_bevel = np.clip((d_stone - w_joint) / w_bevel, 0.0, 1.0)
    blend_bevel = t_bevel * t_bevel * (3.0 - 2.0 * t_bevel)

    # Per-flag pseudorandom variations
    flag_rng = np.random.default_rng(seed + 202)
    per_flag_height_offset = flag_rng.uniform(-0.015, 0.015, size=(ny_rows, nx_cols)).astype(np.float32)
    per_flag_tilt_x = flag_rng.uniform(-0.008, 0.008, size=(ny_rows, nx_cols)).astype(np.float32)
    per_flag_tilt_y = flag_rng.uniform(-0.008, 0.008, size=(ny_rows, nx_cols)).astype(np.float32)
    per_flag_lum = flag_rng.uniform(-14.0, 14.0, size=(ny_rows, nx_cols)).astype(np.float32)
    per_flag_warmth = flag_rng.uniform(-1.0, 1.0, size=(ny_rows, nx_cols)).astype(np.float32)

    p_h_offset = per_flag_height_offset[r_idx, c_idx]
    p_tilt_x = per_flag_tilt_x[r_idx, c_idx] * ((x_local - 0.5 * wx) / wx)
    p_tilt_y = per_flag_tilt_y[r_idx, c_idx] * ((y_local - 0.5 * hy) / hy)
    p_lum = per_flag_lum[r_idx, c_idx]
    p_warmth = per_flag_warmth[r_idx, c_idx]

    # Broad bedding planes / cleft undulations & fine sand grain
    broad_bedding = fbm_seamless(size, 3, 3, 3, 0.5, rng) * 0.018
    sand_grain = restrained_grain(size, 48, 2, rng) * 0.012

    h_joint = 0.24 + fbm_seamless(size, 8, 8, 2, 0.5, rng) * 0.015
    h_plateau = 0.88 + p_h_offset + p_tilt_x + p_tilt_y + broad_bedding
    height = np.where(d_stone < w_joint, h_joint, h_joint + (h_plateau - h_joint) * blend_bevel)
    height = np.clip(height + sand_grain * blend_bevel, 0.0, 1.0)

    # Base Color construction (Warm greige sandstone)
    # Warm greige base ~ [156, 148, 136]
    greige_r = 156.0 + p_lum + p_warmth * 6.0
    greige_g = 148.0 + p_lum + p_warmth * 2.0
    greige_b = 136.0 + p_lum - p_warmth * 6.0

    sedimentary_variation = fbm_seamless(size, 6, 6, 3, 0.5, rng) * 7.0
    fine_sand_color = restrained_grain(size, 48, 2, rng) * 4.0

    flag_color_r = greige_r + sedimentary_variation + fine_sand_color
    flag_color_g = greige_g + sedimentary_variation * 0.9 + fine_sand_color
    flag_color_b = greige_b + sedimentary_variation * 0.7 + fine_sand_color * 0.8

    # Mortar joint: dark sandy brown ~ [78, 72, 64]
    mortar_grain = fbm_seamless(size, 16, 16, 2, 0.5, rng) * 5.0
    mortar_r = 101.0 + mortar_grain
    mortar_g = 96.0 + mortar_grain
    mortar_b = 87.0 + mortar_grain

    base_r = np.clip(mortar_r * (1.0 - blend_bevel) + flag_color_r * blend_bevel, 0.0, 255.0).astype(np.uint8)
    base_g = np.clip(mortar_g * (1.0 - blend_bevel) + flag_color_g * blend_bevel, 0.0, 255.0).astype(np.uint8)
    base_b = np.clip(mortar_b * (1.0 - blend_bevel) + flag_color_b * blend_bevel, 0.0, 255.0).astype(np.uint8)
    base_rgb = np.stack([base_r, base_g, base_b], axis=-1)

    # Smoothness & Dampness Mask
    moisture_field = fbm_seamless(size, 3, 3, 3, 0.5, rng)
    m_norm = (moisture_field - moisture_field.min()) / (moisture_field.max() - moisture_field.min() + 1e-6)
    damp_factor = np.clip((w_joint + w_bevel - d_stone) / w_bevel, 0.0, 1.0)
    damp_intensity = damp_factor * (0.60 + 0.40 * m_norm)

    s_base = 0.20 + fbm_seamless(size, 12, 12, 2, 0.5, rng) * 0.012
    smoothness = np.clip(s_base + damp_intensity * (0.55 - s_base), 0.18, 0.55)

    normal_rgb = compute_tangent_normal(height, strength=6.0)
    mask_rgba = create_mask_map(smoothness)
    height_gray = np.clip(height * 255.0, 0.0, 255.0).astype(np.uint8)

    return base_rgb, normal_rgb, mask_rgba, height_gray


def generate_kerb_stone(size: int, seed: int):
    """
    KerbStone:
    - Tile: 2.0m
    - Low-contrast grey stone grain without grid
    - Continuous dressed stone material, restrained mineral texture,
      smoothness mostly ~0.20 with subtle damper edges/patches up to 0.55.
    """
    rng = np.random.default_rng(seed)

    # Broad dressed stone undulation & fine tooling (no grid lines)
    broad_cloud = fbm_seamless(size, 2, 2, 4, 0.5, rng) * 0.075
    tooling_grain = fbm_seamless(size, 6, 24, 2, 0.5, rng) * 0.035  # subtle directional dressing
    fine_mineral = restrained_grain(size, 32, 3, rng) * 0.022

    # Continuous dressed stone height field
    height = 0.50 + broad_cloud + tooling_grain + fine_mineral
    height = np.clip(height, 0.0, 1.0)

    # Base Color (low-contrast grey stone)
    # Neutral dressed stone grey ~ [124, 123, 121]
    base_grey = 124.0
    lum_variation = fbm_seamless(size, 3, 3, 3, 0.5, rng) * 9.0
    warmth_shift = fbm_seamless(size, 2, 2, 2, 0.5, rng) * 3.0
    fine_grain_col = restrained_grain(size, 32, 2, rng) * 5.0

    r_col = base_grey + lum_variation + warmth_shift + fine_grain_col
    g_col = (base_grey - 1.0) + lum_variation + fine_grain_col
    b_col = (base_grey - 3.0) + lum_variation - warmth_shift + fine_grain_col * 0.9

    base_r = np.clip(r_col, 0.0, 255.0).astype(np.uint8)
    base_g = np.clip(g_col, 0.0, 255.0).astype(np.uint8)
    base_b = np.clip(b_col, 0.0, 255.0).astype(np.uint8)
    base_rgb = np.stack([base_r, base_g, base_b], axis=-1)

    # Smoothness Mask: mostly ~0.20; subtle damp patches/edges up to 0.55
    # Organic damp seepage pattern along edges and low patches
    moisture_field = fbm_seamless(size, 2, 2, 3, 0.5, rng)
    m_norm = (moisture_field - moisture_field.min()) / (moisture_field.max() - moisture_field.min() + 1e-6)
    damp_mask = np.clip((m_norm - 0.52) / 0.48, 0.0, 1.0) ** 1.4

    s_base = 0.20 + fbm_seamless(size, 16, 16, 2, 0.5, rng) * 0.012
    smoothness = np.clip(s_base + damp_mask * (0.55 - s_base), 0.18, 0.55)

    normal_rgb = compute_tangent_normal(height, strength=5.5)
    mask_rgba = create_mask_map(smoothness)
    height_gray = np.clip(height * 255.0, 0.0, 255.0).astype(np.uint8)

    return base_rgb, normal_rgb, mask_rgba, height_gray


def save_image(img_arr: np.ndarray, file_path: Path):
    """Save numpy array as PNG image."""
    mode = "RGB"
    if img_arr.ndim == 2:
        mode = "L"
    elif img_arr.shape[2] == 4:
        mode = "RGBA"
    img = Image.fromarray(img_arr, mode=mode)
    img.save(file_path, format="PNG")


def main():
    parser = argparse.ArgumentParser(
        description="Generate standalone deterministic seamless 1931-town street textures."
    )
    parser.add_argument(
        "--output-dir",
        type=str,
        default=str(Path(__file__).resolve().parents[1] / "game/Assets/Art/Street"),
        help="Target output directory for texture files and manifest.",
    )
    parser.add_argument(
        "--size",
        type=int,
        default=1024,
        help="Texture dimension in pixels (default: 1024).",
    )
    parser.add_argument(
        "--seed",
        type=int,
        default=1931,
        help="Deterministic PRNG seed (default: 1931).",
    )
    args = parser.parse_args()

    out_dir = Path(args.output_dir).resolve()
    out_dir.mkdir(parents=True, exist_ok=True)
    size = args.size
    if not 256 <= size <= 2048:
        parser.error("Size must be between 256 and 2048")
    seed = args.seed

    print(f"Generating street textures at {size}x{size} (seed={seed}) into {out_dir}...")

    # Texture specifications
    configs = [
        {
            "name": "RoadSetts",
            "tile_metres": 4.0,
            "description": "Grey granite dressed paving blocks (~0.25m x 0.16m, 16x24 courses with 50% staggered running bond)",
            "generator": generate_road_setts,
            "params": {
                "block_dimensions_metres": [0.25, 0.1667],
                "grid_columns": 16,
                "grid_rows": 24,
                "joint_width_mm": 10,
                "bevel_width_mm": 14,
                "smoothness_base": 0.20,
                "smoothness_damp_max": 0.55,
            },
        },
        {
            "name": "FootwayFlags",
            "tile_metres": 4.0,
            "description": "Warm greige sandstone flagstones (~0.8m x 0.6m, 5x6 courses with 50% staggered running bond)",
            "generator": generate_footway_flags,
            "params": {
                "flag_dimensions_metres": [0.80, 0.6667],
                "grid_columns": 5,
                "grid_rows": 6,
                "joint_width_mm": 8,
                "bevel_width_mm": 16,
                "smoothness_base": 0.20,
                "smoothness_damp_max": 0.55,
            },
        },
        {
            "name": "KerbStone",
            "tile_metres": 2.0,
            "description": "Continuous dressed grey stone grain without grid for kerb edges and curbing",
            "generator": generate_kerb_stone,
            "params": {
                "grid": "none",
                "smoothness_base": 0.20,
                "smoothness_damp_max": 0.55,
            },
        },
    ]

    manifest = {
        "generator": "make_street_textures.py",
        "seed": seed,
        "size": size,
        "era": "1931 fictional town; historical stone-street material references",
        "source": "Original procedural textures; Antigravity draft reviewed and integrated locally",
        "surfaceWeather": "Restrained dampness is an art proposal, not an asserted historical rain event",
        "color_conventions": {
            "Base": "sRGB 24-bit RGB, diffuse albedo without baked lighting, shadows, or road paint",
            "Normal": "Linear 24-bit RGB tangent-space (+X right, +Y bitangent green up, +Z normal out), normalized [0..255]",
            "Mask": "Linear 32-bit RGBA (R: 0 / Metallic=0, G: 255 / AO=1.0, B: 0 / Detail=0, A: Smoothness linear [0.0..1.0] -> [0..255])",
            "Height": "Linear 8-bit grayscale [0..255], displacement / height relative field",
        },
        "textures": {},
    }

    for item in configs:
        name = item["name"]
        print(f"  Generating {name}...")
        base_rgb, normal_rgb, mask_rgba, height_gray = item["generator"](size, seed)

        base_file = f"{name}_Base.png"
        normal_file = f"{name}_Normal.png"
        mask_file = f"{name}_Mask.png"
        height_file = f"{name}_Height.png"

        save_image(base_rgb, out_dir / base_file)
        save_image(normal_rgb, out_dir / normal_file)
        save_image(mask_rgba, out_dir / mask_file)
        save_image(height_gray, out_dir / height_file)

        manifest["textures"][name] = {
            "tile_metres": item["tile_metres"],
            "description": item["description"],
            "files": {
                "base": base_file,
                "normal": normal_file,
                "mask": mask_file,
                "height": height_file,
            },
            "parameters": item["params"],
        }

    manifest_path = out_dir / "texture-manifest.json"
    with open(manifest_path, "w", encoding="utf-8") as f:
        json.dump(manifest, f, indent=2)

    print(f"Manifest written to {manifest_path}")
    print("Done! All street textures generated successfully.")


if __name__ == "__main__":
    main()
