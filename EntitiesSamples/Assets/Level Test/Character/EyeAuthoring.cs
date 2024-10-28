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
    public float EdgeDistanceThreshold;
    public float CutAway;
    public int ResolveIterations;
    
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
                ViewDistance = authoring.ViewDistance,
                EdgeDistanceThreshold = authoring.EdgeDistanceThreshold,
                ResolveIterations = authoring.ResolveIterations,
                CutAway = authoring.CutAway
            } );
            
            AddComponent(entity, new InitializeTag());
            //SetComponentEnabled<InitializeTag>( entity, false ); doesnt have the functionality i want for RequireForUpdate
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
    public float EdgeDistanceThreshold;
    public int ResolveIterations;
    public float CutAway;
}
