using UnityEngine;

namespace Roguelite
{
    /// <summary>武器抽象基类：统一冷却，子类实现 Fire。</summary>
    public abstract class PlayerWeapon : MonoBehaviour
    {
        public WeaponData Data { get; private set; }

        float cooldown;

        public void Equip(WeaponData data)
        {
            Data = data;
            cooldown = 0f;
        }

        public void Tick(float dt, Vector2 origin, Vector2 dir, float statDamage, float statCritChance, float statRange, float statAttackInterval)
        {
            if (Data == null) return;
            cooldown -= dt;
            if (cooldown > 0f) return;
            // 攻速词条(AttackSpeed 乘算属性)统一作用于全部武器：武器基础间隔 × 玩家攻速基准比率
            cooldown = Data.attackInterval * (statAttackInterval / PlayerStats.AttackIntervalBase);
            Fire(origin, dir, statDamage, statCritChance, statRange);
        }

        protected abstract void Fire(Vector2 origin, Vector2 dir, float damage, float critChance, float statRange);
    }
}