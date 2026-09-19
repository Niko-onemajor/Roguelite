using NUnit.Framework;
using Roguelite;
using UnityEngine;

namespace Roguelite.Tests
{
    /// <summary>职业系统：四职业开局选择、ApplyClass 基础属性覆盖(法师满蓝)、普攻法强加成、
    /// 技能急速冷却缩放、Q/R 八技能效果(直线/弹射/位移/攻速/挥砍回血/成长生命/电疗减伤/真实伤害)。
    /// 参照 ActiveSlotTests 的 Spawn 模式：EditMode 无生命周期回调，需显式 Init/Register。
    /// ExecuteWarriorCleave 为 internal，配合 InternalsVisibleTo 直接驱动。</summary>
    public class ClassSkillTests
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

        static ClassSkillData Skill(ClassSkillType t, float baseDmg = 0f, float apRatio = 0f, float adRatio = 0f, float cd = 5f, float mana = 10f)
        {
            return new ClassSkillData
            {
                Name = t.ToString(), Type = t, BaseDamage = baseDmg,
                ApRatio = apRatio, AdRatio = adRatio, Cooldown = cd, ManaCost = mana,
            };
        }

        static ClassData MakeClass(float damage = 10f, float ap = 0f, float hp = 100f, float interval = 0.8f,
            float ms = 8f, float armor = 0f, float mr = 0f, float mana = 100f, float haste = 0f,
            WeaponType weapon = WeaponType.Ranged, ClassSkillData q = null, ClassSkillData r = null)
        {
            return new ClassData
            {
                Name = "测试职业", Weapon = weapon,
                damage = damage, abilityPower = ap, maxHP = hp, attackInterval = interval,
                moveSpeed = ms, armor = armor, magicResist = mr, maxMana = mana, abilityHaste = haste,
                QSkill = q, RSkill = r,
            };
        }

        Enemy Spawn(Vector3 pos, float hp, float armor = 0f, float mr = 0f)
        {
            var go = new GameObject("e", typeof(BoxCollider2D));
            go.transform.position = pos;
            var e = go.AddComponent<TestEnemy>();
            var d = ScriptableObject.CreateInstance<EnemyData>();
            d.maxHP = hp;
            d.armor = armor;
            d.magicResist = mr;
            d.goldMin = 1;
            d.goldMax = 1;
            d.scale = 1f;
            e.Init(d);
            EnemyRegistry.Register(e); // EditMode 不触发 OnEnable，需显式注册
            return e;
        }

        /// <summary>带 Poolable 的敌人：死亡走对象池归池(SetActive(false))而非 Destroy，
        /// 规避 EditMode 下 Destroy 报错(Return 无 Poolable 时会走 Destroy)。</summary>
        Enemy SpawnPooled(Vector3 pos, float hp, float armor = 0f, float mr = 0f)
        {
            Enemy e = Spawn(pos, hp, armor, mr);
            e.gameObject.AddComponent<Poolable>().Key = 12345;
            return e;
        }

        [Test]
        public void ApplyClass_Overwrites_Base_Stats_Full_Hp_Mana_Binds_Skills()
        {
            var cls = MakeClass(damage: 15f, hp: 150f, armor: 5f, mr: 5f, mana: 40f, haste: 10f,
                weapon: WeaponType.Melee,
                q: Skill(ClassSkillType.WarriorCleave, 20f, cd: 8f, mana: 20f),
                r: Skill(ClassSkillType.WarriorReign, 8f, cd: 45f, mana: 30f));

            stats.ApplyClass(cls);

            Assert.That(stats.Class, Is.SameAs(cls));
            Assert.That(stats.damage, Is.EqualTo(15f));
            Assert.That(stats.maxHP, Is.EqualTo(150f));
            Assert.That(stats.CurrentHP, Is.EqualTo(150f));     // 满血
            Assert.That(stats.armor, Is.EqualTo(5f));
            Assert.That(stats.magicResist, Is.EqualTo(5f));
            Assert.That(stats.abilityHaste, Is.EqualTo(10f));
            Assert.That(stats.maxMana, Is.EqualTo(40f));
            Assert.That(stats.mana, Is.EqualTo(40f));           // 开局满蓝
            Assert.That(stats.QSkill.Type, Is.EqualTo(ClassSkillType.WarriorCleave));
            Assert.That(stats.RSkill.Type, Is.EqualTo(ClassSkillType.WarriorReign));
        }

