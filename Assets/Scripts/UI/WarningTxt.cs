using UnityEngine;

using TMPro;

public class WarningTxt : MonoBehaviour
{
    public TMP_Text warningMsg;
    public float blinkDuration = 1.5f;
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
        StartCoroutine(BlinkWarning(blinkDuration, blinkInterval));
    }

    public void HideWarning()
    {
        if (warningMsg == null) return;
        warningMsg.text = string.Empty;
        warningMsg.gameObject.SetActive(false);
    }

    private System.Collections.IEnumerator BlinkWarning(float totalDuration, float blinkInterval)
    {
        float elapsed = 0f;
        warningMsg.gameObject.SetActive(true);
        while (elapsed < totalDuration)
        {
            warningMsg.gameObject.SetActive(!warningMsg.gameObject.activeSelf);
            yield return new WaitForSeconds(blinkInterval);
            elapsed += blinkInterval;
        }
        warningMsg.gameObject.SetActive(false);
    }
}
