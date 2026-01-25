using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Rule_Duplicate", menuName = "PickOrPerish/Rules/Duplicate")]
public class DuplicateRuleSO : GameRuleSO
{
    private void OnEnable()
    {
        ruleName = "Duplicate Penalty";
        description = "If multiple players choose same number, all lose 1 point";
        roundNumber = 2;
        ruleType = RuleType.ChoicePenalty;
        enableDuplicatePenalty = true;
        duplicatePenaltyAmount = 1;
        affectsPoints = true;
    }
}
