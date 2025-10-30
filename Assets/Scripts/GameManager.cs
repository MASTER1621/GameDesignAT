using TMPro;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager I;

    [Header("UI")]
    public TMP_Text scoreText;  
    [Header("State")]
    public int score = 0;

    void Awake()
    {
        I = this;
        UpdateScoreUI();
    }

    public void AddScore(int v)
    {
        score += v;
        if (score < 0) score = 0;
        UpdateScoreUI();
    }

    void UpdateScoreUI()
    {
        if (scoreText) scoreText.text = score.ToString("D6"); 
    }
}