        [Test]
        public void ApplyClass_Mage_Zero_Ad_Has_Full_Mana_And_BasicDamage_Uses_AbilityPower()
        {
            var cls = MakeClass(damage: 0f, ap: 20f, hp: 85f, mana: 120f, haste: 35f, interval: 1.2f);
            stats.ApplyClass(cls);

            Assert.That(stats.damage, Is.EqualTo(0f));          // 法师基础攻击力为 0
            Assert.That(stats.abilityPower, Is.EqualTo(20f));
            Assert.That(stats.maxMana, Is.EqualTo(120f));
            Assert.That(stats.mana, Is.EqualTo(120f));
            Assert.That(stats.attackInterval, Is.EqualTo(1.2f));// 法师攻速更慢(削弱平A)
            Assert.That(stats.BasicDamage, Is.EqualTo(6f).Within(1e-4f)); // 0 + 20×0.3

            stats.ApplyBonus(Item(StatType.AbilityPower, 10f));
            Assert.That(stats.BasicDamage, Is.EqualTo(9f).Within(1e-4f)); // 30×0.3
        }

        [Test]
        public void TryCastSkill_Sets_Cooldown_Scaled_By_Haste_Spends_Mana_Raises_Event()
        {
            var cls = MakeClass(mana: 100f, haste: 25f, q: Skill(ClassSkillType.MageDeathRay, cd: 10f, mana: 20f));
            stats.ApplyClass(cls);

            int raised = 0;
            GameEvents.SkillCooldownChanged += () => raised++;
            Assert.That(stats.TryCastSkill(false, Vector2.zero), Is.True);

            Assert.That(stats.QCooldown, Is.EqualTo(8f).Within(1e-4f)); // 10 × 100/(100+25)
            Assert.That(stats.mana, Is.EqualTo(80f));                   // 100 - 20
            Assert.That(raised, Is.EqualTo(1));
        }

        [Test]
        public void TryCastSkill_Rejected_On_Cooldown_Or_Missing_Mana()
        {
            var cls = MakeClass(mana: 100f, q: Skill(ClassSkillType.MageDeathRay, cd: 10f, mana: 20f));
            stats.ApplyClass(cls);
            stats.magicResist = 0f;

            Assert.That(stats.TryCastSkill(false, Vector2.zero), Is.True);
            Assert.That(stats.TryCastSkill(false, Vector2.zero), Is.False); // 冷却中失败

            stats.mana = 0f; // 清零法力
            Assert.That(stats.TryCastSkill(true, Vector2.zero), Is.False);  // 法力不足失败
            Assert.That(stats.RCooldown, Is.EqualTo(0f));                   // 失败不进冷却
            Assert.That(stats.mana, Is.EqualTo(0f));
        }

        [Test]
        public void MageDeathRay_Hits_Enemies_Along_Line()
        {
            var cls = MakeClass(ap: 20f, mana: 120f,
                q: Skill(ClassSkillType.MageDeathRay, baseDmg: 20f, apRatio: 0.8f, cd: 3f, mana: 25f));
            stats.ApplyClass(cls);
            Enemy inLine = Spawn(new Vector3(3f, 0f, 0f), 100f);
            Spawn(new Vector3(0f, 4f, 0f), 100f); // 垂直线外，不应命中

            Assert.That(stats.TryCastSkill(false, Vector2.zero), Is.True); // 玩家无 Facing，默认朝右

            Assert.That(inLine.Health, Is.EqualTo(64f).Within(1e-4f)); // 20 + 20×0.8 = 36
        }

