using UnityEngine;

namespace Roguelite
{
    /// <summary>近战：面前扇形范围判定。</summary>
    public class MeleeWeapon : PlayerWeapon
    {
        protected override void Fire(Vector2 origin, Vector2 dir, float damage, float critChance, float statRange) =>
            DamageSystem.MeleeHit(origin, dir, Data.range, Data.halfAngle, damage, critChance);
    }
}