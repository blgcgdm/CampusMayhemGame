using UnityEngine;
using UnityEngine.UI;

// Menüdeki isim alma paneli. Start'a basınca açılır, BAŞLA isimleri
// kaydedip rastgele haritayı yükler.
public class NameEntry : MonoBehaviour
{
    public GameObject panel;
    public InputField player1;
    public InputField player2;

    void Start()
    {
        if (panel) panel.SetActive(false);
    }

    public void Open()
    {
        if (panel) panel.SetActive(true);
        if (player1) player1.Select();
    }

    public void Confirm()
    {
        PlayerProfiles.SetNames(player1 ? player1.text : "", player2 ? player2.text : "");
        GameFlow.LoadRandomMap();
    }
}
