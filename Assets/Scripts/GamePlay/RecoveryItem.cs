using UnityEngine;

public class RecoveryItem : MonoBehaviour
{
    public enum ItemType
    {
        Protection,
        Time
    }

    public int itemId;
    public ItemType type = ItemType.Protection;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        RecoveryItemInventory inventory = FindAnyObjectByType<RecoveryItemInventory>();
        if (inventory == null)
        {
            Debug.LogWarning("[RecoveryItem] RecoveryItemInventory를 찾을 수 없습니다.");
            return;
        }

        int id = itemId;
        if (id <= 0)
            id = type == ItemType.Time ? 2 : 1;

        if (!inventory.Add(id))
            return;

        Debug.Log($"[RecoveryItem] 획득: id={id}");
        Destroy(gameObject);
    }
}
