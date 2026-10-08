using Unity.Entities;
using UnityEngine;

namespace NetworkingLab.NFE
{
    [DisallowMultipleComponent]
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
                if (authoring.playerPrefab == null)
                {
                    Debug.LogError("NFE: falta asignar NFEPlayer al spawner de la SubScene.", authoring);
                    return;
                }

                Entity entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new NfePlayerSpawner
                {
                    PlayerPrefab = GetEntity(authoring.playerPrefab, TransformUsageFlags.Dynamic)
                });
            }
        }
    }
}
