using UnityEngine;

public class RecoveryItemInventory : MonoBehaviour
{
    public const int MaxSlots = 4;

    public float protectionAmount = 10f;
    public float timeAddSeconds = 10f;

    public int selectedIndex = 0;

    private RecoveryItem.ItemType[] slots = new RecoveryItem.ItemType[MaxSlots];
    private bool[] slotFilled = new bool[MaxSlots];

    private RecoveryItemInventoryUI inventoryUI;

    void Awake()
    {
        inventoryUI = GetComponent<RecoveryItemInventoryUI>();
        if (inventoryUI == null)
            inventoryUI = FindAnyObjectByType<RecoveryItemInventoryUI>();
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
        for (int i = 0; i < MaxSlots; i++)
        {
            slots[i] = RecoveryItem.ItemType.Protection;
            slotFilled[i] = false;
        }

        selectedIndex = 0;
        RefreshUI();
    }

    public bool Add(RecoveryItem.ItemType type)
    {
        for (int i = 0; i < MaxSlots; i++)
        {
            if (slotFilled[i])
                continue;

            slots[i] = type;
            slotFilled[i] = true;
            RefreshUI();
            return true;
        }

        return false;
    }

    public void SelectPrev()
    {
        if (!HasAnyItem())
            return;

        int start = selectedIndex;
        for (int i = 0; i < MaxSlots; i++)
        {
            selectedIndex--;
            if (selectedIndex < 0)
                selectedIndex = MaxSlots - 1;

            if (slotFilled[selectedIndex])
            {
                RefreshUI();
                return;
            }
        }

        selectedIndex = start;
    }

    public void SelectNext()
    {
        if (!HasAnyItem())
            return;

        int start = selectedIndex;
        for (int i = 0; i < MaxSlots; i++)
        {
            selectedIndex++;
            if (selectedIndex >= MaxSlots)
                selectedIndex = 0;

            if (slotFilled[selectedIndex])
            {
                RefreshUI();
                return;
            }
        }

        selectedIndex = start;
    }

    public void UseSelected()
    {
        if (selectedIndex < 0 || selectedIndex >= MaxSlots)
            return;
        if (!slotFilled[selectedIndex])
            return;

        RecoveryItem.ItemType type = slots[selectedIndex];
        Player player = FindAnyObjectByType<Player>();
        Timer timer = FindAnyObjectByType<Timer>();

        if (type == RecoveryItem.ItemType.Protection)
        {
            if (player != null)
                player.AddProtection(protectionAmount);
            Debug.Log($"[RecoveryItemInventory] 방호복 회복 +{protectionAmount:F0}");
        }
        else if (type == RecoveryItem.ItemType.Time)
        {
            if (timer != null)
                timer.AddSeconds(timeAddSeconds);
            Debug.Log($"[RecoveryItemInventory] 시간 +{timeAddSeconds:F0}초");
        }

        slotFilled[selectedIndex] = false;
        RefreshUI();
    }

    public bool IsSlotFilled(int index)
    {
        if (index < 0 || index >= MaxSlots)
            return false;
        return slotFilled[index];
    }

    public RecoveryItem.ItemType GetSlotType(int index)
    {
        if (index < 0 || index >= MaxSlots)
            return RecoveryItem.ItemType.Protection;
        return slots[index];
    }

    public int GetSelectedIndex()
    {
        return selectedIndex;
    }

    private bool HasAnyItem()
    {
        for (int i = 0; i < MaxSlots; i++)
        {
            if (slotFilled[i])
                return true;
        }

        return false;
    }

    private void RefreshUI()
    {
        if (inventoryUI != null)
            inventoryUI.Refresh(this);
    }
}
