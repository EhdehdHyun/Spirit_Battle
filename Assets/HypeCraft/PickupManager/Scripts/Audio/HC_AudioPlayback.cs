namespace HC_Pickups
{
    using UnityEngine;

    // This class manages audio playback for the game, ensuring only one instance exists (Singleton pattern).
    // It provides methods to play audio events and handle audio playback for pickup objects.
    [RequireComponent(typeof(AudioSource))]
    public class HC_AudioPlayback : MonoBehaviour
    {
        // Singleton instance
        public static HC_AudioPlayback Instance { get; private set; }

        // Reference to the AudioSource component
        [SerializeField] AudioSource audioSource;

        // Public property to access the AudioSource
        public AudioSource AudioSource => audioSource;

        private void Awake()
        {
            // Singleton pattern to ensure only one instance exists
            if (Instance != null)
            {
                Debug.LogError("There can only be one HC_AudioPlayback in the scene!" + transform);
                Destroy(gameObject);
                return;
            }
            Instance = this;

            audioSource = GetComponent<AudioSource>(); // Get the AudioSource component
        }

        private void OnValidate()
        {
            audioSource = GetComponent<AudioSource>(); // Get the AudioSource component

            if (audioSource == null)
            {
                Debug.LogWarning("AudioSource component is missing! Please assign an AudioSource component.", this);
                return;
            }
        }

        public void PlayPickupAudio(GameObject pickupObject)
        {
            HC_PickupsManager pickupsManager = HC_PickupsManager.Instance;
            if (pickupsManager == null)
            {
                Debug.LogError("HC_PickupsManager instance not found.");
                return;
            }

            // Check if the pickup object is in the list of managed pickup objects
            foreach (var pu in pickupsManager.pickupManagers)
            {
                if (pu.objectsList.Contains(pickupObject))
                {
                    // Play the corresponding audio event if the audio manager is assigned
                    if (pickupsManager.audioSettings != null)
                    {
                        PlayAudioEvent(pickupsManager.audioSettings.audioEventsList[pickupsManager.audioSettings.audioEventIndex]);
                    }
                    else
                    {
                        Debug.LogError("audioManager is not assigned in HC_PickupsManager.");
                    }
                    break;
                }
            }
        }

        public void PlayAudioEvent(HC_AudioEvent audioEvent)
        {
            if (audioEvent == null)
            {
                Debug.LogError("audioEvent is null in PlayAudioEvent.");
                return;
            }

            if (audioSource == null)
            {
                Debug.LogError("audioSource is not assigned in HC_AudioPlayback.");
                return;
            }
            // Play the audio event using the assigned AudioSource
            audioEvent.Play(audioSource);
        }
    }
}
