using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace NetworkingLab.Editor
{
    public static class NfeBuildTools
    {
        [MenuItem("Networking Lab/NFE/Build Windows Client")]
        public static void BuildWindowsClient()
        {
            Build(BuildTarget.StandaloneWindows64, StandaloneBuildSubtarget.Player,
                "Builds/NFE-WindowsClient/NetworkingLabNFEClient.exe");
        }

        [MenuItem("Networking Lab/NFE/Build Windows Server")]
        public static void BuildWindowsServer()
        {
            Build(BuildTarget.StandaloneWindows64, StandaloneBuildSubtarget.Server,
                "Builds/NFE-WindowsServer/NetworkingLabNFEServer.exe");
        }

        [MenuItem("Networking Lab/NFE/Build Linux Server")]
        public static void BuildLinuxServer()
        {
            Build(BuildTarget.StandaloneLinux64, StandaloneBuildSubtarget.Server,
                "Builds/NFE-LinuxServer/NetworkingLabNFEServer.x86_64");
        }

        private static void Build(BuildTarget target, StandaloneBuildSubtarget subtarget, string output)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new BuildFailedException("Detén Play antes de generar el build NFE.");
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Standalone, target))
            {
                throw new BuildFailedException("Instala el módulo de esta plataforma en Unity Hub.");
            }

            Directory.CreateDirectory(Path.GetDirectoryName(output));
            InsecureHttpOption previousHttpOption = PlayerSettings.insecureHttpOption;
            try
            {
                // El REST SDK de Agones usa HTTP en loopback dentro del mismo Pod.
                if (subtarget == StandaloneBuildSubtarget.Server)
                {
                    PlayerSettings.insecureHttpOption = InsecureHttpOption.AlwaysAllowed;
                }

                BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    scenes = new[] { NetworkLabSceneBuilder.NfeScenePath },
                    locationPathName = output,
                    target = target,
                    subtarget = (int)subtarget,
                    options = BuildOptions.None
                });
                if (report.summary.result != BuildResult.Succeeded)
                {
                    throw new BuildFailedException($"Build NFE falló: {report.summary.result}. Revisa la consola.");
                }

                Debug.Log($"NFE_BUILD: {output}");
            }
            finally
            {
                PlayerSettings.insecureHttpOption = previousHttpOption;
            }
        }
    }
}
