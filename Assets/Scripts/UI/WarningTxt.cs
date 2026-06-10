using UnityEngine;
using TMPro;

[DefaultExecutionOrder(-100)]
public class WarningTxt : MonoBehaviour
{
    public TMP_Text warningMsg;
    public TMP_Text warningLabel;
    public Transform popupBg;
    public int blinkCount = 3;
    public float blinkInterval = 0.3f;

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

        if (popupBg == null)
            popupBg = transform.Find("Bg");
    }

    void SetPopupVisible(bool visible)
    {
        ResolveRefs();

        if (popupBg != null)
            popupBg.gameObject.SetActive(visible);

        if (warningLabel != null)
            warningLabel.gameObject.SetActive(visible);

        if (warningMsg != null)
            warningMsg.gameObject.SetActive(visible);
    }

    public void ShowWarning(string text)
    {
        if (warningMsg == null)
            return;

        warningMsg.text = text;
        StopAllCoroutines();
        StartCoroutine(BlinkWarning(blinkCount, blinkInterval));
    }

    public System.Collections.IEnumerator ShowWarningRoutine(string warningText)
    {
        if (warningMsg == null)
            yield break;

        StopAllCoroutines();
        gameObject.SetActive(true);
        warningMsg.text = warningText;

        int safeCount = Mathf.Max(1, blinkCount);
        float safeInterval = Mathf.Max(0.01f, blinkInterval);

        for (int i = 0; i < safeCount; i++)
        {
            SetPopupVisible(true);
            warningMsg.ForceMeshUpdate(true);
            yield return new WaitForSeconds(safeInterval);
            SetPopupVisible(false);
            yield return new WaitForSeconds(safeInterval);
        }

        HideWarning();
    }

    public void HideWarning()
    {
        StopAllCoroutines();
        if (warningMsg != null)
            warningMsg.text = string.Empty;
        SetPopupVisible(false);
    }

    private System.Collections.IEnumerator BlinkWarning(int count, float interval)
    {
        int safeCount = Mathf.Max(1, count);
        float safeInterval = Mathf.Max(0.01f, interval);

        for (int i = 0; i < safeCount; i++)
        {
            SetPopupVisible(true);
            yield return new WaitForSeconds(safeInterval);
            SetPopupVisible(false);
            yield return new WaitForSeconds(safeInterval);
        }

        SetPopupVisible(false);
    }
}
