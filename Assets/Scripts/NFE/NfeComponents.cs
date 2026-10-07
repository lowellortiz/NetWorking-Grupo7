using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using UnityEngine;

namespace NetworkingLab.NFE
{
    public struct NfePlayerTag : IComponentData
    {
    }

    public struct NfePlayerState : IComponentData
    {
        [GhostField] public int PlayerNumber;
        [GhostField(Quantization = 1000)] public float2 Position;
    }

    public struct NfePlayerInput : IInputComponentData
    {
        public float2 Move;
        public InputEvent PowerUp;
    }

    public sealed class NfePlayerAuthoring : MonoBehaviour
    {
        private sealed class NfePlayerBaker : Baker<NfePlayerAuthoring>
        {
            public override void Bake(NfePlayerAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.None);
                AddComponent<NfePlayerTag>(entity);
                AddComponent<NfePlayerState>(entity);
                AddComponent<NfePlayerInput>(entity);
            }
        }
    }

    public struct NfePlayerSpawner : IComponentData
    {
        public Entity PlayerPrefab;
    }

    public sealed class NfePlayerSpawnerAuthoring : MonoBehaviour
    {
        [SerializeField] private GameObject playerPrefab;

        public void Configure(GameObject prefab)
        {
            playerPrefab = prefab;
        }

        private sealed class NfePlayerSpawnerBaker : Baker<NfePlayerSpawnerAuthoring>
        {
            public override void Bake(NfePlayerSpawnerAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new NfePlayerSpawner
                {
                    PlayerPrefab = GetEntity(authoring.playerPrefab, TransformUsageFlags.Dynamic)
                });
            }
        }
    }
}
