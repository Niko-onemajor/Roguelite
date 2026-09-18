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
        /// <summary>锻体独立池(基础属性卡，锻体白/金/彩倍率强化)，与商店装备池互不共用。</summary>
        public List<ShopItemData> forgeItems = new List<ShopItemData>();
        public List<ShopItemData> runes = new List<ShopItemData>(); // 符文三选一池(开局/7/11/15波前)
        /// <summary>职业池：开局四选一，决定基础属性/武器类型(近战砍刀或远程飞弹)/Q基础技能/R大招。</summary>
        public List<ClassData> classes = new List<ClassData>();

        public static GameConfig Default()
        {
            var cfg = new GameConfig();

            // ── 职业 4 种（战士/坦克=近战挥砍；法师/射手=远程飞弹，不再区分手枪/冲锋枪）──
            cfg.classes.Add(Class("战士", "近战 · 高攻击高抗性", WeaponType.Melee,
                damage: 15f, ap: 0f, hp: 150f, interval: 0.8f, ms: 8f, armor: 5f, mr: 5f, mana: 40f, haste: 10f,
                new ClassSkillData { Name = "大杀四方", Desc = "短延迟后挥击身周敌人（物理），外圈命中回复已损生命", Type = ClassSkillType.WarriorCleave, BaseDamage = 20f, AdRatio = 1f, Cooldown = 8f, ManaCost = 20f },
                new ClassSkillData { Name = "终极统治", Desc = "15秒内+100最大生命，每秒对身周敌人造成魔法伤害", Type = ClassSkillType.WarriorReign, BaseDamage = 8f, AdRatio = 0.2f, ApRatio = 0.1f, Cooldown = 45f, ManaCost = 30f }));
            cfg.classes.Add(Class("法师", "远程 · 蓝量多技能急速高，普攻附加法强", WeaponType.Ranged,
                damage: 0f, ap: 20f, hp: 85f, interval: 0.8f, ms: 7.5f, armor: 0f, mr: 0f, mana: 120f, haste: 35f,
                new ClassSkillData { Name = "死亡射线", Desc = "向面朝方向发射混乱光线，沿途敌人受魔法伤害", Type = ClassSkillType.MageDeathRay, BaseDamage = 25f, ApRatio = 0.8f, Cooldown = 6f, ManaCost = 25f },
                new ClassSkillData { Name = "烈焰风暴", Desc = "烈焰在敌方单位间弹射，每次弹射造成魔法伤害", Type = ClassSkillType.MageStorm, BaseDamage = 30f, ApRatio = 0.6f, Cooldown = 25f, ManaCost = 60f }));
            cfg.classes.Add(Class("射手", "远程 · 攻速更快技能冷却更短", WeaponType.Ranged,
                damage: 11f, ap: 0f, hp: 85f, interval: 0.55f, ms: 8f, armor: 0f, mr: 0f, mana: 60f, haste: 0f,
                new ClassSkillData { Name = "闪避突袭", Desc = "向前翻滚一小段距离，下一次普攻附加物理伤害", Type = ClassSkillType.ArcherDash, BaseDamage = 15f, AdRatio = 0.5f, Cooldown = 4f, ManaCost = 20f },
                new ClassSkillData { Name = "定圣诀", Desc = "15秒内攻速提升22%，普攻附加魔法伤害并对附近敌人溅射", Type = ClassSkillType.ArcherUlt, BaseDamage = 10f, ApRatio = 0.4f, Cooldown = 45f, ManaCost = 40f }));
            cfg.classes.Add(Class("坦克", "近战 · 血量最高双抗最高", WeaponType.Melee,
                damage: 10f, ap: 0f, hp: 210f, interval: 0.8f, ms: 7.5f, armor: 18f, mr: 15f, mana: 40f, haste: 0f,
                new ClassSkillData { Name = "电击疗法", Desc = "电击自身：4秒内每秒对身周敌人造成魔法伤害并减伤", Type = ClassSkillType.TankShock, BaseDamage = 12f, Cooldown = 10f, ManaCost = 15f },
                new ClassSkillData { Name = "盛宴", Desc = "吞食最近敌人造成真实伤害，击杀后最大生命永久+40", Type = ClassSkillType.TankFeast, BaseDamage = 60f, Cooldown = 30f, ManaCost = 40f }));

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

            // 锻体专属池(低价基础属性卡，与商店装备分离；锻体按稀有度白/金/彩 1x/1.5x/2x 强化)
            cfg.forgeItems.Add(Shop("攻击力+6", StatType.AttackDamage, 15, 6f));
            cfg.forgeItems.Add(Shop("攻速×0.85", StatType.AttackSpeed, 15, 0.85f));
            cfg.forgeItems.Add(Shop("生命+20", StatType.MaxHP, 10, 20f));
            cfg.forgeItems.Add(Shop("移速+0.5", StatType.MoveSpeed, 10, 0.5f));
            cfg.forgeItems.Add(Shop("攻击距离+1", StatType.AttackRange, 10, 1f));
            cfg.forgeItems.Add(Shop("暴击+8%", StatType.CritChance, 20, 0.08f));
            cfg.forgeItems.Add(Shop("暴击伤害+0.3", StatType.CritDamage, 20, 0.3f));
            cfg.forgeItems.Add(Shop("护甲+10", StatType.Armor, 15, 10f));
            cfg.forgeItems.Add(Shop("魔抗+10", StatType.MagicResist, 15, 10f));
            cfg.forgeItems.Add(Shop("法术强度+6", StatType.AbilityPower, 15, 6f));

            // ── 装备导入(LOL 风格，数值按当前基数缩放：基础攻击10/生命120/移速8/价带10~45) ──
            // 缩放基准：攻击力/8、法术强度/15、生命/10、护甲魔抗/3.5、移速/45→量、攻速按 1/(1+%)、
            // 暴击/2、全能吸血/2、价格/80。被动主动仅文案展示，战斗挂钩后续实现。
            #region AD 攻击力装备
            cfg.shopItems.Add(Equip("无尽之刃", 42, "",
                B(StatType.AttackDamage, 9f), B(StatType.CritChance, 0.12f), B(StatType.CritDamage, 0.4f)));
            cfg.shopItems.Add(Equip("三相之力", 40, "被动“追击”：每1.5秒一次普攻附带 攻击力×1 的额外物理伤害。",
                B(StatType.AttackDamage, 4f), B(StatType.AttackSpeed, 0.77f), B(StatType.MaxHP, 35f), B(StatType.AbilityHaste, 5f)));
            MarkPassive(cfg.shopItems, "三相之力", PassiveType.Spellblade);
            cfg.shopItems.Add(Equip("斯特拉克的挑战护手", 38, "被动“救主灵刃”：生命值低于30%时获得相当于最大生命35%的护盾（冷却30秒）。",
                B(StatType.AttackDamage, 5f), B(StatType.MaxHP, 40f)));
            MarkPassive(cfg.shopItems, "斯特拉克的挑战护手", PassiveType.Lifeline);
            cfg.shopItems.Add(Equip("黑色切割者", 38, "被动“切割”：普攻削减目标护甲10点（持续5秒，累加上限30）。",
                B(StatType.AttackDamage, 5f), B(StatType.MaxHP, 40f), B(StatType.AbilityHaste, 7f)));
            MarkPassive(cfg.shopItems, "黑色切割者", PassiveType.BlackCleaver);
            cfg.shopItems.Add(Equip("死亡之舞", 38, "被动：受到的物理伤害30%将以流血形式在3秒内持续扣除。",
                B(StatType.AttackDamage, 7f), B(StatType.Armor, 13f), B(StatType.AbilityHaste, 5f)));
            MarkPassive(cfg.shopItems, "死亡之舞", PassiveType.DeathDance);
            cfg.shopItems.Add(Equip("破败王者之刃", 40, "被动：普攻附加目标当前生命值6%的额外物理伤害。",
                B(StatType.AttackDamage, 5f), B(StatType.AttackSpeed, 0.8f), B(StatType.Omnivamp, 0.05f)));
            MarkPassive(cfg.shopItems, "破败王者之刃", PassiveType.RuinKing);
            cfg.shopItems.Add(Equip("饮血剑", 42, "被动“猩红护盾”：生命偷取溢出治疗量转化为护盾（上限最大生命15%）。",
                B(StatType.AttackDamage, 10f), B(StatType.Omnivamp, 0.07f)));
            MarkPassive(cfg.shopItems, "饮血剑", PassiveType.Bloodshield);
            cfg.shopItems.Add(Equip("海克斯镜片 C44", 35, "被动“高倍望远镜”：对距离≥6的敌人伤害+25%；被动“奥术瞄准”：参与击杀+0.5攻击距离（上限3）。",
                B(StatType.AttackDamage, 6f), B(StatType.CritChance, 0.12f)));
            MarkPassive(cfg.shopItems, "海克斯镜片 C44", PassiveType.Longshot);
            cfg.shopItems.Add(Equip("霸王血铠", 41, "被动“专横”：获得相当于你2.5%额外生命值的攻击力。被动“报复”：获得基于你的百分比已损失生命值的12%攻击力提升。",
                B(StatType.AttackDamage, 5f), B(StatType.MaxHP, 55f)));
            cfg.shopItems[cfg.shopItems.Count - 1].passiveType = PassiveType.Tyrant;
            #endregion

            #region AP 法术强度装备
            cfg.shopItems.Add(Equip("兰德里的折磨", 38, "被动：技能施加灼烧，3秒内每秒造成 目标最大生命1%+法强×0.05 的魔法伤害。",
                B(StatType.AbilityPower, 6f), B(StatType.MaxHP, 30f)));
            MarkPassive(cfg.shopItems, "兰德里的折磨", PassiveType.LiandryBurn);
            cfg.shopItems.Add(Equip("灭世者的死亡之帽", 45, "被动：法术强度×1.4。",
                B(StatType.AbilityPower, 9f)));
            MarkPassive(cfg.shopItems, "灭世者的死亡之帽", PassiveType.Deathcap);
            cfg.shopItems.Add(Equip("虚空之杖", 38, "被动“虚空穿透”：法术穿透提升40%。",
                B(StatType.AbilityPower, 5f), B(StatType.MagicPen, 8f)));
            MarkPassive(cfg.shopItems, "虚空之杖", PassiveType.VoidPen);
            cfg.shopItems.Add(Equip("蜕生", 38, "被动“死中焕生”：参与击杀回复最大生命5%。",
                B(StatType.AbilityPower, 5f), B(StatType.AbilityHaste, 7f), B(StatType.MagicPen, 6f)));
            MarkPassive(cfg.shopItems, "蜕生", PassiveType.ReapHeal);
            cfg.shopItems.Add(Equip("裂隙制造者", 38, "被动“虚空灌注”：额外生命值6%转化为法术强度。",
                B(StatType.AbilityPower, 5f), B(StatType.MaxHP, 35f), B(StatType.AbilityHaste, 5f)));
            MarkPassive(cfg.shopItems, "裂隙制造者", PassiveType.VoidInfusion);
            cfg.shopItems.Add(Equip("暗夜收割者", 38, "被动：魔法伤害后 移速×1.3 持续2秒。",
                B(StatType.AbilityPower, 6f), B(StatType.MaxHP, 30f), B(StatType.AbilityHaste, 8f)));
            MarkPassive(cfg.shopItems, "暗夜收割者", PassiveType.NightHarvest);
            cfg.shopItems.Add(Equip("影焰", 38, "被动：对生命低于50%的敌人造成+20%魔法伤害。",
                B(StatType.AbilityPower, 7f), B(StatType.MaxHP, 20f)));
            MarkPassive(cfg.shopItems, "影焰", PassiveType.ShadowflameLowHp);
            #endregion

            #region 攻速与暴击装备
            cfg.shopItems.Add(Equip("卢安娜的飓风", 35, "被动“分裂箭”：普攻对附近另一敌人造成50%攻击力的物理伤害。",
                B(StatType.AttackSpeed, 0.71f), B(StatType.CritChance, 0.12f), B(StatType.MoveSpeed, 0.4f)));
            MarkPassive(cfg.shopItems, "卢安娜的飓风", PassiveType.Hurricane);
            cfg.shopItems.Add(Equip("猎魔人弩箭", 33, "被动“开战弹幕”：每6秒获得3次必定暴击的普攻。",
                B(StatType.AttackSpeed, 0.71f), B(StatType.CritChance, 0.12f), B(StatType.MoveSpeed, 0.3f)));
            MarkPassive(cfg.shopItems, "猎魔人弩箭", PassiveType.CritBarrage);
            #endregion

            #region 坦克与防御装备
            cfg.shopItems.Add(Equip("日炎圣盾", 35, "被动“献祭”：每秒对周围2.8内敌人造成 5+法强×0.15 魔法伤害。",
                B(StatType.MaxHP, 45f), B(StatType.Armor, 9f), B(StatType.MagicResist, 9f), B(StatType.AbilityHaste, 5f)));
            MarkPassive(cfg.shopItems, "日炎圣盾", PassiveType.Sunfire);
            cfg.shopItems.Add(Equip("荆棘之甲", 34, "被动“荆棘”：受到物理伤害时，反弹30%该伤害的魔法给近处敌人。",
                B(StatType.Armor, 17f), B(StatType.MaxHP, 40f)));
            MarkPassive(cfg.shopItems, "荆棘之甲", PassiveType.Thornmail);
            cfg.shopItems.Add(Equip("自然之力", 35, "被动“风暴之力”：受到魔法伤害后 移速×1.15/层（最多3层，持续5秒）。",
                B(StatType.MagicResist, 17f), B(StatType.MaxHP, 40f), B(StatType.MoveSpeed, 0.4f)));
            MarkPassive(cfg.shopItems, "自然之力", PassiveType.ForceOfNature);
            cfg.shopItems.Add(Equip("振奋盔甲", 34, "被动：提升所有治疗与护盾效果30%。",
                B(StatType.MaxHP, 40f), B(StatType.MagicResist, 11f), B(StatType.AbilityHaste, 7f), B(StatType.HealShieldPower, 0.3f)));
            cfg.shopItems.Add(Equip("狂徒铠甲", 38, "被动“狂徒之心”：脱离战斗5秒后，每秒回复最大生命6%。",
                B(StatType.MaxHP, 100f), B(StatType.HPRegen, 0.2f)));
            MarkPassive(cfg.shopItems, "狂徒铠甲", PassiveType.Warmogs);
            cfg.shopItems.Add(Equip("无终恨意", 35, "被动“苦楚”：每1.5秒对周围2.8内敌人造成 8+法强×0.2 魔法伤害并回复6生命。",
                B(StatType.Armor, 14f), B(StatType.MaxHP, 40f), B(StatType.AbilityHaste, 5f)));
            MarkPassive(cfg.shopItems, "无终恨意", PassiveType.EndlessHatred);
            cfg.shopItems.Add(Equip("原生质护带", 31, "被动“救主灵刃”：生命低于30%时获得相当于最大生命35%的护盾（冷却30秒）。",
                B(StatType.MaxHP, 60f), B(StatType.AbilityHaste, 5f)));
            MarkPassive(cfg.shopItems, "原生质护带", PassiveType.Lifeline);
            cfg.shopItems.Add(Equip("亡者的板甲", 36, "被动：移动时每1秒积1层气势（上限5层，每层+4%移速）；普攻消耗全部层数，每层+2伤害。",
                B(StatType.Armor, 17f), B(StatType.MaxHP, 30f), B(StatType.MoveSpeed, 0.4f)));
            MarkPassive(cfg.shopItems, "亡者的板甲", PassiveType.DeadMans);
            cfg.shopItems.Add(Equip("深渊面具", 35, "被动“腐蚀”：周围4.5内敌人承受的魔法伤害×1.15。",
                B(StatType.MagicResist, 17f), B(StatType.MaxHP, 40f), B(StatType.AbilityHaste, 7f)));
            MarkPassive(cfg.shopItems, "深渊面具", PassiveType.AbyssalMask);
            #endregion

            #region 鞋子
            cfg.shopItems.Add(Equip("狂战士胫甲", 14, "",
                B(StatType.AttackSpeed, 0.77f), B(StatType.MoveSpeed, 1f)));
            cfg.shopItems.Add(Equip("法师之靴", 14, "",
                B(StatType.MoveSpeed, 1f), B(StatType.MagicPen, 5f)));
            cfg.shopItems.Add(Equip("铁板靴", 14, "被动“护甲硬化”：受到的物理伤害×0.88。",
                B(StatType.MoveSpeed, 1f), B(StatType.Armor, 6f)));
            MarkPassive(cfg.shopItems, "铁板靴", PassiveType.Steelcaps);
            cfg.shopItems.Add(Equip("水银之靴", 15, "被动：韧性+30%（减免控制时间）。",
                B(StatType.MoveSpeed, 1f), B(StatType.MagicResist, 7f), B(StatType.Tenacity, 0.3f)));
            cfg.shopItems.Add(Equip("明朗之靴", 12, "",
                B(StatType.MoveSpeed, 1f), B(StatType.AbilityHaste, 5f)));
            cfg.shopItems.Add(Equip("轻灵之靴", 13, "被动：韧性+25%（替代减速抗性）。",
                B(StatType.MoveSpeed, 1.3f), B(StatType.Tenacity, 0.25f)));
            #endregion

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

            BuildRunes(cfg);
            return cfg;
        }

        /// <summary>符文池填充：机制简化为一次性基础属性增益(多属性卡)，按 白银/龙魂/黄金/棱彩 四阶定价。
        /// 选择后由 RuneSystem 走 ApplyBonus 施加属性并记录。</summary>
        static void BuildRunes(GameConfig cfg)
        {
            // ── 白银强化符文(基础属性) ──
            cfg.runes.Add(Rune("灵巧", "获得 50% 攻击速度。", 5, B(StatType.AttackSpeed, 0.67f)));
            cfg.runes.Add(Rune("大力", "获得 10% 攻击力。", 5, B(StatType.AttackDamage, 10f)));
            cfg.runes.Add(Rune("巫师式思考", "获得 20% 法术强度。", 5, B(StatType.AbilityPower, 20f)));
            cfg.runes.Add(Rune("急救用具", "获得 20% 治疗和护盾强度。", 5, B(StatType.HealShieldPower, 0.2f)));
            cfg.runes.Add(Rune("由心及物", "最大生命值提升相当于一半法力值的数额。", 5,
                B(StatType.MaxHP, 40f), B(StatType.Mana, 80f)));
            cfg.runes.Add(Rune("易损", "持续伤害可暴击造成额外伤害，并获得 20% 暴击几率。", 5, PassiveType.Perishable, B(StatType.CritChance, 0.2f)));
            cfg.runes.Add(Rune("会心防守", "以暴击几率进行防御并减免伤害，获得 20% 暴击几率。", 5, PassiveType.ParryDefense, B(StatType.CritChance, 0.2f)));
            cfg.runes.Add(Rune("唯快不破", "移动速度高于目标时造成额外伤害。", 5, PassiveType.Swiftness, B(StatType.MoveSpeed, 0.8f)));
            cfg.runes.Add(Rune("重量级打击手", "普通攻击附带相当于最大生命 4% 的额外物理伤害。", 5, PassiveType.HeavyHitter, B(StatType.MaxHP, 60f)));
            cfg.runes.Add(Rune("侵蚀", "伤害施加 4 秒的 1.5% 护甲与魔抗击碎效果。", 5, PassiveType.Eroding,
                B(StatType.ArmorPen, 6f), B(StatType.MagicPen, 6f)));
            cfg.runes.Add(Rune("点亮", "每第 4 次普通攻击发射 4 枚额外魔法飞弹。", 5, PassiveType.LightStrike, B(StatType.AbilityPower, 15f)));
            cfg.runes.Add(Rune("裁决使", "对生命低于 50% 的敌人多造成 15% 伤害。", 5, PassiveType.Executioner, B(StatType.AttackDamage, 8f)));

            // ── 龙魂类(归入白银阶) ──
            cfg.runes.Add(Rune("炼狱龙魂", "每5秒在自身周围引发爆炸(90+12%攻击力+6%法强)。", 5,
                PassiveType.InfernoSoul, B(StatType.AttackDamage, 12f), B(StatType.AbilityPower, 6f)));
            cfg.runes.Add(Rune("山脉龙魂", "脱离战斗5秒后获得护盾(上限最大生命10%)，并获得生命/双抗加成。", 5,
                PassiveType.MountainSoul, B(StatType.MaxHP, 60f), B(StatType.Armor, 6f), B(StatType.MagicResist, 6f)));
            cfg.runes.Add(Rune("海洋龙魂", "普通攻击命中回复生命与法力。", 5,
                PassiveType.OceanSoul, B(StatType.HPRegen, 3f), B(StatType.Mana, 40f), B(StatType.MaxHP, 40f)));
            cfg.runes.Add(Rune("海克斯科技龙魂", "周期性使下一次伤害型技能或攻击触发连锁闪电(50真实伤害，弹射至多3个额外目标，减速)，内置冷却8秒。", 5,
                PassiveType.HextechLightning, B(StatType.AttackSpeed, 0.95f)));

            // ── 黄金强化符文(技能强化/功能) ──
            cfg.runes.Add(Rune("循环往复", "提供 60 技能急速。", 15, B(StatType.AbilityHaste, 60f)));
            cfg.runes.Add(Rune("术士果汁盒", "根据法术强度获得全能吸血，每 100 法强额外 3.5%。", 15,
                PassiveType.ArcaneVamp, B(StatType.Omnivamp, 0.1f), B(StatType.AbilityPower, 10f)));
            cfg.runes.Add(Rune("超凡邪恶", "技能命中永久获得法术强度。", 15, PassiveType.UnholyMastery, B(StatType.AbilityPower, 40f)));
            cfg.runes.Add(Rune("牙仙子", "每颗牙齿藏品给予 5 穿甲与 5 法术穿透。", 15,
                PassiveType.ToothTally, B(StatType.ArmorPen, 5f), B(StatType.MagicPen, 5f)));
            cfg.runes.Add(Rune("魔法飞弹", "技能命中发射真实伤害飞弹(基于目标最大生命)。", 15,
                PassiveType.MagicMissile, B(StatType.AbilityPower, 10f), B(StatType.MagicPen, 5f)));
            cfg.runes.Add(Rune("豪猪尖刺", "受到伤害累积尖刺层数，满层爆发并减速周围敌人。", 15,
                PassiveType.Porcupine, B(StatType.Armor, 15f), B(StatType.MagicResist, 15f)));
            cfg.runes.Add(Rune("坦克引擎", "参与击杀后体型变大并永久提升最大生命。", 15, PassiveType.TankGrowth, B(StatType.MaxHP, 80f)));
            cfg.runes.Add(Rune("缩小引擎", "参与击杀后变小并获得技能急速与移动速度。", 15,
                PassiveType.ShrinkBoost, B(StatType.AbilityHaste, 8f), B(StatType.MoveSpeed, 0.3f), B(StatType.Size, 0.96f)));

            // ── 棱彩强化符文(高级大额) ──
            cfg.runes.Add(Rune("炼狱导管", "技能命中施加持续5秒灼烧(6+14%额外攻击力+6%法强)，灼烧每造成一次伤害使各基础技能冷却-0.08秒。", 25,
                PassiveType.InfernalConduit, B(StatType.AbilityPower, 10f)));
            cfg.runes.Add(Rune("珠光护手", "技能可暴击(145%总伤害)，获得 25% 暴击几率，每 100 法强额外 4.5% 暴击。", 25,
                PassiveType.PearledFist, B(StatType.CritChance, 0.25f), B(StatType.AbilityPower, 15f)));
            cfg.runes.Add(Rune("双刀流", "普通攻击额外发射一枚 40% 伤害的次级箭矢，获得 20% 总攻速。", 25,
                PassiveType.TwinBlade, B(StatType.AttackSpeed, 0.8f), B(StatType.AttackDamage, 10f)));
            cfg.runes.Add(Rune("歌利亚巨人", "获得 35% 额外最大生命、15% 适应之力与 50% 体型。", 25,
                B(StatType.MaxHP, 150f), B(StatType.AttackDamage, 15f), B(StatType.Size, 1.15f)));
            cfg.runes.Add(Rune("亮出你的剑", "视为近战：+30%攻击力、+25%攻速、+30%生命、+20%吸血、+25%移速。", 25,
                B(StatType.AttackDamage, 25f), B(StatType.AttackSpeed, 0.75f),
                B(StatType.MaxHP, 50f), B(StatType.Omnivamp, 0.2f), B(StatType.MoveSpeed, 0.6f)));
            cfg.runes.Add(Rune("物法皆修", "攻击叠法术强度，技能叠攻击力，可无限叠加。", 25,
                PassiveType.BinaryAmp, B(StatType.AttackDamage, 10f), B(StatType.AbilityPower, 15f)));
            cfg.runes.Add(Rune("蛋白粉奶昔", "获得 25% 治疗和护盾强度。", 25, B(StatType.HealShieldPower, 0.25f)));
            cfg.runes.Add(Rune("尤里卡", "相当于 30% 法术强度的技能急速。", 25,
                B(StatType.AbilityHaste, 20f), B(StatType.AbilityPower, 10f)));
            cfg.runes.Add(Rune("最万用的瞄准镜", "近战获得 250 攻击距离，远程获得 150 攻击距离。", 25, B(StatType.AttackRange, 2f)));
            cfg.runes.Add(Rune("踢踏舞", "普攻获得移动速度，并拥有相当于总移速 10% 的额外攻速。", 25,
                PassiveType.Tiptoe, B(StatType.AttackSpeed, 0.9f), B(StatType.MoveSpeed, 0.5f)));
            cfg.runes.Add(Rune("无限循环往复", "初始 60 技能急速，每击杀额外获得技能急速。", 25,
                PassiveType.InfCycle, B(StatType.AbilityHaste, 60f), B(StatType.AttackDamage, 5f)));
            cfg.runes.Add(Rune("大招工具人", "技能急速翻倍作用于终极技能，获得 100 技能急速。", 25, B(StatType.AbilityHaste, 100f)));

            // 去重防呆
            var seen = new HashSet<string>();
            foreach (ShopItemData r in new List<ShopItemData>(cfg.runes))
                if (string.IsNullOrEmpty(r.displayName) || !seen.Add(r.displayName))
                    cfg.runes.Remove(r);
        }

        /// <summary>符文工厂：多组属性 + 机制文案(选择后 ApplyBonus 施加)。</summary>
        static ShopItemData Rune(string name, string desc, int price, params StatBonus[] stats) =>
            Rune(name, desc, price, PassiveType.None, stats);

        /// <summary>符文工厂(机制版)：额外绑定战斗被动(灼烧减CD/连锁闪电等)，经 ApplyBonus 写入掩码生效。</summary>
        static ShopItemData Rune(string name, string desc, int price, PassiveType type, params StatBonus[] stats)
        {
            var i = ScriptableObject.CreateInstance<ShopItemData>();
            i.displayName = name; i.basePrice = price; i.passive = desc; i.passiveType = type;
            for (int k = 0; k < stats.Length; k++) i.bonuses.Add(stats[k]);
            return i;
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

        /// <summary>创建一个职业：基础属性 + 开局武器类型 + Q/R 技能。</summary>
        static ClassData Class(string name, string desc, WeaponType weapon,
            float damage, float ap, float hp, float interval, float ms, float armor, float mr, float mana, float haste,
            ClassSkillData q, ClassSkillData r)
        {
            return new ClassData
            {
                Name = name, Desc = desc, Weapon = weapon,
                damage = damage, abilityPower = ap, maxHP = hp, attackInterval = interval,
                moveSpeed = ms, armor = armor, magicResist = mr, maxMana = mana, abilityHaste = haste,
                QSkill = q, RSkill = r,
            };
        }

        /// <summary>LOL 风格装备：多组属性 + 被动/主动文案(仅展示)。</summary>
        static ShopItemData Equip(string name, int baseP, string passive, params StatBonus[] stats)
        {
            var i = ScriptableObject.CreateInstance<ShopItemData>();
            i.displayName = name; i.basePrice = baseP; i.passive = passive;
            i.iconKey = IconKeyOf(name); // 对应 Resources/Items 下的图标文件(缺失则无图标,文字兜底)
            for (int k = 0; k < stats.Length; k++) i.bonuses.Add(stats[k]);
            return i;
        }

        /// <summary>装备图标文件映射：中文名 → Resources/Items 下的英文小写文件名(无扩展名)。</summary>
        static readonly System.Collections.Generic.Dictionary<string, string> ItemIcons = new()
        {
            { "无尽之刃", "infinity_edge" }, { "三相之力", "trinity_force" },
            { "斯特拉克的挑战护手", "steraks_gage" }, { "黑色切割者", "black_cleaver" },
            { "死亡之舞", "deaths_dance" }, { "破败王者之刃", "blade_of_ruined_king" },
            { "饮血剑", "bloodthirster" }, { "海克斯镜片 C44", "hextech_lens_c44" },
            { "霸王血铠", "tyrant_blood_armor" },
            { "兰德里的折磨", "liandrys_torment" }, { "灭世者的死亡之帽", "rabadons_deathcap" },
            { "虚空之杖", "void_staff" }, { "蜕生", "malignance" }, { "裂隙制造者", "riftmaker" },
            { "暗夜收割者", "night_harvester" }, { "影焰", "shadowflame" },
            { "卢安娜的飓风", "runaans_hurricane" }, { "猎魔人弩箭", "hunter_crossbow" },
            { "日炎圣盾", "sunfire_aegis" }, { "荆棘之甲", "thornmail" },
            { "自然之力", "force_of_nature" }, { "振奋盔甲", "spirit_visage" },
            { "狂徒铠甲", "warmogs_armor" }, { "无终恨意", "unending_despair" },
            { "原生质护带", "primordial_sash" }, { "亡者的板甲", "dead_mans_plate" },
            { "深渊面具", "abyssal_mask" },
            { "狂战士胫甲", "berserker_greaves" }, { "法师之靴", "sorcerers_shoes" },
            { "铁板靴", "steelcaps" }, { "水银之靴", "mercury_treads" },
            { "明朗之靴", "lucidity_boots" }, { "轻灵之靴", "swiftness_boots" },
        };

        static string IconKeyOf(string name) => ItemIcons.TryGetValue(name, out var v) ? v : "";

        /// <summary>按名称给已添加的装备绑定战斗被动(位掩码叠加，多件被动装备可共存)。</summary>
        static void MarkPassive(List<ShopItemData> items, string name, PassiveType type)
        {
            ShopItemData item = items.Find(s => s.displayName == name);
            if (item == null) return;
            item.passiveType = type;
        }

        /// <summary>按名称给已添加的装备绑定主动效果(装备栏 1-0 键触发)与基础冷却。</summary>
        static void MarkActive(List<ShopItemData> items, string name, ActiveType type, float cooldown)
        {
            ShopItemData item = items.Find(s => s.displayName == name);
            if (item == null) return;
            item.activeType = type;
            item.activeCooldown = cooldown;
        }

        static StatBonus B(StatType t, float v) => new StatBonus(t, v);

        static WaveBatch B(EnemyData e, int count, float interval) =>
            new WaveBatch { enemy = e, count = count, spawnInterval = interval };
    }
}