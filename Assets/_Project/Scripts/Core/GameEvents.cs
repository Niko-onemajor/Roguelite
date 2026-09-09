using System;

namespace Roguelite
{
    /// <summary>全局事件中心。状态变更广播，UI/系统订阅刷新。</summary>
    public static class GameEvents
    {
        public static event Action<float, float> HPChanged;     // current, max
        public static event Action<int> GoldChanged;            // current
        public static event Action<int, int> WaveChanged;       // currentIndex(1-based), total
        public static event Action<bool, int, int> GameEnded;   // victory, wavesCleared, kills

        public static void RaiseHP(float cur, float max) => HPChanged?.Invoke(cur, max);
        public static void RaiseGold(int gold) => GoldChanged?.Invoke(gold);
        public static void RaiseWave(int index, int total) => WaveChanged?.Invoke(index, total);
        public static void RaiseGameEnded(bool victory, int wavesCleared, int kills) =>
            GameEnded?.Invoke(victory, wavesCleared, kills);

        /// <summary>域重载/退出场景时清空，防止跨场景残留。</summary>
        public static void ClearAll()
        {
            HPChanged = null; GoldChanged = null; WaveChanged = null;
            GameEnded = null;
        }
    }
}