using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ProtectionHpBarUI : MonoBehaviour
{
    private const int StepCount = 7;

    [Header("FillArea 자식 Fill_0~Fill_6 Image (비우면 자동 탐색)")]
    public Image[] fillSteps;

    public TMP_Text percentText;

    void Awake()
    {
        ResolveRefs();
    }

    public void ResolveRefs()
    {
        if (percentText == null)
        {
            Transform text = transform.Find("Text (TMP)");
            if (text != null)
                percentText = text.GetComponent<TMP_Text>();
        }

        if (fillSteps != null && fillSteps.Length > 0)
            return;

        Transform fillArea = transform.Find("FillArea");
        if (fillArea == null)
            return;

        fillSteps = new Image[StepCount];
        for (int i = 0; i < StepCount; i++)
        {
            Transform child = fillArea.Find("Fill_" + i);
            if (child != null)
                fillSteps[i] = child.GetComponent<Image>();
        }
    }

    public void SetProtection(float current, float max)
    {
        if (max <= 0f)
            max = 100f;

        int percent = Mathf.Clamp(Mathf.FloorToInt(current), 0, Mathf.FloorToInt(max));
        if (percentText != null)
            percentText.text = percent.ToString("D3") + "%";

        int step = ProtectionToStep(current, max);
        ApplyStep(step);
    }

    public static int ProtectionToStep(float current, float max)
    {
        if (current <= 0f)
            return -1;

        if (max <= 0f)
            max = 100f;

        float ratio = Mathf.Clamp01(current / max);
        return Mathf.Clamp(StepCount - 1 - Mathf.FloorToInt(ratio * (StepCount - 1)), 0, StepCount - 1);
    }

    private void ApplyStep(int step)
    {
        if (fillSteps == null || fillSteps.Length == 0)
            return;

        for (int i = 0; i < fillSteps.Length; i++)
        {
            if (fillSteps[i] == null)
                continue;
            fillSteps[i].gameObject.SetActive(i == step);
        }
    }
}
