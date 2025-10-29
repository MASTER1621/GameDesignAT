using UnityEngine;
using TMPro;

public class HighScoreDisplay : MonoBehaviour
{
    [Header("Level 1")]
    public TMP_Text l1ScoreText;
    public TMP_Text l1TimeText;

    [Header("Level 2")]
    public TMP_Text l2ScoreText;
    public TMP_Text l2TimeText;

    void Start() { Refresh(); }

    public void Refresh()
    {
        int l1Score = PlayerPrefs.GetInt("HS_L1_Score", 0);
        int l1TimeMs = PlayerPrefs.GetInt("HS_L1_TimeMS", 0);
        int l2Score = PlayerPrefs.GetInt("HS_L2_Score", 0);
        int l2TimeMs = PlayerPrefs.GetInt("HS_L2_TimeMS", 0);

        if (l1ScoreText) l1ScoreText.text = l1Score.ToString("D6");
        if (l1TimeText)  l1TimeText.text  = FormatMs(l1TimeMs);
        if (l2ScoreText) l2ScoreText.text = l2Score.ToString("D6");
        if (l2TimeText)  l2TimeText.text  = FormatMs(l2TimeMs);
    }

    string FormatMs(int ms)
    {
        if (ms < 0) ms = 0;
        int minutes = ms / 60000;
        int seconds = (ms % 60000) / 1000;
        int centi   = (ms % 1000) / 10; //“msms”
        return $"{minutes:00}:{seconds:00}:{centi:00}";
    }
}
