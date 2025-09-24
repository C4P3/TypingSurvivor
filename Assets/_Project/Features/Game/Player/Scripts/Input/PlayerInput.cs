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
        public event Action OnInteractIntent;

        // Typing Actions
        public event Action OnCancelTypingIntent;

        private GameControls _gameControls;

        private void Awake()
        {
            _gameControls = new GameControls();
        }

        private void OnEnable()
        {
            // Subscribe to events regardless of which map is active
            SubscribeGameplayEvents();
            SubscribeTypingEvents();

            // Enable a default map
            EnableGameplayInput();
        }

        private void OnDisable()
        {
            UnsubscribeGameplayEvents();
            UnsubscribeTypingEvents();
            
            _gameControls.Player.Disable();
            _gameControls.Typing.Disable();
        }

        public void EnableGameplayInput()
        {
            _gameControls.Typing.Disable();
            _gameControls.Player.Enable();
        }

        public void EnableTypingInput()
        {
            _gameControls.Player.Disable();
            _gameControls.Typing.Enable();
        }

        private void SubscribeGameplayEvents()
        {
            _gameControls.Player.Move.performed += HandleMovePerformed;
            _gameControls.Player.Move.canceled += HandleMoveCanceled;
            _gameControls.Player.Interact.performed += HandleInteractPerformed;
        }

        private void UnsubscribeGameplayEvents()
        {
            _gameControls.Player.Move.performed -= HandleMovePerformed;
            _gameControls.Player.Move.canceled -= HandleMoveCanceled;
            _gameControls.Player.Interact.performed -= HandleInteractPerformed;
        }
        
        private void SubscribeTypingEvents()
        {
            _gameControls.Typing.Cancel.performed += HandleCancelTypingPerformed;
        }

        private void UnsubscribeTypingEvents()
        {
            _gameControls.Typing.Cancel.performed -= HandleCancelTypingPerformed;
        }

        private void HandleMovePerformed(InputAction.CallbackContext context)
        {
            OnMovePerformed?.Invoke(context.ReadValue<Vector2>());
        }

        private void HandleMoveCanceled(InputAction.CallbackContext context)
        {
            OnMoveCanceled?.Invoke();
        }

        private void HandleInteractPerformed(InputAction.CallbackContext context)
        {
            OnInteractIntent?.Invoke();
        }
        
        private void HandleCancelTypingPerformed(InputAction.CallbackContext context)
        {
            OnCancelTypingIntent?.Invoke();
        }
    }
}