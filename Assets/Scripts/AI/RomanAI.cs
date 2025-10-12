using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System; // for Action

// AI controlled version of Roman. It chases a locked target and performs
// the same animations / skills as the player version when available.
[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(BoxCollider2D))]
[RequireComponent(typeof(Animator))]
public class RomanAI : MonoBehaviour
{
    [Header("Target Lock")]
    [Tooltip("Target transform to chase. If null, will auto-find by tag")] 
    [SerializeField] private Transform target;
    [SerializeField] private string targetTag = "Player";

    [Header("Movement Settings")]
    [SerializeField] private float speed = 20f;
    [SerializeField] private float jumpForce = 18f;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private TrailRenderer tr;

    [Header("Ranges")]
    [SerializeField] private float detectRange = 12f;
    [SerializeField] private float attackRange = 1.9f;
    [SerializeField] private float stopDistance = 1.0f;

    [Header("Dash Settings")]
    [SerializeField] private float dashingPower = 100f;
    [SerializeField] private float dashnigTime = 0.2f;
    [SerializeField] private float dashingCooldown = 1f;
    private bool canDash = true;
    private bool isDashing;

    private Rigidbody2D body;
    private BoxCollider2D boxCollider;
    private Animator anim;

    private bool ground;

    [Header("Attack")]
    public GameObject AttackPoint;
    public float radius = 1f;
    public GameObject CrouchAttackPoint;
    public float crouchRadius = 1f;
    [Tooltip("Layers to damage when AI attacks (should include Player)")] 
    public LayerMask Opponent;
    [Header("Damage Values")] public float damage = 100f; // legacy default
    public float normalAttackDamage = 70f;
    public float crouchAttackDamage = 180f;
    [SerializeField] private float attackCooldown = 0.8f;
    private float attackTimer = 0f;
    private bool attack;

    [Header("Ultimate Skill")]
    [SerializeField] private float ultimateCooldown = 8f;
    [SerializeField] private float ultimateDamage = 500f;
    [SerializeField] private float ultimateHitRadiusMultiplier = 2f;
    private float ultimateTimer = 0f;
    private bool ultimateReady = false; // start on cooldown like Roman
    private bool inUltimate = false;
    private bool preparingUltimate = false;
    [SerializeField] private bool logUltimateCooldown = true;
    private int ultimateLastLoggedSecond = -1;

    [Header("Ultimate Animation")]
    [SerializeField] private string ultimateTrigger = "ulti";

    [Header("Ultimate Warp (disabled)")]
    [SerializeField] private bool enableUltimateHitWarp = false;

    private bool ultimateDamageFired = false;

    [Header("Ultimate Damage Point")]
    public UltimateDamagePoint ultimateDamagePoint;

    [Header("Ultimate Box Area")]
    [SerializeField] private bool ultimateUseBox = false;
    [SerializeField] private Vector2 ultimateBoxSize = new Vector2(4f, 2f);
    [SerializeField] private float ultimateBoxAngle = 0f;
    [SerializeField] private Transform UltimateAttackPoint;

    [Header("Air Jump")]
    [SerializeField] private int extraJumpsMax = 1; // 1 = double jump
    private int extraJumps;

