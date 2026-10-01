using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

// Menü: GameJam > Platformları ve Spawnları Düzelt
// 1) Platform çarpışması: tile'ların kalın çarpışması yerine her platformun ÜSTÜNE ince (0.25)
//    tek yönlü bir şerit koyar. Kalın kutuda karakter alttan zıplayınca platformun içinde
//    takılıp durabiliyordu; ince şeritte içinde kalacak yer yok.
// 2) Spawn: spawnpoint1/2'yi altlarındaki platformun tam üstüne oturtur, Fighter'ların
//    spawnPoint bağlantısını kurar (Map2-5'te boştu, düşen karakter (0,0)'da boşlukta doğuyordu)
//    ve karakterleri başlangıçta spawn noktasına koyar. Map1-Map5'e uygular, tekrar çalıştırmak güvenli.
public static class FixPlatformsAndSpawns
{
    static readonly string[] Scenes =
    {
        "Assets/Scenes/Map1.unity", "Assets/Scenes/Map2.unity", "Assets/Scenes/Map3.unity",
        "Assets/Scenes/Map4.unity", "Assets/Scenes/Map5.unity"
    };
    const string Holder = "PlatformColliders";
    const float Thickness = 0.25f;

    struct Surface { public float xMin, xMax, top; }

    [MenuItem("GameJam/Platformları ve Spawnları Düzelt")]
    static void Run()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        var mat = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>("Assets/Settings/Frictionless.physicsMaterial2D");
        string current = EditorSceneManager.GetActiveScene().path;

        foreach (var path in Scenes)
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) == null) continue;
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            var go = GameObject.Find("Platforms");
            var tm = go ? go.GetComponent<Tilemap>() : null;
            if (tm == null) { Debug.LogError("GameJam: Platforms yok: " + path); continue; }

            // --- eski çarpışmaları kapat ---
            foreach (var c in go.GetComponents<Collider2D>()) c.enabled = false;
            foreach (var e in go.GetComponents<PlatformEffector2D>()) e.enabled = false;
            var old = go.transform.Find(Holder);
            if (old) Object.DestroyImmediate(old.gameObject);

            var holder = new GameObject(Holder);
            holder.transform.SetParent(go.transform, false);
            holder.layer = go.layer;

            // --- yüzeyleri bul ve ince şerit koy ---
            var surfaces = new List<Surface>();
            tm.CompressBounds();
            var b = tm.cellBounds;
            for (int y = b.yMin; y < b.yMax; y++)
            {
                int start = int.MinValue;
                for (int x = b.xMin; x <= b.xMax; x++)
                {
                    bool surf = x < b.xMax && tm.HasTile(new Vector3Int(x, y, 0)) && !tm.HasTile(new Vector3Int(x, y + 1, 0));
                    if (surf && start == int.MinValue) start = x;
                    if (!surf && start != int.MinValue)
                    {
                        Vector3 a = tm.CellToWorld(new Vector3Int(start, y, 0));
                        Vector3 c = tm.CellToWorld(new Vector3Int(x, y + 1, 0));
                        var s = new Surface { xMin = a.x, xMax = c.x, top = c.y };
                        surfaces.Add(s);

                        var strip = new GameObject($"Surface_{y}_{start}_{x - 1}");
                        strip.layer = go.layer;
                        strip.transform.SetParent(holder.transform, true);
                        strip.transform.position = new Vector3((s.xMin + s.xMax) / 2f, s.top - Thickness / 2f, 0f);
                        var box = strip.AddComponent<BoxCollider2D>();
                        box.size = new Vector2(s.xMax - s.xMin, Thickness);
                        box.sharedMaterial = mat;
                        box.usedByEffector = true;
                        var eff = strip.AddComponent<PlatformEffector2D>();
                        eff.useOneWay = true;
                        eff.surfaceArc = 160f;
                        eff.useSideFriction = false;
                        start = int.MinValue;
                    }
                }
            }

            // --- spawnlar ---
            var fighters = Object.FindObjectsByType<Fighter>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 1; i <= 2; i++)
            {
                var sp = GameObject.Find("spawnpoint" + i);
                if (sp == null) { Debug.LogError($"GameJam: {path} spawnpoint{i} yok"); continue; }
                var p = sp.transform.position;
                var surf = PickSurface(surfaces, p);
                float x = Mathf.Clamp(p.x, surf.xMin + 0.6f, surf.xMax - 0.6f);
                sp.transform.position = new Vector3(x, surf.top + 0.05f, 0f);
                EditorUtility.SetDirty(sp.transform);

                foreach (var f in fighters)
                {
                    if (f.playerIndex != i) continue;
                    f.spawnPoint = sp.transform;
                    f.transform.position = sp.transform.position;
                    PrefabUtility.RecordPrefabInstancePropertyModifications(f);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(f.transform);
                    EditorUtility.SetDirty(f);
                }
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"GameJam: {path} -> {surfaces.Count} ince platform, spawnlar platforma oturtuldu ({fighters.Length} dövüşçü).");
        }
        if (!string.IsNullOrEmpty(current)) EditorSceneManager.OpenScene(current);
    }

    // Spawn'ın altındaki en yakın platform; yoksa yatayda en yakın olan
    static Surface PickSurface(List<Surface> list, Vector3 p)
    {
        Surface best = list[0]; float bestScore = float.MaxValue;
        foreach (var s in list)
        {
            float dx = p.x < s.xMin ? s.xMin - p.x : p.x > s.xMax ? p.x - s.xMax : 0f;
            float dy = s.top <= p.y + 0.5f ? p.y - s.top : 100f + (s.top - p.y);
            float score = dx * 10f + Mathf.Abs(dy);
            if (score < bestScore) { bestScore = score; best = s; }
        }
        return best;
    }
}
