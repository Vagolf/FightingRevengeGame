using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[DisallowMultipleComponent]
public class EnemyAI : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private string playerTag = "Player";
    private Transform player;

    [Header("Ranges")]
    [SerializeField] private float detectRange = 12f;
    [SerializeField] private float attackRange = 2.2f;
    [SerializeField] private float stopDistance = 1.2f;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 6f;

    [Header("Attack")]
    [SerializeField] private Transform attackPoint;
    [SerializeField] private float attackRadius = 1.8f;
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private float damage = 50f;
    [SerializeField] private float attackCooldown = 1.0f;
    private float attackTimer = 0f;

    private Rigidbody2D rb;
    private Animator anim;
    private bool combatStarted;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
    }

    private void Start()
    {
        var p = GameObject.FindGameObjectWithTag(playerTag);
        if (p != null) player = p.transform;
    }

    private void Update()
    {
        if (Timer.GateBlocked)
        {
            // stop sliding while countdown
            rb.velocity = new Vector2(0f, rb.velocity.y);
            SetRun(false);
            return;
        }
        if (player == null)
        {
            var p = GameObject.FindGameObjectWithTag(playerTag);
            if (p != null) player = p.transform; else return;
        }

        if (attackTimer > 0f) attackTimer -= Time.deltaTime;

        // measure from attack point if available for accuracy
        Vector3 attackPos = attackPoint != null ? attackPoint.position : transform.position;
        float dist = Vector2.Distance(attackPos, player.position);
        if (dist > detectRange)
        {
            // ??????????????
            rb.velocity = new Vector2(0f, rb.velocity.y);
            SetRun(false);
            combatStarted = false; // reset combat flag when target far away
            return;
        }

        // ???????????????????
        var dir = Mathf.Sign(player.position.x - transform.position.x);
        if (dir != 0) transform.localScale = new Vector3(dir > 0 ? 1f : -1f, 1f, 1f);

        // move until within attackRange
        if (dist > attackRange)
        {
            // ????????????
            rb.velocity = new Vector2(dir * moveSpeed, rb.velocity.y);
            SetRun(true);
            // start timer when we first engage (only if timer not already running)
            if (!combatStarted)
            {
                var timer = FindObjectOfType<Timer>();
                if (timer != null && !timer.IsCountingDown && !timer.IsRunning)
                    timer.Restart(3f, 120f);
                combatStarted = true;
            }
        }
        else
        {
            // ?????????/????
            rb.velocity = new Vector2(0f, rb.velocity.y);
            SetRun(false);
            if (!combatStarted)
            {
                var timer = FindObjectOfType<Timer>();
                if (timer != null && !timer.IsCountingDown && !timer.IsRunning)
                    timer.Restart(3f, 120f);
                combatStarted = true;
            }
            TryAttack();
        }
    }

    private void TryAttack()
    {
        if (attackTimer > 0f) return;
        if (attackPoint == null)
        {
            Debug.LogWarning("EnemyAI: Missing AttackPoint on Enemy.");
            return;
        }
        // play anim and apply damage via overlap
        if (anim)
        {
            anim.SetBool("run", false);
            anim.SetBool("atk", true);
        }
        var hits = Physics2D.OverlapCircleAll(attackPoint.position, attackRadius, playerLayer);
        foreach (var h in hits)
        {
            h.GetComponent<HealthCh>()?.TakeDamage(damage);
        }
        attackTimer = attackCooldown;
        // ???????? atk ????????????????? (?????????????????????????????????)
        if (isActiveAndEnabled) Invoke(nameof(ClearAttackFlag), 0.05f);
    }

    private void ClearAttackFlag()
    {
        if (anim != null) anim.SetBool("atk", false);
    }

    private void SetRun(bool run)
    {
        if (anim != null) anim.SetBool("run", run);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.6f, 0f, 0.8f);
        Gizmos.DrawWireSphere(transform.position, detectRange);
        Gizmos.color = Color.red;
        if (attackPoint != null)
            Gizmos.DrawWireSphere(attackPoint.position, attackRadius);
        else
            Gizmos.DrawWireSphere(transform.position, attackRadius);
    }
}
