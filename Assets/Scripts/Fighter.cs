using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(FighterCombat))]
public class Fighter : MonoBehaviour
{
    [System.Serializable]
    public struct KeyMap
    {
        public KeyCode left;
        public KeyCode right;
        public KeyCode jump;
        public KeyCode attack;

        public KeyCode[] All()
        {
            return new[] { left, right, jump, attack };
        }
    }

    [Header("Kimlik")]
    public int playerIndex = 1;

    [Header("Tuşlar")]
    public KeyMap keys = new KeyMap
    {
        left = KeyCode.A, right = KeyCode.D, jump = KeyCode.W, attack = KeyCode.LeftShift
    };

    [Header("Hareket")]
    public float moveSpeed = 5f;
    public float jumpForce = 8f;

    [Header("Zemin")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.15f;
    public LayerMask groundLayer;

    [Header("Ring-out")]
    public Transform spawnPoint;
    public float killDepth = -8f;
    public float killWidth = 12f;

    public int Facing { get; private set; }
    public bool IsGrounded { get; private set; }

    private Rigidbody2D body;
    private FighterCombat combat;
    private FighterAnimator anim;
    private float moveInput;

    void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        combat = GetComponent<FighterCombat>();
        anim = GetComponent<FighterAnimator>();
        Facing = 1;
    }

    void Update()
    {
        if (!MatchRunning)
        {
            Freeze();
            return;
        }

        if (IsOutOfBounds())
        {
            if (MatchState.Instance != null) MatchState.Instance.ReportRingOut(this);
            Respawn();
            return;
        }

        if (combat.IsStunned)
        {
            moveInput = 0f;
            return;
        }

        moveInput = 0f;
        if (Input.GetKey(keys.left)) moveInput -= 1f;
        if (Input.GetKey(keys.right)) moveInput += 1f;

        if (moveInput > 0f) Facing = 1;
        else if (moveInput < 0f) Facing = -1;

        if (Input.GetKeyDown(keys.jump) && IsGrounded)
        {
            Jump();
        }

        if (Input.GetKeyDown(keys.attack) && combat.TryAttack(Facing) && anim != null)
        {
            anim.PlayAttack();
        }
    }

    void FixedUpdate()
    {
        if (!MatchRunning) return;

        IsGrounded = groundCheck != null
            && Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);

        if (combat.IsStunned) return;

        float targetX = moveInput * moveSpeed;
        float accel = IsGrounded ? 60f : 12f;
        float newX = Mathf.MoveTowards(body.linearVelocity.x, targetX, accel * Time.fixedDeltaTime);
        body.linearVelocity = new Vector2(newX, body.linearVelocity.y);
    }

    // MatchState yoksa (cıplak bir test sahnesi) dovusçu yine de calisir.
    bool MatchRunning
    {
        get { return MatchState.Instance == null || MatchState.Instance.IsRunning; }
    }

    // Sure bitince karakterler olduklari yerde kalir. Time.timeScale
    // kullanilmiyor: global durum ve Time.time durdugu icin bekleme
    // surelerini bozar.
    void Freeze()
    {
        if (!body.simulated) return;

        body.linearVelocity = Vector2.zero;
        body.simulated = false;
    }

    void Jump()
    {
        body.linearVelocity = new Vector2(body.linearVelocity.x, jumpForce);
        if (anim != null) anim.PlayJump();
    }

    bool IsOutOfBounds()
    {
        return transform.position.y < killDepth || Mathf.Abs(transform.position.x) > killWidth;
    }

    void Respawn()
    {
        transform.position = spawnPoint != null ? spawnPoint.position : Vector3.zero;
        body.linearVelocity = Vector2.zero;
        moveInput = 0f;
        combat.ClearStun();
    }

    void OnDrawGizmosSelected()
    {
        if (groundCheck == null) return;
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
    }
}
