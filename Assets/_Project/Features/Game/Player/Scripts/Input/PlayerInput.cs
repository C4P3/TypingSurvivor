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

        // Typing Actions
        public event Action<Vector2> OnTypingMovePerformed;
        public event Action OnCancelTypingIntent;

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
            SubscribeTypingEvents();
            EnableGameplayInput();
        }

        private void OnDisable()
        {
            UnsubscribeGameplayEvents();
            UnsubscribeTypingEvents();
            _gameControls.Gameplay.Disable();
            _gameControls.Typing.Disable();
        }


        public void EnableGameplayInput()
        {
            _gameControls.Typing.Disable();
            _gameControls.Gameplay.Enable();
        }

        public void EnableTypingInput()
        {
            _gameControls.Gameplay.Disable();
            _gameControls.Typing.Enable();
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
        
        private void SubscribeTypingEvents()
        {
            _gameControls.Typing.Move.performed += HandleTypingMovePerformed;
            _gameControls.Typing.Cancel.performed += HandleCancelTypingPerformed;
        }

        private void UnsubscribeTypingEvents()
        {
            _gameControls.Typing.Move.performed -= HandleTypingMovePerformed;
            _gameControls.Typing.Cancel.performed -= HandleCancelTypingPerformed;
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
        
        private void HandleTypingMovePerformed(InputAction.CallbackContext context)
        {
            OnTypingMovePerformed?.Invoke(context.ReadValue<Vector2>());
        }

        private void HandleCancelTypingPerformed(InputAction.CallbackContext context)
        {
            OnCancelTypingIntent?.Invoke();
        }
    }
}