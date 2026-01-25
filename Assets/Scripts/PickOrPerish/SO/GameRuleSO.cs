using UnityEngine;

[CreateAssetMenu(fileName = "GameRule", menuName = "PickOrPerish/Rule")]
public class GameRuleSO : ScriptableObject
{
    // Basic Info
    public string ruleName;
    [TextArea(3, 5)] public string description;
    
    // When to activate
    public int minPlayers = 4;  // Minimum players needed for this rule
    public int maxPlayers = 10; // Maximum players for this rule
    public int roundNumber = 1; // Which round this activates (1 = first round)
    
    // Rule Effects
    public RuleType ruleType;
    public float multiplier = 0.8f; // For average rule
    public bool affectsTarget = true;
    public bool affectsPoints = false;
    
    // Duplicate Penalty specific
    public bool enableDuplicatePenalty = false;
    public int duplicatePenaltyAmount = 1;
    
    // Precision Bonus specific
    public bool enablePrecisionBonus = false;
    public int precisionBonusAmount = 2;
    
    // Final Duel specific
    public bool isFinalDuelRule = false;
    
    // Visuals
    public Sprite ruleIcon;
    public Color ruleColor = Color.white;
}

public enum RuleType
{
    TargetModifier,    // Changes how target is calculated (Average × Multiplier)
    ChoicePenalty,     // Penalizes certain choices (Duplicates)
    WinCondition,      // Changes win conditions (Final Duel)
    SpecialEffect      // Other effects
}