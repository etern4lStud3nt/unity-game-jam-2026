using Enemies;
using UnityEngine;

// Scatters coins when the enemy on this object dies. Each coin is a MoneyPickup prefab with a Rigidbody2D;
// it bursts upwards, falls to the ground and waits there to be collected. The player walks through the coins
// (collisions with the player are ignored); only the pickup trigger touches them.
[RequireComponent(typeof(EnemyBrain))]
public class EnemyMoneyDrop : MonoBehaviour
{
    [Tooltip("Total money this enemy drops.")]
    [SerializeField, Min(0)] private int amount = 3;
    [Tooltip("Money per coin; the amount is split into coins of this value (the last one takes the rest).")]
    [SerializeField, Min(1)] private int coinValue = 1;
    [SerializeField] private MoneyPickup coinPrefab;
    [Tooltip("Upward launch speed range of the coins.")]
    [SerializeField] private Vector2 launchUp = new Vector2(6f, 10f);
    [Tooltip("Sideways launch speed range (either direction).")]
    [SerializeField] private float launchSide = 3f;
    [SerializeField] private string playerTag = "Player";

    private EnemyBrain brain;

    private void Awake() => brain = GetComponent<EnemyBrain>();

    private void OnEnable() => brain.Died += OnDied;

    private void OnDisable() => brain.Died -= OnDied;

    private void OnDied(EnemyBrain enemy)
    {
        if (coinPrefab == null || amount <= 0)
            return;

        GameObject player = GameObject.FindWithTag(playerTag);
        Collider2D[] playerColliders = player != null ? player.GetComponentsInChildren<Collider2D>() : new Collider2D[0];
        Vector3 origin = transform.position;

        for (int left = amount; left > 0; left -= coinValue)
        {
            MoneyPickup coin = Instantiate(coinPrefab, origin, Quaternion.identity);
            coin.Value = Mathf.Min(coinValue, left);

            if (coin.TryGetComponent(out Rigidbody2D body))
                body.linearVelocity = new Vector2(Random.Range(-launchSide, launchSide), Random.Range(launchUp.x, launchUp.y));

            // Only the solid collider ignores the player; the pickup trigger must still see them.
            PlayerPassThrough.Ignore(coin.GetComponentsInChildren<Collider2D>(), playerColliders);
        }
    }
}
