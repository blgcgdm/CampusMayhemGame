using UnityEngine;


[RequireComponent(typeof(Rigidbody2D))]
public class FighterCombat : MonoBehaviour
{
    [Header("Saldırı")]
    public Vector2 attackOffset = new Vector2(0.7f, 0.1f);
    public float attackRadius = 0.7f;
    public float attackCooldown = 0.35f;
    public LayerMask fighterLayers;

    [Header("Savrulma")]
    public float knockbackForce = 11f;
    public float knockbackLift = 0.45f;
    public float stunDuration = 0.3f;
    public float invulnerability = 0.4f;
    public float maxKnockbackSpeed = 16f;

    public bool IsStunned { get { return Time.time < stunnedUntil; } }

    private Rigidbody2D body;
    private float stunnedUntil;
    private float invulnerableUntil;
    private float nextAttackTime;

    void Awake()
    {
        body = GetComponent<Rigidbody2D>();
    }


    public bool TryAttack(int facing)
    {
        if (Time.time < nextAttackTime) return false;
        nextAttackTime = Time.time + attackCooldown;

        Collider2D[] hits = Physics2D.OverlapCircleAll(AttackCenter(facing), attackRadius, fighterLayers);

        foreach (Collider2D hit in hits)
        {
            FighterCombat target = hit.GetComponentInParent<FighterCombat>();
            if (target == null || target == this) continue;

            target.TakeHit(facing);
        }

        return true;
    }

    public void TakeHit(int attackerFacing)
    {
        if (Time.time < invulnerableUntil) return;

        invulnerableUntil = Time.time + invulnerability;
        stunnedUntil = Time.time + stunDuration;


        Vector2 push = new Vector2(attackerFacing < 0 ? -1f : 1f, knockbackLift).normalized * knockbackForce;
        if (push.magnitude > maxKnockbackSpeed) push = push.normalized * maxKnockbackSpeed;

        body.linearVelocity = push;
    }

    public void ClearStun()
    {
        stunnedUntil = 0f;
        invulnerableUntil = 0f;
    }

    Vector2 AttackCenter(int facing)
    {
        float x = attackOffset.x * (facing < 0 ? -1f : 1f);
        return (Vector2)transform.position + new Vector2(x, attackOffset.y);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(AttackCenter(1), attackRadius);
        Gizmos.DrawWireSphere(AttackCenter(-1), attackRadius);
    }
}
