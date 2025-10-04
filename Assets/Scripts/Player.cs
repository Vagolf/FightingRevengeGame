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

    [Header("Dash Settings")]
    [SerializeField] private float dashingPower = 100f;
    [SerializeField] private float dashnigTime = 0.2f;
    [SerializeField] private float dashingCooldown = 1f;
    private bool canDash = true;
    private bool isDashing;

    [Header("Sound")] 
    [SerializeField] private AudioSource jumpSoundEffect;
    [SerializeField] private AudioSource dashSoundEffect;

    private Rigidbody2D body;
    public BoxCollider2D boxCollider;
    public Animator anim;

    public bool isCrouching = false;
    private float horizontalInput;
    private bool ground;

    [Header("Attack")]
    public GameObject AttackPoint;
    public float radius = 1f;
    public GameObject CrouchAttackPoint;
    public float crouchRadius = 1f;
    public LayerMask Enemy;
    public float damage = 100f;
    private bool attack;

    [Header("Ultimate Skill")]
    [SerializeField] private float ultimateCooldown = 8f;
    [SerializeField] private float ultimateDamage = 500f;
    [SerializeField] private float ultimateHitRadiusMultiplier = 2f;
    private float ultimateTimer = 0f;
    private bool ultimateReady = true;
    private bool inUltimate = false;
    private bool preparingUltimate = false; // สถานะใหม่สำหรับเฟสเตรียมก่อนเริ่ม Ultimate
    [SerializeField] private bool logUltimateCooldown = true;
    private int ultimateLastLoggedSecond = -1;

    [Header("Ultimate Animation")]
    [SerializeField] private string ultimateTrigger = "ulti";

    [Header("Ultimate Warp On Hit")] // วาร์ปตอนเฟรมดาเมจ
    [SerializeField] private bool enableUltimateHitWarp = true;
    [SerializeField] private float ultimateHitWarpDistance = 5f;
    [SerializeField] private bool ultimateHitWarpUseTrail = true;
    private bool ultimateHitWarpDone = false;

    [Header("Ultimate Warp Collision")]
    [Tooltip("Layers ที่กันไม่ให้วาร์ปทะลุ")] [SerializeField] private LayerMask ultimateWarpBlockLayers;
    [Tooltip("ลดระยะก่อนชนกำแพง (offset)")] [SerializeField] private float ultimateWarpSkin = 0.1f;
    [Tooltip("ใช้ขอบ hitbox ของ player (ครึ่งกว้าง) ป้องกันฝังผนัง")] [SerializeField] private bool ultimateWarpUseColliderBounds = true;

    [Header("Ultimate Warp Effects")]
    [SerializeField] private GameObject ultimateWarpStartEffect;
    [SerializeField] private GameObject ultimateWarpEndEffect;
    [SerializeField] private bool spawnEffectsInUnscaledTime = true;
    [SerializeField] private Vector2 effectOffset = Vector2.zero;

    [Header("Debug")]
    [SerializeField] private bool verboseUltimateLog = true; // toggle log รายละเอียด
    private bool lastRunAnimValue = false; // ตรวจจับการเปลี่ยนค่า run

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        boxCollider = GetComponent<BoxCollider2D>();
        anim = GetComponent<Animator>();
    }

    private void Start()
    {
        ultimateReady = true;
        inUltimate = false;
        preparingUltimate = false;
        if (verboseUltimateLog) Debug.Log("[ULTI] Init complete");
    }

    private void Update()
    {
        if (preparingUltimate)
        {
            // ระหว่างเตรียม Ultimate บังคับ run = false และไม่รับ input
            if (verboseUltimateLog) Debug.Log("[ULTI] Preparing phase... holding state");
            anim.SetBool("run", false);
            return;
        }
        // ระหว่าง Ultimate หรือกำลัง Dash ไม่รับ input อื่น
        if (inUltimate)
        {
            if (verboseUltimateLog) Debug.Log("[ULTI] In ultimate - input blocked");
            return;
        }
        if (isDashing) return;

        // Ultimate Input (กด K + อยู่บนพื้น + cooldown เสร็จ)
        if (Input.GetKeyDown(KeyCode.K) && ultimateReady && IsGrounded())
        {
            if (verboseUltimateLog) Debug.Log($"[ULTI] K pressed. run={anim.GetBool("run")}, velX={body.velocity.x:F3}, grounded={ground}");
            StartCoroutine(PrepareAndStartUltimate());
            return;
        }

        // Move left-right
        horizontalInput = Input.GetAxis("Horizontal");

        // Crouch
        isCrouching = Input.GetKey(KeyCode.S) && IsGrounded();

        // Attack (J)
        if (Input.GetKeyDown(KeyCode.J) && ground && !isCrouching && !attack && !isDashing)
        {
            body.velocity = new Vector2(0, body.velocity.y);
            anim.SetBool("run", false);
            anim.SetBool("atk", true);
            attack = true;
            speed = 0;
        }
        else
        {
            if (isCrouching)
            {
                if (Input.GetKeyDown(KeyCode.J) && ground && !attack && !isDashing)
                {
                    body.velocity = new Vector2(0, body.velocity.y);
                    anim.SetBool("run", false);
                    anim.SetBool("atk", true);
                    attack = true;
                    speed = 0;
                }
                else if (!isDashing)
                {
                    body.velocity = new Vector2(0, body.velocity.y);
                }
            }
            else if (!isDashing)
            {
                speed = 20f;
                body.velocity = new Vector2(horizontalInput * speed, body.velocity.y);
            }
        }

        // Dash (L)
        if (Input.GetKeyDown(KeyCode.L) && canDash)
            StartCoroutine(Dash());

        // Flip
        if (horizontalInput > 0.01f)
            transform.localScale = new Vector3(1, 1, 1);
        else if (horizontalInput < -0.01f)
            transform.localScale = new Vector3(-1, 1, 1);

        // Jump
        if (Input.GetKeyDown(KeyCode.W) && IsGrounded() && !isCrouching)
            Jump();

        // Animator update
        bool desiredRun = (horizontalInput != 0 && ground && !isCrouching);
        anim.SetBool("run", desiredRun);
        if (desiredRun != lastRunAnimValue)
        {
            if (verboseUltimateLog) Debug.Log($"[ANIM] run set -> {desiredRun} (velX={body.velocity.x:F2})");
            lastRunAnimValue = desiredRun;
        }
        anim.SetBool("ground", ground);
        anim.SetBool("crouch", isCrouching);
        anim.SetFloat("yVelocity", body.velocity.y);

        // Ultimate Cooldown
        if (!ultimateReady)
        {
            ultimateTimer += Time.deltaTime;
            float remain = Mathf.Max(0f, ultimateCooldown - ultimateTimer);
            if (logUltimateCooldown)
            {
                int sec = Mathf.CeilToInt(remain);
                if (sec != ultimateLastLoggedSecond)
                {
                    Debug.Log($"[ULTI] Cooldown remaining: {remain:F2}s");
                    ultimateLastLoggedSecond = sec;
                }
            }
            if (ultimateTimer >= ultimateCooldown)
            {
                ultimateTimer = 0f;
                ultimateReady = true;
                ultimateLastLoggedSecond = -1;
                if (logUltimateCooldown)
                    Debug.Log("[ULTI] Ready!");
            }
        }
    }

    private void StartUltimate()
    {
        if (verboseUltimateLog) Debug.Log("[ULTI] StartUltimate called");
        inUltimate = true;
        preparingUltimate = false;
        ultimateReady = false;
        ultimateTimer = 0f;
        ultimateLastLoggedSecond = -1;
        ultimateHitWarpDone = false; // reset warp flag
        if (logUltimateCooldown) Debug.Log("[ULTI] Activated - entering time stop");

        body.velocity = Vector2.zero;
        Time.timeScale = 0f;

        anim.updateMode = AnimatorUpdateMode.UnscaledTime;
        if (!string.IsNullOrEmpty(ultimateTrigger))
            anim.SetTrigger(ultimateTrigger);
    }

    public void UltimateDamageEvent()
    {
        if (AttackPoint == null) return;

        if (enableUltimateHitWarp && !ultimateHitWarpDone)
        {
            PerformUltimateWarp();
        }

        float hitRadius = radius * ultimateHitRadiusMultiplier;
        Collider2D[] enemies = Physics2D.OverlapCircleAll(AttackPoint.transform.position, hitRadius, Enemy);
        int hitCount = 0;
        foreach (var e in enemies)
        {
            var hp = e.GetComponent<HealthEnemy>();
            if (hp != null)
            {
                hp.TakeDamage(ultimateDamage);
                hitCount++;
            }
        }
        if (logUltimateCooldown) Debug.Log($"[ULTI] Damage Event executed. Hits: {hitCount}");
    }

    private void PerformUltimateWarp()
    {
        ultimateHitWarpDone = true;
        Vector3 startPos = transform.position;
        // spawn start effect
        SpawnWarpEffect(ultimateWarpStartEffect, startPos + (Vector3)effectOffset, "start");

        float direction = transform.localScale.x >= 0 ? 1f : -1f;
        float halfWidth = 0.0f;
        if (ultimateWarpUseColliderBounds && boxCollider != null)
            halfWidth = boxCollider.bounds.extents.x;

        Vector2 origin = startPos;
        Vector2 dir = new Vector2(direction, 0f);
        float maxDistance = ultimateHitWarpDistance + halfWidth;
        RaycastHit2D hit = Physics2D.Raycast(origin, dir, maxDistance, ultimateWarpBlockLayers);
        Vector3 target = startPos + new Vector3(direction * ultimateHitWarpDistance, 0f, 0f);
        if (hit.collider != null)
        {
            float dist = hit.distance - ultimateWarpSkin - halfWidth;
            if (dist < 0f) dist = 0f;
            target = startPos + new Vector3(direction * dist, 0f, 0f);
            if (verboseUltimateLog) Debug.Log($"[ULTI] Warp blocked by {hit.collider.name} @ {hit.point}, adjusted dist={dist:F2}");
        }
        else if (verboseUltimateLog)
        {
            Debug.Log($"[ULTI] Warp free path dist={ultimateHitWarpDistance:F2}");
        }

        if (ultimateHitWarpUseTrail && tr != null)
        {
            bool prev = tr.emitting;
            tr.emitting = true;
            tr.Clear();
            transform.position = target;
            tr.emitting = prev;
        }
        else
        {
            transform.position = target;
        }

        SpawnWarpEffect(ultimateWarpEndEffect, target + (Vector3)effectOffset, "end");
        if (verboseUltimateLog) Debug.Log($"[ULTI] Warp executed to {target}");
    }

    private void SpawnWarpEffect(GameObject prefab, Vector3 position, string phase)
    {
        if (prefab == null) return;
        GameObject fx = Instantiate(prefab, position, Quaternion.identity);
        if (spawnEffectsInUnscaledTime)
        {
            // ถ้า effect มี ParticleSystem ให้ใช้ unscaledTime
            var ps = fx.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                var main = ps.main;
                main.useUnscaledTime = true;
            }
        }
        if (verboseUltimateLog) Debug.Log($"[ULTI] Spawn effect {prefab.name} ({phase})");
    }

    public void UltimateFinishEvent()
    {
        if (verboseUltimateLog) Debug.Log("[ULTI] UltimateFinishEvent called");
        Time.timeScale = 1f;
        anim.updateMode = AnimatorUpdateMode.Normal;
        inUltimate = false;

        anim.ResetTrigger(ultimateTrigger);
        anim.SetBool("run", false);
        anim.SetBool("atk", false);
        anim.SetBool("crouch", false);
        anim.SetFloat("yVelocity", 0f);
        anim.Play("idle-K", 0, 0f);
        if (logUltimateCooldown) Debug.Log("[ULTI] Finished and returned to idle");
    }

    private void Jump()
    {
        body.velocity = new Vector2(body.velocity.x, jumpForce);
        ground = false;
        anim.SetTrigger("jump");
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
            ground = true;
        }
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

    public void OnAttackStart() { anim.SetBool("atk", true); attack = true; }
    public void OnAttackEnd() { StopAttackAnimation(); }
    public void StopAttackAnimation() { anim.SetBool("atk", false); attack = false; speed = 20f; }

    public void Attack()
    {
        if (AttackPoint == null) return;
        Collider2D[] enemy = Physics2D.OverlapCircleAll(AttackPoint.transform.position, radius, Enemy);
        foreach (Collider2D enemyGameobject in enemy)
        {
            var hp = enemyGameobject.GetComponent<HealthEnemy>();
            if (hp != null) hp.TakeDamage(damage);
        }
    }

    public void CrouchAttack()
    {
        if (CrouchAttackPoint == null) return;
        Collider2D[] enemy = Physics2D.OverlapCircleAll(CrouchAttackPoint.transform.position, crouchRadius, Enemy);
        foreach (Collider2D enemyGameobject in enemy)
        {
            var hp = enemyGameobject.GetComponent<HealthEnemy>();
            if (hp != null) hp.TakeDamage(damage * 3f);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (AttackPoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(AttackPoint.transform.position, radius);
        }
        if (AttackPoint != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(AttackPoint.transform.position, radius * ultimateHitRadiusMultiplier);
        }
        if (CrouchAttackPoint != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(CrouchAttackPoint.transform.position, crouchRadius);
        }
    }

    public void DebugAnimatorParameters()
    {
#if UNITY_EDITOR
        if (anim == null || anim.runtimeAnimatorController == null) return;
        foreach (var p in anim.parameters)
            Debug.Log($"Param: {p.name} ({p.type})");
#endif
    }

    public bool isUltimating { get { return inUltimate; } }

    private IEnumerator PrepareAndStartUltimate()
    {
        if (verboseUltimateLog) Debug.Log("[ULTI] PrepareAndStartUltimate() entered");
        preparingUltimate = true;
        body.velocity = new Vector2(0f, body.velocity.y);
        anim.SetBool("run", false);
        // Log state snapshot
        if (verboseUltimateLog) Debug.Log($"[ULTI] Pre-Yield State: velX={body.velocity.x:F3}, grounded={ground}, runParam={anim.GetBool("run")}");
        // รอ 1 เฟรมให้ Animator เคลียร์ transition วิ่ง
        yield return null;
        if (verboseUltimateLog) Debug.Log($"[ULTI] Post-Yield State: runParam={anim.GetBool("run")}, yVel={body.velocity.y:F3}");
        StartUltimate();
    }
}
