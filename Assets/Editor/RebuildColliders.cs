using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

// Menü: GameJam > Çarpışmaları Yenile
// Map2-5, Map1'in kopyasından kurulduğu için CompositeCollider2D'de Map1'in eski
// çarpışma şekli kalmıştı (görünmez platformlar / içinden düşülen platformlar).
// Her map'te tilemap çarpışmasını baştan üretir.
public static class RebuildColliders
{
    static readonly string[] Scenes =
    {
        "Assets/Scenes/Map1.unity", "Assets/Scenes/Map2.unity", "Assets/Scenes/Map3.unity",
        "Assets/Scenes/Map4.unity", "Assets/Scenes/Map5.unity"
    };

    [MenuItem("GameJam/Çarpışmaları Yenile")]
    static void Run()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        // Map5'in kendi çizdiğimiz tile'ları: sprite fizik şekline güvenme, tam hücre kullan
        foreach (var guid in AssetDatabase.FindAssets("t:Tile", new[] { "Assets/Art/GrassTiles" }))
        {
            var tile = AssetDatabase.LoadAssetAtPath<Tile>(AssetDatabase.GUIDToAssetPath(guid));
            if (tile != null && tile.colliderType != Tile.ColliderType.Grid)
            {
                tile.colliderType = Tile.ColliderType.Grid;
                EditorUtility.SetDirty(tile);
            }
        }
        AssetDatabase.SaveAssets();

        string current = EditorSceneManager.GetActiveScene().path;
        int done = 0;
        foreach (var path in Scenes)
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) == null) continue;
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            var go = GameObject.Find("Platforms");
            if (go == null) continue;

            var tm = go.GetComponent<Tilemap>();
            tm.RefreshAllTiles();
            var tc = go.GetComponent<TilemapCollider2D>();
            if (tc != null) tc.ProcessTilemapChanges();
            var comp = go.GetComponent<CompositeCollider2D>();
            if (comp != null)
            {
                comp.generationType = CompositeCollider2D.GenerationType.Synchronous;
                comp.GenerateGeometry();
                EditorUtility.SetDirty(comp);
            }
            EditorUtility.SetDirty(go);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"GameJam: {path} çarpışma yolları: {(comp != null ? comp.pathCount : -1)}");
            done++;
        }
        if (!string.IsNullOrEmpty(current)) EditorSceneManager.OpenScene(current);
        Debug.Log($"GameJam: {done} map'in çarpışmaları yenilendi.");
    }
}
