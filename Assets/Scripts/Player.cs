using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float speed = 20f;
    [SerializeField] private float jumpForce = 18f;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private TrailRenderer tr;

    //now it's can't dash, but you can add it back if you want
    [Header("Dash Settings")]
    /**
    [SerializeField] private float dashPower = 25f;
    [SerializeField] private int dashCounter = 0;
    [SerializeField] private float dashTime = 0.2f;
    [SerializeField] private float dashCooldown = 2f;
    */

    [SerializeField] private float dashPower = 5f;
    [SerializeField] private float dashTime = 0.1f;
    [SerializeField] private float dashCooldown = 2f;
    private bool canDash = true;
    private bool isDashing = false;

    [Header("Sound")]
    [SerializeField] private AudioSource jumpSoundEffect;
    [SerializeField] private AudioSource dashSoundEffect;

    private Rigidbody2D body;
    public BoxCollider2D boxCollider;
    public Animator anim;

    //private bool canDash = true;
    //private bool isDashing = false;
    public bool isCrouching = false;
    private float horizontalInput;
    private bool ground;

    [Header("Attack")]
    //basic attack
    public GameObject AttackPoint;
    public float radius;

    //crouch attack
    public GameObject CrouchAttackPoint; // Uncomment if you have a separate point for crouch attack
    public float crouchRadius; // Uncomment if you have a separate radius for crouch attack

    public LayerMask Enemy;
    public float damage;

    private bool attack;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        boxCollider = GetComponent<BoxCollider2D>();
        anim = GetComponent<Animator>();
    }

    private void Update()
    {
        // Move left-right
        horizontalInput = Input.GetAxis("Horizontal");

        // Removed early return: still update animator while dashing

        // Crouch (S)
        if (Input.GetKey(KeyCode.S) && IsGrounded())
        {
            isCrouching = true;
        }
        else
        {
            isCrouching = false;
        }

        // Attack (J)
        if (Input.GetKeyDown(KeyCode.J) && ground && !isCrouching && !attack && !isDashing)
        {
            // Normal attack
            body.velocity = new Vector2(0, body.velocity.y);
            anim.SetBool("run", false);
            anim.SetBool("atk", true);
            attack = true;
            speed = 0;
        }
        else
        {
            // Prevent running if crouching
            if (isCrouching)
            {
                // Crouch attack
                if (Input.GetKeyDown(KeyCode.J) && ground && !attack && !isDashing)
                {
                    body.velocity = new Vector2(0, body.velocity.y);
                    anim.SetBool("run", false);
                    anim.SetBool("atk", true); // Use a different trigger for crouch attack
                    attack = true;
                    speed = 0;
                }
                else
                {
                    if (!isDashing)
                        body.velocity = new Vector2(0, body.velocity.y);
                }
            }
            else
            {
                // Only apply input-based horizontal movement when not dashing
                if (!isDashing)
                {
                    speed = 20f; // Reset speed when not crouching or attacking
                    body.velocity = new Vector2(horizontalInput * speed, body.velocity.y);
                }
            }
        }

        if (Input.GetKeyDown(KeyCode.L) && !ground && canDash && !isDashing) //!ground && dashCounter < 1 && Input.GetKeyDown(KeyCode.L) && horizontalInput != 0
        {
            StartCoroutine(Dash());
        }

        // Flip player (A, D)
        if (horizontalInput > 0.01f)
            transform.localScale = new Vector3(1, 1, 1);
        else if (horizontalInput < -0.01f)
            transform.localScale = new Vector3(-1, 1, 1);

        // Jump (W)
        if (Input.GetKeyDown(KeyCode.W) && IsGrounded() && !isCrouching)
        {
            Jump();
        }

        // Animator update
        anim.SetBool("run", horizontalInput != 0 && ground && !isCrouching);
        anim.SetBool("ground", ground);
        anim.SetBool("crouch", isCrouching);
        anim.SetFloat("yVelocity", body.velocity.y);
    }

    private void Jump()
    {
        body.velocity = new Vector2(body.velocity.x, jumpForce);
        ground = false;
        anim.SetTrigger("jump");
        jumpSoundEffect?.Play();
    }

    private bool IsGrounded()
    {
        ground = Physics2D.BoxCast(boxCollider.bounds.center, boxCollider.bounds.size, 0, Vector2.down, 0.1f, groundLayer);
        return ground;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.tag == "Ground")
        {
            //dashCounter = 0; // Reset dash counter when touching the ground
            ground = true;

            // If landing while dashing, stop dash immediately to prevent sliding
            if (isDashing)
            {
                tr.emitting = false;
                isDashing = false;
                // If no input, stop horizontal movement
                if (Mathf.Abs(horizontalInput) < 0.01f)
                    body.velocity = new Vector2(0f, body.velocity.y);
            }
        }

    }

    // Dash functionality (optional, can be removed if not needed) 21-6-68
    /**
    private void Dash()
    {
        dashCounter++;
        body.constraints |= RigidbodyConstraints2D.FreezePositionY; // Freeze all movement
        Vector2 dashDirection = new Vector2(horizontalInput, 0).normalized; // Dash direction based on facing
        body.velocity = dashDirection * dashPower;
        tr.emitting = true; // Start trail effect
        Invoke("enbleCharacterMovement", dashTime); // Unfreeze after dash time     
    }
    private void enbleCharacterMovement()
    {
        body.constraints &= ~RigidbodyConstraints2D.FreezePositionY; // Unfreeze vertical movement
        tr.emitting = false; // Stop trail effect
    }
    **/

    //Dash with early stop on landing
    private IEnumerator Dash()
    {
        canDash = false;
        isDashing = true;

        float dir = Mathf.Sign(transform.localScale.x);
        tr.emitting = true;
        dashSoundEffect?.Play();

        float elapsed = 0f;
        while (elapsed < dashTime)
        {
            // Apply dash horizontal velocity
            body.velocity = new Vector2(dir * dashPower, body.velocity.y);

            // Stop dash early if we touch ground to avoid sliding
            if (ground)
            {
                break;
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        tr.emitting = false;
        isDashing = false;

        // If grounded or no input, stop horizontal movement
        if (ground || Mathf.Abs(horizontalInput) < 0.01f)
        {
            body.velocity = new Vector2(0f, body.velocity.y);
        }

        yield return new WaitForSeconds(dashCooldown);
        canDash = true;
    }

    // Functions for Animation Events
    public void OnAttackStart()
    {
        anim.SetBool("atk", true);
        attack = true;
    }

    public void OnAttackEnd()
    {
        StopAttackAnimation();
    }

    public void StopAttackAnimation()
    {
        anim.SetBool("atk", false);
        attack = false;
    }

    public void Attack()
    {
        Collider2D[] enemy = Physics2D.OverlapCircleAll(AttackPoint.transform.position, radius, Enemy);
        foreach (Collider2D enemyGameobject in enemy)
        {
            Debug.Log("Hit Enemy");
            enemyGameobject.GetComponent<HealthEnemy>().TakeDamage(100); // Assuming Enemy script has TakeDamage method
        }
    }
    // Add this method for crouch attack
    public void CrouchAttack()
    {
        Collider2D[] enemy = Physics2D.OverlapCircleAll(CrouchAttackPoint.transform.position, crouchRadius, Enemy);
        foreach (Collider2D enemyGameobject in enemy)
        {
            Debug.Log("Crouch Hit Enemy");
            enemyGameobject.GetComponent<HealthEnemy>().TakeDamage(300); // Example: more damage for crouch attack
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (AttackPoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(AttackPoint.transform.position, radius);
        }
        if (CrouchAttackPoint != null) { // Uncomment if you have a separate point for crouch attack
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(CrouchAttackPoint.transform.position, crouchRadius); // Uncomment if you have a separate radius for crouch attack
        }
    }

    private void stopTakingDamage()
    {
        anim.SetBool("hurt", false);
        body.velocity = new Vector2(body.velocity.x, 0); // Reset vertical velocity
    }
}
