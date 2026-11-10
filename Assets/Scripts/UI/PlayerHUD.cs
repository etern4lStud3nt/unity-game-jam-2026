using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Top-left HUD: one icon per point of health (Hollow Knight masks) and the player's money underneath.
// Fed only by the EventBus (PlayerHealthChanged, MoneyChanged), so it never looks for the player or the wallet.
// Lives on the HUD prefab; layout and sizes are set on the prefab's objects (the Health row's layout group, the
// icon template, the texts), sprites, colours and timings here.
public class PlayerHUD : MonoBehaviour
{
    [Header("Parts (on the HUD prefab)")]
    [Tooltip("Row the health icons are created in. Give it a Horizontal Layout Group for spacing.")]
    [SerializeField] private RectTransform healthRow;
    [Tooltip("One health icon; copied once per point of max health.")]
    [SerializeField] private Image healthIconPrefab;
    [SerializeField] private TextMeshProUGUI moneyText;
    [Tooltip("The short \"+5\" next to the money.")]
    [SerializeField] private TextMeshProUGUI gainText;

    [Header("Health look")]
    [Tooltip("Sprite of a full health icon. Empty = keep the template's sprite.")]
    [SerializeField] private Sprite fullSprite;
    [Tooltip("Sprite of a lost health icon (a cracked mask...). Empty = the full sprite tinted with Empty Color.")]
    [SerializeField] private Sprite emptySprite;
    [Tooltip("Tint of full icons. White = the sprite's own colours.")]
    [SerializeField] private Color fullColor = new Color(0.93f, 0.93f, 0.96f);
    [SerializeField] private Color emptyColor = new Color(0.2f, 0.2f, 0.25f, 0.6f);
    [SerializeField] private Color lostFlashColor = Color.white;
    [SerializeField, Min(0f)] private float loseShakeTime = 0.25f;
    [SerializeField, Min(0f)] private float loseShakeAmount = 4f;

    [Header("Money")]
    [Tooltip("Seconds the number takes to count to a new value.")]
    [SerializeField, Min(0f)] private float countTime = 0.4f;
    [Tooltip("Seconds the +N stays on screen.")]
    [SerializeField, Min(0.01f)] private float gainTime = 0.9f;
    [Tooltip("Pixels the +N drifts up while fading.")]
    [SerializeField] private float gainRise = 10f;

    private readonly List<Image> icons = new List<Image>();
    private int shownHealth;
    private int shownMoney;
    private int pendingGain;
    private Vector2 gainHome;
    private Coroutine countRoutine;
    private Coroutine gainRoutine;

    private void Awake()
    {
        if (healthIconPrefab != null)
            healthIconPrefab.gameObject.SetActive(false);
        if (gainText != null)
        {
            gainHome = gainText.rectTransform.anchoredPosition;
            gainText.alpha = 0f;
        }
    }

    // Subscribed in OnEnable, before PlayerHealthEvents sends the starting health in its Start.
    private void OnEnable()
    {
        EventBus.Subscribe<PlayerHealthChanged>(OnHealthChanged);
        EventBus.Subscribe<MoneyChanged>(OnMoneyChanged);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<PlayerHealthChanged>(OnHealthChanged);
        EventBus.Unsubscribe<MoneyChanged>(OnMoneyChanged);
    }

    private void Start()
    {
        shownMoney = MoneyWallet.Balance;
        if (moneyText != null)
            moneyText.text = shownMoney.ToString();
    }

    private void OnHealthChanged(PlayerHealthChanged evt)
    {
        bool rebuilt = evt.Max != icons.Count;
        if (rebuilt)
            RebuildIcons(evt.Max);
        // Only animate real losses, not the first value or a rebuild.
        SetHealth(evt.Current, !rebuilt && evt.Delta < 0);
    }

    private void RebuildIcons(int max)
    {
        foreach (Image icon in icons)
            Destroy(icon.gameObject);
        icons.Clear();
        if (healthIconPrefab == null || healthRow == null)
            return;
        for (int i = 0; i < max; i++)
        {
            Image icon = Instantiate(healthIconPrefab, healthRow);
            icon.name = "Health " + (i + 1);
            if (fullSprite != null)
                icon.sprite = fullSprite;
            icon.gameObject.SetActive(true);
            icons.Add(icon);
        }
        shownHealth = max;
    }

    private void SetHealth(int current, bool animate)
    {
        for (int i = 0; i < icons.Count; i++)
        {
            bool full = i < current;
            if (animate && !full && i < shownHealth)
                StartCoroutine(LoseIcon(icons[i]));
            else
                ShowIcon(icons[i], full);
        }
        shownHealth = current;
    }

    // The lost icon flashes and shakes for a moment, then empties.
    private IEnumerator LoseIcon(Image icon)
    {
        RectTransform rect = icon.rectTransform;
        Vector2 home = rect.anchoredPosition;
        icon.color = lostFlashColor;
        for (float t = 0f; t < loseShakeTime; t += Time.unscaledDeltaTime)
        {
            rect.anchoredPosition = home + Random.insideUnitCircle * loseShakeAmount;
            yield return null;
        }
        rect.anchoredPosition = home;
        ShowIcon(icon, false);
    }

    private void ShowIcon(Image icon, bool full)
    {
        if (emptySprite != null)
        {
            icon.sprite = full ? (fullSprite != null ? fullSprite : healthIconPrefab.sprite) : emptySprite;
            icon.color = full ? fullColor : Color.white;
        }
        else
        {
            icon.color = full ? fullColor : emptyColor;
        }
    }

    private void OnMoneyChanged(MoneyChanged evt)
    {
        if (moneyText != null)
        {
            if (countRoutine != null)
                StopCoroutine(countRoutine);
            countRoutine = StartCoroutine(CountTo(evt.Balance));
        }
        if (evt.Delta > 0 && gainText != null)
        {
            if (gainRoutine != null)
                StopCoroutine(gainRoutine);
            gainRoutine = StartCoroutine(ShowGain(evt.Delta));
        }
    }

    // Counts the number up (or down) over countTime instead of jumping.
    private IEnumerator CountTo(int target)
    {
        int start = shownMoney;
        for (float t = 0f; t < countTime; t += Time.unscaledDeltaTime)
        {
            shownMoney = Mathf.RoundToInt(Mathf.Lerp(start, target, t / countTime));
            moneyText.text = shownMoney.ToString();
            yield return null;
        }
        shownMoney = target;
        moneyText.text = target.ToString();
        countRoutine = null;
    }

    // "+N" next to the amount that drifts up and fades. Quick pickups add up into one number.
    private IEnumerator ShowGain(int amount)
    {
        pendingGain = gainText.alpha > 0.05f ? pendingGain + amount : amount;
        gainText.text = "+" + pendingGain;
        RectTransform rect = gainText.rectTransform;
        for (float t = 0f; t < gainTime; t += Time.unscaledDeltaTime)
        {
            float k = t / gainTime;
            rect.anchoredPosition = gainHome + Vector2.up * (gainRise * k);
            gainText.alpha = 1f - k * k;
            yield return null;
        }
        rect.anchoredPosition = gainHome;
        gainText.alpha = 0f;
        pendingGain = 0;
        gainRoutine = null;
    }
}
