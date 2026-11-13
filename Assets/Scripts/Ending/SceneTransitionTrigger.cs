using UnityEngine;

// Calls SceneTransition from a UnityEvent, e.g. a lucky number pickup's Picked Up (Mother's 9 -> the ending).
public class SceneTransitionTrigger : MonoBehaviour
{
    [Tooltip("Scene to load. It must be in File > Build Profiles > Scene List.")]
    [SerializeField] private string scene = "Ending";
    [Tooltip("Wait before the screen starts going black, in seconds (lets a pickup message show).")]
    [SerializeField, Min(0f)] private float delay = 1.5f;
    [SerializeField, Min(0f)] private float fadeOut = 1f;
    [SerializeField, Min(0f)] private float fadeIn = 1f;

    public void Go() => SceneTransition.Load(scene, delay, fadeOut, fadeIn);
}
