using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

// Menü: GameJam > Alt Platform Geçişini Aç
// Map1 düzeninde (DemoScene de aynı düzen) alt platformların iç uçları ortadaki
// platformun altına giriyordu; aradaki 1 birimlik boşluktan karakter geçemiyordu.
// Alt platformları içten birer kare kısaltır, kenar tile'larını düzeltir.
public static class FixMap1Gap
{
    static readonly string[] Scenes = { "Assets/Scenes/DemoScene.unity", "Assets/Scenes/Map1.unity" };
    static TileBase T(int i) => AssetDatabase.LoadAssetAtPath<TileBase>($"Assets/Art/Tiles/Medieval_tiles_free2_{i}.asset");

    [MenuItem("GameJam/Alt Platform Geçişini Aç")]
    static void Apply()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        string current = EditorSceneManager.GetActiveScene().path;

        foreach (var path in Scenes)
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) == null) continue;
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            var tm = GameObject.Find("Platforms")?.GetComponent<Tilemap>();
            if (tm == null) continue;
            Undo.RegisterCompleteObjectUndo(tm, "Alt platform geçişi");

            // Ortadaki platformun altına giren iç uçları sil
            foreach (int y in new[] { -3, -4 })
            {
                tm.SetTile(new Vector3Int(-2, y, 0), null);
                tm.SetTile(new Vector3Int(1, y, 0), null);
            }

            // Map1'in kenarlı tile'larında yeni uçlara kenar parçası koy
            bool medieval = tm.GetTile(new Vector3Int(-4, -3, 0)) != null &&
                            AssetDatabase.GetAssetPath(tm.GetTile(new Vector3Int(-4, -3, 0))).Contains("Medieval_tiles_free2");
            if (medieval)
            {
                tm.SetTile(new Vector3Int(-3, -3, 0), T(890)); // sol alt platform: sağ uç (üst)
                tm.SetTile(new Vector3Int(-3, -4, 0), T(914)); // sol alt platform: sağ uç (gövde)
                tm.SetTile(new Vector3Int(2, -3, 0), T(882));  // sağ alt platform: sol uç (üst)
                tm.SetTile(new Vector3Int(2, -4, 0), T(906));  // sağ alt platform: sol uç (gövde)
            }

            var composite = tm.GetComponent<CompositeCollider2D>();
            if (composite != null) composite.GenerateGeometry();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }
        if (!string.IsNullOrEmpty(current)) EditorSceneManager.OpenScene(current);
        Debug.Log("GameJam: alt platformlar kısaltıldı, orta platformun altından geçiş açıldı.");
    }
}
