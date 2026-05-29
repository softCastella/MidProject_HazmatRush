using UnityEngine;

// 씬 UI 버튼용. OnClick은 이 컴포넌트에 연결하고, 실제 로드는 SceneLoadManager 싱글톤에 위임합니다.
public class SceneLoadUI : MonoBehaviour
{
    public void StartButton()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayButtonSfx();

        if (SceneLoadManager.Instance == null)
        {
            Debug.LogWarning("[SceneLoadUI] SceneLoadManager가 없습니다.");
            return;
        }
        SceneLoadManager.Instance.StartButton();
    }

    public void TitleButton()
    {
        // if (AudioManager.Instance != null)
        //     AudioManager.Instance.PlayButtonSfx();

        if (SceneLoadManager.Instance == null)
        {
            Debug.LogWarning("[SceneLoadUI] SceneLoadManager가 없습니다.");
            return;
        }
        SceneLoadManager.Instance.TitleButton();
    }
}
