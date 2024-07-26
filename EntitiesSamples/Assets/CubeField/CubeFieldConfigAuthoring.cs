using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

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
        return new int2(5,10);
    }
}

public struct CubeFieldConfig : IComponentData
{
    public Entity cubePrefab;
    public int2 dimensions;
}