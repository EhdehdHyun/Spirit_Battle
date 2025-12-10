namespace HC_Pickups
{
    using UnityEngine;
    using UnityEditor;

    // This class defines an audio event that can be played in the game.
    // It allows for randomization of volume and pitch, and provides editor functionality for previewing the audio.
    [CreateAssetMenu(fileName = "AudioEvent", menuName = "HC_ScriptableObjects/Events/AudioEvent", order = 2)]
    public class HC_AudioEvent : HC_AudioEventBase
    {
        [Header("Add Audio Clips")]
        [SerializeField] AudioClip[] audioClips;

        [Header("Volume Range: 0 - 100%")]
        [Tooltip("Controls the volume of the audio")]
        [Range(0, 100), SerializeField] private float volume = 100;

        [Tooltip("Adjust the volume variation (0 - 100%)")]
        [Range(0, 100), SerializeField] private float randomizeVolume = 0;

        [Header("Pitch Range: 0 - 3")]
        [Tooltip("Control the pitch of the audio")]
        [Range(0, 3), SerializeField] private float pitch = 1;

        [Tooltip("Adjust the pitch variation")]
        [Range(0, 3), SerializeField] private float randomizePitch = 0;

#if UNITY_EDITOR
        private AudioSource previewSource;
#endif

        // Public properties for accessing private fields
        public AudioClip[] AudioClips => audioClips;
        public float Volume => volume;
        public float RandomizeVolume => randomizeVolume;
        public float Pitch => pitch;
        public float RandomizePitch => randomizePitch;

        public override void Play(AudioSource audioSource)
        {
            if (audioSource == null)
            {
                Debug.LogError("audioSource is null in PU_AudioEvent.Play.");
                return;
            }

            if (audioClips == null || audioClips.Length == 0)
            {
                Debug.LogError("audioClips array is null or empty in PU_AudioEvent.Play.");
                return;
            }

            // Select a random audio clip from the array
            audioSource.clip = audioClips[Random.Range(0, audioClips.Length)];

            // Apply volume and pitch with random variations
            audioSource.volume = (volume / 100f) * (1 + Random.Range(-randomizeVolume / 200f, randomizeVolume / 200f));
            audioSource.pitch = pitch * (1 + Random.Range(-randomizePitch / 2f, randomizePitch / 2f));

            // Play the selected audio clip
            audioSource.PlayOneShot(audioSource.clip, volume / 100f);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (previewSource != null) Play(previewSource);
        }

        private void OnEnable()
        {
            previewSource = EditorUtility.CreateGameObjectWithHideFlags("Audio Preview", HideFlags.HideAndDontSave, typeof(AudioSource)).GetComponent<AudioSource>();
        }

        public void PlayFromEditor()
        {
            Play(previewSource);
        }

        public void StopFromEditor()
        {
            previewSource.Stop();
        }

        private void OnDisable()
        {
            DestroyImmediate(previewSource.gameObject);
        }
#endif
    }
}
