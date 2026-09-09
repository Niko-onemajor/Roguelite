using UnityEngine;

namespace Roguelite
{
    public enum WeaponType { Ranged, Melee, AoE }

    [CreateAssetMenu(fileName = "WeaponData", menuName = "Roguelite/WeaponData")]
    public class WeaponData : ScriptableObject
    {
        public string displayName = "武器";
        public WeaponType type = WeaponType.Ranged;
        public float damage = 10f;          // 单次命中伤害
        public float attackInterval = 0.8f; // 两次开火间隔(秒)
        public float range = 6f;            // 远程=射程 / 近战=挥砍半径 / 范围=爆炸半径
        public float speed = 10f;           // 远程弹速
        public float halfAngle = 100f;      // 近战扇区半角(度)
        public Color color = Color.yellow;  // 弹丸占位色
    }
}