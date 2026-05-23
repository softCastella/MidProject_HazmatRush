using System.Collections.Generic;
using UnityEngine;

public class PollutantManager : MonoBehaviour
{
    public Player player;
    public GameObject pollutantPrefab;
    public PollutantSpawner[] spawners;
    public Vector2 spawnTimeRange = new Vector2(2f, 3f);

    private float moveTime = 0f;
    private float nextSpawnTime;

    void Awake()
    {
        if (player == null)
            player = FindObjectOfType<Player>();

        if (spawners == null || spawners.Length == 0)
            spawners = FindObjectsOfType<PollutantSpawner>();

        nextSpawnTime = Random.Range(spawnTimeRange.x, spawnTimeRange.y);
    }

    void Update()
    {
        if (player == null)
            return;

        if (player.isMoving)
        {
            moveTime += Time.deltaTime;
        }
        else
        {
            moveTime = 0f;
        }

        if (moveTime >= nextSpawnTime)
        {
            SpawnPollutant();
            moveTime = 0f;
            nextSpawnTime = Random.Range(spawnTimeRange.x, spawnTimeRange.y);
        }
    }

    private void SpawnPollutant()
    {
        if (pollutantPrefab == null)
        {
            Debug.LogError("PollutantManager: pollutantPrefab이 할당되지 않았습니다.");
            return;
        }

        List<PollutantSpawner> available = new List<PollutantSpawner>();
        foreach (var spawner in spawners)
        {
            if (spawner != null && spawner.isActive)
                available.Add(spawner);
        }

        if (available.Count == 0)
        {
            Debug.LogWarning("PollutantManager: 활성화된 스포너가 없습니다.");
            return;
        }

        int index = Random.Range(0, available.Count);
        available[index].Spawn(pollutantPrefab);
        Debug.Log("PollutantManager: 오염물이 생성되었습니다.");
    }
}
