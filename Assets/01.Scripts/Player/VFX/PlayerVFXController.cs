using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

public class PlayerVFXController : MonoBehaviour
{
    [System.Serializable]
    public class SlashCfg
    {
        public VisualEffect prefab;
        public float lifeTime = 0.35f;
        public Vector3 localPos;
        public Vector3 localEuler;
        public int prewarm = 3;
    }

    [Header("Slash")]
    [SerializeField] private Transform slashSocket;
    [SerializeField] private List<SlashCfg> slashes = new();

    private readonly List<Queue<VisualEffect>> pools = new();

    private void Awake()
    {
        pools.Clear();
        for (int i = 0; i < slashes.Count; i++)
        {
            pools.Add(new Queue<VisualEffect>());

            var cfg = slashes[i];
            for (int j = 0; j < Mathf.Max(0, cfg.prewarm); j++)
            {
                var vfx = CreateInstance(cfg);
                pools[i].Enqueue(vfx);
            }
        }
    }
  
    private VisualEffect CreateInstance(SlashCfg cfg)
    {
        if(!cfg.prefab) return null;

        var vfx = Instantiate(cfg.prefab);
        vfx.gameObject.SetActive(false);
        return vfx;
    }

    private VisualEffect Get(int idx)
    {
        if(idx < 0 || idx >= slashes.Count) return null;

        if (pools[idx].Count > 0)
        {
            return pools[idx].Dequeue();
        }
        return CreateInstance(slashes[idx]);
    }

    private void Release(int idx, VisualEffect vfx)
    {
        if (!vfx) return;

        vfx.gameObject.SetActive(false);

        if(idx >= 0 && idx < pools.Count)
        {
            pools[idx].Enqueue(vfx);
        }
        else
        {
            Destroy(vfx.gameObject);
        }
    }
    public void PlaySlash(int comboIndex)
    {
        if (!slashSocket) return;
        if (slashes == null || slashes.Count == 0) return;

        int idx = Mathf.Clamp(comboIndex - 1, 0, slashes.Count - 1);
        var cfg = slashes[idx];

        var vfx = Get(idx);
        if (!vfx) return;

        vfx.transform.SetParent(slashSocket, false);
        vfx.transform.localPosition = cfg.localPos;
        vfx.transform.localRotation = Quaternion.Euler(cfg.localEuler);

        vfx.gameObject.SetActive(true);
        vfx.Reinit();
        vfx.SendEvent("OnPlay");

        StartCoroutine(CoRelease(idx, vfx, Mathf.Max(0.01f, cfg.lifeTime)));
    }

    private IEnumerator CoRelease(int idx, VisualEffect vfx, float t)
    {
        yield return new WaitForSeconds(t);
        Release(idx, vfx);
    }
}
