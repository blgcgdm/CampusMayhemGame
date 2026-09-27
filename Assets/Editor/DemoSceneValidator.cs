using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

// DemoScene'in iki oyunculu bağlantılarını doğrular. Unity Test Framework
// kullanılmıyor: asmdef'li bir assembly Assembly-CSharp'ı göremez, yani
// Fighter tipine erişemezdi. Assets/Editor görebiliyor.
public static class DemoSceneValidator
{
    // Tek kaynak: kurulum script'i sahneyi aramayla buluyor.
    static string ScenePath { get { return DemoSceneSetup.ScenePath; } }
    const int FighterLayer = 8;
    const int GroundLayer = 0;

    static readonly List<string> failures = new List<string>();

    [MenuItem("Campus Mayhem/Validate Two Player DemoScene")]
    public static void Validate()
    {
        failures.Clear();

        Scene scene = EditorSceneManager.GetActiveScene().path == ScenePath
            ? EditorSceneManager.GetActiveScene()
            : EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        GameObject[] roots = scene.GetRootGameObjects();
        Fighter[] fighters = roots.SelectMany(r => r.GetComponentsInChildren<Fighter>(true)).ToArray();

        Require(fighters.Length == 2, "Sahnede " + fighters.Length + " Fighter var, 2 olmali");
        CheckNoPackScripts(roots);
        CheckKeysAreDisjoint(fighters);
        Require(Physics2D.GetIgnoreLayerCollision(FighterLayer, FighterLayer),
                "Layer " + FighterLayer + " kendisiyle carpisiyor, oyuncular birbirini itiyor");

        foreach (Fighter f in fighters) CheckFighter(f);
        CheckMatch(roots);
        CheckGround(roots);

        Require(fighters.Select(f => f.playerIndex).Distinct().Count() == fighters.Length,
                "playerIndex degerleri benzersiz degil");

        foreach (string failure in failures) Debug.LogError("VALIDATE FAIL: " + failure);

        if (failures.Count == 0) Debug.Log("DEMO_VALIDATE_OK");
        else Debug.LogError("DEMO_VALIDATE_FAILED: " + failures.Count + " ihlal");
    }

    // Tek sahne yerine akisin tamami: menu 0. sirada mi, havuzda harita
    // var mi, her harita oynanabilir mi, silinen MatchHud'dan missing
    // script kalmis mi.
    [MenuItem("GameJam/Oyun Akisini Dogrula")]
    public static void ValidateFlow()
    {
        failures.Clear();

        var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).ToArray();

        if (scenes.Length == 0) { Report(); return; }

        Require(scenes[0].path == "Assets/Boot.unity",
                "Build Settings 0. sirasi " + scenes[0].path + ", Boot.unity olmali");
        Require(scenes.Any(e => e.path == "Assets/MainMenu.unity"), "MainMenu Build Settings'te yok");
        Require(scenes.Count(e => e.path.Contains("/Map")) >= 1, "Havuzda hic harita yok");

        foreach (var entry in scenes)
        {
            Require(System.IO.File.Exists(entry.path), "Build Settings'te olmayan sahne: " + entry.path);
        }

        foreach (var entry in scenes.Where(e => e.path.Contains("/Map") && System.IO.File.Exists(e.path)))
        {
            Scene map = EditorSceneManager.OpenScene(entry.path, OpenSceneMode.Single);
            string name = System.IO.Path.GetFileNameWithoutExtension(entry.path);
            GameObject[] roots = map.GetRootGameObjects();

            CheckNoMissingScripts(roots);
            Require(roots.SelectMany(r => r.GetComponentsInChildren<Fighter>(true)).Count() == 2, name + ": 2 Fighter olmali");
            Require(roots.SelectMany(r => r.GetComponentsInChildren<MatchState>(true)).Any(), name + ": MatchState yok");
            Require(roots.SelectMany(r => r.GetComponentsInChildren<MatchHudSkin>(true)).Any(), name + ": MatchHudSkin yok");
        }

        Scene menu = EditorSceneManager.OpenScene("Assets/MainMenu.unity", OpenSceneMode.Single);
        GameObject[] menuRoots = menu.GetRootGameObjects();
        CheckNoMissingScripts(menuRoots);
        Require(menuRoots.SelectMany(r => r.GetComponentsInChildren<MainMenu>(true)).Any(), "Menu sahnesinde MainMenu bileseni yok");

