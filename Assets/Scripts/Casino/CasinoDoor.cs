using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// The door into the casino, placed in the level. Interact in front of it: the screen goes black, the casino scene is
// loaded on top of the level (only the first time) and the player comes out at the casino's leave door. Leaving the
// casino brings the player back here.
public class CasinoDoor : CasinoInteractable
{
    [Tooltip("Name of the casino scene. It must be in File > Build Profiles > Scene List.")]
    [SerializeField] private string casinoScene = "Casino";
    [Tooltip("Where the player stands when they come back out. Empty = this door's position.")]
    [SerializeField] private Transform returnPoint;
    [SerializeField, Min(0f)] private float fadeTime = 0.3f;
    [Tooltip("Arrow above the entrance while the player has an unplayed lucky number. Hidden in interaction range.")]
    [SerializeField] private GameObject marker;

    protected override void Update()
    {
        base.Update();
        if (marker != null)
            marker.SetActive(Player == null && !Busy && CasinoTable.HasNumberToPlay);
    }

    protected override void Use(Rigidbody2D player)
    {
        StartCoroutine(Enter(player));
    }

    // Also used by SaveGame to begin a new game inside this casino, with a working return door.
    public IEnumerator Enter(Rigidbody2D player)
    {
        Busy = true;
        CasinoTravel.ReturnPosition = (returnPoint != null ? returnPoint : transform).position;
        yield return CasinoTravel.Move(player, fadeTime, LoadCasino(), CasinoSpawn);
        Busy = false;
    }

    private IEnumerator LoadCasino()
    {
        if (SceneManager.GetSceneByName(casinoScene).isLoaded)
            yield break;
        AsyncOperation loading = SceneManager.LoadSceneAsync(casinoScene, LoadSceneMode.Additive);
        if (loading == null)
        {
            Debug.LogError($"Casino door: scene \"{casinoScene}\" could not be loaded. Is it in the Build Profiles scene list?", this);
            yield break;
        }
        while (!loading.isDone)
            yield return null;
    }

    private static Vector3? CasinoSpawn()
    {
        CasinoLeaveDoor door = FindAnyObjectByType<CasinoLeaveDoor>();
        if (door == null)
        {
            Debug.LogError("Casino door: the casino scene has no CasinoLeaveDoor to arrive at.");
            return null;
        }
        return door.SpawnPosition;
    }
}
