using UnityEngine;

using TMPro;

public class WarningTxt : MonoBehaviour
{
    public TMP_Text warningMsg;
    public int blinkCount = 3;
    public float blinkInterval = 0.3f;

    void Awake()
    {
        if (warningMsg == null)
            warningMsg = GetComponentInChildren<TMP_Text>(true);
        if (warningMsg != null)
            warningMsg.text = string.Empty;
        if (warningMsg != null)
            warningMsg.gameObject.SetActive(false);
    }

    public void ShowWarning(string text)
    {
        if (warningMsg == null) return;
        warningMsg.text = text;
        if (warningMsg.transform.parent != null)
            warningMsg.transform.parent.gameObject.SetActive(true);
        warningMsg.gameObject.SetActive(true);
        StopAllCoroutines();
        StartCoroutine(BlinkWarning(blinkCount, blinkInterval));
    }

    public void HideWarning()
    {
        if (warningMsg == null) return;
        warningMsg.text = string.Empty;
        warningMsg.gameObject.SetActive(false);
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
