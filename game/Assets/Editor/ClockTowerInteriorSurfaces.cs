using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using L = ClockTowerInteriorLayout;

/// <summary>Deterministic metre-scale limestone, paving and height-graded dust materials.</summary>
public static class ClockTowerInteriorSurfaces {
    const string Root = "Assets/Art/ClockTowerInterior";
    public const int BandCount = 8;
    public static readonly float[] DustAmounts = { .025f, .06f, .15f, .30f, .48f, .66f, .84f, 1f };
    public static Material[] Treads, Prints, Ridges;
    public static Material Wall, Floor;
    public static int Band(float height) => Mathf.Clamp(Mathf.FloorToInt(height / 2f), 0, BandCount - 1);
    static Color C(string hex) { ColorUtility.TryParseHtmlString(hex, out var color); return color; }

    public static void Prepare() {
        Directory.CreateDirectory(Root + "/SurfaceTextures");
        Directory.CreateDirectory(Root + "/Materials");
        AssetDatabase.Refresh();
        MakeMasonry("Limestone", 4, 4, 1.52f, .72f, .006f, C("#C3B8A1"), C("#8D887A"), .0025f);
        MakeMasonry("WornPaving", 2, 4, 1.16f, 1.52f, .009f, C("#A39780"), C("#666458"), .0035f);
        Wall = Mat("Tower inner limestone bricks", Color.white, .12f);
        TextureMaterial(Wall, "Limestone", .62f);
        Floor = Mat("Tower ground worn stone slabs", Color.white, .09f);
        TextureMaterial(Floor, "WornPaving", .72f);
        Treads = new Material[BandCount]; Prints = new Material[BandCount]; Ridges = new Material[BandCount];
        var grain = AssetDatabase.LoadAssetAtPath<Texture2D>(Root + "/DustyOak.png");
        var transfer = AssetDatabase.LoadAssetAtPath<Texture2D>(Root + "/DisplacedDust.png");
        for (int i = 0; i < BandCount; i++) {
            float amount = DustAmounts[i];
            Color dustyWood = Color.Lerp(C("#786B57"), C("#B4AC97"), amount);
            // At ground level the exposed wood nearly matches the thin dusty
            // film. High above, the same shoe exposes warm wood beneath powder.
            Color pressure = Color.Lerp(C("#776A57"), C("#8A8271"), amount);
            Treads[i] = Mat("Tower stair dust band " + i.ToString("00"), dustyWood, Mathf.Lerp(.16f, .055f, amount));
            Treads[i].SetTexture("_BaseMap", grain);
            Treads[i].SetTextureScale("_BaseMap", Vector2.one);
            Prints[i] = Mat("Tower shoe pressure band " + i.ToString("00"), pressure, .08f);
            Prints[i].SetTexture("_BaseMap", transfer);
            Prints[i].SetTextureScale("_BaseMap", new Vector2(15, 15));
            Prints[i].SetFloat("_AlphaClip", 1);
            Prints[i].SetFloat("_Cutoff", Mathf.Lerp(.605f, .43f, amount));
            Prints[i].EnableKeyword("_ALPHATEST_ON");
            Prints[i].renderQueue = (int)RenderQueue.AlphaTest;
            Prints[i].SetOverrideTag("RenderType", "TransparentCutout");
            Ridges[i] = Mat("Tower sole dust ridges band " + i.ToString("00"), dustyWood * .91f, .05f);
            EditorUtility.SetDirty(Treads[i]); EditorUtility.SetDirty(Prints[i]); EditorUtility.SetDirty(Ridges[i]);
        }
    }

    static Material Mat(string name, Color color, float smoothness) {
        string path = Root + "/Materials/" + name + ".mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (!material) { material = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(material, path); }
        material.SetColor("_BaseColor", color); material.SetFloat("_Metallic", 0); material.SetFloat("_Smoothness", smoothness);
        EditorUtility.SetDirty(material); return material;
    }

