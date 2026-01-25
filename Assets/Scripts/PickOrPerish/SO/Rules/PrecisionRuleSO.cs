using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Rule_Precision", menuName = "PickOrPerish/Rules/Precision")]
public class PrecisionRuleSO : GameRuleSO
{
    private void OnEnable()
    {
        ruleName = "Precision Bonus";
        description = "Exact match makes others lose 2 points instead of 1";
        roundNumber = 3;
        ruleType = RuleType.SpecialEffect;
        enablePrecisionBonus = true;
        precisionBonusAmount = 2;
        affectsPoints = true;
    }
}