namespace HC_Pickups
{
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// HC_AnimationBehaviourSO is an abstract base class for defining animation behaviors in Unity.
    /// It is designed to be inherited by ScriptableObjects that implement specific animation logic, 
    /// such as floating or rotating objects. This class provides a structure for applying and stopping 
    /// animation behaviors on target Transforms using coroutines.
    /// 
    /// Key Features:
    /// - Abstract methods for applying and stopping animation behaviors.
    /// - Supports coroutine-based animations by passing a dictionary to manage active coroutines.
    /// - Designed to be extended for custom animation behaviors.
    /// 
    /// Usage:
    /// - Create a new ScriptableObject that inherits from HC_AnimationBehaviourSO.
    /// - Implement the ApplyBehavior and StopBehavior methods to define the animation logic.
    /// - Use these ScriptableObjects in conjunction with MonoBehaviours to control animations dynamically.
    /// </summary>
    public abstract class HC_AnimationBehaviourSO : ScriptableObject
    {
        /// <summary>
        /// Applies the animation behavior to the specified target Transform.
        /// This method is intended to be overridden in derived classes to define custom animation logic.
        /// </summary>
        /// <param name="target">The Transform to which the animation behavior will be applied.</param>
        /// <param name="coroutineDict">A dictionary to manage active coroutines for the target Transform.</param>
        /// <param name="runner">The MonoBehaviour instance used to start coroutines.</param>
        public abstract void ApplyBehavior(Transform target, Dictionary<Transform, Coroutine> coroutineDict, MonoBehaviour runner);

        /// <summary>
        /// Stops the animation behavior for the specified target Transform.
        /// This method is intended to be overridden in derived classes to define logic for stopping animations.
        /// </summary>
        /// <param name="target">The Transform for which the animation behavior will be stopped.</param>
        /// <param name="coroutineDict">A dictionary to manage active coroutines for the target Transform.</param>
        /// <param name="runner">The MonoBehaviour instance used to stop coroutines.</param>
        public abstract void StopBehavior(Transform target, Dictionary<Transform, Coroutine> coroutineDict, MonoBehaviour runner);
    }
}
