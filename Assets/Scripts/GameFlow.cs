using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;


public static class GameFlow
{
    public const string MenuScene = "MainMenu";
    const string MapPrefix = "Map";

    static string lastMap;

    public static int MapCount
    {
        get { return MapNames().Count; }
    }

    public static bool IsMapScene(string sceneName)
    {
        return sceneName.StartsWith(MapPrefix);
    }

    public static void LoadMenu()
    {
        SceneManager.LoadScene(MenuScene);
    }

    public static void LoadRandomMap()
    {
        List<string> maps = MapNames();
        if (maps.Count == 0)
        {
            Debug.LogError("GameFlow: Build Settings'te harita yok. GameJam > Oyun Akisini Kur calistirilmali.");
            return;
        }

        lastMap = Pick(maps);
        SceneManager.LoadScene(lastMap);
    }

    static string Pick(List<string> maps)
    {
        if (maps.Count == 1) return maps[0];

        string name;
        do { name = maps[Random.Range(0, maps.Count)]; }
        while (name == lastMap);

        return name;
    }

    static List<string> MapNames()
    {
        var names = new List<string>();
        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            string name = NameOf(SceneUtility.GetScenePathByBuildIndex(i));
            if (IsMapScene(name)) names.Add(name);
        }
        return names;
    }

    static string NameOf(string path)
    {
        int slash = path.LastIndexOf('/') + 1;
        int dot = path.LastIndexOf('.');
        return dot > slash ? path.Substring(slash, dot - slash) : path.Substring(slash);
    }
}
