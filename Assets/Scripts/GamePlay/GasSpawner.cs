using UnityEngine;

// GasSpawner는 TypeD(가스) 오염원이 생성될 위치만 관리합니다.
public class GasSpawner : MonoBehaviour
{
    public Transform[] spawnPoints;
    public bool isActive = true;

    public Vector2 SpawnPosition
    {
        get
        {
            if (spawnPoints != null && spawnPoints.Length > 0)
                return spawnPoints[Random.Range(0, spawnPoints.Length)].position;
            return transform.position;
        }
    }

    public GameObject Spawn(GameObject prefab)
    {
        if (prefab == null)
        {
            Debug.LogError("GasSpawner: Spawn할 prefab이 없습니다.");
            return null;
        }

        return Instantiate(prefab, SpawnPosition, Quaternion.identity);
    }
}
