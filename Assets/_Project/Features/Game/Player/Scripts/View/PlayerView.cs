using UnityEngine;
using TypingSurvivor.Features.Game.Player;

public class PlayerView : MonoBehaviour
{
    [Header("スプライト参照")]
    [SerializeField] private SpriteRenderer _spriteRenderer;
    [SerializeField] private Sprite _spriteUp;
    [SerializeField] private Sprite _spriteDown;
    [SerializeField] private Sprite _spriteLeft;
    [SerializeField] private Sprite _spriteRight;

    private PlayerFacade _facade;

    /// <summary>
    /// PlayerFacadeのOnNetworkSpawn後に呼び出され、初期化を行う
    /// </summary>
    public void Initialize(PlayerFacade facade)
    {
        _facade = facade;

        if (_spriteRenderer == null)
        {
            _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        // 念のため古い購読を解除してから、新しい購読を開始する
        _facade.NetworkFacingDirection.OnValueChanged -= HandleDirectionChanged;
        _facade.NetworkFacingDirection.OnValueChanged += HandleDirectionChanged;

        // 初期表示のために、現在の値で一度スプライトを更新する
        HandleDirectionChanged(Vector3Int.zero, _facade.NetworkFacingDirection.Value);
    }

    private void OnDisable()
    {
        // オブジェクトが無効化される際に、イベントの購読を解除する
        if (_facade != null)
        {
            _facade.NetworkFacingDirection.OnValueChanged -= HandleDirectionChanged;
        }
    }

    /// <summary>
    /// NetworkFacingDirectionの値が変更されたときに呼び出されるハンドラ
    /// </summary>
    private void HandleDirectionChanged(Vector3Int previousDirection, Vector3Int newDirection)
    {
        if (newDirection == Vector3Int.right)
        {
            _spriteRenderer.sprite = _spriteRight;
        }
        else if (newDirection == Vector3Int.left)
        {
            _spriteRenderer.sprite = _spriteLeft;
        }
        else if (newDirection == Vector3Int.up)
        {
            _spriteRenderer.sprite = _spriteUp;
        }
        else if (newDirection == Vector3Int.down)
        {
            _spriteRenderer.sprite = _spriteDown;
        }
    }

    /// <summary>
    /// PlayerFacadeからStateの変更通知を受け取る（既存のメソッド）
    /// </summary>
    public void UpdateAnimation(PlayerState newValue)
    {
        // Animatorを使ったアニメーションロジックはここに記述
    }
}