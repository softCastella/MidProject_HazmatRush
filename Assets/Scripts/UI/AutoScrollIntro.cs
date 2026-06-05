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

    [Header("씬 전환 (검은 화면으로 암전)")]
    public Image fadeOverlay;
    public float fadeOutDuration = 0.5f;
    public string fallbackLoadingSceneName = "LoadingScene";

    private Coroutine scrollRoutine;
    private bool sceneLoading;

    void Awake()
    {
        Camera cam = Camera.main;
        if (cam != null)
            cam.backgroundColor = Color.black;

        EnsureFadeOverlay();
        SetOverlayAlpha(0f);
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
        EnsureFadeOverlay();

        if (fadeOverlay != null && fadeOutDuration > 0f)
        {
            float time = 0f;
            while (time < fadeOutDuration)
            {
                time += Time.deltaTime;
                SetOverlayAlpha(Mathf.Lerp(0f, 1f, time / fadeOutDuration));
                yield return null;
            }

            SetOverlayAlpha(1f);
        }
        else if (fadeOverlay != null)
        {
            SetOverlayAlpha(1f);
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

    private void EnsureFadeOverlay()
    {
        if (fadeOverlay != null)
            return;

        Transform found = transform.root.Find("FadeOverlay");
        if (found != null)
        {
            fadeOverlay = found.GetComponent<Image>();
            if (fadeOverlay != null)
                return;
        }

        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
            return;

        GameObject go = new GameObject("FadeOverlay");
        go.transform.SetParent(canvas.transform, false);
        go.transform.SetAsLastSibling();

        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        fadeOverlay = go.AddComponent<Image>();
        fadeOverlay.color = new Color(0f, 0f, 0f, 0f);
        fadeOverlay.raycastTarget = false;
    }

    private void SetOverlayAlpha(float alpha)
    {
        if (fadeOverlay == null)
            return;

        Color color = fadeOverlay.color;
        color.a = Mathf.Clamp01(alpha);
        fadeOverlay.color = color;
        fadeOverlay.raycastTarget = color.a > 0.01f;
    }
}
