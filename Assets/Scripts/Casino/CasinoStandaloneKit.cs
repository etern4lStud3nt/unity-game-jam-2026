using UnityEngine;
using UnityEngine.SceneManagement;

// Lets the casino scene be played on its own. The player, cameras, input and HUD listed here are saved inactive and
// switched on only when the casino is the scene that was opened (Play on Casino.unity). When a CasinoDoor loads the
// casino on top of the level, they stay off and the level's own player and camera are used.
[DefaultExecutionOrder(-1000)]
public class CasinoStandaloneKit : MonoBehaviour
{
    [Tooltip("Root objects that make the scene playable (player, cameras, Input Manager, HUD...). Keep them inactive in the scene.")]
    [SerializeField] private GameObject[] kit;
    [Tooltip("The player's body. Moved to the leave door's spawn point at the start.")]
    [SerializeField] private Rigidbody2D player;
    [Tooltip("Lucky numbers given at the start when the casino is played on its own, to try the table.")]
    [SerializeField] private LuckyNumberData[] testLuckyNumbers;

    private void Awake()
    {
        // Loaded on top of the level by a CasinoDoor: the level is the active scene and brings everything.
        if (gameObject.scene != SceneManager.GetActiveScene())
            return;

        CasinoLeaveDoor door = FindAnyObjectByType<CasinoLeaveDoor>();
        if (player != null && door != null)
            player.transform.position = door.SpawnPosition;

        foreach (GameObject item in kit)
            if (item != null)
                item.SetActive(true);

        foreach (LuckyNumberData number in testLuckyNumbers)
            if (number != null)
                number.Grant();
    }
}
