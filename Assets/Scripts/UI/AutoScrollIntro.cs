using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class AutoScrollIntro : MonoBehaviour
{
    public static bool fadeInFromTitle;

    public ScrollRect scrollRect;
    public float duration = 12f;
    public float startDelay = 1f;
    public bool playOnStart = true;
    public bool loadNextSceneAfterScroll = true;
    public float endDelay = 0.5f;

    [Header("씬 전환 (검은 화면으로 암전)")]
    public Image fadeOverlay;
    public float fadeInDuration = 0.8f;
    public float fadeOutDuration = 0.5f;
    public string fallbackLoadingSceneName = "LoadingScene";

    [Header("빠른 스크롤")]
    public RectTransform skipButtonRect;
    public float fastScrollMultiplier = 2f;

    private Coroutine scrollRoutine;
    private bool sceneLoading;

    void Awake()
    {
        Camera cam = Camera.main;
        EnsureFadeOverlay();

        if (fadeInFromTitle)
        {
            if (cam != null)
                cam.backgroundColor = Color.black;
            SetOverlayColor(0f, 0f, 0f, 1f);
        }
        else
        {
            if (cam != null)
                cam.backgroundColor = Color.black;
            SetOverlayAlpha(0f);
        }
    }

    private void Start()
    {
        if (!playOnStart)
            return;

        if (fadeInFromTitle)
        {
            fadeInFromTitle = false;
            scrollRoutine = StartCoroutine(FadeInThenScroll());
            return;
        }

        scrollRoutine = StartCoroutine(AutoScroll());
    }

    public void SkipIntro()
    {
        BeginExitToLoading();
    }

    private IEnumerator FadeInThenScroll()
    {
        EnsureFadeOverlay();

        if (fadeOverlay != null && fadeInDuration > 0f)
        {
            SetOverlayColor(0f, 0f, 0f, 1f);

            float time = 0f;
            while (time < fadeInDuration)
            {
                time += Time.deltaTime;
                float t = Mathf.Clamp01(time / fadeInDuration);
                float eased = Mathf.SmoothStep(0f, 1f, t);
                SetOverlayAlpha(1f - eased);
                yield return null;
            }
        }

        SetOverlayAlpha(0f);
        scrollRoutine = StartCoroutine(AutoScroll());
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
            float speed = (Input.GetMouseButton(0) && !IsPointerOverSkipButton()) ? fastScrollMultiplier : 1f;
            t += Time.deltaTime * speed;
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

        if (fadeOverlay != null)
            SetOverlayColor(0f, 0f, 0f, fadeOverlay.color.a);

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

    private bool IsPointerOverSkipButton()
    {
        if (skipButtonRect == null) return false;
        return RectTransformUtility.RectangleContainsScreenPoint(skipButtonRect, Input.mousePosition, null);
    }

    private void SetOverlayColor(float r, float g, float b, float alpha)
    {
        if (fadeOverlay == null)
            return;

        float a = Mathf.Clamp01(alpha);
        fadeOverlay.color = new Color(r, g, b, a);
        fadeOverlay.raycastTarget = a > 0.01f;
    }
}