        [Test]
        public void MageFlameSeed_Bounces_With_Delay_Damage_And_Slow()
        {
            var cls = MakeClass(mana: 100f,
                r: Skill(ClassSkillType.MageFlameSeed, baseDmg: 20f, apRatio: 0.5f, cd: 30f, mana: 60f));
            stats.ApplyClass(cls);
            Enemy a = Spawn(new Vector3(2f, 0f, 0f), 100f);
            Enemy b = Spawn(new Vector3(4f, 0f, 0f), 100f);
            Enemy c = Spawn(new Vector3(6f, 0f, 0f), 100f);

            Assert.That(stats.TryCastSkill(true, Vector2.zero), Is.True);

            var seeds = Object.FindObjectsByType<FlameSeed>(FindObjectsSortMode.None);
            Assert.That(seeds.Length, Is.EqualTo(1));
            var seed = seeds[0];
            seed.gameObject.AddComponent<Poolable>().Key = 7777; // 归池而非 Destroy，EditMode 兼容

            // 火焰之种延迟飞行：逐段前进，命中 A→B→C，共3段(每段移动速度7/秒)
            for (int i = 0; i < 8; i++) seed.Advance(0.5f);

            Assert.That(stats.mana, Is.EqualTo(40f));                  // 耗蓝 60
            Assert.That(stats.RCooldown, Is.EqualTo(30f).Within(1e-4f)); // 冷却30(无急速)
            Assert.That(a.Health, Is.EqualTo(80f).Within(1e-4f));      // 20 + 0×0.5 = 20
            Assert.That(b.Health, Is.EqualTo(80f).Within(1e-4f));
            Assert.That(c.Health, Is.EqualTo(80f).Within(1e-4f));
            Assert.That(a.speedMult, Is.EqualTo(FlameSeed.SlowMult));  // 命中减速(35%)
            Assert.That(b.speedMult, Is.EqualTo(FlameSeed.SlowMult));
            Assert.That(c.speedMult, Is.EqualTo(FlameSeed.SlowMult));
        }

        [Test]
        public void ArcherDash_Dashes_Forward_And_Grants_Next_Attack_Bonus()
        {
            var cls = MakeClass(damage: 10f,
                q: Skill(ClassSkillType.ArcherDash, baseDmg: 15f, adRatio: 0.5f, cd: 4f, mana: 20f));
            stats.ApplyClass(cls);
            Vector3 before = playerGo.transform.position;

            Assert.That(stats.TryCastSkill(false, Vector2.zero), Is.True);

            Assert.That(playerGo.transform.position.x, Is.GreaterThan(before.x + 2f)); // 朝右翻滚 3 单位
            Assert.That(stats.NextAttackBonus, Is.EqualTo(20f).Within(1e-4f)); // 15 + 10×0.5
            Assert.That(stats.ConsumeNextAttackBonus(), Is.EqualTo(20f).Within(1e-4f));
            Assert.That(stats.ConsumeNextAttackBonus(), Is.EqualTo(0f)); // 消费后清零
        }

        [Test]
        public void ArcherUlt_Attacks_Faster_Then_Expires_And_Restores()
        {
            var cls = MakeClass(interval: 0.8f,
                r: Skill(ClassSkillType.ArcherUlt, baseDmg: 10f, apRatio: 0.4f, cd: 45f, mana: 50f));
            stats.ApplyClass(cls);
            Assert.That(stats.AttackIntervalEffective, Is.EqualTo(0.8f));

            Assert.That(stats.TryCastSkill(true, Vector2.zero), Is.True);
            Assert.That(stats.AttackIntervalEffective, Is.EqualTo(0.8f * 0.82f).Within(1e-4f)); // 攻速×1.22

            stats.TickSkills(15.1f, Vector2.zero);
            Assert.That(stats.AttackIntervalEffective, Is.EqualTo(0.8f).Within(1e-4f)); // 到期恢复
        }

        [Test]
        public void WarriorCleave_Damages_And_Heals_Missing_Hp()
        {
            var cls = MakeClass(damage: 15f, hp: 100f, weapon: WeaponType.Melee,
                q: Skill(ClassSkillType.WarriorCleave, baseDmg: 20f, adRatio: 1f, cd: 8f, mana: 20f));
            stats.ApplyClass(cls);
            Enemy e = Spawn(new Vector3(1f, 0f, 0f), 200f);
            stats.TakeDamage(40f); // 90% 血，为回血留空间

            float before = stats.CurrentHP;
            stats.ExecuteWarriorCleave(Vector2.zero); // internal：绕过协程延迟直接结算

            Assert.That(e.Health, Is.EqualTo(165f).Within(1e-4f));  // 20 + 15×1 = 35
            Assert.That(stats.CurrentHP, Is.GreaterThan(before));   // 命中回血
            Assert.That(stats.CurrentHP, Is.LessThanOrEqualTo(100f));
        }

