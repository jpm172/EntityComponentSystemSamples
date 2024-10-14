using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial class CameraFollowSystem : SystemBase
{

    private EntityQuery playerQuery;
    private static float3 offset = new float3(0,0, -10);

    protected override void OnCreate()
    {
        playerQuery = SystemAPI.QueryBuilder().WithAll<PlayerInputs, LocalTransform>().Build();
        RequireForUpdate( playerQuery );
    }

    protected override void OnUpdate()
    {
        LocalTransform t = playerQuery.GetSingleton<LocalTransform>();
        Camera.main.transform.position = t.Position + offset;

    }
}
