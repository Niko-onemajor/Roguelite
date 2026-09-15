using UnityEngine;

namespace Roguelite
{
    /// <summary>子弹原型(惰性构建，inactive 入池)。</summary>
    public static partial class ProjectileFactory
    {
        static GameObject friendly;
        static GameObject enemy;

        public static GameObject FriendlyPrototype => friendly ??= Build("Bullet_Friendly");
        public static GameObject EnemyPrototype => enemy ??= Build("Bullet_Enemy");

        static GameObject Build(string name)
        {
            GameObject go = new GameObject(name,
                typeof(SpriteRenderer), typeof(Rigidbody2D), typeof(CircleCollider2D), typeof(Poolable), typeof(Projectile));
            Rigidbody2D rb = go.GetComponent<Rigidbody2D>();
            rb.isKinematic = true;
            rb.gravityScale = 0f;
            CircleCollider2D col = go.GetComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.18f;
            SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
            sr.sprite = SpriteArt.LoadOrPlaceholder("bullet");
            float scale = SpriteArt.NormalizeFactor(sr.sprite, 0.36f);
            sr.sortingOrder = 2;
            go.transform.localScale = Vector3.one * scale;
            go.SetActive(false);
            return go;
        }
    }
}