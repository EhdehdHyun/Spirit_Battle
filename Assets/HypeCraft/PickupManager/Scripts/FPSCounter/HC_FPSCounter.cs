using UnityEngine;
using TMPro;

namespace HC_Pickups
{
    public class HC_FPSCounter : MonoBehaviour
    {
        public TextMeshProUGUI fpsText; // Reference to the TextMeshProUGUI component to display FPS
        private float deltaTime; // Time between frames
        private float updateInterval = 0.5f; // Update interval in seconds
        private float timeSinceLastUpdate = 0f; // Time since the last update

        void Update()
        {
            // Calculate the time between frames
            deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;
            timeSinceLastUpdate += Time.unscaledDeltaTime;

            // Update the FPS display at the specified interval
            if (timeSinceLastUpdate >= updateInterval)
            {
                float fps = 1.0f / deltaTime; // Calculate FPS
                fpsText.text = Mathf.Ceil(fps).ToString() + " FPS"; // Update the text component with the FPS value
                timeSinceLastUpdate = 0f; // Reset the update timer
            }
        }
    }
}

