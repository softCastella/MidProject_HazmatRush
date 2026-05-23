using UnityEngine;

public class Pollutant : MonoBehaviour
{
    public int damage = 10;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            Player player = other.GetComponent<Player>();
            Debug.Log("플레이어와 오염물이 충돌했습니다.");
            // player.TakeDamage(damage);
            // Destroy(gameObject);
        }
    }
}
