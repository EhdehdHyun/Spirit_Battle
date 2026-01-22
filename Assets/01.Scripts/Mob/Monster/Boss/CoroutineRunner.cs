using System.Collections;
using UnityEngine;

public class CoroutineRunner : MonoBehaviour
{
    private static CoroutineRunner _inst;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        Ensure();
    }

    private static void Ensure()
    {
        if (_inst != null) return;

        var go = new GameObject("[CoroutineRunner]");
        DontDestroyOnLoad(go);
        _inst = go.AddComponent<CoroutineRunner>();
    }

    public static Coroutine Run(IEnumerator routine)
    {
        if (routine == null) return null;
        Ensure();
        return _inst.StartCoroutine(routine);
    }

    public static void Stop(Coroutine routine)
    {
        if (_inst == null || routine == null) return;
        _inst.StopCoroutine(routine);
    }
}