using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

// Menü: GameJam > Map2 Oluştur
// SampleScene'in kopyasından "Map2" sahnesi kurar: köprü + yan çıkıntılar + orta ve tepe platformlar,
// gün batımı arka planı ve alttaki uçurum sisi. SampleScene'e dokunmaz.
public static class Map2Builder
{
    const string Src = "Assets/Scenes/SampleScene.unity";
    const string Dst = "Assets/Scenes/Map2.unity";
    static readonly int[] Cap    = { 825, 826, 827, 828, 829, 830, 831, 832, 833 };
    static readonly int[] Bridge = { 996, 997, 998, 999, 1000, 1001, 1002, 1003, 1004 };

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

    [MenuItem("GameJam/Map2 Oluştur")]
    static void Build()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(Src) == null)
        { EditorUtility.DisplayDialog("GameJam", Src + " bulunamadı.", "Tamam"); return; }
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(Dst) != null)
        {
            if (!EditorUtility.DisplayDialog("GameJam", "Map2 zaten var. Üzerine yazılsın mı?", "Evet", "Hayır")) return;
            AssetDatabase.DeleteAsset(Dst);
        }
        AssetDatabase.CopyAsset(Src, Dst);
        var scene = EditorSceneManager.OpenScene(Dst);

        var tm = GameObject.Find("Platforms")?.GetComponent<Tilemap>();
        if (tm == null) { EditorUtility.DisplayDialog("GameJam", "'Platforms' Tilemap'i bulunamadı.", "Tamam"); return; }
        tm.ClearAllTiles();
        Run(tm, -4, 3, -3, Bridge);   // alt köprü (ana zemin)
        Run(tm, -8, -6, -1, Cap);     // sol kenar çıkıntısı
        Run(tm, 5, 7, -1, Cap);       // sağ kenar çıkıntısı
        Run(tm, -4, -2, 1, Cap);      // sol orta
        Run(tm, 1, 3, 1, Cap);        // sağ orta
        tm.SetTile(new Vector3Int(-1, 3, 0), Tile(Cap[0]));  // tepe
        tm.SetTile(new Vector3Int(0, 3, 0), Tile(Cap[8]));

        var bg = GameObject.Find("Background");
        var dusk = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Background/Background_Dusk.png");
        if (bg && dusk) bg.GetComponent<SpriteRenderer>().sprite = dusk;

        var fogSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Background/Fog.png");
        if (fogSprite)
        {
            var fog = new GameObject("AbyssFog");
            var r = fog.AddComponent<SpriteRenderer>();
            r.sprite = fogSprite;
            r.sortingOrder = 20; // düşen karakterler sisin içinde kaybolsun
            fog.transform.position = new Vector3(0f, -4.25f, 0f);
        }

        Place("HeroKnight",  new Vector3(-6.5f, 0.1f, 0f));
        Place("LightBandit", new Vector3( 6.5f, 0.1f, 0f));
        Place("spawnpoint1", new Vector3(-6.5f, 0.1f, 0f));
        Place("spawnpoint2", new Vector3( 6.5f, 0.1f, 0f));

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        var list = EditorBuildSettings.scenes.ToList();
        if (!list.Any(s => s.path == Dst)) { list.Add(new EditorBuildSettingsScene(Dst, true)); EditorBuildSettings.scenes = list.ToArray(); }
        Debug.Log("GameJam: Map2 oluşturuldu (Assets/Scenes/Map2).");
    }
}
