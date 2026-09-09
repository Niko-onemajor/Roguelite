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

        Rigidbody2D rb;

        void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            Stats = GetComponent<PlayerStats>();
        }

        void OnEnable() => Instance = this;

        void OnDisable()
        {
            if (Instance == this) Instance = null;
        }

        void FixedUpdate()
        {
            if (!inputEnabled) return;
            Vector2 move = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            if (move.sqrMagnitude > 1f) move = Vector2.ClampMagnitude(move, 1f);
            rb.MovePosition(rb.position + move * (Stats != null ? Stats.moveSpeed : 1f) * Time.fixedDeltaTime);
            if (move.sqrMagnitude > 0.0001f) Facing = move.normalized;
        }
    }
}