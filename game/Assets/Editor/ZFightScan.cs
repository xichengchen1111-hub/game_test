using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Reports z-fighting candidates in the generated scenes with the shared CoplanarDetector:
// same-plane (<= 2mm), same-facing overlaps >= 1 cm². "visible" pairs shade differently
// (material, texture coordinates or light layers); "identical" pairs cannot flicker.
// Moving parts (doors, drawers, hands, wheels) are checked at their authored pose and listed as MOVING.
// Skinned actors are not MeshRenderers and are not checked. Expected result: 0 EXACT and 0 MOVING groups.
// Batch: Unity -batchmode -quit -projectPath game -executeMethod ZFightScan.Run -logFile ...
public static class ZFightScan {
    [Serializable] class Finding { public string scene, a, materialA, b, materialB, kind; public float area, offsetMm; public float[] at, normal; public int pairs; public bool moving; }
    [Serializable] class Report { public int triangles, renderers, visibleExact, visibleNear, movingExact; public List<Finding> groups = new List<Finding>(); public string scope; }

    public static void Run() {
        var report = new Report { scope = "MeshRenderers (LOD0, no skinned actors) in Town, OtherTown and ClockShop as generated. Moving parts (doors, drawers, hands, wheels) are checked at their authored pose and counted separately. exact: offset <= 0.5mm; near: 0.5-2mm." };
        foreach (var path in new[] { "Assets/Scenes/Town.unity", "Assets/Scenes/OtherTown.unity", "Assets/Scenes/ClockShop.unity" }) {
            if (!File.Exists(path)) continue;
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            var det = new CoplanarDetector(.002f);
            var moving = new HashSet<MeshRenderer>();
            foreach (var root in scene.GetRootGameObjects())
                foreach (var r in root.GetComponentsInChildren<MeshRenderer>()) {
                    det.Add(r);
                    if (ZFightRepair.Moving(r.transform, root.transform)) moving.Add(r);
                }
            report.triangles += det.tris.Count; report.renderers += det.renderers.Count;
            var groups = new Dictionary<string, Finding>();
            foreach (var p in det.Detect().Where(p => p.kind != "identical")) {
                var A = det.tris[p.a]; var B = det.tris[p.b];
                string an = Path(det.renderers[A.renderer].transform), bn = Path(det.renderers[B.renderer].transform);
                string key = string.CompareOrdinal(an + A.material, bn + B.material) <= 0 ? an + "|" + A.material + "|" + bn + "|" + B.material : bn + "|" + B.material + "|" + an + "|" + A.material;
                if (!groups.TryGetValue(key, out var g)) groups[key] = g = new Finding { scene = scene.name, a = an, materialA = A.material, b = bn, materialB = B.material, kind = p.kind, at = new[] { p.centre.x, p.centre.y, p.centre.z }, normal = new[] { A.n.x, A.n.y, A.n.z },
                    moving = moving.Contains(det.renderers[A.renderer]) || moving.Contains(det.renderers[B.renderer]) };
                g.area += p.area; g.pairs++; g.offsetMm = Mathf.Max(g.offsetMm, p.offset * 1000);
            }
            foreach (var g in groups.Values) { if (g.offsetMm > .5f) report.visibleNear++; else if (g.moving) report.movingExact++; else report.visibleExact++; }
            report.groups.AddRange(groups.Values);
            Debug.Log("ZFIGHT_SCAN_SCENE: " + scene.name + " triangles=" + det.tris.Count + " visibleGroups=" + groups.Count);
        }
        Directory.CreateDirectory("../output/demo/zfight");
        File.WriteAllText("../output/demo/zfight/scan.json", JsonUtility.ToJson(report, true));
        File.WriteAllLines("../output/demo/zfight/scan.txt", report.groups.OrderBy(g => g.offsetMm > .5f).ThenByDescending(g => g.area).Select(g =>
            $"{(g.offsetMm > .5f ? "near " : g.moving ? "MOVING" : "EXACT")} {g.kind,-11} {g.area * 10000f,9:0.0} cm²  {g.pairs,5}p  off {g.offsetMm:0.0}mm  [{g.scene}] {g.a} ({g.materialA})  <>  {g.b} ({g.materialB})  at {g.at[0]:0.00},{g.at[1]:0.00},{g.at[2]:0.00}"));
        Debug.Log("ZFIGHT_SCAN_DONE: " + report.triangles + " triangles, " + report.renderers + " renderers, visible exact groups=" + report.visibleExact + ", moving-part exact groups=" + report.movingExact + ", near (0.5-2mm)=" + report.visibleNear);
    }
    static string Path(Transform t) {
        var parts = new List<string>();
        for (int i = 0; t && i < 4; i++, t = t.parent) parts.Add(t.name);
        parts.Reverse(); return string.Join("/", parts);
    }
}
