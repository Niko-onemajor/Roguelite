using UnityEngine;

namespace Roguelite
{
    /// <summary>敌人抽象基类：注册/血量/接触伤害/死亡掉落回收。</summary>
    [RequireComponent(typeof(Collider2D))]
    public abstract class Enemy : MonoBehaviour
    {
        public EnemyData Data { get; private set; }
        public float Health { get; private set; }

        protected float attackTimer;
        SpriteRenderer sr;

        public void Init(EnemyData data)
        {
            Data = data;
            Health = data.maxHP;
            attackTimer = data.attackInterval;
            if (sr == null) sr = GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.color = data.color;
                transform.localScale = Vector3.one * data.scale;
            }
        }

        void OnEnable() => EnemyRegistry.Register(this);
        void OnDisable() => EnemyRegistry.Unregister(this);

        void LateUpdate()
        {
            if (Data != null) EnemyUpdate(Time.deltaTime);
        }

        protected virtual void EnemyUpdate(float dt) => Behavior(dt, PlayerController.Instance);
        protected abstract void Behavior(float dt, PlayerController player);

        /// <summary>finalDamage 为最终伤害(暴击已算好)。</summary>
        public void TakeDamage(float finalDamage, bool wasCrit)
        {
            if (Data == null) return;
            Health -= finalDamage;
            if (Health <= 0f) Die();
        }

        protected virtual void Die()
        {
            if (PlayerStats.Instance != null) PlayerStats.Instance.NotifyKill();
            int gold = Random.Range(Data.goldMin, Data.goldMax + 1);
            PickupFactory.Spawn(transform.position, gold);
            PoolManager.Return(gameObject);
        }

        void OnTriggerStay2D(Collider2D other)
        {
            if (Data == null || Data.contactDamage <= 0f) return;
            if (other.GetComponentInParent<PlayerController>() == null) return;
            attackTimer -= Time.deltaTime;
            if (attackTimer <= 0f)
            {
                attackTimer = Data.attackInterval;
                if (PlayerStats.Instance != null) PlayerStats.Instance.TakeDamage(Data.contactDamage);
            }
        }
    }
}