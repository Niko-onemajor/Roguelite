using System;
using System.Collections.Generic;
using UnityEngine;

namespace Roguelite
{
    /// <summary>商店一次提供的 3 选 1 内容。</summary>
    [Serializable]
    public class ShopOffer
    {
        public List<ShopItemData> items = new List<ShopItemData>();
        public List<int> prices = new List<int>();
    }
}