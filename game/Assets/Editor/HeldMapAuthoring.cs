using System;
using UnityEditor;
using UnityEngine;

public static class HeldMapAuthoring {
    public static void EnsureMaterial() {
        var shader=Shader.Find("WrongDate/HeldMapInk");
        if(!shader)throw new InvalidOperationException("Held paper map shader missing");
        const string path="Assets/Resources/HeldMapInk.mat";
        var material=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(!material){material=new Material(shader);AssetDatabase.CreateAsset(material,path);}
        material.shader=shader;EditorUtility.SetDirty(material);
    }
}
