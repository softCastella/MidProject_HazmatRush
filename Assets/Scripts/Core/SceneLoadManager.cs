using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoadManager : MonoBehaviour
{
    public static SceneLoadManager Instance { get; private set; }

    public string titleSceneName = "TitleScene";
    public string gameSceneName = "GameScene";

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void StartButton()
    {
        LoadScene(gameSceneName);
    }

    public void TitleButton()
    {
        LoadScene(titleSceneName);
    }

    public void LoadScene(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
            return;

        Time.timeScale = 1f;
        SceneManager.LoadScene(sceneName);
    }
}
