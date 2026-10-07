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

            AutoConnectPort = 0;
            bool clientOnly = Array.Exists(Environment.GetCommandLineArgs(), arg =>
                string.Equals(arg, "-client", StringComparison.OrdinalIgnoreCase));

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
                CreateServerWorld("ServerWorld");
                CreateClientWorld("ClientWorld");
            }

            return true;
        }
    }
}
