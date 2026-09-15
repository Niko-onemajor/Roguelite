using UnityEngine;

namespace Roguelite
{
    /// <summary>子弹。team=0 打敌人，team=1 打玩家。</summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    public class Projectile : MonoBehaviour
    {
        public int Team { get; private set; }

        float damage;
        float critChance;
        float life;
        Rigidbody2D rb;
        SpriteRenderer sr;

        void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            sr = GetComponent<SpriteRenderer>();
        }

        public void Shoot(Vector2 dir, float speed, float dmg, int team, float maxRange, float critChanceValue, Color color)
        {
            Team = team;
            damage = dmg;
            critChance = critChanceValue;
            life = maxRange / Mathf.Max(0.01f, speed);
            rb.velocity = dir * speed;
            if (sr != null) sr.color = color;
            // 贴图朝向飞行方向：素材(laser)为水平向右，旋转到 dir 角度
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }

        void Update()
        {
            life -= Time.deltaTime;
            if (life <= 0f) PoolManager.Return(gameObject);
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (Team == 0)
            {
                Enemy enemy = other.GetComponentInParent<Enemy>();
                if (enemy != null && enemy.Data != null)
                {
                    bool crit = DamageUtilities.RollCrit(critChance, DamageSystem.Rng);
                    enemy.TakeDamage(DamageUtilities.ComputeCrit(damage, crit, 2f), crit);
                    PoolManager.Return(gameObject);
                }
            }
            else if (other.GetComponentInParent<PlayerController>() != null)
            {
                if (PlayerStats.Instance != null) PlayerStats.Instance.TakeDamage(damage);
                PoolManager.Return(gameObject);
            }
        }
    }
}