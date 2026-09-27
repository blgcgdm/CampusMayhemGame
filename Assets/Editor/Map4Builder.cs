using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

// Menü: GameJam > Map4 Oluştur
// "Lav Zindanı": üç taş ayak arasında iki lav çukuru, çukurların üstünde basamaklar,
// yan yüksek çıkıntılar ve tepe platformu. Kale zindanı arka planı + altta lav.
// SampleScene'in kopyasından kurulur, SampleScene'e dokunmaz.
public static class Map4Builder
{
    const string Src = "Assets/Scenes/SampleScene.unity";
    const string Dst = "Assets/Scenes/Map4.unity";
    static readonly int[] Cap  = { 825, 826, 827, 828, 829, 830, 831, 832, 833 };
    static readonly int[] Top2 = { 882, 883, 884, 885, 886, 887, 888, 889, 890 };
    static readonly int[] Body = { 906, 907, 908, 909, 910, 911, 912, 913, 914 };

    static TileBase Tile(int i) =>
        AssetDatabase.LoadAssetAtPath<TileBase>($"Assets/Art/Tiles/Medieval_tiles_free2_{i}.asset");

    static void Run(Tilemap tm, int x0, int x1, int y, int[] set)
    {
        int len = x1 - x0 + 1;
        for (int i = 0; i < len; i++)
        {
            int idx = i == 0 ? set[0] : i == len - 1 ? set[8] : set[1 + (i - 1) % 7];
            tm.SetTile(new Vector3Int(x0 + i, y, 0), Tile(idx));
        }
    }

    static void Place(string name, Vector3 pos)
    {
        var go = GameObject.Find(name);
        if (go) go.transform.position = pos;
    }

    [MenuItem("GameJam/Map4 Oluştur")]
    static void Build()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(Src) == null)
        { EditorUtility.DisplayDialog("GameJam", Src + " bulunamadı.", "Tamam"); return; }
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(Dst) != null)
        {
            if (!EditorUtility.DisplayDialog("GameJam", "Map4 zaten var. Üzerine yazılsın mı?", "Evet", "Hayır")) return;
            AssetDatabase.DeleteAsset(Dst);
        }
        AssetDatabase.CopyAsset(Src, Dst);
        var scene = EditorSceneManager.OpenScene(Dst);

        var tm = GameObject.Find("Platforms")?.GetComponent<Tilemap>();
        if (tm == null) { EditorUtility.DisplayDialog("GameJam", "'Platforms' Tilemap'i bulunamadı.", "Tamam"); return; }
        tm.ClearAllTiles();

        // Üç taş ayak (lavın içine iner), aralarında 2 birimlik lav çukurları
        foreach (var (x0, x1) in new[] { (-8, -5), (-2, 1), (4, 7) })
        {
            Run(tm, x0, x1, -3, Top2);
            for (int y = -4; y >= -6; y--) Run(tm, x0, x1, y, Body);
        }
        Run(tm, -4, -3, -1, Cap);   // çukurların üstündeki basamaklar
        Run(tm, 2, 3, -1, Cap);
        Run(tm, -7, -5, 1, Cap);    // yan yüksek çıkıntılar
        Run(tm, 4, 6, 1, Cap);
        Run(tm, -1, 0, 3, Cap);     // tepe

        var bg = GameObject.Find("Background");
        var dungeon = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Background/Background_Dungeon.png");
        if (bg && dungeon) bg.GetComponent<SpriteRenderer>().sprite = dungeon;

        var lavaSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Background/Lava.png");
        if (lavaSprite)
        {
            var lava = new GameObject("Lava");
            var r = lava.AddComponent<SpriteRenderer>();
            r.sprite = lavaSprite;
            r.sortingOrder = 20; // düşen karakterler lavın içinde kaybolsun
            lava.transform.position = new Vector3(0f, -4.975f, 0f);
        }

        Place("HeroKnight",  new Vector3(-6f, -1.9f, 0f));
        Place("LightBandit", new Vector3( 6f, -1.9f, 0f));
        Place("spawnpoint1", new Vector3(-6f, -1.9f, 0f));
        Place("spawnpoint2", new Vector3( 6f, -1.9f, 0f));

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        var list = EditorBuildSettings.scenes.ToList();
        if (!list.Any(s => s.path == Dst)) { list.Add(new EditorBuildSettingsScene(Dst, true)); EditorBuildSettings.scenes = list.ToArray(); }
        Debug.Log("GameJam: Map4 oluşturuldu (Assets/Scenes/Map4).");
    }
}
