using System;

namespace Roguelite
{
    /// <summary>纯逻辑伤害计算，便于单测。</summary>
    public static class DamageUtilities
    {
        public static bool RollCrit(float critChance, Random rng) =>
            rng != null && rng.NextDouble() < critChance;

        public static float ComputeCrit(float damage, bool isCrit, float critMultiplier) =>
            isCrit ? damage * critMultiplier : damage;
    }
}