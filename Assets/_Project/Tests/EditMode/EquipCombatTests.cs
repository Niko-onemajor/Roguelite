using System.Collections.Generic;
using NUnit.Framework;
using Roguelite;
using UnityEngine;

namespace Roguelite.Tests
{
    /// <summary>装备战斗挂钩：护甲穿透/全能吸血/暴击伤害倍率/攻速词条/魔法减伤/生命回复/法力韧性/空符文池跳过。</summary>
    public class EquipCombatTests
    {
        class TestEnemy : Enemy
        {
            protected override void Behavior(float dt, PlayerController player) { }
        }

        class CountingWeapon : PlayerWeapon
        {
            public int Fired;
            protected override void Fire(Vector2 origin, Vector2 dir, float damage, float critChance, float statRange) => Fired++;
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
            EnemyRegistry.Register(e); // EditMode 不触发 OnEnable，需显式注册(供 TickSpell 索敌)
            return e;
        }

        [Test]
        public void ArmorPen_Reduces_Effective_Armor()
        {
            Enemy e = Spawn(Vector3.zero, 500f, armor: 15f);
            Enemy plain = Spawn(new Vector3(50f, 0f, 0f), 500f, armor: 15f);

            // 基准：无穿透 → 100*100/115
            DamageSystem.HitEnemy(plain, 100f, 0f);
            float plainLoss = 500f - plain.Health;

            // 玩家护甲穿透 10 → 有效护甲 5 → 100*100/105
            stats.armorPen = 10f;
            DamageSystem.HitEnemy(e, 100f, 0f);

            Assert.That(plainLoss, Is.EqualTo(100f * 100f / 115f).Within(0.01f));
            Assert.That(500f - e.Health, Is.EqualTo(100f * 100f / 105f).Within(0.01f));
        }

        [Test]
        public void Omnivamp_Heals_Player_By_Damage_Dealt()
        {
            stats.TakeDamage(50f); // 降到 70/120，确保回血可感知
            stats.omnivamp = 0.5f;
            Enemy e = Spawn(Vector3.zero, 1000f);
            float hp = stats.CurrentHP;

            DamageSystem.HitEnemy(e, 20f, 0f); // 20 伤害 → 吸血 10

            Assert.That(stats.CurrentHP, Is.EqualTo(hp + 10f).Within(0.01f));
        }

        [Test]
        public void HitEnemy_Uses_Player_CritMultiplier()
        {
            stats.critMultiplier = 3f; // 暴击伤害 300%
            Enemy e = Spawn(Vector3.zero, 1000f);

            DamageSystem.HitEnemy(e, 100f, critChance: 1f); // 必暴 → 300

            Assert.That(e.Health, Is.EqualTo(700f).Within(0.01f));
        }

        [Test]
        public void MagicDamage_Reduced_By_MagicResist_But_Physical_Ignores_It()
        {
            stats.magicResist = 100f; // 魔抗 100 → 魔法减半
            float hp = stats.CurrentHP;

            stats.TakeMagicDamage(30f); // 走魔抗: 30*100/(100+100)=15
            float afterMagic = stats.CurrentHP;
            stats.TakeDamage(100f);     // 物理按护甲(0) → 掉 100
            float afterPhysical = stats.CurrentHP;

            Assert.That(hp - afterMagic, Is.EqualTo(15f).Within(0.01f));
            Assert.That(afterMagic - afterPhysical, Is.EqualTo(100f).Within(0.01f));
        }

        [Test]
        public void TickRegen_Heals_When_Not_Full()
        {
            stats.hpRegen = 3f;
            stats.TakeDamage(30f);
            float hp = stats.CurrentHP;

            stats.TickRegen();

            Assert.That(stats.CurrentHP, Is.EqualTo(hp + 3f).Within(0.001f));
        }

        [Test]
        public void Heal_Scales_With_HealShieldPower()
        {
            stats.healShieldPower = 1f; // 治疗强度 +100%
            stats.TakeDamage(60f);
            float hp = stats.CurrentHP;

            stats.Heal(10f);

            Assert.That(stats.CurrentHP, Is.EqualTo(hp + 20f).Within(0.001f));
        }

