using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SplashController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CanvasGroup logoCanvasGroup;
    [SerializeField] private RectTransform logoTransform;
    [SerializeField] private Image shineImage;

    [Header("Scene")]
    [SerializeField] private string nextSceneName = "TitleScene";

    [Header("Timing")]
    [SerializeField] private float totalDuration = 2f;
    [SerializeField] private float fadeDuration = 0.65f;
    [Tooltip("페이드인 시작 후 SFX 재생까지 대기(초). 페이드인과 같이 들리게 맞춤")]
    [SerializeField] private float splashSfxDelay = 0.22f;
    [SerializeField] private float fadeOutDuration = 1.5f;
    [SerializeField] private float scaleDuration = 1.2f;
    [SerializeField] private float shineDelay = 0.9f;
    [SerializeField] private float shineDuration = 0.25f;

    [Header("Scale")]
    [SerializeField] private Vector3 startScale = new Vector3(0.96f, 0.96f, 1f);
    [SerializeField] private Vector3 endScale = new Vector3(1.08f, 1.08f, 1f);
    [Tooltip("endScale 대비 추가 배율. 0.5 = 지금 최대 크기보다 50% 더 큼")]
    [SerializeField] private float peakScaleExtra = 0.5f;
    [Tooltip("페이드아웃 때 peak 대비 살짝만 줄어드는 비율 (완전히 작아지지 않음)")]
    [SerializeField] private float fadeOutShrinkRatio = 0.1f;

    private void Start()
    {
        logoCanvasGroup.alpha = 0f;
        logoTransform.localScale = startScale;

        if (shineImage != null)
        {
            Color c = shineImage.color;
            c.a = 0f;
            shineImage.color = c;
        }

        StartCoroutine(PlaySplash());
    }

    private IEnumerator PlaySplash()
    {
        AsyncOperation loadOp = SceneManager.LoadSceneAsync(nextSceneName);
        loadOp.allowSceneActivation = false;

        StartCoroutine(ScaleLogo());
        yield return StartCoroutine(FadeInLogo());

        yield return new WaitForSeconds(shineDelay);
        if (shineImage != null)
            yield return StartCoroutine(PlayShine());

        float remain = totalDuration - shineDelay - shineDuration;
        if (remain > 0f)
            yield return new WaitForSeconds(remain);

        yield return StartCoroutine(FadeOutLogo());
        StopSplashSfx();

        if (loadOp.progress < 0.9f)
        {
            while (loadOp.progress < 0.9f)
                yield return null;
        }

        loadOp.allowSceneActivation = true;
    }

    private void PlaySplashSfx()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySplashSfx();
    }

    private void StopSplashSfx()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.StopSplashSfx();
    }

    private IEnumerator FadeInLogo()
    {
        float time = 0f;
        bool sfxPlayed = false;

        while (time < fadeDuration)
        {
            time += Time.deltaTime;
            logoCanvasGroup.alpha = Mathf.Clamp01(time / fadeDuration);

            if (!sfxPlayed && time >= splashSfxDelay)
            {
                PlaySplashSfx();
                sfxPlayed = true;
            }

            yield return null;
        }

        logoCanvasGroup.alpha = 1f;

        if (!sfxPlayed)
            PlaySplashSfx();
    }

    private Vector3 GetPeakScale()
    {
        float mul = 1f + peakScaleExtra;
        return new Vector3(endScale.x * mul, endScale.y * mul, endScale.z);
    }

    private IEnumerator FadeOutLogo()
    {
        if (fadeOutDuration <= 0f)
        {
            logoCanvasGroup.alpha = 0f;
            yield break;
        }

        Vector3 fadeStartScale = logoTransform.localScale;
        float shrinkMul = 1f - fadeOutShrinkRatio;
        Vector3 fadeEndScale = fadeStartScale * shrinkMul;
        float time = 0f;

        while (time < fadeOutDuration)
        {
            time += Time.deltaTime;
            float t = Mathf.Clamp01(time / fadeOutDuration);

            // 알파: 잠깐 남아 있다가 서서히 흐려짐
            float alphaEased = 1f - Mathf.Pow(1f - t, 2.2f);
            float alpha = Mathf.Lerp(1f, 0f, alphaEased);
            logoCanvasGroup.alpha = alpha;

            // 스케일: 살짝 줄어들기만 시작 (알파보다 느리게)
            float scaleEased = t * t * 0.4f;
            logoTransform.localScale = Vector3.Lerp(fadeStartScale, fadeEndScale, scaleEased);

            if (alpha <= 0.02f)
            {
                logoCanvasGroup.alpha = 0f;
                yield break;
            }

            yield return null;
        }

        logoCanvasGroup.alpha = 0f;
    }

    // 로고 확대 — peakScale(endScale + 50%)까지
    private IEnumerator ScaleLogo()
    {
        Vector3 peakScale = GetPeakScale();
        float time = 0f;

        while (time < scaleDuration)
        {
            time += Time.deltaTime;
            float t = time / scaleDuration;
            t = 1f - Mathf.Pow(1f - t, 3f);
            logoTransform.localScale = Vector3.Lerp(startScale, peakScale, t);
            yield return null;
        }

        logoTransform.localScale = peakScale;
    }

    private IEnumerator PlayShine()
    {
        float half = shineDuration * 0.5f;
        float time = 0f;
        Color c = shineImage.color;

        while (time < half)
        {
            time += Time.deltaTime;
            c.a = Mathf.Lerp(0f, 0.5f, time / half);
            shineImage.color = c;
            yield return null;
        }

        time = 0f;
        while (time < half)
        {
            time += Time.deltaTime;
            c.a = Mathf.Lerp(0.5f, 0f, time / half);
            shineImage.color = c;
            yield return null;
        }

        c.a = 0f;
        shineImage.color = c;
    }
}