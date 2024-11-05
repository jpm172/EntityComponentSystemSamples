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
    public float RelativeAngle;
    public float ViewDistance;
    public float EdgeDistanceThreshold;
    public float CutAway;
    public float Hardness;
    public float Strength;
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
                Hardness = authoring.Hardness,
                Strength = authoring.Strength,
                RelativeAngle = authoring.RelativeAngle,
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
    public float RelativeAngle;
    public float ViewDistance;
    public float EdgeDistanceThreshold;
    public int ResolveIterations;
    public float CutAway;

    public float Hardness;
    public float Strength;

    //public unsafe Vector3* Pointer;
    //public BlobPtr<Vector3> Pointer;
}