        [Test]
        public void ManaTenacity_Apply_And_Describe()
        {
            var item = ScriptableObject.CreateInstance<ShopItemData>();
            item.displayName = "法力+韧性";
            item.basePrice = 20;
            item.bonuses.Add(new StatBonus(StatType.Mana, 5f));
            item.bonuses.Add(new StatBonus(StatType.Tenacity, 0.5f));

            stats.ApplyBonus(item);

            Assert.That(stats.mana, Is.EqualTo(5f));
            Assert.That(stats.tenacity, Is.EqualTo(0.5f));
            Assert.That(StatText.Describe(item), Does.Contain("法力 +5"));
            Assert.That(StatText.Describe(item), Does.Contain("韧性 +50%"));
        }

        [Test]
        public void Weapon_Tick_Respects_Player_AttackSpeed()
        {
            var wd = ScriptableObject.CreateInstance<WeaponData>();
            wd.type = WeaponType.Ranged;
            wd.attackInterval = 0.8f;
            var w = playerGo.AddComponent<CountingWeapon>();
            w.Equip(wd);

            // 攻速词条: interval = 0.8 * (0.4/0.8) = 0.4s
            w.Tick(0.1f, Vector2.zero, Vector2.right, 10f, 0f, 6f, statAttackInterval: 0.4f);
            Assert.That(w.Fired, Is.EqualTo(1));
            w.Tick(0.3f, Vector2.zero, Vector2.right, 10f, 0f, 6f, statAttackInterval: 0.4f);
            Assert.That(w.Fired, Is.EqualTo(1)); // 冷却未到
            w.Tick(0.1f, Vector2.zero, Vector2.right, 10f, 0f, 6f, statAttackInterval: 0.4f);
            Assert.That(w.Fired, Is.EqualTo(2)); // 0.5s 内发射 2 发

            // 无攻速词条(基准)：间隔 0.8s，同样的 0.5s 只能 1 发
            var w2 = playerGo.AddComponent<CountingWeapon>();
            w2.Equip(wd);
            w2.Tick(0.5f, Vector2.zero, Vector2.right, 10f, 0f, 6f, statAttackInterval: 0.8f);
            Assert.That(w2.Fired, Is.EqualTo(1));
        }

        [Test]
        public void OverlordArmor_Tyrant_Passive_Grants_AttackDamage()
        {
            var item = ScriptableObject.CreateInstance<ShopItemData>();
            item.displayName = "霸王血铠";
            item.basePrice = 41;
            item.bonuses.Add(new StatBonus(StatType.AttackDamage, 5f));
            item.bonuses.Add(new StatBonus(StatType.MaxHP, 55f));
            item.passiveType = PassiveType.Tyrant;

            stats.ApplyBonus(item);

            // 战斗挂钩：被动类型绑定 + 专横(额外生命值 2.5%→攻击力)
            Assert.That(stats.passiveType, Is.EqualTo(PassiveType.Tyrant));
            Assert.That(stats.damage, Is.EqualTo(15f).Within(0.001f)); // 基础 10 + 5
            Assert.That(stats.maxHP, Is.EqualTo(175f).Within(0.001f)); // 120 + 55
            Assert.That(stats.TotalDamage, Is.EqualTo(15f + 55f * 0.025f).Within(0.001f)); // 满血：只有专横

            // 报复：损失 50% 生命 → 攻击力 +12% × 已损失比
            stats.TakeDamage(stats.maxHP * 0.5f);
            Assert.That(stats.TotalDamage, Is.EqualTo(15f + 55f * 0.025f + 15f * 0.5f * 0.12f).Within(0.02f));
        }

        [Test]
        public void Empty_Rune_Pool_OpenOffer_AutoSkips()
        {
            var runeGo = new GameObject("Rune");
            try
            {
                var rune = runeGo.AddComponent<RuneSystem>();
                rune.pool = null; // 符文池未配置(待后续补充)

                rune.OpenOffer();

                Assert.That(rune.IsAwaitingChoice, Is.False); // 不等待选择，波次不卡死
                Assert.That(rune.Cards, Is.Empty);
            }
            finally
            {
                Object.DestroyImmediate(runeGo);
            }
        }

