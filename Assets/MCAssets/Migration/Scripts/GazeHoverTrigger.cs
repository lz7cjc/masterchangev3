using UnityEngine;
using UnityEngine.Events;

// GazeReticlePointer calls OnGazeEnter() and OnGazeExit() on this component
// when its raycast hits or leaves a collider. ConstellationOrb subscribes to
// the events in Start() — no changes needed to GazeReticlePointer at all.
public class GazeHoverTrigger : MonoBehaviour
{
    public UnityEvent onEnter;
    public UnityEvent onExit;

    public void OnGazeEnter() => onEnter?.Invoke();
    public void OnGazeExit() => onExit?.Invoke();
}