using NUnit.Framework;
using Roguelite;
using UnityEngine;

namespace Roguelite.Tests
{
    /// <summary>装备栏(Brotato 式 8 槽，含主动/被动装备)：自动入槽/交换/冷却(受技能急速缩放)/数字键效果分发(救赎/实现者/舒瑞娅/兰顿)。</summary>
    public class ActiveSlotTests
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

        static ShopItemData Active(ActiveType type, float cooldown = 0f)
        {
            var item = ScriptableObject.CreateInstance<ShopItemData>();
            item.displayName = type.ToString();
            item.activeType = type;
            item.activeCooldown = cooldown;
            return item;
        }

        Enemy Spawn(Vector3 pos, float hp)
        {
            var go = new GameObject("e", typeof(BoxCollider2D));
            go.transform.position = pos;
            var e = go.AddComponent<TestEnemy>();
            var d = ScriptableObject.CreateInstance<EnemyData>();
            d.maxHP = hp;
            d.goldMin = 1;
            d.goldMax = 1;
            d.scale = 1f;
            e.Init(d);
            EnemyRegistry.Register(e); // EditMode 不触发 OnEnable，需显式注册
            return e;
        }

        [Test]
        public void AddEquip_Fills_First_Empty_Slot_And_Full_Returns_False()
        {
            for (int i = 0; i < PlayerStats.EquipmentSlotCount; i++)
                Assert.That(stats.TryAddEquip(Active(ActiveType.MoveBurst)), Is.True);

            Assert.That(stats.TryAddEquip(Active(ActiveType.MoveBurst)), Is.False); // 满仓不入
            int filled = 0;
            for (int i = 0; i < stats.EquipSlots.Count; i++)
                if (stats.EquipSlots[i] != null) filled++;
            Assert.That(filled, Is.EqualTo(PlayerStats.EquipmentSlotCount)); // 8 槽全满
            Assert.That(stats.EquipSlots[1].Item.activeType, Is.EqualTo(ActiveType.MoveBurst));
            Assert.That(stats.IsEquipFull, Is.True);
        }

        [Test]
        public void AddEquip_Accepts_PassiveItem_But_Cannot_Trigger_It()
        {
            var item = Active(ActiveType.None); // 无主动效果的被动装备也能占槽
            Assert.That(stats.TryAddEquip(item), Is.True);
            Assert.That(stats.EquipSlots[0].Item, Is.SameAs(item));
            Assert.That(stats.EquipSlots[0].Remaining, Is.EqualTo(0f)); // 被动装备无冷却

            Assert.That(stats.TryUseActive(0, Vector2.zero), Is.False); // 被动装备不可触发
        }

        [Test]
        public void SwapActiveSlots_Exchanges_Positions()
        {
            var a = Active(ActiveType.Redemption, 10f);
            var b = Active(ActiveType.AoeBlast, 8f);
            stats.TryAddEquip(a);
            stats.TryAddEquip(b);

            stats.SwapActiveSlots(0, 1);

            Assert.That(stats.EquipSlots[0].Item, Is.SameAs(b));
            Assert.That(stats.EquipSlots[1].Item, Is.SameAs(a));
        }

        [Test]
        public void UseActive_Triggers_Enters_Cooldown_Then_Refreshes()
        {
            stats.TryAddEquip(Active(ActiveType.MoveBurst, 10f));

            Assert.That(stats.TryUseActive(0, Vector2.zero), Is.True);
            Assert.That(stats.EquipSlots[0].Remaining, Is.EqualTo(10f).Within(0.001f));
            Assert.That(stats.TryUseActive(0, Vector2.zero), Is.False); // 冷却中禁用

            stats.TickActive(10f);
            Assert.That(stats.TryUseActive(0, Vector2.zero), Is.True); // 冷却结束可再触发
        }

        [Test]
        public void UseActive_Cooldown_Scaled_By_AbilityHaste()
        {
            stats.TryAddEquip(Active(ActiveType.MoveBurst, 10f));
            stats.abilityHaste = 100f; // HasteCooldownScale = 0.5

            stats.TryUseActive(0, Vector2.zero);

            Assert.That(stats.EquipSlots[0].Remaining, Is.EqualTo(5f).Within(0.001f));
        }

