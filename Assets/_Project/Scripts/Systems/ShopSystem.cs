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

        public bool IsAwaitingChoice { get; private set; }
        public IReadOnlyList<ShopSlot> Slots => slots;

        public int PriceOf(ShopItemData item) => item != null ? item.basePrice : 0;

        /// <summary>开启本回合商店：清空并按池子补满 3 个槽位，触发 UI 展示。</summary>
        public void OpenOffer()
        {
            IsAwaitingChoice = true;
            slots.Clear();
            for (int i = 0; i < SlotCount; i++) slots.Add(new ShopSlot());
            for (int i = 0; i < slots.Count; i++) FillSlot(slots[i]);
            GameEvents.RaiseShop(this);
        }

        /// <summary>购买槽位装备：固定价格，买后槽位清空(锁定状态保留)。</summary>
        public bool TryPurchase(int slotIndex)
        {
            if (!IsAwaitingChoice || stats == null) return false;
            if (slotIndex < 0 || slotIndex >= slots.Count) return false;
            ShopSlot slot = slots[slotIndex];
            if (slot.Item == null) return false;

            int price = slot.Item.basePrice;
            if (stats.Gold < price) return false;

            stats.AddGold(-price);
            stats.ApplyBonus(slot.Item);
            slot.Item = null;
            return true;
        }

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

        /// <summary>结束商店，进入下一波。</summary>
        public void End()
        {
            for (int i = 0; i < slots.Count; i++) slots[i].Locked = false;
            IsAwaitingChoice = false;
        }

        void FillSlot(ShopSlot slot)
        {
            slot.Item = PickItem();
        }

        ShopItemData PickItem()
        {
            if (pool == null || pool.Length == 0) return null;
            return pool[rng.Next(pool.Length)];
        }
    }
}