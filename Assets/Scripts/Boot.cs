using UnityEngine;

public class Boot : MonoBehaviour
{
    [SerializeField] AudioClip menuMusic;
    [SerializeField] AudioClip mapMusic;

    void Start()
    {
        MusicManager.Create(menuMusic, mapMusic);
        GameFlow.LoadMenu();
    }
}