        [Test]
        public void MagicDamage_Reduced_By_Enemy_MagicResist()
        {
            Enemy e = Spawn(Vector3.zero, 500f, magicResist: 100f); // 魔抗100 → 减半

            DamageSystem.CastMagic(e, 100f, 0f);

            Assert.That(500f - e.Health, Is.EqualTo(50f).Within(0.01f));
        }

        [Test]
        public void MagicDamage_AbilityPower_Adds_BaseScaled()
        {
            stats.abilityPower = 10f;
            Enemy e = Spawn(Vector3.zero, 500f);

            DamageSystem.CastMagic(e, 15f, 0.7f); // 15 + 10×0.7 = 22

            Assert.That(500f - e.Health, Is.EqualTo(22f).Within(0.01f));
        }

        [Test]
        public void MagicDamage_MagicPen_Reduces_Enemy_Resist()
        {
            stats.magicPen = 50f;
            Enemy e = Spawn(Vector3.zero, 500f, magicResist: 100f); // 有效魔抗50 → 100*100/150

            DamageSystem.CastMagic(e, 100f, 0f);

            Assert.That(500f - e.Health, Is.EqualTo(100f * 100f / 150f).Within(0.01f));
        }

        [Test]
        public void Mana_Apply_Increases_Max_And_Current()
        {
            var item = ScriptableObject.CreateInstance<ShopItemData>();
            item.bonuses.Add(new StatBonus(StatType.Mana, 20f));

            stats.ApplyBonus(item);

            Assert.That(stats.maxMana, Is.EqualTo(20f));
            Assert.That(stats.mana, Is.EqualTo(20f));
        }

        [Test]
        public void Mana_Spend_And_Recharge()
        {
            stats.mana = 10f;
            stats.maxMana = 10f;

            Assert.That(stats.TrySpendMana(6f), Is.True); // 足够
            Assert.That(stats.mana, Is.EqualTo(4f));
            Assert.That(stats.TrySpendMana(6f), Is.False); // 不足
            Assert.That(stats.mana, Is.EqualTo(4f));       // 失败不扣

            stats.RechargeMana();                          // 每秒回 上限×10% = 1
            Assert.That(stats.mana, Is.EqualTo(5f).Within(0.001f));
        }

        [Test]
        public void AbilityHaste_Shrinks_Spell_Cooldown()
        {
            Assert.That(stats.HasteCooldownScale, Is.EqualTo(1f)); // 0 急速 = 1

            stats.abilityHaste = 100f;
            Assert.That(stats.HasteCooldownScale, Is.EqualTo(0.5f).Within(0.0001f)); // 100急速 ≈ 半冷却
        }

        [Test]
        public void ArcaneBolt_Spends_Mana_And_Damages_Nearest()
        {
            stats.abilityPower = 10f;
            stats.mana = stats.maxMana = 20f;
            stats.passiveType = PassiveType.ArcaneBolt; // 海克斯科技枪刃 被动
            stats.range = 6f;
            Enemy e = Spawn(Vector2.one * 2f, 500f);    // 范围内

            stats.TickSpell(3f, Vector2.zero);          // 冷却到点 → 施放 15+10×0.7=22

            Assert.That(500f - e.Health, Is.EqualTo(22f).Within(0.01f)); // 魔法伤害吃魔抗(0)=全量
            Assert.That(stats.mana, Is.EqualTo(14f).Within(0.001f));     // 消耗6法力
        }

        [Test]
        public void ArcaneBolt_No_Target_Or_No_Mana_Retries_Without_Cost()
        {
            stats.mana = stats.maxMana = 4f; // 法力 < 消耗6
            stats.passiveType = PassiveType.ArcaneBolt;
            stats.range = 6f;
            Enemy e = Spawn(Vector2.one * 2f, 500f);

            stats.TickSpell(3f, Vector2.zero); // 法力不足 → 不施放
            Assert.That(e.Health, Is.EqualTo(500f));
            Assert.That(stats.mana, Is.EqualTo(4f).Within(0.001f));

            stats.mana = 20f;
            EnemyRegistry.Clear(); // 无目标
            stats.TickSpell(3f, Vector2.zero);
            Assert.That(stats.mana, Is.EqualTo(20f).Within(0.001f)); // 空目标不消耗
        }
    }
}