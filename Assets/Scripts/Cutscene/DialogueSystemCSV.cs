using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class DialogueSystemCSV : MonoBehaviour
{
    public Image background;
    public Image characterLeft;
    public Image characterRight;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI dialogueText;
    public GameObject nextIndicator;
    public Button skipButton;
    public float textSpeed = 0.03f;
    public string csvFileName = "Dialogue"; // ใส่ชื่อไฟล์ CSV ที่อยู่ใน Resources

    private List<DialogueLine> lines;
    private int currentLineIndex = 0;
    private bool isTyping = false;
    private bool skipTyping = false;
    private AudioSource audioSource;
    private Coroutine typeCoroutine;
    private bool cutsceneEnded = false;

    void Awake()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        nextIndicator.SetActive(false);
    }

    void Start()
    {
        skipButton.onClick.AddListener(SkipCutscene);
        lines = DialogueLoader.LoadFromCSV(csvFileName);
        StartCoroutine(PlayDialogue());
    }

    IEnumerator PlayDialogue()
    {
        while (currentLineIndex < lines.Count)
        {
            DialogueLine line = lines[currentLineIndex];

            // โหลดและเปลี่ยนพื้นหลัง
            if (!string.IsNullOrEmpty(line.backgroundSprite))
                background.sprite = Resources.Load<Sprite>("Sprites/Backgrounds/" + line.backgroundSprite);

            // โหลดและเปลี่ยน sprite ตัวละครซ้าย/ขวา (รองรับ expression)
            if (!string.IsNullOrEmpty(line.leftSprite))
            {
                string leftPath = "Sprites/Characters/" + line.leftSprite;
                if (!string.IsNullOrEmpty(line.leftExpression))
                    leftPath += "_" + line.leftExpression;
                Sprite leftSprite = Resources.Load<Sprite>(leftPath);
                if (leftSprite != null) characterLeft.sprite = leftSprite;
            }
            if (!string.IsNullOrEmpty(line.rightSprite))
            {
                string rightPath = "Sprites/Characters/" + line.rightSprite;
                if (!string.IsNullOrEmpty(line.rightExpression))
                    rightPath += "_" + line.rightExpression;
                Sprite rightSprite = Resources.Load<Sprite>(rightPath);
                if (rightSprite != null) characterRight.sprite = rightSprite;
            }

            // ชื่อคนพูด
            nameText.text = line.speakerName;

            // เล่น voice clip ถ้ามี
            if (!string.IsNullOrEmpty(line.voiceClip))
            {
                AudioClip voice = Resources.Load<AudioClip>("Audio/VO/" + line.voiceClip);
                if (voice != null)
                {
                    audioSource.Stop();
                    audioSource.clip = voice;
                    audioSource.Play();
                }
            }
            else
            {
                audioSource.Stop();
            }

            // Typewriter effect
            dialogueText.text = "";
            isTyping = true;
            skipTyping = false;
            nextIndicator.SetActive(false);
            typeCoroutine = StartCoroutine(TypeLine(line.dialogue));
            yield return new WaitUntil(() => !isTyping);

            // กระพริบ next indicator
            Coroutine blink = StartCoroutine(BlinkNextIndicator());

            // รอ input เพื่อไปต่อ
            yield return new WaitUntil(() => (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0)) && !isTyping);
            StopCoroutine(blink);
            nextIndicator.SetActive(false);

            // waitAfter
            if (line.waitAfter > 0f)
                yield return new WaitForSeconds(line.waitAfter);

            currentLineIndex++;
        }
        EndCutscene();
    }

    IEnumerator TypeLine(string line)
    {
        dialogueText.text = "";
        foreach (char letter in line)
        {
            if (skipTyping)
            {
                dialogueText.text = line;
                break;
            }
            dialogueText.text += letter;
            yield return new WaitForSeconds(textSpeed);
        }
        isTyping = false;
    }

    IEnumerator BlinkNextIndicator()
    {
        while (true)
        {
            nextIndicator.SetActive(true);
            yield return new WaitForSeconds(0.5f);
            nextIndicator.SetActive(false);
            yield return new WaitForSeconds(0.5f);
        }
    }

    void Update()
    {
        if (cutsceneEnded) return;
        if (isTyping && (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0)))
        {
            skipTyping = true;
        }
    }

    public void SkipCutscene()
    {
        StopAllCoroutines();
        audioSource.Stop();
        EndCutscene();
    }

    void EndCutscene()
    {
        cutsceneEnded = true;
        nextIndicator.SetActive(false);
        dialogueText.text = "";
        nameText.text = "";
        // TODO: ส่ง control กลับไป gameplay หรือโหลด scene ต่อไป
        Debug.Log("Cutscene Ended - load gameplay scene here");
    }
}
