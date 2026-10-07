using NetworkingLab.Player;
using UnityEngine;

namespace NetworkingLab.PowerUp
{
    public sealed class PlayerPowerUp : MonoBehaviour
    {
        [SerializeField] private string playerName = "Player";
        [SerializeField] private PlayerInputSource inputSource;

        public string PlayerName => playerName;

        public void Configure(string displayName, PlayerInputSource source)
        {
            playerName = displayName;
            inputSource = source;
        }

        private void OnEnable()
        {
            if (inputSource != null)
            {
                inputSource.PowerUpPressed += ActivatePowerUp;
            }
        }

        private void OnDisable()
        {
            if (inputSource != null)
            {
                inputSource.PowerUpPressed -= ActivatePowerUp;
            }
        }

        public void ActivatePowerUp()
        {
            PowerUpEvents.RaiseActivated(playerName);
        }
    }
}
