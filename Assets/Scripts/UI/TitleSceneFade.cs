using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TitleSceneFade : MonoBehaviour
{
    [Header("씬 이탈 페이드아웃 (타이틀 → 인트로)")]
    [Tooltip("비우면 Canvas 위에 검은 FadeOverlay를 만듭니다.")]
    public Image fadeOverlay;
    public float fadeOutDuration = 0.25f;

    private bool loading;

    public void FadeOutToIntro()
    {
        if (loading)
            return;

        loading = true;
        StartCoroutine(FadeOutToIntroRoutine());
    }

    private IEnumerator FadeOutToIntroRoutine()
    {
        SceneLoadManager mgr = SceneLoadManager.Instance;
        if (mgr == null)
        {
            Debug.LogWarning("[TitleSceneFade] SceneLoadManager가 없습니다.");
            loading = false;
            yield break;
        }

        Camera cam = Camera.main;
        if (cam != null)
            cam.backgroundColor = Color.black;

        mgr.pendingStageIndex = 0;
        mgr.nextSceneName = mgr.gameSceneName;

        string introName = mgr.introSceneName;
        if (string.IsNullOrEmpty(introName))
        {
            mgr.StartButton();
            yield break;
        }

        AutoScrollIntro.fadeInFromTitle = true;

        AsyncOperation loadOp = SceneManager.LoadSceneAsync(introName);
        loadOp.allowSceneActivation = false;

        EnsureFadeOverlay();
        if (fadeOutDuration > 0f && fadeOverlay != null)
            yield return FadeOverlayTo(1f, fadeOutDuration);
        else if (fadeOverlay != null)
            SetOverlayAlpha(1f);

        if (loadOp.progress < 0.9f)
        {
            while (loadOp.progress < 0.9f)
                yield return null;
        }

        loadOp.allowSceneActivation = true;
    }

    private IEnumerator FadeOverlayTo(float targetAlpha, float duration)
    {
        if (fadeOverlay == null)
            yield break;

        float startAlpha = fadeOverlay.color.a;
        float time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;
            SetOverlayAlpha(Mathf.Lerp(startAlpha, targetAlpha, time / duration));
            yield return null;
        }

        SetOverlayAlpha(targetAlpha);
    }

    private void EnsureFadeOverlay()
    {
        if (fadeOverlay != null)
            return;

        Canvas canvas = GetComponent<Canvas>();
        if (canvas == null)
            canvas = GetComponentInParent<Canvas>();
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
