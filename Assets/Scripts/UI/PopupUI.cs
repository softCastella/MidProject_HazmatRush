using System.Collections;
using TMPro;
using UnityEngine;

public class PopupUI : MonoBehaviour
{
    public GameObject popupPanel;
    public TMP_Text popupText;
    public float showDuration = 2f;
    public float fadeDuration = 0.25f;

    private CanvasGroup canvasGroup;

    void Awake()
    {
        if (popupPanel == null)
            Debug.LogWarning("PopupUI: popupPanel가 할당되지 않았습니다.");

        if (popupText == null)
            Debug.LogWarning("PopupUI: popupText가 할당되지 않았습니다.");

        if (popupPanel != null)
        {
            canvasGroup = popupPanel.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = popupPanel.AddComponent<CanvasGroup>();

            canvasGroup.alpha = 0f;
            popupPanel.SetActive(false);
        }
    }

    public void Show(string message, float duration = -1f, float fade = -1f)
    {
        if (popupPanel == null || popupText == null)
            return;

        if (!gameObject.activeInHierarchy)
            gameObject.SetActive(true);

        popupText.text = message;
        StopAllCoroutines();
        popupPanel.SetActive(true);

        float showTime = duration > 0f ? duration : showDuration;
        float fadeTime = fade >= 0f ? fade : fadeDuration;
        StartCoroutine(ShowRoutine(showTime, fadeTime));
    }

    private IEnumerator ShowRoutine(float showTime, float fadeTime)
    {
        yield return StartCoroutine(FadeTo(1f, fadeTime));
        yield return new WaitForSeconds(showTime);
        yield return StartCoroutine(FadeTo(0f, fadeTime));
        popupPanel.SetActive(false);
    }

    private IEnumerator FadeTo(float targetAlpha, float duration)
    {
        if (canvasGroup == null)
            yield break;

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
