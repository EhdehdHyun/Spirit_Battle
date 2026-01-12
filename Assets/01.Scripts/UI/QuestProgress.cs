public class QuestProgress
{
    public int Current;
    public int Target;

    public QuestProgress(int target)
    {
        Current = 0;
        Target = target;
    }

    public bool IsComplete => Current >= Target;
}