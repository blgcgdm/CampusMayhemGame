using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Menü: GameJam > Skor Arayüzü Oluştur
// MatchState'e bağlı pixel art HUD'u (Assets/UI/MatchHudSkin.prefab) kurar ve
// DemoScene + Map1-Map5 sahnelerine ekler (varsa tekrar eklemez).
public static class ScoreUIBuilder
{
    const string ArtDir = "Assets/Art/UI";
    const string PrefabPath = "Assets/UI/MatchHudSkin.prefab";
    static readonly string[] Scenes =
    {
        "Assets/Scenes/DemoScene.unity", "Assets/Scenes/Map1.unity", "Assets/Scenes/Map2.unity",
        "Assets/Scenes/Map3.unity", "Assets/Scenes/Map4.unity", "Assets/Scenes/Map5.unity"
    };

    static readonly Color Gold = new Color(1f, 0.82f, 0.29f);
    static readonly Color Ink = new Color(0.055f, 0.047f, 0.11f);
    static readonly Color P1 = new Color(0.95f, 0.36f, 0.4f);
    static readonly Color P2 = new Color(0.38f, 0.6f, 1f);
    static Font font;

    [MenuItem("GameJam/Skor Arayüzü Oluştur")]
    public static void Build()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        SetupSprite("UI_Panel", new Vector4(12, 12, 12, 12));
        SetupSprite("UI_Banner", new Vector4(12, 0, 12, 0));
        SetupSprite("UI_Button", new Vector4(6, 6, 6, 6));
        foreach (var n in new[] { "UI_Medal_Gold", "UI_Medal_Silver", "UI_Crown" }) SetupSprite(n, Vector4.zero);

        var prefab = BuildPrefab();
        if (prefab == null) return;

