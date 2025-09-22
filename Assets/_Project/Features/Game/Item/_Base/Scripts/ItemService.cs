public class ItemService : IItemService
{
    // 役割: IItemServiceインターフェースを実装する、サーバーサイドの実行エンジン。
    // 責務:
    // PlayerFacadeからAcquireItem(itemId)の要求を受け取る。
    // ItemRegistryに問い合わせ、対応するItemDataを取得する。
    // DIで注入された各種Writerサービスへの参照をまとめ、ItemExecutionContextを生成する。
    // ItemDataが持つEffectsリストをループし、全てのIItemEffect.Execute(context)を呼び出す。
}