using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

// Menü: GameJam > Map5 Oluştur
// "Savaş Alanı": çimenli toprak, ahşap platform ve sandıklarla kurulan yeşil savaş alanı.
// Map1'in kopyasından kurulur, Map1'e dokunmaz.
public static class Map5Builder
{
    const string Src = "Assets/Scenes/Map1.unity";
    const string Dst = "Assets/Scenes/Map5.unity";
    const string Dir = "Assets/Art/GrassTiles";

    // Sprite'tan Tile asset'i üretir (yoksa), collider sprite şeklini kullanır
    static TileBase T(string name)
    {
        string path = $"{Dir}/Grass_{name}.asset";
        var tile = AssetDatabase.LoadAssetAtPath<Tile>(path);
        if (tile != null) return tile;
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{Dir}/Grass_{name}.png");
        if (sprite == null) { Debug.LogError("GameJam: sprite bulunamadı: " + name); return null; }
        tile = ScriptableObject.CreateInstance<Tile>();
        tile.sprite = sprite;
        tile.colliderType = Tile.ColliderType.Sprite;
        AssetDatabase.CreateAsset(tile, path);
        return tile;
    }

    static void Run(Tilemap tm, int x0, int x1, int y, string l, string m, string r)
    {
        for (int x = x0; x <= x1; x++)
            tm.SetTile(new Vector3Int(x, y, 0), T(x == x0 ? l : x == x1 ? r : m));
    }

    static void Place(string name, Vector3 pos)
    {
        var go = GameObject.Find(name);
        if (go) go.transform.position = pos;
    }

    [MenuItem("GameJam/Map5 Oluştur")]
    static void Build()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(Src) == null)
        { EditorUtility.DisplayDialog("GameJam", Src + " bulunamadı.", "Tamam"); return; }
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(Dst) != null)
        {
            if (!EditorUtility.DisplayDialog("GameJam", "Map5 zaten var. Üzerine yazılsın mı?", "Evet", "Hayır")) return;
            AssetDatabase.DeleteAsset(Dst);
        }
        AssetDatabase.CopyAsset(Src, Dst);
        var scene = EditorSceneManager.OpenScene(Dst);

        var tm = GameObject.Find("Platforms")?.GetComponent<Tilemap>();
        if (tm == null) { EditorUtility.DisplayDialog("GameJam", "'Platforms' Tilemap'i bulunamadı.", "Tamam"); return; }
        tm.ClearAllTiles();

        // Ortada çimenli toprak adası (ekranın altına iner)
        Run(tm, -3, 2, -2, "Top_L", "Top_M", "Top_R");
        for (int y = -3; y >= -6; y--) Run(tm, -3, 2, y, "Dirt_L", "Dirt_M", "Dirt_R");
        // Yanlarda ahşap platformlar
        Run(tm, -8, -6, 0, "Wood_L", "Wood_M", "Wood_R");
        Run(tm, 5, 7, 0, "Wood_L", "Wood_M", "Wood_R");
        // Üst ahşap platformlar
        Run(tm, -4, -2, 2, "Wood_L", "Wood_M", "Wood_R");
        Run(tm, 1, 3, 2, "Wood_L", "Wood_M", "Wood_R");
        // Alt köşelerde tek sandıklar (riskli kaçış noktaları)
        tm.SetTile(new Vector3Int(-7, -3, 0), T("Crate"));
        tm.SetTile(new Vector3Int(6, -3, 0), T("Crate"));

        var bg = GameObject.Find("Background");
        var field = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Background/Background_Battlefield.png");
        if (bg && field) bg.GetComponent<SpriteRenderer>().sprite = field;

        Place("HeroKnight",  new Vector3(-6.5f, 1.1f, 0f));
        Place("LightBandit", new Vector3( 6.5f, 1.1f, 0f));
        Place("spawnpoint1", new Vector3(-6.5f, 1.1f, 0f));
        Place("spawnpoint2", new Vector3( 6.5f, 1.1f, 0f));

        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        var list = EditorBuildSettings.scenes.ToList();
        if (!list.Any(s => s.path == Dst)) { list.Add(new EditorBuildSettingsScene(Dst, true)); EditorBuildSettings.scenes = list.ToArray(); }
        Debug.Log("GameJam: Map5 oluşturuldu (Assets/Scenes/Map5).");
    }
}
