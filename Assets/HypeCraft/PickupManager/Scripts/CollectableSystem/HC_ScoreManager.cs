namespace HC_Pickups
{
    using UnityEngine;

    /// <summary>
    /// HC_ScoreManager is responsible for managing the player's score in the game.
    /// It follows the Singleton pattern to ensure only one instance exists.
    /// The class provides methods to increment the score and update the HUD.
    /// </summary>
    public class HC_ScoreManager : MonoBehaviour
    {
        // Singleton instance of HC_ScoreManager
        public static HC_ScoreManager Instance { get; private set; }

        [Header("Decide on the amount of points")]
        // The amount of points to increment the score by
        [SerializeField] int scoreAmount = 1;

        // The current score of the player
        public int CurrentScore { get; private set; }

        private void Awake()
        {
            // Ensure only one instance of HC_ScoreManager exists, singleton pattern
            if (Instance != null)
            {
                Debug.LogError("There can only be one HC_ScoreManager in the scene!" + transform);
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        // Method to handle the collection of an object
        public void Collect(GameObject collectedObject)
        {
            // Increment the score by the predefined score amount
            IncrementScore(scoreAmount);
        }

        // Method to increment the score by a specified amount
        public void IncrementScore(int amount)
        {
            // Increment the current score by the specified amount
            CurrentScore += amount;

            // Update the score text in the HUD
            HC_HUDManager.Instance.UpdateScoreText(CurrentScore);
        }
    }
}
