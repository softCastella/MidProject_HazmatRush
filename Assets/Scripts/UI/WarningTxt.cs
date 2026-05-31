using UnityEngine;
using TMPro;

[DefaultExecutionOrder(-100)]
public class WarningTxt : MonoBehaviour
{
    public TMP_Text warningMsg;
    public int blinkCount = 3;
    public float blinkInterval = 0.3f;

    void Awake()
    {
        if (warningMsg == null)
            warningMsg = GetComponentInChildren<TMP_Text>(true);
        HideWarning();
    }

    public void ShowWarning(string text)
    {
        if (warningMsg == null)
            return;

        warningMsg.text = text;
        warningMsg.gameObject.SetActive(true);
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
            warningMsg.gameObject.SetActive(true);
            warningMsg.ForceMeshUpdate(true);
            yield return new WaitForSeconds(safeInterval);
            warningMsg.gameObject.SetActive(false);
            yield return new WaitForSeconds(safeInterval);
        }

        HideWarning();
    }

    public void HideWarning()
    {
        StopAllCoroutines();
        if (warningMsg != null)
        {
            warningMsg.text = string.Empty;
            warningMsg.gameObject.SetActive(false);
        }
    }

    private System.Collections.IEnumerator BlinkWarning(int count, float interval)
    {
        int safeCount = Mathf.Max(1, count);
        float safeInterval = Mathf.Max(0.01f, interval);

        for (int i = 0; i < safeCount; i++)
        {
            warningMsg.gameObject.SetActive(true);
            yield return new WaitForSeconds(safeInterval);
            warningMsg.gameObject.SetActive(false);
            yield return new WaitForSeconds(safeInterval);
        }

        warningMsg.gameObject.SetActive(false);
    }
}
