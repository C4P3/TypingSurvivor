namespace TypingSurvivor.Features.Game.Level.Data
{
    public enum TileInteractionType
    {
        /// <summary>
        /// 通行可能（何もない空間）
        /// </summary>
        Walkable,
        /// <summary>
        /// 破壊可能（通常のブロック）
        /// </summary>
        Destructible,
        /// <summary>
        /// 破壊不能（岩盤、Unchiなど）
        /// </summary>
        Indestructible
    }
}
