using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Build Settings'i oynanabilirlik kontrolunden gecen haritalardan uretir.
// Harita listesi kodda yazili degil: yeni bir harita oynanabilir hale
// gelince bu komutu yeniden calistirmak onu havuza aliyor.
public static class GameFlowSetup
{
    const string BootScene = "Assets/Boot.unity";
    const string MenuScene = "Assets/MainMenu.unity";
    const string MapFolder = "Assets/Scenes";

    [MenuItem("GameJam/Oyun Akisini Kur")]
    public static void Setup()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(MenuScene) == null)
            throw new System.Exception("Menu sahnesi yok: " + MenuScene);

        string current = EditorSceneManager.GetActiveScene().path;
        var playable = new List<string>();

        foreach (string path in MapPaths())
        {
            Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            string problem = Unplayable(scene);

            if (problem == null) playable.Add(path);
            else Debug.LogWarning("GameFlow: " + Path.GetFileNameWithoutExtension(path) + " havuza alinmadi, " + problem);
        }

        EnsureMenuScript();
        EnsureBootScene();

        // 0. sira Boot: kullanici her zaman oradan basliyor. GameFlow
        // sahneleri indeksle degil isimle buldugu icin sira degisse de
        // bozulmuyor.
        EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(BootScene, true),
                new EditorBuildSettingsScene(MenuScene, true),
            }
            .Concat(playable.Select(p => new EditorBuildSettingsScene(p, true)))
            .ToArray();

        // EditorBuildSettings yazmasi tembel; acikca flush edilmezse
        // ProjectSettings diske dusmuyor.
        AssetDatabase.SaveAssets();

        if (!string.IsNullOrEmpty(current) && File.Exists(current))
            EditorSceneManager.OpenScene(current);

        Debug.Log("GAMEFLOW_SETUP_OK boot + menu + " + playable.Count + " harita: "
                  + string.Join(", ", playable.Select(Path.GetFileNameWithoutExtension)));
    }

    public static void SetupFromCLI()
    {
        Setup();
    }

    static IEnumerable<string> MapPaths()
    {
        return Directory.GetFiles(MapFolder, "Map*.unity")
            .Select(p => p.Replace('\\', '/'))
            .OrderBy(p => p);
    }

    // Oynanamaz bir haritayi havuza almak oyuncuyu bos bir sahneye dusurur.
    static string Unplayable(Scene scene)
    {
        GameObject[] roots = scene.GetRootGameObjects();

        int fighters = roots.SelectMany(r => r.GetComponentsInChildren<Fighter>(true)).Count();
        if (fighters != 2) return fighters + " Fighter var, 2 olmali";

        if (!roots.SelectMany(r => r.GetComponentsInChildren<MatchState>(true)).Any())
            return "MatchState yok";

        if (!roots.SelectMany(r => r.GetComponentsInChildren<MatchHudSkin>(true)).Any())
            return "MatchHudSkin yok, once GameJam > Skor Arayuzu Olustur";

        int spawns = roots.SelectMany(r => r.GetComponentsInChildren<Transform>(true))
            .Count(t => t.name.StartsWith("spawnpoint"));
        if (spawns < 2) return spawns + " spawn noktasi var, 2 olmali";

        return GroundProblem(roots);
    }

    static void EnsureBootScene()
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(BootScene) == null)
        {
            Scene created = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            new GameObject("Boot").AddComponent<Boot>();
            EditorSceneManager.SaveScene(created, BootScene);
            return;
        }

        Scene boot = EditorSceneManager.OpenScene(BootScene, OpenSceneMode.Single);
        if (!boot.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Boot>(true)).Any())
        {
            new GameObject("Boot").AddComponent<Boot>();
            EditorSceneManager.MarkSceneDirty(boot);
            EditorSceneManager.SaveScene(boot);
        }
    }

    // Haritalar zemini iki farkli sekilde kuruyor: bazilari tilemap'in
    // CompositeCollider2D'sini kullaniyor, PlatformSurfaces'in isledigi
    // haritalarda ise composite kapatilip elle BoxCollider2D + effector
    // konuyor. O yuzden "composite dolu mu" diye sormak yanlis; soru
    // "ortada zemin var mi" olmali.
    static string GroundProblem(GameObject[] roots)
    {
        var fighters = new HashSet<GameObject>(
            roots.SelectMany(r => r.GetComponentsInChildren<Fighter>(true)).Select(f => f.gameObject));

        int solid = 0;
        foreach (Collider2D col in roots.SelectMany(r => r.GetComponentsInChildren<Collider2D>(true)))
        {
            if (!col.enabled || col.isTrigger) continue;
            if (fighters.Contains(col.transform.root.gameObject)) continue;

            var composite = col as CompositeCollider2D;
            if (composite != null)
            {
                composite.GenerateGeometry();
                if (composite.shapeCount == 0) continue;
            }

            solid++;
        }

        return solid > 0 ? null : "zemin collider'i yok, karakterler icinden gecer";
    }

    // Sahne MainMenu script'ini GUID ile ariyor; meta o GUID'e ayarlandigi
    // icin bag kendiliginden cozuluyor. Yine de cozulmediyse burada tamamla.
    static void EnsureMenuScript()
    {
        Scene scene = EditorSceneManager.OpenScene(MenuScene, OpenSceneMode.Single);

        if (!scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<MainMenu>(true)).Any())
        {
            var host = scene.GetRootGameObjects().FirstOrDefault(r => r.GetComponent<Canvas>() != null)
                       ?? scene.GetRootGameObjects().First();
            host.AddComponent<MainMenu>();
            Debug.LogWarning("GameFlow: MainMenu bileseni yoktu, eklendi. Butonun onClick bagi elle kontrol edilmeli.");
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }
}
