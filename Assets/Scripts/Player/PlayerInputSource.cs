using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace NetworkingLab.Player
{
    /// <summary>
    /// Adapts an Input System action map to the small input surface used by gameplay.
    /// Bindings live in the InputActionAsset, not in PlayerController.
    /// </summary>
    public sealed class PlayerInputSource : MonoBehaviour
    {
        [SerializeField] private InputActionAsset inputActions;
        [SerializeField] private string actionMapName;

        private InputActionMap actionMap;
        private InputAction moveAction;
        private InputAction powerUpAction;

        public event Action PowerUpPressed;

        public Vector2 Movement => moveAction?.ReadValue<Vector2>() ?? Vector2.zero;
        public InputActionAsset InputActions => inputActions;
        public string ActionMapName => actionMapName;

        public void Configure(InputActionAsset actions, string mapName)
        {
            inputActions = actions;
            actionMapName = mapName;
            ResolveActions();
        }

        private void OnEnable()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            ResolveActions();
            powerUpAction.performed += OnPowerUpPerformed;
            actionMap.Enable();
        }

        private void OnDisable()
        {
            if (powerUpAction != null)
            {
                powerUpAction.performed -= OnPowerUpPerformed;
            }

            actionMap?.Disable();
        }

        private void ResolveActions()
        {
            if (inputActions == null || string.IsNullOrWhiteSpace(actionMapName))
            {
                return;
            }

            actionMap = inputActions.FindActionMap(actionMapName, true);
            moveAction = actionMap.FindAction("Move", true);
            powerUpAction = actionMap.FindAction("PowerUp", true);
        }

        private void OnPowerUpPerformed(InputAction.CallbackContext context)
        {
            PowerUpPressed?.Invoke();
        }
    }
}
