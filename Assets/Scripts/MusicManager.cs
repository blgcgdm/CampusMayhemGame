using UnityEngine;
using UnityEngine.SceneManagement;


public class MusicManager : MonoBehaviour
{
    static MusicManager instance;

    AudioClip menuMusic;
    AudioClip mapMusic;
    AudioSource source;

    public static void Create(AudioClip menu, AudioClip map)
    {
        if (instance != null) return;

        var go = new GameObject("MusicManager");
        DontDestroyOnLoad(go);

        instance = go.AddComponent<MusicManager>();
        instance.menuMusic = menu;
        instance.mapMusic = map;

        instance.source = go.AddComponent<AudioSource>();
        instance.source.loop = true;
        instance.source.playOnAwake = false;
        instance.source.spatialBlend = 0f;

        SceneManager.sceneLoaded += instance.OnSceneLoaded;
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (instance == this) instance = null;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == GameFlow.MenuScene) Play(menuMusic);
        else if (GameFlow.IsMapScene(scene.name)) Play(mapMusic);
        // Baska sahnelerde (ornegin Boot) calan muzige dokunma.
    }

    void Play(AudioClip clip)
    {
        if (clip == null) { source.Stop(); source.clip = null; return; }
        if (source.clip == clip && source.isPlaying) return;

        source.clip = clip;
        source.Play();
    }
}
