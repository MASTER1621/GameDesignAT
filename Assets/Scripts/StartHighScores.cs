using TMPro;
using UnityEngine;

public class StartHighScores : MonoBehaviour
{
    public TMP_Text highScoreL1;
    public TMP_Text bestTimeL1;
    public TMP_Text highScoreL2;
    public TMP_Text bestTimeL2;

    void Awake()
    {
        SetRow("Level1", highScoreL1, bestTimeL1);
        SetRow("Level2", highScoreL2, bestTimeL2);
    }

    void SetRow(string levelKey, TMP_Text scoreTxt, TMP_Text timeTxt)
    {
        int s = PlayerPrefs.GetInt("HighScore_" + levelKey, 0);
        int ms = PlayerPrefs.GetInt("BestTime_" + levelKey + "_ms", int.MaxValue);
        if (scoreTxt) scoreTxt.text = s.ToString("D6");
        if (timeTxt)  timeTxt.text  = ms == int.MaxValue ? "00:00:00" : FormatMs(ms);
    }

    string FormatMs(int ms)
    {
        int cs = Mathf.RoundToInt(ms / 10f);
        int m = cs / 6000;
        int s = (cs / 100) % 60;
        int c = cs % 100;
        return $"{m:00}:{s:00}:{c:00}";
    }

    public void ResetAll()
    {
        PlayerPrefs.DeleteKey("HighScore_Level1");
        PlayerPrefs.DeleteKey("BestTime_Level1_ms");
        PlayerPrefs.DeleteKey("HighScore_Level2");
        PlayerPrefs.DeleteKey("BestTime_Level2_ms");
        PlayerPrefs.Save();
        Awake();
    }
}
