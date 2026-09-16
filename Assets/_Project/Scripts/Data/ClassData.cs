namespace Roguelite
{
    /// <summary>职业技能类型（Q=基础技能，R=大招），效果实现在 PlayerStats#region 职业技能。</summary>
    public enum ClassSkillType
    {
        None,
        MageDeathRay,   // 法师Q 死亡射线：直线沿途魔法伤害
        MageStorm,      // 法师R 烈焰风暴：敌人间弹射
        ArcherDash,     // 射手Q 闪避突袭：冲刺+下一次普攻附加伤害
        ArcherUlt,      // 射手R 定圣诀：攻速提升+普攻附加魔法伤害与溅射
        WarriorCleave,  // 战士Q 大杀四方：延迟后身周物理伤害+外圈回血
        WarriorReign,   // 战士R 终极统治：+最大生命+每秒身周魔法伤害
        TankShock,      // 坦克Q 电击疗法：持续身周魔法伤害+减伤
        TankFeast,      // 坦克R 盛宴：对最近敌人真实伤害，击杀成长生命
    }

    /// <summary>单个职业技能的数值参数（冷却受技能急速缩放，耗蓝走 TrySpendMana）。</summary>
    public class ClassSkillData
    {
        public string Name = "";
        public string Desc = "";
        public ClassSkillType Type = ClassSkillType.None;
        public float BaseDamage;  // 基础伤害
        public float ApRatio;     // 法术强度加成系数
        public float AdRatio;     // 攻击力加成系数
        public float Cooldown;    // 基础冷却(秒)，实际 = 冷却 × HasteCooldownScale
        public float ManaCost;    // 法力消耗
    }

    /// <summary>职业定义：基础属性 + 开局武器类型(近战挥砍/远程飞弹) + Q/R 技能。</summary>
    public class ClassData
    {
        public string Name = "";
        public string Desc = "";

        /// <summary>开局武器类别：Melee=身前挥砍刀，Ranged=发射飞弹（不再区分手枪/冲锋枪）。</summary>
        public WeaponType Weapon = WeaponType.Ranged;

        // ── 基础属性（ApplyClass 覆盖 PlayerStats 对应字段）──
        public float damage;
        public float abilityPower;
        public float maxHP;
        public float attackInterval;
        public float moveSpeed;
        public float armor;
        public float magicResist;
        public float maxMana;
        public float abilityHaste;

        public ClassSkillData QSkill;
        public ClassSkillData RSkill;
    }
}