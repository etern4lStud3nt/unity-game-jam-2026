using Enemies;
using UnityEngine;
using UnityEngine.InputSystem;

// LOCAL ONLY (listed in .git/info/exclude): stand-in for the player's attack while testing enemies.
// K = 1 damage to the enemy closest to the player, L = kill it.
public class EnemyDebugDamage : MonoBehaviour
{
    [SerializeField] private string playerTag = "Player";
    [SerializeField, Min(1)] private int damage = 1;

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
            return;

        if (keyboard.kKey.wasPressedThisFrame)
        {
            EnemyBrain target = Closest();
            if (target != null)
                target.Health.TakeDamage(damage);
        }

        if (keyboard.lKey.wasPressedThisFrame)
        {
            EnemyBrain target = Closest();
            if (target != null)
                target.Health.Kill();
        }
    }

    private EnemyBrain Closest()
    {
        GameObject player = GameObject.FindWithTag(playerTag);
        Vector3 from = player != null ? player.transform.position : Vector3.zero;

        EnemyBrain best = null;
        float bestDistance = float.PositiveInfinity;
        foreach (EnemyBrain enemy in FindObjectsByType<EnemyBrain>())
        {
            if (enemy.Health == null || enemy.Health.IsDead)
                continue;
            float distance = (enemy.transform.position - from).sqrMagnitude;
            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = enemy;
            }
        }
        return best;
    }

    private void OnGUI()
    {
        GUI.Label(new Rect(10, 10, 400, 22), "K: hit nearest enemy   L: kill nearest enemy");
    }
}
