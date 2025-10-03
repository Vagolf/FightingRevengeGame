using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

public class Player : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float speed = 20f;
    [SerializeField] private float jumpForce = 18f;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private TrailRenderer tr;

    //now it's can't dash, but you can add it back if you want
    [Header("Dash Settings")]
    private bool canDash = true;
    private bool isDashing;
    private float dashingPower = 100f;
    private float dashnigTime = 0.2f;
    private float dashingCooldown = 1f;

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

    [Header("Ultimate Skill")]
    [SerializeField] private float ultimateCooldown = 8f;
    private bool canUltimate = true;
    public bool isUltimating = false;
    private float ultimateTimer = 0f;
    [SerializeField] private float ultimateDashPower = 50f; // Dash power for ultimate
    private bool waitingForGroundUltimate = false; // Flag for waiting ultimate

    [Header("Event Animation")]
    [SerializeField] private string eventAnimationTrigger = "ulti"; // Event animation trigger name
    [SerializeField] private float freezeDuration = 3f; // Freeze duration after animation
    [SerializeField] private bool autoTriggerOnStart = false; // Auto trigger on start
    private bool hasPlayedEventAnimation = false;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        boxCollider = GetComponent<BoxCollider2D>();
        anim = GetComponent<Animator>();
    }

    private void Start()
    {
        // Debug animator parameters on start for troubleshooting
        DebugAnimatorParameters();
        
        // Initialize states
        waitingForGroundUltimate = false;
        isUltimating = false;
        canUltimate = true;
        
        Debug.Log("Player initialized - Ultimate system ready");
    }

    private void Update()
    {
        // Ultimate has highest priority - block all other inputs when ultimate is active
        if (isUltimating)
        {
            // Only allow ultimate input, block everything else
            return;
        }
        
        if (isDashing)
        {
            // Disable input during dash
            return;
        }
        
        // Move left-right
        horizontalInput = Input.GetAxis("Horizontal");

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

        if (Input.GetKeyDown(KeyCode.L) && canDash)
        {
            StartCoroutine(Dash());
        }

        // Ultimate (K) - Check conditions before allowing ultimate
        if (Input.GetKeyDown(KeyCode.K) && canUltimate && !isUltimating && !waitingForGroundUltimate)
        {
            Debug.Log($"K pressed - IsGrounded: {IsGrounded()}, canUltimate: {canUltimate}");
            
            // Must be on ground to use ultimate
            if (IsGrounded())
            {
                Debug.Log("Executing ultimate immediately (on ground)");
                // Stop current movement and execute ultimate
                body.velocity = Vector2.zero;
                StartCoroutine(UltimateSkill());
            }
            else
            {
                Debug.Log("Setting flag to wait for ground");
                // Set flag to wait for ground
                waitingForGroundUltimate = true;
                canUltimate = false;
            }
        }
        
        // Check if waiting for ground and now grounded
        if (waitingForGroundUltimate && IsGrounded())
        {
            Debug.Log("Ground detected while waiting - executing ultimate!");
            waitingForGroundUltimate = false;
            body.velocity = Vector2.zero;
            
            // Double check we can still execute ultimate
            if (!isUltimating)
            {
                StartCoroutine(UltimateSkill());
            }
            else
            {
                Debug.LogWarning("Already ultimating - canceling ground ultimate");
                canUltimate = true; // Reset availability
            }
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

        // Animator update - but not during ultimate
        if (!isUltimating)
        {
            anim.SetBool("run", horizontalInput != 0 && ground && !isCrouching);
            anim.SetBool("ground", ground);
            anim.SetBool("crouch", isCrouching);
            anim.SetFloat("yVelocity", body.velocity.y);
        }

        // Ultimate cooldown timer - but not when waiting for ground
        if (!canUltimate && !waitingForGroundUltimate)
        {
            ultimateTimer += Time.deltaTime;
            if (ultimateTimer >= ultimateCooldown)
            {
                canUltimate = true;
                ultimateTimer = 0f;
                Debug.Log("Ultimate cooldown complete - can use ultimate again");
            }
        }
        
        // Reset waiting flag if ultimate becomes available again (safety reset)
        if (canUltimate && waitingForGroundUltimate)
        {
            Debug.Log("Safety reset - clearing waiting flag");
            waitingForGroundUltimate = false;
        }

        // Manual reset for debugging (R key)
        if (Input.GetKeyDown(KeyCode.R))
        {
            Debug.Log("Manual ultimate system reset!");
            waitingForGroundUltimate = false;
            canUltimate = true;
            isUltimating = false;
            ultimateTimer = 0f;
            Time.timeScale = 1f;
            if (anim != null)
            {
                anim.updateMode = AnimatorUpdateMode.Normal;
            }
        }
    }

    private void Jump()
    {
        body.velocity = new Vector2(body.velocity.x, jumpForce);
        ground = false;
        anim.SetTrigger("jump");
    }

    private bool IsGrounded()
    {
        bool wasGrounded = ground;
        ground = Physics2D.BoxCast(boxCollider.bounds.center, boxCollider.bounds.size, 0, Vector2.down, 0.1f, groundLayer);
        
        // Debug when ground state changes
        if (wasGrounded != ground)
        {
            Debug.Log($"Ground state changed: {wasGrounded} -> {ground}");
            if (waitingForGroundUltimate && ground)
            {
                Debug.Log("Ground detected while waiting for ultimate!");
            }
        }
        
        return ground;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.tag == "Ground")
        {
            ground = true;
            
            Debug.Log("Ground collision detected");

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

    //Dash 
    private IEnumerator Dash()
    {
        canDash = false;
        isDashing = true;
        float originalGravity = body.gravityScale;
        body.gravityScale = 0f; // Disable gravity during dash
        body.velocity = new Vector2(transform.localScale.x * dashingPower, 0f); // Dash in facing direction
        tr.emitting = true; // Start trail effect
        yield return new WaitForSeconds(dashnigTime);
        tr.emitting = false; // Stop trail effect
        body.gravityScale = originalGravity; // Restore original gravity
        isDashing = false;
        yield return new WaitForSeconds(dashingCooldown);
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

    // Ultimate Skill coroutine
    private IEnumerator UltimateSkill()
    {
        // Set ultimating state (canUltimate is already false from input check)
        isUltimating = true;
        
        // Stop player movement immediately
        body.velocity = Vector2.zero;
        
        // Stop time but keep ultimate animation running
        Time.timeScale = 0f;
        
        // Set Animator to use Unscaled Time for ultimate animation
        AnimatorUpdateMode originalUpdateMode = anim.updateMode;
        anim.updateMode = AnimatorUpdateMode.UnscaledTime;
        
        // Check if trigger exists before using it
        if (HasAnimatorParameter(eventAnimationTrigger))
        {
            anim.SetTrigger(eventAnimationTrigger);
        }
        else if (HasAnimatorParameter("ulti"))
        {
            anim.SetTrigger("ulti");
        }
        else if (HasAnimatorParameter("ultimate"))
        {
            anim.SetTrigger("ultimate");
        }
        else
        {
            Debug.LogError("No valid ultimate trigger found!");
            // Reset states if no trigger found
            isUltimating = false;
            canUltimate = true; // Reset ultimate availability
            Time.timeScale = 1f;
            anim.updateMode = originalUpdateMode;
            yield break;
        }
        
        // Wait for animation event to call EndUltimate() - blocks all other inputs
        while (isUltimating)
        {
            yield return null;
        }
        
        // This part will only execute AFTER EndUltimate() is called from animation event
        // Restore time and animator settings
        Time.timeScale = 1f;
        anim.updateMode = originalUpdateMode;
        
        // Dash player forward after ultimate ends
        StartCoroutine(UltimateDash());
    }

    // Helper method to reset animator to idle state
    private void ResetAnimatorToIdle()
    {
        // Reset all boolean parameters to false
        anim.SetBool("run", false);
        anim.SetBool("atk", false);
        anim.SetBool("crouch", false);
        anim.SetBool("ground", true);
        
        // Reset float parameters
        anim.SetFloat("yVelocity", 0f);
        
        // If there's an idle trigger, use it
        if (HasAnimatorParameter("idle"))
        {
            anim.SetTrigger("idle");
        }
    }

    // Helper method to check if animator parameter exists
    private bool HasAnimatorParameter(string paramName)
    {
        if (anim == null || anim.runtimeAnimatorController == null)
            return false;
            
        foreach (AnimatorControllerParameter param in anim.parameters)
        {
            if (param.name == paramName)
                return true;
        }
        return false;
    }

    // Ultimate dash after animation ends
    private IEnumerator UltimateDash()
    {
        float originalGravity = body.gravityScale;
        body.gravityScale = 0f; // Disable gravity during dash
        
        // Dash in facing direction
        float dashDirection = transform.localScale.x;
        Vector2 dashVelocity = new Vector2(dashDirection * ultimateDashPower, 0f);
        body.velocity = dashVelocity;
        
        // Enable trail if available
        if (tr != null)
            tr.emitting = true;
        
        yield return new WaitForSeconds(0.3f); // Dash duration
        
        // Stop dash
        body.gravityScale = originalGravity;
        if (tr != null)
            tr.emitting = false;
            
        // Restore normal movement
        body.velocity = new Vector2(0f, body.velocity.y);
    }

    // Call this from animation event at the end of ultimate animation
    public void EndUltimate()
    {
        Debug.Log("EndUltimate called from Animation Event");
        
        if (isUltimating)
        {
            isUltimating = false;
            
            // Force return to idle immediately
            ForceReturnToIdle();
            
            // Note: canUltimate will be reset by cooldown timer
        }
    }

    // Force animator to return to idle state immediately
    private void ForceReturnToIdle()
    {
        // Reset all animator parameters to idle state
        anim.SetBool("run", false);
        anim.SetBool("atk", false);
        anim.SetBool("crouch", false);
        anim.SetBool("ground", IsGrounded());
        anim.SetFloat("yVelocity", body.velocity.y);
        
        // Force play idle animation directly
        if (HasAnimatorParameter("idle"))
        {
            anim.SetTrigger("idle");
        }
        
        // Alternative: Force play idle animation by name
        if (anim.runtimeAnimatorController != null)
        {
            // Try to find and play idle animation directly
            anim.Play("idle-K", 0, 0f); // Layer 0, normalized time 0
        }
    }

    // Animation event method for ultimate effect (call this at the key frame)
    public void UltimateEffect()
    {
        // Deal damage to all enemies in range
        if (AttackPoint != null)
        {
            Collider2D[] enemies = Physics2D.OverlapCircleAll(AttackPoint.transform.position, radius * 2f, Enemy);
            foreach (Collider2D enemy in enemies)
            {
                var healthComponent = enemy.GetComponent<HealthEnemy>();
                if (healthComponent != null)
                {
                    healthComponent.TakeDamage(500); // Ultimate damage
                }
            }
        }
    }

    // Helper method to debug all animator parameters
    public void DebugAnimatorParameters()
    {
        Debug.Log("=== ANIMATOR PARAMETERS DEBUG ===");
        if (anim == null || anim.runtimeAnimatorController == null)
        {
            Debug.LogWarning("Animator or RuntimeAnimatorController is null");
            return;
        }
        
        foreach (AnimatorControllerParameter param in anim.parameters)
        {
            string value = "";
            switch (param.type)
            {
                case AnimatorControllerParameterType.Float:
                    value = anim.GetFloat(param.name).ToString("F2");
                    break;
                case AnimatorControllerParameterType.Int:
                    value = anim.GetInteger(param.name).ToString();
                    break;
                case AnimatorControllerParameterType.Bool:
                    value = anim.GetBool(param.name).ToString();
                    break;
                case AnimatorControllerParameterType.Trigger:
                    value = "Trigger";
                    break;
            }
            Debug.Log($"Parameter: {param.name} ({param.type}) = {value}");
        }
        Debug.Log("=== ANIMATOR PARAMETERS DEBUG END ===");
    }

    // Event Animation methods - separate from ultimate
    public void TriggerEventAnimation()
    {
        if (!hasPlayedEventAnimation && !string.IsNullOrEmpty(eventAnimationTrigger))
        {
            StartCoroutine(PlayEventAnimation());
        }
    }

    private IEnumerator PlayEventAnimation()
    {
        hasPlayedEventAnimation = true;

        // Set Animator to use Unscaled Time for event animation
        AnimatorUpdateMode originalUpdateMode = anim.updateMode;
        float originalAnimSpeed = anim.speed;
        
        anim.updateMode = AnimatorUpdateMode.UnscaledTime;
        
        // Use the same trigger as ultimate
        if (HasAnimatorParameter(eventAnimationTrigger))
        {
            anim.SetTrigger(eventAnimationTrigger);
        }
        else
        {
            yield break;
        }

        // Wait for animation to start and complete
        yield return new WaitForEndOfFrame();
        
        AnimatorStateInfo state = anim.GetCurrentAnimatorStateInfo(0);
        while (state.normalizedTime < 1.0f)
        {
            yield return null;
            state = anim.GetCurrentAnimatorStateInfo(0);
        }

        // Freeze animation pose for specified duration
        anim.speed = 0f;
        yield return new WaitForSecondsRealtime(freezeDuration);

        // Restore original animation settings
        anim.speed = originalAnimSpeed;
        anim.updateMode = originalUpdateMode;
    }

    // Public method to reset event animation state
    public void ResetEventAnimation()
    {
        hasPlayedEventAnimation = false;
    }

    // Wait for player to land on ground then execute ultimate
    private IEnumerator WaitForGroundThenUltimate()
    {
        // This method is deprecated - now using flag system in Update()
        // Keeping for reference but should not be called
        Debug.LogWarning("WaitForGroundThenUltimate called - this method is deprecated!");
        yield return null;
    }
}
