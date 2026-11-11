using UnityEngine;
using Yarn.Unity;

// Connects Yarn Spinner's DialogueRunner to the game. Anything can start a node with DialogueBridge.Run("Node")
// without a scene reference, and every conversation goes out as DialogueStarted / DialogueEnded on the EventBus
// (InputManager blocks gameplay input while one runs). Lives on the Dialogue prefab next to the DialogueRunner.
[RequireComponent(typeof(DialogueRunner))]
public class DialogueBridge : MonoBehaviour
{
    private static DialogueBridge instance;

    private DialogueRunner runner;
    private string node;

    // Domain reload is off, so the static must be cleared by hand between Play sessions.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => instance = null;

    // Starts a node of the Yarn project. False if there is no Dialogue prefab in the scene, a conversation is
    // already running, or the node doesn't exist.
    public static bool Run(string nodeName)
    {
        if (instance == null)
            instance = FindAnyObjectByType<DialogueBridge>();
        if (instance == null)
        {
            Debug.LogWarning($"DialogueBridge.Run(\"{nodeName}\"): no Dialogue prefab in the scene.");
            return false;
        }
        return instance.Begin(nodeName);
    }

    private void Awake()
    {
        instance = this;
        runner = GetComponent<DialogueRunner>();
        runner.onDialogueStart?.AddListener(OnStarted);
        runner.onDialogueComplete?.AddListener(OnCompleted);
    }

    private void OnDestroy()
    {
        runner.onDialogueStart?.RemoveListener(OnStarted);
        runner.onDialogueComplete?.RemoveListener(OnCompleted);
    }

    private bool Begin(string nodeName)
    {
        if (runner.IsDialogueRunning)
            return false;
        if (runner.YarnProject == null || System.Array.IndexOf(runner.YarnProject.NodeNames, nodeName) < 0)
        {
            Debug.LogWarning($"{name}: the Yarn project has no node \"{nodeName}\".", this);
            return false;
        }
        node = nodeName;
        runner.StartDialogue(nodeName).Forget();
        return true;
    }

    private void OnStarted() => EventBus.Publish(new DialogueStarted { Node = node });

    private void OnCompleted() => EventBus.Publish(new DialogueEnded());
}
