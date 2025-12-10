namespace HC_Pickups
{
    using UnityEngine;

    /// <summary>
    /// HC_Collectable is responsible for setting up the collectable objects in the game.
    /// It ensures that the collectable object has a BoxCollider component set as a trigger.
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    public class HC_Collectable : MonoBehaviour
    {
        /// <summary>
        /// Determines if this object can currently be collected by the player.
        /// Set this to false to make the object uncollectable (for example, if it should be locked or disabled).
        /// </summary>
        [Tooltip("Can this object be collected?")]
        public bool canBeCollected = true;

        private void Reset()
        {
            GetComponent<BoxCollider>().isTrigger = true;
        }
    }
}
