using UnityEngine;
using System.Collections;

// Oyuncunun sadece dövüş tarafı. Hareket, zıplama ve zemin kontrolü
// HeroKnight.cs'in işi; ikisi birden linearVelocity yazarsa çakışırlar.
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerAttack : MonoBehaviour
{
    [Header("Savrulma (Knockback) Durumu")]
    public bool isPlayerKnockedOut = false;
    public float knockoutDuration = 0.3f;

    [Header("Saldırı Ayarları")]
    public Vector2 attackOffset = new Vector2(0.6f, 0f);
    public float attackRange = 0.8f;
    public LayerMask enemyLayers;
    public float knockbackForce = 8f;

    private Rigidbody2D rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    // HeroKnight saldırı animasyonunu tetiklediği anda çağırır.
    // Girdi tek yerde okunsun diye burada Input'a bakılmıyor.
    public void Attack(int facingDirection)
    {
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(AttackCenter(facingDirection), attackRange, enemyLayers);

        foreach (Collider2D hit in hitEnemies)
        {
            Enemy enemy = hit.GetComponent<Enemy>();
            if (enemy == null) continue;

            float pushX = hit.transform.position.x - transform.position.x;
            Vector2 knockbackDir = new Vector2(pushX, 0.5f).normalized;

            enemy.TakeHit(knockbackDir, knockbackForce);
        }
    }

    public void TakeHit(Vector2 forceVector)
    {
        // Üst üste binen coroutine'ler isPlayerKnockedOut üzerinde yarışır
        // ve ilk biten bayrağı erken kapatır.
        if (isPlayerKnockedOut) return;

        StartCoroutine(PlayerKnockbackRoutine(forceVector));
    }

    private IEnumerator PlayerKnockbackRoutine(Vector2 force)
    {
        isPlayerKnockedOut = true;
        rb.linearVelocity = force;

        yield return new WaitForSeconds(knockoutDuration);

        isPlayerKnockedOut = false;
    }

    private Vector2 AttackCenter(int facingDirection)
    {
        float x = attackOffset.x * (facingDirection < 0 ? -1f : 1f);
        return (Vector2)transform.position + new Vector2(x, attackOffset.y);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(AttackCenter(1), attackRange);
        Gizmos.DrawWireSphere(AttackCenter(-1), attackRange);
    }
}
