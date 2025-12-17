using UnityEngine;

public class ParryTelegraphUI : MonoBehaviour
{
    [SerializeField] private GameObject yellowRing;
    [SerializeField] private GameObject redRing;

    public void Show(ParryTelegraphType type, float duration)
    {
        if (yellowRing) yellowRing.SetActive(type == ParryTelegraphType.SlowYellow);
        if (redRing) redRing.SetActive(type == ParryTelegraphType.FastRed);
    }

    public void Hide()
    {
        if (yellowRing) yellowRing.SetActive(false);
        if (redRing) redRing.SetActive(false);
    }
}
