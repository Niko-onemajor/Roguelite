using NUnit.Framework;
using Roguelite;
using UnityEngine;

namespace Roguelite.Tests
{
    /// <summary>装备被动挂钩测试：逐件验证 GameConfig 中已绑定被动在 PlayerStats/Enemy 上的战斗行为。</summary>
    public class PassiveCombatTests
    {
        class TestEnemy : Enemy
        {
            protected override void Behavior(float dt, PlayerController player) { }
        }

        GameObject playerGo;
        PlayerStats stats;

        [SetUp]
        public void SetUp()
        {
            playerGo = new GameObject("Player", typeof(PlayerStats));
            stats = playerGo.GetComponent<PlayerStats>();
            stats.ResetForRun();
            PlayerStats.Instance = stats;
            EnemyRegistry.Clear();
        }

        [TearDown]
        public void TearDown()
        {
            EnemyRegistry.Clear();
            if (PlayerStats.Instance == stats) PlayerStats.Instance = null;
            Object.DestroyImmediate(playerGo);
            GameEvents.ClearAll();
            PoolManager.ClearAll();
            foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
                if (go.name.StartsWith("e") || go.name.StartsWith("Test"))
                    Object.DestroyImmediate(go);
        }

        static EnemyData Data(float hp, float armor = 0f, float magicResist = 0f)
        {
            var d = ScriptableObject.CreateInstance<EnemyData>();
            d.maxHP = hp;
            d.armor = armor;
            d.magicResist = magicResist;
            d.goldMin = 1;
            d.goldMax = 1;
            d.scale = 1f;
            return d;
        }

        Enemy Spawn(Vector3 pos, float hp, float armor = 0f, float magicResist = 0f)
        {
            var go = new GameObject("e", typeof(BoxCollider2D));
            go.transform.position = pos;
            var e = go.AddComponent<TestEnemy>();
            e.Init(Data(hp, armor, magicResist));
            EnemyRegistry.Register(e); // EditMode 不触发 OnEnable，需显式注册(供光环/溅射/反射索敌)
            return e;
        }

        /// <summary>装备一件被动(无属性词条)，每用例独立实例保证被动互不干扰。</summary>
        void Apply(PassiveType pt)
        {
            var item = ScriptableObject.CreateInstance<ShopItemData>();
            item.displayName = "测试装备";
            item.basePrice = 20;
            item.passiveType = pt;
            stats.ApplyBonus(item);
        }

        [Test]
        public void Deathcap_Boosts_TotalAbilityPower()
        {
            var item = ScriptableObject.CreateInstance<ShopItemData>();
            item.displayName = "死帽";
            item.passiveType = PassiveType.Deathcap;
            item.bonuses.Add(new StatBonus(StatType.AbilityPower, 9f));
            stats.ApplyBonus(item);

            Assert.That(stats.TotalAbilityPower, Is.EqualTo(12.6f).Within(0.001f)); // 9 × 1.4
        }

        [Test]
        public void VoidInfusion_ExtraHp_Converts_To_AbilityPower()
        {
            var item = ScriptableObject.CreateInstance<ShopItemData>();
            item.displayName = "裂隙制造者";
            item.passiveType = PassiveType.VoidInfusion;
            item.bonuses.Add(new StatBonus(StatType.AbilityPower, 5f));
            item.bonuses.Add(new StatBonus(StatType.MaxHP, 35f)); // 额外生命 35
            stats.ApplyBonus(item);

            Assert.That(stats.TotalAbilityPower, Is.EqualTo(5f + 35f * 0.06f).Within(0.001f));
        }

        [Test]
        public void Spellblade_Procs_Every_1_5s()
        {
            Apply(PassiveType.Spellblade);
            stats.damage = 10f;
            Enemy e = Spawn(Vector3.zero, 1000f);
            float hp0 = e.Health;

            DamageSystem.HitEnemy(e, 10f, 0f);            // 普攻10 + 咒刃附伤10
            float hp1 = e.Health;
            DamageSystem.HitEnemy(e, 10f, 0f);            // 冷却中：仅普攻10
            float hp2 = e.Health;
            stats.TickPassives(1.5f, Vector3.zero);       // 冷却结束
            DamageSystem.HitEnemy(e, 10f, 0f);            // 再次触发射(普攻10+附伤10)
            float hp3 = e.Health;

            Assert.That(hp0 - hp1, Is.EqualTo(20f).Within(0.01f));
            Assert.That(hp1 - hp2, Is.EqualTo(10f).Within(0.01f));
            Assert.That(hp2 - hp3, Is.EqualTo(20f).Within(0.01f));
        }

        [Test]
        public void RuinKing_Drains_6pct_Current_Hp()
        {
            Apply(PassiveType.RuinKing);
            Enemy e = Spawn(Vector3.zero, 100f);

            stats.OnAttackHit(e, 10f, Vector3.zero); // 附带当前生命6% = 6

            Assert.That(e.Health, Is.EqualTo(94f).Within(0.01f));
        }

