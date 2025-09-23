using UnityEngine;

[CreateAssetMenu(fileName = "GameConfig", menuName = "Settings/Game Configuration")]
public class GameConfig : ScriptableObject
{
    public GameRuleSettings RuleSettings;
    public PlayerDefaultStats PlayerStats;
    public ItemRegistry ItemRegistry;
    // ... 他の全体設定アセット ...
}