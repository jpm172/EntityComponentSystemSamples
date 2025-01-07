using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

public class FuseAuthoring : MonoBehaviour
{
    public float Timer;
    public float ExplosionRadius;
    public float Penetration;
    public float Drag;
    public class FuzeBaker : Baker<FuseAuthoring>
    {
        public override void Bake( FuseAuthoring authoring )
        {
            Entity entity = GetEntity(TransformUsageFlags.None);
            
            AddComponent(entity, new Fuze
            {
                Timer = authoring.Timer,
                ExplosionRadius = authoring.ExplosionRadius,
                Penetration = authoring.Penetration
            });
            AddComponent(entity, new ProjectileInfo
            {
                Drag = authoring.Drag
            });
        }
    }
}

public struct Fuze : IComponentData
{
    public float Timer;
    public float ExplosionRadius;
    public float Penetration;
}