        [Test]
        public void MoveBurst_Boosts_Effective_MoveSpeed_Then_Expires()
        {
            stats.TryAddEquip(Active(ActiveType.MoveBurst, 5f));
            Assert.That(stats.EffectiveMoveSpeed, Is.EqualTo(8f).Within(0.001f)); // 基准移速

            stats.TryUseActive(0, Vector2.zero);

            Assert.That(stats.EffectiveMoveSpeed, Is.EqualTo(8f * 1.6f).Within(0.001f));
            stats.TickActive(3f);
            Assert.That(stats.EffectiveMoveSpeed, Is.EqualTo(8f * 1.6f).Within(0.001f)); // 5s 内仍生效
            stats.TickActive(3f);
            Assert.That(stats.EffectiveMoveSpeed, Is.EqualTo(8f).Within(0.001f));         // 超时恢复
        }

        [Test]
        public void UseActive_Empty_Slot_Returns_False()
        {
            Assert.That(stats.TryUseActive(3, Vector2.zero), Is.False);
        }

        [Test]
        public void ManaMeld_NoMana_Fails_Without_Cooldown()
        {
            stats.TryAddEquip(Active(ActiveType.ManaMeld, 8f));
            stats.mana = stats.maxMana = 0f; // 无法力

            Assert.That(stats.TryUseActive(0, Vector2.zero), Is.False); // 触发失败
            Assert.That(stats.EquipSlots[0].Remaining, Is.EqualTo(0f).Within(0.001f)); // 未进入冷却

            stats.TakeDamage(50f); // 制造缺口以便测回血
            stats.mana = stats.maxMana = 20f;
            Assert.That(stats.TryUseActive(0, Vector2.zero), Is.True);
            Assert.That(stats.mana, Is.EqualTo(8f).Within(0.001f)); // 消耗 12 法力
            Assert.That(stats.CurrentHP, Is.EqualTo(70f + 120f * 0.1f).Within(0.001f)); // 回 10% 最大生命
        }

        [Test]
        public void Redemption_Heals_And_Damages_Nearby_Enemies()
        {
            stats.TakeDamage(50f); // 700→实际掉 50(护甲0)，剩 70
            float hp = stats.CurrentHP;
            Enemy near = Spawn(Vector2.one, 500f);       // 半径 5 内
            Enemy far = Spawn(new Vector3(50f, 0f, 0f), 500f);
            stats.TryAddEquip(Active(ActiveType.Redemption, 10f));

            stats.TryUseActive(0, Vector2.zero);

            Assert.That(stats.CurrentHP, Is.EqualTo(hp + 120f * 0.12f).Within(0.001f)); // 治疗 12% 上限生命
            Assert.That(500f - near.Health, Is.EqualTo(15f).Within(0.01f));             // 满血敌方(魔抗0) 受 15 魔伤
            Assert.That(far.Health, Is.EqualTo(500f));                                  // 范围外不受
        }

        [Test]
        public void AoeBlast_Damages_Enemies_In_Radius_With_Ap()
        {
            stats.abilityPower = 10f;
            Enemy near = Spawn(Vector2.right * 2f, 500f);   // 半径 3.5 内
            Enemy outside = Spawn(Vector2.right * 9f, 500f);
            stats.TryAddEquip(Active(ActiveType.AoeBlast, 8f));

            stats.TryUseActive(0, Vector2.zero);

            Assert.That(500f - near.Health, Is.EqualTo(20f + 10f * 0.6f).Within(0.01f)); // 20 + AP×0.6
            Assert.That(outside.Health, Is.EqualTo(500f));
        }

        [Test]
        public void Mana_Changes_Raise_Event_For_Blue_Bar()
        {
            int calls = 0;
            GameEvents.ManaChanged += (cur, max) => calls++;

            var item = ScriptableObject.CreateInstance<ShopItemData>();
            item.displayName = "蓝瓶测试";
            item.statType = StatType.Mana;
            item.addValue = 20f;
            stats.ApplyBonus(item); // 法力装备：上限与当前同增 → 广播

            Assert.That(stats.maxMana, Is.EqualTo(20f).Within(0.001f));
            int afterEquip = calls;
            Assert.That(afterEquip, Is.GreaterThanOrEqualTo(1));

            Assert.That(stats.TrySpendMana(5f), Is.True); // 消耗 → 广播
            Assert.That(calls, Is.EqualTo(afterEquip + 1));
            stats.RechargeMana(); // 每秒回蓝 → 广播
            Assert.That(calls, Is.EqualTo(afterEquip + 2));
        }
    }
}