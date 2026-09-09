using UnityEngine;

namespace Roguelite
{
    /// <summary>金币拾取物：触发即收，带磁吸。</summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    public class Pickup : MonoBehaviour
    {
        public int Gold { get; private set; }

        Rigidbody2D rb;
        SpriteRenderer sr;

        void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            sr = GetComponent<SpriteRenderer>();
        }

        public void SetGold(int gold, Color color)
        {
            Gold = gold;
            if (sr != null) sr.color = color;
            if (rb != null) rb.velocity = Random.insideUnitCircle * 2.5f;
        }

        void Update()
        {
            PlayerController p = PlayerController.Instance;
            if (p == null) return;
            Vector2 delta = (Vector2)p.transform.position - (Vector2)transform.position;
            float radius = p.Stats != null ? p.Stats.pickupRadius : 2.5f;
            if (delta.sqrMagnitude <= radius * radius)
                transform.position += (Vector3)(delta.normalized * 8f * Time.deltaTime);
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (other.GetComponentInParent<PlayerController>() != null)
            {
                if (PlayerStats.Instance != null) PlayerStats.Instance.AddGold(Gold);
                PoolManager.Return(gameObject);
            }
        }
    }
}