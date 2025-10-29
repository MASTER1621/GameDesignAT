using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ExitToStart : MonoBehaviour
{
    [SerializeField] string startSceneName = "StartScene";
    public void GoToStart() => SceneManager.LoadScene(startSceneName);
}
