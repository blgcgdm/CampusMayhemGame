using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

// Menü: GameJam > Platformları Güzelleştir
// "Platforms" tilemap'indeki her yatay platformu başlıklı (kenarlı) platform tile'larıyla değiştirir.
// En alttaki platformlar 2 kat yüksek yapılır. Ctrl/Cmd+Z ile geri alınabilir.
public static class MapDecorator
{
    const string Path = "Assets/Art/Tiles/Medieval_tiles_free2_{0}.asset";
    static readonly int[] Cap  = { 825, 826, 827, 828, 829, 830, 831, 832, 833 }; // tek katlı platform
    static readonly int[] Top2 = { 882, 883, 884, 885, 886, 887, 888, 889, 890 }; // 2 katlı: üst
    static readonly int[] Bot2 = { 906, 907, 908, 909, 910, 911, 912, 913, 914 }; // 2 katlı: alt

    static TileBase Load(int i) => AssetDatabase.LoadAssetAtPath<TileBase>(string.Format(Path, i));

    // 0 = sol uç, 8 = sağ uç, 1..7 = orta parçalar (sırayla tekrar eder)
    static TileBase Pick(int[] set, int i, int len) =>
        Load(i == 0 ? set[0] : i == len - 1 ? set[8] : set[1 + (i - 1) % 7]);

    [MenuItem("GameJam/Platformları Güzelleştir")]
    static void Decorate()
    {
        var go = GameObject.Find("Platforms");
        var tm = go ? go.GetComponent<Tilemap>() : null;
        if (tm == null) { EditorUtility.DisplayDialog("GameJam", "'Platforms' adında bir Tilemap bulunamadı.", "Tamam"); return; }
        if (Load(Cap[0]) == null) { EditorUtility.DisplayDialog("GameJam", "Tile dosyaları Assets/Art/Tiles içinde bulunamadı.", "Tamam"); return; }

        tm.CompressBounds();
        var b = tm.cellBounds;
        var rows = new List<(int y, int x0, int x1)>();
        for (int y = b.yMin; y < b.yMax; y++)
        {
            int start = int.MinValue;
            for (int x = b.xMin; x <= b.xMax; x++)
            {
                bool has = x < b.xMax && tm.HasTile(new Vector3Int(x, y, 0));
                if (has && start == int.MinValue) start = x;
                if (!has && start != int.MinValue) { rows.Add((y, start, x - 1)); start = int.MinValue; }
            }
        }
        if (rows.Count == 0) return;
        int lowest = int.MaxValue;
        foreach (var r in rows) lowest = Mathf.Min(lowest, r.y);

        Undo.RegisterCompleteObjectUndo(tm, "Platformları Güzelleştir");
        foreach (var r in rows)
        {
            int len = r.x1 - r.x0 + 1;
            bool tall = r.y == lowest && !tm.HasTile(new Vector3Int(r.x0, r.y - 1, 0));
            for (int i = 0; i < len; i++)
            {
                var p = new Vector3Int(r.x0 + i, r.y, 0);
                tm.SetTile(p, Pick(tall ? Top2 : Cap, i, len));
                if (tall) tm.SetTile(p + Vector3Int.down, Pick(Bot2, i, len));
            }
        }
        EditorUtility.SetDirty(tm);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(go.scene);
        Debug.Log($"GameJam: {rows.Count} platform güzelleştirildi.");
    }
}
