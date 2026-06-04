using UnityEngine;

public class RecoveryItemSpawner : MonoBehaviour
{
    public Transform[] spawnPoints;

    public int PointCount
    {
        get
        {
            if (spawnPoints != null && spawnPoints.Length > 0)
                return spawnPoints.Length;
            return 1;
        }
    }

    public Vector2 GetPointPosition(int index)
    {
        if (spawnPoints != null && spawnPoints.Length > 0)
        {
            int i = Mathf.Clamp(index, 0, spawnPoints.Length - 1);
            return spawnPoints[i].position;
        }

        return transform.position;
    }

    public GameObject Spawn(GameObject prefab, int pointIndex)
    {
        if (prefab == null)
            return null;

        return Instantiate(prefab, GetPointPosition(pointIndex), Quaternion.identity);
    }
}
