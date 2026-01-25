using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class RuleManager : MonoBehaviour
{
    public static RuleManager Instance { get; private set; }

    [SerializeField] private float targetMultiplier = 0.8f;
    [SerializeField] private int duplicatePenaltyAmount = 1;   // -1
    [SerializeField] private int precisionBonusAmount = 2;     // -2
    [SerializeField] private List<GameRuleSO> activeRules;

    private void Awake()
    {
        Instance = this;
    }

    public RoundResult ProcessSubmissions(Dictionary<int, int> submissions)
    {
        RoundResult result = new RoundResult
        {
            duplicatePenaltyAmount = duplicatePenaltyAmount,
            precisionBonusAmount = precisionBonusAmount
        };

        // Apply marking rules (duplicate, duel etc.)
        if (activeRules != null)
        {
            foreach (var rule in activeRules)
                ApplyRule(rule, submissions, result);
        }

        // Calculate target
        CalculateTarget(submissions, result);

        // Determine winners & losers
        DetermineWinnersAndLosers(submissions, result);

        // 🔹 PATCHED: If closest players are duplicates → no winner
        if (result.winnerPlayerId != -1 && result.penalizedPlayers.Contains(result.winnerPlayerId))
        {
            // Remove winner
            result.winnerPlayerId = -1;
        }

        return result;
    }

    private void ApplyRule(GameRuleSO rule, Dictionary<int, int> submissions, RoundResult result)
    {
        switch (rule.ruleType)
        {
            case RuleType.ChoicePenalty:
                ApplyDuplicatePenalty(submissions, result);
                break;

            case RuleType.WinCondition:
                ApplyFinalDuelRule(submissions, result);
                break;
        }
    }

    private void CalculateTarget(Dictionary<int, int> submissions, RoundResult result)
    {
        if (submissions.Count == 0) return;
        float avg = submissions.Values.Sum() / submissions.Count;
        result.calculatedTarget = avg * targetMultiplier;
    }

    private void DetermineWinnersAndLosers(Dictionary<int, int> submissions, RoundResult result)
    {
        if (submissions.Count == 0) return;
        if (result.finalDuelTriggered) return;

        var closest = submissions
            .OrderBy(p => Mathf.Abs(p.Value - result.calculatedTarget))
            .First();

        result.winnerPlayerId = closest.Key;
        result.winningNumber = closest.Value;

        // Precision rule
        if (Mathf.Approximately(closest.Value, result.calculatedTarget))
            result.precisionHit = true;

        DetermineLosers(submissions, result);
    }

    private void DetermineLosers(Dictionary<int, int> submissions, RoundResult result)
    {
        result.losingPlayerIds.Clear();
        foreach (var p in submissions)
            if (p.Key != result.winnerPlayerId)
                result.losingPlayerIds.Add(p.Key);
    }

    private void ApplyDuplicatePenalty(Dictionary<int, int> submissions, RoundResult result)
    {
        var groups = submissions
            .GroupBy(p => p.Value)
            .Where(g => g.Count() > 1);

        foreach (var g in groups)
        {
            foreach (var p in g)
            {
                if (!result.penalizedPlayers.Contains(p.Key))
                    result.penalizedPlayers.Add(p.Key);
            }
        }
    }

    private void ApplyFinalDuelRule(Dictionary<int, int> submissions, RoundResult result)
    {
        if (submissions.Count != 2) return;

        var players = submissions.Keys.ToArray();
        int p1 = players[0];
        int p2 = players[1];
        int v1 = submissions[p1];
        int v2 = submissions[p2];

        if ((v1 == 0 && v2 == 100) || (v1 == 100 && v2 == 0))
        {
            result.finalDuelTriggered = true;

            if (v1 == 100)
            {
                result.winnerPlayerId = p1;
                result.losingPlayerIds = new List<int> { p2 };
            }
            else
            {
                result.winnerPlayerId = p2;
                result.losingPlayerIds = new List<int> { p1 };
            }
        }
    }
}

public class RoundResult
{
    public int winnerPlayerId = -1;
    public int winningNumber;
    public float calculatedTarget;

    public List<int> losingPlayerIds = new List<int>();
    public List<int> penalizedPlayers = new List<int>();

    public bool precisionHit = false;
    public bool finalDuelTriggered = false;

    public int duplicatePenaltyAmount = 1;   // -1
    public int precisionBonusAmount = 2;     // -2

    // 🔥 FINAL SCORING
    public Dictionary<int, int> GetPointChanges()
    {
        var changes = new Dictionary<int, int>();

        var allPlayers = losingPlayerIds
            .Concat(penalizedPlayers)
            .Append(winnerPlayerId)
            .Where(id => id != -1)
            .Distinct()
            .ToList();

        foreach (int id in allPlayers)
        {
            bool isWinner = id == winnerPlayerId;
            bool isLoser = losingPlayerIds.Contains(id);
            bool isDuplicate = penalizedPlayers.Contains(id);

            // Precision + duplicate → -2
            if (precisionHit && isDuplicate)
                changes[id] = -precisionBonusAmount;

            // Any duplicate → -1
            else if (isDuplicate)
                changes[id] = -duplicatePenaltyAmount;

            // Precision loser → -2
            else if (precisionHit && isLoser)
                changes[id] = -precisionBonusAmount;

            // Normal loser → -1
            else if (isLoser)
                changes[id] = -1;

            // Winner (no duplicate) → 0
            else
                changes[id] = 0;
        }

        return changes;
    }
}
