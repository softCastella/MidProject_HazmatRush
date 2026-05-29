using UnityEngine;

public class ItemSelectManager : MonoBehaviour
{
    public GameObject[] itemPrefabs;
    public ItemManager itemManager;
    public Item.ItemType[] itemTypes;
    public int selectedIndex = 0;

    void Awake()
    {
        if (itemManager == null)
            itemManager = FindAnyObjectByType<ItemManager>();

        if (itemTypes == null || itemTypes.Length == 0)
            itemTypes = new[] { Item.ItemType.Scanner, Item.ItemType.Neutralizer, Item.ItemType.GeneralPad, Item.ItemType.OilPad };
    }

    void Start()
    {
        UpdateUI();
    }

    void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsPaused)
            return;

        if (Input.GetKeyDown(KeyCode.Z))
        {
            SelectNextItem();
        }
    }

    public void SelectNextItem()
    {
        if (itemPrefabs == null || itemPrefabs.Length == 0)
        {
            Debug.LogWarning("ItemSelectManager: itemPrefabs가 비어 있어 아이템 변경을 수행할 수 없습니다.");
            return;
        }

        selectedIndex = (selectedIndex + 1) % itemPrefabs.Length;
        UpdateUI();
        Debug.Log($"선택된 아이템: {GetSelectedItemName()} (index={selectedIndex}, type={SelectedItemType})");
    }

    public void SetSelectedIndex(int index)
    {
        if (itemTypes == null || itemTypes.Length == 0)
            return;

        selectedIndex = Mathf.Clamp(index, 0, itemTypes.Length - 1);
        UpdateUI();
        Debug.Log($"추천 아이템 위치로 선택 변경: {GetSelectedItemName()}");
    }

    public Item.ItemType SelectedItemType
    {
        get
        {
            if (itemTypes != null && selectedIndex >= 0 && selectedIndex < itemTypes.Length)
                return itemTypes[selectedIndex];

            var item = GetSelectedItem();
            if (item != null)
                return InferTypeFromName(item.name);

            return Item.ItemType.Scanner;
        }
    }

    public bool IsSelectedItemRecommendedFor(Pollutant pollutant)
    {
        return pollutant != null && SelectedItemType == pollutant.RecommendedItemType;
    }

    private Item.ItemType InferTypeFromName(string itemName)
    {
        if (string.IsNullOrEmpty(itemName))
            return Item.ItemType.Scanner;

        string lower = itemName.ToLower();
        if (lower.Contains("bottle") || lower.Contains("neutral"))
            return Item.ItemType.Neutralizer;
        if (lower.Contains("yellow") || lower.Contains("general") || lower.Contains("pad"))
            return Item.ItemType.GeneralPad;
        if (lower.Contains("blue") || lower.Contains("oil") || lower.Contains("absorb"))
            return Item.ItemType.OilPad;
        return Item.ItemType.Scanner;
    }

    public string GetSelectedItemName()
    {
        var item = GetSelectedItem();
        return item != null ? item.name : "None";
    }

    public GameObject GetSelectedItem()
    {
        if (itemPrefabs == null || itemPrefabs.Length == 0)
            return null;

        if (selectedIndex < 0 || selectedIndex >= itemPrefabs.Length)
            selectedIndex = 0;

        return itemPrefabs[selectedIndex];
    }

    private void UpdateUI()
    {
        if (itemManager != null)
            itemManager.SetSelected(selectedIndex);
    }
}
