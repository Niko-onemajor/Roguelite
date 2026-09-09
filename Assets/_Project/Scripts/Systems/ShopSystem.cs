using System.Collections.Generic;
using UnityEngine;

namespace Roguelite
{
    /// <summary>波间商店：3 选 1，价格随购买次数递增。</summary>
    public class ShopSystem : MonoBehaviour
    {
        public ShopItemData[] pool;
        public PlayerStats stats;

        readonly Dictionary<ShopItemData, int> purchasedCount = new Dictionary<ShopItemData, int>();
        readonly System.Random rng = new System.Random();

        public bool IsAwaitingChoice { get; private set; }

        public int PriceOf(ShopItemData item)
        {
            int n = purchasedCount.TryGetValue(item, out int v) ? v : 0;
            return item.basePrice + item.priceStep * n;
        }

        public ShopOffer OpenOffer()
        {
            IsAwaitingChoice = true;
            var offer = new ShopOffer();
            var available = new List<ShopItemData>(pool);
            while (offer.items.Count < 3 && available.Count > 0)
            {
                int idx = rng.Next(available.Count);
                var item = available[idx];
                available.RemoveAt(idx);
                offer.items.Add(item);
                offer.prices.Add(PriceOf(item));
            }
            GameEvents.RaiseShop(offer);
            return offer;
        }

        public bool TryPurchase(ShopItemData item)
        {
            if (!IsAwaitingChoice || stats == null) return false;
            int price = PriceOf(item);
            if (stats.Gold < price) return false;
            stats.AddGold(-price);
            stats.ApplyBonus(item);
            purchasedCount[item] = purchasedCount.TryGetValue(item, out int n) ? n + 1 : 1;
            IsAwaitingChoice = false;
            return true;
        }
    }
}