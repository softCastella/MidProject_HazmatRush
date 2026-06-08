using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RecoveryItemInventoryUI : MonoBehaviour
{
    public Transform[] slotRoots;
    public Transform contentRoot;
    public GameObject invEmptySlotPrefab;
    public GameObject invProtectRecovPrefab;
    public GameObject invTimeRecovPrefab;

    public string countChildName = "Count";
    public string dimChildName = "dim";

    private const string InvItemViewName = "InvItemView";
    private const float InvImagePosY = 12.4f;
    private const float InvImageSize = 44f;
    private const float InvImageSizeTime = 66f;
    private const float InvNamePosY = -31.7f;

    void Awake()
    {
        if (contentRoot == null)
            contentRoot = FindContentRoot();

        if (slotRoots == null || slotRoots.Length == 0)
            slotRoots = GetContentChildSlots();
    }

    public int SlotRootCount
    {
        get
        {
            if (slotRoots == null)
                return 0;
            return slotRoots.Length;
        }
    }

    public void EnsureSlotRootCount(int needed)
    {
        if (needed <= SlotRootCount)
            return;
        if (contentRoot == null || invEmptySlotPrefab == null)
            return;

        while (SlotRootCount < needed)
        {
            GameObject slotObj = Instantiate(invEmptySlotPrefab, contentRoot);
            slotObj.name = "slot" + SlotRootCount;
            AppendSlotRoot(slotObj.transform);
        }
    }

    public void Refresh(RecoveryItemInventory inventory)
    {
        if (inventory == null || slotRoots == null)
            return;

        int occupied = inventory.GetOccupiedSlotCount();

        for (int i = 0; i < slotRoots.Length; i++)
        {
            if (slotRoots[i] == null)
                continue;

            bool filled = i < occupied;
            int itemId = filled ? inventory.GetItemIdAt(i) : 0;
            int itemCount = filled ? inventory.GetItemCountAt(i) : 0;

            UpdateInvItemView(slotRoots[i], itemId, inventory);

            TMP_Text countText = FindChildText(slotRoots[i], countChildName);
            if (countText != null)
            {
                if (filled && itemCount > 0)
                {
                    countText.gameObject.SetActive(true);
                    countText.text = itemCount.ToString();
                }
                else
                {
                    countText.text = "";
                    countText.gameObject.SetActive(false);
                }
            }

            Transform dimMark = FindDimChild(slotRoots[i]);
            if (dimMark != null)
            {
                bool isSelected = filled && inventory.GetSelectedIndex() == i;
                dimMark.gameObject.SetActive(!isSelected);
            }
        }
    }

    private void UpdateInvItemView(Transform slot, int itemId, RecoveryItemInventory inventory)
    {
        ClearInvItemView(slot);
        if (itemId <= 0)
            return;

        GameObject prefab = GetInvPrefab(itemId);
        if (prefab == null)
            return;

        GameObject view = Instantiate(prefab, slot);
        view.name = InvItemViewName;
        view.SetActive(true);

        RectTransform rt = view.GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.localScale = Vector3.one;
        }

        RecoveryItemRow def = inventory.GetItemDef(itemId);
        ApplyInvItemViewLayout(view.transform, def);
    }

    private void ApplyInvItemViewLayout(Transform view, RecoveryItemRow def)
    {
        Transform image = view.Find("Image");
        if (image != null)
        {
            image.gameObject.SetActive(true);
            image.localRotation = Quaternion.identity;
            image.localScale = Vector3.one;

            RectTransform imageRt = image.GetComponent<RectTransform>();
            if (imageRt != null)
            {
                imageRt.anchorMin = new Vector2(0.5f, 0.5f);
                imageRt.anchorMax = new Vector2(0.5f, 0.5f);
                imageRt.pivot = new Vector2(0.5f, 0.5f);
                imageRt.anchoredPosition = new Vector2(0f, InvImagePosY);
                float imageSize = def != null && def.id == 2 ? InvImageSizeTime : InvImageSize;
                imageRt.sizeDelta = new Vector2(imageSize, imageSize);
            }

            Image imageComp = image.GetComponent<Image>();
            if (imageComp != null)
                imageComp.preserveAspect = true;
        }

        Transform name = view.Find("Name");
        if (name != null)
        {
            name.gameObject.SetActive(true);
            name.localRotation = Quaternion.identity;
            name.localScale = Vector3.one;

            RectTransform nameRt = name.GetComponent<RectTransform>();
            if (nameRt != null)
            {
                nameRt.anchorMin = new Vector2(0.5f, 0.5f);
                nameRt.anchorMax = new Vector2(0.5f, 0.5f);
                nameRt.pivot = new Vector2(0.5f, 0.5f);
                nameRt.anchoredPosition = new Vector2(0f, InvNamePosY);
                nameRt.sizeDelta = new Vector2(80f, 30f);
            }

            TMP_Text nameText = name.GetComponent<TMP_Text>();
            if (nameText != null && def != null && !string.IsNullOrEmpty(def.displayName))
            {
                nameText.text = def.displayName.Replace(" ", "\n");
                nameText.lineSpacing = 0f;
            }
        }
    }

    private GameObject GetInvPrefab(int itemId)
    {
        if (itemId == 2)
            return invTimeRecovPrefab;
        return invProtectRecovPrefab;
    }

    private void ClearInvItemView(Transform slot)
    {
        Transform old = slot.Find(InvItemViewName);
        if (old != null)
            Destroy(old.gameObject);
    }

    private void AppendSlotRoot(Transform slot)
    {
        if (slotRoots == null)
        {
            slotRoots = new Transform[1];
            slotRoots[0] = slot;
            return;
        }

        Transform[] next = new Transform[slotRoots.Length + 1];
        for (int i = 0; i < slotRoots.Length; i++)
            next[i] = slotRoots[i];
        next[slotRoots.Length] = slot;
        slotRoots = next;
    }

    private Transform FindContentRoot()
    {
        Transform scroll = transform.Find("Scroll View");
        if (scroll == null)
            return null;

        Transform viewport = scroll.Find("Viewport");
        if (viewport == null)
            return null;

        return viewport.Find("Content");
    }

    private Transform[] GetContentChildSlots()
    {
        if (contentRoot == null)
            return new Transform[0];

        Transform[] result = new Transform[contentRoot.childCount];
        for (int i = 0; i < contentRoot.childCount; i++)
            result[i] = contentRoot.GetChild(i);
        return result;
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

    private TMP_Text FindChildText(Transform root, string childName)
    {
        Transform child = root.Find(childName);
        if (child != null)
            return child.GetComponent<TMP_Text>();

        return null;
    }
}
