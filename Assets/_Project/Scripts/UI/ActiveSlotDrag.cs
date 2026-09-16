using UnityEngine;
using UnityEngine.EventSystems;

namespace Roguelite
{
    /// <summary>主动装备栏槽位的拖拽组件(UGUI Drag Handler)：
    /// 拖起记录源槽 → 松手时由 HitTest 判定指针落点槽位 → 回调 OnDrop(源, 目标)。
    /// 目的槽有装备则交换位置，空槽则移入，栏外则不变化。</summary>
    public class ActiveSlotDrag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        /// <summary>该组件所属的槽位索引。</summary>
        public int SlotIndex;

        /// <summary>拖拽开始回调(用于源槽视觉反馈)。</summary>
        public System.Action<int> OnDragStart;

        /// <summary>屏幕坐标 → 槽位索引；超出所有槽返回 -1(由 HudView 提供)。</summary>
        public System.Func<Vector2, int> HitTest;

        /// <summary>拖拽结束回调(source, target)；target=-1 表示拖到栏外。</summary>
        public System.Action<int, int> OnDrop;

        public void OnBeginDrag(PointerEventData eventData)
        {
            OnDragStart?.Invoke(SlotIndex);
        }

        public void OnDrag(PointerEventData eventData)
        {
            // 拖拽全程无需额外处理；是否落位在松手时统一判定
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            int target = HitTest != null ? HitTest(eventData.position) : -1;
            OnDrop?.Invoke(SlotIndex, target);
        }
    }
}