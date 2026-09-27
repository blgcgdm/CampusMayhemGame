using UnityEngine;

// Menü sahnesindeki Start butonu bu tipin PlayGame metodunu arıyor.
public class MainMenu : MonoBehaviour
{
    public void PlayGame()
    {
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
