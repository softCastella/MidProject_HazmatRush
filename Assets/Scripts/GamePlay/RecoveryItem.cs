using UnityEngine;

public class RecoveryItem : MonoBehaviour
{
    public enum ItemType
    {
        Protection,
        Time
    }

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

        if (!inventory.Add(type))
        {
            Debug.Log("[RecoveryItem] 인벤토리가 가득 찼습니다.");
            return;
        }

        Debug.Log($"[RecoveryItem] 획득: {type}");
        Destroy(gameObject);
    }
}
