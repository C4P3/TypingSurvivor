/// <summary>
/// ゲーム状態の読み取り専用インターフェース。UIなど表示系が使用する。
/// </summary>
public interface IGameStateReader
{
    float CurrentOxygen { get; }
    int CurrentScore { get; }
    event System.Action<float> OnOxygenChanged;
    event System.Action<int> OnScoreChanged;
}