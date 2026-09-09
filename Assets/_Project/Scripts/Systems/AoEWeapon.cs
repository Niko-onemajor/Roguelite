using UnityEngine;

namespace Roguelite
{
    /// <summary>范围：以玩家为中心圆形爆炸。</summary>
    public class AoEWeapon : PlayerWeapon
    {
        protected override void Fire(Vector2 origin, Vector2 dir, float damage, float critChance, float statRange) =>
            DamageSystem.RadiusHit(origin, Data.range, damage, critChance);
    }
}