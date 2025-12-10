namespace HC_Pickups
{
    using UnityEngine;
    using TMPro;

    /// <summary>
    /// HC_HUDManager is responsible for managing the Heads-Up Display (HUD) in the game.
    /// It follows the Singleton pattern to ensure only one instance exists.
    /// The class provides methods to update the score text on the HUD.
    /// </summary>
    public class HC_HUDManager : MonoBehaviour
    {
        // Singleton instance of HC_HUDManager
        public static HC_HUDManager Instance { get; private set; }

        [Header("Show score text in the inspector")]
        // Reference to the TMP_Text component that displays the score
        [SerializeField] TMP_Text scoreText;

        private void Awake()
        {
            // Ensure only one instance of HC_HUDManager exists
            if (Instance != null)
            {
                Debug.LogError("There can only be one HC_HUDManager in the scene!" + transform);
                Destroy(gameObject);
                return;
            }
            Instance = this;

            // Find the child object named "Collectable UI"
            Transform childTransform = transform.Find("Collectable UI");
            if (childTransform != null)
            {
                // Find the "Score Text" object within the "Collectable UI" child object
                Transform scoreTextTransform = childTransform.Find("Score Text");
                if (scoreTextTransform != null)
                {
                    // Get the TMP_Text component from the "Score Text" object
                    scoreText = scoreTextTransform.GetComponent<TMP_Text>();
                }
                else
                {
                    Debug.LogError("Score Text not found in the child object!");
                }
            }
            else
            {
                Debug.LogError("Collectable UI not found in the hierarchy!");
            }
        }

        // Method to update the score text on the HUD
        public void UpdateScoreText(int scoreAmount)
        {
            // Set the score text to the provided score amount
            scoreText.text = scoreAmount.ToString();
        }
    }
}
