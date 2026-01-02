namespace HC_Pickups
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;

    // This ScriptableObject manages rotation animations for objects in Unity.
    // It allows enabling/disabling rotation, selecting the rotation axis, and defining the rotation behavior using an AnimationCurve.
    // The rotation behavior can be applied to and stopped for target Transforms, and it uses coroutines to handle the rotation logic.
    // The rotation settings can be saved and updated in the Unity Editor.

    [CreateAssetMenu(fileName = "RotationSettings", menuName = "HC_ScriptableObjects/Settings/RotationSettings")]
    public class HC_RotationAnimationSettingsSO : HC_AnimationBehaviourSO
    {
        // Event triggered when the 'rotationOn' property changes
        public event Action<bool> OnRotationOnChanged;

        public string headerText = "Rotation Animation Settings";

        [Header("Rotation Animation Settings")]
        [Space]

        [Tooltip("Rotation On/Off  -  " + "Turn on rotation animation")]
        public bool rotationEnabled; // Toggle for enabling/disabling the rotation animation
        [Tooltip("You can modify how the object moves over time")]
        public AnimationCurve rotationCurve = AnimationCurve.Linear(0, 0, 1, 1); // Curve defining the rotation behavior over time
        [Space]

        [Tooltip("Select the axis of rotation")]
        public ChooseAxis rotationAxis = ChooseAxis.Y; // Defines the axis of rotation
        public enum ChooseAxis { X, Y, Z } // Enum for selecting rotation axis
        [Tooltip("Speed of the rotation animation")]
        [Range(0, 5)] public float rotationMovementSpeed = 1f;

        // Property with a getter and setter for 'rotationOn' with an event notification
        public bool RotationOn
        {
            get => rotationEnabled;
            set
            {
                if (rotationEnabled != value) // Check if the value has changed
                {
                    rotationEnabled = value;
                    OnRotationOnChanged?.Invoke(rotationEnabled); // Trigger the event
                }
            }
        }

        // Apply rotation behavior to a target Transform
        public override void ApplyBehavior(Transform target, Dictionary<Transform, Coroutine> coroutineDict, MonoBehaviour runner)
        {
            // Start the rotation coroutine if it's not already running
            if (!coroutineDict.ContainsKey(target))
            {
                coroutineDict[target] = runner.StartCoroutine(RotateObject(target, runner));
            }
        }

        // Stop rotation behavior for a target Transform
        public override void StopBehavior(Transform target, Dictionary<Transform, Coroutine> coroutineDict, MonoBehaviour runner)
        {
            if (coroutineDict.ContainsKey(target)) // Check if the rotation is active
            {
                runner.StopCoroutine(coroutineDict[target]); // Stop the running coroutine
                coroutineDict.Remove(target); // Remove the target from the dictionary
                target.localRotation = Quaternion.identity; // Reset the rotation to its default state
            }
        }

        // Coroutine that handles the rotation logic
        public IEnumerator RotateObject(Transform objTransform, MonoBehaviour runner)
        {
            float timer = 0f; // Timer to track the elapsed time

            while (true) // Infinite loop for continuous rotation
            {
                timer += Time.deltaTime * rotationMovementSpeed; // Increment the timer based on speed
                float curveValue = rotationCurve.Evaluate((timer % 1f)); // Use modulo to loop through the curve values

                float rotationValue = curveValue * 360f; // Map the curve value to a full rotation (360 degrees)
                switch (rotationAxis) // Apply rotation based on the chosen axis
                {
                    case ChooseAxis.X:
                        objTransform.localRotation = Quaternion.Euler(rotationValue, 0, 0);
                        break;
                    case ChooseAxis.Y:
                        objTransform.localRotation = Quaternion.Euler(0, rotationValue, 0);
                        break;
                    case ChooseAxis.Z:
                        objTransform.localRotation = Quaternion.Euler(0, 0, rotationValue);
                        break;
                }

                yield return null; // Wait for the next frame
            }
        }

        private void OnDisable()
        {
            OnRotationOnChanged = null; // Unsubscribe from the event
        }
    }
}
