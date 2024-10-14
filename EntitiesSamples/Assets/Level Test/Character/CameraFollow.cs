using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial class CameraFollowSystem : SystemBase
{

    private EntityQuery playerQuery;
    private static float3 offset = new float3(0,0, -10);
    private Transform _playerTracker;
    
    
    protected override void OnCreate()
    {
        playerQuery = SystemAPI.QueryBuilder().WithAll<PlayerInputs, LocalTransform>().Build();
        _playerTracker = GameObject.FindGameObjectWithTag( "Player" ).transform;
        RequireForUpdate( playerQuery );
    }

    protected override void OnUpdate()
    {
        LocalTransform t = playerQuery.GetSingleton<LocalTransform>();
        _playerTracker.position = t.Position;
        //Camera.main.transform.position = t.Position + offset;

    }
}
