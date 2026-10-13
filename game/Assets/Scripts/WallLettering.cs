using UnityEngine;
// Painted lettering on a wall. The stock font material ignores depth and shows through
// masonry, so the lettering uses the depth-tested world text material and follows the
// font's live atlas.
public sealed class WallLettering : MonoBehaviour {
    public Font font;
    public Material material;
    void OnEnable(){Font.textureRebuilt+=AtlasChanged;AtlasChanged(font);}
    void OnDisable(){Font.textureRebuilt-=AtlasChanged;}
    void AtlasChanged(Font updated){if(updated==font&&font&&material)material.mainTexture=font.material.mainTexture;}
}
