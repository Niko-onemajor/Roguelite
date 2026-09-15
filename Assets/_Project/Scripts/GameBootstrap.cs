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

            var cam = EnsureCamera();
            EnsureEventSystem();
            ArenaBackground.Build(cam);

            GameConfig cfg = GameConfig.Default();

            GameObject playerGo = EnsurePlayer();
            var stats = playerGo.GetComponent<PlayerStats>();
            var combat = playerGo.GetComponent<CombatSystem>();
            combat.ClearEquips();
            combat.Equip(cfg.weapons[0]); // 初始仅 1 把武器，避免开局齐射；其余武器留待商店扩展

            var spawner = gameObject.AddComponent<EnemySpawner>();
            var shop = gameObject.AddComponent<ShopSystem>();
            shop.stats = stats;
            shop.pool = cfg.shopItems.ToArray();
            var forge = gameObject.AddComponent<ForgeSystem>();
            forge.Pool = cfg.shopItems;
            var wave = gameObject.AddComponent<WaveManager>();

            BuildUI(shop, forge);

            stats.ResetForRun();
            wave.BeginRun(cfg.waves, spawner, shop, forge, stats);
        }

        #region Helpers
        Camera EnsureCamera()
        {
            if (Camera.main != null)
            {
                Camera.main.transform.position = new Vector3(0f, 0f, -10f); // 竞技场固定视角
                return Camera.main;
            }
            var go = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            go.tag = "MainCamera";
            var cam = go.GetComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 6f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.08f, 0.09f, 0.1f);
            go.transform.position = new Vector3(0f, 0f, -10f);
            return cam;
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
            float playerScale = SpriteArt.NormalizeFactor(sr.sprite, 1f); // 统一角色视觉尺寸(直径1世界单位)，同占位圆
            go.transform.localScale = Vector3.one * playerScale;
            var rb = go.AddComponent<Rigidbody2D>();
            rb.isKinematic = true; // kinematic + MovePosition 驱动；接触伤害走 Enemy 距离判定，不依赖物理回调
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            var col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.4f / playerScale; // 世界半径=0.4，视觉直径1单位的0.8倍贴合碰撞
            go.AddComponent<PlayerStats>(); // 必须先于 PlayerController：其 Awake 需 GetComponent<PlayerStats>()
            go.AddComponent<HitFlash>(); // 玩家受击闪红
            go.AddComponent<PlayerController>();
            go.AddComponent<CombatSystem>();
            return go;
        }

        void BuildUI(ShopSystem shop, ForgeSystem forge)
        {
            var canvas = UIBuilder.Canvas();
            var hud = canvas.AddComponent<HudView>();
            hud.Build(canvas.transform);
            var shopView = canvas.AddComponent<ShopView>();
            shopView.shop = shop;
            shopView.Build(canvas.transform);
            var forgeView = canvas.AddComponent<ForgeView>();
            forgeView.forge = forge;
            forgeView.Build(canvas.transform);
            var endless = canvas.AddComponent<EndlessChoiceView>();
            endless.Build(canvas.transform);
            var over = canvas.AddComponent<GameOverView>();
            over.Build(canvas.transform);
        }
        #endregion
    }
}