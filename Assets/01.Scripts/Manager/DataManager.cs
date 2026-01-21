using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class DataManager
{
    public Data_tableLoader Data_TableLoader { get; private set; }
    public skill_Data_TableLoader Skill_Data_TableLoader { get; private set; }
    public DialogueTableLoader DialogueTableLoader { get; private set; }
    public Quest_Data_Loader Quest_Data_Loader { get; private set; }
    public Reward_Data_Loader Reward_Data_Loader { get; private set; }

    public SaveData CurrentData { get; set; }

    private string savePath;

    public void Initialize()
    {
        Data_TableLoader = new Data_tableLoader();
        Skill_Data_TableLoader = new skill_Data_TableLoader();
        DialogueTableLoader = new DialogueTableLoader();
        Quest_Data_Loader = new Quest_Data_Loader();
        Reward_Data_Loader = new Reward_Data_Loader();

        savePath = Path.Combine(Application.persistentDataPath, "savefile.json");
        LoadSaveFile();
    }

    public void Save()
    {
        if (CurrentData == null) CurrentData = new SaveData();

        string json = JsonUtility.ToJson(CurrentData, true);
        File.WriteAllText(savePath, json);
        
    }

    public void LoadSaveFile()
    {
        if (File.Exists(savePath))
        {
            string json = File.ReadAllText(savePath);
            CurrentData = JsonUtility.FromJson<SaveData>(json);
        }
        else
        {
            CurrentData = new SaveData();
        }
    }
}