using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

// Menü: GameJam > Tek Yönlü Platformlar
// "Platforms" tilemap'ine PlatformEffector2D ekler: karakterler platformların
// altından zıplayıp içinden geçebilir, üstüne inince durur (Gun Mayhem gibi).
// DemoScene ve Map1-Map5'e uygular, tekrar çalıştırmak güvenli.
public static class OneWayPlatforms
{
    static readonly string[] Scenes =
    {
        "Assets/Scenes/DemoScene.unity", "Assets/Scenes/Map1.unity", "Assets/Scenes/Map2.unity",
        "Assets/Scenes/Map3.unity", "Assets/Scenes/Map4.unity", "Assets/Scenes/Map5.unity"
    };

    [MenuItem("GameJam/Tek Yönlü Platformlar")]
    static void Apply()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        string current = EditorSceneManager.GetActiveScene().path;
        int done = 0;

        foreach (var path in Scenes)
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) == null) continue;
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            var go = GameObject.Find("Platforms");
            if (go == null || go.GetComponent<Tilemap>() == null) continue;

            var effector = go.GetComponent<PlatformEffector2D>();
            if (effector == null) effector = go.AddComponent<PlatformEffector2D>();
            effector.useOneWay = true;
            effector.surfaceArc = 170f;
            effector.useSideFriction = false;
            effector.useSideBounce = false;

            var composite = go.GetComponent<CompositeCollider2D>();
            if (composite != null) composite.usedByEffector = true;
            else
            {
                var tc = go.GetComponent<TilemapCollider2D>();
                if (tc != null) tc.usedByEffector = true;
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            done++;
        }
        if (!string.IsNullOrEmpty(current)) EditorSceneManager.OpenScene(current);
        Debug.Log($"GameJam: {done} sahnede platformlar tek yönlü yapıldı.");
    }
}
