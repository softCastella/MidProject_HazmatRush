using UnityEngine;

public class PollutantSpawner : MonoBehaviour
{
    public Transform spawnPoint;
    public bool isActive = true;

    public Vector2 SpawnPosition
    {
        get
        {
            if (spawnPoint != null)
                return spawnPoint.position;
            return transform.position;
        }
    }

    public void Spawn(GameObject prefab)
    {
        if (prefab == null)
        {
            Debug.LogError("PollutantSpawner: Spawn할 prefab이 없습니다.");
            return;
        }

        Instantiate(prefab, SpawnPosition, Quaternion.identity);
    }
}
