using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

public class CubeFieldSystem : SystemBase
{
    //
    protected override void OnCreate()
    {
        Debug.Log( "onCreate" );
        RequireForUpdate<CubeFieldConfig>();
    }
    
    protected override void OnUpdate()
    {
        if ( Input.GetKeyDown( KeyCode.Space ) )
        {
            CubeFieldConfig config = SystemAPI.GetSingleton<CubeFieldConfig>();

            for ( int i = 0; i < config.count; i++ )
            {
                Entity spawnedEntity = EntityManager.Instantiate( config.cubePrefab );
                EntityManager.SetComponentData( spawnedEntity, new LocalTransform
                {
                    Position = new float3(UnityEngine.Random.Range( -10,10 ), UnityEngine.Random.Range( -10,10 ), 0)
                } );
            }
            
            
        }
    }
}
