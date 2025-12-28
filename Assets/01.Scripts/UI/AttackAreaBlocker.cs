using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttackAreaBlocker : MonoBehaviour
{
        [SerializeField] private Collider blockCollider;
        bool opened;

        public void Open()
        {
            if (opened) return;
            opened = true;
            blockCollider.enabled = false;
            TutorialManager.Instance.ShowSimpleMessage(null);
        }
}
