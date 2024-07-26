using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;

public class CubeFieldConfigAuthoring : MonoBehaviour
{
    public GameObject cubePrefab;
    public int2 dimensions;

    public class Baker : Baker<CubeFieldConfigAuthoring>
    {
        public override void Bake( CubeFieldConfigAuthoring authoring )
        {
            Entity entity = GetEntity(TransformUsageFlags.None);
            AddComponent(entity, new CubeFieldConfig
            {
                cubePrefab = GetEntity(authoring.cubePrefab, TransformUsageFlags.Dynamic),
                dimensions = authoring.CalcuateDimensions()//authoring.dimensions
            });
        }
    }

    private int2 CalcuateDimensions()
    {
        Random.seed = 1;
        return new int2(Random.Range( 1,4 ),Random.Range( 1,4 ));
    }
}

public struct CubeFieldConfig : IComponentData
{
    public Entity cubePrefab;
    public int2 dimensions;
}