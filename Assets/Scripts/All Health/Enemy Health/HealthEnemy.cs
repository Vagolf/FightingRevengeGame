using System.Collections;
using UnityEngine;

public class HealthEnemy : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] public float startingHealth;

    public HealthBar healthBar; // Reference to the HealthBar script

    private bool isDead;
    public float currentHealth { get; private set; }
    private Animator anim;

    [Header("iFrames")]
    [SerializeField] private float iFramesDuration = 0.5f;
    [SerializeField] private int numberOfFlashes = 3;
    private SpriteRenderer spriteRend;
    private bool isInvulnerable = false;

    [Header("PopUpDamage")]
    public GameObject popUpDamagePrefab;

    private void Start()
    {
        isDead = false;
        currentHealth = startingHealth;
        healthBar.setMaxHealth(startingHealth);
        healthBar.SetHealth(currentHealth);
        anim = GetComponent<Animator>();
        spriteRend = GetComponent<SpriteRenderer>();
    }

    private void Update()
    {
        if (startingHealth < 0) // For test damage
        {
            
            Debug.Log("Enemy health is below zero, taking damage.");
        }
    }

    public void SetHealth(float healthChange)
    {
        currentHealth += healthChange;
        currentHealth = Mathf.Clamp(currentHealth, 0, startingHealth);
        healthBar.SetHealth(currentHealth);

        if (currentHealth <= 0 && !isDead)
        {
            anim.SetTrigger("die");
            isDead = true;
        }
    }

    public void TakeDamage(float damage)
    {
        if (!isInvulnerable && !isDead)
        {
            currentHealth = Mathf.Clamp(currentHealth - damage, 0, startingHealth);
            healthBar.SetHealth(currentHealth);
            SpawnPopup(damage);

            if (currentHealth <= 0 && !isDead)
            {
                anim.SetTrigger("die");
                isDead = true;
            }
            else
            {
                anim.SetTrigger("hurt");
                StartCoroutine(Invulnerability());
            }
        }
    }

    // Ultimate multi-hit damage ignoring iFrames and using no timeScale dependent waits.
    public void TakeDamageUltimate(float damage)
    {
        if (isDead) return;
        // ignore isInvulnerable for ultimate
        currentHealth = Mathf.Clamp(currentHealth - damage, 0, startingHealth);
        healthBar.SetHealth(currentHealth);
        SpawnPopup(damage);
        if (currentHealth <= 0 && !isDead)
        {
            anim.SetTrigger("die");
            isDead = true;
        }
        else
        {
            anim.SetTrigger("hurt"); // stay in hurt chain by repeated triggers
        }
    }

    private void SpawnPopup(float damage)
    {
        if (popUpDamagePrefab == null) return;
        GameObject popUp = Instantiate(popUpDamagePrefab, transform.position, Quaternion.identity);
        var tmp = popUp.GetComponentInChildren<TMPro.TextMeshProUGUI>();
        if (tmp != null) tmp.text = damage.ToString();
        else
        {
            var tm = popUp.GetComponentInChildren<TextMesh>();
            if (tm != null) tm.text = damage.ToString();
        }
    }

    private IEnumerator Invulnerability()
    {
        isInvulnerable = true;
        for (int i = 0; i < numberOfFlashes; i++)
        {
            spriteRend.color = new Color(1, 0, 0, 0.5f);
            yield return new WaitForSeconds(iFramesDuration / (numberOfFlashes * 2));
            spriteRend.color = Color.white;
            yield return new WaitForSeconds(iFramesDuration / (numberOfFlashes * 2));
        }
        isInvulnerable = false;
    }

    // Revive and restore to full for a new round
    public void ResetForNewRound()
    {
        StopAllCoroutines();
        isInvulnerable = false;
        isDead = false;
        gameObject.SetActive(true);
        currentHealth = startingHealth;
        if (healthBar != null)
        {
            healthBar.setMaxHealth(startingHealth);
            healthBar.SetHealth(currentHealth);
        }
        if (spriteRend != null)
        {
            spriteRend.enabled = true;
            spriteRend.color = Color.white;
        }
        if (anim != null)
        {
            anim.ResetTrigger("die");
            anim.ResetTrigger("hurt");
            anim.Update(0f);
        }
        var enemyCtrl = GetComponent<Enemy>();
        if (enemyCtrl != null) enemyCtrl.enabled = true;
        var rb = GetComponent<Rigidbody2D>();
        if (rb != null) rb.velocity = Vector2.zero;
    }
}
