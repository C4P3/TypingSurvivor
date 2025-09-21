using UnityEngine;
using Unity.Netcode;
using InputSystem;
using UnityEngine.InputSystem;

public class PlayerMovement : NetworkBehaviour
{
    private InputSystem_Actions _gameInputs;
    private Vector2 direction = new Vector2(0, 0);

    private void Awake()
    {
        _gameInputs = new InputSystem_Actions();

        _gameInputs.Player.Move.started += OnMove;
        _gameInputs.Player.Move.performed += OnMove;
        _gameInputs.Player.Move.canceled += OnMove;

        _gameInputs.Enable();
    }
    public override void OnDestroy()
    {
        _gameInputs?.Dispose();
        base.OnDestroy();
    }

    private void Update()
    {
        if (IsOwner == false)
        {
            return;
        }

        float moveSpeed = 3f;
        transform.Translate(direction * moveSpeed * Time.deltaTime);
    }

    private void OnMove(InputAction.CallbackContext context)
    {
        if (IsOwner == false)
        {
            return;
        }
        direction = context.ReadValue<Vector2>();
    }
}