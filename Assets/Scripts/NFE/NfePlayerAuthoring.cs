using Unity.Entities;
using UnityEngine;

namespace NetworkingLab.NFE
{
    // Una clase MonoBehaviour por archivo permite guardar una referencia de script real.
    [DisallowMultipleComponent]
    public sealed class NfePlayerAuthoring : MonoBehaviour
    {
        private sealed class NfePlayerBaker : Baker<NfePlayerAuthoring>
        {
            public override void Bake(NfePlayerAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent<NfePlayerTag>(entity);
                AddComponent<NfePlayerState>(entity);
                AddComponent<NfePlayerInput>(entity);
            }
        }
    }
}
