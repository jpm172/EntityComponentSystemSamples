using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

public class EyeAuthoring : MonoBehaviour
{
    public float Resolution;
    public float FOV;
    public float ViewDistance;
    
    public class EyeBaker : Baker<EyeAuthoring>
    {
        public override void Bake(EyeAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            var world = World.DefaultGameObjectInjectionWorld;

            AddComponent( entity, new EyeComponent
            {
                Resolution = authoring.Resolution,
                FOV = authoring.FOV,
                ViewDistance = authoring.ViewDistance
            } );
        }
    }
}

public struct EntityPrefabComponent : IComponentData
{
    public Entity Value;
}

public struct EyeComponent : IComponentData
{
    public float Resolution;
    public float FOV;
    public float ViewDistance;
    
}
