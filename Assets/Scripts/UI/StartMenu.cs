using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class StartMenu : MonoBehaviour
{
    [Header("Scene Names (set in Inspector)")]
    [SerializeField] string level1SceneName = "Level01_A4";   
    [SerializeField] string level2SceneName = "Level02_HD";   // probs not gonna do

    [Header("Buttons")]
    [SerializeField] Button level1Button;
    [SerializeField] Button level2Button;

    void Awake()
    {
        if (level1Button) level1Button.onClick.AddListener(() => LoadSceneIfInBuild(level1SceneName));
        if (level2Button) level2Button.onClick.AddListener(() => LoadSceneIfInBuild(level2SceneName));
    }

    void LoadSceneIfInBuild(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName)) return;
        if (Application.CanStreamedLevelBeLoaded(sceneName))
            SceneManager.LoadScene(sceneName);
        else
            Debug.LogWarning($"Scene '{sceneName}' not in Build Settings. (OK if Level 2 not ready)");
    }
}