        [Test]
        public void WarriorReign_Adds_MaxHp_Auras_Then_Removes_On_Expire()
        {
            var cls = MakeClass(damage: 15f, hp: 100f,
                r: Skill(ClassSkillType.WarriorReign, baseDmg: 8f, adRatio: 0.2f, cd: 45f, mana: 30f));
            stats.ApplyClass(cls);
            Enemy e = Spawn(new Vector3(1f, 0f, 0f), 100f);

            Assert.That(stats.TryCastSkill(true, Vector2.zero), Is.True);
            Assert.That(stats.maxHP, Is.EqualTo(200f));      // +100 临时生命
            Assert.That(stats.CurrentHP, Is.EqualTo(200f));

            stats.TickSkills(1.1f, Vector2.zero);            // 第一秒光环：8 + 15×0.2 = 11
            Assert.That(e.Health, Is.EqualTo(89f).Within(1e-4f));

            stats.TickSkills(14f, Vector2.zero);             // 期间到期：扣回附加生命
            Assert.That(stats.maxHP, Is.EqualTo(100f));
            Assert.That(stats.CurrentHP, Is.EqualTo(100f));  // 扣回后钳制不超上限
        }

        [Test]
        public void TankShock_Ticks_Magic_Damage_And_Reduces_Incoming_Damage()
        {
            var cls = MakeClass(hp: 210f, armor: 18f, mr: 15f, mana: 40f,
                q: Skill(ClassSkillType.TankShock, baseDmg: 12f, cd: 10f, mana: 15f));
            stats.ApplyClass(cls);
            Enemy e = Spawn(new Vector3(1f, 0f, 0f), 100f);

            Assert.That(stats.TryCastSkill(false, Vector2.zero), Is.True);
            stats.TickSkills(1.1f, Vector2.zero); // 电疗第一跳：12 魔法伤害(目标无魔抗)
            Assert.That(e.Health, Is.EqualTo(88f).Within(1e-4f));

            float before = stats.CurrentHP;
            stats.TakeDamage(100f); // 物理 100：护甲 18 → 84.7458，再 ×(1-25%)
            float expected = 100f * (100f / 118f) * 0.75f;
            Assert.That(stats.CurrentHP, Is.EqualTo(before - expected).Within(0.01f));
        }

        [Test]
        public void TankFeast_TrueDamage_Ignores_Armor_And_Grows_On_Kill()
        {
            var cls = MakeClass(hp: 210f, mana: 40f,
                r: Skill(ClassSkillType.TankFeast, baseDmg: 60f, cd: 30f, mana: 40f));
            stats.ApplyClass(cls);
            Enemy victim = SpawnPooled(new Vector3(1f, 0f, 0f), 40f, armor: 1000f, mr: 1000f); // 高抗性：真伤无视

            Assert.That(stats.TryCastSkill(true, Vector2.zero), Is.True);

            Assert.That(stats.maxHP, Is.EqualTo(250f));      // 击杀 → 永久 +40
            Assert.That(stats.CurrentHP, Is.EqualTo(250f));
            Assert.That(victim.Health, Is.LessThanOrEqualTo(0f)); // 已击杀(归池)
        }

        [Test]
        public void TankFeast_No_Kill_No_Growth()
        {
            var cls = MakeClass(hp: 210f, mana: 40f,
                r: Skill(ClassSkillType.TankFeast, baseDmg: 20f, cd: 30f, mana: 40f));
            stats.ApplyClass(cls);
            Spawn(new Vector3(1f, 0f, 0f), 100f); // 20 真伤打不死

            Assert.That(stats.TryCastSkill(true, Vector2.zero), Is.True);
            Assert.That(stats.maxHP, Is.EqualTo(210f)); // 未击杀不成长
        }

        [Test]
        public void ClassSelectSystem_Choose_Selects_Class_Once()
        {
            var go = new GameObject("cs", typeof(ClassSelectSystem));
            var selector = go.GetComponent<ClassSelectSystem>();
            selector.classes = new[]
            {
                MakeClass(damage: 15f), MakeClass(ap: 20f), MakeClass(damage: 11f, interval: 0.55f), MakeClass(hp: 210f, armor: 18f),
            };

            selector.OpenOffer();
            Assert.That(selector.IsAwaitingChoice, Is.True);
            Assert.That(selector.Selected, Is.Null);

            selector.Choose(2);
            Assert.That(selector.IsAwaitingChoice, Is.False);
            Assert.That(selector.Selected.damage, Is.EqualTo(11f));
            Assert.That(selector.Selected.attackInterval, Is.EqualTo(0.55f));

            selector.Choose(0); // 已选定后不再生效
            Assert.That(selector.Selected.damage, Is.EqualTo(11f));

            Object.DestroyImmediate(go);
        }

        static ShopItemData Item(StatType type, float value)
        {
            var item = ScriptableObject.CreateInstance<ShopItemData>();
            item.displayName = "测试词条";
            item.statType = type;
            item.addValue = value;
            item.basePrice = 10;
            return item;
        }
    }
}