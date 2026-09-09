using System.Collections.Generic;
using UnityEngine;

namespace Roguelite
{
    /// <summary>场上存活敌人注册表，供自动索敌使用。</summary>
    public static class EnemyRegistry
    {
        static readonly List<Enemy> enemies = new List<Enemy>();

        public static IReadOnlyList<Enemy> All => enemies;
        public static int AliveCount => enemies.Count;

        public static void Register(Enemy e)
        {
            if (!enemies.Contains(e)) enemies.Add(e);
        }

        public static void Unregister(Enemy e) => enemies.Remove(e);

        public static Enemy Nearest(Vector2 pos, float maxDistance)
        {
            Enemy best = null;
            float bestSq = maxDistance * maxDistance;
            for (int i = 0; i < enemies.Count; i++)
            {
                Enemy e = enemies[i];
                if (e == null) continue;
                float sq = ((Vector2)e.transform.position - pos).sqrMagnitude;
                if (sq <= bestSq) { bestSq = sq; best = e; }
            }
            return best;
        }

        public static void Clear() => enemies.Clear();
    }
}