        Report();
    }

    public static void ValidateFlowFromCLI()
    {
        ValidateFlow();
        EditorApplication.Exit(failures.Count == 0 ? 0 : 1);
    }

    static void Report()
    {
        foreach (string failure in failures) Debug.LogError("VALIDATE FAIL: " + failure);

        if (failures.Count == 0) Debug.Log("FLOW_VALIDATE_OK");
        else Debug.LogError("FLOW_VALIDATE_FAILED: " + failures.Count + " ihlal");
    }

    public static void ValidateFromCLI()
    {
        Validate();
        EditorApplication.Exit(failures.Count == 0 ? 0 : 1);
    }

    // Zemin olmadan oyun oynanmiyor: karakterler her seyin icinden gecip
    // sonsuz ring-out dongusune giriyordu.
    static void CheckGround(GameObject[] roots)
    {
        var maps = roots.SelectMany(r => r.GetComponentsInChildren<Tilemap>(true)).ToArray();
        if (maps.Length == 0) { Fail("Sahnede Tilemap yok"); return; }

        foreach (Tilemap map in maps)
        {
            int broken = 0;
            foreach (Vector3Int cell in map.cellBounds.allPositionsWithin)
            {
                TileBase tile = map.GetTile(cell);
                if (tile == null) continue;
                if (string.IsNullOrEmpty(AssetDatabase.GetAssetPath(tile))) broken++;
            }
            Require(broken == 0, map.name + ": " + broken + " hucrede cozulmeyen tile var, o hucrelerde zemin yok");

            var composite = map.GetComponent<CompositeCollider2D>();
            if (composite == null) continue;

            // Geometri kurulumdan hemen sonra henuz oturmamis olabiliyor;
            // yeniden uretmek idempotent ve kontrolu deterministik yapiyor.
            composite.GenerateGeometry();
            Require(composite.shapeCount > 0,
                    map.name + ": CompositeCollider2D bos (shapeCount 0), karakterler icinden gecer");
        }
    }

    static void CheckMatch(GameObject[] roots)
    {
        var states = roots.SelectMany(r => r.GetComponentsInChildren<MatchState>(true)).ToArray();
        Require(states.Length == 1, "Sahnede " + states.Length + " MatchState var, 1 olmali");
        if (states.Length == 1)
        {
            Require(states[0].matchDuration > 0f, "matchDuration sifir veya negatif");
        }

        var skins = roots.SelectMany(r => r.GetComponentsInChildren<MatchHudSkin>(true)).ToArray();
        Require(skins.Length == 1, "Sahnede " + skins.Length + " MatchHudSkin var, 1 olmali");
        if (skins.Length != 1) return;

        MatchHudSkin skin = skins[0];
        Require(skin.hudRoot != null, "MatchHudSkin.hudRoot bagli degil");
        Require(skin.timerText != null, "MatchHudSkin.timerText bagli degil");
        Require(skin.endScreen != null, "MatchHudSkin.endScreen bagli degil");
        Require(skin.winnerText != null, "MatchHudSkin.winnerText bagli degil");
        Require(skin.goldMedal != null && skin.silverMedal != null, "MatchHudSkin madalya sprite'lari bagli degil");

        for (int i = 0; i < 2; i++)
        {
            Require(skin.scoreTexts.Length > i && skin.scoreTexts[i] != null, "MatchHudSkin.scoreTexts[" + i + "] bagli degil");
            Require(skin.rowTexts.Length > i && skin.rowTexts[i] != null, "MatchHudSkin.rowTexts[" + i + "] bagli degil");
            Require(skin.rowMedals.Length > i && skin.rowMedals[i] != null, "MatchHudSkin.rowMedals[" + i + "] bagli degil");
        }
    }

    // Bildirilen bug tam olarak buydu: tek bir eksene iki karakter
    // baglanmisti. Ayni tus iki oyuncuda gorunurse bir daha sessizce
    // gecmesin.
    static void CheckKeysAreDisjoint(Fighter[] fighters)
    {
        for (int a = 0; a < fighters.Length; a++)
        {
            for (int b = a + 1; b < fighters.Length; b++)
            {
                var shared = fighters[a].keys.All().Intersect(fighters[b].keys.All()).ToArray();
                Require(shared.Length == 0,
                        fighters[a].name + " ve " + fighters[b].name + " ayni tuslari paylasiyor: "
                        + string.Join(", ", shared));
            }
        }

        foreach (Fighter f in fighters)
        {
            var keys = f.keys.All();
            Require(keys.Distinct().Count() == keys.Length, f.name + " ayni tusu iki role vermis");
            Require(keys.All(k => k != KeyCode.None), f.name + " atanmamis tus var");
        }
    }

    static void CheckFighter(Fighter f)
    {
        string who = f.name;

        Require(f.gameObject.layer == FighterLayer, who + " layer " + f.gameObject.layer + ", " + FighterLayer + " olmali");
        var body = f.GetComponent<Rigidbody2D>();
        if (body == null) Fail(who + " Rigidbody2D yok");
        else Require(body.collisionDetectionMode == CollisionDetectionMode2D.Continuous,
                     who + " collisionDetectionMode " + body.collisionDetectionMode + ", iki oyuncu da Continuous olmali");
        Require(f.spawnPoint != null, who + " spawnPoint bagli degil");
        Require(f.groundCheck != null, who + " groundCheck bagli degil");
        Require(f.groundLayer.value == 1 << GroundLayer,
                who + " groundLayer " + f.groundLayer.value + ", " + (1 << GroundLayer) + " olmali");

        var combat = f.GetComponent<FighterCombat>();
        if (combat == null) { Fail(who + " FighterCombat yok"); return; }
        Require(combat.fighterLayers.value == 1 << FighterLayer,
                who + " fighterLayers " + combat.fighterLayers.value + ", " + (1 << FighterLayer) + " olmali");

        foreach (Collider2D col in f.GetComponentsInChildren<Collider2D>(true))
        {
            Require(col.sharedMaterial != null && col.sharedMaterial.friction == 0f,
                    who + " collider surtunmesi sifir degil, karakter duvara yapisir: " + col.name);
        }

        CheckAnimatorParameters(f);
    }

    // Adaptorun bekledigi parametre gercekten yoksa Animator cagrisi
    // sessizce hicbir sey yapar. Iki paketin isimleri farkli oldugu icin
    // bu hata cok kolay olusuyor, o yuzden burada yakaliyoruz.
    static void CheckAnimatorParameters(Fighter f)
    {
        var anim = f.GetComponent<FighterAnimator>();
        if (anim == null) { Fail(f.name + " FighterAnimator yok"); return; }

        var animator = f.GetComponent<Animator>();
        if (animator == null) { Fail(f.name + " Animator yok"); return; }

        AnimatorController controller = Resolve(animator.runtimeAnimatorController);
        if (controller == null) { Fail(f.name + " AnimatorController cozulemedi"); return; }

        var declared = controller.parameters.ToDictionary(p => p.name, p => p.type);

        Expect(declared, f.name, anim.groundedBool, AnimatorControllerParameterType.Bool);
        Expect(declared, f.name, anim.airSpeedFloat, AnimatorControllerParameterType.Float);
        Expect(declared, f.name, anim.stateInt, AnimatorControllerParameterType.Int);
        Expect(declared, f.name, anim.jumpTrigger, AnimatorControllerParameterType.Trigger);
        Expect(declared, f.name, anim.attackTrigger, AnimatorControllerParameterType.Trigger);
    }

    static void Expect(Dictionary<string, AnimatorControllerParameterType> declared,
                       string who, string name, AnimatorControllerParameterType type)
    {
        if (!declared.ContainsKey(name)) Fail(who + " Animator'unde '" + name + "' parametresi yok");
        else Require(declared[name] == type, who + " '" + name + "' tipi " + declared[name] + ", " + type + " olmali");
    }

    static AnimatorController Resolve(RuntimeAnimatorController rac)
    {
        var overrides = rac as AnimatorOverrideController;
        return overrides != null ? Resolve(overrides.runtimeAnimatorController) : rac as AnimatorController;
    }

    static void CheckNoPackScripts(GameObject[] roots)
    {
        foreach (GameObject root in roots)
        {
            Require(root.GetComponentsInChildren<HeroKnight>(true).Length == 0, "HeroKnight.cs hala sahnede, girdiyi iki kere okur");
            Require(root.GetComponentsInChildren<Bandit>(true).Length == 0, "Bandit.cs hala sahnede, girdiyi iki kere okur");
            Require(root.GetComponentsInChildren<Sensor_HeroKnight>(true).Length == 0, "Sensor_HeroKnight kaldi");
            Require(root.GetComponentsInChildren<Sensor_Bandit>(true).Length == 0, "Sensor_Bandit kaldi");
        }
    }

    static void CheckNoMissingScripts(GameObject[] roots)
    {
        foreach (GameObject root in roots)
        {
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            {
                int missing = t.GetComponents<Component>().Count(c => c == null);
                if (missing > 0) Fail(missing + " adet missing script: " + PathOf(t));
            }
        }
    }

    static string PathOf(Transform t)
    {
        string path = t.name;
        for (Transform p = t.parent; p != null; p = p.parent) path = p.name + "/" + path;
        return path;
    }

    static void Require(bool condition, string message)
    {
        if (!condition) Fail(message);
    }

    static void Fail(string message)
    {
        failures.Add(message);
    }
}
