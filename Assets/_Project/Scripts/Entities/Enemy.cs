using UnityEngine;

namespace Roguelite
{
    /// <summary>敌人抽象基类：注册/血量/接触伤害/死亡掉落回收。</summary>
    [RequireComponent(typeof(Collider2D))]
    public abstract class Enemy : MonoBehaviour
    {
        public EnemyData Data { get; private set; }
        public float Health { get; private set; }

        // ── 装备被动临时状态(默认无增益；由被动光环/命中效果写入) ──
        /// <summary>移速倍率(基克减速/冰霜等改变，1=正常)，由 TickDebuffs 计时恢复。</summary>
        public float speedMult = 1f;
        /// <summary>攻击间隔倍率(冰霜之心光环 1.5，1=正常)。</summary>
        public float attackIntervalMult = 1f;
        /// <summary>承伤魔法伤害倍率(深渊面具易伤 1.15，1=正常)。</summary>
        public float magicVulnMult = 1f;
        /// <summary>残疫单目标附伤内置冷却(秒)。</summary>
        public float malignCd;

        float shredAmount;   // 黑切 破甲值(平减)
        float shredTimer;    // 破甲剩余秒
        float slowTimer;     // 减速剩余秒

        protected float attackTimer;
        SpriteRenderer sr;

        public void Init(EnemyData data)
        {
            Data = data;
            Health = data.maxHP;
            attackTimer = 0f; // 首次接触立即触发(射击/接触伤害)；随后按 attackInterval 节流
            if (sr == null) sr = GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                if (sr.sprite == SpriteArt.Fallback) sr.color = data.color; // 占位圆按数据染色；真实贴图保留原色
                // 视觉直径统一为 data.scale(世界单位)，与占位圆一致；碰撞体(直径0.8*scale)贴合视觉
                float k = SpriteArt.NormalizeFactor(sr.sprite, 1f) * data.scale;
                transform.localScale = Vector3.one * k;
                CircleCollider2D col = GetComponent<CircleCollider2D>();
                if (col != null) col.radius = 0.4f * data.scale / k; // local 半径*scale(k)=0.4*scale，贴合视觉
            }
        }

        void OnEnable() => EnemyRegistry.Register(this);
        void OnDisable() => EnemyRegistry.Unregister(this);

        void LateUpdate()
        {
            if (Data != null) EnemyUpdate(Time.deltaTime);
        }

        protected virtual void EnemyUpdate(float dt)
        {
            TickDebuffs(dt); // 破甲/减速计时恢复(由 LateUpdate 每帧驱动)
            Behavior(dt, PlayerController.Instance);
            transform.position = ArenaBounds.Clamp(transform.position); // 限制在竞技场内
            if (Data.contactDamage > 0f) TryContactDamage();
        }
        protected abstract void Behavior(float dt, PlayerController player);

        /// <summary>装备被动施加：物理破甲(黑切)。duration 秒后失效；平减数值可多次叠加累计(上限30)。</summary>
        public void Shred(float amount, float duration)
        {
            shredAmount = Mathf.Min(30f, shredAmount + amount);
            shredTimer = duration;
        }

        /// <summary>装备被动施加：减速(基克)。持续 duration 秒后恢复原速。</summary>
        public void Slow(float targetSpeedMult, float duration)
        {
            speedMult = targetSpeedMult;
            slowTimer = duration;
        }

        /// <summary>装备被动状态计时(每帧)：破甲/减速到期恢复、残疫单目标附伤冷却。EditMode 测试可显式调用。</summary>
        internal void TickDebuffs(float dt)
        {
            if (shredTimer > 0f) { shredTimer -= dt; if (shredTimer <= 0f) shredAmount = 0f; }
            if (slowTimer > 0f) { slowTimer -= dt; if (slowTimer <= 0f) speedMult = 1f; }
            if (malignCd > 0f) malignCd -= dt;
        }

