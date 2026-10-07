using System.Collections;
using NetworkingLab.PowerUp;
using UnityEngine;
using UnityEngine.UI;

namespace NetworkingLab.UI
{
    public sealed class PowerUpMessageUI : MonoBehaviour
    {
        [SerializeField] private Text messageText;
        [SerializeField, Min(0f)] private float visibleSeconds = 2f;

        private Coroutine clearRoutine;

        public string CurrentMessage => messageText == null ? string.Empty : messageText.text;

        public void Configure(Text targetText, float duration)
        {
            messageText = targetText;
            visibleSeconds = duration;
        }

        private void OnEnable()
        {
            PowerUpEvents.Activated += ShowMessage;
        }

        private void OnDisable()
        {
            PowerUpEvents.Activated -= ShowMessage;
        }

        private void ShowMessage(string playerName)
        {
            messageText.text = $"{playerName} activated PowerUp";

            if (clearRoutine != null)
            {
                StopCoroutine(clearRoutine);
            }

            clearRoutine = StartCoroutine(ClearAfterDelay());
        }

        private IEnumerator ClearAfterDelay()
        {
            yield return new WaitForSeconds(visibleSeconds);
            messageText.text = string.Empty;
            clearRoutine = null;
        }
    }
}
