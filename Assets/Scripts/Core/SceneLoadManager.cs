using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoadManager : MonoBehaviour
{
    // 씬마다 각자 존재하는 일반 컴포넌트입니다.
    // DontDestroyOnLoad로 유지하면 새 씬의 SceneLoadManager가 파괴되어
    // 그 씬 버튼의 OnClick 참조가 끊기므로 사용하지 않습니다.
    public string titleSceneName = "TitleScene";
    public string gameSceneName = "GameScene";

    public void StartButton()
    {
        Time.timeScale = 1f; // 일시정지 상태로 씬을 떠난 경우 대비
        UnityEngine.SceneManagement.SceneManager.LoadScene(gameSceneName);
    }

    public void TitleButton()
    {
        Time.timeScale = 1f; // 일시정지 상태로 씬을 떠난 경우 대비
        UnityEngine.SceneManagement.SceneManager.LoadScene(titleSceneName);
    }
}
