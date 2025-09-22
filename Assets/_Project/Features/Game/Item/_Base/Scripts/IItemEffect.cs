/// <summary>
/// 全てのアイテム効果が実装するインターフェース。
/// </summary>
public interface IItemEffect
{
    // 効果を実行する。引数はItemExecutionContextのみ。
    void Execute(ItemExecutionContext context);
}
