using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NPCIdentity : MonoBehaviour
{
    [Header("NPC Identity")] [SerializeField]
    private int npcID;

    public int NPCID => npcID;
}