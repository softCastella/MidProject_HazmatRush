using UnityEngine;

public class Pollutant : MonoBehaviour
{
    public enum PollutantType
    {
        TypeA,
        TypeB,
        TypeC
    }

    public PollutantType type = PollutantType.TypeA;
    public int hp;
    private float damagePerSecond;
    public float edgeHitRatio = 0.7f; // 이미지 가장자리 영역만 맞히도록 거리 기준

    public static int activeCount = 0;

    private float halfWidth = 0.5f;

    void Awake()
    {
        SetHpByType();
        Renderer rend = GetComponent<Renderer>();
        if (rend != null)
            halfWidth = rend.bounds.extents.x;
    }

    void SetHpByType()
    {
        switch (type)
        {
            case PollutantType.TypeA: // 혼합오염원
                hp = 50;
                damagePerSecond = 5;
                break;
            case PollutantType.TypeB: // 오일오염원
                hp = 20;
                damagePerSecond = 2;
                break;
            case PollutantType.TypeC: // 산성오염원
                hp = 30;
                damagePerSecond = 3;
                break;
        }
    }

    void OnEnable()
    {
        activeCount++;
    }

    void OnDestroy()
    {
        activeCount = Mathf.Max(0, activeCount - 1);
    }

    void OnTriggerStay2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        if (!IsEdgeContact(other.transform.position))
            return;

        hp -= Mathf.RoundToInt(damagePerSecond * Time.deltaTime);

        if (hp <= 0f)
        {
            Destroy(gameObject);
            
        }
    }

    bool IsEdgeContact(Vector3 playerPos)
    {
        float dist = Vector2.Distance(transform.position, playerPos);
        return dist >= halfWidth * edgeHitRatio;
    }
}
