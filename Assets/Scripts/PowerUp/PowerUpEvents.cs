using System;

namespace NetworkingLab.PowerUp
{
    /// <summary>
    /// Framework-neutral gameplay event. A later networking adapter can publish the same event
    /// after server validation without changing the UI.
    /// </summary>
    public static class PowerUpEvents
    {
        public static event Action<string> Activated;

        public static void RaiseActivated(string playerName)
        {
            Activated?.Invoke(playerName);
        }
    }
}
