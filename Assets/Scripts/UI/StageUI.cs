using UnityEngine;
using TMPro;

public class StageUI : MonoBehaviour
{
    public TMP_Text stageText;
    public TMP_Text pollutantCountText;

    public void SetStageInfo(string label, int cleared, int total)
    {
        if (stageText != null)
            stageText.text = label;

        if (pollutantCountText != null)
            pollutantCountText.text = $"오염원: {cleared}/{total}";
    }
}