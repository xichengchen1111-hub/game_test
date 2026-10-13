using UnityEditor;
using UnityEditor.Build;
using UnityEngine.Rendering;
public static partial class BuildSlice {
 public static void BuildHouseStaging(){
  string company=PlayerSettings.companyName,id=PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Standalone);
  try{PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Standalone,"com.qingchuanstudio.wrongdatetest.currentmac");BuildFor(BuildTarget.StandaloneOSX,"../builds/house-review/住宅验收.app",new[]{GraphicsDeviceType.Metal},1,"Qingchuan Fresh 20261008");}
  finally{PlayerSettings.companyName=company;PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Standalone,id);AssetDatabase.SaveAssets();}
 }
}
