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
            var rune = gameObject.AddComponent<RuneSystem>();
            rune.pool = cfg.shopItems;
            var wave = gameObject.AddComponent<WaveManager>();

            BuildUI(combat, shop, forge, rune, wave);

            stats.ResetForRun();
            wave.BeginRun(cfg.waves, spawner, shop, rune, stats);
        }

        #region Helpers
        Camera EnsureCamera()
        {
            if (Camera.main != null)
            {
                Camera.main.transform.position = new Vector3(0f, 0f, -10f); // 初始归位，随后由 CameraFollow 锁定玩家居中
                if (Camera.main.GetComponent<CameraFollow>() == null)
                    Camera.main.gameObject.AddComponent<CameraFollow>();
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
            go.AddComponent<CameraFollow>();
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
            float playerScale = SpriteArt.NormalizeFactor(sr.sprite, 1.5f); // 统一角色视觉尺寸(直径1.5世界单位)，比初始放大1.5倍
            go.transform.localScale = Vector3.one * playerScale;
            var rb = go.AddComponent<Rigidbody2D>();
            rb.isKinematic = true; // kinematic + MovePosition 驱动；接触伤害走 Enemy 距离判定，不依赖物理回调
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            var col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.4f / playerScale; // 世界半径=0.4：贴合人形贴图的真实身形视觉(视觉整体直径1.5，但身体仅占中间小部分)，比之前0.6更准
            go.AddComponent<PlayerStats>(); // 必须先于 PlayerController：其 Awake 需 GetComponent<PlayerStats>()
            go.AddComponent<HitFlash>(); // 玩家受击闪红
            go.AddComponent<PlayerController>();
            go.AddComponent<CombatSystem>();
            return go;
        }

        void BuildUI(CombatSystem combat, ShopSystem shop, ForgeSystem forge, RuneSystem rune, WaveManager wave)
        {
            var canvas = UIBuilder.Canvas();
            var hud = canvas.AddComponent<HudView>();
            hud.Build(canvas.transform);
            var pause = canvas.AddComponent<PauseView>();
            pause.wave = wave;
            pause.Build(canvas.transform);
            var shopView = canvas.AddComponent<ShopView>();
            shopView.shop = shop;
            shopView.forge = forge;
            shopView.Build(canvas.transform);
            var forgeView = canvas.AddComponent<ForgeView>();
            forgeView.forge = forge;
            forgeView.Build(canvas.transform);
            var runeView = canvas.AddComponent<RuneView>();
            runeView.rune = rune;
            runeView.Build(canvas.transform);
            var endless = canvas.AddComponent<EndlessChoiceView>();
            endless.Build(canvas.transform);
            var over = canvas.AddComponent<GameOverView>();
            over.Build(canvas.transform);
            // 详情面板最后构建，保证渲染层级在暂停/商店之上
            var info = canvas.AddComponent<PlayerInfoView>();
            info.combat = combat;
            info.Build(canvas.transform);
            pause.info = info;
            shopView.info = info;
        }
        #endregion
    }
}