using UnityEngine;

public class ItemRegistry : ScriptableObject
{
    // 役割: ゲーム内に存在する全てのItemDataアセットをリストとして保持する、静的なデータベース。
    // 責務:
    // LevelManagerがマップ生成時にランダムなアイテムを選ぶ際や、ItemServiceがIDからItemDataを取得する際に利用される。
}