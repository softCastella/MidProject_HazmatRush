using UnityEngine;
using TMPro;

[DefaultExecutionOrder(-100)]
public class WarningTxt : MonoBehaviour
{
    public TMP_Text warningMsg;
    public TMP_Text warningLabel;

    [Header("경고 연출")]
    [Tooltip("깜빡이기 전 고정 표시 시간(초).")]
    public float warningHoldDuration = 1.2f;
    [Tooltip("깜빡임 횟수 (ON→OFF 1세트).")]
    public int blinkCount = 3;
    [Tooltip("깜빡임 ON 또는 OFF 유지 시간(초).")]
    public float blinkInterval = 0.28f;

    private bool isMessageVisible;

    public bool IsMessageVisible => isMessageVisible;

    void Awake()
    {
        ResolveRefs();
        HideWarning();
    }

    void ResolveRefs()
    {
        if (warningMsg == null)
        {
            Transform msg = transform.Find("WarningMsg");
            if (msg != null)
                warningMsg = msg.GetComponent<TMP_Text>();
        }

        if (warningLabel == null)
        {
            Transform label = transform.Find("WarningLabel");
            if (label != null)
                warningLabel = label.GetComponent<TMP_Text>();
        }
    }

    // Bg ~ Bg (5) · WarningLabel · WarningMsg 등 자식 전부 함께 ON/OFF
    void SetPopupVisible(bool visible)
    {
        ResolveRefs();

        for (int i = 0; i < transform.childCount; i++)
            transform.GetChild(i).gameObject.SetActive(visible);
    }

    public void ShowWarning(string text)
    {
        if (warningMsg == null)
            return;

        warningMsg.text = text;
        StopAllCoroutines();
        StartCoroutine(ShowWarningRoutine(text));
    }

    public System.Collections.IEnumerator ShowWarningRoutine(string warningText)
    {
        if (warningMsg == null)
            yield break;

        StopAllCoroutines();
        gameObject.SetActive(true);
        warningMsg.text = warningText;
        isMessageVisible = true;

        float holdTime = Mathf.Max(0f, warningHoldDuration);
        if (holdTime > 0f)
        {
            SetPopupVisible(true);
            warningMsg.ForceMeshUpdate(true);
            yield return new WaitForSeconds(holdTime);
        }

        int count = Mathf.Max(1, blinkCount);
        float interval = Mathf.Max(0.1f, blinkInterval);

        for (int i = 0; i < count; i++)
        {
            SetPopupVisible(true);
            warningMsg.ForceMeshUpdate(true);
            yield return new WaitForSeconds(interval);

            SetPopupVisible(false);
            yield return new WaitForSeconds(interval);
        }

        HideWarning();
    }

    public void HideWarning()
    {
        StopAllCoroutines();
        isMessageVisible = false;
        if (warningMsg != null)
            warningMsg.text = string.Empty;
        SetPopupVisible(false);
    }
}
