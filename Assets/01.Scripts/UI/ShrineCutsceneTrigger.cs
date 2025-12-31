using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShrineCutsceneTrigger : MonoBehaviour
{
    public ShrineCutsceneManager cutscene;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        cutscene.PlayCutscene();
        gameObject.SetActive(false);
    }
}

