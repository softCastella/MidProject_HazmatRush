using UnityEngine;

public class UIItem : MonoBehaviour
{
    public GameObject selectedFrame;

    public void SetSelected(bool selected)
    {
        if (selectedFrame != null)
            selectedFrame.SetActive(selected);
    }
}
