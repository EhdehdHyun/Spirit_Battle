using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public DataManager Data { get; private set; }
    public QuestManager Quest { get; private set; }
    
    public bool IsUIBlocked { get; private set; }

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        Data = new DataManager();
        Data.Initialize();
    }
    public void SetUIBlock(bool blocked)
    {
        IsUIBlocked = blocked;
    }
}