using UnityEditor;

// Assets/Art altına eklenen her görseli otomatik pixel-art ayarlarıyla içe aktarır.
public class PixelArtImporter : AssetPostprocessor
{
    void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith("Assets/Art/")) return;
        if (!assetImporter.importSettingsMissing) return; // sadece ilk import

        var ti = (TextureImporter)assetImporter;
        ti.textureType = TextureImporterType.Sprite;
        ti.spriteImportMode = assetPath.Contains("/Tiles/") ? SpriteImportMode.Multiple : SpriteImportMode.Single;
        ti.spritePixelsPerUnit = 16;
        ti.filterMode = UnityEngine.FilterMode.Point;
        ti.textureCompression = TextureImporterCompression.Uncompressed;
        ti.mipmapEnabled = false;
        ti.alphaIsTransparency = true;
    }
}
