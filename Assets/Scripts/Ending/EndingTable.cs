using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

// The last roulette, in the black casino. Interact: the confession plays (a Yarn node), the big wheel pops up on
// screen and spins, and before it lands it shrinks away, the floor drops away and the player falls forever: lucky numbers and coins rush past, then
// only darkness, then the screen goes black and the wake-up scene loads. Input stays frozen from the spin on.
public class EndingTable : CasinoInteractable
{
    [Header("Confession")]
    [Tooltip("Yarn node played when the player uses the table. Empty = straight to the spin.")]
    [SerializeField] private string confessionNode = "Ending_Confession";

    [Header("Spin")]
    [Tooltip("The on-screen spin (a canvas with the big wheel). Hidden until the confession ends.")]
    [SerializeField] private GameObject spinScreen;
    [Tooltip("The big wheel on the spin screen. Its default state is the looping spin animation.")]
    [SerializeField] private Animator spinWheel;
    [Tooltip("Spin animation speed (1 = as the clip is made).")]
    [SerializeField, Min(0.01f)] private float spinSpeed = 1.25f;
    [Tooltip("How long the wheel spins before the floor gives way, in seconds.")]
    [SerializeField, Min(0f)] private float spinBeforeFall = 2.5f;
    [Tooltip("How long the wheel takes to pop in and to shrink away, in seconds.")]
    [SerializeField, Min(0.01f)] private float popTime = 0.2f;

    [Header("Floor gives way")]
    [Tooltip("Everything that disappears: the floor and the small wheel on it. Its colliders switch off and its sprites fade.")]
    [SerializeField] private GameObject floor;
    [Tooltip("How long the floor shakes before it goes, in seconds.")]
    [SerializeField, Min(0f)] private float shakeTime = 0.6f;
    [Tooltip("How far it shakes, in units.")]
    [SerializeField, Min(0f)] private float shakeStrength = 0.08f;
    [Tooltip("How long it takes to fade away, in seconds.")]
    [SerializeField, Min(0.01f)] private float vanishTime = 0.5f;

    [Header("Fall")]
    [Tooltip("Starts as soon as the ending scene opens and keeps playing into the wake-up scene (the AudioManager survives the load).")]
    [SerializeField] private MusicTrack fallMusic;
    [SerializeField, Min(0f)] private float musicFadeIn = 1f;
    [Tooltip("How long the fall lasts before the screen goes black, in seconds.")]
    [SerializeField, Min(0.1f)] private float fallTime = 8f;
    [Tooltip("The player turns slowly while falling, in degrees per second.")]
    [SerializeField] private float tumbleSpeed = 60f;
    [Tooltip("Fog that starts with the fall (thick fog left, right and behind the player).")]
    [SerializeField] private ParticleSystem[] fogOnFall;
    [Tooltip("Fog that stops when the fall starts (the fog over the platform, so it doesn't cover the player).")]
    [SerializeField] private ParticleSystem[] fogOffOnFall;
    [Tooltip("Share of the fall with things rushing past (0-1); the rest is darkness.")]
    [SerializeField, Range(0f, 1f)] private float debrisShare = 0.6f;
    [Tooltip("Seconds between two things rushing past.")]
    [SerializeField, Min(0.01f)] private float debrisInterval = 0.15f;
    [Tooltip("How far left and right of the player they appear, in units.")]
    [SerializeField, Min(0f)] private float debrisSpread = 8f;
    [Tooltip("Coins and chips that rush past.")]
    [SerializeField] private Sprite[] debrisSprites;
    [Tooltip("Size of a debris sprite, as a scale.")]
    [SerializeField, Min(0.01f)] private float debrisScale = 1.66f;
    [Tooltip("Font for the lucky numbers that rush past.")]
    [SerializeField] private TMP_FontAsset numberFont;
    [SerializeField] private Color numberColor = new(1f, 0.8f, 0.25f, 1f);
    [Tooltip("Numbers shown if the player has none (testing).")]
    [SerializeField] private int[] fallbackNumbers = { 7, 6, 15, 9, 5 };

    [Header("Wake up")]
    [Tooltip("Scene loaded after the fall. It must be in File > Build Profiles > Scene List.")]
    [SerializeField] private string wakeScene = "WakeUp";
    [SerializeField, Min(0f)] private float fadeOut = 2.5f;
    [Tooltip("Fade into the wake-up scene. 0 = it starts black and fades itself in.")]
    [SerializeField, Min(0f)] private float fadeIn = 0f;

    private bool talking;

    protected override void Start()
    {
        base.Start();
        if (spinScreen != null)
            spinScreen.SetActive(false);
        StartMusic();
    }

    private void StartMusic()
    {
        AudioManager audio = AudioManager.Instance;
        if (fallMusic == null || audio == null)
            return;
        // Nothing (a room, a boss) may take the music away from here on; the hold ends when this table is gone.
        audio.HoldMusic(this);
        audio.PlayMusic(fallMusic, musicFadeIn);
    }

    private void OnEnable()
    {
        EventBus.Subscribe<DialogueEnded>(OnDialogueEnded);
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        EventBus.Unsubscribe<DialogueEnded>(OnDialogueEnded);
    }

    private void OnDialogueEnded(DialogueEnded evt) => talking = false;

    protected override void Use(Rigidbody2D player)
    {
        StartCoroutine(Play(player));
    }

