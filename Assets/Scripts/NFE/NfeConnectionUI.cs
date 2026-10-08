using System;
using System.Collections;
using Unity.Entities;
using Unity.NetCode;
using Unity.Networking.Transport;
using UnityEngine;
using UnityEngine.UI;

namespace NetworkingLab.NFE
{
    /// <summary>Conexión NFE explícita, con campos editables y arranque headless.</summary>
    public sealed class NfeConnectionUI : MonoBehaviour
    {
        public const ushort DefaultPort = 7980;

        [SerializeField] private InputField addressInput;
        [SerializeField] private InputField portInput;
        [SerializeField] private Text statusText;
        [SerializeField] private Text localPlayerText;

        private World listeningServer;
        private ushort listeningPort;
        private World connectionWorld;
        private Entity connectionEntity;
        private string connectedEndpoint;
        private bool connectionPending;
        private bool wasConnected;

        public void Configure(InputField address, InputField port, Text status, Text localPlayer)
        {
            addressInput = address;
            portInput = port;
            statusText = status;
            localPlayerText = localPlayer;
        }

        private IEnumerator Start()
        {
            if (addressInput != null) addressInput.text = ReadArgument("-address", "127.0.0.1");
            if (portInput != null) portInput.text = ReadArgument("-port", DefaultPort.ToString());
            SetLocalPlayer("Local player: none");
            SetStatus("Offline");

            // Espera a que los Worlds y sus singletons de transporte estén inicializados.
            yield return null;
#if UNITY_SERVER
            StartServer();
#else
            if (HasArgument("-client")) ConnectClient();
            else if (Application.isBatchMode) StartServer();
#endif
        }

        private void Update()
        {
            World client = ClientServerBootstrap.ClientWorld;
            if (client == null || !client.IsCreated) return;

            using EntityQuery query = client.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<NetworkId>());
            query.CompleteDependency();
            if (!query.IsEmptyIgnoreFilter)
            {
                int networkId = query.GetSingleton<NetworkId>().Value;
                SetLocalPlayer($"Local client ID: {networkId}");
                if (!wasConnected)
                {
                    SetStatus($"Connected to {connectedEndpoint}");
                    wasConnected = true;
                    connectionPending = false;
                }
            }
            else if (wasConnected || (connectionPending && !ConnectionExists()))
            {
                SetLocalPlayer("Local player: none");
                SetStatus("Disconnected. Check the server, Address and Port before reconnecting.");
                wasConnected = false;
                connectionPending = false;
            }
        }

        public void StartServer()
        {
            TryStartServer();
        }

        private bool TryStartServer()
        {
            World server = ClientServerBootstrap.ServerWorld;
            if (server == null || !server.IsCreated)
            {
                SetStatus("Server World is not available. Check NetCode PlayMode Tools.");
                return false;
            }

            if (!TryReadPort(out ushort port)) return false;
            if (listeningServer == server)
            {
                if (listeningPort != port)
                {
                    SetStatus($"Server already listens on UDP {listeningPort}. Stop Play to change it.");
                    return false;
                }
                return true;
            }

            // NFE-01: una consulta RW obtiene el singleton, no un componente sin entidad.
            using EntityQuery driverQuery = server.EntityManager.CreateEntityQuery(
                ComponentType.ReadWrite<NetworkStreamDriver>());
            if (driverQuery.IsEmptyIgnoreFilter)
            {
                SetStatus("Server transport is not ready yet. Try again after initialization.");
                return false;
            }

            driverQuery.CompleteDependency();
            bool listening = driverQuery.GetSingletonRW<NetworkStreamDriver>().ValueRW
                .Listen(NetworkEndpoint.AnyIpv4.WithPort(port));
            if (!listening)
            {
                SetStatus($"Could not listen on UDP {port}. Check whether another server uses it.");
                return false;
            }

            listeningServer = server;
            listeningPort = port;
            SetStatus($"Server listening on UDP {port}");
            NfeAgonesLifecycle lifecycle = GetComponent<NfeAgonesLifecycle>();
            if (lifecycle == null) lifecycle = gameObject.AddComponent<NfeAgonesLifecycle>();
            lifecycle.Begin(server);
            return true;
        }

        public void ConnectClient()
        {
            World client = ClientServerBootstrap.ClientWorld;
            if (client == null || !client.IsCreated)
            {
                SetStatus("Client World is not available in this play mode");
                return;
            }

            if (!TryReadPort(out ushort port)) return;
            string address = ReadAddress();
            if (!NetworkEndpoint.TryParse(address, port, out NetworkEndpoint endpoint, NetworkFamily.Ipv4))
            {
                SetStatus("Invalid Address. Enter an IPv4 address, for example 127.0.0.1.");
                return;
            }

            using EntityQuery connections = client.EntityManager.CreateEntityQuery(
                ComponentType.ReadOnly<NetworkStreamConnection>());
            if (!connections.IsEmptyIgnoreFilter)
            {
                if (wasConnected)
                    SetStatus($"Connected to {connectedEndpoint}");
                else if (connectionPending)
                    SetStatus($"Connecting to {connectedEndpoint}");
                else
                    SetStatus("Client is already connected or connecting");
                return;
            }

            // NFE-01: Connect inicia la conexión; NetworkId confirma el handshake después.
            using EntityQuery driverQuery = client.EntityManager.CreateEntityQuery(
                ComponentType.ReadWrite<NetworkStreamDriver>());
            if (driverQuery.IsEmptyIgnoreFilter)
            {
                SetStatus("Client transport is not ready yet. Try again after initialization.");
                return;
            }

            driverQuery.CompleteDependency();
            connectionEntity = driverQuery.GetSingletonRW<NetworkStreamDriver>().ValueRW
                .Connect(client.EntityManager, endpoint);
            connectionWorld = client;
            connectedEndpoint = $"{address}:{port}";
            connectionPending = connectionEntity != Entity.Null;
            wasConnected = false;
            SetStatus(connectionPending ? $"Connecting to {connectedEndpoint}" : "Could not create a connection");
        }

        public void StartHost()
        {
            if (!TryStartServer()) return;
            // El cliente del host está en el mismo proceso: debe usar loopback.
            if (addressInput != null) addressInput.text = "127.0.0.1";
            ConnectClient();
        }

        private bool ConnectionExists()
        {
            return connectionWorld != null && connectionWorld.IsCreated
                && connectionWorld.EntityManager.Exists(connectionEntity);
        }

        private string ReadAddress()
        {
            return addressInput != null ? addressInput.text.Trim() : ReadArgument("-address", "127.0.0.1");
        }

        private bool TryReadPort(out ushort port)
        {
            string value = portInput != null ? portInput.text : ReadArgument("-port", DefaultPort.ToString());
            if (ushort.TryParse(value, out port) && port > 0) return true;
            SetStatus("Invalid Port. Enter a number from 1 to 65535.");
            return false;
        }

        private void SetStatus(string value)
        {
            if (statusText != null) statusText.text = $"Status: {value}";
            Debug.Log($"NFE_STATUS: {value}");
        }

        private void SetLocalPlayer(string value)
        {
            if (localPlayerText != null) localPlayerText.text = value;
        }

        private static string ReadArgument(string name, string fallback)
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int index = 0; index < args.Length - 1; index++)
            {
                if (string.Equals(args[index], name, StringComparison.OrdinalIgnoreCase)) return args[index + 1];
            }
            return fallback;
        }

        private static bool HasArgument(string name)
        {
            return Array.Exists(Environment.GetCommandLineArgs(), value =>
                string.Equals(value, name, StringComparison.OrdinalIgnoreCase));
        }
    }
}
