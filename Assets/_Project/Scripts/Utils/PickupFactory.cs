using System.Collections.Generic;
using UnityEngine;

namespace Roguelite
{
    /// <summary>金币拾取物工厂。维护场上(未拾取)金币登记表，供回合末 BankAll 自动入库。</summary>
    public static class PickupFactory
    {
        static GameObject prototype;

        /// <summary>当前场上未拾取的金币拾取物。</summary>
        public static readonly List<Pickup> Active = new List<Pickup>();

        public static GameObject Prototype
        {
            get
            {
                if (prototype == null)
                {
                    prototype = new GameObject("Coin",
                        typeof(SpriteRenderer), typeof(Rigidbody2D), typeof(CircleCollider2D), typeof(Poolable), typeof(Pickup));
                    Rigidbody2D rb = prototype.GetComponent<Rigidbody2D>();
                    rb.isKinematic = true;
                    rb.gravityScale = 0f;
                    CircleCollider2D col = prototype.GetComponent<CircleCollider2D>();
                    col.isTrigger = true;
                    col.radius = 0.18f;
                    SpriteRenderer sr = prototype.GetComponent<SpriteRenderer>();
                    sr.sprite = SpriteArt.LoadOrPlaceholder("coin");
                    sr.sortingOrder = 3;
                    prototype.transform.localScale = Vector3.one * SpriteArt.NormalizeFactor(sr.sprite, 0.5f);
                    prototype.SetActive(false);
                }
                return prototype;
            }
        }

        public static Pickup Spawn(Vector3 position, int gold)
        {
            GameObject go = PoolManager.Spawn(Prototype, position, Quaternion.identity);
            Pickup pickup = go.GetComponent<Pickup>();
            pickup.SetGold(gold, Color.yellow);
            return pickup;
        }

        /// <summary>注册场上拾取物(Pickup.OnEnable 调用)。</summary>
        public static void Register(Pickup p)
        {
            if (p != null && !Active.Contains(p)) Active.Add(p);
        }

        /// <summary>注销(Pickup.OnDisable 调用，回收后不再参与入库)。</summary>
        public static void Unregister(Pickup p)
        {
            if (p != null) Active.Remove(p);
        }

        /// <summary>把场上所有未拾取金币一次性入库并回收（回合倒计时结束时调用，像土豆兄弟自动结算）。</summary>
        public static void BankAll()
        {
            if (PlayerStats.Instance == null) return;
            int total = 0;
            var snapshot = new List<Pickup>(Active);
            foreach (Pickup p in snapshot)
            {
                if (p == null) continue;
                total += p.Gold;
                Unregister(p); // 显式注销，不依赖 OnDisable(批处理 EditMode 不触发生命周期)
                PoolManager.Return(p.gameObject);
            }
            if (total > 0) PlayerStats.Instance.AddGold(total);
        }
    }
}