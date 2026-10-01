using System;
using System.Collections.Generic;
using UnityEngine;


public class MatchState : MonoBehaviour
{
    public static MatchState Instance { get; private set; }

    [Header("Süre")]
    public float matchDuration = 45f;
    
    [Header("Test")]
    public bool timerFrozen;
    public KeyCode freezeKey = KeyCode.F1;

    public float TimeLeft { get; private set; }
    public bool IsRunning { get; private set; }

    
    private readonly Dictionary<int, int> scores = new Dictionary<int, int>();

    void Awake()
    {
        Instance = this;
        TimeLeft = matchDuration;
        IsRunning = true;
        scores.Clear();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Update()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (Input.GetKeyDown(freezeKey)) timerFrozen = !timerFrozen;
        if (timerFrozen) return;
#endif

        if (!IsRunning) return;

        TimeLeft -= Time.deltaTime;
        if (TimeLeft > 0f) return;

        TimeLeft = 0f;
        IsRunning = false;
    }

    public int ScoreOf(int playerIndex)
    {
        int score;
        return scores.TryGetValue(playerIndex, out score) ? score : 0;
    }

    
    public void ReportRingOut(Fighter victim)
    {
        if (!IsRunning) return;

        int opponent = victim.playerIndex == 1 ? 2 : 1;
        scores[opponent] = ScoreOf(opponent) + 1;
    }

    public string ResultText()
    {
        int p1 = ScoreOf(1);
        int p2 = ScoreOf(2);

        if (p1 > p2) return "Player 1 kazandi";
        if (p2 > p1) return "Player 2 kazandi";
        return "Berabere";
    }
}
