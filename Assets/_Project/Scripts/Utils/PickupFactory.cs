using UnityEngine;

namespace Roguelite
{
    /// <summary>金币拾取物工厂。</summary>
    public static class PickupFactory
    {
        static GameObject prototype;

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
                    sr.sprite = PlaceholderArt.Circle();
                    sr.sortingOrder = 3;
                    prototype.transform.localScale = Vector3.one * 0.36f;
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
    }
}