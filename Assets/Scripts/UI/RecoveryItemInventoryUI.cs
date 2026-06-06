using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RecoveryItemInventoryUI : MonoBehaviour
{
    public Transform[] slotRoots;
    public string countChildName = "Count";
    public string dimChildName = "dim";

    void Awake()
    {
        if (slotRoots == null || slotRoots.Length == 0)
            slotRoots = GetDirectChildSlots();
    }

    public void Refresh(RecoveryItemInventory inventory)
    {
        if (inventory == null || slotRoots == null)
            return;

        for (int i = 0; i < slotRoots.Length && i < RecoveryItemInventory.MaxSlots; i++)
        {
            if (slotRoots[i] == null)
                continue;

            bool filled = inventory.IsSlotFilled(i);

            TMP_Text countText = FindChildText(slotRoots[i], countChildName);
            if (countText != null)
                countText.text = filled ? "1" : "";

            Transform dimMark = FindDimChild(slotRoots[i]);
            if (dimMark != null)
            {
                bool isSelected = filled && inventory.GetSelectedIndex() == i;
                dimMark.gameObject.SetActive(!isSelected);
            }
        }
    }

    private Transform FindDimChild(Transform slot)
    {
        Transform dim = slot.Find(dimChildName);
        if (dim != null)
            return dim;

        foreach (Transform child in slot)
        {
            if (child.name == dimChildName)
                return child;

            Transform found = FindDimChild(child);
            if (found != null)
                return found;
        }

        return null;
    }

    private Transform[] GetDirectChildSlots()
    {
        Transform[] result = new Transform[transform.childCount];
        for (int i = 0; i < transform.childCount; i++)
            result[i] = transform.GetChild(i);
        return result;
    }

    private TMP_Text FindChildText(Transform root, string childName)
    {
        Transform child = root.Find(childName);
        if (child != null)
            return child.GetComponent<TMP_Text>();

        TMP_Text text = root.GetComponentInChildren<TMP_Text>(true);
        return text;
    }
}