    private IEnumerator Play(Rigidbody2D player)
    {
        Busy = true;

        if (!string.IsNullOrEmpty(confessionNode))
        {
            talking = DialogueBridge.Run(confessionNode);
            while (talking)
                yield return null;
        }

        // From here on the player only watches.
        InputManager input = InputManager.Instance;
        if (input != null)
            input.BlockGameplay();

        if (spinScreen != null)
            spinScreen.SetActive(true);
        if (spinWheel != null)
        {
            spinWheel.Rebind();
            spinWheel.speed = spinSpeed;
        }
        yield return ScalePop.Run(spinWheel != null ? spinWheel.transform : null, 0f, 1f, popTime);
        yield return new WaitForSeconds(spinBeforeFall);

        // No result: the wheel shrinks away still spinning while the floor goes.
        StartCoroutine(HideWheel());
        yield return DropFloor();
        yield return Fall(player);

        SceneTransition.Load(wakeScene, 0f, fadeOut, fadeIn);
    }

    private IEnumerator HideWheel()
    {
        yield return ScalePop.Run(spinWheel != null ? spinWheel.transform : null, 1f, 0f, popTime);
        if (spinScreen != null)
            spinScreen.SetActive(false);
    }

    // Shakes the floor, switches its colliders off so the player drops, and fades it away.
    private IEnumerator DropFloor()
    {
        if (floor == null)
            yield break;

        Transform target = floor.transform;
        Vector3 home = target.localPosition;
        for (float t = 0f; t < shakeTime; t += Time.deltaTime)
        {
            target.localPosition = home + (Vector3)(Random.insideUnitCircle * shakeStrength);
            yield return null;
        }
        target.localPosition = home;

        foreach (Collider2D c in floor.GetComponentsInChildren<Collider2D>())
            c.enabled = false;
        StartCoroutine(Vanish());
    }

    private IEnumerator Vanish()
    {
        var sprites = new List<(SpriteRenderer r, Color c)>();
        foreach (SpriteRenderer r in floor.GetComponentsInChildren<SpriteRenderer>())
            sprites.Add((r, r.color));
        var maps = new List<(UnityEngine.Tilemaps.Tilemap m, Color c)>();
        foreach (UnityEngine.Tilemaps.Tilemap m in floor.GetComponentsInChildren<UnityEngine.Tilemaps.Tilemap>())
            maps.Add((m, m.color));

        for (float t = 0f; t < vanishTime; t += Time.deltaTime)
        {
            float a = 1f - t / vanishTime;
            foreach (var (r, c) in sprites)
                r.color = new Color(c.r, c.g, c.b, c.a * a);
            foreach (var (m, c) in maps)
                m.color = new Color(c.r, c.g, c.b, c.a * a);
            yield return null;
        }
        floor.SetActive(false);
    }

    // The player falls; numbers and coins appear below and rush past, then stop.
    private IEnumerator Fall(Rigidbody2D player)
    {
        var debris = new List<Transform>();
        float nextDebris = 0f;
        int[] numbers = Numbers();
        int nextNumber = 0;

        if (player != null)
        {
            // Smooth motion at this speed, so the following camera doesn't shake; the turn is done by physics too.
            player.interpolation = RigidbodyInterpolation2D.Interpolate;
            player.freezeRotation = false;
            player.angularDamping = 0f;
            player.angularVelocity = tumbleSpeed;
        }
        foreach (ParticleSystem fog in fogOffOnFall)
            if (fog != null)
                fog.Stop();
        foreach (ParticleSystem fog in fogOnFall)
            if (fog != null)
                fog.Play();

        for (float t = 0f; t < fallTime; t += Time.deltaTime)
        {
            if (player != null)
            {
                if (t < fallTime * debrisShare && t >= nextDebris)
                {
                    nextDebris = t + debrisInterval;
                    bool number = numbers.Length > 0 && Random.value < 0.45f;
                    Vector3 at = player.position + new Vector2(Random.Range(-debrisSpread, debrisSpread), -9f);
                    debris.Add(number ? MakeNumber(numbers[nextNumber++ % numbers.Length], at) : MakeSprite(at));
                }

                UpdateDebris(debris, player.position.y);
            }
            yield return null;
        }
    }

    // Turns the things rushing past and removes the ones that went out of sight above the player.
    private static void UpdateDebris(List<Transform> debris, float playerY)
    {
        for (int i = debris.Count - 1; i >= 0; i--)
        {
            Transform d = debris[i];
            if (d != null && d.position.y <= playerY + 10f)
            {
                d.Rotate(0f, 0f, 40f * Time.deltaTime);
                continue;
            }
            if (d != null)
                Destroy(d.gameObject);
            debris.RemoveAt(i);
        }
    }

    private int[] Numbers()
    {
        var list = new List<int>();
        foreach (LuckyNumberData data in LuckyNumbers.All)
            list.Add(data.Number);
        return list.Count > 0 ? list.ToArray() : fallbackNumbers;
    }

    private Transform MakeNumber(int value, Vector3 at)
    {
        var go = new GameObject("Falling Number " + value);
        go.transform.position = at;
        var text = go.AddComponent<TextMeshPro>();
        if (numberFont != null)
            text.font = numberFont;
        text.text = value.ToString();
        text.fontSize = Random.Range(5f, 9f);
        text.color = numberColor;
        text.alignment = TextAlignmentOptions.Center;
        text.sortingOrder = -2;
        return go.transform;
    }

    private Transform MakeSprite(Vector3 at)
    {
        var go = new GameObject("Falling Coin");
        go.transform.position = at;
        go.transform.localScale = Vector3.one * debrisScale;
        var r = go.AddComponent<SpriteRenderer>();
        if (debrisSprites != null && debrisSprites.Length > 0)
            r.sprite = debrisSprites[Random.Range(0, debrisSprites.Length)];
        r.sortingOrder = -2;
        return go.transform;
    }
}
