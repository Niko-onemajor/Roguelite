using UnityEngine;

namespace Roguelite
{
    /// <summary>远程型：保持距离并发射弹幕。</summary>
    public class EnemyRanged : Enemy
    {
        protected override void Behavior(float dt, PlayerController player)
        {
            if (player == null || Data == null) return;
            Vector2 to = player.transform.position - transform.position;
            float dist = to.magnitude;
            Vector2 dir = dist > 0.001f ? to / dist : Vector2.zero;
            float keep = Mathf.Max(0.1f, Data.keepDistance);

            if (dist < keep * 0.9f) transform.position += (Vector3)(-dir * (Data.moveSpeed * dt));
            else if (dist > keep * 1.4f) transform.position += (Vector3)(dir * (Data.moveSpeed * dt));

            attackTimer -= dt;
            if (attackTimer <= 0f && dist < Data.range && dist > 0.001f)
            {
                attackTimer = Data.attackInterval;
                GameObject bullet = PoolManager.Spawn(ProjectileFactory.EnemyPrototype, transform.position, Quaternion.identity);
                bullet.GetComponent<Projectile>().Shoot(dir, Data.projectileSpeed, Data.projectileDamage, 1, Data.range, 0f, Color.magenta);
            }
        }
    }
}