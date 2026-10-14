"""Preserve one Tripo humanoid export and prepare a reusable period character.

Requires an already skinned FBX with real Idle, Walk and Talk actions. Original downloads are
immutable; derived assets go to this character's own source and Unity folders.
"""
import argparse
import hashlib
import json
import re
import shutil
import stat
import subprocess
import zipfile
from pathlib import Path

from PIL import Image, ImageOps

ROOT = Path(__file__).resolve().parents[2]
HEIGHTS = {"Lawyer": 1.80, "CustodyGuard": 1.83, "DutyOfficer": 1.65,
           "PatrolOfficer": 1.76, "ReceptionOfficer": 1.60}


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def preserve_archive(archive, original):
    signature = digest(archive)
    saved = original / (archive.stem + "-" + signature[:12] + archive.suffix)
    original.mkdir(parents=True, exist_ok=True)
    if not saved.exists():
        shutil.copy2(archive, saved)
    unpacked = original / ("export-" + signature[:12])
    with zipfile.ZipFile(saved) as bundle:
        for entry in bundle.infolist():
            target = (unpacked / entry.filename).resolve()
            if not target.is_relative_to(unpacked.resolve()) or stat.S_ISLNK(entry.external_attr >> 16):
                raise ValueError("Unsafe archive member: " + entry.filename)
        bundle.extractall(unpacked)
    return saved, unpacked, signature


def texture(files, *tokens, slot=None):
    matches = [p for p in files if any(token in p.stem.lower().replace("_", "") for token in tokens)]
    # Tripo's image-to-model FBX export names its atlas pages tripo_image_<id>_0_<slot>:
    # slot 0 is base colour and slot 3 the tangent normal (checked against the 2026-10-13 exports).
    if not matches and slot is not None:
        matches = [p for p in files if re.fullmatch(r"tripo_image_[0-9a-f-]+_0_" + str(slot), p.stem.lower())]
    unique = {digest(p): p for p in matches}
    if len(unique) > 1:
        raise ValueError("Several distinct " + "/".join(tokens) + " textures; export one shared atlas or choose a single model bundle: " + str(matches))
    return next(iter(unique.values()), None)


