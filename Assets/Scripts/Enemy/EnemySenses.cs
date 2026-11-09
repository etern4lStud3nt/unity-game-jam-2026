using System;
using UnityEngine;

namespace Enemies
{
    // The enemy's eyes: finds the player and decides whether it can see them (range in front, a shorter range
    // behind, height limit, and no ground in between). EnemyBrain calls Refresh once per frame before the states run.
    public class EnemySenses : MonoBehaviour
    {
        [SerializeField] private string playerTag = "Player";
        [Tooltip("Layers that block sight (the Ground layer).")]
        [SerializeField] private LayerMask obstacleLayers;

        private EnemyStats stats;
        private EnemyMotor motor;
        private Collider2D body;
        private Transform player;
        private Collider2D playerCollider;
        private Health playerHealth;
        private Rigidbody2D playerBody;
        private float lastSeenTime = float.NegativeInfinity;
        private float nextSearchTime;

        // Raised once each time a player object is found, so the brain can set up collisions with it.
        public event Action<Transform> PlayerFound;

        public Transform Player => player;
        public bool HasPlayer => player != null && player.gameObject.activeInHierarchy && (playerHealth == null || !playerHealth.IsDead);
        public bool CanSeePlayer { get; private set; }
        public float TimeSinceSeen => Time.time - lastSeenTime;

        // Centre of the player's body (not the pivot, which can sit at the feet).
        public Vector2 PlayerPoint => playerCollider != null ? (Vector2)playerCollider.bounds.center : (Vector2)player.position;
        // The player's current velocity (for aiming ahead of them).
        public Vector2 PlayerVelocity => playerBody != null ? playerBody.linearVelocity : Vector2.zero;
        public int PlayerDirection => HasPlayer ? (PlayerPoint.x >= transform.position.x ? 1 : -1) : 0;
        public float HorizontalDistance => HasPlayer ? Mathf.Abs(PlayerPoint.x - transform.position.x) : float.PositiveInfinity;
        public float VerticalDistance => HasPlayer ? Mathf.Abs(PlayerPoint.y - Eye.y) : float.PositiveInfinity;

        // Sight starts at the centre of the enemy's own body.
        private Vector2 Eye => body != null ? (Vector2)body.bounds.center : (Vector2)transform.position;

        private void Reset()
        {
            obstacleLayers = LayerMask.GetMask("Ground");
        }

        public void Configure(EnemyStats enemyStats, EnemyMotor enemyMotor)
        {
            stats = enemyStats;
            motor = enemyMotor;
            body = GetComponent<Collider2D>();
        }

        // Counts as seeing the player right now, e.g. when hit from behind.
        public void Alert() => lastSeenTime = Time.time;

        public void Refresh()
        {
            if (player == null)
                FindPlayer();

            CanSeePlayer = HasPlayer && stats != null && LooksAtPlayer();
            if (CanSeePlayer)
                lastSeenTime = Time.time;
        }

        private bool LooksAtPlayer()
        {
            Vector2 target = PlayerPoint;
            float dx = target.x - transform.position.x;
            bool inFront = motor == null || Mathf.Sign(dx) == motor.Facing;
            float range = inFront ? stats.SightRange : stats.BackSightRange;
            if (Mathf.Abs(dx) > range || VerticalDistance > stats.VerticalSight)
                return false;

            return Physics2D.Linecast(Eye, target, obstacleLayers).collider == null;
        }

        private void FindPlayer()
        {
            // Searching by tag is cheap but not free; the player may also spawn later, so retry slowly.
            if (Time.time < nextSearchTime)
                return;
            nextSearchTime = Time.time + 0.5f;

            GameObject found = GameObject.FindWithTag(playerTag);
            if (found == null)
                return;

            player = found.transform;
            playerHealth = found.GetComponentInParent<Health>();
            playerBody = found.GetComponentInParent<Rigidbody2D>();
            playerCollider = null;
            foreach (Collider2D col in found.GetComponentsInChildren<Collider2D>())
            {
                if (col.isTrigger)
                    continue;
                playerCollider = col;
                break;
            }
            PlayerFound?.Invoke(player);
        }

        private void OnDrawGizmosSelected()
        {
            if (stats == null)
                return;
            int facing = motor != null ? motor.Facing : 1;
            Gizmos.color = CanSeePlayer ? Color.red : new Color(1f, 0.6f, 0f);
            Vector3 eye = Eye;
            Vector3 front = new Vector3(facing * stats.SightRange, 0f);
            Vector3 back = new Vector3(-facing * stats.BackSightRange, 0f);
            Vector3 up = new Vector3(0f, stats.VerticalSight);
            Gizmos.DrawLine(eye + back + up, eye + front + up);
            Gizmos.DrawLine(eye + back - up, eye + front - up);
            Gizmos.DrawLine(eye + front + up, eye + front - up);
            Gizmos.DrawLine(eye + back + up, eye + back - up);
            if (HasPlayer)
                Gizmos.DrawLine(eye, PlayerPoint);
        }
    }
}
