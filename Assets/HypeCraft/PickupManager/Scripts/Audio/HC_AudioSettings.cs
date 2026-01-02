namespace HC_Pickups
{
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// HC_AudioSettings is responsible for managing audio settings in the game.
    /// It provides functionality to enable or disable audio, manage a list of audio events,
    /// and select specific audio events for playback. This class ensures that only one instance
    /// exists in the scene using the Singleton pattern.
    /// </summary>

    public class HC_AudioSettings : MonoBehaviour
    {
        public static HC_AudioSettings Instance { get; private set; } // Singleton instance

        [Header("Audio Settings")]
        [Space]

        [Tooltip("Audio On/Off  -  Collection sound")]
        public bool audioEnabled = true; // Toggle for enabling/disabling audio
        [Tooltip("Index of the selected audio event")]
        [Range(0, 100)] public int audioEventIndex = 0; // Index of the selected audio event

        [Tooltip("Add audio events/audio clips  -  An audio event contains audio clips")]
        public List<HC_AudioEvent> audioEventsList = new List<HC_AudioEvent>(); // List of audio events

        [HideInInspector] public bool moreSettings = false; // Additional settings toggle

        private void Awake()
        {
            // Singleton pattern to ensure only one instance exists
            if (Instance != null)
            {
                Debug.LogError("There can only be one HC_AudioSettings in the scene!" + transform);
                Destroy(gameObject);
                return;
            }
            Instance = this;

            // Check if the audio events list is empty
            if (audioEventsList.Count == 0)
            {
                Debug.LogWarning("audioEventsList is empty! Please add audio events.");
            }
        }

        private void OnValidate()
        {
            // Clamp the index to the list size
            audioEventIndex = Mathf.Clamp(audioEventIndex, 0, audioEventsList.Count - 1);
        }
    }
}