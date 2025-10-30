using TMPro;
using UnityEngine;

public class HighScoreDisplay : MonoBehaviour
{
    public TMP_Text highScoreTextL1;
    public TMP_Text bestTimeTextL1;

    void Start()
    {
        int score = PlayerPrefs.GetInt("HighScore_Level1", 0);
        int bestMs = PlayerPrefs.GetInt("BestTime_Level1_ms", 0);
        if (highScoreTextL1) highScoreTextL1.text = score.ToString("D6");
        if (bestTimeTextL1) bestTimeTextL1.text = FormatMs(bestMs);
    }

    string FormatMs(int ms)
    {
        if (ms <= 0) return "00:00:00";
        int mm = ms / 60000;
        int ss = (ms % 60000) / 1000;
        int cs = (ms % 1000) / 10;
        return mm.ToString("00") + ":" + ss.ToString("00") + ":" + cs.ToString("00");
    }
}
