using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class AutoScrollIntro : MonoBehaviour
{
    public ScrollRect scrollRect;
    public float duration = 12f;
    public float startDelay = 1f;
    public bool playOnStart = true;
    public bool loadNextSceneAfterScroll = true;
    public float endDelay = 0.5f;

    [Header("씬 전환")]
    public CanvasGroup fadeGroup;
    public float fadeOutDuration = 0.5f;
    public string fallbackLoadingSceneName = "LoadingScene";

    private Coroutine scrollRoutine;
    private bool sceneLoading;

    void Awake()
    {
        if (fadeGroup == null)
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                fadeGroup = canvas.GetComponent<CanvasGroup>();
                if (fadeGroup == null)
                    fadeGroup = canvas.gameObject.AddComponent<CanvasGroup>();
                fadeGroup.alpha = 1f;
            }
        }
    }

    private void Start()
    {
        if (playOnStart)
            scrollRoutine = StartCoroutine(AutoScroll());
    }

    public void SkipIntro()
    {
        BeginExitToLoading();
    }

    public IEnumerator AutoScroll()
    {
        Canvas.ForceUpdateCanvases();
        yield return null;

        if (scrollRect == null)
        {
            Debug.LogWarning("[AutoScrollIntro] scrollRect가 없습니다.");
            BeginExitToLoading();
            yield break;
        }

        scrollRect.verticalNormalizedPosition = 1f;
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

        BeginExitToLoading();
    }

    private void BeginExitToLoading()
    {
        if (sceneLoading)
            return;

        sceneLoading = true;

        if (scrollRoutine != null)
        {
            StopCoroutine(scrollRoutine);
            scrollRoutine = null;
        }

        StartCoroutine(ExitToLoadingRoutine());
    }

    private IEnumerator ExitToLoadingRoutine()
    {
        if (fadeGroup != null && fadeOutDuration > 0f)
        {
            float startAlpha = fadeGroup.alpha;
            float time = 0f;

            while (time < fadeOutDuration)
            {
                time += Time.deltaTime;
                fadeGroup.alpha = Mathf.Lerp(startAlpha, 0f, time / fadeOutDuration);
                yield return null;
            }

            fadeGroup.alpha = 0f;
        }

        if (SceneLoadManager.Instance != null)
        {
            SceneLoadManager.Instance.LoadAfterIntro();
            yield break;
        }

        Debug.LogWarning("[AutoScrollIntro] SceneLoadManager 없음 → LoadingScene 직접 로드");
        if (string.IsNullOrEmpty(fallbackLoadingSceneName))
        {
            sceneLoading = false;
            yield break;
        }

        SceneManager.LoadScene(fallbackLoadingSceneName);
    }
}
