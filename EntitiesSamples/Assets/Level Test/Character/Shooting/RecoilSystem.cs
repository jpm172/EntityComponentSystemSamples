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
        input.RecoilOffset = new float3(-4,0,0);
        float3 relativeRecoil = input.RecoilOffset;
        float3 forward = math.normalizesafe( transform.Position-input.AimPosition );
        
        
        
        float3 right = math.rotate( quaternion.RotateZ( math.radians( 90 ) ), forward );


        float relativeAngle = math.radians( 90 * math.sign( input.RecoilOffset.x ));
        relativeRecoil = math.rotate( quaternion.RotateZ( relativeAngle ), forward  )* math.length( input.RecoilOffset );

        Debug.DrawLine( input.AimPosition, input.AimPosition + forward, Color.green, 0.1f );
        Debug.DrawLine( input.AimPosition, input.AimPosition + right, Color.red, 0.1f );
        Debug.DrawLine( input.AimPosition, input.AimPosition - right, Color.blue, 0.1f );


        DrawRecoil( input, relativeRecoil );
        
        
        
    }

    private void DrawRecoil(PlayerInputs input, float3 pos)
    {
        float3 center = input.AimPosition + pos;
        float size = 0.25f;
        Debug.DrawLine( center + new float3(-size, -size, 0) , center+ new float3(size, size, 0) , Color.yellow, 0.1f );
        Debug.DrawLine( center + new float3(-size, size, 0) , center+ new float3(size, -size, 0) , Color.yellow, 0.1f );
        
    }

}
