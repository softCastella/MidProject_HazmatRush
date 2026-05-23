using UnityEngine;

// PollutantSpawner는 오염물 생성 위치를 관리하는 클래스입니다.
// Manager가 이 스포너를 사용하면, spawnPoints 배열 중 하나를 랜덤으로 골라 해당 위치에 오염물을 생성합니다.
public class PollutantSpawner : MonoBehaviour
{
    // 실제 오염원이 생성될 위치들. 여러 개의 스폰 포인트를 등록할 수 있습니다.
    public Transform[] spawnPoints;

    // 이 스포너가 활성화된 상태인지 여부. 비활성화된 스포너는 생성 대상에서 제외됩니다.
    public bool isActive = true;

    // spawnPoints에 값이 있으면 그 중 하나를 랜덤으로 선택하여 위치를 반환합니다.
    // 값이 없으면 이 오브젝트의 Transform 위치를 대신 사용합니다.
    public Vector2 SpawnPosition
    {
        get
        {
            if (spawnPoints != null && spawnPoints.Length > 0)
                return spawnPoints[Random.Range(0, spawnPoints.Length)].position;
            return transform.position;
        }
    }

    // 실제로 프리팹을 생성하는 메서드
    public GameObject Spawn(GameObject prefab)
    {
        // 할당된 생성할 오브젝트가 없으면 오류를 출력하고 종료
        if (prefab == null)
        {
            Debug.LogError("PollutantSpawner: Spawn할 prefab이 없습니다.");
            return null;
        }

        // 선택된 위치에서 오염물 프리팹을 생성합니다.
        return Instantiate(prefab, SpawnPosition, Quaternion.identity);
    }
}
