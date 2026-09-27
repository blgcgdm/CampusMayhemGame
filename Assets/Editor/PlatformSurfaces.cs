using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

// Menü: GameJam > Platform Çarpışmalarını Kur
// Map2-Map5'te tilemap çarpışması güvenilir üretilmediği için (Map1'in eski şekli kalıyordu),
// çarpışmayı tile'lardan bağımsız, elle kurar: her platformun ÜST yüzeyine tek yönlü bir
// BoxCollider2D koyar. Görünen her platformun üstünde durulur, altından zıplayınca içinden geçilir.
// Tekrar çalıştırmak güvenli (eskileri silip yeniden kurar).
public static class PlatformSurfaces
{
    static readonly string[] Scenes =
    {
        "Assets/Scenes/Map2.unity", "Assets/Scenes/Map3.unity",
        "Assets/Scenes/Map4.unity", "Assets/Scenes/Map5.unity"
    };
    const string Holder = "PlatformColliders";

    [MenuItem("GameJam/Platform Çarpışmalarını Kur")]
    static void Run()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        var friction = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>("Assets/Settings/Frictionless.physicsMaterial2D");
        string current = EditorSceneManager.GetActiveScene().path;

        foreach (var path in Scenes)
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) == null) continue;
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            var go = GameObject.Find("Platforms");
            var tm = go ? go.GetComponent<Tilemap>() : null;
            if (tm == null) { Debug.LogError("GameJam: Platforms yok: " + path); continue; }

            // Eski (bozuk) tilemap çarpışmasını kapat
            var tc = go.GetComponent<TilemapCollider2D>(); if (tc) tc.enabled = false;
            var comp = go.GetComponent<CompositeCollider2D>(); if (comp) comp.enabled = false;
            var eff = go.GetComponent<PlatformEffector2D>(); if (eff) eff.enabled = false;

            var old = go.transform.Find(Holder);
            if (old) Object.DestroyImmediate(old.gameObject);
            var holder = new GameObject(Holder);
            holder.transform.SetParent(go.transform, false);
            holder.layer = go.layer;

            // Üst yüzey hücreleri: üstü boş olan dolu hücreler, yatayda birleştirilmiş
            tm.CompressBounds();
            var b = tm.cellBounds;
            int count = 0;
            for (int y = b.yMin; y < b.yMax; y++)
            {
                int start = int.MinValue;
                for (int x = b.xMin; x <= b.xMax; x++)
                {
                    bool surf = x < b.xMax && tm.HasTile(new Vector3Int(x, y, 0)) && !tm.HasTile(new Vector3Int(x, y + 1, 0));
                    if (surf && start == int.MinValue) start = x;
                    if (!surf && start != int.MinValue)
                    {
                        AddSurface(tm, holder.transform, start, x - 1, y, friction);
                        start = int.MinValue; count++;
                    }
                }
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"GameJam: {path} -> {count} platform yüzeyi kuruldu.");
        }
        if (!string.IsNullOrEmpty(current)) EditorSceneManager.OpenScene(current);
    }

    static void AddSurface(Tilemap tm, Transform parent, int x0, int x1, int y, PhysicsMaterial2D mat)
    {
        Vector3 a = tm.CellToWorld(new Vector3Int(x0, y, 0));
        Vector3 c = tm.CellToWorld(new Vector3Int(x1 + 1, y + 1, 0));
        var s = new GameObject($"Surface_{y}_{x0}_{x1}");
        s.layer = parent.gameObject.layer;
        s.transform.SetParent(parent, true);
        s.transform.position = new Vector3((a.x + c.x) / 2f, (a.y + c.y) / 2f, 0f);
        var box = s.AddComponent<BoxCollider2D>();
        box.size = new Vector2(c.x - a.x, c.y - a.y);
        box.sharedMaterial = mat;
        box.usedByEffector = true;
        var e = s.AddComponent<PlatformEffector2D>();
        e.useOneWay = true;
        e.surfaceArc = 170f;
        e.useSideFriction = false;
    }
}
