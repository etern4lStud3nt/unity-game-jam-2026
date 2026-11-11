using UnityEngine;
using Yarn.Unity;

// Advances Yarn lines with the game's own keys: during a conversation InputManager turns Interact, Jump and Attack
// into Confirm. A press finishes the typing; the next press goes to the next line. Lives on the Dialogue prefab next
// to the LineAdvancer, whose Input Mode is set to Third-Party.
public class DialogueInput : MonoBehaviour, ILineAdvancerInput
{
    [SerializeField] private LineAdvancer lineAdvancer;

    public LineAdvancer LineAdvancer { get => lineAdvancer; set => lineAdvancer = value; }

    public void OnDialogueStarted()
    {
        if (InputManager.Instance != null)
            InputManager.Instance.Confirm += Advance;
        else
            Debug.LogWarning($"{name}: no InputManager in the scene; lines can't be advanced.", this);
    }

    public void OnDialogueComplete()
    {
        if (InputManager.Instance != null)
            InputManager.Instance.Confirm -= Advance;
    }

    private void OnDestroy() => OnDialogueComplete();

    private void Advance()
    {
        if (lineAdvancer == null)
            return;
        lineAdvancer.OnInputHurryUpLines();
        lineAdvancer.OnInputHurryUpOptions();
    }
}
