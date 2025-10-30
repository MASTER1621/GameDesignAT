using UnityEngine;
using UnityEngine.SceneManagement;

public class ResetHighScores : MonoBehaviour
{
    public void ResetAll()
    {
        PlayerPrefs.DeleteKey("HighScore_Level1");
        PlayerPrefs.DeleteKey("BestTime_Level1_ms");
        PlayerPrefs.Save();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
