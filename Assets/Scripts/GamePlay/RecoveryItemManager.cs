using UnityEngine;

public class RecoveryItemManager : MonoBehaviour
{
    public GameObject protectionItemPrefab;
    public GameObject timeItemPrefab;

    [Header("오염원 정화 드랍 — 1단계: 발생 확률(%)")]
    public float dropChanceTypeA = 30f;
    public float dropChanceTypeB = 20f;
    public float dropChanceTypeC = 40f;
    public float dropChanceTypeD = 20f;

    [Header("오염원 정화 드랍 — 2단계: 방호복 회복제 확률(%, 나머지=시간 연장기)")]
    public float dropProtectionChance = 70f;

    [Header("드랍 연출 — 위로 튀었다 좌/우 착지")]
    [Tooltip("오염원 위치 기준 드랍·착지 Y 보정 (스프라이트가 땅에 묻히면 올림)")]
    public float dropYOffset = 30f;
    public float dropJumpHeight = 240f;
    public float dropLandOffsetX = 160f;
    public float dropDuration = 0.6f;

    [Header("테스트 (빌드 전 OFF)")]
    [Tooltip("체크 시 오염원 정화 확률 무시하고 무조건 드랍")]
    public bool testAlwaysDrop = false;

    private int landedMapItemCount = 0;

    public bool HasLandedRecoveryItemsOnMap()
    {
        return landedMapItemCount > 0;
    }

    public void OnMapRecoveryItemLanded()
    {
        landedMapItemCount++;
    }

    public void OnMapRecoveryItemRemoved()
    {
        landedMapItemCount = Mathf.Max(0, landedMapItemCount - 1);
    }

    // 클리어 직전 맵 회복 아이템을 인벤에 자동 추가 (인벤 가득 차면 남은 것은 제외)
    public int AutoCollectMapRecoveryItems()
    {
        RecoveryItem[] items = FindObjectsByType<RecoveryItem>(FindObjectsSortMode.None);
        int collected = 0;

        for (int i = 0; i < items.Length; i++)
        {
            if (items[i] == null)
                continue;

            if (items[i].ForceCollectToInventory())
                collected++;
        }

        landedMapItemCount = 0;
        if (collected > 0)
            Debug.Log($"[RecoveryItemManager] 클리어 전 자동 획득 {collected}개");

        return collected;
    }

    public void ClearMapRecoveryItems()
    {
        RecoveryItem[] items = FindObjectsByType<RecoveryItem>(FindObjectsSortMode.None);
        for (int i = 0; i < items.Length; i++)
        {
            if (items[i] != null)
                Destroy(items[i].gameObject);
        }

        landedMapItemCount = 0;
    }

    public void ResetForStage()
    {
        ClearMapRecoveryItems();

        RecoveryItemInventory inventory = FindAnyObjectByType<RecoveryItemInventory>();
        if (inventory != null)
            inventory.ResetInventory();
    }

    // 오염원 중화 성공 시 그 위치에 회복 아이템 생성 (플레이어 접촉 → 인벤 추가)
    public void TryDropOnPollutantCleared(Pollutant.PollutantType pollutantType, Vector3 position)
    {
        float dropChance = GetDropChance(pollutantType);
        if (!testAlwaysDrop && Random.Range(0f, 100f) >= dropChance)
            return;

        RecoveryItem.ItemType itemType = Random.Range(0f, 100f) < dropProtectionChance
            ? RecoveryItem.ItemType.Protection
            : RecoveryItem.ItemType.Time;

        GameObject prefab = GetPrefab(itemType);
        if (prefab == null)
        {
            Debug.LogWarning("[RecoveryItemManager] 드랍 프리팹이 비어 있습니다.");
            return;
        }

        float landSide = Random.value < 0.5f ? -1f : 1f;
        float landX = dropLandOffsetX * landSide;
        string landDir = landSide < 0f ? "좌" : "우";

        Vector3 spawnPos = position;
        spawnPos.y += dropYOffset;

        GameObject obj = Instantiate(prefab, spawnPos, Quaternion.identity);
        RecoveryItem item = obj.GetComponent<RecoveryItem>();
        if (item != null)
        {
            item.type = itemType;
            item.itemId = itemType == RecoveryItem.ItemType.Time ? 2 : 1;
            item.StartDropArc(spawnPos, dropJumpHeight, landX, dropDuration);
        }

        string itemName = itemType == RecoveryItem.ItemType.Time ? "시간 연장기" : "방호복 회복제";
        Debug.Log($"[RecoveryItemManager] 드랍: {itemName} (오염원 {pollutantType}, 확률 {dropChance:F0}%, 착지 {landDir})");
    }

    private float GetDropChance(Pollutant.PollutantType pollutantType)
    {
        switch (pollutantType)
        {
            case Pollutant.PollutantType.TypeB:
                return dropChanceTypeB;
            case Pollutant.PollutantType.TypeC:
                return dropChanceTypeC;
            case Pollutant.PollutantType.TypeD:
                return dropChanceTypeD;
            default:
                return dropChanceTypeA;
        }
    }

    private GameObject GetPrefab(RecoveryItem.ItemType type)
    {
        if (type == RecoveryItem.ItemType.Time)
            return timeItemPrefab;
        return protectionItemPrefab;
    }
}
