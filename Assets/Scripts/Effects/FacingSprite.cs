using UnityEngine;
using CharacterController = Assets.Scripts.CharacterController;

// Turns the character's sprite to the way it faces (the CharacterController's Facing). The art faces right.
[RequireComponent(typeof(SpriteRenderer))]
public class FacingSprite : MonoBehaviour
{
    [SerializeField] private CharacterController characterController;
    [Tooltip("Tick if the art faces left instead of right.")]
    [SerializeField] private bool artFacesLeft;

    private SpriteRenderer sprite;

    private void Awake()
    {
        sprite = GetComponent<SpriteRenderer>();
        if (characterController == null)
            characterController = GetComponentInParent<CharacterController>();
    }

    private void LateUpdate()
    {
        if (characterController != null)
            sprite.flipX = (characterController.Facing < 0f) != artFacesLeft;
    }
}
