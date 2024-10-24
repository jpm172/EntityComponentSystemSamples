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

    public GameObject EyePrefab;
    
    public class EyeBaker : Baker<EyeAuthoring>
    {
        public override void Bake(EyeAuthoring authoring)
        {
            Entity entity = GetEntity(authoring.EyePrefab, TransformUsageFlags.Renderable);
            
            World world = World.DefaultGameObjectInjectionWorld;
            EntityManager entityManager = world.EntityManager;
            entityManager.Instantiate( entity );
            /*
            var world = World.DefaultGameObjectInjectionWorld;
            var entityManager = world.EntityManager;
            
            var matList = new List<Material>();
            matList.Add( authoring.EyeMaterial );
            
            var desc = new RenderMeshDescription(
                shadowCastingMode: ShadowCastingMode.Off,
                receiveShadows: false);

            var renderMeshArray = new RenderMeshArray(matList.ToArray(), new[] { authoring.Mesh });
            
            RenderMeshUtility.AddComponents(
                entity,
                entityManager,
                desc,
                renderMeshArray,
                MaterialMeshInfo.FromRenderMeshArrayIndices(0, 0));
                */
            
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
