namespace TypingSurvivor.Features.Game.Typing
{
    /// <summary>
    /// タイピングのお題を提供するサービスのインターフェース。
    /// </summary>
    public interface IWordProvider
    {
        /// <summary>
        /// 次のタイピングチャレンジを取得する。
        /// </summary>
        /// <returns>新しいタイピングチャレンジ。</returns>
        TypingChallenge GetNextChallenge();
    }
}
