using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using CharacterController = Assets.Scripts.CharacterController;

// The save game. Sitting at a bench (CheckpointReached) and beating a boss save it; opening the level loads it:
// the player starts at the last bench with their abilities, lucky numbers (and which were played at the roulette
// table), money and beaten bosses. Beaten bosses are switched off. Put one in the level scene and list every lucky
// number asset so saved names can be found again. F9 (editor and development builds) deletes the save and
// reloads the scene.
[DefaultExecutionOrder(-500)]
public class SaveGame : MonoBehaviour
{
    [Serializable]
    private class Data
    {
        public string scene;
        public Vector3 bench;
        public bool hasBench;
        public bool dash;
        public bool wallJump;
        public bool doubleJump;
        public int money;
        public List<string> luckyNumbers = new();
        public List<string> playedNumbers = new();
        public List<string> beatenBosses = new();
    }

    [Tooltip("Every lucky number asset in the game, so a saved name can be turned back into its asset.")]
    [SerializeField] private LuckyNumberData[] luckyNumbers;
    [Tooltip("What a new game starts with (no save yet), e.g. the player's own lucky number that opens Dash.")]
    [SerializeField] private LuckyNumberData[] startingLuckyNumbers;
    [SerializeField] private string playerTag = "Player";

    private static readonly HashSet<string> BeatenBosses = new();
    private static string FilePath => Path.Combine(Application.persistentDataPath, "save.json");

    private Data data;
    private CharacterController player;

    // Domain reload is off, so the static must be cleared by hand between Play sessions.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => BeatenBosses.Clear();

    public static bool IsBossBeaten(string roomId) => !string.IsNullOrEmpty(roomId) && BeatenBosses.Contains(roomId);

    // Loads before anything else starts, so pickups and doors already see the restored state in their Start.
    private void Awake()
    {
        GameObject playerObject = GameObject.FindWithTag(playerTag);
        if (playerObject != null)
            player = playerObject.GetComponent<CharacterController>();

        data = Read();
        if (data == null || data.scene != SceneManager.GetActiveScene().name)
        {
            // A new game: no save for this scene yet.
            data = new Data { scene = SceneManager.GetActiveScene().name };
            foreach (LuckyNumberData number in startingLuckyNumbers)
                if (number != null)
                    data.luckyNumbers.Add(number.name);
        }
        Apply();
    }

    private IEnumerator Start()
    {
        // A frame later every boss has found its arena (in its own Start); switch off the beaten ones.
        yield return null;
        foreach (Bosses.BossFight boss in FindObjectsByType<Bosses.BossFight>())
            if (IsBossBeaten(boss.RoomId))
            {
                boss.DisableIntros();
                boss.gameObject.SetActive(false);
            }
    }

    private void OnEnable()
    {
        EventBus.Subscribe<CheckpointReached>(OnBench);
        EventBus.Subscribe<BossDefeated>(OnBossDefeated);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<CheckpointReached>(OnBench);
        EventBus.Unsubscribe<BossDefeated>(OnBossDefeated);
    }

    private void Update()
    {
        if (!Debug.isDebugBuild)
            return;
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && keyboard.f9Key.wasPressedThisFrame)
        {
            Delete();
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }

    private void OnBench(CheckpointReached evt)
    {
        data.bench = evt.Position;
        data.hasBench = true;
        Save();
    }

    private void OnBossDefeated(BossDefeated evt)
    {
        if (!string.IsNullOrEmpty(evt.RoomId))
            BeatenBosses.Add(evt.RoomId);
        Save();
    }

    private void Apply()
    {
        BeatenBosses.Clear();
        BeatenBosses.UnionWith(data.beatenBosses);

        if (player != null)
        {
            player.DashUnlocked |= data.dash;
            player.WallJumpUnlocked |= data.wallJump;
            player.DoubleJumpUnlocked |= data.doubleJump;
            if (data.hasBench)
            {
                Vector3 at = data.bench;
                player.transform.position = at;
                if (player.TryGetComponent(out Rigidbody2D body))
                    body.position = at;
            }
        }

        foreach (string name in data.luckyNumbers)
            if (Find(name) is LuckyNumberData number)
                number.Grant();
        foreach (string name in data.playedNumbers)
            if (Find(name) is LuckyNumberData number)
                CasinoTable.MarkPlayed(number);
        if (data.money > 0)
            MoneyWallet.Set(data.money);
    }

    // Writes what the player has now; the bench stays the last one sat at.
    public void Save()
    {
        if (player != null)
        {
            data.dash = player.DashUnlocked;
            data.wallJump = player.WallJumpUnlocked;
            data.doubleJump = player.DoubleJumpUnlocked;
        }
        data.money = MoneyWallet.Balance;
        data.luckyNumbers.Clear();
        foreach (LuckyNumberData number in LuckyNumbers.All)
            data.luckyNumbers.Add(number.name);
        data.playedNumbers = new List<string>(CasinoTable.PlayedNames());
        data.beatenBosses = new List<string>(BeatenBosses);
        File.WriteAllText(FilePath, JsonUtility.ToJson(data, true));
    }

    [ContextMenu("Delete Save")]
    public void Delete()
    {
        if (File.Exists(FilePath))
            File.Delete(FilePath);
        data = new Data { scene = SceneManager.GetActiveScene().name };
        BeatenBosses.Clear();
        // The run's state lives in statics that survive a scene reload: start it over too.
        LuckyNumbers.Clear();
        CasinoTable.ForgetPlayed();
        MoneyWallet.Set(0);
    }

    private static Data Read()
    {
        try
        {
            return File.Exists(FilePath) ? JsonUtility.FromJson<Data>(File.ReadAllText(FilePath)) : null;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"SaveGame: could not read {FilePath}, starting fresh. {e.Message}");
            return null;
        }
    }

    private LuckyNumberData Find(string assetName) =>
        luckyNumbers == null ? null : Array.Find(luckyNumbers, n => n != null && n.name == assetName);
}
