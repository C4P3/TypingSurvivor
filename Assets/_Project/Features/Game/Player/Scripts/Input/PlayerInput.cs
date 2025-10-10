using GameControlsInput;
using System;
using TypingSurvivor.Features.Core.Settings;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TypingSurvivor.Features.Game.Player.Input
{
    public class PlayerInput : MonoBehaviour
    {
        public event Action<Vector2> OnMovePerformed;
        public event Action OnMoveCanceled;

        public GameControls GameControls { get; private set; }
        private InputAction _moveInteractAction;

        private void Awake()
        {
            if (SettingsManager.Instance == null)
            {
                Debug.LogError("SettingsManager not found! PlayerInput cannot function.");
                return;
            }
            // Use the shared GameControls instance from the SettingsManager
            GameControls = SettingsManager.Instance.SharedGameControls;
            _moveInteractAction = GameControls.Gameplay.MoveInteract;
        }

        private void OnEnable()
        {
            SubscribeGameplayEvents();
            EnableGameplayInput();
        }

        private void OnDisable()
        {
            // We don't disable the GameControls here as it's shared.
            // The SettingsScreen might need it.
            UnsubscribeGameplayEvents();
        }

        public void EnableGameplayInput()
        {
            GameControls.Gameplay.Enable();
        }

        private void SubscribeGameplayEvents()
        {
            GameControls.Gameplay.Move.performed += HandleMovePerformed;
            GameControls.Gameplay.Move.canceled += HandleMoveCanceled;
        }

        private void UnsubscribeGameplayEvents()
        {
            GameControls.Gameplay.Move.performed -= HandleMovePerformed;
            GameControls.Gameplay.Move.canceled -= HandleMoveCanceled;
        }
        
        private void HandleMovePerformed(InputAction.CallbackContext context)
        {
            if (!_moveInteractAction.IsPressed()) return;
            OnMovePerformed?.Invoke(context.ReadValue<Vector2>());
        }

        private void HandleMoveCanceled(InputAction.CallbackContext context)
        {
            OnMoveCanceled?.Invoke();
        }
    }
}