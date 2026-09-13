using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Roguelite
{
    /// <summary>运行时装配入口：把 GameBootstrap 挂到任意场景的空物体上即可开玩。
    /// Awake 中以代码构建 相机/玩家/管理器/UI，随后开始第 1 波。</summary>
    public class GameBootstrap : MonoBehaviour
    {
        static GameBootstrap _instance;

        void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;

            EnsureCamera();
            EnsureEventSystem();

            GameConfig cfg = GameConfig.Default();

            GameObject playerGo = EnsurePlayer();
            var stats = playerGo.GetComponent<PlayerStats>();
            var combat = playerGo.GetComponent<CombatSystem>();
            combat.ClearEquips();
            foreach (var w in cfg.weapons) combat.Equip(w);

            var spawner = gameObject.AddComponent<EnemySpawner>();
            var shop = gameObject.AddComponent<ShopSystem>();
            shop.stats = stats;
            shop.pool = cfg.shopItems.ToArray();
            var wave = gameObject.AddComponent<WaveManager>();

            BuildUI(shop);

            stats.ResetForRun();
            wave.BeginRun(cfg.waves, spawner, shop, stats);
        }

        #region Helpers
        void EnsureCamera()
        {
            if (Camera.main != null) return;
            var go = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            go.tag = "MainCamera";
            var cam = go.GetComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 6f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.08f, 0.09f, 0.1f);
        }

        void EnsureEventSystem()
        {
            if (FindFirstObjectByType<EventSystem>() != null) return;
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }

        GameObject EnsurePlayer()
        {
            var existing = FindFirstObjectByType<PlayerStats>(); // 仅在启动时查找一次
            if (existing != null) return existing.gameObject;

            var go = new GameObject("Player");
            go.transform.position = Vector3.zero;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = SpriteArt.LoadOrPlaceholder("player");
            sr.color = SpriteArt.HasReal("player") ? Color.white : new Color(0.3f, 0.85f, 0.6f);
            var rb = go.AddComponent<Rigidbody2D>();
            rb.isKinematic = true;
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            var col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.4f;
            go.AddComponent<PlayerController>();
            go.AddComponent<PlayerStats>();
            go.AddComponent<CombatSystem>();
            return go;
        }

        void BuildUI(ShopSystem shop)
        {
            var canvas = UIBuilder.Canvas();
            var hud = canvas.AddComponent<HudView>();
            hud.Build(canvas.transform);
            var shopView = canvas.AddComponent<ShopView>();
            shopView.shop = shop;
            shopView.Build(canvas.transform);
            var endless = canvas.AddComponent<EndlessChoiceView>();
            endless.Build(canvas.transform);
            var over = canvas.AddComponent<GameOverView>();
            over.Build(canvas.transform);
        }
        #endregion
    }
}