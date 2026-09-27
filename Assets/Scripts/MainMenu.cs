using UnityEngine;

// Menü sahnesindeki Start butonu bu tipin PlayGame metodunu arıyor.
public class MainMenu : MonoBehaviour
{
    // Isim paneli varsa once o acilir; panel yoksa oyun yine baslar.
    public void PlayGame()
    {
        var entry = FindFirstObjectByType<NameEntry>();
        if (entry != null) { entry.Open(); return; }

        GameFlow.LoadRandomMap();
    }

    public void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
