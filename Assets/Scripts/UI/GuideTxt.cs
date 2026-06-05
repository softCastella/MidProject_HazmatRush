using System.Collections;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
[DefaultExecutionOrder(-100)]
public class GuideTxt : MonoBehaviour
{
    public bool introFinished = false;

    public TMP_Text guideText;
    public string defaultMessage;
    public float showDelay = 0f;
    public float showDuration = 3f;
    public float fadeDuration = 0.5f;
    public Player player;
    public Timer timer;
    public Background background;

    private CanvasGroup canvasGroup;

    void Awake()
    {
        introFinished = false;
        canvasGroup = GetComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        if (guideText != null)
            guideText.gameObject.SetActive(true);

        if (guideText == null)
            guideText = GetComponentInChildren<TMP_Text>(true);

        if (guideText != null && !string.IsNullOrEmpty(defaultMessage) && string.IsNullOrEmpty(guideText.text))
            guideText.text = defaultMessage;

        if (player == null)
            player = FindAnyObjectByType<Player>();

        if (timer == null)
            timer = FindAnyObjectByType<Timer>();

        if (background == null)
            background = FindAnyObjectByType<Background>();

        if (!string.IsNullOrEmpty(defaultMessage) && IsTutorialStage())
        {
            if (player != null) player.canMove = false;
        }
        else if (!string.IsNullOrEmpty(defaultMessage))
        {
            introFinished = true;
            HideGuide();
        }
    }

    void Start()
    {
        if (string.IsNullOrEmpty(defaultMessage))
        {
            introFinished = true;
            return;
        }

        if (!IsTutorialStage())
        {
            introFinished = true;
            HideGuide();
            if (player != null) player.canMove = true;
            if (timer != null) timer.isRunning = true;
            return;
        }

        StartCoroutine(ShowGuideRoutine(defaultMessage, showDuration, showDelay));
    }

    // Stage 1-1(튜토리얼)만 시작 가이드·아이템 안내 표시
    public static bool IsTutorialStage()
    {
        return GetStageIndex() == 0;
    }

    private static int GetStageIndex()
    {
        if (SceneLoadManager.Instance != null && SceneLoadManager.Instance.pendingStageIndex >= 0)
            return SceneLoadManager.Instance.pendingStageIndex;

        StageManager stageManager = FindAnyObjectByType<StageManager>();
        if (stageManager != null)
            return stageManager.currentStageIndex;

        return 0;
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

    public IEnumerator ShowItemSelectHintRoutine(string message, float duration)
    {
        if (guideText == null)
            guideText = GetComponentInChildren<TMP_Text>(true);
        if (guideText == null)
            yield break;

        guideText.text = message;
        guideText.gameObject.SetActive(true);

        yield return StartCoroutine(FadeTo(1f, fadeDuration));
        yield return new WaitForSeconds(duration > 0f ? duration : showDuration);
        yield return StartCoroutine(FadeTo(0f, fadeDuration));

        Debug.Log($"[GuideTxt] {message}");
    }

    // 페이드 없이 즉시 문구를 노출합니다. (오대응 패널티 안내 등)
    public void ShowGuideImmediate(string message)
    {
        StopAllCoroutines();
        SetGuideText(message);
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup != null)
            canvasGroup.alpha = 1f;
    }

    public void HideGuide()
    {
        StopAllCoroutines();
        if (canvasGroup != null)
            canvasGroup.alpha = 0f;
        if (guideText != null)
            guideText.gameObject.SetActive(false);
    }

    private IEnumerator ShowGuideRoutine(string message, float duration, float delay)
    {
        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        float displayDuration = duration > 0f ? duration : showDuration;
        yield return StartCoroutine(FadeTo(1f, fadeDuration));
        yield return new WaitForSeconds(displayDuration);
        yield return StartCoroutine(FadeTo(0f, fadeDuration));

        introFinished = true;

        if (player != null) player.canMove = true;
        if (timer != null) timer.isRunning = true;
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
