using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using L = ClockTowerInteriorLayout;
using Surfaces = ClockTowerInteriorSurfaces;

/// <summary>
/// Editable deterministic interior kit. The Meshy exterior is a separate imported asset.
/// Generated assets are updated in place, so a scene rebuild preserves their GUIDs.
/// </summary>
public static class ClockTowerInterior {
    const string Art = "Assets/Art/ClockTowerInterior";
    static Material dust, timber, iron, brass, darkPrint, dustyPrint, stone;

    public static L Build(Transform root) {
        L.ValidateDimensions();
        Directory.CreateDirectory(Art);
        Directory.CreateDirectory(Art + "/Meshes");
        Directory.CreateDirectory(Art + "/Materials");
        AssetDatabase.Refresh();
        var previous = root.Find("Clock tower interior");
        if (previous) UnityEngine.Object.DestroyImmediate(previous.gameObject);
        Transform kit = Group("Clock tower interior", root);
        var layout = root.GetComponent<L>() ?? root.gameObject.AddComponent<L>();
        layout.PopulateReviewPath();
        MakeMaterials();
        Surfaces.Prepare();
        Surfaces.BuildInnerWalls(kit);
        BuildGround(kit);
        BuildStaircase(kit, layout);
        BuildPlatform(kit, layout, L.MiddleFloor, L.MiddleEndAngle, false);
        BuildPlatform(kit, layout, L.MachineFloor, L.EndAngle, true);
        BuildMechanism(kit, layout);
        BuildLighting(kit);
        ClockTowerAtmosphere.Build(kit);
        ClockTowerMaterialFinish.Apply(kit);
        EditorUtility.SetDirty(layout);
        AssetDatabase.SaveAssets();
        Debug.Log($"CLOCK_TOWER_INTERIOR_BUILD_PASS: segments={L.StepCount}; lowerRise={L.Riser:F5}; upperRise={L.UpperRiser:F5}; " +
            $"clearWidth={L.ClearStairWidth:F3}; turnHeadroom={L.TurnHeadroom:F3}; " +
            $"deckHeadroom={L.DeckHeadroom:F3}; middleFloor={L.MiddleFloor:F5}; footprints={layout.footprintCount}");
        return layout;
    }

