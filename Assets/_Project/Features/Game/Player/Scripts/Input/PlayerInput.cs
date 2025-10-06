using System;
using UnityEngine;
using UnityEngine.InputSystem;
using GameControlsInput;

namespace TypingSurvivor.Features.Game.Player.Input
{
    public class PlayerInput : MonoBehaviour
    {
        // Gameplay Actions
        public event Action<Vector2> OnMovePerformed;
        public event Action OnMoveCanceled;

        private GameControls _gameControls;
        private InputAction _moveInteractAction;

        private void Awake()
        {
            _gameControls = new GameControls();
            _moveInteractAction = _gameControls.Gameplay.MoveInteract;
        }

        private void OnEnable()
        {
            SubscribeGameplayEvents();
            EnableGameplayInput();
        }

        private void OnDisable()
        {
            UnsubscribeGameplayEvents();
            _gameControls.Gameplay.Disable();
        }


        public void EnableGameplayInput()
        {
            _gameControls.Gameplay.Enable();
        }

        private void SubscribeGameplayEvents()
        {
            _gameControls.Gameplay.Move.performed += HandleMovePerformed;
            _gameControls.Gameplay.Move.canceled += HandleMoveCanceled;
        }

        private void UnsubscribeGameplayEvents()
        {
            _gameControls.Gameplay.Move.performed -= HandleMovePerformed;
            _gameControls.Gameplay.Move.canceled -= HandleMoveCanceled;
        }
        
        private void HandleMovePerformed(InputAction.CallbackContext context)
        {
            // MoveInteractキーが押されていなければ、移動イベントを発行しない
            if (!_moveInteractAction.IsPressed()) return;
            
            OnMovePerformed?.Invoke(context.ReadValue<Vector2>());
        }

        private void HandleMoveCanceled(InputAction.CallbackContext context)
        {
            OnMoveCanceled?.Invoke();
        }
    }
}