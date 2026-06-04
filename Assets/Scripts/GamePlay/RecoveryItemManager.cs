using UnityEngine;

public class RecoveryItemManager : MonoBehaviour
{
    public RecoveryItemSpawner spawner;
    public GameObject protectionItemPrefab;
    public GameObject timeItemPrefab;

    public int spawnMinCount = 1;
    public int spawnMaxCount = 3;

    private void Start()
    {
        if (spawner == null)
            spawner = FindAnyObjectByType<RecoveryItemSpawner>();

        SpawnFromPlan();
    }

    public void ResetForStage()
    {
        RecoveryItem[] items = FindObjectsByType<RecoveryItem>(FindObjectsSortMode.None);
        for (int i = 0; i < items.Length; i++)
        {
            if (items[i] != null)
                Destroy(items[i].gameObject);
        }

        RecoveryItemInventory inventory = FindAnyObjectByType<RecoveryItemInventory>();
        if (inventory != null)
            inventory.ResetInventory();

        SpawnFromPlan();
    }

    private void SpawnFromPlan()
    {
        if (spawner == null)
            return;

        if (RecoveryItemSpawnPlan.entries == null || RecoveryItemSpawnPlan.entries.Length == 0)
        {
            RecoveryItemSpawnPlan.PrepareRandom(spawner.PointCount, spawnMinCount, spawnMaxCount);
        }

        if (RecoveryItemSpawnPlan.entries == null)
            return;

        for (int i = 0; i < RecoveryItemSpawnPlan.entries.Length; i++)
        {
            RecoveryItemSpawnPlan.Entry entry = RecoveryItemSpawnPlan.entries[i];
            GameObject prefab = GetPrefab(entry.type);
            if (prefab == null)
                continue;

            spawner.Spawn(prefab, entry.pointIndex);
        }
    }

    private GameObject GetPrefab(RecoveryItem.ItemType type)
    {
        if (type == RecoveryItem.ItemType.Time)
            return timeItemPrefab;
        return protectionItemPrefab;
    }
}
