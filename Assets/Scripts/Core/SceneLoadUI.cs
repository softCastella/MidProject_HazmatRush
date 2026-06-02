using UnityEngine;

// 씬 UI 버튼용. OnClick은 이 컴포넌트에 연결하고, 실제 로드는 SceneLoadManager 싱글톤에 위임합니다.
public class SceneLoadUI : MonoBehaviour
{
    [Header("TitleScene — 저장 없음: Start만, 있음: Continue(시작+이어하기)")]
    public GameObject startOnlyGroup;
    public GameObject continueGroup;

    void OnEnable()
    {
        RefreshTitleMenu();
    }

    void RefreshTitleMenu()
    {
        bool hasSave = GameSaveManager.HasSave();
        if (startOnlyGroup != null)
            startOnlyGroup.SetActive(!hasSave);
        if (continueGroup != null)
            continueGroup.SetActive(hasSave);
    }

    private void PlayButtonSfx()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayButtonSfx();
    }

    private bool HasSceneLoader()
    {
        if (SceneLoadManager.Instance != null)
            return true;

        Debug.LogWarning("[SceneLoadUI] SceneLoadManager가 없습니다.");
        return false;
    }

    public void StartButton()
    {
        PlayButtonSfx();
        if (!HasSceneLoader())
            return;
        SceneLoadManager.Instance.StartButton();
    }

    public void ContinueButton()
    {
        PlayButtonSfx();
        if (!HasSceneLoader())
            return;
        SceneLoadManager.Instance.ContinueButton();
    }

    public void TitleButton()
    {
        PlayButtonSfx();
        if (!HasSceneLoader())
            return;
        SceneLoadManager.Instance.TitleButton();
    }
}
