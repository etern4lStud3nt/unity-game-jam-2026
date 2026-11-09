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
        Unlock(controller);
        ScreenMessage.Show(string.IsNullOrEmpty(title) ? DefaultTitle() : title, string.IsNullOrEmpty(hint) ? DefaultHint() : hint);
        EventBus.Publish(new AbilityUnlocked { Ability = ability });
        StartCoroutine(Pop());
    }

    private void Unlock(CharacterController controller)
    {
        switch (ability)
        {
            case PlayerAbility.Dash: controller.DashUnlocked = true; break;
            case PlayerAbility.WallJump: controller.WallJumpUnlocked = true; break;
            case PlayerAbility.DoubleJump: controller.DoubleJumpUnlocked = true; break;
        }
    }

    private string DefaultTitle()
    {
        switch (ability)
        {
            case PlayerAbility.WallJump: return "Wall Jump";
            case PlayerAbility.DoubleJump: return "Double Jump";
            default: return "Dash";
        }
    }

    private string DefaultHint()
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
