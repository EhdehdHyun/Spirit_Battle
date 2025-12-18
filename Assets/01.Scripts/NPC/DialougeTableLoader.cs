using System.Collections.Generic;
using UnityEngine;

public class DialogueTableLoader
{
    private Dictionary<string, DialogueRow> dialogueDict;

    public DialogueTableLoader()
    {
        Load();
    }

    private void Load()
    {
        TextAsset json = Resources.Load<TextAsset>("JSON/Dialogue_Data_Table");
        var wrapper = JsonUtility.FromJson<DialogueWrapper>(json.text);

        dialogueDict = new Dictionary<string, DialogueRow>();
        foreach (var row in wrapper.Dialogues)
            dialogueDict[row.DialogueID] = row;
    }

    public DialogueRow GetDialogue(string id)
    {
        dialogueDict.TryGetValue(id, out var row);
        return row;
    }
}

[System.Serializable]
public class DialogueWrapper
{
    public List<DialogueRow> Dialogues;
}

[System.Serializable]
public class DialogueRow
{
    public string DialogueID;
    public string SpeakerID;
    public string SpeakerName;
    public string Text;
    public string NextID;
    public bool IsChoice;
}