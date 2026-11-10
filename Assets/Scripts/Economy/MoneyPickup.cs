using UnityEngine;

// A coin (or pile of money) the player collects by touching it. Works lying in the level (no Rigidbody2D) or
// dropped by an enemy (with a Rigidbody2D so it bursts out and falls; see EnemyMoneyDrop).
// Needs a trigger collider on this object for the pickup area.
public class MoneyPickup : MonoBehaviour
{
    [SerializeField, Min(1)] private int value = 1;
    [SerializeField] private SoundData pickupSound;
    [SerializeField] private string playerTag = "Player";
    [Tooltip("Seconds after spawning before it can be picked up, so dropped coins visibly burst out first.")]
    [SerializeField, Min(0f)] private float pickupDelay;

    private float collectableAt;
    private bool collected;

    public int Value { get => value; set => this.value = Mathf.Max(1, value); }

    private void OnEnable()
    {
        collectableAt = Time.time + pickupDelay;
    }

    private void OnTriggerEnter2D(Collider2D other) => TryCollect(other);

    private void OnTriggerStay2D(Collider2D other) => TryCollect(other);

    private void TryCollect(Collider2D other)
    {
        if (collected || Time.time < collectableAt)
            return;
        Rigidbody2D body = other.attachedRigidbody;
        if (!(other.CompareTag(playerTag) || (body != null && body.CompareTag(playerTag))))
            return;

        collected = true;
        MoneyWallet.Add(value);
        if (pickupSound != null && AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(pickupSound, transform.position);
        Destroy(gameObject);
    }
}
