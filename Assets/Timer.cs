using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class Timer : MonoBehaviour
{
    // Global short gate: true = block other systems
    public static bool GateBlocked { get; private set; }
    public bool IsCountingDown => countdownTime > 0f;
    public bool IsRunning => countdownTime <= 0f && remainingTime > 0f;
    [SerializeField] TextMeshProUGUI timerText;
    [SerializeField] float remainingTime;
    [SerializeField] TextMeshProUGUI countdownText;
    [SerializeField] float countdownTime = 3f; // 3-second pre-countdown
    [SerializeField] float defaultRemainingSeconds = 120f; // default main timer length
    

    private void OnEnable()
    {
        // Fully reset when enabled
        Restart(3f, defaultRemainingSeconds);
    }

    void Update()
    {
        // short countdown gate (independent from UI assignment)
        if (countdownTime > 0f)
        {
            countdownTime -= Time.deltaTime;
            if (countdownText)
                countdownText.text = Mathf.CeilToInt(Mathf.Max(0f, countdownTime)).ToString();
            if (countdownTime > 0f)
            {
                GateBlocked = true; // block others while counting
                return; // still counting down
            }
            if (countdownText) countdownText.enabled = false; // hide when finished
            GateBlocked = false; // release gate when countdown done
        }

        if (remainingTime > 0)
        {
            remainingTime -= Time.deltaTime;
        }
        else if (remainingTime < 0)
        {
            remainingTime = 0;
            // Timer has finished, you can add additional logic here if needed
        }
        int minutes = Mathf.FloorToInt(remainingTime / 60);
        int seconds = Mathf.FloorToInt(remainingTime % 60);
        timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);


    }

    // Reset and start countdown + main timer
    public void Restart(float countdownSeconds = 3f, float remainingSeconds = 120f)
    {
        countdownTime = Mathf.Max(0f, countdownSeconds);
        remainingTime = Mathf.Max(0f, remainingSeconds);
        GateBlocked = countdownTime > 0f;
        if (countdownText)
        {
            countdownText.enabled = countdownTime > 0f;
            if (countdownTime > 0f)
                countdownText.text = Mathf.CeilToInt(countdownTime).ToString();
        }
        // update main timer display immediately
        if (timerText)
        {
            int minutes = Mathf.FloorToInt(remainingTime / 60f);
            int seconds = Mathf.FloorToInt(remainingTime % 60f);
            timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
        }
    }
}
