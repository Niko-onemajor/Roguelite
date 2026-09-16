using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Roguelite
{
    /// <summary>已获取符文(每回合锻体选择的卡牌效果)，用于暂停面板回看。</summary>
    public sealed class RuneInfo
    {
        public string Name;   // 卡牌名：如 伤害+6
        public string Desc;   // 生效后效果描述：如 金卡 ×1.5
        public Color Color;   // 稀有度对应颜色
    }

    /// <summary>玩家属性与资源(HP/金币/击杀/符文)，供商店/锻体/符文增益与系统读取。
    /// 属性表含 LOL 风格 17 项；未挂钩的词条仅存储展示，待装备效果导入。</summary>
    public class PlayerStats : MonoBehaviour
    {
        public static PlayerStats Instance { get; set; }

        // ── 17 项属性（LOL 风格命名，中文注释）──
        public float damage = 10f;           // 攻击力(AD)
        public float abilityPower = 0f;      // 法术强度(AP)
        public float attackInterval = 0.8f;  // 攻速基准(秒/发，受 AttackSpeed 乘算)
        public float critChance = 0.05f;     // 暴击几率
        public float critMultiplier = 2f;    // 暴击伤害倍率(加算)
        public float armorPen = 0f;          // 护甲穿透
        public float magicPen = 0f;          // 法术穿透
        public float omnivamp = 0f;          // 全能吸血(0~1，按造成伤害回血)
        public float maxHP = 120f;           // 生命值上限
        public float hpRegen = 0f;           // 生命回复(每秒)
        public float armor = 0f;             // 护甲(减伤 护甲/(100+护甲))
        public float magicResist = 0f;       // 魔法抗性
        public float healShieldPower = 0f;   // 治疗与护盾强度
        public float abilityHaste = 0f;      // 技能急速(当前仅存储)
        public float moveSpeed = 8f;         // 移动速度(追兵3.2，差距2.5倍：能被甩开但需走位)
        public float range = 6f;             // 攻击距离
        public float size = 1f;              // 体型(当前仅存储，视觉缩放后续接入)
        public float mana = 0f;              // 当前法力(法术消耗；默认0需法力值装备累积)
        public float maxMana = 0f;           // 法力上限(由 法力值 词条累积)
        public float tenacity = 0f;          // 韧性(控制减免，当前仅存储)
        public float pickupRadius = 2.5f;

        /// <summary>攻速基准间隔(秒/发)：战斗用 武器间隔 × (attackInterval/此处)，使攻速词条统一作用于全部武器。</summary>
        public const float AttackIntervalBase = 0.8f;

        // ── 被动增益参数（由装备 passiveType 绑定，见 ApplyBonus）──
        public const float BaseHP = 120f;              // 基础生命值(计算“额外生命值”用)
        public PassiveType passiveType = PassiveType.None;
        const float TyrantHpToDamage = 0.025f;         // 专横：额外生命值 2.5% → 攻击力
        const float TyrantMissingToDamage = 0.12f;     // 报复：已损失生命值% 的 12% → 攻击力

        // ── 法力/法术参数（海克斯科技枪刃 等法系被动触发）──
        /// <summary>法力自然回复：每秒回复 法力上限 × 该比例。</summary>
        const float ManaRegenFractionPerSec = 0.1f;
        const float ArcaneInterval = 3f;               // 奥术弹基础冷却(秒)，受技能急速缩放
        const float ArcaneManaCost = 6f;               // 每发奥术弹消耗法力
        const float ArcaneBaseDamage = 15f;            // 奥术弹基础伤害(法强按 AP×0.7 加成)
        const float ArcaneApRatio = 0.7f;
        const float AbortRetryInterval = 0.25f;        // 法力不足/无目标时提前重试冷却

        float spellCooldown;

        // ── 被动装备位掩码(多件被动可共存；passiveType 保留“最后绑定”兼容展示与旧测试) ──
        ulong passiveMask;

        /// <summary>当前总护盾值(救主灵刃/猩红护盾等)，受击先扣盾再扣生命。</summary>
        public float Shield;

        // ── 被动运行时状态 ──
        float spellbladeCd;      // 咒刃(三相/黄昏)冷却
        float energizedCd;       // 盈能(岚切/疾射火炮)冷却
        float stormSurgeCd;      // 风暴狂涌冷却
        float ludenCd;           // 卢登回声冷却
        float lifelineCd;        // 救主灵刃冷却
        float timeSinceDamaged;  // 距上次受伤秒数(狂徒脱战判定)
        float timeSinceAttack;   // 距上次攻击秒数(亡者叠层判定)
        float deadmanStackTimer;
        int deadmanStacks;       // 亡者的板甲 气势层数
        int barrageCount;        // 猎魔人弩箭 剩余必暴次数
        float barrageTimer;      // 猎魔人 弹幕刷新计时
        float msAmpRemaining;    // 通用移速增幅(暗夜收割者/风暴狂涌)
        float msAmpValue = 1f;
        float msFervorRemaining; // 命中攻速移速(幻影/班德尔)
        float msFervorValue = 1f;
        float asBuffRemaining;   // 攻速增益(香炉/幻影/班德尔)剩余秒数
        float asBuffMult = 1f;   // 攻速增益倍率(作用于攻击间隔)
        float apBuffRemaining;   // 流水法杖 法强增益剩余秒数
        float apBuffValue;       // 流水法杖 法强增益点数
        float natureMsRemaining; // 自然之力 层数剩余
        int natureStacks;        // 自然之力 移速层数
        float burnTimer = 1f;    // 日炎 献祭周期
        float endlessTimer = 1.5f; // 无终恨意 周期
        float bleedPool;         // 死亡之舞 待结算流血
        bool spearBoost;         // 朔极之矛 充能标记
        int omnivampKillStacks;  // 击杀叠吸血层数
        float rangeBonus;        // 海克斯镜片 击杀叠攻击距离
        readonly List<BurnDot> burns = new List<BurnDot>();

        /// <summary>灼烧 DoT(兰德里的折磨)：每秒 dps 魔法伤害，持续 duration。</summary>
        sealed class BurnDot
        {
            public Enemy Enemy;
            public float Dps;
            public float Remaining;
        }

        // ── 装备栏（Brotato 式：所有装备共占 8 槽，数字键 1-8 触发带主动效果的装备；槽满不可再购）──
        /// <summary>装备栏槽位数（1-8 数字键）。</summary>
        public const int EquipmentSlotCount = 8;
        readonly ActiveSlot[] _equipSlots = new ActiveSlot[EquipmentSlotCount];

        float moveBurstRemaining;                     // 舒瑞娅 移速爆发剩余秒数

        public IReadOnlyList<ActiveSlot> EquipSlots => _equipSlots;

        const float RegenTickSeconds = 1f;

        public float CurrentHP { get; private set; }
        public int Gold { get; private set; }
        public int Kills { get; private set; }

        /// <summary>被动是否生效：被动掩码命中 或 最后绑定被动(兼容直接赋值/旧测试)。
        /// 多件被动装备同时持有时可共存叠加(位掩码)。</summary>
        public bool HasPassive(PassiveType p) =>
            p != PassiveType.None && ((passiveMask & (1UL << (int)p)) != 0 || passiveType == p);

        /// <summary>猎魔人弩箭：剩余必定暴击的普攻次数(HitEnemy 每次命中消耗)。</summary>
        public int BarrageCount { get => barrageCount; set => barrageCount = Mathf.Max(0, value); }

        /// <summary>攻击距离(含海克斯镜片 击杀叠层)。</summary>
        public float Range => range + rangeBonus;

        /// <summary>实际攻速间隔(香炉/幻影/班德尔 攻速增益 乘算)，供武器冷却计算。</summary>
        public float AttackIntervalEffective => attackInterval * (asBuffRemaining > 0f ? asBuffMult : 1f);

        /// <summary>实际面板法术强度 = 基础法强 + 裂隙制造者(额外生命6%) + 流水法杖增益，再 × 死亡之帽1.4。</summary>
        public float TotalAbilityPower
        {
            get
            {
                float ap = abilityPower;
                if (HasPassive(PassiveType.VoidInfusion)) ap += Mathf.Max(0f, maxHP - BaseHP) * 0.06f;
                if (HasPassive(PassiveType.Deathcap)) ap *= 1.4f;
                if (apBuffRemaining > 0f) ap += apBuffValue;
                return ap;
            }
        }

        /// <summary>实际治疗与护盾强度 = 基础 + 歌之权冠(最大法力×0.003)；贪婪胫甲低血时再加20%。</summary>
        public float TotalHealShieldPower
        {
            get
            {
                float hsp = healShieldPower;
                if (HasPassive(PassiveType.CrownHealPower)) hsp += maxMana * 0.003f;
                if (HasPassive(PassiveType.GreedTreads) && maxHP > 0f && CurrentHP / maxHP < 0.5f) hsp += 0.2f;
                return hsp;
            }
        }

        /// <summary>战斗实际面板攻击力 = 基础攻击力 + 被动动态加成(贪婪胫甲/霸王血铠 专横/报复)。</summary>
        public float TotalDamage
        {
            get
            {
                float total = damage;
                // 贪婪胫甲：生命≥50% 时造成伤害+8%
                if (HasPassive(PassiveType.GreedTreads) && maxHP > 0f && CurrentHP / maxHP >= 0.5f)
                    total *= 1.08f;
                if (!HasPassive(PassiveType.Tyrant)) return total;
                float bonusHp = Mathf.Max(0f, maxHP - BaseHP); // 额外生命值
                float missingPct = maxHP > 0f ? Mathf.Clamp01(1f - CurrentHP / maxHP) : 0f;
                return total + bonusHp * TyrantHpToDamage + total * missingPct * TyrantMissingToDamage;
            }
        }

        /// <summary>自动入库金币累计值(回合末未拾取金币自动结算的部分)。</summary>
        public int BankedGold { get; private set; }

        /// <summary>本局已获取的符文(每回合锻体选择的卡牌效果)，供暂停面板查看。</summary>
        public readonly List<RuneInfo> Runes = new List<RuneInfo>();

        void OnEnable() => Instance = this;

        void OnDisable()
        {
            if (Instance == this) Instance = null;
        }

        public void ResetForRun()
        {
            CurrentHP = maxHP;
            Gold = 0;
            Kills = 0;
            BankedGold = 0;
            Runes.Clear();
            Shield = 0f;
            bleedPool = 0f;
            burns.Clear();
            GameEvents.RaiseHP(CurrentHP, maxHP);
            GameEvents.RaiseGold(Gold);
            GameEvents.RaiseGoldBanked(0);
            GameEvents.RaiseMana(mana, maxMana);
            StartRegenLoop();
        }

        public void TakeDamage(float dmg) => ReceiveDamage(dmg, magicDamage: false);

        /// <summary>魔法伤害：按魔抗减伤后走统一受击管线。</summary>
        public void TakeMagicDamage(float dmg) => ReceiveDamage(dmg, magicDamage: true);

        void ReceiveDamage(float dmg, bool magicDamage)
        {
            float resist = magicDamage ? magicResist : armor;
            float reduced = dmg * (100f / (100f + Mathf.Max(0f, resist)));
            // 铁板靴：受到的物理(普攻类)伤害 ×0.88
            if (!magicDamage && HasPassive(PassiveType.Steelcaps)) reduced *= 0.88f;

            // 荆棘之甲：受物理伤害 反弹 30% 魔法伤害给最近敌人
            if (!magicDamage && HasPassive(PassiveType.Thornmail) && reduced > 0f)
            {
                Enemy target = EnemyRegistry.Nearest(transform.position, float.MaxValue);
                if (target != null) DamageSystem.CastMagic(target, reduced * 0.3f, 0f);
            }

            // 死亡之舞：物理伤害 30% 转 3s 流血延迟扣除(流血在 TickPassives 结算，不受护盾/抗性影响)
            float instant = reduced;
            if (!magicDamage && HasPassive(PassiveType.DeathDance))
            {
                instant = reduced * 0.7f;
                bleedPool += reduced * 0.3f;
            }

            // 护盾吸收(救主灵刃/猩红护盾)
            if (instant > 0f && Shield > 0f)
            {
                float absorbed = Mathf.Min(Shield, instant);
                Shield -= absorbed;
                instant -= absorbed;
                if (absorbed > 0f) TriggerSupportBuffs();
            }
            if (instant > 0f) CurrentHP = Mathf.Max(0f, CurrentHP - instant);
            GameEvents.RaiseHP(CurrentHP, maxHP);

            // 自然之力：受技能(魔法)伤害 → 移速层数
            if (magicDamage && HasPassive(PassiveType.ForceOfNature) && reduced > 0f)
            {
                natureStacks = Mathf.Min(3, natureStacks + 1);
                natureMsRemaining = 5f;
            }

            // 救主灵刃(斯特拉克/玛莫提乌斯/原生质护带)：生命<30% 获得护盾
            if (HasPassive(PassiveType.Lifeline) && lifelineCd <= 0f && maxHP > 0f && CurrentHP / maxHP < 0.3f)
            {
                Shield = Mathf.Max(Shield, maxHP * 0.35f);
                lifelineCd = 30f;
                TriggerSupportBuffs();
            }

            timeSinceDamaged = 0f; // 狂徒 脱战计时重置
            var flash = GetComponent<HitFlash>(); // 玩家受击反馈：闪红
            if (flash != null) flash.Flash(Color.red, 0.12f);
        }

        /// <summary>治疗：受治疗与护盾强度加成，不溢出当前生命上限；
        /// 溢出的治疗量在持有饮血剑时转化为猩红护盾。</summary>
        public void Heal(float amount)
        {
            if (amount <= 0f) return;
            float healed = amount * (1f + TotalHealShieldPower);
            float capped = Mathf.Min(maxHP - CurrentHP, healed);
            if (capped > 0f) CurrentHP += capped;
            // 饮血剑 猩红护盾：溢出治疗转护盾(上限最大生命15%)
            if (HasPassive(PassiveType.Bloodshield) && healed > capped)
            {
                float overflow = Mathf.Min(maxHP * 0.15f - Shield, healed - capped);
                if (overflow > 0f) Shield += overflow;
            }
            GameEvents.RaiseHP(CurrentHP, maxHP);
            TriggerSupportBuffs(); // 治疗/护盾交互(香炉/流水法杖)
        }

        /// <summary>治疗/护盾交互后 触发 香炉(攻速)与流水法杖(法强)增益(单机简化：对自身生效)。</summary>
        void TriggerSupportBuffs()
        {
            if (HasPassive(PassiveType.ArdentCenser))
            {
                asBuffMult = 1f / 1.15f; // 攻速+15% → 攻击间隔 ×0.87
                asBuffRemaining = 4f;
            }
            if (HasPassive(PassiveType.FlowingStaff))
            {
                apBuffValue = 4f;
                apBuffRemaining = 4f;
            }
        }

        /// <summary>每秒生命回复结算(供回复协程逐秒调用，也与全能吸血共用 Heal 加算治疗强度)。</summary>
        public void TickRegen()
        {
            if (hpRegen > 0f) Heal(hpRegen);
        }

        /// <summary>每秒法力回复：按法力上限比例回复，不超上限。</summary>
        public void RechargeMana()
        {
            if (maxMana <= 0f) return;
            mana = Mathf.Min(maxMana, mana + maxMana * ManaRegenFractionPerSec);
            GameEvents.RaiseMana(mana, maxMana);
        }

        /// <summary>尝试消耗法力，不足返回 false(施法失败，调用方应中断本次施放)。</summary>
        public bool TrySpendMana(float cost)
        {
            if (cost <= 0f) return true;
            if (mana < cost) return false;
            mana -= cost;
            GameEvents.RaiseMana(mana, maxMana);
            return true;
        }

        /// <summary>技能急速 → 冷却缩放(0 急速=1.0；急速越高系数越小，100 急速≈半冷却)。</summary>
        public float HasteCooldownScale => 100f / (100f + abilityHaste);

        /// <summary>有效移速 = 基础移速 ×(舒瑞娅爆发/暗夜收割者/风暴狂涌/幻影/班德尔/自然之力/亡者板甲 被动叠加)。</summary>
        public float EffectiveMoveSpeed
        {
            get
            {
                float mult = moveSpeed;
                if (moveBurstRemaining > 0f) mult *= MoveBurstSpeedMult;
                if (msAmpRemaining > 0f) mult *= msAmpValue;
                if (msFervorRemaining > 0f) mult *= msFervorValue;
                mult *= 1f + natureStacks * 0.15f;   // 自然之力：每层+15%移速
                mult *= 1f + deadmanStacks * 0.04f;  // 亡者的板甲：每层气势+4%移速
                return mult;
            }
        }

        #region 装备被动挂钩(OnAttackHit / OnKill / TickPassives)

        /// <summary>普攻命中敌人后触发被动(DamageSystem.HitEnemy 每发普攻调用一次)。
        /// dealt 为实际造成伤害；cleaveHit 标识溅射命中的目标(不重复触发 on-hit 效果，防递归)。</summary>
        public void OnAttackHit(Enemy enemy, float dealt, Vector2 pos, bool cleaveHit = false)
        {
            if (enemy == null) return;
            timeSinceAttack = 0f;
            if (cleaveHit) return;

            // 三相之力/黄昏与黎明 “咒刃”：每1.5s一次普攻附带 攻击力/法强×1
            if (spellbladeCd <= 0f)
            {
                if (HasPassive(PassiveType.Spellblade))
                {
                    enemy.TakeDamage(damage, false);
                    spellbladeCd = 1.5f;
                }
                else if (HasPassive(PassiveType.SpellbladeArcane))
                {
                    DamageSystem.CastMagic(enemy, abilityPower, 0f);
                    spellbladeCd = 1.5f;
                }
            }
            // 破败王者之刃：普攻附加目标当前生命6% 物理伤害
            if (HasPassive(PassiveType.RuinKing) && enemy.Health > 0f)
                enemy.TakeDamage(enemy.Health * 0.06f, false);
            // 岚切/疾射火炮 “盈能”：每2.5s一次 普攻附带 10+攻击力×0.3 魔法伤害
            if (HasPassive(PassiveType.Energized) && energizedCd <= 0f)
            {
                DamageSystem.CastMagic(enemy, 10f + damage * 0.3f, 0f);
                energizedCd = 2.5f;
            }
            // 黑色切割者：削减目标护甲10(持续5s，累加上限30)
            if (HasPassive(PassiveType.BlackCleaver)) enemy.Shred(10f, 5f);
            // 基克的聚合：减速目标35%、2s
            if (HasPassive(PassiveType.FrostBite)) enemy.Slow(0.65f, 2f);
            // 亡者的板甲：普攻消耗全部气势层数，每层+2 物理伤害
            if (HasPassive(PassiveType.DeadMans) && deadmanStacks > 0)
            {
                enemy.TakeDamage(deadmanStacks * 2f, false);
                deadmanStacks = 0;
            }
            // 海克斯镜片“高倍望远镜”：距离≥6 时伤害+25%
            if (HasPassive(PassiveType.Longshot) &&
                (enemy.transform.position - transform.position).sqrMagnitude >= 36f)
                enemy.TakeDamage(dealt * 0.25f, false);
            // 幻影之舞/班德尔音管：命中后 2s 内 攻速×0.9、移速×1.1
            if (HasPassive(PassiveType.StrikerFervor))
            {
                asBuffMult = 0.9f;
                asBuffRemaining = 2f;
                msFervorValue = 1.1f;
                msFervorRemaining = 2f;
            }
            // 卢安娜的飓风：对附近另一敌人造成 50% 伤害(直接结算，不在触发 on-hit，避免递归)
            if (HasPassive(PassiveType.Hurricane))
            {
                Enemy other = NearbyEnemy(pos, enemy);
                if (other != null) other.TakeDamage(dealt * 0.5f, false);
            }
            // 巨型九头蛇/贪欲九头蛇：普攻对周围2.6内敌人溅射
            if (HasPassive(PassiveType.TitanicCleave) || HasPassive(PassiveType.RavenousCleave))
            {
                float splash = HasPassive(PassiveType.TitanicCleave) ? damage * 0.8f : dealt * 0.5f;
                var copy = new List<Enemy>(EnemyRegistry.All);
                float r2 = 2.6f * 2.6f;
                for (int i = 0; i < copy.Count; i++)
                {
                    Enemy e = copy[i];
                    if (e == null || e == enemy || e.Data == null) continue;
                    if (((Vector2)e.transform.position - pos).sqrMagnitude > r2) continue;
                    e.TakeDamage(splash, false);
                }
            }
        }

        /// <summary>普攻半径内最近的另一敌人(卢安娜分裂箭)。</summary>
        Enemy NearbyEnemy(Vector2 origin, Enemy exclude)
        {
            float r2 = 2.6f * 2.6f;
            Enemy best = null;
            float bestSq = r2;
            for (int i = 0; i < EnemyRegistry.All.Count; i++)
            {
                Enemy e = EnemyRegistry.All[i];
                if (e == null || e == exclude || e.Data == null) continue;
                float sq = ((Vector2)e.transform.position - origin).sqrMagnitude;
                if (sq <= bestSq) { bestSq = sq; best = e; }
            }
            return best;
        }

        /// <summary>敌人死亡(非撞击消失)触发被动：击杀叠吸血(无尽饥渴/暴食胫甲)、蜕生回血、海克斯镜片叠距离。</summary>
        public void OnKill(Enemy enemy)
        {
            if (HasPassive(PassiveType.KillVamp) && omnivampKillStacks < 6)
            {
                omnivamp += 0.01f;
                omnivampKillStacks++;
            }
            if (HasPassive(PassiveType.ReapHeal)) Heal(maxHP * 0.05f);
            if (HasPassive(PassiveType.Longshot) && rangeBonus < 3f)
            {
                float add = Mathf.Min(3f - rangeBonus, 0.5f);
                rangeBonus += add;
            }
        }

        /// <summary>逐帧被动驱动(CombatSystem.Update 每帧调用)：日炎/无终恨意/狂徒脱战回血/亡者叠层/
        /// 猎魔人弹幕刷新/死亡之舞流血/灼烧DoT/冰霜之心与深渊面具光环。EditMode 测试可显式调用。</summary>
        public void TickPassives(float dt, Vector2 origin)
        {
            if (passiveMask == 0 && passiveType == PassiveType.None) return;
            timeSinceDamaged += dt;
            timeSinceAttack += dt;

            // 通用冷却计时
            spellbladeCd = Mathf.Max(0f, spellbladeCd - dt);
            energizedCd = Mathf.Max(0f, energizedCd - dt);
            stormSurgeCd = Mathf.Max(0f, stormSurgeCd - dt);
            ludenCd = Mathf.Max(0f, ludenCd - dt);
            lifelineCd = Mathf.Max(0f, lifelineCd - dt);
            msAmpRemaining = Mathf.Max(0f, msAmpRemaining - dt);
            msFervorRemaining = Mathf.Max(0f, msFervorRemaining - dt);
            asBuffRemaining = Mathf.Max(0f, asBuffRemaining - dt);
            apBuffRemaining = Mathf.Max(0f, apBuffRemaining - dt);
            if (natureMsRemaining > 0f)
            {
                natureMsRemaining -= dt;
                if (natureMsRemaining <= 0f) natureStacks = 0;
            }

            // 狂徒铠甲：脱战(5s未受伤)后每秒回复最大生命6%
            if (HasPassive(PassiveType.Warmogs) && timeSinceDamaged >= 5f && CurrentHP < maxHP)
                Heal(maxHP * 0.06f * dt);

            // 亡者的板甲：离开攻击1s后 每1s积1层气势(上限5)；普攻消耗见 OnAttackHit
            if (HasPassive(PassiveType.DeadMans) && timeSinceAttack > 1f)
            {
                deadmanStackTimer -= dt;
                if (deadmanStackTimer <= 0f)
                {
                    deadmanStacks = Mathf.Min(5, deadmanStacks + 1);
                    deadmanStackTimer = 1f;
                }
            }

            // 猎魔人弩箭：每6s刷新 3次必定暴击弹幕
            if (HasPassive(PassiveType.CritBarrage))
            {
                barrageTimer -= dt;
                if (barrageTimer <= 0f) { barrageCount = 3; barrageTimer = 6f; }
            }

            // 日炎圣盾“献祭”：每秒对周围2.8敌人 5+法强×0.15 魔法伤害
            if (HasPassive(PassiveType.Sunfire))
            {
                burnTimer -= dt;
                if (burnTimer <= 0f) { DamageSystem.CastMagicAoe(origin, 2.8f, 5f, 0.15f); burnTimer = 1f; }
            }
            // 无终恨意“苦楚”：每1.5s 对周围2.8敌人 8+法强×0.2 魔法伤害 并回复6生命
            if (HasPassive(PassiveType.EndlessHatred))
            {
                endlessTimer -= dt;
                if (endlessTimer <= 0f)
                {
                    DamageSystem.CastMagicAoe(origin, 2.8f, 8f, 0.2f);
                    Heal(6f);
                    endlessTimer = 1.5f;
                }
            }

            // 死亡之舞：3s 流血结算(真实伤害，不吃护盾/抗性)
            if (HasPassive(PassiveType.DeathDance) && bleedPool > 0f)
            {
                float loss = bleedPool * dt / 3f;
                bleedPool = Mathf.Max(0f, bleedPool - loss);
                if (loss > 0f)
                {
                    CurrentHP = Mathf.Max(0f, CurrentHP - loss);
                    GameEvents.RaiseHP(CurrentHP, maxHP);
                    timeSinceDamaged = 0f;
                }
            }

            // 兰德里的折磨 灼烧DoT
            if (burns.Count > 0)
            {
                for (int i = burns.Count - 1; i >= 0; i--)
                {
                    BurnDot dot = burns[i];
                    if (dot == null || dot.Enemy == null || dot.Enemy.Health <= 0f) { burns.RemoveAt(i); continue; }
                    dot.Remaining -= dt;
                    if (dot.Remaining <= 0f) { burns.RemoveAt(i); continue; }
                    dot.Enemy.TakeMagicDamage(dot.Dps * dt);
                }
            }

            // 冰霜之心/深渊面具 光环：范围内敌人 攻速×1.5 / 承魔伤×1.15，范围外复位
            if (HasPassive(PassiveType.FrozenHeart) || HasPassive(PassiveType.AbyssalMask))
            {
                var copy = new List<Enemy>(EnemyRegistry.All);
                for (int i = 0; i < copy.Count; i++)
                {
                    Enemy e = copy[i];
                    if (e == null || e.Data == null) continue;
                    float distSq = ((Vector2)e.transform.position - origin).sqrMagnitude;
                    e.attackIntervalMult = HasPassive(PassiveType.FrozenHeart) && distSq <= 4f * 4f ? 1.5f : 1f;
                    e.magicVulnMult = HasPassive(PassiveType.AbyssalMask) && distSq <= 4.5f * 4.5f ? 1.15f : 1f;
                }
            }
        }

        /// <summary>兰德里的折磨：为目标施加灼烧(刷新时间；每秒 目标最大生命1%+法强×0.05 魔法伤害)。</summary>
        public void ApplyBurn(Enemy enemy, float dps, float duration)
        {
            for (int i = 0; i < burns.Count; i++)
            {
                if (burns[i].Enemy == enemy)
                {
                    burns[i].Dps = dps;
                    burns[i].Remaining = duration;
                    return;
                }
            }
            burns.Add(new BurnDot { Enemy = enemy, Dps = dps, Remaining = duration });
        }

        /// <summary>通用移速增幅(暗夜收割者/风暴狂涌)。</summary>
        public void PushMoveAmp(float mult, float duration)
        {
            msAmpValue = mult;
            msAmpRemaining = Mathf.Max(msAmpRemaining, duration);
        }

        /// <summary>朔极之矛：取走并消费充能标记(魔法伤害×1.25 一次)。</summary>
        public bool TakeSpearBoost()
        {
            bool ready = spearBoost;
            spearBoost = false;
            return ready;
        }

        /// <summary>朔极之矛：魔法命中后 给下一次充能。</summary>
        public void SetSpearBoost() => spearBoost = true;

        /// <summary>风暴狂涌：每4s一次的附伤就绪标记。</summary>
        public bool StormSurgeReady => stormSurgeCd <= 0f;
        public void ConsumeStormSurge() => stormSurgeCd = 4f;

        /// <summary>卢登的回声：每2.5s一次的溅射附伤就绪标记。</summary>
        public bool LudenReady => ludenCd <= 0f;
        public void ConsumeLuden() => ludenCd = 2.5f;

        #endregion

        #region Active Items

        /// <summary>装备栏是否已满(最多 EquipmentSlotCount 件)。</summary>
        public bool IsEquipFull => EquippedCount >= EquipmentSlotCount;

        /// <summary>当前已装备数量(槽位非空数)。</summary>
        public int EquippedCount
        {
            get
            {
                int n = 0;
                for (int i = 0; i < _equipSlots.Length; i++) if (_equipSlots[i] != null) n++;
                return n;
            }
        }

        /// <summary>购买装备后自动装入首个空槽(主动/被动装备均可)；满则不放入。</summary>
        public bool TryAddEquip(ShopItemData item)
        {
            if (item == null) return false;
            for (int i = 0; i < _equipSlots.Length; i++)
            {
                if (_equipSlots[i] == null)
                {
                    _equipSlots[i] = new ActiveSlot { Item = item };
                    GameEvents.RaiseActiveSlotsChanged();
                    return true;
                }
            }
            return false; // 无空槽
        }

        /// <summary>出售装备：从装备栏移除(含属性与被动反向清除)。返回是否移除成功。</summary>
        public bool RemoveEquip(ShopItemData item)
        {
            if (item == null) return false;
            for (int i = 0; i < _equipSlots.Length; i++)
            {
                if (_equipSlots[i] != null && _equipSlots[i].Item == item)
                {
                    _equipSlots[i] = null;
                    RemoveBonus(item);
                    GameEvents.RaiseActiveSlotsChanged();
                    return true;
                }
            }
            return false;
        }

        /// <summary>交换两个装备栏槽位的位置(换键：把 3 槽装备换到 1 槽等)。</summary>
        public void SwapActiveSlots(int a, int b)
        {
            if (a == b || a < 0 || a >= _equipSlots.Length || b < 0 || b >= _equipSlots.Length) return;
            (_equipSlots[a], _equipSlots[b]) = (_equipSlots[b], _equipSlots[a]);
            GameEvents.RaiseActiveSlotsChanged();
        }

        /// <summary>尝试触发指定槽位的主动效果：需要槽内有主动装备且冷却就绪(受技能急速缩放)。</summary>
        public bool TryUseActive(int slotIndex, Vector2 origin)
        {
            if (slotIndex < 0 || slotIndex >= _equipSlots.Length) return false;
            ActiveSlot slot = _equipSlots[slotIndex];
            if (slot == null || slot.Item == null) return false;
            if (slot.Item.activeType == ActiveType.None) return false; // 被动装备无主动效果
            if (slot.Remaining > 0f) return false;

            if (!ActiveEffect.Run(this, slot.Item, origin)) return false;
            slot.Remaining = slot.Item.activeCooldown * HasteCooldownScale;
            GameEvents.RaiseActiveSlotsChanged();
            return true;
        }

        /// <summary>装备栏冷却与临时增益计时(CombatSystem.Update 每帧驱动)。
        /// 冷却显示秒数发生变化时广播 ActiveSlotsChanged，HUD 装备栏自动读秒刷新。</summary>
        public void TickActive(float dt)
        {
            bool displayChanged = false;
            for (int i = 0; i < _equipSlots.Length; i++)
            {
                ActiveSlot slot = _equipSlots[i];
                if (slot == null || slot.Remaining <= 0f) continue;
                int before = Mathf.CeilToInt(slot.Remaining);
                slot.Remaining = Mathf.Max(0f, slot.Remaining - dt);
                if (Mathf.CeilToInt(slot.Remaining) != before) displayChanged = true;
            }
            if (moveBurstRemaining > 0f) moveBurstRemaining = Mathf.Max(0f, moveBurstRemaining - dt);
            if (displayChanged) GameEvents.RaiseActiveSlotsChanged();
        }

        /// <summary>触发舒瑞娅的狂想曲 移速爆发(供 ActiveEffect 调用)。</summary>
        public void StartMoveBurst(float seconds) => moveBurstRemaining = Mathf.Max(moveBurstRemaining, seconds);

        internal static float MoveBurstSpeedMult => 1.6f;
        internal static float MoveBurstDuration => 5f;
        internal const float RedemptionHealPct = 0.12f;
        internal static float AoeBlastRadius => 3.5f;
        internal static float AoeBlastApRatio => 0.6f;
        internal static float ManaMeldCost => 12f;
        internal static float ManaMeldHealPct => 0.1f;

        #endregion

        bool _regenRunning;

        /// <summary>启动每秒生命回复循环；防重复启动(回合重置/锻体加 HPRegen 后仍只跑一条)。</summary>
        public void StartRegenLoop()
        {
            if (_regenRunning) return;
            _regenRunning = true;
            StartCoroutine(RegenLoop());
        }

        IEnumerator RegenLoop()
        {
            var wait = new WaitForSeconds(RegenTickSeconds);
            while (true)
            {
                yield return wait;
                TickRegen();
                RechargeMana();
            }
        }

        /// <summary>被动法术驱动帧步进(CombatSystem.Update 每帧调用)：奥术弹冷却受技能急速缩放，释放消耗法力。
        /// 法力不足或范围内无目标时提前重试，不空消耗。</summary>
        public void TickSpell(float dt, Vector2 origin)
        {
            if (!HasPassive(PassiveType.ArcaneBolt)) return;
            spellCooldown -= dt;
            if (spellCooldown > 0f) return;

            Enemy target = EnemyRegistry.Nearest(origin, Range);
            if (target == null) { spellCooldown = AbortRetryInterval; return; }
            if (!TrySpendMana(ArcaneManaCost)) { spellCooldown = AbortRetryInterval; return; }

            DamageSystem.CastMagic(target, ArcaneBaseDamage, ArcaneApRatio);
            spellCooldown = ArcaneInterval * HasteCooldownScale;
        }

        public void AddGold(int amount)
        {
            Gold += amount;
            GameEvents.RaiseGold(Gold);
        }

        /// <summary>回合末自动入库金币：计入 Gold 与 BankedGold 累计，并广播入库事件(HUD 显示)。</summary>
        public void BankGold(int amount)
        {
            if (amount <= 0) return;
            BankedGold += amount;
            AddGold(amount);
            GameEvents.RaiseGoldBanked(BankedGold);
        }

        /// <summary>记录一枚符文(锻体选定卡牌的效果)，供暂停面板查看。</summary>
        public void RecordRune(string name, string desc, Color color)
        {
            Runes.Add(new RuneInfo { Name = name, Desc = desc, Color = color });
        }

        public void NotifyKill() => Kills++;

        public void ApplyBonus(ShopItemData item) => ApplyBonus(item, 1f);

        /// <summary>全属性增益：multiplier 为倍率(锻体白1x/金1.5x/彩2x；商店/符文为1x)。
        /// 多属性装备逐项应用；AttackSpeed 是乘算词条，用 value^multiplier 使高阶收益递增(更强更快)，其余加算。</summary>
        public void ApplyBonus(ShopItemData item, float multiplier)
        {
            if (item == null) return;
            if (item.IsMulti)
            {
                foreach (var b in item.bonuses) ApplyStat(b.type, b.value, multiplier);
            }
            else
            {
                ApplyStat(item.statType, item.addValue, multiplier);
            }
            if (item.passiveType != PassiveType.None)
            {
                passiveType = item.passiveType;              // 最后绑定(兼容展示/旧测试)
                passiveMask |= 1UL << (int)item.passiveType; // 位掩码：多件被动装备可共存
            }
            GameEvents.RaiseHP(CurrentHP, maxHP);
        }

        void ApplyStat(StatType type, float value, float multiplier)
        {
            switch (type)
            {
                case StatType.AttackDamage: damage += value * multiplier; break;
                case StatType.AbilityPower: abilityPower += value * multiplier; break;
                case StatType.AttackSpeed: attackInterval *= Mathf.Pow(value, multiplier); break;
                case StatType.CritChance: critChance += value * multiplier; break;
                case StatType.CritDamage: critMultiplier += value * multiplier; break;
                case StatType.ArmorPen: armorPen += value * multiplier; break;
                case StatType.MagicPen: magicPen += value * multiplier; break;
                case StatType.Omnivamp: omnivamp += value * multiplier; break;
                case StatType.MaxHP:
                    float bonus = value * multiplier;
                    maxHP += bonus;
                    CurrentHP += bonus;
                    break;
                case StatType.HPRegen: hpRegen += value * multiplier; break;
                case StatType.Armor: armor += value * multiplier; break;
                case StatType.MagicResist: magicResist += value * multiplier; break;
                case StatType.HealShieldPower: healShieldPower += value * multiplier; break;
                case StatType.AbilityHaste: abilityHaste += value * multiplier; break;
                case StatType.MoveSpeed: moveSpeed += value * multiplier; break;
                case StatType.AttackRange: range += value * multiplier; break;
                case StatType.Size: size = Mathf.Max(0.1f, size + value * multiplier); break;
                case StatType.Mana:
                    maxMana += value * multiplier;
                    mana += value * multiplier; // 装备法力值同时提高上限与当前值
                    GameEvents.RaiseMana(mana, maxMana);
                    break;
                case StatType.Tenacity: tenacity += value * multiplier; break;
                default: break;
            }
        }

        /// <summary>出售装备时的属性反向清除(与 ApplyBonus 互逆)：减去加算属性、攻速除法还原，
        /// 生命/法力扣减时钳制当前值不越上限；清被动位掩码，最后绑定一致时重置 passiveType。</summary>
        public void RemoveBonus(ShopItemData item)
        {
            if (item == null) return;
            if (item.IsMulti)
            {
                foreach (var b in item.bonuses) RemoveStat(b.type, b.value);
            }
            else
            {
                RemoveStat(item.statType, item.addValue);
            }
            if (item.passiveType != PassiveType.None)
            {
                passiveMask &= ~(1UL << (int)item.passiveType);
                if (passiveType == item.passiveType) passiveType = PassiveType.None;
            }
            GameEvents.RaiseHP(CurrentHP, maxHP);
        }

        void RemoveStat(StatType type, float value)
        {
            switch (type)
            {
                case StatType.AttackDamage: damage = Mathf.Max(0f, damage - value); break;
                case StatType.AbilityPower: abilityPower = Mathf.Max(0f, abilityPower - value); break;
                case StatType.AttackSpeed: attackInterval = Mathf.Max(0.05f, attackInterval / value); break;
                case StatType.CritChance: critChance = Mathf.Max(0f, critChance - value); break;
                case StatType.CritDamage: critMultiplier = Mathf.Max(1f, critMultiplier - value); break;
                case StatType.ArmorPen: armorPen = Mathf.Max(0f, armorPen - value); break;
                case StatType.MagicPen: magicPen = Mathf.Max(0f, magicPen - value); break;
                case StatType.Omnivamp: omnivamp = Mathf.Max(0f, omnivamp - value); break;
                case StatType.MaxHP:
                    maxHP = Mathf.Max(0f, maxHP - value);
                    if (CurrentHP > maxHP) CurrentHP = maxHP;
                    break;
                case StatType.HPRegen: hpRegen = Mathf.Max(0f, hpRegen - value); break;
                case StatType.Armor: armor = Mathf.Max(0f, armor - value); break;
                case StatType.MagicResist: magicResist = Mathf.Max(0f, magicResist - value); break;
                case StatType.HealShieldPower: healShieldPower = Mathf.Max(0f, healShieldPower - value); break;
                case StatType.AbilityHaste: abilityHaste = Mathf.Max(0f, abilityHaste - value); break;
                case StatType.MoveSpeed: moveSpeed = Mathf.Max(0f, moveSpeed - value); break;
                case StatType.AttackRange: range = Mathf.Max(0f, range - value); break;
                case StatType.Size: size = Mathf.Max(0.1f, size - value); break;
                case StatType.Mana:
                    maxMana = Mathf.Max(0f, maxMana - value);
                    if (mana > maxMana) mana = maxMana;
                    GameEvents.RaiseMana(mana, maxMana);
                    break;
                case StatType.Tenacity: tenacity = Mathf.Max(0f, tenacity - value); break;
                default: break;
            }
        }
    }
}