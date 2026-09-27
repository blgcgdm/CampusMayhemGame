using System.Linq;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Menüye isim alma panelini kurar. Idempotent: panel varsa sıfırdan
// yeniden üretir, elle bağlanacak referans bırakmaz.
public static class NameUIBuilder
{
    const string MenuScene = "Assets/MainMenu.unity";
    const string PanelName = "NameEntryPanel";

    static Font font;

    [MenuItem("GameJam/Isim Ekranini Kur")]
    public static void Build()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        Scene scene = EditorSceneManager.OpenScene(MenuScene, OpenSceneMode.Single);

        var canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null) throw new System.Exception("Menu sahnesinde Canvas yok");

        foreach (var old in canvas.GetComponentsInChildren<Transform>(true).Where(t => t.name == PanelName).ToList())
            Object.DestroyImmediate(old.gameObject);

        if (Object.FindFirstObjectByType<EventSystem>() == null)
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

        GameObject panel = Panel(canvas.transform);
        InputField p1 = Field(panel.transform, "Player1", "1. OYUNCU", 70f);
        InputField p2 = Field(panel.transform, "Player2", "2. OYUNCU", -30f);

        var entry = canvas.GetComponent<NameEntry>() ?? canvas.gameObject.AddComponent<NameEntry>();
        entry.panel = panel;
        entry.player1 = p1;
        entry.player2 = p2;

        Button start = Btn(panel.transform, "StartButton", "BASLA", -150f);
        while (start.onClick.GetPersistentEventCount() > 0)
            UnityEventTools.RemovePersistentListener(start.onClick, 0);
        UnityEventTools.AddPersistentListener(start.onClick, entry.Confirm);

        panel.SetActive(false);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("NAME_UI_OK");
    }

    static GameObject Panel(Transform parent)
    {
        var go = new GameObject(PanelName, typeof(RectTransform));
        go.transform.SetParent(parent, false);

        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        go.AddComponent<Image>().color = new Color(0.05f, 0.05f, 0.11f, 0.92f);
        Label(go.transform, "Title", "OYUNCU ISIMLERI", 54, 190f);
        return go;
    }

    static Text Label(Transform parent, string name, string content, int size, float y)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        Place(go, y, new Vector2(700f, 70f));

        var text = go.AddComponent<Text>();
        text.font = font;
        text.fontSize = size;
        text.color = Color.white;
        text.alignment = TextAnchor.MiddleCenter;
        text.text = content;
        return text;
    }

    static InputField Field(Transform parent, string name, string placeholder, float y)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        Place(go, y, new Vector2(560f, 76f));
        go.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.12f);

        Text hint = Label(go.transform, "Placeholder", placeholder, 34, 0f);
        hint.color = new Color(1f, 1f, 1f, 0.45f);
        hint.alignment = TextAnchor.MiddleLeft;
        Inset(hint.rectTransform);

        Text value = Label(go.transform, "Text", "", 34, 0f);
        value.alignment = TextAnchor.MiddleLeft;
        Inset(value.rectTransform);

        var field = go.AddComponent<InputField>();
        field.textComponent = value;
        field.placeholder = hint;
        field.characterLimit = 12;
        return field;
    }

    static Button Btn(Transform parent, string name, string content, float y)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        Place(go, y, new Vector2(320f, 90f));
        go.AddComponent<Image>().color = new Color(0.95f, 0.36f, 0.4f);
        Label(go.transform, "Label", content, 40, 0f);
        return go.AddComponent<Button>();
    }

    static void Place(GameObject go, float y, Vector2 size)
    {
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(0f, y);
        rect.sizeDelta = size;
    }

    static void Inset(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(20f, 0f);
        rect.offsetMax = new Vector2(-20f, 0f);
    }
}
