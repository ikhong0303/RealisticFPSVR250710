using UnityEngine;
using UnityEngine.SceneManagement;

public class StartButtonHandler : MonoBehaviour
{
    public string nextSceneName = "Stage01"; // 전환할 씬 이름

    public void StartGame()
    {
        SceneManager.LoadScene(nextSceneName);
    }
}