def prepare_textures(folder, art):
    files = [p for p in folder.rglob("*") if p.suffix.lower() in (".png", ".jpg", ".jpeg", ".tga")]
    base = texture(files, "basecolor", "albedo", "diffuse", slot=0)
    normal = texture(files, "normal", slot=3)
    metallic = texture(files, "metallic", "metalness")
    rough = texture(files, "roughness")
    if base is None:
        raise ValueError("No BaseColor/albedo texture in export; export the FBX with its texture folder.")
    colour = Image.open(base).convert("RGB"); colour.thumbnail((2048, 2048), Image.Resampling.LANCZOS)
    art.mkdir(parents=True, exist_ok=True); colour.save(art / "BaseColor.png")
    size = colour.size
    norm = Image.open(normal).convert("RGB").resize(size, Image.Resampling.LANCZOS) if normal else Image.new("RGB", size, (128, 128, 255))
    norm.save(art / "Normal.png")
    metal = Image.open(metallic).convert("L").resize(size, Image.Resampling.LANCZOS) if metallic else Image.new("L", size, 0)
    roughness = Image.open(rough).convert("L").resize(size, Image.Resampling.LANCZOS) if rough else Image.new("L", size, 179)
    Image.merge("RGBA", (metal, metal, metal, ImageOps.invert(roughness))).save(art / "MetallicSmoothness.png")
    return {"BaseColor": str(base), "Normal": str(normal) if normal else "flat normal fallback",
            "Metallic": str(metallic) if metallic else "nonmetal fallback", "Roughness": str(rough) if rough else "0.70 fallback",
            "size": list(size), "normalConvention": "Tripo export, same convention as existing station-clerk importer"}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--archive", type=Path, required=True)
    parser.add_argument("--blender", type=Path, required=True)
    parser.add_argument("--role", required=True)
    parser.add_argument("--cast", choices=("police", "hospital"), default="police", help="Which cast folder receives the character")
    parser.add_argument("--height", type=float)
    parser.add_argument("--model", help="Relative FBX path inside archive, required if several FBXs exist")
    for kind in ("idle", "walk", "talk"):
        parser.add_argument("--" + kind + "-action", help="Exact supplied " + kind + " action; none is synthesized")
        parser.add_argument("--" + kind + "-range", nargs=2, type=float, metavar=("START", "END"), help="Selected cycle in source frames")
    parser.add_argument("--root", type=Path, default=ROOT)
    parser.add_argument("--asset-id", default="", help="Tripo asset identity for provenance")
    parser.add_argument("--skip-renders", action="store_true", help="Diagnostic export only; not visual acceptance")
    parser.add_argument("--fixture-label", default="", help="Label an isolated test; never use a fixture as an accepted character")
    args = parser.parse_args()
    root = args.root.resolve(); height = args.height or HEIGHTS.get(args.role)
    if height is None:
        raise ValueError("Give --height for a role outside the police cast.")
    # Children of the hospital cast stand as low as 1.1 m.
    if not 1.0 <= height <= 2.1:
        raise ValueError("Character height must be metres in [1.0, 2.1].")
    source = root / "assets/source" / (args.cast + "-cast") / args.role / "tripo"
    art = root / "game/Assets/Art" / (args.cast.capitalize() + "Cast") / args.role
    review = root / "builds/cast-review" / args.role
    saved, unpacked, signature = preserve_archive(args.archive.resolve(), source / "original")
    models = sorted(unpacked.rglob("*.fbx")) + sorted(unpacked.rglob("*.FBX"))
    if args.model:
        model = (unpacked / args.model).resolve()
        if not model.is_relative_to(unpacked.resolve()) or not model.is_file():
            raise ValueError("--model must identify an FBX inside this archive.")
    elif len(models) == 1:
        model = models[0]
    else:
        raise ValueError("Choose --model from: " + str([str(p.relative_to(unpacked)) for p in models]))
    texture_report = prepare_textures(unpacked, art)
    command = [str(args.blender.resolve()), "--background", "--factory-startup", "--python-exit-code", "1",
               "--python", str(Path(__file__).with_name("prepare_period_character.py")), "--",
               "--model", str(model), "--source", str(source), "--art", str(art), "--review", str(review),
               "--role", args.role, "--height", str(height)]
    for kind in ("idle", "walk", "talk"):
        if getattr(args, kind + "_action"): command += ["--" + kind + "-action", getattr(args, kind + "_action")]
        if getattr(args, kind + "_range"): command += ["--" + kind + "-range", *map(str, getattr(args, kind + "_range"))]
    if args.skip_renders: command += ["--skip-renders"]
    if args.fixture_label: command += ["--fixture-label", args.fixture_label]
    subprocess.run(command, cwd=root, check=True)
    report = {"role": args.role, "tripoAsset": args.asset_id, "archive": str(saved.relative_to(root)),
              "archiveSHA256": signature, "originalPreserved": True, "heightMetres": height,
              "textures": texture_report, "source": str((source / "Character.blend").relative_to(root)),
              "unity": str((art / "Character.fbx").relative_to(root)),
              "motion": str((art / "Motion.json").relative_to(root)), "fixtureLabel": args.fixture_label,
              "sourceSHA256": digest(source / "Character.blend"), "fbxSHA256": digest(art / "Character.fbx"),
              "license": "No third-party license inherited from existing characters; retain any license in original export.",
              "runtimeVerified": False, "meshSelfIntersectionVerified": False, "appearanceAccepted": False}
    (source / "import-results.json").write_text(json.dumps(report, ensure_ascii=False, indent=2) + "\n")
    print("PERIOD_CHARACTER_IMPORT_PASS", args.role, flush=True)


if __name__ == "__main__":
    main()
