using UnityEngine;

namespace Roguelite
{
    /// <summary>开局职业选择状态机：挂到 Bootstrap 空物体，OpenOffer 弹选择面板，
    /// 玩家四选一后 Selected 非空，GameBootstrap 据此应用职业并开波。</summary>
    public class ClassSelectSystem : MonoBehaviour
    {
        public ClassData[] classes;

        public ClassData Selected { get; private set; }
        public bool IsAwaitingChoice { get; private set; }

        /// <summary>弹出职业选择(开局、BeginRun 前)。</summary>
        public void OpenOffer()
        {
            IsAwaitingChoice = true;
            Selected = null;
            GameEvents.RaiseClassOffer(this);
        }

        /// <summary>选择第 idx 个职业生效；选择后不可再改。</summary>
        public void Choose(int idx)
        {
            if (!IsAwaitingChoice || classes == null || idx < 0 || idx >= classes.Length) return;
            Selected = classes[idx];
            IsAwaitingChoice = false;
        }
    }
}