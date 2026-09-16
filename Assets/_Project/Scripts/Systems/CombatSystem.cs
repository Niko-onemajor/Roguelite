using System.Collections.Generic;
using UnityEngine;

namespace Roguelite
{
    /// <summary>挂在玩家身上：每把武器独立索敌并驱动冷却。商店/结算冻结输入时自动停火。</summary>
    public class CombatSystem : MonoBehaviour
    {
        public PlayerStats Stats { get; private set; }

        readonly List<PlayerWeapon> weapons = new List<PlayerWeapon>();

        /// <summary>当前已装备武器(只读)，供暂停/商店详情面板展示。</summary>
        public IReadOnlyList<PlayerWeapon> Weapons => weapons;

        void Awake() => Stats = GetComponent<PlayerStats>();

        public void ClearEquips()
        {
            foreach (var w in weapons)
                if (w != null) Destroy(w);
            weapons.Clear();
        }

        public void Equip(WeaponData data)
        {
            if (data == null) return;
            PlayerWeapon weapon = data.type switch
            {
                WeaponType.Melee => gameObject.AddComponent<MeleeWeapon>(),
                WeaponType.AoE => gameObject.AddComponent<AoEWeapon>(),
                _ => gameObject.AddComponent<RangedWeapon>()
            };
            weapon.Equip(data);
            weapons.Add(weapon);
        }

        void Update()
        {
            if (weapons.Count == 0) return;
            PlayerController pc = PlayerController.Instance;
            if (pc == null || !pc.inputEnabled) return;

            Vector2 origin = transform.position;
            Stats.TickSpell(Time.deltaTime, origin); // 被动法术(奥术弹等)：消耗法力，冷却受技能急速
            Stats.TickActive(Time.deltaTime);        // 主动装备栏：冷却倒计时/临时增益计时
            for (int i = 0; i < weapons.Count; i++)
            {
                PlayerWeapon w = weapons[i];
                if (w == null || w.Data == null) continue;
                float effRange = w.Data.range + Stats.range;
                Enemy target = EnemyRegistry.Nearest(origin, effRange);
                if (target == null) continue;
                Vector2 to = (Vector2)target.transform.position - origin;
                Vector2 dir = to.sqrMagnitude > 0.0001f ? to.normalized : Vector2.right;
                w.Tick(Time.deltaTime, origin, dir, Stats.TotalDamage, Stats.critChance, Stats.range, Stats.attackInterval);
            }
        }
    }
}