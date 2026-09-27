using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

// Sayac, skor ve game over paneli.
public class MatchHud : MonoBehaviour
{
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI scoreText;
    public GameObject gameOverPanel;
    public TextMeshProUGUI resultText;
    public KeyCode restartKey = KeyCode.R;

    void Update()
    {
        MatchState match = MatchState.Instance;
        if (match == null) return;

        timerText.text = Mathf.CeilToInt(match.TimeLeft).ToString();
        scoreText.text = match.ScoreOf(1) + " - " + match.ScoreOf(2);

        bool showPanel = !match.IsRunning;
        if (gameOverPanel.activeSelf != showPanel) gameOverPanel.SetActive(showPanel);
        if (!showPanel) return;

        resultText.text = match.ResultText();


        if (Input.GetKeyDown(restartKey))
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}
