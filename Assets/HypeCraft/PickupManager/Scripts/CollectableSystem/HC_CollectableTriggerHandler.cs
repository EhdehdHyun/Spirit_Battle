namespace HC_Pickups
{
    using UnityEngine;

    /// <summary>
    /// HC_CollectableTriggerHandler is responsible for handling the collection of objects in the game.
    /// It detects when a player collides with a collectable object and triggers the collection process.
    /// The class ensures that the collectable object interacts with other components to update the score and play audio.
    /// </summary>
    public class HC_CollectableTriggerHandler : MonoBehaviour
    {
        // Method called when a 3D collider enters the trigger
        private void OnTriggerEnter(Collider collision)
        {
            // Check if the collided object has the HC_Collectable component
            HC_Collectable collectable = collision.gameObject.GetComponent<HC_Collectable>();
            // Only collect the object if it exists and canBeCollected is true
            if (collectable != null && collectable.canBeCollected)
            {
                // Collect the object
                Collect(collision.gameObject);

                // Check if audio is enabled before playing pickup audio
                if (HC_AudioSettings.Instance.audioEnabled)
                {
                    HC_AudioPlayback.Instance.PlayPickupAudio(collision.gameObject);
                }

                // Deactivate the collected object
                collision.gameObject.SetActive(false);
            }
        }

        // Method to handle the collection of an object
        public void Collect(GameObject collectedObject)
        {
            // Call the Collect method from the HC_ScoreManager to update the score
            HC_ScoreManager.Instance.Collect(collectedObject);

            // Call the RemoveEffect method from the HC_EffectSettingsSO to handle effects
            HC_PickupsManager.Instance.effectSettings.RemoveEffect(collectedObject.transform);
        }
    }
}
