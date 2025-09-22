using UnityEngine;

public class InGameHUDManager : MonoBehaviour
{
    // 子オブジェクトなどから参照を設定するUI部品
    [SerializeField] private OxygenView _oxygenView;
    [SerializeField] private ScoreView _scoreView;

    // DIコンテナから注入される、読み取り専用のインターフェース
    private IGameStateReader _gameStateReader;
    private IPlayerStatusSystemReader _playerStatusReader; // Reader I/Fを分離


    // [Inject]の代わりに、外部から呼び出される公開の初期化メソッドを用意
    public void Initialize(IGameStateReader gameStateReader, IPlayerStatusSystemReader playerStatusReader)
    {
        _gameStateReader = gameStateReader;
        _playerStatusReader = playerStatusReader;

        // イベントの購読など
        OnEnable();
    }

    private void OnEnable()
    {
        // イベントの購読を開始する
        SubscribeEvents();
    }
    private void OnDisable()
    {
        // 必ず購読解除
        UnSubscribeEvents();
    }

    private void SubscribeEvents()
    {
        // 各リーダーが持つイベントを購読する
        _gameStateReader.OnOxygenChanged += OnOxygenChanged;
        _gameStateReader.OnScoreChanged += OnScoreChanged;
    }
    private void UnSubscribeEvents()
    {
        // 購読解除する
        _gameStateReader.OnOxygenChanged -= OnOxygenChanged;
        _gameStateReader.OnScoreChanged -= OnScoreChanged;
    }

    // イベントを受け取ったら、担当のUI部品に更新を指示する
    private void OnOxygenChanged(float newOxygenValue)
    {
        _oxygenView.UpdateView(newOxygenValue, _playerStatusReader.GetStatValue(PlayerStat.MaxOxygen));
    }

    private void OnScoreChanged(int newScoreValue)
    {
        _scoreView.UpdateView(newScoreValue);
    }
}