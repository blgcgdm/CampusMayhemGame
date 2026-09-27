using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(EnemyAI))]
public class Enemy : MonoBehaviour
{
    public int scoreValue = 10;

    [Header("Savrulma (Knockback) Durumu")]
    public float knockoutDuration = 0.3f; // Player'daki savrulma süresiyle aynı mantık
    public float maxKnockbackSpeed = 14f; // Aşırı yüksek fırlamayı engellemek için üst sınır

    [Header("Vuruş Dokunulmazlığı")]
    public float hitInvulnerabilityDuration = 0.4f; // Savrulma sırasında tekrar vurulamasın

    [Header("Ring-out")]
    public float deathHeight = -10f;

    private Rigidbody2D rb;
    private EnemyAI enemyAI;
    private float invulnerableUntil = 0f;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        enemyAI = GetComponent<EnemyAI>();
    }

    void Update()
    {
        if (transform.position.y < deathHeight)
        {
            Die();
        }
    }

    // Player kılıçla vurunca bu fonksiyon çalışır
    public void TakeHit(Vector2 knockbackDirection, float force)
    {
        // KRİTİK: Enemy zaten savruluyorken (havadayken) tekrar vurulursa
        // hız her seferinde sıfırlanıp yeniden fırlatılıyordu, bu da
        // üst üste binerek enemy'nin ekranın dışına fırlamasına sebep oluyordu.
        // Dokunulmazlık süresi dolmadan yeni vuruşu tamamen görmezden gel.
        if (Time.time < invulnerableUntil) return;

        invulnerableUntil = Time.time + hitInvulnerabilityDuration;

        Vector2 knockbackVelocity = knockbackDirection * force;

        if (knockbackVelocity.magnitude > maxKnockbackSpeed)
        {
            knockbackVelocity = knockbackVelocity.normalized * maxKnockbackSpeed;
        }

        rb.linearVelocity = knockbackVelocity;
        enemyAI.Stun(knockoutDuration);
    }

    void Die()
    {
        if (ScoreManager.instance != null)
        {
            ScoreManager.instance.AddScore(scoreValue);
        }
        Destroy(gameObject);
    }
}