        string current = EditorSceneManager.GetActiveScene().path;
        int added = 0;
        foreach (var path in Scenes)
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) == null) continue;
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            if (Object.FindFirstObjectByType<MatchHudSkin>() == null)
            {
                PrefabUtility.InstantiatePrefab(prefab);
                added++;
            }
            if (Object.FindFirstObjectByType<EventSystem>() == null)
                new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }
        if (!string.IsNullOrEmpty(current)) EditorSceneManager.OpenScene(current);
        Debug.Log($"GameJam: Skor arayüzü hazır ({PrefabPath}), {added} sahneye eklendi.");
    }

    static void SetupSprite(string name, Vector4 border)
    {
        string path = $"{ArtDir}/{name}.png";
        var ti = AssetImporter.GetAtPath(path) as TextureImporter;
        if (ti == null) { Debug.LogError("GameJam: bulunamadı " + path); return; }
        ti.textureType = TextureImporterType.Sprite;
        ti.spriteImportMode = SpriteImportMode.Single;
        ti.spritePixelsPerUnit = 16;
        ti.filterMode = FilterMode.Point;
        ti.textureCompression = TextureImporterCompression.Uncompressed;
        ti.mipmapEnabled = false;
        ti.spriteBorder = border;
        ti.SaveAndReimport();
    }

    static Sprite S(string name) => AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtDir}/{name}.png");

    static RectTransform Node(string name, Transform parent, Vector2 anchor, Vector2 pos, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = 5;
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        return rt;
    }

    // pixelScale: sprite'ın bir pikseli kaç ekran pikseli olsun
    static Image Img(string name, Transform parent, string sprite, Vector2 anchor, Vector2 pos, Vector2 size, bool sliced, float pixelScale)
    {
        var rt = Node(name, parent, anchor, pos, size);
        var img = rt.gameObject.AddComponent<Image>();
        img.sprite = S(sprite);
        img.type = sliced ? Image.Type.Sliced : Image.Type.Simple;
        img.pixelsPerUnitMultiplier = 100f / 16f / pixelScale;
        img.raycastTarget = false;
        return img;
    }

    static Text Txt(string name, Transform parent, string text, int size, Color color, Vector2 anchor, Vector2 pos, Vector2 box, TextAnchor align)
    {
        var rt = Node(name, parent, anchor, pos, box);
        var t = rt.gameObject.AddComponent<Text>();
        t.font = font; t.text = text; t.fontSize = size; t.fontStyle = FontStyle.Bold;
        t.color = color; t.alignment = align; t.raycastTarget = false;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        var o = rt.gameObject.AddComponent<Outline>();
        o.effectColor = Ink; o.effectDistance = new Vector2(3, -3);
        return t;
    }

    static void Btn(string name, Transform parent, string label, Vector2 pos, UnityAction action)
    {
        var img = Img(name, parent, "UI_Button", new Vector2(0.5f, 0.5f), pos, new Vector2(340, 100), true, 4);
        img.raycastTarget = true;
        var b = img.gameObject.AddComponent<Button>();
        var cb = b.colors; cb.highlightedColor = new Color(1f, 0.95f, 0.8f); cb.pressedColor = new Color(0.8f, 0.7f, 0.55f); b.colors = cb;
        UnityEventTools.AddPersistentListener(b.onClick, action);
        Txt("Label", img.transform, label, 34, Color.white, new Vector2(0.5f, 0.5f), new Vector2(0, 2), new Vector2(320, 90), TextAnchor.MiddleCenter);
    }

    static GameObject BuildPrefab()
    {
        if (S("UI_Panel") == null) { EditorUtility.DisplayDialog("GameJam", "UI görselleri Assets/Art/UI içinde bulunamadı.", "Tamam"); return null; }
        var C = new Vector2(0.5f, 0.5f);

        var root = new GameObject("MatchHudSkin", typeof(RectTransform));
        root.layer = 5;
        var canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        var scaler = root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        root.AddComponent<GraphicRaycaster>();
        var skin = root.AddComponent<MatchHudSkin>();

        // ---- HUD ----
        var hud = Node("SkinHUD", root.transform, C, Vector2.zero, Vector2.zero);
        hud.anchorMin = Vector2.zero; hud.anchorMax = Vector2.one; hud.sizeDelta = Vector2.zero;
        skin.hudRoot = hud.gameObject;

        var timer = Img("Timer", hud, "UI_Panel", new Vector2(0.5f, 1), new Vector2(0, -80), new Vector2(200, 120), true, 3);
        Txt("Label", timer.transform, "SÜRE", 22, Gold, C, new Vector2(0, 30), new Vector2(180, 30), TextAnchor.MiddleCenter);
        skin.timerText = Txt("Value", timer.transform, "45", 56, Color.white, C, new Vector2(0, -12), new Vector2(180, 64), TextAnchor.MiddleCenter);

        var names = new[] { "OYUNCU 1", "OYUNCU 2" };
        var cols = new[] { P1, P2 };
        for (int i = 0; i < 2; i++)
        {
            var anchor = i == 0 ? new Vector2(0, 1) : new Vector2(1, 1);
            var box = Img($"Player{i + 1}", hud, "UI_Panel", anchor, new Vector2(i == 0 ? 200 : -200, -80), new Vector2(340, 120), true, 3);
            Txt("Name", box.transform, names[i], 28, cols[i], C, new Vector2(0, 24), new Vector2(300, 40), TextAnchor.MiddleCenter);
            skin.scoreTexts[i] = Txt("Score", box.transform, "0", 52, Color.white, C, new Vector2(0, -16), new Vector2(300, 60), TextAnchor.MiddleCenter);
        }

        // ---- Maç sonu ----
        var end = Node("EndScreen", root.transform, C, Vector2.zero, Vector2.zero);
        end.anchorMin = Vector2.zero; end.anchorMax = Vector2.one; end.sizeDelta = Vector2.zero;
        var dim = end.gameObject.AddComponent<Image>(); dim.color = new Color(0, 0, 0, 0.65f);
        skin.endScreen = end.gameObject;
        end.gameObject.SetActive(false);

        var panel = Img("Panel", end, "UI_Panel", C, new Vector2(0, -20), new Vector2(900, 700), true, 4);
        var banner = Img("Banner", end, "UI_Banner", C, new Vector2(0, 320), new Vector2(760, 120), true, 5);
        Img("Crown", end, "UI_Crown", C, new Vector2(0, 400), new Vector2(84, 60), false, 6);
        Txt("Title", banner.transform, "MAÇ BİTTİ", 56, Color.white, C, new Vector2(0, 14), new Vector2(700, 80), TextAnchor.MiddleCenter);

        var p = panel.transform;
        skin.winnerText = Txt("Winner", p, "OYUNCU 1 KAZANDI!", 64, Gold, C, new Vector2(0, 180), new Vector2(860, 90), TextAnchor.MiddleCenter);
        Txt("RankingTitle", p, "SIRALAMA", 32, Gold, C, new Vector2(0, 95), new Vector2(800, 50), TextAnchor.MiddleCenter);
        for (int i = 0; i < 2; i++)
        {
            float y = 20 - i * 90;
            skin.rowMedals[i] = Img($"Medal{i + 1}", p, i == 0 ? "UI_Medal_Gold" : "UI_Medal_Silver", C, new Vector2(-280, y), new Vector2(48, 64), false, 4);
            skin.rowTexts[i] = Txt($"Row{i + 1}", p, names[i] + "      0 PUAN", 42, cols[i], C, new Vector2(40, y), new Vector2(560, 60), TextAnchor.MiddleLeft);
        }
        skin.goldMedal = S("UI_Medal_Gold");
        skin.silverMedal = S("UI_Medal_Silver");

        Btn("RestartButton", p, "TEKRAR OYNA", new Vector2(-190, -230), skin.Restart);
        Btn("MenuButton", p, "ANA MENÜ", new Vector2(190, -230), skin.MainMenu);
        Txt("Hint", p, "R: yeniden başla", 24, new Color(0.8f, 0.8f, 0.9f), C, new Vector2(0, -312), new Vector2(600, 36), TextAnchor.MiddleCenter);

        System.IO.Directory.CreateDirectory("Assets/UI");
        var prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);
        return prefab;
    }
}
