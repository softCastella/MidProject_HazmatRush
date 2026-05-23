using UnityEngine;

// PollutantManager는 오염원 생성만 담당하는 간단한 클래스입니다.
public class PollutantManager : MonoBehaviour
{
    public Player player;
    public GameObject[] pollutants; // 등록된 오염원 프리팹 목록
    public PollutantSpawner spawner;
    public Background scroll;
    public float rangeBuffer = 0.5f;
    public Vector2 timeRange = new Vector2(2f, 3f);

    // 현재 누적된 이동 시간. Player가 이동 중일 때만 시간 누적을 합니다.
    private float moveTime = 0f;

    // 다음 생성 시점까지 필요한 시간
    private float nextSpawnTime;

    void Awake()
    {
        // Player를 Inspector에 할당하지 않았다면 씬에서 자동으로 검색합니다.
        if (player == null)
            player = FindObjectOfType<Player>();

        if (spawner == null)
            spawner = FindObjectOfType<PollutantSpawner>();

        if (scroll == null)
            scroll = FindObjectOfType<Background>();

        nextSpawnTime = Random.Range(timeRange.x, timeRange.y);
    }

    void Update()
    {
        // Player가 없으면 생성 로직을 실행하지 않습니다.
        if (player == null)
            return;

        // Player가 이동 중일 때만 누적 시간을 증가시킵니다.
        if (player.isMoving)
        {
            moveTime += Time.deltaTime;
        }
        else
        {
            // Player가 멈추면 누적 시간을 초기화합니다.
            moveTime = 0f;
        }

        // 누적 시간이 다음 생성 시점을 넘어섰으면 새로운 오염원을 생성합니다.
        if (moveTime >= nextSpawnTime)
        {
            SpawnPollutant();
            moveTime = 0f;
            nextSpawnTime = Random.Range(timeRange.x, timeRange.y);
        }

        // 오염원이 사라지면 배경을 다시 움직이도록 합니다.
        if (scroll != null && Pollutant.activeCount == 0)
            scroll.ResumeScroll();
    }

    // 실제로 오염원을 생성하는 함수
    private void SpawnPollutant()
    {
        if (Pollutant.activeCount > 0)
            return;

        if (pollutants == null || pollutants.Length == 0)
        {
            Debug.LogError("PollutantManager: 오염원 프리팹을 등록하세요.");
            return;
        }

        GameObject selectedPrefab = pollutants[Random.Range(0, pollutants.Length)];
        if (selectedPrefab == null)
        {
            Debug.LogError("PollutantManager: 등록된 오염원 프리팹 중 하나가 비어있습니다.");
            return;
        }

        if (spawner == null)
        {
            Debug.LogWarning("PollutantManager: PollutantSpawner가 할당되지 않았습니다.");
            return;
        }

        if (!spawner.isActive)
        {
            Debug.LogWarning("PollutantManager: 할당된 스포너가 비활성화되어 있습니다.");
            return;
        }

        GameObject created = spawner.Spawn(selectedPrefab);
        if (created != null)
        {
            if (scroll != null)
                scroll.PauseScroll();

            if (player != null)
            {
                float targetX = GetEdgeX(created);
                player.GrowRange(targetX, rangeBuffer);
            }

            Debug.Log($"PollutantManager: {selectedPrefab.name}이 생성되었습니다.");
        }
    }

    private float GetEdgeX(GameObject obj)
    {
        if (obj == null || player == null)
            return player != null ? player.transform.position.x : 0f;

        float centerX = obj.transform.position.x;
        float halfWidth = 0.5f;
        Renderer rend = obj.GetComponent<Renderer>();
        if (rend != null)
            halfWidth = rend.bounds.extents.x;

        float playerX = player.transform.position.x;
        float buffer = 0.1f;

        if (centerX >= playerX)
            return centerX - halfWidth - buffer;
        else
            return centerX + halfWidth + buffer;
    }
}
