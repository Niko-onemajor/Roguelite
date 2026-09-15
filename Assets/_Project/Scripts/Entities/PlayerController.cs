using UnityEngine;

namespace Roguelite
{
    /// <summary>WASD 移动；inputEnabled=false 表示商店/结算暂停态。</summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerController : MonoBehaviour
    {
        public static PlayerController Instance { get; private set; }

        public PlayerStats Stats { get; private set; }
        public Vector2 Facing { get; private set; } = Vector2.right;
        [HideInInspector] public bool inputEnabled = true;

        void Awake()
        {
            Stats = GetComponent<PlayerStats>();
        }

        void OnEnable()
        {
            Instance = this;
            if (Stats == null) Stats = GetComponent<PlayerStats>(); // 组件添加顺序兜底
        }

        void OnDisable()
        {
            if (Instance == this) Instance = null;
        }

        void Update()
        {
            if (!inputEnabled) return;
            Vector2 move = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            if (move.sqrMagnitude > 1f) move = Vector2.ClampMagnitude(move, 1f);
            // 与敌人一致的 transform 位移实现，杜绝物理层(插值/休眠)干扰速度表现
            transform.position += (Vector3)(move * (Stats != null ? Stats.moveSpeed : 1f) * Time.deltaTime);
            transform.position = ArenaBounds.Clamp(transform.position); // 限制在竞技场内
            if (move.sqrMagnitude > 0.0001f) Facing = move.normalized;
        }

        /// <summary>调试：Scene 视图绘制玩家碰撞体积线框(绿色圆)，核对贴图与碰撞盒是否贴合。</summary>
        void OnDrawGizmos()
        {
            CircleCollider2D col = GetComponent<CircleCollider2D>();
            if (col == null) return;
            Gizmos.color = new Color(0f, 1f, 0.2f, 0.9f);
            Gizmos.DrawWireSphere(transform.position, col.radius * Mathf.Max(0.0001f, transform.localScale.x));
        }
    }
}