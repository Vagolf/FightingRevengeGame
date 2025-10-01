using UnityEngine;
using System.Collections;

public class EventPause : MonoBehaviour
{
    public Animator animator;   // Animator ของตัวละคร
    public string animationName = "Kaisa-ltimate"; // ชื่อ animation
    public float freezeDuration = 3f; // เวลาที่ค้างไว้
    public bool autoTriggerOnStart = false; // เรียกเองตอน Start หรือไม่

    private bool hasPlayed = false;

    private void Start()
    {
        if (autoTriggerOnStart)
        {
            TriggerEventAnimation();
        }
    }

    public void TriggerEventAnimation()
    {
        if (!hasPlayed)
            StartCoroutine(PlayEventAnimation());
    }

    private IEnumerator PlayEventAnimation()
    {
        hasPlayed = true;

        // ตั้ง Animator ให้เล่น animation โดยใช้ Unscaled Time
        animator.updateMode = AnimatorUpdateMode.UnscaledTime;
        animator.Play(animationName, 0, 0);

        // รอจน animation เล่นจบ 1 รอบ
        AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
        while (state.normalizedTime < 1.0f)
        {
            yield return null;
            state = animator.GetCurrentAnimatorStateInfo(0);
        }

        // Freeze pose ปัจจุบัน
        animator.speed = 0f;

        // ค้างไว้ freezeDuration วินาที (ไม่โดน TimeScale)
        yield return new WaitForSecondsRealtime(freezeDuration);

        // กลับมาเล่นปกติ
        animator.speed = 1f;
        // ปิดตัวเอง ไม่ให้ทำงานซ้ำ
        enabled = false;
    }
}
