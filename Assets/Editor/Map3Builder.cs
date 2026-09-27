using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

// Menü: GameJam > Map3 Oluştur
// "İkiz Kuleler": iki kulenin arasında uçurum, üstünde basamak platformlar. Gündüz arka planı.
// SampleScene'in kopyasından kurulur, SampleScene'e dokunmaz.
public static class Map3Builder
{
    const string Src = "Assets/Scenes/SampleScene.unity";
    const string Dst = "Assets/Scenes/Map3.unity";
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

    [MenuItem("GameJam/Map3 Oluştur")]
    static void Build()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(Src) == null)
        { EditorUtility.DisplayDialog("GameJam", Src + " bulunamadı.", "Tamam"); return; }
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(Dst) != null)
        {
            if (!EditorUtility.DisplayDialog("GameJam", "Map3 zaten var. Üzerine yazılsın mı?", "Evet", "Hayır")) return;
            AssetDatabase.DeleteAsset(Dst);
        }
        AssetDatabase.CopyAsset(Src, Dst);
        var scene = EditorSceneManager.OpenScene(Dst);

        var tm = GameObject.Find("Platforms")?.GetComponent<Tilemap>();
        if (tm == null) { EditorUtility.DisplayDialog("GameJam", "'Platforms' Tilemap'i bulunamadı.", "Tamam"); return; }
        tm.ClearAllTiles();

        // İkiz kuleler: ekranın altına kadar inen gövde
        foreach (var (x0, x1) in new[] { (-8, -5), (4, 7) })
        {
            Run(tm, x0, x1, -3, Top2);
            for (int y = -4; y >= -6; y--) Run(tm, x0, x1, y, Body);
        }
        Run(tm, -3, -2, -1, Cap);   // uçurum üstü basamaklar
        Run(tm, 1, 2, -1, Cap);
        Run(tm, -1, 0, 1, Cap);     // orta tepe
        Run(tm, -8, -6, 1, Cap);    // kule üstü çıkıntılar
        Run(tm, 5, 7, 1, Cap);

        var bg = GameObject.Find("Background");
        var day = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Background/Background_Day.png");
        if (bg && day) bg.GetComponent<SpriteRenderer>().sprite = day;

        Place("HeroKnight",  new Vector3(-6f, -1.9f, 0f));
        Place("LightBandit", new Vector3( 6f, -1.9f, 0f));
        Place("spawnpoint1", new Vector3(-6f, -1.9f, 0f));
        Place("spawnpoint2", new Vector3( 6f, -1.9f, 0f));

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        var list = EditorBuildSettings.scenes.ToList();
        if (!list.Any(s => s.path == Dst)) { list.Add(new EditorBuildSettingsScene(Dst, true)); EditorBuildSettings.scenes = list.ToArray(); }
        Debug.Log("GameJam: Map3 oluşturuldu (Assets/Scenes/Map3).");
    }
}
