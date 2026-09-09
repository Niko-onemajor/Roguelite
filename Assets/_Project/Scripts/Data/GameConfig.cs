using System.Collections.Generic;
using UnityEngine;

namespace Roguelite
{
    /// <summary>运行时默认配置(CreateInstance，不依赖 .asset；可被 Editor 落盘替换)。</summary>
    public class GameConfig
    {
        public WaveConfig waves;
        public List<WeaponData> weapons = new List<WeaponData>();
        public List<EnemyData> enemies = new List<EnemyData>();
        public List<ShopItemData> shopItems = new List<ShopItemData>();

        public static GameConfig Default()
        {
            var cfg = new GameConfig();

            // 武器 4 把
            cfg.weapons.Add(Weapon("手枪", WeaponType.Ranged, 10f, 0.6f, 8f, 10f, Color.yellow));
            cfg.weapons.Add(Weapon("冲锋枪", WeaponType.Ranged, 5f, 0.18f, 7f, 12f, Color.cyan));
            cfg.weapons.Add(Weapon("砍刀", WeaponType.Melee, 14f, 0.8f, 1.6f, 0f, Color.green));
            cfg.weapons.Add(Weapon("手雷", WeaponType.AoE, 8f, 2.0f, 3.5f, 0f, Color.red));

            // 敌人 3 种
            cfg.enemies.Add(Enemy(EnemyType.Chaser, 20f, 2.6f, 10f, 1.5f, 0f, 0f, 8f, 1, 2, 1f, new Color(0.9f, 0.3f, 0.3f)));
            cfg.enemies.Add(Enemy(EnemyType.Ranged, 15f, 1.8f, 0f, 2.5f, 4.5f, 8f, 8f, 1, 2, 1f, new Color(0.7f, 0.4f, 0.9f)));
            cfg.enemies.Add(Enemy(EnemyType.Tank, 60f, 1.2f, 20f, 1.5f, 0f, 0f, 8f, 3, 5, 1.6f, new Color(0.5f, 0.5f, 0.6f)));

            // 商店 6 项
            cfg.shopItems.Add(Shop("伤害+6", StatType.Damage, 15, 8, 6f));
            cfg.shopItems.Add(Shop("攻速×0.85", StatType.AttackSpeed, 15, 8, 0.85f));
            cfg.shopItems.Add(Shop("生命+20", StatType.MaxHP, 10, 5, 20f));
            cfg.shopItems.Add(Shop("移速+0.5", StatType.MoveSpeed, 10, 5, 0.5f));
            cfg.shopItems.Add(Shop("射程+1", StatType.Range, 10, 5, 1f));
            cfg.shopItems.Add(Shop("暴击+8%", StatType.CritChance, 20, 10, 0.08f));

            // 波次 5 波
            cfg.waves = ScriptableObject.CreateInstance<WaveConfig>();
            cfg.waves.prepareTime = 2f;
            cfg.waves.wave1 = new List<WaveBatch> { B(cfg.enemies[0], 6, 0.7f) };
            cfg.waves.wave2 = new List<WaveBatch> { B(cfg.enemies[0], 8, 0.6f), B(cfg.enemies[1], 3, 1.2f) };
            cfg.waves.wave3 = new List<WaveBatch> { B(cfg.enemies[0], 8, 0.5f), B(cfg.enemies[1], 4, 1.0f), B(cfg.enemies[2], 2, 2.0f) };
            cfg.waves.wave4 = new List<WaveBatch> { B(cfg.enemies[0], 10, 0.45f), B(cfg.enemies[1], 6, 0.9f), B(cfg.enemies[2], 3, 1.8f) };
            cfg.waves.wave5 = new List<WaveBatch> { B(cfg.enemies[0], 12, 0.4f), B(cfg.enemies[1], 8, 0.8f), B(cfg.enemies[2], 5, 1.5f) };
            return cfg;
        }

        static WeaponData Weapon(string name, WeaponType t, float dmg, float interval, float range, float speed, Color c)
        {
            var w = ScriptableObject.CreateInstance<WeaponData>();
            w.displayName = name; w.type = t; w.damage = dmg; w.attackInterval = interval;
            w.range = range; w.speed = speed; w.color = c;
            return w;
        }

        static EnemyData Enemy(EnemyType t, float hp, float spd, float cDmg, float interval, float keep, float pDmg, float pSpd, int gMin, int gMax, float scale, Color c)
        {
            var e = ScriptableObject.CreateInstance<EnemyData>();
            e.type = t; e.maxHP = hp; e.moveSpeed = spd; e.contactDamage = cDmg; e.attackInterval = interval;
            e.keepDistance = keep; e.projectileDamage = pDmg; e.projectileSpeed = pSpd;
            e.goldMin = gMin; e.goldMax = gMax; e.scale = scale; e.color = c;
            return e;
        }

        static ShopItemData Shop(string name, StatType s, int baseP, int step, float add)
        {
            var i = ScriptableObject.CreateInstance<ShopItemData>();
            i.displayName = name; i.statType = s; i.basePrice = baseP; i.priceStep = step; i.addValue = add;
            return i;
        }

        static WaveBatch B(EnemyData e, int count, float interval) =>
            new WaveBatch { enemy = e, count = count, spawnInterval = interval };
    }
}