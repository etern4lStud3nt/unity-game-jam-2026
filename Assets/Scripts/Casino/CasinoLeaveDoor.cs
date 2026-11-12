using System.Collections;
using UnityEngine;

// The casino's way out. The player arrives at its spawn point when they come in, and Interact here takes them back
// to the level door they came through. The casino scene stays loaded, so coming back is instant.
public class CasinoLeaveDoor : CasinoInteractable
{
    [Tooltip("Where the player stands when they arrive in the casino. Empty = this door's position.")]
    [SerializeField] private Transform spawnPoint;
    [SerializeField, Min(0f)] private float fadeTime = 0.3f;

    public Vector3 SpawnPosition => (spawnPoint != null ? spawnPoint : transform).position;

    protected override void Use(Rigidbody2D player)
    {
        if (!CasinoTravel.ReturnPosition.HasValue)
        {
            Debug.LogWarning("Casino leave door: the player did not come in through a CasinoDoor, so there is nowhere to go back to.", this);
            return;
        }
        StartCoroutine(Leave(player));
    }

    private IEnumerator Leave(Rigidbody2D player)
    {
        Busy = true;
        yield return CasinoTravel.Move(player, fadeTime, null, () => CasinoTravel.ReturnPosition);
        Busy = false;
    }
}
