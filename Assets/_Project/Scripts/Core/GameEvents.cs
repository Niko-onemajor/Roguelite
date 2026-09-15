using System;

namespace Roguelite
{
    /// <summary>全局事件中心。状态变更广播，UI/系统订阅刷新。</summary>
    public static class GameEvents
    {
        public static event Action<float, float> HPChanged;     // current, max
        public static event Action<int> GoldChanged;            // current
        public static event Action<int> GoldBanked;             // 自动入库累计值(回合末未拾取金币)
        public static event Action<int, int> WaveChanged;       // currentIndex(1-based), total(-1=无尽)
        public static event Action<float> CombatTimeChanged;    // 回合剩余秒数(战斗倒计时)
        public static event Action<ShopSystem> ShopOpened;       // 展示商店(内含3槽+锁定状态)
        public static event Action<RuneSystem> RuneOffer;      // 符文选择展示(开局/7/11/15波前)
        public static event Action<ForgeSystem> ForgeOffer;     // 锻体卡牌展示(商店内“锻体”按钮触发)
        public static event Action<bool, int, int> GameEnded;   // victory, wavesCleared, kills
        public static event Action EndlessChoiceOffered;        // 通关脚本波后弹出 结算/无尽 选择
        public static event Action<bool> EndlessChosen;         // true=无尽, false=结算

        public static void RaiseHP(float cur, float max) => HPChanged?.Invoke(cur, max);
        public static void RaiseGold(int gold) => GoldChanged?.Invoke(gold);
        public static void RaiseGoldBanked(int bankedTotal) => GoldBanked?.Invoke(bankedTotal);
        public static void RaiseWave(int index, int total) => WaveChanged?.Invoke(index, total);
        public static void RaiseCombatTime(float remaining) => CombatTimeChanged?.Invoke(remaining);
        public static void RaiseShop(ShopSystem system) => ShopOpened?.Invoke(system);
        public static void RaiseRuneOffer(RuneSystem rune) => RuneOffer?.Invoke(rune);
        public static void RaiseForgeOffer(ForgeSystem forge) => ForgeOffer?.Invoke(forge);
        public static void RaiseGameEnded(bool victory, int wavesCleared, int kills) =>
            GameEnded?.Invoke(victory, wavesCleared, kills);
        public static void RaiseEndlessChoiceOffered() => EndlessChoiceOffered?.Invoke();
        public static void RaiseEndlessChosen(bool endless) => EndlessChosen?.Invoke(endless);

        /// <summary>域重载/退出场景时清空，防止跨场景残留。</summary>
        public static void ClearAll()
        {
            HPChanged = null; GoldChanged = null; GoldBanked = null; WaveChanged = null; CombatTimeChanged = null;
            ShopOpened = null; GameEnded = null;
            EndlessChoiceOffered = null; EndlessChosen = null; ForgeOffer = null; RuneOffer = null;
        }
    }
}