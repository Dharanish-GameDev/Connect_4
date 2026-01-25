using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Rule_FinalDuel", menuName = "PickOrPerish/Rules/FinalDuel")]
public class FinalDuelRuleSO : GameRuleSO
{
    private void OnEnable()
    {
        ruleName = "Final Duel";
        description = "0 vs 100: 100 always wins";
        minPlayers = 2;
        maxPlayers = 2;
        ruleType = RuleType.WinCondition;
        isFinalDuelRule = true;
    }
}