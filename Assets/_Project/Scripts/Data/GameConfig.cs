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

            // 基础道具(低价常见)：单属性，保留原价带
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

            // ── 装备导入(LOL 风格，数值按当前基数缩放：基础攻击10/生命120/移速8/价带10~45) ──
            // 缩放基准：攻击力/8、法术强度/15、生命/10、护甲魔抗/3.5、移速/45→量、攻速按 1/(1+%)、
            // 暴击/2、全能吸血/2、价格/80。被动主动仅文案展示，战斗挂钩后续实现。
            #region AD 攻击力装备
            cfg.shopItems.Add(Equip("无尽之刃", 42, "",
                B(StatType.AttackDamage, 9f), B(StatType.CritChance, 0.12f), B(StatType.CritDamage, 0.4f)));
            cfg.shopItems.Add(Equip("三相之力", 40, "被动“追击”：施放技能后下一次普攻附加额外物理伤害；被动“加速”：普攻后获得移动速度。",
                B(StatType.AttackDamage, 4f), B(StatType.AttackSpeed, 0.77f), B(StatType.MaxHP, 35f), B(StatType.AbilityHaste, 5f)));
            cfg.shopItems.Add(Equip("斯特拉克的挑战护手", 38, "被动“救主灵刃”：生命值低于30%时获得护盾。",
                B(StatType.AttackDamage, 5f), B(StatType.MaxHP, 40f)));
            cfg.shopItems.Add(Equip("黑色切割者", 38, "被动“切割”：物理伤害会削减目标护甲。",
                B(StatType.AttackDamage, 5f), B(StatType.MaxHP, 40f), B(StatType.AbilityHaste, 7f)));
            cfg.shopItems.Add(Equip("死亡之舞", 38, "所受伤害的一部分将以流血形式在3秒内持续扣除。",
                B(StatType.AttackDamage, 7f), B(StatType.Armor, 13f), B(StatType.AbilityHaste, 5f)));
            cfg.shopItems.Add(Equip("玛莫提乌斯之噬", 38, "被动“救主灵刃”：承受魔法伤害使生命值过低时获得护盾与全能吸血。",
                B(StatType.AttackDamage, 7f), B(StatType.MagicResist, 14f), B(StatType.AbilityHaste, 5f)));
            cfg.shopItems.Add(Equip("朔极之矛", 38, "被动：技能命中敌人后提升下一次技能的伤害。",
                B(StatType.AttackDamage, 8f), B(StatType.MaxHP, 45f), B(StatType.AbilityHaste, 8f)));
            cfg.shopItems.Add(Equip("破败王者之刃", 40, "被动：普攻附加目标当前生命值百分比的额外物理伤害。",
                B(StatType.AttackDamage, 5f), B(StatType.AttackSpeed, 0.8f), B(StatType.Omnivamp, 0.05f)));
            cfg.shopItems.Add(Equip("巨型九头蛇", 40, "被动：普攻对周围敌人造成基于最大生命值的物理伤害。",
                B(StatType.AttackDamage, 5f), B(StatType.MaxHP, 50f)));
            cfg.shopItems.Add(Equip("贪欲九头蛇", 40, "被动：普攻和技能对周围敌人造成伤害。",
                B(StatType.AttackDamage, 9f), B(StatType.AbilityHaste, 7f), B(StatType.Omnivamp, 0.05f)));
            cfg.shopItems.Add(Equip("饮血剑", 42, "被动“猩红护盾”：生命偷取溢出的治疗量转化为护盾。",
                B(StatType.AttackDamage, 10f), B(StatType.Omnivamp, 0.07f)));
            cfg.shopItems.Add(Equip("岚切", 40, "被动“电冲”：盈能攻击附带额外魔法伤害并提供移动速度。",
                B(StatType.AttackDamage, 6f), B(StatType.AttackSpeed, 0.83f), B(StatType.CritChance, 0.12f)));
            cfg.shopItems.Add(Equip("无尽饥渴", 38, "被动“饥荒”：额外攻击力提升技能急速；被动“盛宴”：参与击杀后获得全能吸血。",
                B(StatType.AttackDamage, 7f), B(StatType.Omnivamp, 0.03f)));
            cfg.shopItems.Add(Equip("海克斯镜片 C44", 35, "被动“高倍望远镜”：距离越远伤害越高；被动“奥术瞄准”：参与击杀后获得额外攻击距离。",
                B(StatType.AttackDamage, 6f), B(StatType.CritChance, 0.12f)));
            cfg.shopItems.Add(Equip("霸王血铠", 41, "被动“专横”：获得相当于你2.5%额外生命值的攻击力。被动“报复”：获得基于你的百分比已损失生命值的12%攻击力提升。",
                B(StatType.AttackDamage, 5f), B(StatType.MaxHP, 55f)));
            cfg.shopItems[cfg.shopItems.Count - 1].passiveType = PassiveType.Tyrant;
            #endregion

            #region AP 法术强度装备
            cfg.shopItems.Add(Equip("卢登的回声", 34, "被动“回声”：技能伤害消耗回声层数造成额外伤害并弹射至附近敌人。",
                B(StatType.AbilityPower, 7f), B(StatType.AbilityHaste, 3f)));
            cfg.shopItems.Add(Equip("兰德里的折磨", 38, "被动：技能造成灼烧，基于目标最大生命值持续魔法伤害。",
                B(StatType.AbilityPower, 6f), B(StatType.MaxHP, 30f)));
            cfg.shopItems.Add(Equip("灭世者的死亡之帽", 45, "被动：法术强度提升40%。",
                B(StatType.AbilityPower, 9f)));
            cfg.shopItems.Add(Equip("虚空之杖", 38, "",
                B(StatType.AbilityPower, 5f), B(StatType.MagicPen, 8f)));
            cfg.shopItems.Add(Equip("蜕生", 38, "被动“死中焕生”：参与击杀后生成治疗新星。",
                B(StatType.AbilityPower, 5f), B(StatType.AbilityHaste, 7f), B(StatType.MagicPen, 6f)));
            cfg.shopItems.Add(Equip("裂隙制造者", 38, "被动“虚空腐蚀”：持续作战伤害渐增并获全能吸血；被动“虚空灌注”：额外生命值转化为法术强度。",
                B(StatType.AbilityPower, 5f), B(StatType.MaxHP, 35f), B(StatType.AbilityHaste, 5f)));
            cfg.shopItems.Add(Equip("视界专注", 34, "被动“高能射击”：远距离施法显形敌人并提升对其伤害。",
                B(StatType.AbilityPower, 5f), B(StatType.AbilityHaste, 8f)));
            cfg.shopItems.Add(Equip("黄昏与黎明", 38, "被动“咒刃”：施放技能后下一次攻击附加额外魔法伤害。",
                B(StatType.AbilityPower, 5f), B(StatType.MaxHP, 30f), B(StatType.AbilityHaste, 7f), B(StatType.AttackSpeed, 0.8f)));
            cfg.shopItems.Add(Equip("实现者", 38, "主动“法力具现”：消耗法力回血。",
                B(StatType.AbilityPower, 6f), B(StatType.AbilityHaste, 3f)));
            MarkActive(cfg.shopItems, "实现者", ActiveType.ManaMeld, 8f);
            cfg.shopItems.Add(Equip("海克斯科技枪刃", 38, "主动→被动“奥术弹”：每3秒(受技能急速缩减)自动对最近敌人发射魔法弹，消耗6法力，造伤15+法强×0.7。",
                B(StatType.AbilityPower, 5f), B(StatType.AttackDamage, 5f), B(StatType.Omnivamp, 0.05f)));
            cfg.shopItems[cfg.shopItems.Count - 1].passiveType = PassiveType.ArcaneBolt;
            cfg.shopItems.Add(Equip("暗夜收割者", 38, "被动：对敌方英雄造成伤害时附加额外魔法伤害并提供移动速度。",
                B(StatType.AbilityPower, 6f), B(StatType.MaxHP, 30f), B(StatType.AbilityHaste, 8f)));
            cfg.shopItems.Add(Equip("影焰", 38, "被动：对低生命值敌人造成暴击伤害。",
                B(StatType.AbilityPower, 7f), B(StatType.MaxHP, 20f)));
            cfg.shopItems.Add(Equip("风暴狂涌", 36, "被动：对敌方英雄造成伤害后触发额外魔法伤害和移动速度。",
                B(StatType.AbilityPower, 6f), B(StatType.AbilityHaste, 5f), B(StatType.MoveSpeed, 0.8f)));
            cfg.shopItems.Add(Equip("残疫", 35, "被动：终极技能获得额外冷却缩减并对敌人施加灼烧。",
                B(StatType.AbilityPower, 6f), B(StatType.AbilityHaste, 7f)));
            #endregion

            #region 攻速与暴击装备
            cfg.shopItems.Add(Equip("卢安娜的飓风", 35, "被动：普攻向附近敌人发射分裂箭，造成部分攻击力的物理伤害。",
                B(StatType.AttackSpeed, 0.71f), B(StatType.CritChance, 0.12f), B(StatType.MoveSpeed, 0.4f)));
            cfg.shopItems.Add(Equip("疾射火炮", 32, "被动：盈能攻击获得额外攻击距离和魔法伤害。",
                B(StatType.AttackSpeed, 0.74f), B(StatType.CritChance, 0.12f), B(StatType.MoveSpeed, 0.3f)));
            cfg.shopItems.Add(Equip("幻影之舞", 32, "被动：普攻后获得攻击速度和移动速度。",
                B(StatType.AttackSpeed, 0.71f), B(StatType.CritChance, 0.12f), B(StatType.MoveSpeed, 0.55f)));
            cfg.shopItems.Add(Equip("猎魔人弩箭", 33, "被动“开战弹幕”：施放终极技能后一段时间内获得攻速并必定暴击。",
                B(StatType.AttackSpeed, 0.71f), B(StatType.CritChance, 0.12f), B(StatType.MoveSpeed, 0.3f)));
            #endregion

            #region 坦克与防御装备
            cfg.shopItems.Add(Equip("日炎圣盾", 35, "被动“献祭”：对周围敌人造成持续魔法伤害。",
                B(StatType.MaxHP, 45f), B(StatType.Armor, 9f), B(StatType.MagicResist, 9f), B(StatType.AbilityHaste, 5f)));
            cfg.shopItems.Add(Equip("冰霜之心", 31, "被动：降低周围敌人的攻击速度。",
                B(StatType.Armor, 14f), B(StatType.AbilityHaste, 7f)));
            cfg.shopItems.Add(Equip("荆棘之甲", 34, "被动：受到普攻时反弹魔法伤害并施加重伤。",
                B(StatType.Armor, 17f), B(StatType.MaxHP, 40f)));
            cfg.shopItems.Add(Equip("兰顿之兆", 34, "被动：受到暴击时减少伤害；主动：对周围敌人造成魔法伤害。",
                B(StatType.Armor, 17f), B(StatType.MaxHP, 40f)));
            MarkActive(cfg.shopItems, "兰顿之兆", ActiveType.AoeBlast, 8f);
            cfg.shopItems.Add(Equip("自然之力", 35, "被动：受到技能伤害时获得移动速度和魔法抗性。",
                B(StatType.MagicResist, 17f), B(StatType.MaxHP, 40f), B(StatType.MoveSpeed, 0.4f)));
            cfg.shopItems.Add(Equip("振奋盔甲", 34, "被动：提升所有治疗和护盾效果。",
                B(StatType.MaxHP, 40f), B(StatType.MagicResist, 11f), B(StatType.AbilityHaste, 7f)));
            cfg.shopItems.Add(Equip("狂徒铠甲", 38, "被动“狂徒之心”：脱离战斗后每0.5秒回复最大生命值。",
                B(StatType.MaxHP, 100f), B(StatType.HPRegen, 0.2f)));
            cfg.shopItems.Add(Equip("无终恨意", 35, "被动“苦楚”：战斗中对附近敌人造成伤害并治疗自身。",
                B(StatType.Armor, 14f), B(StatType.MaxHP, 40f), B(StatType.AbilityHaste, 5f)));
            cfg.shopItems.Add(Equip("原生质护带", 31, "被动“救主灵刃”：生命值过低时获得护盾、持续治疗与移速/体型提升。",
                B(StatType.MaxHP, 60f), B(StatType.AbilityHaste, 5f)));
            cfg.shopItems.Add(Equip("亡者的板甲", 36, "被动：移动积攒气势，普攻消耗气势造成额外伤害。",
                B(StatType.Armor, 17f), B(StatType.MaxHP, 30f), B(StatType.MoveSpeed, 0.4f)));
            cfg.shopItems.Add(Equip("深渊面具", 35, "被动：技能伤害使目标承受更多魔法伤害。",
                B(StatType.MagicResist, 17f), B(StatType.MaxHP, 40f), B(StatType.AbilityHaste, 7f)));
            #endregion

            #region 辅助装备
            cfg.shopItems.Add(Equip("班德尔音管", 25, "被动“嘹亮旋律”：减速/定身敌人后获得移速并强化附近友军攻速。",
                B(StatType.MaxHP, 20f), B(StatType.Armor, 6f), B(StatType.MagicResist, 6f), B(StatType.AbilityHaste, 5f)));
            cfg.shopItems.Add(Equip("歌之权冠", 22, "被动：基于最大法力值提升治疗与护盾强度。",
                B(StatType.MaxHP, 20f), B(StatType.HealShieldPower, 0.15f)));
            cfg.shopItems.Add(Equip("米凯尔的祝福", 29, "主动：解除友方英雄身上的控制效果并治疗。",
                B(StatType.MaxHP, 25f), B(StatType.AbilityHaste, 5f)));
            cfg.shopItems.Add(Equip("炽热香炉", 29, "被动：为友军提供护盾/治疗时使其获得攻击速度与额外魔法伤害。",
                B(StatType.MaxHP, 20f), B(StatType.AbilityPower, 3f), B(StatType.AbilityHaste, 5f)));
            cfg.shopItems.Add(Equip("流水法杖", 29, "被动：为友军提供护盾/治疗时使其获得法术强度与技能急速。",
                B(StatType.MaxHP, 20f), B(StatType.AbilityPower, 3f), B(StatType.AbilityHaste, 3f)));
            cfg.shopItems.Add(Equip("救赎", 29, "主动：治疗自身并对周围敌人造成魔法伤害。",
                B(StatType.MaxHP, 20f), B(StatType.AbilityHaste, 5f)));
            MarkActive(cfg.shopItems, "救赎", ActiveType.Redemption, 10f);
            cfg.shopItems.Add(Equip("舒瑞娅的狂想曲", 31, "主动：短暂大幅提升移动速度。",
                B(StatType.AbilityPower, 2f), B(StatType.MaxHP, 35f), B(StatType.AbilityHaste, 5f)));
            MarkActive(cfg.shopItems, "舒瑞娅的狂想曲", ActiveType.MoveBurst, 12f);
            cfg.shopItems.Add(Equip("基克的聚合", 28, "被动：施放终极技能时生成冰霜风暴，减速敌人并强化友军攻击。",
                B(StatType.MaxHP, 20f), B(StatType.Armor, 6f), B(StatType.MagicResist, 6f), B(StatType.AbilityHaste, 5f)));
            #endregion

            #region 鞋子
            cfg.shopItems.Add(Equip("狂战士胫甲", 14, "",
                B(StatType.AttackSpeed, 0.77f), B(StatType.MoveSpeed, 1f)));
            cfg.shopItems.Add(Equip("法师之靴", 14, "",
                B(StatType.MoveSpeed, 1f), B(StatType.MagicPen, 5f)));
            cfg.shopItems.Add(Equip("铁板靴", 14, "被动：减少来自普攻的伤害。",
                B(StatType.MoveSpeed, 1f), B(StatType.Armor, 6f)));
            cfg.shopItems.Add(Equip("水银之靴", 15, "被动：减免控制时间（韧性30%）。",
                B(StatType.MoveSpeed, 1f), B(StatType.MagicResist, 7f)));
            cfg.shopItems.Add(Equip("明朗之靴", 12, "",
                B(StatType.MoveSpeed, 1f), B(StatType.AbilityHaste, 5f)));
            cfg.shopItems.Add(Equip("轻灵之靴", 13, "被动：减速抗性25%。",
                B(StatType.MoveSpeed, 1.3f)));
            cfg.shopItems.Add(Equip("暴食胫甲", 12, "被动“猎杀”：参与击杀时获得全能吸血，可叠加6层。",
                B(StatType.MoveSpeed, 1f), B(StatType.Omnivamp, 0.02f)));
            cfg.shopItems.Add(Equip("贪婪胫甲", 12, "被动：生命值高于50%时造成额外伤害，低于50%时治疗护盾回复提升。",
                B(StatType.MoveSpeed, 1f), B(StatType.Omnivamp, 0.02f)));
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

        /// <summary>LOL 风格装备：多组属性 + 被动/主动文案(仅展示)。</summary>
        static ShopItemData Equip(string name, int baseP, string passive, params StatBonus[] stats)
        {
            var i = ScriptableObject.CreateInstance<ShopItemData>();
            i.displayName = name; i.basePrice = baseP; i.passive = passive;
            for (int k = 0; k < stats.Length; k++) i.bonuses.Add(stats[k]);
            return i;
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