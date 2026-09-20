using System.Collections;
using UnityEngine;

namespace Roguelite
{
    /// <summary>受击闪色反馈：临时变色 duration 秒后恢复。
    /// 玩家受伤闪红由 PlayerStats.TakeDamage 调用 Flash；敌人受击闪白由 Enemy.TakeDamage 调用 Flash。</summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class HitFlash : MonoBehaviour
    {
        SpriteRenderer sr;
        Coroutine flashRoutine;

        void Awake()
        {
            sr = GetComponent<SpriteRenderer>();
        }

        /// <summary>闪色：临时变色 duration 秒后恢复。以调用时刻颜色为恢复目标，兼容实体初始化染色。
        /// inactive 对象无法 StartCoroutine(回池/接触消失的敌人仍可能被残余的持续性伤害触发)，直接忽略。</summary>
        public void Flash(Color flashColor, float duration = 0.12f)
        {
            if (sr == null || !gameObject.activeInHierarchy) return;
            if (flashRoutine != null) StopCoroutine(flashRoutine);
            flashRoutine = StartCoroutine(FlashRoutine(flashColor, duration));
        }

        IEnumerator FlashRoutine(Color flashColor, float duration)
        {
            Color restore = sr.color; // 记录当前色作为恢复目标
            sr.color = flashColor;
            yield return new WaitForSeconds(duration);
            sr.color = restore;
            flashRoutine = null;
        }
    }
}