using System.Collections.Generic;
using UnityEngine;

public class ItemManager : MonoBehaviour
{
    public Transform[] slots;
    public string dimChildName = "dim";

    void Awake()
    {
        if (slots == null || slots.Length == 0)
            slots = GetDirectChildSlots();
    }

    public void SetSelected(int index)
    {
        if (slots == null || slots.Length == 0)
            return;

        if (index < 0 || index >= slots.Length)
            index = 0;

        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == null)
                continue;

            SetSlotDim(slots[i], i != index);
        }
    }

    private void SetSlotDim(Transform slot, bool dimActive)
    {
        if (slot == null)
            return;

        Transform dimTransform = slot.Find(dimChildName);
        if (dimTransform != null)
        {
            dimTransform.gameObject.SetActive(dimActive);
            return;
        }

        // 하위 구조가 다른 경우에도 dim을 찾도록 탐색
        dimTransform = FindDeepChild(slot, dimChildName);
        if (dimTransform != null)
            dimTransform.gameObject.SetActive(dimActive);
    }

    private Transform[] GetDirectChildSlots()
    {
        List<Transform> list = new List<Transform>();
        foreach (Transform child in transform)
        {
            list.Add(child);
        }
        return list.ToArray();
    }

    private Transform FindDeepChild(Transform parent, string childName)
    {
        foreach (Transform child in parent)
        {
            if (child.name == childName)
                return child;

            Transform found = FindDeepChild(child, childName);
            if (found != null)
                return found;
        }
        return null;
    }
}
