using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoadManager : MonoBehaviour
{
    public static SceneLoadManager Instance { get; private set; }

    public string titleSceneName = "TitleScene";
    public string introSceneName = "IntroStoryScene";
    public string gameSceneName = "GameScene";
    public string loadingSceneName = "LoadingScene";

    public string nextSceneName;
    public int pendingStageIndex = -1;

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
        GameSaveManager.EnsureSaveDirectory();

        pendingStageIndex = 0;
        nextSceneName = gameSceneName;

        if (!string.IsNullOrEmpty(introSceneName))
            LoadScene(introSceneName);
        else if (string.IsNullOrEmpty(loadingSceneName))
            LoadScene(gameSceneName);
        else
            LoadScene(loadingSceneName);
    }

    public void LoadAfterIntro()
    {
        if (string.IsNullOrEmpty(loadingSceneName))
            LoadScene(string.IsNullOrEmpty(nextSceneName) ? gameSceneName : nextSceneName);
        else
            LoadScene(loadingSceneName);
    }

    public void ContinueButton()
    {
        GameSaveData data = GameSaveManager.Load();
        if (data == null)
        {
            StartButton();
            return;
        }

        pendingStageIndex = data.continueStageIndex;
        nextSceneName = gameSceneName;

        if (string.IsNullOrEmpty(loadingSceneName))
            LoadScene(gameSceneName);
        else
            LoadScene(loadingSceneName);
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
