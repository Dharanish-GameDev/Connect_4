using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Rule_Average", menuName = "PickOrPerish/Rules/Average")]
public class AverageRuleSO : GameRuleSO
{
    private void OnEnable()
    {
        ruleName = "Average Rule";
        description = "Target = Average of all numbers × 0.8";
        roundNumber = 1;
        ruleType = RuleType.TargetModifier;
        multiplier = 0.8f;
        affectsTarget = true;
    }
}
