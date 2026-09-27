using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Menü: GameJam > Map'leri Oyuna Hazırla
// Furkan'ın DemoScene kurulumunu (Campus Mayhem > Setup Two Player DemoScene) Map1-Map5'e uygular:
// paketin demo scriptlerini söker, karakterlere Fighter/FighterCombat/FighterAnimator ekler,
// Oyuncu 1 = A/D/W/Sol Shift, Oyuncu 2 = Oklar/Sağ Shift, sahneye Match (süre+skor) koyar.
// Önce pixel art skor arayüzünü yeniden kurar. Furkan'ın kodunu değiştirmez, onu çağırır.
public static class PrepareMaps
{
    static readonly string[] Maps =
    {
        "Assets/Scenes/Map1.unity", "Assets/Scenes/Map2.unity", "Assets/Scenes/Map3.unity",
        "Assets/Scenes/Map4.unity", "Assets/Scenes/Map5.unity"
    };

    const BindingFlags Any = BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public;

    [MenuItem("GameJam/Map'leri Oyuna Hazırla")]
    static void Run()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        Type setup = typeof(DemoSceneSetup);
        MethodInfo configure = setup.GetMethod("Configure", Any);
        MethodInfo buildMatch = setup.GetMethod("BuildMatch", Any);
        MethodInfo frictionless = setup.GetMethod("EnsureFrictionless", Any);
        FieldInfo profilesField = setup.GetField("Profiles", Any);
        if (configure == null || buildMatch == null || frictionless == null || profilesField == null)
        {
            EditorUtility.DisplayDialog("GameJam", "DemoSceneSetup değişmiş, kurulum fonksiyonları bulunamadı. Furkan'a sor.", "Tamam");
            return;
        }
        // Önce skor arayüzünü yenile: eski prefab'daki "HUD" adlı çocuk,
        // Furkan'ın kurulumunun sildiği "HUD" ile karışmasın diye "SkinHUD" oldu.
        ScoreUIBuilder.Build();

        var profiles = (Array)profilesField.GetValue(null);
        var mat = frictionless.Invoke(null, null);

        int ok = 0;
        foreach (var path in Maps)
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) == null) continue;
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            try
            {
                foreach (var profile in profiles) configure.Invoke(null, new object[] { scene, profile, mat });
                buildMatch.Invoke(null, new object[] { scene });
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                ok++;
            }
            catch (TargetInvocationException e)
            {
                Debug.LogError($"GameJam: {path} hazırlanamadı: {e.InnerException?.Message}");
            }
        }
        Physics2D.IgnoreLayerCollision(8, 8, true);
        AssetDatabase.SaveAssets();
        EditorSceneManager.OpenScene(Maps[0]);
        Debug.Log($"GameJam: {ok} map iki oyunculu hale getirildi.");
    }
}
