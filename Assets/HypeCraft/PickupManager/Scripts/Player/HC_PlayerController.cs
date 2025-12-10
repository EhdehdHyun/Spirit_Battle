namespace HC_Pickups
{
    using UnityEngine;

    // This class manages player movement and camera rotation in a Unity game.
    // It uses the CharacterController component to handle player movement and mouse input for camera rotation.
    [RequireComponent(typeof(CharacterController))]
    public class HC_PlayerController : MonoBehaviour
    {
        public static HC_PlayerController Instance { get; private set; } // Singleton instance

        [SerializeField] float movementSpeed = 5.0f;
        [SerializeField] float mouseSensitivity = 2.0f;

        float verticalRotation = 0.0f; // Vertical rotation angle
        CharacterController characterController; // Reference to the CharacterController component

        private void Awake()
        {
            // Singleton pattern to ensure only one instance exists
            if (Instance != null && Instance != this)
            {
                Debug.LogError("There can only be one HC_PlayerController in the scene!" + transform);
                Destroy(gameObject);
                return;
            }
            Instance = this;

            characterController = GetComponent<CharacterController>(); // Get the CharacterController component
        }

        void Start()
        {
            Cursor.lockState = CursorLockMode.Locked; // Lock the cursor to the center of the screen
        }

        void Update()
        {
            // Mouse look
            float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
            float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

            transform.Rotate(0, mouseX, 0); // Rotate the player horizontally

            verticalRotation -= mouseY;
            verticalRotation = Mathf.Clamp(verticalRotation, -90.0f, 90.0f); // Clamp vertical rotation
            Camera.main.transform.localRotation = Quaternion.Euler(verticalRotation, 0, 0); // Rotate the camera vertically

            // Movement
            float moveForward = Input.GetAxis("Vertical") * movementSpeed;
            float moveSide = Input.GetAxis("Horizontal") * movementSpeed;

            Vector3 movement = new Vector3(moveSide, 0, moveForward);
            movement = transform.rotation * movement; // Apply rotation to movement

            characterController.Move(movement * Time.deltaTime); // Move the player
        }
    }
}
