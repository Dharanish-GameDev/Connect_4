using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.Netcode;
using Random = UnityEngine.Random;

public class NetworkGameManager : NetworkBehaviour
{
    public static NetworkGameManager Instance { get; private set; }

    [SerializeField] private PanelManager _panelManager;
    public PanelManager PanelManager => _panelManager;

    [Range(1, 10)]
    [SerializeField] private int countDownSeconds = 3;

    private NetworkVariable<int> currentRound = new NetworkVariable<int>(1);

    private List<NetworkPlayer> players = new List<NetworkPlayer>();

    public NetworkVariable<int> timer = new NetworkVariable<int>(25);

    public event Action OnTimerEnd;

    [SerializeField] private int DefaultWaitTime = 25;
    private Coroutine countDownCoroutine;

    private bool winnerFound = false;
    
    [SerializeField] private List<PoP_PlayerDataSO> playerDatas = new List<PoP_PlayerDataSO>();

    private void Awake() => Instance = this;

    #region Timer
    public void StartCountDown()
    {
        if (winnerFound) return;

        if (countDownCoroutine != null)
        {
            StopCoroutine(countDownCoroutine);
            countDownCoroutine = null;
        }

        EnableTimerTextClientRPC();
        countDownCoroutine = StartCoroutine(CountDownCoroutine());
    }

    private IEnumerator CountDownCoroutine()
    {
        timer.Value = DefaultWaitTime;

        while (timer.Value > 0)
        {
            yield return new WaitForSeconds(1f);
            timer.Value--;
        }

        FireTimerCountdownEndClientRPC();
    }

    [ClientRpc]
    private void FireTimerCountdownEndClientRPC()
    {
        Debug.Log($"Countdown finished in {countDownSeconds} Secs");
        OnTimerEnd?.Invoke();
    }

    [ClientRpc]
    private void EnableTimerTextClientRPC() => _panelManager.EnableTimerText();
    #endregion

    #region Register
    public void RegisterTimerValueChanged(NetworkVariable<int>.OnValueChangedDelegate onValueChanged)
    {
        timer.OnValueChanged -= onValueChanged;
        timer.OnValueChanged += onValueChanged;
    }

    public void RegisterOnCurrentRoundValueChanged(NetworkVariable<int>.OnValueChangedDelegate onValueChanged)
    {
        currentRound.OnValueChanged -= onValueChanged;
        currentRound.OnValueChanged += onValueChanged;
    }

    public void RegisterMeToTheMatch(NetworkPlayer player)
    {
        if (!players.Contains(player)) players.Add(player);
    }
    #endregion

    #region Submissions
    private Dictionary<int, int> submittedNumbersDict = new Dictionary<int, int>();

    public void SubmittedNumber(int playerId, int currentNumber)
    {
        if (currentNumber < 0) return;

        if (!submittedNumbersDict.ContainsKey(playerId))
            submittedNumbersDict.Add(playerId, currentNumber);

        var activePlayers = GetActivePlayers();

        if (IsServer &&
            activePlayers.Count > 0 &&
            activePlayers.Count == submittedNumbersDict.Count)
        {
            Debug.Log($"Round Finishing in {countDownSeconds} Secs");

            if (countDownCoroutine != null)
            {
                StopCoroutine(countDownCoroutine);
                countDownCoroutine = null;
            }

            FireTimerCountdownEndClientRPC();
            StartCountDownClientRPC(countDownSeconds);

            Invoke(nameof(AdvanceRound), countDownSeconds);
        }
    }
    #endregion

    #region RoundFlow

    private void AdvanceRound()
    {
        if (!IsServer || winnerFound) return;

        currentRound.Value += 1;

        Dictionary<int, int> playerScores = GetPlayerScores();

        RoundResult result = RuleManager.Instance.ProcessSubmissions(submittedNumbersDict);

        if (result != null)
        {
            // 🔹 PATCHED: get only non-duplicate winners or none
            int[] winners = GetRoundWinners(result, submittedNumbersDict).ToArray();

            if (winners.Length == 0)
                Debug.Log("❌ No Winner this round (duplicate rule triggered)");
            else
                Debug.Log("🏆 Winners: " + string.Join(", ", winners));

            // 🔹 Update UI
            UpdateAfterRoundUIClientRPC(winners, result.calculatedTarget);

            // Apply point changes
            var pointChanges = result.GetPointChanges();
            foreach (var change in pointChanges)
                playerScores[change.Key] += change.Value;

            UpdatePointChanges(playerScores);
            StartCountDown();
        }

        submittedNumbersDict.Clear();
    }

    private List<int> GetRoundWinners(RoundResult result, Dictionary<int, int> submissions)
    {
        List<int> winners = new List<int>();
        float minDiff = submissions.Min(p => Mathf.Abs(p.Value - result.calculatedTarget));

        foreach (var p in submissions)
        {
            if (Mathf.Approximately(Mathf.Abs(p.Value - result.calculatedTarget), minDiff))
            {
                if (!result.penalizedPlayers.Contains(p.Key))
                    winners.Add(p.Key);
            }
        }

        return winners;
    }
    #endregion

    #region Scoring
    private void UpdatePointChanges(Dictionary<int, int> pointChanges)
    {
        foreach (var change in pointChanges)
        {
            NetworkPlayer player = players.FirstOrDefault(p => p.GetPlayerID() == change.Key);
            if (player != null)
                player.UpdateCurrentScore(change.Value);
        }

        // Winner check for 2-player remaining logic (unchanged)
        if (pointChanges.Count == 2)
        {
            var remaining = pointChanges.Where(p => p.Value > 0).Select(p => p.Key).ToList();
            if (remaining.Count == 1)
            {
                int winnerId = remaining[0];
                winnerFound = true;
                AnnounceWinnerClientRPC(winnerId);
            }
        }
    }
    #endregion

    #region Helpers
    private Dictionary<int, int> GetPlayerScores()
    {
        Dictionary<int, int> scores = new Dictionary<int, int>();
        foreach (var p in GetActivePlayers())
            scores[p.GetPlayerID()] = p.GetCurrentScore();
        return scores;
    }

    private List<NetworkPlayer> GetActivePlayers()
    {
        return players.Where(p => p.GetCurrentPlayerState() == PlayerState.Active).ToList();
    }
    #endregion

    #region RPCs
    [ClientRpc]
    private void StartCountDownClientRPC(int countDownSeconds)
        => _panelManager.StartCountdown(countDownSeconds);

    [ClientRpc]
    private void UpdateAfterRoundUIClientRPC(int[] winnerIds, float targetNumber)
        => _panelManager.UpdateAfterRoundUI(winnerIds, targetNumber);

    [ClientRpc]
    private void AnnounceWinnerClientRPC(int winnerId)
        => _panelManager.AnnounceWinner(winnerId);
    #endregion

    #region DataHandlers

    public PoP_PlayerDataSO GetPlayerDataForId(int playerId)
    {
        PoP_PlayerDataSO playerDataSO = null;
        playerDataSO = playerDatas.FirstOrDefault(p => p.playerID == playerId);
        return playerDataSO;
    }

    #endregion
#if UNITY_EDITOR
    [Range(1, 10)]
    [SerializeField] private int roundDebug = 1;

    [ContextMenu("DebugCurrentRound")]
    private void DebugCurrentRound()
    {
        if (IsServer) currentRound.Value = roundDebug;
    }
#endif
}
