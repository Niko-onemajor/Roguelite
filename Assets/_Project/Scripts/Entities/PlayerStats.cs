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

        // ── 主动装备栏（数字键 1-0 触发，购买带主动效果装备自动入槽）──
        /// <summary>主动栏槽位数（1-0 数字键）。</summary>
        public const int ActiveSlotCount = 10;
        readonly ActiveSlot[] _activeSlots = new ActiveSlot[ActiveSlotCount];

        float moveBurstRemaining;                     // 舒瑞娅 移速爆发剩余秒数

        public IReadOnlyList<ActiveSlot> ActiveSlots => _activeSlots;

        const float RegenTickSeconds = 1f;

        public float CurrentHP { get; private set; }
        public int Gold { get; private set; }
        public int Kills { get; private set; }

        /// <summary>战斗实际面板攻击力 = 基础攻击力 + 被动动态加成(如霸王血铠 专横/报复)。</summary>
        public float TotalDamage
        {
            get
            {
                if (passiveType != PassiveType.Tyrant) return damage;
                float bonusHp = Mathf.Max(0f, maxHP - BaseHP); // 额外生命值
                float missingPct = maxHP > 0f ? Mathf.Clamp01(1f - CurrentHP / maxHP) : 0f;
                return damage + bonusHp * TyrantHpToDamage + damage * missingPct * TyrantMissingToDamage;
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
            GameEvents.RaiseHP(CurrentHP, maxHP);
            GameEvents.RaiseGold(Gold);
            GameEvents.RaiseGoldBanked(0);
            StartRegenLoop();
        }

        public void TakeDamage(float dmg) => ReceiveDamage(dmg, magicDamage: false);

        /// <summary>魔法伤害：按魔抗减伤后走统一受击管线。</summary>
        public void TakeMagicDamage(float dmg) => ReceiveDamage(dmg, magicDamage: true);

        void ReceiveDamage(float dmg, bool magicDamage)
        {
            float resist = magicDamage ? magicResist : armor;
            float reduced = dmg * (100f / (100f + Mathf.Max(0f, resist)));
            CurrentHP = Mathf.Max(0f, CurrentHP - reduced);
            GameEvents.RaiseHP(CurrentHP, maxHP);
            var flash = GetComponent<HitFlash>(); // 玩家受击反馈：闪红
            if (flash != null) flash.Flash(Color.red, 0.12f);
        }

        /// <summary>治疗：受治疗与护盾强度加成，不溢出当前生命上限。</summary>
        public void Heal(float amount)
        {
            if (amount <= 0f) return;
            float healed = amount * (1f + healShieldPower);
            CurrentHP = Mathf.Min(maxHP, CurrentHP + healed);
            GameEvents.RaiseHP(CurrentHP, maxHP);
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
        }

        /// <summary>尝试消耗法力，不足返回 false(施法失败，调用方应中断本次施放)。</summary>
        public bool TrySpendMana(float cost)
        {
            if (cost <= 0f) return true;
            if (mana < cost) return false;
            mana -= cost;
            return true;
        }

        /// <summary>技能急速 → 冷却缩放(0 急速=1.0；急速越高系数越小，100 急速≈半冷却)。</summary>
        public float HasteCooldownScale => 100f / (100f + abilityHaste);

        /// <summary>有效移速(舒瑞娅 移速爆发期间提升)。</summary>
        public float EffectiveMoveSpeed => moveSpeed * (moveBurstRemaining > 0f ? MoveBurstSpeedMult : 1f);

        #region Active Items

        /// <summary>购买带主动效果的装备后自动入首个空槽；满则不放入。</summary>
        public bool TryAddActive(ShopItemData item)
        {
            if (item == null || item.activeType == ActiveType.None) return false;
            for (int i = 0; i < _activeSlots.Length; i++)
            {
                if (_activeSlots[i] == null)
                {
                    _activeSlots[i] = new ActiveSlot { Item = item };
                    GameEvents.RaiseActiveSlotsChanged();
                    return true;
                }
            }
            return false; // 无空槽
        }

        /// <summary>交换两个主动槽位的位置(换键：把 3 槽装备换到 1 槽等)。</summary>
        public void SwapActiveSlots(int a, int b)
        {
            if (a == b || a < 0 || a >= _activeSlots.Length || b < 0 || b >= _activeSlots.Length) return;
            (_activeSlots[a], _activeSlots[b]) = (_activeSlots[b], _activeSlots[a]);
            GameEvents.RaiseActiveSlotsChanged();
        }

        /// <summary>尝试触发指定槽位的主动效果：需要冷却就绪(受技能急速缩放)。</summary>
        public bool TryUseActive(int slotIndex, Vector2 origin)
        {
            if (slotIndex < 0 || slotIndex >= _activeSlots.Length) return false;
            ActiveSlot slot = _activeSlots[slotIndex];
            if (slot == null) return false;
            if (slot.Remaining > 0f) return false;

            if (!ActiveEffect.Run(this, slot.Item, origin)) return false;
            slot.Remaining = slot.Item.activeCooldown * HasteCooldownScale;
            GameEvents.RaiseActiveSlotsChanged();
            return true;
        }

        /// <summary>主动栏冷却与临时增益计时(CombatSystem.Update 每帧驱动)。
        /// 冷却显示秒数发生变化时广播 ActiveSlotsChanged，HUD 主动栏自动读秒刷新。</summary>
        public void TickActive(float dt)
        {
            bool displayChanged = false;
            for (int i = 0; i < _activeSlots.Length; i++)
            {
                ActiveSlot slot = _activeSlots[i];
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
            if (passiveType != PassiveType.ArcaneBolt) return;
            spellCooldown -= dt;
            if (spellCooldown > 0f) return;

            Enemy target = EnemyRegistry.Nearest(origin, range);
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
            if (item.passiveType != PassiveType.None) passiveType = item.passiveType; // 绑定战斗被动(血铠 专横/报复)
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
                    break;
                case StatType.Tenacity: tenacity += value * multiplier; break;
                default: break;
            }
        }
    }
}