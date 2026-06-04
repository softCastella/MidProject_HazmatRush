using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class AutoScrollIntro : MonoBehaviour
{
    public ScrollRect scrollRect;
    public float duration = 12f;   // 위에서 아래로 내려가는 시간
    public float startDelay = 1f;  // 시작 전 잠깐 대기
    public bool playOnStart = true;
    public bool loadNextSceneAfterScroll = true;
    public float endDelay = 0.5f;

    private Coroutine scrollRoutine;
    private bool sceneLoading;

    private void Start()
    {
        if (playOnStart)
            scrollRoutine = StartCoroutine(AutoScroll());
    }

    public void SkipIntro()
    {
        if (sceneLoading)
            return;

        if (scrollRoutine != null)
        {
            StopCoroutine(scrollRoutine);
            scrollRoutine = null;
        }

        GoToLoadingScene();
    }

    public IEnumerator AutoScroll()
    {
        Canvas.ForceUpdateCanvases();
        yield return null;

        scrollRect.verticalNormalizedPosition = 1f; // 맨 위에서 시작
        yield return new WaitForSeconds(startDelay);

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / duration);
            scrollRect.verticalNormalizedPosition = 1f - p;
            yield return null;
        }

        scrollRect.verticalNormalizedPosition = 0f;

        if (endDelay > 0f)
            yield return new WaitForSeconds(endDelay);

        if (!loadNextSceneAfterScroll)
            yield break;

        GoToLoadingScene();
    }

    private void GoToLoadingScene()
    {
        if (sceneLoading)
            return;

        sceneLoading = true;

        if (SceneLoadManager.Instance == null)
        {
            Debug.LogWarning("[AutoScrollIntro] SceneLoadManager가 없어 다음 씬으로 넘어가지 않습니다.");
            sceneLoading = false;
            return;
        }

        SceneLoadManager.Instance.LoadAfterIntro();
    }
}