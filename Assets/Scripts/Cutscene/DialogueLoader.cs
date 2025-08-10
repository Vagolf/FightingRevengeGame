using UnityEngine;
using System.Collections.Generic;
using System.IO;

[System.Serializable]
public class DialogueLine
{
    public string speakerName;
    public string dialogue;
    public string leftSprite;
    public string rightSprite;
    public string backgroundSprite;
    public string leftExpression;
    public string rightExpression;
    public string voiceClip;
    public float waitAfter;
}

public class DialogueLoader
{
    public static List<DialogueLine> LoadFromCSV(string fileName)
    {
        List<DialogueLine> lines = new List<DialogueLine>();

        TextAsset csvData = Resources.Load<TextAsset>(fileName);
        if (csvData == null)
        {
            Debug.LogError("CSV file not found in Resources: " + fileName);
            return lines;
        }           

        string[] data = csvData.text.Split('\n');
        for (int i = 1; i < data.Length; i++) // เริ่มจาก 1 เพื่อข้าม header
        {
            if (string.IsNullOrWhiteSpace(data[i])) continue;

            string[] row = data[i].Split(',');
            if (row.Length < 9) continue; // ข้อมูลไม่ครบ
            DialogueLine line = new DialogueLine
            {
                speakerName = row[0],
                dialogue = row[1],
                leftSprite = row[2],
                rightSprite = row[3],
                backgroundSprite = row[4],
                leftExpression = row[5],
                rightExpression = row[6],
                voiceClip = row[7],
                waitAfter = float.TryParse(row[8], out float wait) ? wait : 0f
            };
            lines.Add(line);
        }
        return lines;
    }
}

