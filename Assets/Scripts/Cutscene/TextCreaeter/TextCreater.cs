using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TextCreater : MonoBehaviour
{
    public static TMPro.TMP_Text viewText;
    public static bool runText;
    public static int charCount;
    [SerializeField] string transferText;
    [SerializeField] int internalCount;

    // Update is called once per frame
    void Update()
    {
        internalCount = charCount;
        charCount = GetComponent<TMPro.TMP_Text>().text.Length;
        if (runText == true)
        {
            runText = false;
            viewText = GetComponent<TMPro.TMP_Text>();
            transferText = viewText.text;
            viewText.text = "";
            StartCoroutine(RunText());
        }
    }

    IEnumerator RunText()
    {
        foreach (char c in transferText)
        {
            viewText.text += c;
            yield return new WaitForSeconds(0.05f);
        }
    }
}
