using UnityEditor;
using UnityEngine;

public static class PropSoundAuthoring {
    public static void EnsureImports() {
        foreach(string name in new[]{"MapUnfold","MapFold","WatchTakeOut","WatchPutAway"}) {
            string path="Assets/Resources/PropSounds/"+name+".wav";
            var importer=AssetImporter.GetAtPath(path) as AudioImporter;
            if(!importer)throw new System.InvalidOperationException("Missing held-item audio: "+path);
            var settings=importer.defaultSampleSettings;
            if(settings.compressionFormat==AudioCompressionFormat.PCM&&settings.loadType==AudioClipLoadType.DecompressOnLoad)continue;
            settings.compressionFormat=AudioCompressionFormat.PCM;
            settings.loadType=AudioClipLoadType.DecompressOnLoad;
            settings.sampleRateSetting=AudioSampleRateSetting.PreserveSampleRate;
            importer.defaultSampleSettings=settings;importer.SaveAndReimport();
        }
    }
}