        /// <summary>手动距离判定接触伤害：与玩家中心距离 &lt; 双方碰撞半径之和即视为撞到玩家。
        /// 撞到后怪物立即消失(不掉金币/不记击杀，防贴脸刷钱)，杜绝与玩家模型重叠。
        /// internal 供 EditMode 测试直接驱动。</summary>
        internal void TryContactDamage()
        {
            PlayerController pc = PlayerController.Instance;
            if (pc == null) return;
            float d = Vector3.Distance(transform.position, pc.transform.position);
            CircleCollider2D my = GetComponent<CircleCollider2D>();
            CircleCollider2D other = pc.GetComponent<CircleCollider2D>();
            float hitRadius = (my != null ? my.radius * transform.localScale.x : 0.4f)
                            + (other != null ? other.radius * pc.transform.localScale.x : 0.5f);
            if (d > hitRadius) return;
            if (PlayerStats.Instance != null) PlayerStats.Instance.TakeDamage(Data.contactDamage);
            PoolManager.Return(gameObject); // 撞击即消失，不进入 Die(不掉金币/不记击杀)
        }

        /// <summary>finalDamage 为最终物理伤害(暴击已算好)；护甲/黑切破甲降低受到的物理伤害；吸血按实际造成伤害结算。</summary>
        public void TakeDamage(float finalDamage, bool wasCrit)
        {
            if (Data == null) return;
            // 有效护甲 = 怪物护甲 - 玩家护甲穿透(下限0) - 黑切破甲；护甲减伤：dmg*100/(100+armor)
            PlayerStats ps = PlayerStats.Instance;
            float effectiveArmor = Mathf.Max(0f, Data.armor - (ps != null ? ps.armorPen : 0f) - shredAmount);
            ApplyDamage(finalDamage * (100f / (100f + effectiveArmor)));
        }

        /// <summary>魔法伤害：按怪物魔抗与玩家法术穿透结算，并受深渊面具易伤倍率影响。</summary>
        public void TakeMagicDamage(float dmg)
        {
            if (Data == null) return;
            PlayerStats ps = PlayerStats.Instance;
            float effectiveResist = Mathf.Max(0f, Data.magicResist - (ps != null ? ps.magicPen : 0f));
            ApplyDamage(dmg * (100f / (100f + effectiveResist)) * magicVulnMult);
        }

        /// <summary>统一伤害结算：扣血/吸血/受击闪红/死亡。reduced 为已过减伤后的最终数值。</summary>
        void ApplyDamage(float reduced)
        {
            if (Data == null) return;
            PlayerStats ps = PlayerStats.Instance;
            Health -= reduced;
            if (ps != null && ps.omnivamp > 0f) ps.Heal(reduced * ps.omnivamp); // 全能吸血按实际造成伤害回血
            if (Health > 0f)
            {
                var flash = GetComponent<HitFlash>(); // 非致命受击闪红(白对白底贴图不可见，改红更清晰)
                if (flash != null) flash.Flash(Color.red, 0.15f);
            }
            if (Health <= 0f) Die();
        }

        protected virtual void Die()
        {
            PlayerStats ps = PlayerStats.Instance;
            if (ps != null)
            {
                ps.NotifyKill();
                ps.OnKill(this); // 装备击杀被动挂钩(击杀叠吸血/蜕生回血等)
            }
            int gold = Random.Range(Data.goldMin, Data.goldMax + 1);
            PickupFactory.Spawn(transform.position, gold);
            PoolManager.Return(gameObject);
        }

        void OnTriggerStay2D(Collider2D other) { } // 接触伤害改为 TryContactDamage 距离判定，此处保留空实现仅为物理回调兼容签名

        /// <summary>调试：Scene 视图绘制碰撞体积线框(白色圆)，核对贴图与碰撞盒是否贴合。</summary>
        void OnDrawGizmos()
        {
            CircleCollider2D col = GetComponent<CircleCollider2D>();
            if (col == null) return;
            Gizmos.color = Color.white;
            Gizmos.DrawWireSphere(transform.position, col.radius * Mathf.Max(0.0001f, transform.localScale.x));
        }
    }
}