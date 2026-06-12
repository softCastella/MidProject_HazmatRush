using UnityEngine;

public class AppBootstrap : MonoBehaviour
{
    [SerializeField] private string firstSceneName = "SplashScene";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void ForceWindowedOnPcBuild()
    {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        if (Screen.fullScreenMode != FullScreenMode.Windowed)
            Screen.SetResolution(1920, 1080, FullScreenMode.Windowed);
#endif
    }

    void Awake()
    {
        Application.runInBackground = true;
    }

    void Start()
    {
        if (SceneLoadManager.Instance == null)
        {
            Debug.LogError("[AppBootstrap] SceneLoadManager가 없습니다.");
            return;
        }

        SceneLoadManager.Instance.LoadScene(firstSceneName);
    }
}
