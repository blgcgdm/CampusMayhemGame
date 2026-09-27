using UnityEngine;

// Ring-out oyununda arenadan düşebilmek şart. Oyuncu için dönüş yolu
// olmazsa sahne ilk düşüşte kilitleniyor.
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerRespawn : MonoBehaviour
{
    public float killHeight = -10f;

    private Vector3 spawnPoint;
    private Rigidbody2D rb;

    void Awake()
    {
        spawnPoint = transform.position;
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        if (transform.position.y >= killHeight) return;

        transform.position = spawnPoint;
        rb.linearVelocity = Vector2.zero;
    }
}
