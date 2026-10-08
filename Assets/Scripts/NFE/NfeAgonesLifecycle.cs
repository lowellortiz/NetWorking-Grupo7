using System;
using System.Collections;
using System.Text;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Networking;

namespace NetworkingLab.NFE
{
    /// <summary>Ready y Health mediante el REST SDK local de Agones.</summary>
    public sealed class NfeAgonesLifecycle : MonoBehaviour
    {
        private World serverWorld;
        private string sdkUrl;
        private bool started;
        private bool healthReported;
        private bool healthFailed;

        public void Begin(World server)
        {
            if (started) return;
            string value = Environment.GetEnvironmentVariable("AGONES_SDK_HTTP_PORT");
            if (!ushort.TryParse(value, out ushort port) || port == 0) return;

            started = true;
            serverWorld = server;
            sdkUrl = $"http://127.0.0.1:{port}";
            StartCoroutine(ReportHealth());
            StartCoroutine(ReportReady());
        }

        private IEnumerator ReportReady()
        {
            // Listen ya tuvo éxito. Falta que la SubScene termine de cargar el prefab.
            while (ServerExists() && !SpawnerReady()) yield return new WaitForSecondsRealtime(0.5f);
            bool ready = false;
            while (ServerExists() && !ready)
            {
                yield return Post("/ready", success => ready = success);
                if (!ready) yield return new WaitForSecondsRealtime(2f);
            }

            if (ready) Debug.Log("NFE_AGONES: Ready accepted");
        }

        private IEnumerator ReportHealth()
        {
            // Los latidos también cubren la carga inicial y reintentan si el sidecar inicia después.
            while (ServerExists())
            {
                yield return Post("/health", success =>
                {
                    if (success && (!healthReported || healthFailed))
                    {
                        Debug.Log("NFE_AGONES: Health accepted");
                        healthReported = true;
                    }
                    else if (!success && !healthFailed)
                    {
                        Debug.LogWarning("NFE_AGONES: waiting for REST SDK; health will retry");
                    }

                    healthFailed = !success;
                });
                yield return new WaitForSecondsRealtime(2f);
            }
        }

        private bool ServerExists()
        {
            return serverWorld != null && serverWorld.IsCreated;
        }

        private bool SpawnerReady()
        {
            using EntityQuery query = serverWorld.EntityManager.CreateEntityQuery(
                ComponentType.ReadOnly<NfePlayerSpawner>());
            query.CompleteDependency();
            if (query.IsEmptyIgnoreFilter) return false;
            Entity prefab = query.GetSingleton<NfePlayerSpawner>().PlayerPrefab;
            return serverWorld.EntityManager.Exists(prefab)
                && serverWorld.EntityManager.HasComponent<NfePlayerState>(prefab);
        }

        private IEnumerator Post(string path, Action<bool> completed)
        {
            using UnityWebRequest request = new(sdkUrl + path, "POST");
            request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes("{}"));
            request.downloadHandler = new DownloadHandlerBuffer();
            request.timeout = 2;
            request.SetRequestHeader("Content-Type", "application/json");
            yield return request.SendWebRequest();
            completed(request.result == UnityWebRequest.Result.Success);
        }
    }
}
