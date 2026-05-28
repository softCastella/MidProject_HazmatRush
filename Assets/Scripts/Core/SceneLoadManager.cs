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

    public static SceneLoadManager EnsureInstance()
    {
        if (Instance != null)
            return Instance;

        Instance = FindAnyObjectByType<SceneLoadManager>();
        if (Instance != null)
            return Instance;

        var go = new GameObject("SceneLoadManager");
        Instance = go.AddComponent<SceneLoadManager>();
        DontDestroyOnLoad(go);
        return Instance;
    }

    public void StartButton()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(gameSceneName);
    }

    public void TitleButton()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(titleSceneName);
    }
}
