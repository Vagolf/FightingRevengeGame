using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SceneSethStart : MonoBehaviour
{
    public GameObject Fadescene;
    public GameObject ChaSeth;
    public GameObject ChKaisa;
    public GameObject textBox;

    [SerializeField] string textToSpeak;
    [SerializeField] int currentTextLength;
    [SerializeField] int textLength;
    [SerializeField] GameObject mainTextObject;

    [SerializeField] GameObject nextBotton;
    [SerializeField] int eventPos = 0;
    private void Update()
    {
        textLength = TextCreater.charCount;
    }

    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(EvenStart());
    }

    // Update is called once per frame
    IEnumerator EvenStart()
    {
        yield return new WaitForSeconds(2f);
        Fadescene.SetActive(false);
        ChaSeth.SetActive(true);
        yield return new WaitForSeconds(2f);
        ChKaisa.SetActive(true);
        //Talk to Seth
        yield return new WaitForSeconds(2f);
        mainTextObject.SetActive(true);
        textToSpeak = "The journey to revenge does not rely solely on anger, but also requires wisdom and skill.";
        textBox.GetComponent<TMPro.TMP_Text>().text = textToSpeak;
        currentTextLength = textToSpeak.Length;
        TextCreater.runText = true;
        yield return new WaitForSeconds(0.05f);
        yield return new WaitForSeconds(1);
        yield return new WaitUntil(() => textLength == currentTextLength);
        yield return new WaitForSeconds(0.3f);
        nextBotton.SetActive(true);
        eventPos = 1;

        //textBox.SetActive(true);
    }
    
    IEnumerator EventOne()
    {
        nextBotton.SetActive(false);
        textBox.SetActive(true);
        yield return new WaitForSeconds(0.5f);

        textToSpeak = "I already know but I'll keep doing it.";
        textBox.GetComponent<TMPro.TMP_Text>().text = textToSpeak;
        currentTextLength = textToSpeak.Length;
        TextCreater.runText = true;
        yield return new WaitForSeconds(0.05f);
        yield return new WaitForSeconds(1);
        yield return new WaitUntil(() => textLength == currentTextLength);
        yield return new WaitForSeconds(0.3f);
        nextBotton.SetActive(true);
    }

    public  void NextBotton()
    {
        if (eventPos == 1)
        {
            StartCoroutine(EventOne());

        }
    }

}
