using UnityEngine;

namespace Roguelite
{
    /// <summary>追逐型：笔直逼近玩家，接触伤害。</summary>
    public class EnemyChaser : Enemy
    {
        protected override void Behavior(float dt, PlayerController player)
        {
            if (player == null || Data == null) return;
            Vector3 dir = player.transform.position - transform.position;
            if (dir.sqrMagnitude > 0.001f)
                transform.position += dir.normalized * (Data.moveSpeed * dt);
        }
    }
}