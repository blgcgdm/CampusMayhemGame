using System.Linq;
using UnityEngine;
using UnityEngine.UI;


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
    public Text standingsText;                        

    [Header("Görünüm")]
    public string[] playerNames = { "OYUNCU 1", "OYUNCU 2" };
    public Color[] playerColors = { new Color(0.95f, 0.36f, 0.4f), new Color(0.38f, 0.6f, 1f) };
    public Color lowTimeColor = new Color(1f, 0.35f, 0.3f);

    bool shownEnd;

    void Start()
    {
        if (endScreen) endScreen.SetActive(false);

        // Isimler menude girildi; HUD ve kazanan metni onlari gostersin.
        playerNames[0] = PlayerProfiles.Name(1);
        playerNames[1] = PlayerProfiles.Name(2);
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
        // Beraberlikte kimseye puan yazilmiyor.
        if (!draw) PlayerProfiles.RecordWin(playerNames[first]);

        //ShowStandings();

        if (endScreen) endScreen.SetActive(true);
    }

    // Genel tablo bitis ekraninda, tum oynanan maclarin birikimi.
    // void ShowStandings()
    // {
    //     if (endScreen == null) return;
    //
    //     Text label = EnsureStandingsLabel();
    //     var rows = PlayerProfiles.Standings().Take(6)
    //         .Select(e => e.Key + "   " + e.Value);
    //
    //     label.text = "GENEL TABLO\n" + string.Join("\n", rows);
    // }

    // Prefab'a dokunmamak icin etiket calisma zamaninda uretiliyor.
    Text EnsureStandingsLabel()
    {
        if (standingsText != null) return standingsText;

        var go = new GameObject("Standings", typeof(RectTransform));
        go.transform.SetParent(endScreen.transform, false);

        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(1f, 0.5f);
        rect.anchorMax = new Vector2(1f, 0.5f);
        rect.pivot = new Vector2(1f, 0.5f);
        rect.anchoredPosition = new Vector2(-60f, 0f);
        rect.sizeDelta = new Vector2(420f, 340f);

        standingsText = go.AddComponent<Text>();
        standingsText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        standingsText.fontSize = 30;
        standingsText.color = Color.white;
        standingsText.alignment = TextAnchor.UpperLeft;
        standingsText.lineSpacing = 1.25f;

        return standingsText;
    }

    // Tekrar oynamak ayni haritayi degil yeni bir rastgele harita yukluyor.
    public void Restart()
    {
        GameFlow.LoadRandomMap();
    }

    public void MainMenu()
    {
        GameFlow.LoadMenu();
    }
}
