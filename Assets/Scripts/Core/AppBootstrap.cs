using UnityEngine;

public class AppBootstrap : MonoBehaviour
{
    [SerializeField] private string firstSceneName = "SplashScene";

    const int PcLaunchWidth = 1280;
    const int PcLaunchHeight = 720;

    // 설계 1920×1080 · 첫 기동은 창 1280×720(16:9). 전체화면·최대화는 사용자가 Alt+Enter 등으로 선택.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void ForceWindowedOnPcBuild()
    {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        if (Screen.fullScreenMode != FullScreenMode.Windowed)
            Screen.SetResolution(PcLaunchWidth, PcLaunchHeight, FullScreenMode.Windowed);
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
