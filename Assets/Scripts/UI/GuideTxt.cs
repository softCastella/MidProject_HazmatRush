using System.Collections;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
[DefaultExecutionOrder(-100)]
public class GuideTxt : MonoBehaviour
{
    private enum PopupMode
    {
        Hidden,
        Short,
        Long
    }

    public bool introFinished = false;

    public TMP_Text guideText;
    public GameObject popupShortRoot;
    public GameObject popupLongRoot;
    [Tooltip("이 글자 수보다 길면 bg1, 짧으면 bg0")]
    public int longMessageCharThreshold = 16;
    public string defaultMessage;
    public float showDelay = 0f;
    public float showDuration = 3f;
    public float fadeDuration = 0.5f;
    public Player player;
    public Timer timer;
    public Background background;

    private CanvasGroup canvasGroup;
    private bool isMessageVisible;
    private Color defaultGuideColor = Color.white;

    public bool IsMessageVisible => isMessageVisible;

    void Awake()
    {
        introFinished = false;
        canvasGroup = GetComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        if (guideText == null)
            guideText = GetComponentInChildren<TMP_Text>(true);

        if (guideText != null)
            defaultGuideColor = guideText.color;

        ResolvePopupRoots();
        ApplyPopup(PopupMode.Hidden);

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

    void ResolvePopupRoots()
    {
        if (popupShortRoot == null)
        {
            Transform found = transform.Find("bg0");
            if (found != null)
                popupShortRoot = found.gameObject;
        }

        if (popupLongRoot == null)
        {
            Transform found = transform.Find("bg1");
            if (found != null)
                popupLongRoot = found.gameObject;
        }
    }

    bool IsLongMessage(string message)
    {
        if (string.IsNullOrEmpty(message))
            return false;

        return message.Length > longMessageCharThreshold;
    }

    PopupMode GetPopupMode(string message, bool visible)
    {
        if (!visible)
            return PopupMode.Hidden;
        if (IsLongMessage(message))
            return PopupMode.Long;
        return PopupMode.Short;
    }

    void SetChildrenActive(Transform parent, bool active)
    {
        for (int i = 0; i < parent.childCount; i++)
            parent.GetChild(i).gameObject.SetActive(active);
    }

    // 팝업은 bg0 / bg1 루트 Image만 사용. 자식 bg (1~3)은 중복이라 항상 OFF.
    void ApplyPopup(PopupMode mode)
    {
        ResolvePopupRoots();

        if (popupShortRoot != null)
        {
            SetChildrenActive(popupShortRoot.transform, false);
            popupShortRoot.SetActive(mode == PopupMode.Short);
        }

        if (popupLongRoot != null)
        {
            SetChildrenActive(popupLongRoot.transform, false);
            popupLongRoot.SetActive(mode == PopupMode.Long);
        }

        if (guideText != null)
            guideText.gameObject.SetActive(mode != PopupMode.Hidden);
    }

    public void SetGuideText(string message)
    {
        if (guideText == null)
            guideText = GetComponentInChildren<TMP_Text>(true);

        if (guideText != null)
        {
            guideText.text = message;
            guideText.color = defaultGuideColor;
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

        StopAllCoroutines();
        isMessageVisible = true;
        SetGuideText(message);
        ApplyPopup(GetPopupMode(message, true));

        yield return StartCoroutine(FadeTo(1f, fadeDuration));
        yield return new WaitForSeconds(duration > 0f ? duration : showDuration);
        yield return StartCoroutine(FadeTo(0f, fadeDuration));
        ApplyPopup(PopupMode.Hidden);
        isMessageVisible = false;

        Debug.Log($"[GuideTxt] {message}");
    }

    public void ShowGuideImmediate(string message)
    {
        ShowGuideImmediate(message, defaultGuideColor);
    }

    public void ShowGuideImmediate(string message, Color textColor)
    {
        StopAllCoroutines();
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();
        gameObject.SetActive(true);
        isMessageVisible = true;
        if (guideText == null)
            guideText = GetComponentInChildren<TMP_Text>(true);
        if (guideText != null)
        {
            guideText.text = message;
            guideText.color = textColor;
        }
        ApplyPopup(GetPopupMode(message, true));
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
    }

    public void HideGuide()
    {
        StopAllCoroutines();
        isMessageVisible = false;
        if (canvasGroup != null)
            canvasGroup.alpha = 0f;
        if (guideText != null)
            guideText.color = defaultGuideColor;
        ApplyPopup(PopupMode.Hidden);
    }

    private IEnumerator ShowGuideRoutine(string message, float duration, float delay)
    {
        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        isMessageVisible = true;
        SetGuideText(message);
        ApplyPopup(GetPopupMode(message, true));

        float displayDuration = duration > 0f ? duration : showDuration;
        yield return StartCoroutine(FadeTo(1f, fadeDuration));
        yield return new WaitForSeconds(displayDuration);
        yield return StartCoroutine(FadeTo(0f, fadeDuration));
        ApplyPopup(PopupMode.Hidden);
        isMessageVisible = false;

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
