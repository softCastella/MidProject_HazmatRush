using System.Collections;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
public class GuideTxt : MonoBehaviour
{
    public TMP_Text guideText;
    public string defaultMessage;
    public float showDelay = 0f;
    public float showDuration = 3f;
    public float fadeDuration = 0.5f;

    private CanvasGroup canvasGroup;

    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        if (guideText == null)
            guideText = GetComponentInChildren<TMP_Text>(true);

        if (guideText != null && !string.IsNullOrEmpty(defaultMessage) && string.IsNullOrEmpty(guideText.text))
            guideText.text = defaultMessage;
    }

    void Start()
    {
        if (!string.IsNullOrEmpty(defaultMessage))
            StartCoroutine(ShowGuideRoutine(defaultMessage, showDuration, showDelay));
    }

    public void SetGuideText(string message)
    {
        if (guideText == null)
            guideText = GetComponentInChildren<TMP_Text>(true);

        if (guideText != null)
        {
            guideText.text = message;
            guideText.gameObject.SetActive(true);
        }
    }

    public void ShowGuideText(string message, float duration = -1f, float delay = 0f)
    {
        SetGuideText(message);
        StopAllCoroutines();
        StartCoroutine(ShowGuideRoutine(message, duration, delay));
    }

    private IEnumerator ShowGuideRoutine(string message, float duration, float delay)
    {
        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        float displayDuration = duration > 0f ? duration : showDuration;
        yield return StartCoroutine(FadeTo(1f, fadeDuration));
        yield return new WaitForSeconds(displayDuration);
        yield return StartCoroutine(FadeTo(0f, fadeDuration));
    }

    private IEnumerator FadeTo(float targetAlpha, float duration)
    {
        if (duration <= 0f)
        {
            canvasGroup.alpha = targetAlpha;
            yield break;
        }

        float startAlpha = canvasGroup.alpha;
        float time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, time / duration);
            yield return null;
        }

        canvasGroup.alpha = targetAlpha;
    }
}
