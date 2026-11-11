using UnityEngine;
using UnityEngine.Events;

// Runs On Signal when a Yarn script says <<signal Name>> with this Signal Name, e.g. <<signal open_gate>> opens a
// gate set up in the inspector. Put it on an active object; the objects it changes can be inactive.
public class DialogueSignalListener : MonoBehaviour
{
    [Tooltip("The name after <<signal ...>> in the Yarn script.")]
    [SerializeField] private string signalName;
    [Tooltip("Only react the first time.")]
    [SerializeField] private bool once;
    [SerializeField] private UnityEvent onSignal;

    private bool fired;

    private void OnEnable() => EventBus.Subscribe<DialogueSignal>(OnSignal);

    private void OnDisable() => EventBus.Unsubscribe<DialogueSignal>(OnSignal);

    private void OnSignal(DialogueSignal evt)
    {
        if (evt.Name != signalName || (once && fired))
            return;
        fired = true;
        onSignal.Invoke();
    }
}
