namespace HC_Pickups
{
    using UnityEngine;

    // This abstract class serves as a base for audio events in the game.
    // It defines a method that must be implemented by derived classes to play an audio event using an AudioSource.
    public abstract class HC_AudioEventBase : ScriptableObject
    {
        // Abstract method to play an audio event using the provided AudioSource.
        // Derived classes must implement this method to define how the audio event is played.
        public abstract void Play(AudioSource audioSource);
    }
}