    [Header("Debug")] 
    [SerializeField] private bool verboseLog = false;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        boxCollider = GetComponent<BoxCollider2D>();
        anim = GetComponent<Animator>();
        if (tr == null) tr = GetComponent<TrailRenderer>();
    }

    private void Start()
    {
        // Start with ultimate on cooldown to mirror Roman
        ultimateReady = false;
        ultimateTimer = 0f;
        ultimateLastLoggedSecond = -1;
        inUltimate = false;
        preparingUltimate = false;
        extraJumps = extraJumpsMax;
        if (tr)
        {
            tr.emitting = false;
            tr.enabled = false;
            tr.Clear();
        }
        if (target == null)
        {
            var t = GameObject.FindGameObjectWithTag(targetTag);
            if (t != null) target = t.transform;
        }
    }

    private void Update()
    {
        // cooldowns
        TickUltimateCooldown(Time.deltaTime);
        if (attackTimer > 0f) attackTimer -= Time.deltaTime;

        if (Timer.GateBlocked)
        {
            SetRun(false);
            return;
        }

        if (target == null)
        {
            var t = GameObject.FindGameObjectWithTag(targetTag);
            if (t != null) target = t.transform; else { SetRun(false); return; }
        }

        IsGrounded();

        if (preparingUltimate) { SetRun(false); return; }
        if (inUltimate) { return; }
        if (isDashing) return;
        if (movementLocked)
        {
            // freeze horizontal motion but keep gravity
            body.velocity = new Vector2(0f, body.velocity.y);
            SetRun(false);
            return;
        }

        // Auto use Ultimate when ready and close enough
        if (ultimateReady && ground)
        {
            float distToTarget = Mathf.Abs(target.position.x - transform.position.x);
            if (distToTarget <= detectRange)
            {
                StartCoroutine(PrepareAndStartUltimate());
                return;
            }
        }

        // Movement towards target
        Vector3 attackPos = AttackPoint != null ? AttackPoint.transform.position : transform.position;
        float dist = Vector2.Distance(attackPos, target.position);
        float dir = Mathf.Sign(target.position.x - transform.position.x);

        // flip
        if (dir > 0.01f) transform.localScale = new Vector3(1, 1, 1);
        else if (dir < -0.01f) transform.localScale = new Vector3(-1, 1, 1);

        if (dist > attackRange)
        {
            body.velocity = new Vector2(dir * speed, body.velocity.y);
            SetRun(true);
            // optionally dash to close gap
            if (canDash && dist > attackRange * 2.5f)
                StartCoroutine(Dash());
        }
        else
        {
            // in range -> stop and attack
            body.velocity = new Vector2(0f, body.velocity.y);
            SetRun(false);
            TryAttack();
        }

        // animator params
        anim.SetBool("ground", ground);
        anim.SetFloat("yVelocity", body.velocity.y);
    }

    private void TryAttack()
    {
        if (attackTimer > 0f) return;
        // trigger animation like player
        if (anim)
        {
            anim.SetBool("atk", true);
        }
        // Attack damage is usually applied via Animation Event calling Attack().
        // As a fallback, also call immediately if no event is set.
        Attack();
        attackTimer = attackCooldown;
        if (isActiveAndEnabled) Invoke(nameof(ClearAttackFlag), 0.1f);
    }

    private void ClearAttackFlag()
    {
        if (anim != null) anim.SetBool("atk", false);
    }

    private void SetRun(bool r)
    {
        if (anim != null) anim.SetBool("run", r);
    }

    private void Jump()
    {
        ground = false;
        SetRun(false);
        anim.SetBool("crouch", false);
        anim.SetBool("ground", false);
        anim.ResetTrigger("atk");
        body.velocity = new Vector2(body.velocity.x, jumpForce);
        anim.SetTrigger("jump");
    }

    private bool IsGrounded()
    {
        if (boxCollider == null) return ground;
        ground = Physics2D.BoxCast(boxCollider.bounds.center, boxCollider.bounds.size, 0, Vector2.down, 0.1f, groundLayer);
        return ground;
    }

    private IEnumerator Dash()
    {
        canDash = false;
        isDashing = true;
        float originalGravity = body.gravityScale;
        body.gravityScale = 0f;
        body.velocity = new Vector2(transform.localScale.x * dashingPower, 0f);
        if (tr) tr.emitting = true;
        yield return new WaitForSeconds(dashnigTime);
        if (tr) tr.emitting = false;
        body.gravityScale = originalGravity;
        isDashing = false;
        yield return new WaitForSeconds(dashingCooldown);
        canDash = true;
    }

    // Called by animation event or fallback in TryAttack
    public void Attack()
    {
        if (AttackPoint == null) return;
        float usedDamage = normalAttackDamage;
        Collider2D[] hits = Physics2D.OverlapCircleAll(AttackPoint.transform.position, radius, Opponent);
        foreach (var h in hits)
        {
            var hpEnemy = h.GetComponent<HealthEnemy>();
            if (hpEnemy != null) { hpEnemy.TakeDamage(usedDamage); continue; }
            var hpPlayer = h.GetComponent<HealthCh>();
            if (hpPlayer != null) { hpPlayer.TakeDamage(usedDamage); }
        }
    }

    public void CrouchAttack()
    {
        if (CrouchAttackPoint == null) return;
        Collider2D[] hits = Physics2D.OverlapCircleAll(CrouchAttackPoint.transform.position, crouchRadius, Opponent);
        foreach (var h in hits)
        {
            var hpEnemy = h.GetComponent<HealthEnemy>();
            if (hpEnemy != null) { hpEnemy.TakeDamage(crouchAttackDamage); continue; }
            var hpPlayer = h.GetComponent<HealthCh>();
            if (hpPlayer != null) { hpPlayer.TakeDamage(crouchAttackDamage); }
        }
    }

    // ===== Animation Event Receivers (match Roman) =====
    public void OnAttackStart()
    {
        if (anim != null) anim.SetBool("atk", true);
        attack = true;
    }

    public void OnAttackEnd()
    {
        StopAttackAnimation();
    }

    public void StopAttackAnimation()
    {
        if (anim != null) anim.SetBool("atk", false);
        attack = false;
    }

    // Movement lock during normal attack (optional but keeps parity with Roman)
    private bool movementLocked = false;

    public void NormalAttackLockMovement()
    {
        movementLocked = true;
        if (body != null) body.velocity = new Vector2(0f, body.velocity.y);
    }

    public void NormalAttackUnlockMovement()
    {
        movementLocked = false;
    }

    // Gizmo drawing removed per request

    private IEnumerator PrepareAndStartUltimate()
    {
        preparingUltimate = true;
        body.velocity = new Vector2(0f, body.velocity.y);
        SetRun(false);
        yield return null; // wait 1 frame to settle animator
        StartUltimate();
    }

    private void StartUltimate()
    {
        inUltimate = true;
        preparingUltimate = false;
        ultimateReady = false;
        ultimateTimer = 0f;
        ultimateLastLoggedSecond = -1;
        ultimateDamageFired = false;

        body.velocity = Vector2.zero;
        anim.updateMode = AnimatorUpdateMode.UnscaledTime;
        if (!string.IsNullOrEmpty(ultimateTrigger))
            anim.SetTrigger(ultimateTrigger);
        Time.timeScale = 0f;

        if (ultimateDamagePoint != null)
            ultimateDamagePoint.gameObject.SetActive(true);

        UltimateEventBus.RaiseStart(transform);
    }

    public void UltimateDamageEvent()
    {
        if (AttackPoint == null && UltimateAttackPoint == null) return;
        if (!ultimateDamageFired)
        {
            ultimateDamageFired = true;
            UltimateEventBus.RaiseDamage(transform);
        }
        Vector2 center = UltimateAttackPoint != null ? (Vector2)UltimateAttackPoint.position : (Vector2)AttackPoint.transform.position;

        int hitCount = 0;
        if (ultimateUseBox)
        {
            Collider2D[] hits = Physics2D.OverlapBoxAll(center, ultimateBoxSize, ultimateBoxAngle, Opponent);
            foreach (var h in hits)
            {
                var he = h.GetComponent<HealthEnemy>();
                var hp = h.GetComponent<HealthCh>();
                if (he != null) { he.TakeDamage(ultimateDamage); hitCount++; }
                else if (hp != null) { hp.TakeDamage(ultimateDamage); hitCount++; }
            }
        }
        else
        {
            float hitRadius = radius * ultimateHitRadiusMultiplier;
            Collider2D[] hits = Physics2D.OverlapCircleAll(center, hitRadius, Opponent);
            foreach (var h in hits)
            {
                var he = h.GetComponent<HealthEnemy>();
                var hp = h.GetComponent<HealthCh>();
                if (he != null) { he.TakeDamage(ultimateDamage); hitCount++; }
                else if (hp != null) { hp.TakeDamage(ultimateDamage); hitCount++; }
            }
        }
        if (verboseLog) Debug.Log($"[AI ULTI] Damage hits: {hitCount}");
    }

    public void UltimateFinishEvent()
    {
        Time.timeScale = 1f;
        anim.updateMode = AnimatorUpdateMode.Normal;
        inUltimate = false;

        if (ultimateDamagePoint != null && ultimateDamagePoint.gameObject.activeSelf)
            ultimateDamagePoint.gameObject.SetActive(false);

        anim.ResetTrigger(ultimateTrigger);
        SetRun(false);
        anim.SetBool("atk", false);
        anim.SetBool("crouch", false);
        anim.SetFloat("yVelocity", 0f);

        UltimateEventBus.RaiseFinish(transform);
    }

    private void TickUltimateCooldown(float dt)
    {
        if (ultimateReady) return;
        ultimateTimer += Mathf.Max(0f, dt);
        float remain = Mathf.Max(0f, ultimateCooldown - ultimateTimer);
        if (logUltimateCooldown)
        {
            int sec = Mathf.CeilToInt(remain);
            if (sec != ultimateLastLoggedSecond)
            {
                Debug.Log($"[AI ULTI] Cooldown remaining: {remain:F2}s");
                ultimateLastLoggedSecond = sec;
            }
        }
        if (ultimateTimer >= ultimateCooldown)
        {
            ultimateTimer = 0f;
            ultimateReady = true;
            ultimateLastLoggedSecond = -1;
            if (logUltimateCooldown)
                Debug.Log("[AI ULTI] Ready!");
        }
    }
}
