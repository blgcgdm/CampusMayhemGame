using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

// DemoScene'i iki oyunculu hale getirir. Sahneyi sıfırdan ÜRETMEZ:
// tilemap elle boyanmış, onu yeniden üretmek emeği silerdi. Sadece
// dövüşçü bağlantılarına dokunur ve tekrar tekrar çalıştırılabilir.
public static class DemoSceneSetup
{
    const string SceneName = "DemoScene";

    // Sahne yolu sabit yazilmiyor: sahne bir kez Assets/ kokunden
    // Assets/Scenes/ altina tasindi ve sabit yol oracikta kirildi.
    public static string ScenePath
    {
        get
        {
            foreach (string guid in AssetDatabase.FindAssets("t:Scene " + SceneName))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (Path.GetFileNameWithoutExtension(path) == SceneName) return path;
            }
            throw new System.Exception(SceneName + ".unity projede bulunamadi");
        }
    }
    const string FrictionlessPath = "Assets/Settings/Frictionless.physicsMaterial2D";
    const int FighterLayer = 8;
    const int GroundLayer = 0;

    struct Profile
    {
        public string objectName;
        public string spawnName;
        public int index;
        public Fighter.KeyMap keys;
        public string airSpeedFloat;
        public string attackTrigger;
        public int runState;
        public bool spriteFacesRight;
    }

    static readonly Profile[] Profiles =
    {
        new Profile {
            objectName = "HeroKnight", spawnName = "spawnpoint1", index = 1,
            keys = new Fighter.KeyMap { left = KeyCode.A, right = KeyCode.D, jump = KeyCode.W, attack = KeyCode.LeftShift },
            airSpeedFloat = "AirSpeedY", attackTrigger = "Attack1", runState = 1, spriteFacesRight = true,
        },
        new Profile {
            objectName = "LightBandit", spawnName = "spawnpoint2", index = 2,
            keys = new Fighter.KeyMap { left = KeyCode.LeftArrow, right = KeyCode.RightArrow, jump = KeyCode.UpArrow, attack = KeyCode.RightShift },
            // LightBandit'in Animator'u AirSpeed ve tek Attack kullanıyor,
            // AnimState'te 2 = Run, ve sprite'ı varsayılan olarak sola bakıyor.
            airSpeedFloat = "AirSpeed", attackTrigger = "Attack", runState = 2, spriteFacesRight = false,
        },
    };

    [MenuItem("Campus Mayhem/Setup Two Player DemoScene")]
    public static void Setup()
    {
        Scene scene = EnsureScene();

        PhysicsMaterial2D frictionless = EnsureFrictionless();
        foreach (Profile profile in Profiles) Configure(scene, profile, frictionless);

        RepairTilemap(scene, false);
        BuildMatch(scene);
        AddSceneToBuildSettings();

        Physics2D.IgnoreLayerCollision(FighterLayer, FighterLayer, true);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();

        Debug.Log("DEMO_SETUP_OK");
    }

    public static void SetupFromCLI()
    {
        Setup();
    }

    static Scene EnsureScene()
    {
        Scene scene = EditorSceneManager.GetActiveScene();
        if (scene.path != ScenePath) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        return scene;
    }

    static void Configure(Scene scene, Profile profile, PhysicsMaterial2D frictionless)
    {
        GameObject go = Find(scene, profile.objectName);
        if (go == null) throw new System.Exception("Sahnede bulunamadi: " + profile.objectName);

        if (PrefabUtility.IsPartOfPrefabInstance(go))
            PrefabUtility.UnpackPrefabInstance(go, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);

        StripPackScripts(go);
        SetLayerRecursively(go, FighterLayer);

        Transform groundCheck = ExtractGroundCheck(go);

        // Varsayilan 0.4 surtunme karakteri duvara asiyordu: duvara dogru
        // bastirinca dusmeden havada asili kaliyordu. Zemin hareketi
        // surtunmeye hic guvenmiyor, kontrolcu hizi dogrudan yaziyor.
        foreach (Collider2D col in go.GetComponentsInChildren<Collider2D>(true)) col.sharedMaterial = frictionless;

        // Prefablar farkli geliyordu: HeroKnight Continuous, LightBandit
        // Discrete. Versus oyununda bu adaletsizlik; Discrete olan hizliyken
        // ince platformlarin icinden gecebiliyor.
        var body = go.GetComponent<Rigidbody2D>();
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        var combat = Ensure<FighterCombat>(go);
        combat.fighterLayers = 1 << FighterLayer;

        var fighter = Ensure<Fighter>(go);
        fighter.playerIndex = profile.index;
        fighter.keys = profile.keys;
        fighter.groundCheck = groundCheck;
        fighter.groundLayer = 1 << GroundLayer;
        fighter.spawnPoint = RequireTransform(scene, profile.spawnName);

        var anim = Ensure<FighterAnimator>(go);
        anim.airSpeedFloat = profile.airSpeedFloat;
        anim.attackTrigger = profile.attackTrigger;
        anim.runState = profile.runState;
        anim.spriteFacesRight = profile.spriteFacesRight;

        EditorUtility.SetDirty(go);
    }

    // Paketlerin demo kontrolcüleri oyuncu girdisini kendileri okuyor;
    // ortak kontrolcüyle birlikte durursa iki kere hareket olur.
    static void StripPackScripts(GameObject root)
    {
        foreach (var c in root.GetComponentsInChildren<HeroKnight>(true).Cast<Component>()
                 .Concat(root.GetComponentsInChildren<Bandit>(true))
                 .Concat(root.GetComponentsInChildren<Sensor_HeroKnight>(true))
                 .Concat(root.GetComponentsInChildren<Sensor_Bandit>(true))
                 .ToList())
        {
            Object.DestroyImmediate(c);
        }
    }

    // HeroKnight'in duvar sensorleri sadece wall-slide icindi, o mekanik
    // ortak kontrolcude yok. GroundSensor ise konumu dogru oldugu icin
    // zemin kontrol noktasi olarak yeniden kullaniliyor.
    static Transform ExtractGroundCheck(GameObject root)
    {
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true).ToList())
        {
            if (child == root.transform) continue;
            if (child.name.StartsWith("WallSensor")) Object.DestroyImmediate(child.gameObject);
        }

        Transform ground = root.transform.Find("GroundSensor") ?? root.transform.Find("GroundCheck");
        if (ground == null) throw new System.Exception("GroundSensor cocugu yok: " + root.name);

        foreach (Collider2D col in ground.GetComponents<Collider2D>()) Object.DestroyImmediate(col);
        ground.name = "GroundCheck";
        return ground;
    }

    // Sahneye boyanmis tile'lar hic commit edilmemis asset'lere bakiyordu;
    // GUID'leri hicbir seye cozulmuyor ve Unity yerlerine "Invalid Tile
    // Sprite" yer tutucusunu koyuyor. Yer tutucunun ne gorseli ne carpisma
    // sekli var, zemin bu yuzden tamamen yok olmustu.
    [MenuItem("Campus Mayhem/Repaint Platform Tiles")]
    public static void RepaintTiles()
    {
        RepairTilemap(EnsureScene(), true);
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
    }

    static void RepairTilemap(Scene scene, bool force)
    {
        GameObject go = Find(scene, "Platforms");
        if (go == null) return;

        var map = go.GetComponent<Tilemap>();
        if (map == null) return;

        var occupied = new List<Vector3Int>();
        foreach (Vector3Int cell in map.cellBounds.allPositionsWithin)
        {
            if (map.HasTile(cell)) occupied.Add(cell);
        }

        Tile top = LoadTile(0);
        Tile fill = LoadTile(1);

        int repaired = 0;
        foreach (Vector3Int cell in occupied)
        {
            // Saglam boyanmis hucrelere dokunma; bu komut tekrar
            // calistirildiginda elle yapilmis bir seviyeyi ezmemeli.
            // Bilerek yeniden boyamak icin Repaint Platform Tiles var.
            if (!force && !IsBroken(map.GetTile(cell))) continue;

            // _0 dolu bir blok degil, cogunlukla saydam bir tepe susu.
            // Tek siralik platformlara konunca ortada govde kalmiyor, o
            // yuzden sadece altinda baska hucre olan tepelere geliyor.
            bool openAbove = !occupied.Contains(cell + Vector3Int.up);
            bool hasBelow = occupied.Contains(cell + Vector3Int.down);
            map.SetTile(cell, openAbove && hasBelow ? top : fill);
            repaired++;
        }

        var composite = go.GetComponent<CompositeCollider2D>();
        if (composite != null) composite.GenerateGeometry();

        Debug.Log("Tilemap: " + repaired + " hucre onarildi, " + occupied.Count + " dolu hucre");
    }

    static bool IsBroken(TileBase tile)
    {
        return tile == null || string.IsNullOrEmpty(AssetDatabase.GetAssetPath(tile));
    }

    // Sprite'larin fizik sekli yok, o yuzden colliderType Sprite kalirsa
    // TilemapCollider2D hicbir geometri uretmiyor. Grid tam hucre karesi verir.
    static Tile LoadTile(int index)
    {
        const string folder = "Assets/Hero Knight-Pixel Art/Environment/EnvironmentTiles_";
        var tile = AssetDatabase.LoadAssetAtPath<Tile>(folder + index + ".asset");
        if (tile == null) throw new System.Exception("Tile bulunamadi: " + folder + index + ".asset");

        if (tile.colliderType != Tile.ColliderType.Grid)
        {
            tile.colliderType = Tile.ColliderType.Grid;
            EditorUtility.SetDirty(tile);
        }
        return tile;
    }

    // HUD her koşuda sıfırdan kurulur; yeniden çalıştırmak ikinci bir
    // Canvas üretmesin diye eskisi önce siliniyor.
    // HUD artik MatchHudSkin; onu ScoreUIBuilder kuruyor. Burada sadece
    // MatchState kaliyor, eski sade HUD objesi de temizleniyor.
    static void BuildMatch(Scene scene)
    {
        Destroy(Find(scene, "HUD"));
        Destroy(Find(scene, "Match"));

        var matchGo = new GameObject("Match");
        SceneManager.MoveGameObjectToScene(matchGo, scene);
        matchGo.AddComponent<MatchState>();
    }

    // Build Settings listesini GameFlowSetup yonetiyor; bu sadece
    // DemoScene tek basina acilip test edilebilsin diye duruyor.
    static void AddSceneToBuildSettings()
    {
        if (EditorBuildSettings.scenes.Any(s => s.path == ScenePath && s.enabled)) return;

        EditorBuildSettings.scenes = EditorBuildSettings.scenes
            .Where(s => s.path != ScenePath)
            .Concat(new[] { new EditorBuildSettingsScene(ScenePath, true) })
            .ToArray();
    }

    static void Destroy(GameObject go)
    {
        if (go != null) Object.DestroyImmediate(go);
    }

    // Asset paketinde de sifir surtunmeli bir materyal var ama ona
    // baglanmiyoruz: bu projenin ilk hatasi kopan bir paket GUID'iydi.
    static PhysicsMaterial2D EnsureFrictionless()
    {
        var existing = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(FrictionlessPath);
        if (existing != null) return existing;

        Directory.CreateDirectory(Path.GetDirectoryName(FrictionlessPath));
        var mat = new PhysicsMaterial2D("Frictionless") { friction = 0f, bounciness = 0f };
        AssetDatabase.CreateAsset(mat, FrictionlessPath);
        return mat;
    }

    static T Ensure<T>(GameObject go) where T : Component
    {
        return go.GetComponent<T>() ?? go.AddComponent<T>();
    }

    static Transform RequireTransform(Scene scene, string name)
    {
        GameObject go = Find(scene, name);
        if (go == null) throw new System.Exception("Sahnede bulunamadi: " + name);
        return go.transform;
    }

    static GameObject Find(Scene scene, string name)
    {
        return scene.GetRootGameObjects()
            .SelectMany(r => r.GetComponentsInChildren<Transform>(true))
            .Where(t => t.name == name)
            .Select(t => t.gameObject)
            .FirstOrDefault();
    }

    static void SetLayerRecursively(GameObject go, int layer)
    {
        go.layer = layer;
        foreach (Transform child in go.transform) SetLayerRecursively(child.gameObject, layer);
    }
}
