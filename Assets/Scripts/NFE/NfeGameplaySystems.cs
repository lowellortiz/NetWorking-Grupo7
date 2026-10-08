using NetworkingLab.PowerUp;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using UnityEngine.InputSystem;

namespace NetworkingLab.NFE
{
    public struct NfeGoInGameRequest : IRpcCommand { }

    public struct NfePowerUpResultRpc : IRpcCommand
    {
        public int PlayerNumber;
    }

    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ServerSimulation)]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [CreateAfter(typeof(RpcSystem))]
    public partial struct NfeRpcRegistrationSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            SystemAPI.GetSingletonRW<RpcCollection>().ValueRW.DynamicAssemblyList = true;
            state.Enabled = false;
        }
    }

    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    public partial struct NfeGoInGameClientSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<NfePlayerSpawner>();
            EntityQueryBuilder builder = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<NetworkId>().WithNone<NetworkStreamInGame>();
            state.RequireForUpdate(state.GetEntityQuery(builder));
        }

        public void OnUpdate(ref SystemState state)
        {
            // NFE-02: el cliente solicita entrar y habilita la recepción de Ghosts.
            using EntityCommandBuffer ecb = new(Allocator.Temp);
            foreach (var (_, connection) in SystemAPI.Query<RefRO<NetworkId>>()
                         .WithAll<NetworkStreamConnection>()
                         .WithNone<NetworkStreamInGame, NetworkStreamRequestDisconnect>()
                         .WithEntityAccess())
            {
                ecb.AddComponent<NetworkStreamInGame>(connection);
                Entity request = ecb.CreateEntity();
                ecb.AddComponent<NfeGoInGameRequest>(request);
                ecb.AddComponent(request, new SendRpcCommandRequest { TargetConnection = connection });
            }

            ecb.Playback(state.EntityManager);
        }
    }

    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    public partial struct NfeGoInGameServerSystem : ISystem
    {
        private ComponentLookup<NetworkId> networkIds;

        public void OnCreate(ref SystemState state)
        {
            networkIds = state.GetComponentLookup<NetworkId>(true);
            state.RequireForUpdate<NfePlayerSpawner>();
            EntityQueryBuilder builder = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<NfeGoInGameRequest, ReceiveRpcCommandRequest>();
            state.RequireForUpdate(state.GetEntityQuery(builder));
        }

        public void OnUpdate(ref SystemState state)
        {
            // NFE-02: solo el servidor asigna identidad, posición inicial y propietario.
            Entity prefab = SystemAPI.GetSingleton<NfePlayerSpawner>().PlayerPrefab;
            networkIds.Update(ref state);

            using EntityCommandBuffer ecb = new(Allocator.Temp);
            using NativeHashSet<Entity> accepted = new(4, Allocator.Temp);
            foreach (var (request, rpcEntity) in SystemAPI.Query<RefRO<ReceiveRpcCommandRequest>>()
                         .WithAll<NfeGoInGameRequest>().WithEntityAccess())
            {
                Entity connection = request.ValueRO.SourceConnection;
                ecb.DestroyEntity(rpcEntity);

                // Ignora conexiones cerradas y solicitudes repetidas, incluso en el mismo frame.
                if (!networkIds.HasComponent(connection)
                    || state.EntityManager.HasComponent<NetworkStreamRequestDisconnect>(connection)
                    || state.EntityManager.HasComponent<NetworkStreamInGame>(connection)
                    || !accepted.Add(connection))
                {
                    continue;
                }

                int networkId = networkIds[connection].Value;
                if (networkId <= 0)
                {
                    continue;
                }

                ecb.AddComponent<NetworkStreamInGame>(connection);
                Entity player = ecb.Instantiate(prefab);
                ecb.SetComponent(player, new GhostOwner { NetworkId = networkId });
                ecb.SetComponent(player, new NfePlayerState
                {
                    PlayerNumber = networkId,
                    Position = SpawnPosition(networkId)
                });
                // Al cerrar la conexión, NetCode destruye también su jugador.
                ecb.AppendToBuffer(connection, new LinkedEntityGroup { Value = player });
                UnityEngine.Debug.Log($"NFE_SPAWN: Player {networkId}");
            }

            ecb.Playback(state.EntityManager);
        }

        private static float2 SpawnPosition(int number)
        {
            switch ((number - 1) % 4)
            {
                case 0: return new float2(-5f, 3f);
                case 1: return new float2(5f, 3f);
                case 2: return new float2(-5f, -3f);
                default: return new float2(5f, -3f);
            }
        }
    }

    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    [UpdateInGroup(typeof(GhostInputSystemGroup))]
    public partial struct NfeGatherInputSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<NetworkStreamInGame>();
            state.RequireForUpdate<NfePlayerInput>();
        }

        public void OnUpdate(ref SystemState state)
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null) return;

            float2 movement = float2.zero;
            if (keyboard.aKey.isPressed) movement.x -= 1f;
            if (keyboard.dKey.isPressed) movement.x += 1f;
            if (keyboard.sKey.isPressed) movement.y -= 1f;
            if (keyboard.wKey.isPressed) movement.y += 1f;
            movement = math.normalizesafe(movement);
            bool powerUp = keyboard.spaceKey.wasPressedThisFrame;

            foreach (RefRW<NfePlayerInput> input in
                     SystemAPI.Query<RefRW<NfePlayerInput>>().WithAll<GhostOwnerIsLocal>())
            {
                input.ValueRW = default;
                input.ValueRW.Move = movement;
                if (powerUp) input.ValueRW.PowerUp.Set();
            }
        }
    }

    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ServerSimulation)]
    [UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
    public partial struct NfeMovementSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<NfePlayerInput>();
        }

        public void OnUpdate(ref SystemState state)
        {
            // NFE-03: la misma regla corre en el owner predicho y en el servidor.
            foreach (var (input, player) in SystemAPI
                         .Query<RefRO<NfePlayerInput>, RefRW<NfePlayerState>>()
                         .WithAll<NfePlayerTag, Simulate>())
            {
                float2 move = input.ValueRO.Move;
                if (!math.all(math.isfinite(move)))
                {
                    move = float2.zero;
                }

                float lengthSquared = math.lengthsq(move);
                if (lengthSquared > 1f)
                {
                    move *= math.rsqrt(lengthSquared);
                }

                float2 next = player.ValueRO.Position + move * (5f * SystemAPI.Time.DeltaTime);
                next.x = math.clamp(next.x, -10.5f, 10.5f);
                next.y = math.clamp(next.y, -7.5f, 7.5f);
                player.ValueRW.Position = next;
            }
        }
    }

    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    [UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
    [UpdateAfter(typeof(NfeMovementSystem))]
    public partial struct NfePowerUpServerSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<NetworkStreamInGame>();
            state.RequireForUpdate<NfePlayerInput>();
        }

        public void OnUpdate(ref SystemState state)
        {
            // NFE-04: InputEvent entrega el evento por tick; el servidor anuncia el resultado.
            using EntityCommandBuffer ecb = new(Allocator.Temp);
            foreach (var (input, player) in SystemAPI
                         .Query<RefRO<NfePlayerInput>, RefRO<NfePlayerState>>()
                         .WithAll<NfePlayerTag, Simulate>())
            {
                if (!input.ValueRO.PowerUp.IsSet || player.ValueRO.PlayerNumber <= 0)
                {
                    continue;
                }

                Entity rpc = ecb.CreateEntity();
                ecb.AddComponent(rpc, new NfePowerUpResultRpc
                {
                    PlayerNumber = player.ValueRO.PlayerNumber
                });
                // En Netcode 6.6, Entity.Null ya significa broadcast a las conexiones.
                // SendRpcCommandRequest no tiene el campo BroadcastTargets de la guía.
                ecb.AddComponent(rpc, new SendRpcCommandRequest { TargetConnection = Entity.Null });
            }

            ecb.Playback(state.EntityManager);
        }
    }

    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    public partial struct NfePowerUpClientSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<NfePowerUpResultRpc>();
        }

        public void OnUpdate(ref SystemState state)
        {
            // NFE-04: cada cliente publica su evento de UI y consume la entidad RPC una vez.
            using EntityCommandBuffer ecb = new(Allocator.Temp);
            foreach (var (result, rpcEntity) in SystemAPI.Query<RefRO<NfePowerUpResultRpc>>()
                         .WithAll<ReceiveRpcCommandRequest>().WithEntityAccess())
            {
                if (result.ValueRO.PlayerNumber > 0)
                {
                    PowerUpEvents.RaiseActivated($"Player {result.ValueRO.PlayerNumber}");
                }

                ecb.DestroyEntity(rpcEntity);
            }

            ecb.Playback(state.EntityManager);
        }
    }
}
