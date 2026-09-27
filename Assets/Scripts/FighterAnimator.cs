using UnityEngine;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(Fighter))]
public class FighterAnimator : MonoBehaviour
{
    [Header("Parametre adları")]
    public string groundedBool = "Grounded";
    public string airSpeedFloat = "AirSpeedY";
    public string stateInt = "AnimState";
    public string jumpTrigger = "Jump";
    public string attackTrigger = "Attack1";

    [Header("AnimState değerleri")]
    public int idleState = 0;
    public int runState = 1;

    [Header("Görsel")]
    public bool spriteFacesRight = true;

    private Animator animator;
    private Fighter fighter;
    private Rigidbody2D body;
    private SpriteRenderer sprite;

    void Awake()
    {
        animator = GetComponent<Animator>();
        fighter = GetComponent<Fighter>();
        body = GetComponent<Rigidbody2D>();
        sprite = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        animator.SetBool(groundedBool, fighter.IsGrounded);
        animator.SetFloat(airSpeedFloat, body.linearVelocity.y);
        animator.SetInteger(stateInt, Mathf.Abs(body.linearVelocity.x) > 0.1f ? runState : idleState);

        if (sprite != null)
        {
            sprite.flipX = spriteFacesRight ? fighter.Facing < 0 : fighter.Facing > 0;
        }
    }

    public void PlayJump()
    {
        animator.SetTrigger(jumpTrigger);
    }

    public void PlayAttack()
    {
        animator.SetTrigger(attackTrigger);
    }
}
