using System.Collections;
using UnityEngine;
using CharacterController = Assets.Scripts.CharacterController;

public enum PlayerAbility
{
    Dash,
    WallJump,
    DoubleJump,
}

// An ability lying in the level. Touching it unlocks the ability on the player's CharacterController for good
// (it survives death), shows its name and key on screen, and publishes AbilityUnlocked.
// Lock the abilities on the level's player (CharacterController > Abilities) so the pickups mean something.
[RequireComponent(typeof(Collider2D))]
public class AbilityPickup : MonoBehaviour
{
    [SerializeField] private PlayerAbility ability;
    [Tooltip("Shown on screen. Empty = the ability's name.")]
    [SerializeField] private string title;
    [Tooltip("Second line on screen, usually the key. Empty = the default for the ability.")]
    [SerializeField] private string hint;
    [SerializeField] private string playerTag = "Player";
    [SerializeField, Min(0.01f)] private float popTime = 0.3f;

    private bool taken;

    private void Reset()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (taken)
            return;
        Rigidbody2D body = other.attachedRigidbody;
        if (body == null || !(other.CompareTag(playerTag) || body.CompareTag(playerTag)))
            return;
        if (!body.TryGetComponent(out CharacterController controller))
            return;

        taken = true;
        Grant(controller, ability, title, hint);
        StartCoroutine(Pop());
    }

    // Unlocks the ability on the player, shows its name and key on screen and publishes AbilityUnlocked. Used by the
    // pickups and by anything else that gives an ability (a Yarn command, a casino store...). Empty title or hint =
    // the ability's defaults.
    public static void Grant(CharacterController controller, PlayerAbility ability, string title = null, string hint = null)
    {
        controller.Unlock(ability);
        ScreenMessage.Show(string.IsNullOrEmpty(title) ? DefaultTitle(ability) : title,
            string.IsNullOrEmpty(hint) ? DefaultHint(ability) : hint);
        EventBus.Publish(new AbilityUnlocked { Ability = ability });
    }

    private static string DefaultTitle(PlayerAbility ability)
    {
        switch (ability)
        {
            case PlayerAbility.WallJump: return "Wall Jump";
            case PlayerAbility.DoubleJump: return "Double Jump";
            default: return "Dash";
        }
    }

    private static string DefaultHint(PlayerAbility ability)
    {
        switch (ability)
        {
            case PlayerAbility.WallJump: return "Space on a wall";
            case PlayerAbility.DoubleJump: return "Space in the air";
            default: return "Shift";
        }
    }

    // Swells and fades, then disappears.
    private IEnumerator Pop()
    {
        GetComponent<Collider2D>().enabled = false;
        var sprite = GetComponent<SpriteRenderer>();
        Vector3 scale = transform.localScale;
        Color color = sprite != null ? sprite.color : Color.white;

        for (float t = 0f; t < popTime; t += Time.deltaTime)
        {
            float k = t / popTime;
            transform.localScale = scale * (1f + 0.6f * k);
            if (sprite != null)
                sprite.color = new Color(color.r, color.g, color.b, color.a * (1f - k));
            yield return null;
        }
        gameObject.SetActive(false);
    }
}
