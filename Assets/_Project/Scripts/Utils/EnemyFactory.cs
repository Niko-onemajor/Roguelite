using System.Collections.Generic;
using UnityEngine;

namespace Roguelite
{
    /// <summary>按 EnemyType 构建惰性原型(保持 inactive)，并负责从池生成敌人。</summary>
    public static class EnemyFactory
    {
        static readonly Dictionary<EnemyType, GameObject> prototypes = new Dictionary<EnemyType, GameObject>();

        /// <summary>EnemyType → Resources/Sprites/ 贴图名(空=只用占位圆)。</summary>
        static readonly Dictionary<EnemyType, string> spriteNames = new Dictionary<EnemyType, string>
        {
            { EnemyType.Chaser, "enemy_chaser" },
            { EnemyType.Ranged, "enemy_ranged" },
            { EnemyType.Tank, "enemy_tank" }
        };

        public static GameObject Prototype(EnemyType type)
        {
            if (!prototypes.TryGetValue(type, out GameObject proto) || proto == null) // == 检测已销毁对象
            {
                proto = Build(type);
                proto.SetActive(false);
                prototypes[type] = proto;
            }
            return proto;
        }

        static GameObject Build(EnemyType type)
        {
            GameObject go = new GameObject("Enemy_" + type,
                typeof(SpriteRenderer), typeof(Poolable), typeof(Rigidbody2D), typeof(CircleCollider2D), typeof(HitFlash)); // HitFlash：受击闪白
            switch (type)
            {
                case EnemyType.Ranged: go.AddComponent<EnemyRanged>(); break;
                case EnemyType.Tank: go.AddComponent<EnemyTank>(); break;
                default: go.AddComponent<EnemyChaser>(); break;
            }
            Rigidbody2D rb = go.GetComponent<Rigidbody2D>();
            rb.isKinematic = true;
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            CircleCollider2D col = go.GetComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.4f;
            SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
            sr.sprite = SpriteArt.LoadOrPlaceholder(spriteNames.TryGetValue(type, out string name) ? name : "");
            sr.sortingOrder = 1;
            go.transform.localScale = Vector3.one * 0.8f; // 0.5*0.8=0.4 与碰撞体匹配
            return go;
        }

        public static Enemy Spawn(EnemyData data, Vector3 position)
        {
            GameObject go = PoolManager.Spawn(Prototype(data.type), position, Quaternion.identity);
            if (go == null)
            {
                Debug.LogError("EnemyFactory.Spawn: prototype unavailable for " + data.type);
                return null;
            }
            Enemy enemy = go.GetComponent<Enemy>();
            enemy.Init(data);
            return enemy;
        }
    }
}