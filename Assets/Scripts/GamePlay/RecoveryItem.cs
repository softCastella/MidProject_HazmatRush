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

    public bool IsLandedOnMap => !dropInProgress;

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
        Vector3 endPos = spawnPos + new Vector3(landOffsetX, 0f, 0f);
        Debug.Log($"[RecoveryItem] 아크 시작 — {name} id={itemId}, 시작={spawnPos}, 착지={endPos}, jumpHeight={jumpHeight:F2}, duration={duration:F2}s");
        StartCoroutine(DropArcRoutine(spawnPos, jumpHeight, landOffsetX, duration));
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (dropInProgress)
            return;
        if (!other.CompareTag("Player"))
            return;

        TryCollectToInventory(false);
    }

    // 클리어 직전 맵에 남은 아이템 자동 획득 (아크 중이어도 인벤으로)
    public bool ForceCollectToInventory()
    {
        StopAllCoroutines();
        bool wasLanded = IsLandedOnMap;
        dropInProgress = false;
        if (pickupCollider != null)
            pickupCollider.enabled = false;

        return TryCollectToInventory(wasLanded, true);
    }

    private bool TryCollectToInventory(bool wasLandedOnMap, bool autoCollect = false)
    {
        RecoveryItemInventory inventory = FindAnyObjectByType<RecoveryItemInventory>();
        if (inventory == null)
        {
            Debug.LogWarning("[RecoveryItem] RecoveryItemInventory를 찾을 수 없습니다.");
            return false;
        }

        int id = itemId;
        if (id <= 0)
            id = type == ItemType.Time ? 2 : 1;

        if (!inventory.Add(id))
            return false;

        RecoveryItemManager manager = FindAnyObjectByType<RecoveryItemManager>();
        if (manager != null && wasLandedOnMap)
            manager.OnMapRecoveryItemRemoved();

        if (autoCollect)
            Debug.Log($"[RecoveryItem] 클리어 전 자동 획득: id={id}");
        else
            Debug.Log($"[RecoveryItem] 획득: id={id}");

        Destroy(gameObject);
        return true;
    }

    private IEnumerator DropArcRoutine(Vector3 start, float jumpHeight, float offsetX, float duration)
    {
        Vector3 end = start + new Vector3(offsetX, 0f, 0f);
        transform.position = start;

        if (duration <= 0f)
        {
            transform.position = end;
            Debug.Log($"[RecoveryItem] 아크 생략(duration<=0) — 즉시 착지 {end}");
            FinishDrop();
            yield break;
        }

        float time = 0f;
        bool loggedPeak = false;
        while (time < duration)
        {
            time += Time.deltaTime;
            float t = Mathf.Clamp01(time / duration);
            float x = Mathf.Lerp(start.x, end.x, t);
            float arc = 4f * jumpHeight * t * (1f - t);
            transform.position = new Vector3(x, start.y + arc, start.z);

            if (!loggedPeak && t >= 0.5f)
            {
                loggedPeak = true;
                Debug.Log($"[RecoveryItem] 아크 정점 — pos={transform.position}, peakY={start.y + jumpHeight:F2}");
            }

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

        RecoveryItemManager manager = FindAnyObjectByType<RecoveryItemManager>();
        if (manager != null)
            manager.OnMapRecoveryItemLanded();

        Debug.Log($"[RecoveryItem] 아크 착지 완료 — pos={transform.position}, 픽업 콜라이더={(pickupCollider != null && pickupCollider.enabled ? "ON" : "OFF")}");
    }
}
