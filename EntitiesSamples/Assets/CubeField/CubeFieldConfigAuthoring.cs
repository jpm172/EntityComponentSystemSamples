using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

public class CubeFieldConfigAuthoring : MonoBehaviour
{
    public GameObject cubePrefab;
    public int count;
    
    
    public class Baker : Baker<CubeFieldConfigAuthoring>
    {
        public override void Bake( CubeFieldConfigAuthoring authoring )
        {
            Entity entity = GetEntity(TransformUsageFlags.None);
            AddComponent(entity, new CubeFieldConfig
            {
                cubePrefab = GetEntity(authoring.cubePrefab, TransformUsageFlags.Dynamic),
                count = authoring.count
            });
        }
    }
}

public struct CubeFieldConfig : IComponentData
{
    public Entity cubePrefab;
    public int count;
}