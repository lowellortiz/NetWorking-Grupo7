using System;
using Unity.NetCode;
using UnityEngine.SceneManagement;
using UnityEngine.Scripting;

namespace NetworkingLab.NFE
{
    /// <summary>Creates NFE worlds only for the NFE scene or an explicit -nfe launch.</summary>
    [Preserve]
    public sealed class NfeBootstrap : ClientServerBootstrap
    {
        public override bool Initialize(string defaultWorldName)
        {
            string sceneName = SceneManager.GetActiveScene().name;
            bool explicitNfe = Array.Exists(Environment.GetCommandLineArgs(), arg =>
                string.Equals(arg, "-nfe", StringComparison.OrdinalIgnoreCase));

            if (!explicitNfe && !sceneName.StartsWith("NFE", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            // Mantiene la simulación y la conexión activas al cambiar de ventana.
            UnityEngine.Application.runInBackground = true;
            AutoConnectPort = 0;
            bool clientOnly = Array.Exists(Environment.GetCommandLineArgs(), arg =>
                string.Equals(arg, "-client", StringComparison.OrdinalIgnoreCase));

            // El build Dedicated Server debe crear solo ServerWorld, incluso sin -batchmode.
#if UNITY_SERVER
            CreateServerWorld("ServerWorld");
#elif UNITY_CLIENT
            CreateClientWorld("ClientWorld");
#else
            if (clientOnly)
            {
                CreateClientWorld("ClientWorld");
            }
            else if (UnityEngine.Application.isBatchMode)
            {
                CreateServerWorld("ServerWorld");
            }
            else
            {
                // Respeta Client/Server/ClientAndServer del PlayMode Tools de NetCode.
                CreateDefaultClientServerWorlds();
            }
#endif

            return true;
        }
    }
}
