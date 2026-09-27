using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyAI : MonoBehaviour
{
    public float moveSpeed = 3f;
    public float jumpForce = 9f;
    public float stopDistance = 0.6f;
    public float jumpCooldown = 1f;

    [Header("Yön Ayarı")]
    public bool spriteFacesRightByDefault = true;

    [Header("Zıplama Ayarı")]
    public float wallCheckDistance = 0.6f;
    public float wallCheckHeight = 0.5f; // Ayak hizasindan atilan isin zeminin yan yuzunu siyirip
                                         // surekli "duvar var" diyordu, isini yukari kaldir.
    public float groundCheckDistance = 2f;
    public LayerMask groundLayer;

    [Header("Gun Mayhem Itme Saldırısı")]
    public float attackRange = 1.2f;
    public float attackCooldown = 1.2f;
    public float knockbackForce = 12f;
    public float knockbackUpForce = 4f;

    private Transform player;
    private Rigidbody2D rb;
    private bool isGrounded;
    private float lastJumpTime = -10f;
    private float lastAttackTime = -10f;

    private bool isKnockedOut = false;
    private float knockbackEndTime = 0f;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
        }
    }

   
    public void Stun(float duration)
    {
        isKnockedOut = true;
        knockbackEndTime = Time.time + duration;
    }

    void Update()
    {
        if (isKnockedOut)
        {
            if (Time.time >= knockbackEndTime)
            {
                isKnockedOut = false;
            }
            else
            {
                return;
            }
        }

        RaycastHit2D groundHit = Physics2D.Raycast(transform.position, Vector2.down, groundCheckDistance, groundLayer);
        isGrounded = groundHit.collider != null;

        if (player == null) return;

        bool playerAbove = player.position.y > transform.position.y + 0.8f;

        float moveDir = Mathf.Sign(player.position.x - transform.position.x);
        Vector2 wallRayOrigin = (Vector2)transform.position + Vector2.up * wallCheckHeight;
        RaycastHit2D wallHit = Physics2D.Raycast(wallRayOrigin, new Vector2(moveDir, 0), wallCheckDistance, groundLayer);
        bool wallAhead = wallHit.collider != null;

        if (isGrounded && (playerAbove || wallAhead) && Time.time > lastJumpTime + jumpCooldown)
        {
            Jump();
        }

        float distX = Mathf.Abs(player.position.x - transform.position.x);
        if (distX <= attackRange && Time.time > lastAttackTime + attackCooldown)
        {
            PushAttack();
        }
    }

    void FixedUpdate()
    {
        
        if (isKnockedOut) return;

        if (player != null)
        {
            float distX = player.position.x - transform.position.x;

            
            if (Mathf.Abs(distX) > stopDistance)
            {
                float direction = Mathf.Sign(distX);
                rb.linearVelocity = new Vector2(direction * moveSpeed, rb.linearVelocity.y);
            }
            else
            {
                rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            }

            FacePlayer();
        }
    }

    void FacePlayer()
    {
        float absScale = Mathf.Abs(transform.localScale.x);
        bool playerIsRight = player.position.x > transform.position.x;
        bool faceRight = spriteFacesRightByDefault ? playerIsRight : !playerIsRight;

        transform.localScale = new Vector3(
            faceRight ? absScale : -absScale,
            transform.localScale.y,
            transform.localScale.z
        );
    }

    void Jump()
    {
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        lastJumpTime = Time.time;
    }

    void PushAttack()
    {
        lastAttackTime = Time.time;

        if (player != null)
        {
            PlayerAttack playerAtk = player.GetComponent<PlayerAttack>();
            if (playerAtk != null)
            {
                float pushDir = Mathf.Sign(player.position.x - transform.position.x);
                Vector2 knockback = new Vector2(pushDir * knockbackForce, knockbackUpForce);
                playerAtk.TakeHit(knockback);
            }
        }
    }
}