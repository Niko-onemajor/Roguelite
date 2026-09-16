using NUnit.Framework;
using Roguelite;
using UnityEngine;

namespace Roguelite.Tests
{
    /// <summary>主动装备栏：自动入槽/交换/冷却(受技能急速缩放)/数字键效果分发(救赎/实现者/舒瑞娅/兰顿)。</summary>
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
        public void AddActive_Fills_First_Empty_Slot_And_Full_Returns_False()
        {
            for (int i = 0; i < PlayerStats.ActiveSlotCount; i++)
                Assert.That(stats.TryAddActive(Active(ActiveType.MoveBurst)), Is.True);

            Assert.That(stats.TryAddActive(Active(ActiveType.MoveBurst)), Is.False); // 满仓不入
            int filled = 0;
            for (int i = 0; i < stats.ActiveSlots.Count; i++)
                if (stats.ActiveSlots[i] != null) filled++;
            Assert.That(filled, Is.EqualTo(PlayerStats.ActiveSlotCount)); // 10 槽全满
            Assert.That(stats.ActiveSlots[1].Item.activeType, Is.EqualTo(ActiveType.MoveBurst));
        }

        [Test]
        public void AddActive_Ignores_NonActive_Item()
        {
            var item = Active(ActiveType.None);
            Assert.That(stats.TryAddActive(item), Is.False);
        }

        [Test]
        public void SwapActiveSlots_Exchanges_Positions()
        {
            var a = Active(ActiveType.Redemption, 10f);
            var b = Active(ActiveType.AoeBlast, 8f);
            stats.TryAddActive(a);
            stats.TryAddActive(b);

            stats.SwapActiveSlots(0, 1);

            Assert.That(stats.ActiveSlots[0].Item, Is.SameAs(b));
            Assert.That(stats.ActiveSlots[1].Item, Is.SameAs(a));
        }

        [Test]
        public void UseActive_Triggers_Enters_Cooldown_Then_Refreshes()
        {
            stats.TryAddActive(Active(ActiveType.MoveBurst, 10f));

            Assert.That(stats.TryUseActive(0, Vector2.zero), Is.True);
            Assert.That(stats.ActiveSlots[0].Remaining, Is.EqualTo(10f).Within(0.001f));
            Assert.That(stats.TryUseActive(0, Vector2.zero), Is.False); // 冷却中禁用

            stats.TickActive(10f);
            Assert.That(stats.TryUseActive(0, Vector2.zero), Is.True); // 冷却结束可再触发
        }

        [Test]
        public void UseActive_Cooldown_Scaled_By_AbilityHaste()
        {
            stats.TryAddActive(Active(ActiveType.MoveBurst, 10f));
            stats.abilityHaste = 100f; // HasteCooldownScale = 0.5

            stats.TryUseActive(0, Vector2.zero);

            Assert.That(stats.ActiveSlots[0].Remaining, Is.EqualTo(5f).Within(0.001f));
        }

        [Test]
        public void MoveBurst_Boosts_Effective_MoveSpeed_Then_Expires()
        {
            stats.TryAddActive(Active(ActiveType.MoveBurst, 5f));
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
            stats.TryAddActive(Active(ActiveType.ManaMeld, 8f));
            stats.mana = stats.maxMana = 0f; // 无法力

            Assert.That(stats.TryUseActive(0, Vector2.zero), Is.False); // 触发失败
            Assert.That(stats.ActiveSlots[0].Remaining, Is.EqualTo(0f).Within(0.001f)); // 未进入冷却

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
            stats.TryAddActive(Active(ActiveType.Redemption, 10f));

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
            stats.TryAddActive(Active(ActiveType.AoeBlast, 8f));

            stats.TryUseActive(0, Vector2.zero);

            Assert.That(500f - near.Health, Is.EqualTo(20f + 10f * 0.6f).Within(0.01f)); // 20 + AP×0.6
            Assert.That(outside.Health, Is.EqualTo(500f));
        }
    }
}