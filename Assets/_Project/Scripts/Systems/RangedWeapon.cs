using UnityEngine;

namespace Roguelite
{
    /// <summary>远程：朝索敌方向发射弹丸。</summary>
    public class RangedWeapon : PlayerWeapon
    {
        protected override void Fire(Vector2 origin, Vector2 dir, float damage, float critChance, float statRange)
        {
            GameObject bullet = PoolManager.Spawn(ProjectileFactory.FriendlyPrototype, origin, Quaternion.identity);
            bullet.GetComponent<Projectile>().Shoot(dir, Data.speed, damage, 0, Data.range + statRange, critChance, Data.color);
        }
    }
}