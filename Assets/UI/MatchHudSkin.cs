using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Furkan'ın MatchState'ini okuyan pixel art HUD + maç sonu ekranı.
// Oyun mantığına dokunmaz: puan, süre ve bitiş MatchState'ten gelir.
// Sahnede MatchHud varsa onun sade Canvas'ını gizler (MatchHud çalışmaya devam eder, R ile yeniden başlatma dahil).
public class MatchHudSkin : MonoBehaviour
{
    [Header("HUD")]
    public GameObject hudRoot;
    public Text timerText;
    public Text[] scoreTexts = new Text[2];          // 0 = Oyuncu 1, 1 = Oyuncu 2

    [Header("Maç Sonu")]
    public GameObject endScreen;
    public Text winnerText;
    public Image[] rowMedals = new Image[2];
    public Text[] rowTexts = new Text[2];
    public Sprite goldMedal, silverMedal;

    [Header("Görünüm")]
    public string[] playerNames = { "OYUNCU 1", "OYUNCU 2" };
    public Color[] playerColors = { new Color(0.95f, 0.36f, 0.4f), new Color(0.38f, 0.6f, 1f) };
    public Color lowTimeColor = new Color(1f, 0.35f, 0.3f);
    public bool hideDefaultMatchHud = true;
    public string menuSceneName = "";                 // boşsa Build Settings'teki ilk sahne

    bool shownEnd;

    void Start()
    {
        if (endScreen) endScreen.SetActive(false);
        if (hideDefaultMatchHud)
        {
            var hud = FindFirstObjectByType<MatchHud>();
            if (hud != null)
            {
                var c = hud.GetComponent<Canvas>();
                if (c != null) c.enabled = false;
            }
        }
    }

    void Update()
    {
        var match = MatchState.Instance;
        if (hudRoot) hudRoot.SetActive(match != null);
        if (match == null) return;

        int t = Mathf.CeilToInt(match.TimeLeft);
        if (timerText)
        {
            timerText.text = t.ToString();
            timerText.color = t <= 10 ? lowTimeColor : Color.white;
        }
        for (int i = 0; i < 2; i++)
            if (scoreTexts[i]) scoreTexts[i].text = match.ScoreOf(i + 1).ToString();

        if (!match.IsRunning && !shownEnd) ShowEnd(match);
    }

    void ShowEnd(MatchState match)
    {
        shownEnd = true;
        int s1 = match.ScoreOf(1), s2 = match.ScoreOf(2);
        bool draw = s1 == s2;
        int first = s1 >= s2 ? 0 : 1, second = 1 - first;
        int[] sc = { s1, s2 };

        if (winnerText)
        {
            winnerText.text = draw ? "BERABERE!" : playerNames[first] + " KAZANDI!";
            winnerText.color = draw ? Color.white : playerColors[first];
        }
        int[] order = { first, second };
        for (int row = 0; row < 2; row++)
        {
            int p = order[row];
            if (rowTexts[row])
            {
                rowTexts[row].text = playerNames[p] + "      " + sc[p] + " PUAN";
                rowTexts[row].color = playerColors[p];
            }
            if (rowMedals[row]) rowMedals[row].sprite = (row == 0 || draw) ? goldMedal : silverMedal;
        }
        if (endScreen) endScreen.SetActive(true);
    }

    public void Restart()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void MainMenu()
    {
        if (!string.IsNullOrEmpty(menuSceneName)) SceneManager.LoadScene(menuSceneName);
        else SceneManager.LoadScene(0);
    }
}
