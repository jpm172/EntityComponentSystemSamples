using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

public partial class CubeSystem : SystemBase
{
    protected override void OnUpdate()
    {
        float deltaTime = SystemAPI.Time.DeltaTime;

        foreach ( var (cubeData, tranform) in SystemAPI.Query<RefRO<CubeData>, RefRW<LocalTransform>>() )
        {
            tranform.ValueRW = tranform.ValueRO.RotateY( math.radians(cubeData.ValueRO.speed * deltaTime) );
        }
    }
}