    static void MakeMaterials() {
        dust = Material("Dusty oak tread", "#898983", 0, .08f);
        timber = Material("Aged oak sides", "#514B43", 0, .12f);
        iron = Material("Oiled cast iron", "#353C40", .66f, .30f);
        brass = Material("Patinated brass", "#A8894D", .68f, .38f);
        darkPrint = Material("Dust displaced by shoes", "#72746C", 0, .08f);
        dustyPrint = Material("Sole ridges and scattered dust", "#86877F", 0, .04f);
        stone = Material("Dusty stone floor", "#85847D", 0, .10f);
        // Author a repeatable close-up dust/wood-grain map, shared across every step.
        string texturePath = Art + "/DustyOak.png";
        var texture = new Texture2D(256, 256, TextureFormat.RGB24, false);
        var pixels = new Color[256 * 256];
        for (int y = 0; y < 256; y++) for (int x = 0; x < 256; x++) {
            float grain = Mathf.PerlinNoise(x * .08f, y * .005f);
            float fine = Mathf.PerlinNoise(x * .71f + 42, y * .71f);
            float dirt = Mathf.PerlinNoise(x * .026f, y * .026f);
            float value = .84f + grain * .10f + fine * .055f - dirt * .08f;
            pixels[y * 256 + x] = new Color(value * 1.018f, value * 1.012f, value * .985f);
        }
        texture.SetPixels(pixels); texture.Apply();
        File.WriteAllBytes(texturePath, texture.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(texture);
        AssetDatabase.ImportAsset(texturePath, ImportAssetOptions.ForceUpdate);
        var importer = (TextureImporter)AssetImporter.GetAtPath(texturePath);
        importer.wrapMode = TextureWrapMode.Repeat; importer.mipmapEnabled = true;
        importer.filterMode = FilterMode.Trilinear; importer.anisoLevel = 4;
        importer.SaveAndReimport();
        dust.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath));
        EditorUtility.SetDirty(dust);
        // Small holes expose the dusty tread through each pressure mark. This
        // gives an incomplete transfer impression, rather than a flat black decal.
        string printTexturePath = Art + "/DisplacedDust.png";
        var printTexture = new Texture2D(256, 256, TextureFormat.RGBA32, false);
        var printPixels = new Color[256 * 256];
        for (int y = 0; y < 256; y++) for (int x = 0; x < 256; x++) {
            float broad = Mathf.PerlinNoise(x * .055f + 71, y * .055f + 43);
            float grain = Mathf.PerlinNoise(x * .44f, y * .44f);
            float opacity = broad * .58f + grain * .42f;
            float value = .87f + grain * .12f;
            printPixels[y * 256 + x] = new Color(value, value, value, opacity);
        }
        printTexture.SetPixels(printPixels); printTexture.Apply();
        File.WriteAllBytes(printTexturePath, printTexture.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(printTexture);
        AssetDatabase.ImportAsset(printTexturePath, ImportAssetOptions.ForceUpdate);
        var printImporter = (TextureImporter)AssetImporter.GetAtPath(printTexturePath);
        printImporter.wrapMode = TextureWrapMode.Repeat; printImporter.mipmapEnabled = true;
        printImporter.alphaSource = TextureImporterAlphaSource.FromInput;
        printImporter.filterMode = FilterMode.Trilinear; printImporter.anisoLevel = 4;
        printImporter.SaveAndReimport();
        darkPrint.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(printTexturePath));
        darkPrint.SetTextureScale("_BaseMap", new Vector2(15, 15));
        darkPrint.SetFloat("_AlphaClip", 1); darkPrint.SetFloat("_Cutoff", .38f);
        darkPrint.EnableKeyword("_ALPHATEST_ON"); darkPrint.renderQueue = (int)RenderQueue.AlphaTest;
        darkPrint.SetOverrideTag("RenderType", "TransparentCutout");
        EditorUtility.SetDirty(darkPrint);
    }

    static void BuildGround(Transform kit) {
        var floor = new Geometry();
        floor.Box(new Vector3(0, L.GroundFloor - .07f, 0), new Vector3(L.InteriorWidth, .14f, L.InteriorWidth));
        floor.ProjectHorizontal(1.16f, 1.52f);
        Create("Ground floor", floor, Surfaces.Floor, kit, true);
        var column = new Geometry();
        column.Tube(new Vector3(0, L.GroundFloor, 0), new Vector3(0, L.MachineFloor + .12f, 0), .12f, 16);
        Create("Stair central cast iron column", column, iron, kit, true);
        var basePlate = new Geometry();
        basePlate.Tube(new Vector3(0, L.GroundFloor, 0), new Vector3(0, L.GroundFloor + .05f, 0), .22f, 16);
        Create("Column foot", basePlate, iron, kit, false);
    }

    static void BuildStaircase(Transform kit, L layout) {
        var treads = new Geometry[Surfaces.BandCount]; var sides = new Geometry(); var railing = new Geometry();
        var prints = new Geometry[Surfaces.BandCount]; var printRidges = new Geometry[Surfaces.BandCount];
        for (int band = 0; band < Surfaces.BandCount; band++) {
            treads[band] = new Geometry(); prints[band] = new Geometry(); printRidges[band] = new Geometry();
        }
        var collider = new Geometry(); var outerSafety = new Geometry();
        int count = 0;
        for (int i = 0; i < L.StepCount; i++) {
            float a0 = L.StartAngle + i * L.StepAngle, a1 = a0 + L.StepAngle;
            float top = L.StepTop(i);
            int heightBand = Surfaces.Band(top);
            // The middle deck supplies these five flat segments, avoiding
            // overlapping tread faces and leaving a real standing floor.
            if (!L.IsMiddleLandingSegment(i)) AddStep(treads[heightBand], sides, collider, a0, a1, top);
            // The first two treads form the real entrance from the south door.
            // Leave their outer edge open in both geometry and collision so the
            // capsule can approach radially before turning onto the spiral.
            // Continuous guard infill starts at the third tread (-54 degrees).
            if (i >= 2)
                AnnularBarrier(outerSafety, a0, a1, 1.326f, 1.374f,
                    top - L.TreadThickness, top + 1.04f, 4);
            float middle = (a0 + a1) / 2;
            railing.Tube(L.Polar(middle, .13f, top - .15f), L.Polar(middle, 1.33f, top - .075f), .024f, 6);
            for (int railSide = 0; railSide < 2; railSide++) {
                if (railSide == 1 && i < 2) continue;
                float r = railSide == 0 ? .32f : 1.35f;
                if (i % 2 == 0 || i == L.StepCount - 1) {
                    railing.Tube(L.Polar(middle, r, top), L.Polar(middle, r, top + .98f), .017f, 6);
                    railing.Tube(L.Polar(middle, r, top + .90f), L.Polar(middle, r, top + 1.005f), .023f, 6);
                }
                for (int sub = 0; sub < 3; sub++) {
                    float f0 = sub / 3f, f1 = (sub + 1) / 3f;
                    float y0 = L.PositionHeight(i + f0 + .5f) + .98f;
                    float y1 = L.PositionHeight(i + f1 + .5f) + .98f;
                    railing.Tube(L.Polar(Mathf.Lerp(a0, a1, f0), r, y0),
                        L.Polar(Mathf.Lerp(a0, a1, f1), r, y1), .024f, 7);
                }
            }
            // Alternating left/right prints form a legible, interrupted walking trail.
            if (i % 9 != 5) {
                float radius = i % 2 == 0 ? .83f : 1.035f;
                float jitter = Mathf.Sin(i * 3.41f) * 1.8f;
                Shoe(prints[heightBand], printRidges[heightBand], L.Polar(middle, radius, top + .0035f), middle + 90 + jitter,
                    i % 2 == 0, .95f + .04f * Mathf.Sin(i));
                count++;
            }
        }
        for (int band = 0; band < Surfaces.BandCount; band++) {
            treads[band].ProjectHorizontal(1.2f, 1.2f);
            Create("Stair dust band " + band.ToString("00"), treads[band], Surfaces.Treads[band], kit, false);
        }
        Create("Oak tread risers and undersides", sides, timber, kit, false);
        var walk = Group("Continuous physical spiral stair", kit).gameObject;
        layout.staircaseCollider = walk.AddComponent<MeshCollider>();
        layout.staircaseCollider.sharedMesh = SaveMesh("Spiral stair collision", collider);
        walk.isStatic = true;
        Create("Spiral iron balusters and handrails", railing, iron, kit, false);
        CollisionOnly("Continuous outer spiral guard collision", outerSafety, kit);
        // A single continuous inner cylinder prevents squeezing through the central turn.
        var core = Group("Stair inner safety cylinder", kit).gameObject.AddComponent<CapsuleCollider>();
        core.radius = .285f; core.height = L.MachineFloor - L.GroundFloor;
        core.center = new Vector3(0, (L.MachineFloor + L.GroundFloor) / 2, 0);
        for (int band = 0; band < Surfaces.BandCount; band++) {
            var printObject = Create("Shoe impressions band " + band.ToString("00"), prints[band], Surfaces.Prints[band], kit, false);
            printObject.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            var ridgeObject = Create("Sole dust ridges band " + band.ToString("00"), printRidges[band], Surfaces.Ridges[band], kit, false);
            ridgeObject.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
        }
        layout.footprintCount = count;
    }

    static void AddStep(Geometry topMesh, Geometry sides, Geometry collision, float a0, float a1, float top) {
        const int divisions = 4;
        for (int s = 0; s < divisions; s++) {
            float b0 = Mathf.Lerp(a0, a1, s / (float)divisions), b1 = Mathf.Lerp(a0, a1, (s + 1f) / divisions);
            Vector3 p0 = L.Polar(b0, L.InnerRadius, top), p1 = L.Polar(b1, L.InnerRadius, top);
            Vector3 p2 = L.Polar(b1, L.OuterRadius, top), p3 = L.Polar(b0, L.OuterRadius, top);
            topMesh.Quad(p0, p1, p2, p3); collision.Quad(p0, p1, p2, p3);
            Vector3 drop = Vector3.down * L.TreadThickness;
            sides.Quad(p3 + drop, p2 + drop, p1 + drop, p0 + drop);
            sides.Quad(p1, p0, p0 + drop, p1 + drop);
            sides.Quad(p3, p2, p2 + drop, p3 + drop);
            collision.Quad(p3 + drop, p2 + drop, p1 + drop, p0 + drop);
            collision.Quad(p1, p0, p0 + drop, p1 + drop);
            collision.Quad(p3, p2, p2 + drop, p3 + drop);
            if (s == 0) { sides.Quad(p0, p3, p3 + drop, p0 + drop); collision.Quad(p0, p3, p3 + drop, p0 + drop); }
            if (s == divisions - 1) { sides.Quad(p2, p1, p1 + drop, p2 + drop); collision.Quad(p2, p1, p1 + drop, p2 + drop); }
        }
    }

    static void BuildPlatform(Transform kit, L layout, float floorHeight, float endAngle, bool final) {
        var floor = new Geometry();
        string label = final ? "Machine room" : "Middle landing";
        float openingStart = endAngle - L.OpenHeadroomSteps * L.StepAngle;
        float half = (final ? L.ChamberInteriorWidth : L.InteriorWidth) / 2;
        // The same 90-degree west landing is shared by both real upper floors.
        for (int i = 0; i < 180; i++) {
            float a0 = openingStart + i * 2, a1 = a0 + 2;
            bool opening = a0 < endAngle - .001f;
            float inner = opening ? L.OuterRadius + .025f : .12f;
            float r0 = half / Mathf.Max(Mathf.Abs(Mathf.Cos(a0 * Mathf.Deg2Rad)), Mathf.Abs(Mathf.Sin(a0 * Mathf.Deg2Rad)));
            float r1 = half / Mathf.Max(Mathf.Abs(Mathf.Cos(a1 * Mathf.Deg2Rad)), Mathf.Abs(Mathf.Sin(a1 * Mathf.Deg2Rad)));
            Vector3 p0 = L.Polar(a0, inner, floorHeight), p1 = L.Polar(a1, inner, floorHeight);
            Vector3 p2 = L.Polar(a1, r1, floorHeight), p3 = L.Polar(a0, r0, floorHeight);
            Vector3 drop = Vector3.down * L.TreadThickness;
            floor.Quad(p0, p1, p2, p3); floor.Quad(p3 + drop, p2 + drop, p1 + drop, p0 + drop);
            floor.Quad(p1, p0, p0 + drop, p1 + drop);
            floor.Quad(p3, p2, p2 + drop, p3 + drop);
            if (i == 0 || Mathf.Abs(a0 - endAngle) < .01f) floor.Quad(p0, p3, p3 + drop, p0 + drop);
            if (Mathf.Abs(a1 - endAngle) < .01f) floor.Quad(p2, p1, p1 + drop, p2 + drop);
        }
        floor.ProjectHorizontal(1.2f, 1.2f);
        int heightBand = Surfaces.Band(floorHeight);
        var floorCollider = Create(label + " oak floor with real stair opening", floor, Surfaces.Treads[heightBand], kit, true).GetComponent<MeshCollider>();
        if (final) layout.platformCollider = floorCollider;
        else layout.middlePlatformCollider = floorCollider;
        var rails = new Geometry();
        // The middle landing has two real stair openings, for arrival and onward
        // ascent. Only the final machine floor has a closed radial guard edge.
        if (final) {
            for (int i = 0; i <= 4; i++) {
                float r = Mathf.Lerp(.20f, 1.43f, i / 4f);
                rails.Tube(L.Polar(openingStart, r, floorHeight),
                    L.Polar(openingStart, r, floorHeight + 1.04f), .018f, 6);
            }
            foreach (float y in new[] { floorHeight + .53f, floorHeight + 1.04f })
                rails.Tube(L.Polar(openingStart, .20f, y), L.Polar(openingStart, 1.43f, y), .025f, 7);
        }
        // Protect the thin outer collar of the upper floor as well as its
        // straight closed edge; the stair arrival edge is deliberately open.
        int arcSegments = Mathf.RoundToInt((endAngle - openingStart) / 6f);
        var openingGuard = new Geometry();
        AnnularBarrier(openingGuard, openingStart, endAngle, 1.425f, 1.465f,
            floorHeight - .02f, floorHeight + 1.04f, arcSegments);
        for (int i = 0; i <= arcSegments; i++) {
            float a = Mathf.Lerp(openingStart, endAngle, i / (float)arcSegments);
            if (i % 3 == 0 || i == arcSegments)
                rails.Tube(L.Polar(a, 1.445f, floorHeight), L.Polar(a, 1.445f, floorHeight + 1.04f), .018f, 6);
            if (i == arcSegments) continue;
            float b = Mathf.Lerp(openingStart, endAngle, (i + 1f) / arcSegments);
            foreach (float y in new[] { floorHeight + .53f, floorHeight + 1.04f })
                rails.Tube(L.Polar(a, 1.445f, y), L.Polar(b, 1.445f, y), .025f, 7);
        }
        Create(label + " landing guard rails", rails, iron, kit, true);
        CollisionOnly(label + " opening outer arc safety barrier", openingGuard, kit);
        if (final) {
            var closedEdge = Group("Landing closed edge safety barrier", kit);
            closedEdge.localPosition = L.Polar(openingStart, .815f, floorHeight + .52f);
            closedEdge.localRotation = Quaternion.Euler(0, -openingStart, 0);
            closedEdge.gameObject.AddComponent<BoxCollider>().size = new Vector3(1.23f, 1.04f, .05f);
            closedEdge.gameObject.isStatic = true;
        }
        var prints = new Geometry(); var ridges = new Geometry();
        if (final) {
            Shoe(prints, ridges, new Vector3(-.73f, floorHeight + .0035f, .68f), 225, false, 1);
            Shoe(prints, ridges, new Vector3(-.63f, floorHeight + .0035f, .39f), 245, true, 1);
            Shoe(prints, ridges, new Vector3(-.70f, floorHeight + .0035f, .03f), 180, false, 1);
            layout.footprintCount += 3;
        } else {
            Shoe(prints, ridges, new Vector3(-.73f, floorHeight + .0035f, .23f), 190, true, 1);
            Shoe(prints, ridges, new Vector3(-.72f, floorHeight + .0035f, -.04f), 250, false, 1);
            layout.footprintCount += 2;
        }
        Create(label + " shoe impressions", prints, Surfaces.Prints[heightBand], kit, false).GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
        Create(label + " dust in sole ridges", ridges, Surfaces.Ridges[heightBand], kit, false).GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
    }

    static void BuildMechanism(Transform kit, L layout) {
        Transform root = Group("Traditional turret clock movement", kit);
        root.localPosition = new Vector3(L.MechanismX, L.MachineFloor, 0);
        root.localRotation = Quaternion.Euler(0, 90, 0);
        layout.machinery = root;
        var frame = new Geometry();
        foreach (float x in new[] { -.62f, .62f }) {
            frame.Box(new Vector3(x, .80f, -.05f), new Vector3(.055f, 1.6f, .22f));
            frame.Box(new Vector3(x, .05f, 0), new Vector3(.16f, .10f, .39f));
            frame.Box(new Vector3(x, 1.64f, -.05f), new Vector3(.13f, .08f, .25f));
        }
        foreach (float y in new[] { .43f, 1.52f }) frame.Box(new Vector3(0, y, -.03f), new Vector3(1.30f, .065f, .22f));
        frame.Box(new Vector3(0, .52f, -.08f), new Vector3(1.19f, .06f, .27f));
        frame.Tube(new Vector3(-.62f, .43f, -.10f), new Vector3(.62f, 1.52f, -.10f), .025f, 8);
        Create("Cast iron frame and splayed feet", frame, iron, root, false);
        root.gameObject.AddComponent<ClockTowerAdjustment>().title = "钟塔机芯";
        var guard = root.gameObject.AddComponent<BoxCollider>();
        guard.center = new Vector3(0, .79f, -.01f); guard.size = new Vector3(1.34f, 1.58f, .46f);
        var axles = new Geometry();
        Vector3[] centres = { new Vector3(-.31f, .94f, .17f), new Vector3(.1799f, 1.04f, .17f), new Vector3(.2999f, 1.3366f, .17f) };
        float[] radii = { .30f, .20f, .12f }; int[] teeth = { 30, 20, 12 };
        var moving = new Transform[3];
        for (int i = 0; i < centres.Length; i++) {
            var gear = Group("Brass gear " + teeth[i] + " teeth", root);
            gear.localPosition = centres[i];
            // Offset teeth into the neighbouring wheel's root gaps at both contact angles.
            gear.localRotation = Quaternion.Euler(0, 0, i == 1 ? 4.79f : i == 2 ? 11.72f : 0);
            moving[i] = gear;
            Create("Open spoke wheel " + teeth[i], Gear(radii[i], teeth[i], .047f), brass, gear, false).isStatic = false;
            axles.Tube(centres[i] + Vector3.back * .25f, centres[i] + Vector3.forward * .095f, .025f, 12);
            axles.Tube(centres[i] + Vector3.back * .15f, centres[i] + Vector3.back * .05f, .053f, 12);
            axles.Box(new Vector3(centres[i].x, centres[i].y, -.05f), new Vector3(.14f, .14f, .11f));
            axles.Box(new Vector3(centres[i].x, (centres[i].y + .50f) / 2, -.09f), new Vector3(.045f, centres[i].y - .50f, .06f));
        }
        Create("Steel axles and bearing blocks", axles, iron, root, false);
        var winding = new Geometry();
        winding.Tube(new Vector3(-.31f, .64f, -.08f), new Vector3(-.31f, .64f, .21f), .108f, 24);
        winding.Tube(new Vector3(-.31f, .64f, -.085f), new Vector3(-.31f, .64f, -.055f), .13f, 24);
        winding.Tube(new Vector3(-.31f, .64f, .185f), new Vector3(-.31f, .64f, .215f), .13f, 24);
        for (int i = 0; i < 11; i++) {
            float z = -.03f + i * .019f;
            winding.RingXY(new Vector3(-.31f, .64f, z), .111f, .114f, .010f, 24);
        }
        Create("Winding barrel and coiled cable", winding, brass, root, false);
        var weights = new Geometry();
        foreach (float x in new[] { -.48f, .46f }) {
            weights.Tube(new Vector3(x, .22f, -.12f), new Vector3(x, 1.49f, -.12f), .007f, 5);
            weights.Tube(new Vector3(x, .08f, -.12f), new Vector3(x, .42f, -.12f), .055f, 10);
        }
        Create("Suspended cast weights", weights, iron, root, false);
        var pivot = Group("Pendulum suspension", root); pivot.localPosition = new Vector3(.15f, 1.47f, -.095f);
        var pendulum = new Geometry();
        pendulum.Tube(Vector3.zero, new Vector3(0, -1.12f, 0), .012f, 8);
        pendulum.Tube(new Vector3(0, -1.12f, -.025f), new Vector3(0, -1.12f, .025f), .12f, 24);
        Create("Pendulum rod and brass bob", pendulum, brass, pivot, false).isStatic = false;
        var drive = new Geometry();
        drive.Tube(new Vector3(.30f, 1.34f, -.05f), new Vector3(.30f, 2.25f, -.05f), .019f, 8);
        drive.Tube(new Vector3(.30f, 2.18f, -.05f), new Vector3(.30f, 2.18f, .53f), .019f, 8);
        Create("Vertical dial transmission spindle", drive, iron, root, false);
        var motion = root.gameObject.AddComponent<ClockTowerGearMotion>();
        motion.gears = moving; motion.degreesPerSecond = new[] { 2.4f, -3.6f, 6.0f }; motion.pendulum = pivot;
    }

    static Geometry Gear(float pitchRadius, int teeth, float thickness) {
        var mesh = new Geometry(); int n = teeth * 4;
        float rootRadius = pitchRadius - .014f, tipRadius = pitchRadius + .014f, ringInner = pitchRadius * .73f;
        for (int i = 0; i < n; i++) {
            float a0 = i * Mathf.PI * 2 / n, a1 = (i + 1) * Mathf.PI * 2 / n;
            float r0 = i % 4 == 0 || i % 4 == 3 ? rootRadius : tipRadius;
            int next = (i + 1) % n;
            float r1 = next % 4 == 0 || next % 4 == 3 ? rootRadius : tipRadius;
            Vector3 p0 = new Vector3(Mathf.Cos(a0) * ringInner, Mathf.Sin(a0) * ringInner, -thickness / 2);
            Vector3 p1 = new Vector3(Mathf.Cos(a1) * ringInner, Mathf.Sin(a1) * ringInner, -thickness / 2);
            Vector3 p2 = new Vector3(Mathf.Cos(a1) * r1, Mathf.Sin(a1) * r1, -thickness / 2);
            Vector3 p3 = new Vector3(Mathf.Cos(a0) * r0, Mathf.Sin(a0) * r0, -thickness / 2);
            Vector3 dz = Vector3.forward * thickness;
            mesh.Quad(p0, p1, p2, p3); mesh.Quad(p3 + dz, p2 + dz, p1 + dz, p0 + dz);
            mesh.Quad(p1, p0, p0 + dz, p1 + dz); mesh.Quad(p3, p2, p2 + dz, p3 + dz);
        }
        for (int i = 0; i < 6; i++) {
            float a = i * Mathf.PI / 3;
            mesh.Beam(new Vector3(Mathf.Cos(a) * .035f, Mathf.Sin(a) * .035f, 0),
                new Vector3(Mathf.Cos(a) * (ringInner + .012f), Mathf.Sin(a) * (ringInner + .012f), 0),
                pitchRadius * .13f, thickness);
        }
        mesh.Tube(new Vector3(0, 0, -thickness), new Vector3(0, 0, thickness), .048f, 16);
        return mesh;
    }

    static void BuildLighting(Transform kit) {
        var fittings = new Geometry();
        var lampGlass = new Geometry();
        var warmLights = new List<Light>();
        float[] serviceSegments = { 5, 25, 50, 74 };
        for (int i = 0; i < serviceSegments.Length; i++) {
            float y = L.PositionHeight(serviceSegments[i]) + 2.0f;
            float z = i % 2 == 0 ? -.93f : .93f;
            warmLights.Add(AddLight("Warm stair service lamp " + i, new Vector3(-1.10f, y, z),
                new Color(1, .81f, .60f), 4.2f, 3.5f, kit));
            Vector3 point = new Vector3(-1.30f, y, z);
            fittings.Box(point, new Vector3(.18f, .28f, .13f));
            fittings.Tube(point, point + new Vector3(.22f, -.08f, 0), .024f, 8);
            lampGlass.Tube(point + new Vector3(.22f, -.16f, 0), point + new Vector3(.22f, -.035f, 0), .047f, 12);
        }
        float machineY = L.MachineFloor + 2.10f;
        warmLights.Add(AddLight("Warm machine room service lamp", new Vector3(-L.ChamberInteriorWidth/2 + .415f, machineY, -.88f), new Color(1, .82f, .62f), 4.4f, 3.3f, kit));
        var natural=AddLight("Cool light from middle floor opening", new Vector3(.96f, L.MiddleFloor + 1.35f, .60f),
            new Color(.64f, .71f, .86f), 1.10f, 2.8f, kit);
        DayNightLighting.Bind(natural,DayNightLamp.Role.WindowDaylight);
        var machineBracket = new Vector3(-L.ChamberInteriorWidth/2 + .225f, machineY, -.88f);
        fittings.Box(machineBracket, new Vector3(.18f, .28f, .13f));
        fittings.Tube(machineBracket, machineBracket + new Vector3(.22f, -.08f, 0), .024f, 8);
        lampGlass.Tube(machineBracket + new Vector3(.22f, -.16f, 0), machineBracket + new Vector3(.22f, -.035f, 0), .047f, 12);
        Create("Small wall mounted service light brackets", fittings, iron, kit, false);
        var glow = Material("Tower service lamp warm glass", "#DCC59A", 0, .28f);
        glow.EnableKeyword("_EMISSION"); glow.SetColor("_EmissionColor", new Color(1, .69f, .33f) * 1.9f);
        EditorUtility.SetDirty(glow);
        var bulb=Create("Small amber maintenance lamp glass", lampGlass, glow, kit, false).GetComponent<Renderer>();
        for(int i=0;i<warmLights.Count;i++)DayNightLighting.Bind(warmLights[i],DayNightLamp.Role.Interior,.08f,i==0?new[]{bulb}:new Renderer[0]);
    }

    static Light AddLight(string name, Vector3 position, Color color, float intensity, float range, Transform parent) {
        var light = Group(name, parent).gameObject.AddComponent<Light>();
        light.transform.localPosition = position; light.type = LightType.Point;
        light.color = color; light.intensity = intensity; light.range = range; light.shadows = LightShadows.None;
        return light;
    }

    static void Shoe(Geometry sole, Geometry ridge, Vector3 centre, float direction, bool left, float scale) {
        float a = direction * Mathf.Deg2Rad;
        Vector3 forward = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)), right = new Vector3(forward.z, 0, -forward.x);
        float mirror = left ? -1 : 1;
        Vector3 P(float x, float z, float y = 0) => centre + right * (x * mirror * scale) + forward * (z * scale) + Vector3.up * y;
        var random = new System.Random(Mathf.RoundToInt(centre.x * 10007) ^ Mathf.RoundToInt(centre.y * 7919) ^ Mathf.RoundToInt(centre.z * 4051));
        float R() => (float)random.NextDouble();
        // Toe, ball and narrow instep; unequal chips prevent a stamped icon edge.
        Vector2[] outline = { new Vector2(-.035f,-.022f), new Vector2(-.050f,.036f), new Vector2(-.048f,.090f), new Vector2(-.033f,.125f),
            new Vector2(.003f,.133f), new Vector2(.040f,.116f), new Vector2(.052f,.077f), new Vector2(.043f,.024f), new Vector2(.030f,-.023f) };
        var points = new Vector3[outline.Length * 3];
        for (int i = 0; i < outline.Length; i++) for (int j = 0; j < 3; j++) {
            Vector2 point = Vector2.Lerp(outline[i], outline[(i + 1) % outline.Length], j / 3f);
            point.x += (R() - .5f) * .004f; point.y += (R() - .5f) * .003f;
            points[i * 3 + j] = P(point.x, point.y);
        }
        sole.UpPolygon(points);
        sole.UpPolygon(new[] { P(-.023f,-.122f), P(.024f,-.121f), P(.033f,-.112f), P(.035f,-.061f),
            P(.023f,-.052f), P(-.021f,-.054f), P(-.032f,-.065f), P(-.032f,-.109f) });
        foreach (float zBase in new[] { .021f, .052f, .082f }) {
            float z = zBase + (R() - .5f) * .005f;
            for (int half = 0; half < 2; half++) {
                float x0 = half == 0 ? -.035f + R() * .008f : .004f + R() * .007f;
                float x1 = half == 0 ? -.004f - R() * .005f : .030f - R() * .005f;
                float thick = .003f + R() * .0025f;
                ridge.UpPolygon(new[] { P(x0,z,.001f), P(x1,z+.004f,.001f), P(x1,z+.004f+thick,.001f), P(x0,z+thick,.001f) });
            }
        }
        ridge.UpPolygon(new[] { P(-.022f,-.095f,.001f), P(.019f,-.093f,.001f), P(.017f,-.089f,.001f), P(-.020f,-.091f,.001f) });
        // Dust pushed to the margins partly covers the pressure mark, and leaves
        // a few small crumbs just outside it. All marks share two draw calls.
        for (int i = 0; i < 36; i++) {
            Vector2 edge;
            if (i < 27) edge = Vector2.Lerp(outline[i % outline.Length], outline[(i + 1) % outline.Length], R());
            else {
                float aEdge = R() * Mathf.PI * 2;
                edge = new Vector2(Mathf.Cos(aEdge) * .032f, -.086f + Mathf.Sin(aEdge) * .035f);
            }
            float chip = .0015f + R() * .004f;
            edge.x += (R() - .5f) * .007f; edge.y += (R() - .5f) * .006f;
            ridge.UpPolygon(new[] { P(edge.x-chip,edge.y,.0015f), P(edge.x,edge.y-chip*.7f,.0015f),
                P(edge.x+chip*.8f,edge.y+chip*.2f,.0015f), P(edge.x-chip*.3f,edge.y+chip,.0015f) });
        }
        for (int i = 0; i < 10; i++) {
            float x = (R() - .5f) * .055f, z = -.005f + R() * .10f, size = .001f + R() * .003f;
            ridge.UpPolygon(new[] { P(x-size,z,.0015f), P(x+size*.7f,z-size,.0015f), P(x+size,z+size,.0015f) });
        }
    }

    static Transform Group(string name, Transform parent) {
        var obj = new GameObject(name); obj.transform.SetParent(parent, false); return obj.transform;
    }

    static void CollisionOnly(string name, Geometry geometry, Transform parent) {
        var obj = Group(name, parent).gameObject;
        obj.AddComponent<MeshCollider>().sharedMesh = SaveMesh(name, geometry);
        obj.isStatic = true;
    }

    static void AnnularBarrier(Geometry mesh, float start, float end, float inner, float outer,
        float bottom, float top, int segments) {
        for (int i = 0; i < segments; i++) {
            float a = Mathf.Lerp(start, end, i / (float)segments);
            float b = Mathf.Lerp(start, end, (i + 1f) / segments);
            Vector3 p0 = L.Polar(a, inner, top), p1 = L.Polar(b, inner, top);
            Vector3 p2 = L.Polar(b, outer, top), p3 = L.Polar(a, outer, top);
            Vector3 drop = Vector3.down * (top - bottom);
            mesh.Quad(p0, p1, p2, p3); mesh.Quad(p3 + drop, p2 + drop, p1 + drop, p0 + drop);
            mesh.Quad(p1, p0, p0 + drop, p1 + drop); mesh.Quad(p3, p2, p2 + drop, p3 + drop);
            if (i == 0) mesh.Quad(p0, p3, p3 + drop, p0 + drop);
            if (i == segments - 1) mesh.Quad(p2, p1, p1 + drop, p2 + drop);
        }
    }

    static Material Material(string name, string hex, float metallic, float smoothness) {
        string path = Art + "/Materials/" + name + ".mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (!material) { material = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(material, path); }
        ColorUtility.TryParseHtmlString(hex, out var color);
        material.SetColor("_BaseColor", color); material.SetFloat("_Metallic", metallic); material.SetFloat("_Smoothness", smoothness);
        EditorUtility.SetDirty(material); return material;
    }

    static GameObject Create(string name, Geometry geometry, Material material, Transform parent, bool collision) {
        var obj = Group(name, parent).gameObject;
        Mesh mesh = SaveMesh(name, geometry);
        obj.AddComponent<MeshFilter>().sharedMesh = mesh; obj.AddComponent<MeshRenderer>().sharedMaterial = material;
        if (collision) obj.AddComponent<MeshCollider>().sharedMesh = mesh;
        obj.isStatic = true;
        return obj;
    }

    static Mesh SaveMesh(string name, Geometry geometry) {
        Mesh mesh = geometry.Mesh(name); string path = Art + "/Meshes/" + name + ".asset";
        var saved = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (saved) { EditorUtility.CopySerialized(mesh, saved); UnityEngine.Object.DestroyImmediate(mesh); EditorUtility.SetDirty(saved); return saved; }
        AssetDatabase.CreateAsset(mesh, path); return mesh;
    }

    sealed class Geometry {
        readonly List<Vector3> vertices = new List<Vector3>();
        readonly List<int> triangles = new List<int>();
        readonly List<Vector2> uv = new List<Vector2>();
        public void ProjectHorizontal(float metresX, float metresZ) {
            for (int i = 0; i < vertices.Count; i++) uv[i] = new Vector2(vertices[i].x / metresX, vertices[i].z / metresZ);
        }
        public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d) {
            int n = vertices.Count; vertices.Add(a); vertices.Add(b); vertices.Add(c); vertices.Add(d);
            uv.Add(new Vector2(0,0)); uv.Add(new Vector2(0,1)); uv.Add(new Vector2(1,1)); uv.Add(new Vector2(1,0));
            triangles.Add(n); triangles.Add(n+1); triangles.Add(n+2); triangles.Add(n); triangles.Add(n+2); triangles.Add(n+3);
        }
        public void UpPolygon(Vector3[] p) {
            int n = vertices.Count; foreach (var v in p) { vertices.Add(v); uv.Add(new Vector2(v.x,v.z)); }
            bool reverse = Vector3.Cross(p[1]-p[0], p[2]-p[0]).y < 0;
            for (int i = 1; i < p.Length - 1; i++) { triangles.Add(n); triangles.Add(n+(reverse?i+1:i)); triangles.Add(n+(reverse?i:i+1)); }
        }
        public void Box(Vector3 c, Vector3 size) {
            Vector3 s = size / 2;
            Vector3 a=c+new Vector3(-s.x,-s.y,-s.z), b=c+new Vector3(s.x,-s.y,-s.z), d=c+new Vector3(-s.x,-s.y,s.z), e=c+new Vector3(s.x,-s.y,s.z);
            Vector3 up=Vector3.up*size.y;
            Quad(a,a+up,b+up,b); Quad(e,e+up,d+up,d); Quad(d,d+up,a+up,a); Quad(b,b+up,e+up,e); Quad(a+up,d+up,e+up,b+up); Quad(d,a,b,e);
        }
        public void Beam(Vector3 a, Vector3 b, float width, float depth) {
            Vector3 v = (b-a).normalized, side = new Vector3(-v.y,v.x,0)*width/2, z=Vector3.forward*depth/2;
            Quad(a-side-z,a+side-z,b+side-z,b-side-z); Quad(a+side+z,a-side+z,b-side+z,b+side+z);
            Quad(a-side+z,a-side-z,b-side-z,b-side+z); Quad(a+side-z,a+side+z,b+side+z,b+side-z);
            Quad(a-side-z,a-side+z,a+side+z,a+side-z); Quad(b-side+z,b-side-z,b+side-z,b+side+z);
        }
        public void Tube(Vector3 a, Vector3 b, float radius, int segments) {
            Vector3 axis=(b-a).normalized;
            Vector3 u=Vector3.Cross(axis,Mathf.Abs(axis.y)>.9f?Vector3.right:Vector3.up).normalized;
            Vector3 v=Vector3.Cross(axis,u).normalized;
            for(int i=0;i<segments;i++) {
                float t=i*Mathf.PI*2/segments, t1=(i+1)*Mathf.PI*2/segments;
                Vector3 r0=(u*Mathf.Cos(t)+v*Mathf.Sin(t))*radius, r1=(u*Mathf.Cos(t1)+v*Mathf.Sin(t1))*radius;
                Quad(a+r0,a+r1,b+r1,b+r0);
                // Degenerate fourth vertex produces an ordinary cap triangle.
                Quad(a,a+r1,a+r0,a); Quad(b,b+r0,b+r1,b);
            }
        }
        public void RingXY(Vector3 centre,float inner,float outer,float depth,int segments) {
            for(int i=0;i<segments;i++) {
                float a=i*Mathf.PI*2/segments,b=(i+1)*Mathf.PI*2/segments;
                Vector3 p0=centre+new Vector3(Mathf.Cos(a)*inner,Mathf.Sin(a)*inner,-depth/2);
                Vector3 p1=centre+new Vector3(Mathf.Cos(b)*inner,Mathf.Sin(b)*inner,-depth/2);
                Vector3 p2=centre+new Vector3(Mathf.Cos(b)*outer,Mathf.Sin(b)*outer,-depth/2);
                Vector3 p3=centre+new Vector3(Mathf.Cos(a)*outer,Mathf.Sin(a)*outer,-depth/2), z=Vector3.forward*depth;
                Quad(p0,p1,p2,p3);Quad(p3+z,p2+z,p1+z,p0+z);Quad(p3,p2,p2+z,p3+z);
            }
        }
        public Mesh Mesh(string name) {
            var mesh=new Mesh { name=name, indexFormat=vertices.Count>65535?IndexFormat.UInt32:IndexFormat.UInt16 };
            mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.SetTriangles(triangles,0);
            mesh.RecalculateNormals();mesh.RecalculateBounds();mesh.RecalculateTangents();return mesh;
        }
    }
}
