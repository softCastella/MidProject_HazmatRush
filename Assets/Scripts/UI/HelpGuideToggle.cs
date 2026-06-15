using UnityEngine;

public class HelpGuideToggle : MonoBehaviour
{
    public GameObject keyGuidePanel;
    public GameObject itemGuidePanel;

    void Awake()
    {
        ResolvePanels();
        if (keyGuidePanel != null)
            keyGuidePanel.SetActive(false);
        if (itemGuidePanel != null)
            itemGuidePanel.SetActive(false);
    }

    void Update()
    {
        if (!CanToggle())
            return;

        if (Input.GetKeyDown(KeyCode.K))
            TogglePanel(keyGuidePanel);
        else if (Input.GetKeyDown(KeyCode.I))
            TogglePanel(itemGuidePanel);
    }

    private bool CanToggle()
    {
        if (GameManager.Instance == null)
            return true;

        if (GameManager.Instance.GameEnded)
            return false;
        if (GameManager.Instance.IsPaused)
            return false;
        if (GameManager.Instance.IsPenalty)
            return false;

        PollutantManager pollutantManager = GameManager.Instance.pollutantManager;
        if (pollutantManager == null)
            pollutantManager = FindAnyObjectByType<PollutantManager>();
        if (pollutantManager != null && pollutantManager.IsWarningFreeze)
            return false;

        return true;
    }

    private void ResolvePanels()
    {
        if (keyGuidePanel == null)
            keyGuidePanel = FindPanel("KeyGuidePanel");
        if (keyGuidePanel == null)
            keyGuidePanel = FindPanel("KeyGuidePannel");
        if (itemGuidePanel == null)
            itemGuidePanel = FindPanel("ItemGuide");
        if (itemGuidePanel == null)
            itemGuidePanel = FindPanel("ItemGuide");
    }

    private GameObject FindPanel(string panelName)
    {
        Transform[] children = GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
        {
            if (children[i].name == panelName)
                return children[i].gameObject;
        }

        return null;
    }

    private void TogglePanel(GameObject panel)
    {
        if (panel == null)
            return;

        panel.SetActive(!panel.activeSelf);
    }
}
