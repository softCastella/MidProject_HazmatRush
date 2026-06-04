using System.Collections;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
public class LoadingGuideTxt : MonoBehaviour
{
    public TMP_Text guideText;
    public float fadeDuration = 0.5f;
    public float showDuration = 2f;

    private CanvasGroup canvasGroup;
    private Coroutine loopRoutine;

    // C/V키로 회복 아이템 선택, Space로 사용 (회복 아이템 연결 후)

    private static readonly string[] Messages =
    {
        "A, <-키로 왼쪽이동, D, -> 키로 오른쪽 이동이 가능해요.",
        "오염원 경고가 뜨고 X, Z키(좌,우)로 중화 아이템을 선택할 수 있어요.",
        "C, V키로 회복 아이템을 고르고 Space키로 사용할 수 있어요.",
        "ESC키로 일시정지가 가능해요."
    };

    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        if (guideText == null)
            guideText = GetComponentInChildren<TMP_Text>(true);
    }

    void OnEnable()
    {
        StartLoop();
    }

    void OnDisable()
    {
        StopLoop();
    }

    private void StartLoop()
    {
        StopLoop();
        if (Messages.Length == 0)
            return;

        loopRoutine = StartCoroutine(ShowMessagesLoop());
    }

    private void StopLoop()
    {
        if (loopRoutine == null)
            return;

        StopCoroutine(loopRoutine);
        loopRoutine = null;
    }

    private IEnumerator ShowMessagesLoop()
    {
        int index = 0;

        while (true)
        {
            if (guideText != null)
                guideText.text = Messages[index];

            yield return StartCoroutine(FadeTo(1f, fadeDuration));

            float hold = showDuration > 0f ? showDuration : 0f;
            if (hold > 0f)
                yield return new WaitForSeconds(hold);

            yield return StartCoroutine(FadeTo(0f, fadeDuration));

            index++;
            if (index >= Messages.Length)
                index = 0;
        }
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