        [Test]
        public void BlackCleaver_Shreds_Armor()
        {
            Apply(PassiveType.BlackCleaver);
            stats.armorPen = 0f;
            Enemy e = Spawn(Vector3.zero, 500f, armor: 15f);

            DamageSystem.HitEnemy(e, 100f, 0f); // 命中后 on-hit 破甲10(有效护甲15→100*100/115)
            DamageSystem.HitEnemy(e, 100f, 0f); // 第二次(有效护甲5→100*100/105)

            Assert.That(500f - e.Health, Is.EqualTo(100f * 100f / 115f + 100f * 100f / 105f).Within(0.02f));
        }

        [Test]
        public void DeathDance_Converts_30pct_To_Bleed_And_Ticks()
        {
            Apply(PassiveType.DeathDance);
            stats.TakeDamage(100f); // 无护甲 → 70 即时 + 30 流血池
            Assert.That(stats.CurrentHP, Is.EqualTo(50f).Within(0.01f)); // 120-70

            stats.TickPassives(1.5f, Vector3.zero); // 流血 30×(1.5/3)=15
            Assert.That(stats.CurrentHP, Is.EqualTo(35f).Within(0.02f));
        }

        [Test]
        public void Lifeline_Shields_At_Low_Hp()
        {
            Apply(PassiveType.Lifeline);
            stats.TakeDamage(90f); // 120→30(<30%) 触发 35% 最大生命护盾

            Assert.That(stats.CurrentHP, Is.EqualTo(30f).Within(0.01f));
            Assert.That(stats.Shield, Is.EqualTo(42f).Within(0.01f)); // 120×0.35

            stats.TakeDamage(10f); // 先扣护盾
            Assert.That(stats.Shield, Is.EqualTo(32f).Within(0.01f));
            Assert.That(stats.CurrentHP, Is.EqualTo(30f).Within(0.01f));
        }

        [Test]
        public void Steelcaps_Reduces_Physical_Damage()
        {
            Apply(PassiveType.Steelcaps);
            stats.TakeDamage(100f); // 物理 ×0.88

            Assert.That(stats.CurrentHP, Is.EqualTo(120f - 88f).Within(0.01f));
        }

        [Test]
        public void Thornmail_Reflects_30pct_Magic_To_Nearest()
        {
            Apply(PassiveType.Thornmail);
            Enemy target = Spawn(new Vector3(1f, 0f, 0f), 500f);

            stats.TakeDamage(100f); // 反弹 30 魔法给最近敌人

            Assert.That(500f - target.Health, Is.EqualTo(30f).Within(0.01f));
        }

        [Test]
        public void Liandry_Burn_DoT_Ticks()
        {
            Apply(PassiveType.LiandryBurn);
            Enemy e = Spawn(Vector3.zero, 500f);

            stats.ApplyBurn(e, 10f, 3f);
            stats.TickPassives(1f, Vector3.zero);

            Assert.That(500f - e.Health, Is.EqualTo(10f).Within(0.02f)); // 每秒 10 魔伤
        }

        [Test]
        public void Warmogs_Regen_Only_After_5s_Out_Of_Combat()
        {
            Apply(PassiveType.Warmogs);
            stats.TakeDamage(60f); // 120→60，重置脱战计时
            stats.TickPassives(4.9f, Vector3.zero);
            Assert.That(stats.CurrentHP, Is.EqualTo(60f).Within(0.01f)); // 未满5s不回复

            stats.TickPassives(0.2f, Vector3.zero); // 脱战5.1s → 每秒6%
            Assert.That(stats.CurrentHP, Is.GreaterThan(60f));
        }

        [Test]
        public void Sunfire_Burns_Nearby_Enemies()
        {
            Apply(PassiveType.Sunfire);
            Enemy near = Spawn(new Vector3(1f, 0f, 0f), 500f);
            Enemy far = Spawn(new Vector3(20f, 0f, 0f), 500f);

            stats.TickPassives(1f, Vector3.zero); // 首秒立即献祭

            Assert.That(500f - near.Health, Is.EqualTo(5f).Within(0.02f)); // 5+法强0×0.15
            Assert.That(far.Health, Is.EqualTo(500f));
        }

        [Test]
        public void Hurricane_Splits_50pct_To_Nearby()
        {
            Apply(PassiveType.Hurricane);
            Enemy main = Spawn(Vector3.zero, 1000f);
            Enemy other = Spawn(new Vector3(1f, 0f, 0f), 1000f);

            stats.OnAttackHit(main, 20f, Vector3.zero);

            Assert.That(other.Health, Is.EqualTo(990f).Within(0.01f)); // 附带 50% 伤害
            Assert.That(main.Health, Is.EqualTo(1000f)); // 主目标不受 on-hit 附伤
        }

        [Test]
        public void FrozenHeart_Slows_Attack_Of_Nearby()
        {
            Apply(PassiveType.FrozenHeart);
            Enemy near = Spawn(new Vector3(1f, 0f, 0f), 100f);
            Enemy far = Spawn(new Vector3(10f, 0f, 0f), 100f);

            stats.TickPassives(0.1f, Vector3.zero);

            Assert.That(near.attackIntervalMult, Is.EqualTo(1.5f)); // 攻速间隔×1.5
            Assert.That(far.attackIntervalMult, Is.EqualTo(1f));    // 范围外复位
        }

