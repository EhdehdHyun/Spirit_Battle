namespace HC_Pickups
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;

    // This ScriptableObject manages floating animations for objects in Unity.
    // It allows enabling/disabling floating, setting the base height, speed, and distance of the floating animation.
    // The floating behavior can be applied to and stopped for target Transforms, and it uses coroutines to handle the floating logic.
    // The floating settings can be saved and updated in the Unity Editor.

    [CreateAssetMenu(fileName = "FloatSettings", menuName = "HC_ScriptableObjects/Settings/FloatSettings")]
    public class HC_FloatAnimationSettingsSO : HC_AnimationBehaviourSO
    {
        public string headerText = "Float Animation Settings";

        [Header("Float Animation Settings")]
        [Space]

        [Tooltip("Float On/Off  -  " + "Turn on float animation")]
        public bool floatEnabled; // Toggle for enabling/disabling the floating animation
        public bool previousFloatEnabled; // Stores the previous state of floatEnabled
        [Tooltip("You can modify how the object moves over time")]
        public AnimationCurve floatCurve = AnimationCurve.Linear(0, 0, 1, 1); // Curve defining the floating movement over time
        [Space]

        [Tooltip("Starting height for the floating animation")]
        [Range(0, 10)] public float floatBaseHeight = 0f;
        [Tooltip("Speed of the floating animation")]
        [Range(0.1f, 10f)] public float floatMovementSpeed = 1f;
        [Tooltip("Distance the object moves while floating")]
        [Range(0, 10)] public float floatDistance = 1f;

        private Dictionary<Transform, CoroutineInfo> activeCoroutines = new Dictionary<Transform, CoroutineInfo>();

        private void OnValidate()
        {
            // Ensure that floatMovementSpeed is never less than 0.01
            if (floatMovementSpeed < 0.1f)
            {
                floatMovementSpeed = 0.1f;
            }

            // Ensure the values are updated in all active coroutines
            foreach (var entry in activeCoroutines)
            {
                Transform target = entry.Key;
                CoroutineInfo coroutineInfo = entry.Value;
                MonoBehaviour runner = coroutineInfo.Runner;

                if (runner != null)
                {
                    // Stop the existing coroutine
                    runner.StopCoroutine(coroutineInfo.Coroutine);

                    // Restart the coroutine with updated values
                    coroutineInfo.Coroutine = runner.StartCoroutine(FloatObject(target, runner, coroutineInfo.InitialPosition));
                }
            }

            // Save changes to ensure they persist in the editor
            SaveChanges();
        }

        public void SaveChanges()
        {
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this); // Mark the object as dirty to ensure changes are saved
#endif
        }

        // Apply floating behavior to a target Transform
        public override void ApplyBehavior(Transform target, Dictionary<Transform, Coroutine> coroutineDict, MonoBehaviour runner)
        {
            if (!coroutineDict.ContainsKey(target)) // Check if the floating coroutine is not already running
            {
                Vector3 initialPosition = target.position; // Store the initial position of the target
                Coroutine coroutine = runner.StartCoroutine(FloatObject(target, runner, initialPosition)); // Start the floating coroutine
                coroutineDict[target] = coroutine;
                activeCoroutines[target] = new CoroutineInfo { Coroutine = coroutine, Runner = runner, InitialPosition = initialPosition };
            }
        }

        // Stop floating behavior for a target Transform
        public override void StopBehavior(Transform target, Dictionary<Transform, Coroutine> coroutineDict, MonoBehaviour runner)
        {
            if (coroutineDict.ContainsKey(target)) // Check if the floating coroutine is active
            {
                runner.StopCoroutine(coroutineDict[target]); // Stop the running coroutine
                coroutineDict.Remove(target); // Remove the target from the dictionary
                activeCoroutines.Remove(target); // Remove the target from the active coroutines dictionary
            }
        }

        // Coroutine to make the object float up and down
        public IEnumerator FloatObject(Transform target, MonoBehaviour runner, Vector3 initialPosition)
        {
            while (true) // Loop to alternate between moving up and down
            {
                // Define the starting position and target position for the floating animation
                Vector3 startPosition = initialPosition + Vector3.up * floatBaseHeight;
                Vector3 targetPosition = startPosition + Vector3.up * floatDistance;

                // Move upwards
                yield return AnimatePosition(target, startPosition, targetPosition, runner);

                // Move downwards
                yield return AnimatePosition(target, targetPosition, startPosition, runner);
            }
        }

        // Coroutine to animate the position of the object between two points
        private IEnumerator AnimatePosition(Transform target, Vector3 start, Vector3 end, MonoBehaviour runner)
        {
            float time = 0f; // Timer to track animation progress
            float movementDuration = 1f / floatMovementSpeed; // Duration of movement is the opposite of speed, i.e. higher speed, shorter duration

            while (time < movementDuration) // Animate while time is within the duration of the movement
            {
                float normalizedTime = time / movementDuration; // Normalize the time to a range of 0 to 1
                float curveValue = floatCurve.Evaluate(normalizedTime); // Evaluate the animation curve
                target.position = Vector3.Lerp(start, end, curveValue); // Interpolate position using the curve value
                time += Time.deltaTime; // Increment the timer
                yield return null; // Wait for the next frame
            }

            // Ensure the object reaches the exact endpoint at the end of the animation
            target.position = end;
        }

        // Class to store Coroutine and MonoBehaviour instance
        private class CoroutineInfo
        {
            public Coroutine Coroutine;
            public MonoBehaviour Runner;
            public Vector3 InitialPosition;
        }
    }
}
