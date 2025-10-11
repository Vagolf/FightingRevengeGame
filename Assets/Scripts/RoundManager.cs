using UnityEngine;
using System.Collections;

public class RoundManager : MonoBehaviour
{
    public int playerWinCount = 0;
    public int enemyWinCount = 0;
    public int roundsToWin = 2; // ต้องชนะ 2 รอบถึงจะชนะเกม

    [Header("Refs")]
    public Transform playerSpawn;
    public Transform enemySpawn;
    public GameObject player;
    public GameObject enemy;

    private HealthCh playerHealth;
    private HealthEnemy enemyHealth;
    private bool roundTransitioning;

    private void Update()
    {
        // lazy acquire refs
        if (player == null) player = GameObject.FindGameObjectWithTag("Player");
        if (enemy == null) enemy = GameObject.FindGameObjectWithTag("Enemy");
        if (playerHealth == null && player != null) playerHealth = player.GetComponent<HealthCh>();
        if (enemyHealth == null && enemy != null) enemyHealth = enemy.GetComponent<HealthEnemy>();

        if (roundTransitioning) return;

        // เช็คว่าเลือดหมดหรือยัง แล้วตัดสินรอบ
        if (playerHealth != null && playerHealth.currentHealth <= 0f)
        {
            roundTransitioning = true;
            EnemyWinsRound();
            return;
        }
        if (enemyHealth != null && enemyHealth.currentHealth <= 0f)
        {
            roundTransitioning = true;
            PlayerWinsRound();
            return;
        }
    }

    public void PlayerWinsRound()
    {
        playerWinCount++;
        Debug.Log("Player wins round! Total = " + playerWinCount);

        if (playerWinCount >= roundsToWin)
        {
            Debug.Log("PLAYER WINS THE MATCH!");
            EndMatch(true);
        }
        else
        {
            StartNextRound();
        }
    }

    public void EnemyWinsRound()
    {
        enemyWinCount++;
        Debug.Log("Enemy wins round! Total = " + enemyWinCount);

        if (enemyWinCount >= roundsToWin)
        {
            Debug.Log("ENEMY WINS THE MATCH!");
            EndMatch(false);
        }
        else
        {
            StartNextRound();
        }
    }

    void StartNextRound()
    {
        Debug.Log("Starting next round...");
        StartCoroutine(DoStartNextRound());
    }

    IEnumerator DoStartNextRound()
    {
        // locate refs if not assigned
        if (player == null) player = GameObject.FindGameObjectWithTag("Player");
        if (enemy == null) enemy = GameObject.FindGameObjectWithTag("Enemy");

        // 1) Reset HP
        if (player != null)
            player.GetComponent<HealthCh>()?.ResetForNewRound();
        if (enemy != null)
            enemy.GetComponent<HealthEnemy>()?.ResetForNewRound();

        // 2) Reset positions
        if (player != null && playerSpawn != null)
        {
            var p = player.transform.position;
            var sp = playerSpawn.position;
            player.transform.position = new Vector3(sp.x, sp.y, p.z); // preserve z
        }
        if (enemy != null && enemySpawn != null)
        {
            var p = enemy.transform.position;
            var sp = enemySpawn.position;
            enemy.transform.position = new Vector3(sp.x, sp.y, p.z); // preserve z
        }

        // 3) Optional: short countdown via Timer if present
        var timer = FindObjectOfType<Timer>();
        if (timer != null)
        {
            // re-enable timer component to restart its OnEnable() logic
            timer.enabled = false;
            yield return null; // wait a frame
            timer.enabled = true;
        }

        roundTransitioning = false;
    }

    void EndMatch(bool playerWin)
    {
        if (playerWin)
        {
            // แสดง Victory UI หรือไปฉากต่อไป
            Debug.Log("Show Victory Screen");
        }
        else
        {
            // แสดง Game Over
            Debug.Log("Show Defeat Screen");
        }
    }
}