    static void TextureMaterial(Material material, string name, float bump) {
        material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(Root + "/SurfaceTextures/" + name + "-Albedo.png"));
        material.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(Root + "/SurfaceTextures/" + name + "-Normal.png"));
        material.SetTextureScale("_BaseMap", Vector2.one);
        material.SetFloat("_BumpScale", bump); material.EnableKeyword("_NORMALMAP"); EditorUtility.SetDirty(material);
    }

    static void MakeMasonry(string name, int columns, int rows, float metresX, float metresY,
        float jointWidth, Color faceColor, Color mortarColor, float relief) {
        const int size = 512;
        var colors = new Color[size * size]; var heights = new float[size * size];
        for (int y = 0; y < size; y++) for (int x = 0; x < size; x++) {
            float v = (y + .5f) / size, u = (x + .5f) / size;
            int row = Mathf.FloorToInt(v * rows);
            float shifted = u * columns + (row % 2) * .5f;
            int column = Mathf.FloorToInt(shifted);
            column = ((column % columns) + columns) % columns;
            float bx = Mathf.Repeat(shifted, 1), by = Mathf.Repeat(v * rows, 1);
            float edge = Mathf.Min(Mathf.Min(bx, 1 - bx) * metresX / columns,
                Mathf.Min(by, 1 - by) * metresY / rows);
            float chip = (Mathf.PerlinNoise(u * 75, v * 75) - .5f) * .0025f;
            float blend = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(jointWidth * .43f + chip, jointWidth * .92f + chip, edge));
            float block = Mathf.Repeat(Mathf.Sin((column + 1) * 37.13f + (row + 1) * 73.77f) * 143.71f, 1);
            float rough = Mathf.PerlinNoise(u * 73 + 23, v * 73 + 51);
            float cloud = Mathf.PerlinNoise(u * 11 + 12, v * 11 + 49);
            float wear = .89f + block * .11f + cloud * .045f + (rough - .5f) * .085f;
            Color stone = faceColor * wear;
            colors[y * size + x] = Color.Lerp(mortarColor * (.91f + rough * .13f), stone, blend);
            heights[y * size + x] = blend * relief + (rough - .5f) * relief * .16f;
        }
        var normals = new Color[colors.Length];
        for (int y = 0; y < size; y++) for (int x = 0; x < size; x++) {
            float dx = (heights[y * size + (x + 1) % size] - heights[y * size + (x + size - 1) % size]) / (2 * metresX / size);
            float dy = (heights[((y + 1) % size) * size + x] - heights[((y + size - 1) % size) * size + x]) / (2 * metresY / size);
            Vector3 n = new Vector3(-dx, -dy, 1).normalized;
            normals[y * size + x] = new Color(n.x * .5f + .5f, n.y * .5f + .5f, n.z * .5f + .5f, 1);
        }
        SaveTexture(name + "-Albedo", colors, size, false);
        SaveTexture(name + "-Normal", normals, size, true);
    }

    static void SaveTexture(string name, Color[] pixels, int size, bool normal) {
        string path = Root + "/SurfaceTextures/" + name + ".png";
        var texture = new Texture2D(size, size, TextureFormat.RGB24, false, normal);
        texture.SetPixels(pixels); texture.Apply(); File.WriteAllBytes(path, texture.EncodeToPNG());
        Object.DestroyImmediate(texture); AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
        importer.sRGBTexture = !normal; importer.wrapMode = TextureWrapMode.Repeat;
        importer.mipmapEnabled = true; importer.filterMode = FilterMode.Trilinear; importer.anisoLevel = 8;
        importer.maxTextureSize = size; importer.SaveAndReimport();
    }

    public static void BuildInnerWalls(Transform parent) {
        // Render-only surfaces 6mm in front of the existing shared inner shell.
        // Door opening and all original collision are untouched.
        float h = L.InteriorWidth / 2 - .006f, bottom = L.GroundFloor, top = L.InnerWallHeight - .062f;
        var vertices = new List<Vector3>(); var uvs = new List<Vector2>(); var triangles = new List<int>();
        void Face(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 inward) {
            int n = vertices.Count; vertices.Add(a); vertices.Add(b); vertices.Add(c); vertices.Add(d);
            foreach (Vector3 point in new[] { a, b, c, d })
                uvs.Add(new Vector2((Mathf.Abs(inward.x) > .5f ? point.z : point.x) / 1.52f, point.y / .72f));
            if (Vector3.Dot(Vector3.Cross(b - a, c - a), inward) > 0)
                triangles.AddRange(new[] { n, n + 1, n + 2, n, n + 2, n + 3 });
            else triangles.AddRange(new[] { n, n + 2, n + 1, n, n + 3, n + 2 });
        }
        Face(new Vector3(-h,bottom,-h),new Vector3(-h,bottom,h),new Vector3(-h,top,h),new Vector3(-h,top,-h),Vector3.right);
        Face(new Vector3(h,bottom,-h),new Vector3(h,bottom,h),new Vector3(h,top,h),new Vector3(h,top,-h),Vector3.left);
        Face(new Vector3(-h,bottom,h),new Vector3(h,bottom,h),new Vector3(h,top,h),new Vector3(-h,top,h),Vector3.back);
        float doorHalf = L.DoorWidth / 2, doorTop = L.GroundFloor + L.DoorHeight;
        Face(new Vector3(-h,bottom,-h),new Vector3(-doorHalf,bottom,-h),new Vector3(-doorHalf,top,-h),new Vector3(-h,top,-h),Vector3.forward);
        Face(new Vector3(doorHalf,bottom,-h),new Vector3(h,bottom,-h),new Vector3(h,top,-h),new Vector3(doorHalf,top,-h),Vector3.forward);
        Face(new Vector3(-doorHalf,doorTop,-h),new Vector3(doorHalf,doorTop,-h),new Vector3(doorHalf,top,-h),new Vector3(-doorHalf,top,-h),Vector3.forward);
        var mesh = new Mesh { name = "Metre scale limestone interior wall lining" };
        mesh.SetVertices(vertices); mesh.SetUVs(0, uvs); mesh.SetTriangles(triangles, 0);
        mesh.RecalculateNormals(); mesh.RecalculateBounds(); mesh.RecalculateTangents();
        string path = Root + "/Meshes/Metre scale limestone interior wall lining.asset";
        var saved = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (saved) { EditorUtility.CopySerialized(mesh, saved); Object.DestroyImmediate(mesh); mesh = saved; EditorUtility.SetDirty(mesh); }
        else AssetDatabase.CreateAsset(mesh, path);
        var obj = new GameObject("Metre scale limestone interior wall lining"); obj.transform.SetParent(parent, false);
        obj.AddComponent<MeshFilter>().sharedMesh = mesh; obj.AddComponent<MeshRenderer>().sharedMaterial = Wall; obj.isStatic = true;
    }
}
