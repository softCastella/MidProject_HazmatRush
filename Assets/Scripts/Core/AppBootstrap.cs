using UnityEngine;

public class AppBootstrap : MonoBehaviour
{
    [SerializeField] private string firstSceneName = "SplashScene";

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
