/// <summary>
/// ゲーム状態の書き込み専用インターフェース。アイテム効果など、状態を変更する権限を持つクラスが使用する。
/// </summary>
public interface IGameStateWriter
{
    void AddOxygen(float amount);
    void AddScore(int amount);
    void SetGameOver();
}