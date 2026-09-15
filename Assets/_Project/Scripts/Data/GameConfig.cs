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

            // 武器 4 把(damage 字段为基准伤害；实际伤害=PlayerStats.damage)
            cfg.weapons.Add(Weapon("手枪", WeaponType.Ranged, 10f, 0.6f, 8.5f, 7f, Color.yellow));
            cfg.weapons.Add(Weapon("冲锋枪", WeaponType.Ranged, 5f, 0.18f, 7f, 8f, Color.cyan));
            cfg.weapons.Add(Weapon("砍刀", WeaponType.Melee, 14f, 0.8f, 1.8f, 0f, Color.green));
            cfg.weapons.Add(Weapon("手雷", WeaponType.AoE, 12f, 2.2f, 3.8f, 0f, Color.red));

            // 敌人 3 种：追兵3.2(玩家8可甩开) / 远程弹6(可躲) / 坦克慢而硬
            // 护甲/魔抗因种类不同：追兵脆、远程中、坦克硬(减伤 护甲/(100+护甲))
            cfg.enemies.Add(Enemy(EnemyType.Chaser, 22f, 3.2f, 8f, 1.4f, 0f, 0f, 0f, 8f, 1, 2, 1f, new Color(0.9f, 0.3f, 0.3f)));
            cfg.enemies.Add(Enemy(EnemyType.Ranged, 16f, 2.2f, 0f, 2.6f, 4.5f, 8f, 5f, 6f, 1, 2, 1f, new Color(0.7f, 0.4f, 0.9f), 2f, 6f));
            cfg.enemies.Add(Enemy(EnemyType.Tank, 80f, 1.4f, 14f, 1.6f, 0f, 0f, 0f, 8f, 3, 5, 1.6f, new Color(0.5f, 0.5f, 0.6f), 15f, 10f));

            // 商店 10 项(装备效果后续导入，当前映射到全属性增益；价格固定不再递增)
            cfg.shopItems.Add(Shop("攻击力+6", StatType.AttackDamage, 15, 6f));
            cfg.shopItems.Add(Shop("攻速×0.85", StatType.AttackSpeed, 15, 0.85f));
            cfg.shopItems.Add(Shop("生命+20", StatType.MaxHP, 10, 20f));
            cfg.shopItems.Add(Shop("移速+0.5", StatType.MoveSpeed, 10, 0.5f));
            cfg.shopItems.Add(Shop("攻击距离+1", StatType.AttackRange, 10, 1f));
            cfg.shopItems.Add(Shop("暴击+8%", StatType.CritChance, 20, 0.08f));
            cfg.shopItems.Add(Shop("暴击伤害+0.3", StatType.CritDamage, 20, 0.3f));
            cfg.shopItems.Add(Shop("护甲+10", StatType.Armor, 15, 10f));
            cfg.shopItems.Add(Shop("魔抗+10", StatType.MagicResist, 15, 10f));
            cfg.shopItems.Add(Shop("法术强度+6", StatType.AbilityPower, 15, 6f));

            // 波次 20 波(手调递增)，通关后由 WaveManager 进入无尽模式
            cfg.waves = ScriptableObject.CreateInstance<WaveConfig>();
            cfg.waves.prepareTime = 2f;
            cfg.waves.combatTime = 25f;
            cfg.waves.wave1 = new List<WaveBatch> { B(cfg.enemies[0], 6, 0.7f) };
            cfg.waves.wave2 = new List<WaveBatch> { B(cfg.enemies[0], 8, 0.6f), B(cfg.enemies[1], 3, 1.2f) };
            cfg.waves.wave3 = new List<WaveBatch> { B(cfg.enemies[0], 8, 0.5f), B(cfg.enemies[1], 4, 1.0f), B(cfg.enemies[2], 2, 2.0f) };
            cfg.waves.wave4 = new List<WaveBatch> { B(cfg.enemies[0], 10, 0.45f), B(cfg.enemies[1], 6, 0.9f), B(cfg.enemies[2], 3, 1.8f) };
            cfg.waves.wave5 = new List<WaveBatch> { B(cfg.enemies[0], 12, 0.4f), B(cfg.enemies[1], 8, 0.8f), B(cfg.enemies[2], 5, 1.5f) };
            cfg.waves.wave6 = new List<WaveBatch> { B(cfg.enemies[0], 12, 0.4f), B(cfg.enemies[1], 8, 0.8f), B(cfg.enemies[2], 5, 1.5f) };
            cfg.waves.wave7 = new List<WaveBatch> { B(cfg.enemies[0], 14, 0.38f), B(cfg.enemies[1], 9, 0.8f), B(cfg.enemies[2], 6, 1.4f) };
            cfg.waves.wave8 = new List<WaveBatch> { B(cfg.enemies[0], 14, 0.36f), B(cfg.enemies[1], 10, 0.75f), B(cfg.enemies[2], 6, 1.4f) };
            cfg.waves.wave9 = new List<WaveBatch> { B(cfg.enemies[0], 16, 0.35f), B(cfg.enemies[1], 10, 0.7f), B(cfg.enemies[2], 7, 1.3f) };
            cfg.waves.wave10 = new List<WaveBatch> { B(cfg.enemies[0], 18, 0.34f), B(cfg.enemies[1], 12, 0.7f), B(cfg.enemies[2], 8, 1.3f) };
            cfg.waves.wave11 = new List<WaveBatch> { B(cfg.enemies[0], 20, 0.32f), B(cfg.enemies[1], 13, 0.65f), B(cfg.enemies[2], 9, 1.2f) };
            cfg.waves.wave12 = new List<WaveBatch> { B(cfg.enemies[0], 22, 0.32f), B(cfg.enemies[1], 14, 0.6f), B(cfg.enemies[2], 10, 1.2f) };
            cfg.waves.wave13 = new List<WaveBatch> { B(cfg.enemies[0], 24, 0.3f), B(cfg.enemies[1], 15, 0.6f), B(cfg.enemies[2], 11, 1.15f) };
            cfg.waves.wave14 = new List<WaveBatch> { B(cfg.enemies[0], 26, 0.3f), B(cfg.enemies[1], 17, 0.55f), B(cfg.enemies[2], 12, 1.1f) };
            cfg.waves.wave15 = new List<WaveBatch> { B(cfg.enemies[0], 28, 0.28f), B(cfg.enemies[1], 18, 0.55f), B(cfg.enemies[2], 13, 1.1f) };
            cfg.waves.wave16 = new List<WaveBatch> { B(cfg.enemies[0], 30, 0.27f), B(cfg.enemies[1], 19, 0.5f), B(cfg.enemies[2], 14, 1.05f) };
            cfg.waves.wave17 = new List<WaveBatch> { B(cfg.enemies[0], 32, 0.26f), B(cfg.enemies[1], 21, 0.5f), B(cfg.enemies[2], 15, 1.0f) };
            cfg.waves.wave18 = new List<WaveBatch> { B(cfg.enemies[0], 34, 0.25f), B(cfg.enemies[1], 22, 0.48f), B(cfg.enemies[2], 16, 1.0f) };
            cfg.waves.wave19 = new List<WaveBatch> { B(cfg.enemies[0], 36, 0.25f), B(cfg.enemies[1], 24, 0.45f), B(cfg.enemies[2], 18, 0.95f) };
            cfg.waves.wave20 = new List<WaveBatch> { B(cfg.enemies[0], 40, 0.24f), B(cfg.enemies[1], 26, 0.45f), B(cfg.enemies[2], 20, 0.9f) };
            return cfg;
        }

        static WeaponData Weapon(string name, WeaponType t, float dmg, float interval, float range, float speed, Color c)
        {
            var w = ScriptableObject.CreateInstance<WeaponData>();
            w.displayName = name; w.type = t; w.damage = dmg; w.attackInterval = interval;
            w.range = range; w.speed = speed; w.color = c;
            return w;
        }

        static EnemyData Enemy(EnemyType t, float hp, float spd, float cDmg, float interval, float keep, float range, float pDmg, float pSpd, int gMin, int gMax, float scale, Color c, float armor = 0f, float magicResist = 0f)
        {
            var e = ScriptableObject.CreateInstance<EnemyData>();
            e.type = t; e.maxHP = hp; e.moveSpeed = spd; e.contactDamage = cDmg; e.attackInterval = interval;
            e.keepDistance = keep; e.range = range; e.projectileDamage = pDmg; e.projectileSpeed = pSpd;
            e.goldMin = gMin; e.goldMax = gMax; e.scale = scale; e.color = c;
            e.armor = armor; e.magicResist = magicResist;
            return e;
        }

        static ShopItemData Shop(string name, StatType s, int baseP, float add)
        {
            var i = ScriptableObject.CreateInstance<ShopItemData>();
            i.displayName = name; i.statType = s; i.basePrice = baseP; i.addValue = add;
            return i;
        }

        static WaveBatch B(EnemyData e, int count, float interval) =>
            new WaveBatch { enemy = e, count = count, spawnInterval = interval };
    }
}