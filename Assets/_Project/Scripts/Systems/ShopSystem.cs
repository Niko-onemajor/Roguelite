using System.Collections.Generic;
using UnityEngine;

namespace Roguelite
{
    /// <summary>商店槽位：一件装备 + 锁定状态(刷新时锁定槽位不换货)。</summary>
    public class ShopSlot
    {
        public ShopItemData Item;
        public bool Locked;
    }

    /// <summary>波间商店(Brotato 风格)：同时展示最多 3 件装备，右上角可锁定/解锁，
    /// 刷新(10 金币)时仅替换未锁定槽位；购买装备固定价格不再递增；
    /// 商店保持打开直到点击“结束”，锻体由商店内的“锻体”按钮触发。</summary>
    public class ShopSystem : MonoBehaviour
    {
        public ShopItemData[] pool;
        public PlayerStats stats;

        /// <summary>刷新商店的费用(金币)。</summary>
        public const int RefreshPrice = 10;

        public const int SlotCount = 3;

        readonly System.Random rng = new System.Random();
        readonly List<ShopSlot> slots = new List<ShopSlot>();

        /// <summary>已拥有的装备(按实例去重)：同一件装备只可购买一次，此后商店不再出现。</summary>
        readonly HashSet<ShopItemData> owned = new HashSet<ShopItemData>();

        public bool IsAwaitingChoice { get; private set; }
        public IReadOnlyList<ShopSlot> Slots => slots;

        public int PriceOf(ShopItemData item) => item != null ? item.basePrice : 0;

        /// <summary>开启本回合商店：保留上回合锁定的槽位(锁定装备跨回合继续锁定)，
        /// 其余槽位从(未拥有)池子补满，触发 UI 展示。</summary>
        public void OpenOffer()
        {
            IsAwaitingChoice = true;
            var retained = new List<ShopSlot>();
            foreach (var s in slots)
                if (s != null && s.Locked && s.Item != null) retained.Add(s);

            slots.Clear();
            for (int i = 0; i < SlotCount; i++)
            {
                if (i < retained.Count) slots.Add(retained[i]);
                else { var ns = new ShopSlot(); FillSlot(ns); slots.Add(ns); }
            }
            GameEvents.RaiseShop(this);
        }

        /// <summary>购买槽位装备：固定价格，买后槽位清空并解锁(同件装备不再出现在商店)。
        /// 装备栏满(8 件)时拒绝购买。</summary>
        public bool TryPurchase(int slotIndex)
        {
            if (!IsAwaitingChoice || stats == null) return false;
            if (slotIndex < 0 || slotIndex >= slots.Count) return false;
            ShopSlot slot = slots[slotIndex];
            if (slot.Item == null) return false;

            int price = slot.Item.basePrice;
            if (stats.Gold < price) return false;
            if (stats.IsEquipFull) return false; // 装备栏已满(最多8件)，需先出售再购

            stats.AddGold(-price);
            owned.Add(slot.Item); // 唯一购买：标记已拥有，此后商店不再提供该装备
            stats.ApplyBonus(slot.Item);
            stats.TryAddEquip(slot.Item); // 所有装备(含被动)装入装备栏首空槽
            slot.Item = null;
            slot.Locked = false; // 已购买槽位解除锁定，下次刷新正常补货
            return true;
        }

        /// <summary>出售装备：返还原价八折金币，移除属性与被动并清空装备栏槽位；
        /// 该装备从“已拥有”移除，此后可重新在商店出现(可再入手)。</summary>
        public bool TrySell(ShopItemData item)
        {
            if (!IsAwaitingChoice || stats == null) return false;
            if (item == null) return false;
            if (!stats.RemoveEquip(item)) return false; // 该装备不在装备栏

            stats.AddGold(SellPriceOf(item));
            owned.Remove(item);
            return true;
        }

        /// <summary>出售价格：原价八折(四舍五入)。</summary>
        public static int SellPriceOf(ShopItemData item) => item != null ? Mathf.RoundToInt(item.basePrice * 0.8f) : 0;

        /// <summary>切换槽位锁定状态(空槽不可锁)。</summary>
        public void ToggleLock(int slotIndex)
        {
            if (!IsAwaitingChoice) return;
            if (slotIndex < 0 || slotIndex >= slots.Count) return;
            ShopSlot slot = slots[slotIndex];
            if (slot.Item == null) return;
            slot.Locked = !slot.Locked;
        }

        /// <summary>刷新商店：花费 10 金币，替换所有未锁定槽位(锁定槽保持原货)。</summary>
        public bool TryRefresh()
        {
            if (!IsAwaitingChoice || stats == null) return false;
            if (stats.Gold < RefreshPrice) return false;

            stats.AddGold(-RefreshPrice);
            for (int i = 0; i < slots.Count; i++)
                if (!slots[i].Locked) FillSlot(slots[i]);
            return true;
        }

        /// <summary>结束商店，进入下一波。锁定状态与锁定装备保留，下回合打开商店延续。</summary>
        public void End()
        {
            IsAwaitingChoice = false;
        }

        void FillSlot(ShopSlot slot)
        {
            slot.Item = PickItem();
        }

        /// <summary>从未拥有的装备中按价格加权抽取；全部已入手则返回 null(槽位空置)。</summary>
        ShopItemData PickItem()
        {
            if (pool == null || pool.Length == 0) return null;
            var avail = new List<ShopItemData>(pool.Length);
            for (int i = 0; i < pool.Length; i++)
                if (!owned.Contains(pool[i])) avail.Add(pool[i]);
            if (avail.Count == 0) return null;

            int total = 0;
            for (int i = 0; i < avail.Count; i++) total += WeightOf(avail[i].basePrice);
            int roll = rng.Next(total);
            for (int i = 0; i < avail.Count; i++)
            {
                roll -= WeightOf(avail[i].basePrice);
                if (roll < 0) return avail[i];
            }
            return avail[avail.Count - 1];
        }

        static int WeightOf(int price)
        {
            if (price >= 40) return 2;  // 传说级，最稀有
            if (price >= 30) return 3;  // 史诗级
            if (price >= 20) return 5;  // 精良级
            return 8;                   // 基础/廉价，最常见
        }
    }
}