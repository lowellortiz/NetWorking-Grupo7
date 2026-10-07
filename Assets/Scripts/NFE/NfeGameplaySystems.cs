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
            // STUDENT TODO NFE-02: mark the connection InGame and send NfeGoInGameRequest.
            // Use an EntityCommandBuffer and SendRpcCommandRequest targeted at the connection.
        }
    }

    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    public partial struct NfeGoInGameServerSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<NfePlayerSpawner>();
            EntityQueryBuilder builder = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<NfeGoInGameRequest, ReceiveRpcCommandRequest>();
            state.RequireForUpdate(state.GetEntityQuery(builder));
        }

        public void OnUpdate(ref SystemState state)
        {
            // STUDENT TODO NFE-02: receive NfeGoInGameRequest, instantiate PlayerPrefab,
            // assign GhostOwner/PlayerNumber, and link the player to its connection.
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

    [UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
    public partial struct NfeMovementSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            // STUDENT TODO NFE-03: for simulated ghosts, apply NfePlayerInput.Move to
            // NfePlayerState.Position with DeltaTime and the baseline movement bounds.
        }
    }

    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    [UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
    [UpdateAfter(typeof(NfeMovementSystem))]
    public partial struct NfePowerUpServerSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            // STUDENT TODO NFE-04: detect InputEvent PowerUp on the server and broadcast
            // NfePowerUpResultRpc containing the replicated PlayerNumber.
        }
    }

    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    public partial struct NfePowerUpClientSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            // STUDENT TODO NFE-04: consume NfePowerUpResultRpc on clients, raise
            // PowerUpEvents, then destroy the received RPC entity.
        }
    }
}