        [Test]
        public void KillVamp_Stacks_On_Kill_Up_To_6()
        {
            Apply(PassiveType.KillVamp);
            var e = Spawn(Vector3.zero, 100f);

            for (int i = 0; i < 6; i++) stats.OnKill(e);

            Assert.That(stats.omnivamp, Is.EqualTo(0.06f).Within(0.001f)); // 每杀+1%，上限6%
            stats.OnKill(e); // 超上限不再叠加
            Assert.That(stats.omnivamp, Is.EqualTo(0.06f).Within(0.001f));
        }

        [Test]
        public void CritBarrage_Refills_3_Guaranteed_Crits_Every_6s()
        {
            Apply(PassiveType.CritBarrage);
            stats.TickPassives(0.01f, Vector3.zero); // 初始立即充能

            Assert.That(stats.BarrageCount, Is.EqualTo(3));
        }

        [Test]
        public void GreedTreads_Scales_Damage_By_Hp_Threshold()
        {
            var item = ScriptableObject.CreateInstance<ShopItemData>();
            item.displayName = "贪婪胫甲";
            item.passiveType = PassiveType.GreedTreads;
            item.bonuses.Add(new StatBonus(StatType.MoveSpeed, 1f));
            item.bonuses.Add(new StatBonus(StatType.Omnivamp, 0.02f));
            stats.ApplyBonus(item);

            Assert.That(stats.TotalDamage, Is.EqualTo(10f * 1.08f).Within(0.001f)); // ≥50% 伤害+8%

            stats.TakeDamage(70f); // → 50/120 <50%
            Assert.That(stats.TotalDamage, Is.EqualTo(10f).Within(0.001f));
        }

        [Test]
        public void Energized_Procs_Magic_Damage_Every_2_5s()
        {
            Apply(PassiveType.Energized);
            stats.damage = 10f;
            Enemy e = Spawn(Vector3.zero, 500f);

            stats.OnAttackHit(e, 10f, Vector3.zero); // 附 10+10×0.3=13 魔伤

            Assert.That(500f - e.Health, Is.EqualTo(13f).Within(0.02f));
        }

        [Test]
        public void Cleanse_Active_Heals_30pct_Max_Hp()
        {
            var item = ScriptableObject.CreateInstance<ShopItemData>();
            item.displayName = "米凯尔的祝福";
            item.activeType = ActiveType.Cleanse;
            item.activeCooldown = 15f;
            stats.ApplyBonus(item);
            stats.TryAddEquip(item); // 商店购买路径会自动入槽，测试显式补入

            stats.TakeDamage(60f); // → 60/120
            stats.TryUseActive(0, Vector3.zero);

            Assert.That(stats.CurrentHP, Is.EqualTo(96f).Within(0.02f)); // 治疗 30% 最大生命
        }

        [Test]
        public void FrostBite_Slows_Target_On_Hit()
        {
            Apply(PassiveType.FrostBite);
            stats.damage = 10f;
            Enemy e = Spawn(Vector3.zero, 100f);

            stats.OnAttackHit(e, 10f, Vector3.zero);

            Assert.That(e.speedMult, Is.EqualTo(0.65f)); // 减速35%
        }

        [Test]
        public void CritMissile_Rune_Launches_Magic_Missiles_On_Crit()
        {
            Apply(PassiveType.CritMissile);
            stats.damage = 100f;
            stats.abilityPower = 50f;
            Enemy e = Spawn(Vector3.zero, 1000f);

            DamageSystem.HitEnemy(e, 100f, 1f); // 必定暴击

            // 暴击 200 + 飞弹1枚(11 + 100×0.07 + 50×0.1 = 23) = 223
            Assert.That(1000f - e.Health, Is.EqualTo(223f).Within(0.05f));
        }

        [Test]
        public void CritMissile_Higher_Crit_Chance_Fires_More_Missiles()
        {
            Apply(PassiveType.CritMissile);
            stats.damage = 100f;
            stats.abilityPower = 50f;
            const float oneMissile = 11f + 100f * 0.07f + 50f * 0.1f; // 23

            stats.critChance = 0.5f; // ≤66.6% → 2 枚
            Enemy e2 = Spawn(new Vector3(0f, -3f, 0f), 1000f);
            DamageSystem.HitEnemy(e2, 100f, 1f);
            Assert.That(1000f - e2.Health, Is.EqualTo(200f + 2f * oneMissile).Within(0.05f));

            stats.critChance = 0.8f; // >66.6% → 3 枚
            Enemy e3 = Spawn(new Vector3(0f, -6f, 0f), 1000f);
            DamageSystem.HitEnemy(e3, 100f, 1f);
            Assert.That(1000f - e3.Health, Is.EqualTo(200f + 3f * oneMissile).Within(0.05f));
        }
    }
}