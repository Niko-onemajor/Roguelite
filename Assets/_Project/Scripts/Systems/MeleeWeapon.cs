using UnityEngine;

namespace Roguelite
{
    /// <summary>近战：面前扇形范围判定 + 挥砍刀光特效。</summary>
    public class MeleeWeapon : PlayerWeapon
    {
        protected override void Fire(Vector2 origin, Vector2 dir, float damage, float critChance, float statRange)
        {
            SimpleVfx.Swing(origin, dir, Data.range * 1.05f, Data.halfAngle * Mathf.Deg2Rad,
                new Color(0.95f, 0.95f, 1f, 0.85f));
            DamageSystem.MeleeHit(origin, dir, Data.range, Data.halfAngle, damage, critChance);
        }
    }
}