using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public partial struct RecoilSystem : ISystem
{

    public void OnCreate( ref SystemState state )
    {
        
    }

    public void OnDestroy( ref SystemState state )
    {
        
    }

    public void OnUpdate( ref SystemState state )
    {
        new RecoilJob
        {
            
        }.Schedule();
    }
}

public partial struct RecoilJob : IJobEntity
{


    private void Execute( ref LocalTransform transform, ref PlayerInputs input )
    {
        input.RecoilOffset = new float3(1,0,0);
        float3 forward = math.normalizesafe( transform.Position-input.AimPosition );

        
        float3 right = math.rotate( quaternion.RotateZ( math.radians( 90 ) ), forward );
        float3 left = math.rotate( quaternion.RotateZ( math.radians( -90 ) ), forward );
        
        
        Debug.DrawLine( input.AimPosition, input.AimPosition + forward, Color.green, 0.1f );
        Debug.DrawLine( input.AimPosition, input.AimPosition + right, Color.red, 0.1f );
        Debug.DrawLine( input.AimPosition, input.AimPosition + left, Color.blue, 0.1f );

        //Debug.DrawLine( input.AimPosition, right, Color.red, 0.1f );

        
        
        
    }

}
