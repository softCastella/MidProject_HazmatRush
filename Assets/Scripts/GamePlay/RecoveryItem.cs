using System.Collections;
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

    private Collider2D pickupCollider;
    private bool dropInProgress;

    void Awake()
    {
        pickupCollider = GetComponent<Collider2D>();
    }

    // spawnPos에서 위로 튀었다 좌/우(offsetX 부호)로 착지
    public void StartDropArc(Vector3 spawnPos, float jumpHeight, float landOffsetX, float duration)
    {
        if (pickupCollider != null)
            pickupCollider.enabled = false;

        dropInProgress = true;
        StartCoroutine(DropArcRoutine(spawnPos, jumpHeight, landOffsetX, duration));
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (dropInProgress)
            return;
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

    private IEnumerator DropArcRoutine(Vector3 start, float jumpHeight, float offsetX, float duration)
    {
        Vector3 end = start + new Vector3(offsetX, 0f, 0f);
        transform.position = start;

        if (duration <= 0f)
        {
            transform.position = end;
            FinishDrop();
            yield break;
        }

        float time = 0f;
        while (time < duration)
        {
            time += Time.deltaTime;
            float t = Mathf.Clamp01(time / duration);
            float x = Mathf.Lerp(start.x, end.x, t);
            float arc = 4f * jumpHeight * t * (1f - t);
            transform.position = new Vector3(x, start.y + arc, start.z);
            yield return null;
        }

        transform.position = end;
        FinishDrop();
    }

    private void FinishDrop()
    {
        dropInProgress = false;
        if (pickupCollider != null)
            pickupCollider.enabled = true;
    }
}
