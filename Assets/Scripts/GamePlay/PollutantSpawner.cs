using UnityEngine;

// PollutantSpawner는 오염물 생성 위치를 관리하는 클래스입니다.
// Manager가 이 스포너를 사용하면, spawnPoints 배열 중 하나를 랜덤으로 골라 해당 위치에 오염물을 생성합니다.
public class PollutantSpawner : MonoBehaviour
{
    // 실제 오염원이 생성될 위치들. 여러 개의 스폰 포인트를 등록할 수 있습니다.
    public Transform[] spawnPoints;

    // A/B/C용 spawnPoints 개수 (인덱스 0 ~ normalSpawnPointCount-1, 기본 3 = 0,1,2)
    public int normalSpawnPointCount = 3;

    // TypeD(가스) 전용 spawnPoints 인덱스 (기본 3,4,5,6)
    public int[] typeDSpawnIndices = { 3, 4, 5, 6 };

    // 이 스포너가 활성화된 상태인지 여부. 비활성화된 스포너는 생성 대상에서 제외됩니다.
    public bool isActive = true;

    public Vector2 GetSpawnPosition(bool forTypeD)
    {
        if (forTypeD)
            return GetSpawnPositionAt(true, Random.Range(0, typeDSpawnIndices.Length));
        return GetSpawnPositionAt(false, Random.Range(0, Mathf.Min(normalSpawnPointCount, spawnPoints.Length)));
    }

    public Vector2 GetSpawnPositionAt(bool forTypeD, int pointIndex)
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
            return transform.position;

        if (forTypeD)
            return PickFromIndicesAt(typeDSpawnIndices, pointIndex);

        int count = Mathf.Min(normalSpawnPointCount, spawnPoints.Length);
        if (count <= 0)
            return transform.position;

        int pick = Mathf.Clamp(pointIndex, 0, count - 1);
        if (spawnPoints[pick] != null)
            return spawnPoints[pick].position;

        return transform.position;
    }

    private Vector2 PickFromIndices(int[] indices)
    {
        if (indices == null || indices.Length == 0)
            return transform.position;
        return PickFromIndicesAt(indices, Random.Range(0, indices.Length));
    }

    private Vector2 PickFromIndicesAt(int[] indices, int slotIndex)
    {
        if (indices == null || indices.Length == 0)
            return transform.position;

        int slot = Mathf.Clamp(slotIndex, 0, indices.Length - 1);
        int pick = indices[slot];
        if (pick >= 0 && pick < spawnPoints.Length && spawnPoints[pick] != null)
            return spawnPoints[pick].position;

        return transform.position;
    }

    // 실제로 프리팹을 생성하는 메서드 (forTypeD=true면 typeDSpawnIndices 사용)
    public GameObject Spawn(GameObject prefab, bool forTypeD = false)
    {
        return SpawnAt(prefab, forTypeD, 0);
    }

    public GameObject SpawnAt(GameObject prefab, bool forTypeD, int pointIndex)
    {
        if (prefab == null)
        {
            Debug.LogError("PollutantSpawner: Spawn할 prefab이 없습니다.");
            return null;
        }

        return Instantiate(prefab, GetSpawnPositionAt(forTypeD, pointIndex), Quaternion.identity);
    }
}
