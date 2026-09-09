using UnityEngine;

namespace Roguelite
{
    public enum EnemyType { Chaser, Ranged, Tank }

    [CreateAssetMenu(fileName = "EnemyData", menuName = "Roguelite/EnemyData")]
    public class EnemyData : ScriptableObject
    {
        public EnemyType type = EnemyType.Chaser;
        public float maxHP = 20f;
        public float moveSpeed = 2.6f;
        public float contactDamage = 10f;
        public float attackInterval = 1.5f;   // 接触伤害/远程射击间隔
        public float keepDistance = 0f;       // 远程型保持距离
        public float range = 0f;              // 远程型射击射程(0=不射击)
        public float projectileDamage = 0f;   // 远程型弹幕伤害
        public float projectileSpeed = 8f;
        public int goldMin = 1;
        public int goldMax = 2;
        public float scale = 1f;
        public Color color = Color.red;
    }
}