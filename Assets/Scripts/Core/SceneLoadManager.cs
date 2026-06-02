using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoadManager : MonoBehaviour
{
    public static SceneLoadManager Instance { get; private set; }

    public string titleSceneName = "TitleScene";
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
        BeginGame(0);
    }

    public void ContinueButton()
    {
        GameSaveData data = GameSaveManager.Load();
        if (data == null)
        {
            BeginGame(0);
            return;
        }

        BeginGame(data.continueStageIndex);
    }

    // 시작·이어하기 공통: 스테이지 번호 정한 뒤 로딩 씬(또는 게임 씬)으로 이동
    private void BeginGame(int stageIndex)
    {
        pendingStageIndex = stageIndex;
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
