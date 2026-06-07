using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

[System.Serializable]
public class RecoveryItemRow
{
    public int id;
    public string type;
    public string displayName;
    public string effect;
    public float value;
    public string invPrefab;
    public string mapPrefab;
}

[System.Serializable]
public class RecoveryInvSlot
{
    public int id;
    public int count;
}

public class RecoveryItemInventory : MonoBehaviour
{
    public const int BaseVisibleSlots = 4;

    public TextAsset recoveryItemsJson;
    public int selectedIndex = 0;

    private readonly List<RecoveryInvSlot> slots = new List<RecoveryInvSlot>();
    private RecoveryItemRow[] itemDefs;
    private RecoveryItemInventoryUI inventoryUI;

    void Awake()
    {
        inventoryUI = GetComponent<RecoveryItemInventoryUI>();
        if (inventoryUI == null)
            inventoryUI = FindAnyObjectByType<RecoveryItemInventoryUI>();

        LoadItemDefs();
        EnsureFallbackDefs();
    }

    void Start()
    {
        RefreshUI();
    }

    void Update()
    {
        if (GameManager.Instance != null && (GameManager.Instance.IsPaused || GameManager.Instance.GameEnded || GameManager.Instance.IsPenalty))
            return;

        if (Input.GetKeyDown(KeyCode.C))
            SelectPrev();
        else if (Input.GetKeyDown(KeyCode.V))
            SelectNext();
        else if (Input.GetKeyDown(KeyCode.Space))
            UseSelected();
    }

    public void ResetInventory()
    {
        slots.Clear();
        selectedIndex = 0;
        RefreshUI();
    }

    public bool Add(int itemId)
    {
        if (itemId <= 0)
            return false;

        if (GetItemDef(itemId) == null)
            Debug.LogWarning($"[RecoveryItemInventory] JSON에 id={itemId} 없음 — 획득은 진행");

        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i].id != itemId)
                continue;

            slots[i].count++;
            selectedIndex = i;
            RefreshUI();
            Debug.Log($"[RecoveryItemInventory] 획득(스택) id={itemId}, count={slots[i].count}");
            return true;
        }

        if (inventoryUI != null)
            inventoryUI.EnsureSlotRootCount(slots.Count + 1);

        if (inventoryUI != null && slots.Count >= inventoryUI.SlotRootCount)
        {
            Debug.Log("[RecoveryItemInventory] 빈 칸이 없습니다.");
            return false;
        }

        RecoveryInvSlot entry = new RecoveryInvSlot();
        entry.id = itemId;
        entry.count = 1;
        slots.Add(entry);
        selectedIndex = slots.Count - 1;
        RefreshUI();
        Debug.Log($"[RecoveryItemInventory] 획득(신규) id={itemId}, 칸={selectedIndex}");
        return true;
    }

    public bool Add(RecoveryItem.ItemType type)
    {
        int itemId = type == RecoveryItem.ItemType.Time ? 2 : 1;
        return Add(itemId);
    }

    public void SelectPrev()
    {
        if (slots.Count == 0)
            return;

        selectedIndex--;
        if (selectedIndex < 0)
            selectedIndex = slots.Count - 1;

        RefreshUI();
    }

    public void SelectNext()
    {
        if (slots.Count == 0)
            return;

        selectedIndex++;
        if (selectedIndex >= slots.Count)
            selectedIndex = 0;

        RefreshUI();
    }

    public void UseSelected()
    {
        if (selectedIndex < 0 || selectedIndex >= slots.Count)
            return;

        RecoveryItemRow def = GetItemDef(slots[selectedIndex].id);
        if (def == null)
            return;

        ApplyEffect(def);

        slots[selectedIndex].count--;
        if (slots[selectedIndex].count <= 0)
            slots.RemoveAt(selectedIndex);

        if (slots.Count == 0)
            selectedIndex = 0;
        else if (selectedIndex >= slots.Count)
            selectedIndex = slots.Count - 1;

        RefreshUI();
    }

    public int GetOccupiedSlotCount()
    {
        return slots.Count;
    }

    public int GetItemIdAt(int index)
    {
        if (index < 0 || index >= slots.Count)
            return 0;
        return slots[index].id;
    }

    public int GetItemCountAt(int index)
    {
        if (index < 0 || index >= slots.Count)
            return 0;
        return slots[index].count;
    }

    public bool IsSlotFilled(int index)
    {
        return index >= 0 && index < slots.Count;
    }

    public int GetSelectedIndex()
    {
        return selectedIndex;
    }

    public RecoveryItemRow GetItemDef(int itemId)
    {
        if (itemDefs == null)
            return null;

        for (int i = 0; i < itemDefs.Length; i++)
        {
            if (itemDefs[i] != null && itemDefs[i].id == itemId)
                return itemDefs[i];
        }

        return null;
    }

    private void LoadItemDefs()
    {
        if (recoveryItemsJson == null || string.IsNullOrEmpty(recoveryItemsJson.text))
        {
            Debug.LogWarning("[RecoveryItemInventory] recovery_items.json 미연결 — 기본 정의 사용");
            return;
        }

        List<RecoveryItemRow> rows = JsonConvert.DeserializeObject<List<RecoveryItemRow>>(recoveryItemsJson.text);
        if (rows == null || rows.Count == 0)
        {
            Debug.LogWarning("[RecoveryItemInventory] JSON 파싱 결과가 비어 있습니다.");
            return;
        }

        itemDefs = new RecoveryItemRow[rows.Count];
        for (int i = 0; i < rows.Count; i++)
            itemDefs[i] = rows[i];

        Debug.Log($"[RecoveryItemInventory] JSON 아이템 {itemDefs.Length}종 로드");
    }

    private void EnsureFallbackDefs()
    {
        if (itemDefs != null && itemDefs.Length > 0)
            return;

        itemDefs = new RecoveryItemRow[2];
        itemDefs[0] = new RecoveryItemRow();
        itemDefs[0].id = 1;
        itemDefs[0].effect = "protection";
        itemDefs[0].value = 10f;
        itemDefs[0].displayName = "방호복 회복제";

        itemDefs[1] = new RecoveryItemRow();
        itemDefs[1].id = 2;
        itemDefs[1].effect = "time";
        itemDefs[1].value = 10f;
        itemDefs[1].displayName = "시간 연장기";
    }

    private void ApplyEffect(RecoveryItemRow def)
    {
        if (def == null)
            return;

        if (def.effect == "time")
        {
            Timer timer = FindAnyObjectByType<Timer>();
            if (timer != null)
                timer.AddSeconds(def.value);
            Debug.Log($"[RecoveryItemInventory] 시간 +{def.value:F0}초");
            return;
        }

        Player player = FindAnyObjectByType<Player>();
        if (player != null)
            player.AddProtection(def.value);
        Debug.Log($"[RecoveryItemInventory] 방호복 회복 +{def.value:F0}");
    }

    private void RefreshUI()
    {
        if (inventoryUI != null)
            inventoryUI.Refresh(this);
    }
}
