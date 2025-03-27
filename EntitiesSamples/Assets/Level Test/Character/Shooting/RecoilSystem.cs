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
            DeltaTime = SystemAPI.Time.DeltaTime
        }.Schedule();
    }
}

public partial struct RecoilJob : IJobEntity
{

    public float DeltaTime;
    private void Execute( ref LocalTransform transform, ref PlayerInputs input )
    {
        float speed = math.max( 0.5f - input.TimeSinceShot, input.TimeSinceShot );//
        //float speed = math.abs(0.5f - input.TimeSinceShot);
        input.RecoilValue = MoveTowards( input.RecoilValue, input.TargetRecoilValue, speed );
        //input.RecoilValue = MoveTowards( input.RecoilValue, input.TargetRecoilValue, math.max( 0.5f - input.TimeSinceShot, 0.1f ) );
        //input.RecoilValue = MoveTowards( input.RecoilValue, input.TargetRecoilValue, 0.1f );
        //input.RecoilValue = Vector3.MoveTowards( input.RecoilValue, input.TargetRecoilValue, 0.1f );

        float3 forward = math.normalizesafe( transform.Position-input.AimPosition );

        float3 targetPosition = math.rotate( quaternion.RotateZ( math.radians( 90 ) ), forward ) * input.RecoilValue.x;
        targetPosition -= forward * input.RecoilValue.y;

        input.RecoilOffset = targetPosition;
        
        //input.RecoilOffset = math.rotate( quaternion.RotateZ( math.radians( 90) ), forward  ) * input.RecoilValue.x ;
        //input.RecoilOffset -= forward * input.RecoilValue.y;

        
        
        float3 right = math.rotate( quaternion.RotateZ( math.radians( 90 ) ), forward );
        Debug.DrawLine( input.AimPosition, input.AimPosition + forward, Color.green, 0.1f );
        Debug.DrawLine( input.AimPosition, input.AimPosition + right, Color.red, 0.1f );
        Debug.DrawLine( input.AimPosition, input.AimPosition - right, Color.blue, 0.1f );
        DrawRecoil( input, input.RecoilOffset);
        
        input.TargetRecoilValue = Vector3.MoveTowards( input.TargetRecoilValue, float3.zero, input.TimeSinceShot );
        //input.TargetRecoilValue = MoveTowards( input.TargetRecoilValue, float3.zero, input.TimeSinceShot );
        //input.TargetRecoilValue = MoveTowards( input.TargetRecoilValue, float3.zero, 0.1f );
        input.TimeSinceShot += DeltaTime;
    }

    private void Iteration1(ref LocalTransform transform, ref PlayerInputs input)
    {
        //input.RecoilValue = new float3(-1,1,0);
        float3 dir = math.normalizesafe(input.RecoilValue)*0.1f;
        
        
        float3 forward = math.normalizesafe( transform.Position-input.AimPosition );

        float3 targetPosition = math.rotate( quaternion.RotateZ( math.radians( 90 ) ), forward ) * input.RecoilValue.x;
        targetPosition -= forward * input.RecoilValue.y;

        input.RecoilOffset = targetPosition;
        
        //input.RecoilOffset = math.rotate( quaternion.RotateZ( math.radians( 90) ), forward  ) * input.RecoilValue.x ;
        //input.RecoilOffset -= forward * input.RecoilValue.y;

        
        
        float3 right = math.rotate( quaternion.RotateZ( math.radians( 90 ) ), forward );
        Debug.DrawLine( input.AimPosition, input.AimPosition + forward, Color.green, 0.1f );
        Debug.DrawLine( input.AimPosition, input.AimPosition + right, Color.red, 0.1f );
        Debug.DrawLine( input.AimPosition, input.AimPosition - right, Color.blue, 0.1f );
        DrawRecoil( input, input.RecoilOffset);

        input.RecoilValue -= dir;//
    }
    
    private static float3 MoveTowards(
        float3 current,
        float3 target,
        float maxDistanceDelta)
    {
        float num1 = target.x - current.x;
        float num2 = target.y - current.y;
        float num3 = target.z - current.z;
        float num4 = (float) ((double) num1 * (double) num1 + (double) num2 * (double) num2 + (double) num3 * (double) num3);
        if ((double) num4 == 0.0 || (double) maxDistanceDelta >= 0.0 && (double) num4 <= (double) maxDistanceDelta * (double) maxDistanceDelta)
            return target;
        float num5 = (float) math.sqrt((double) num4);
        return new float3(current.x + num1 / num5 * maxDistanceDelta, current.y + num2 / num5 * maxDistanceDelta, current.z + num3 / num5 * maxDistanceDelta);
    }

    private void DrawRecoil(PlayerInputs input, float3 pos)
    {
        float3 center = input.AimPosition + pos;
        float size = 0.25f;
        Debug.DrawLine( center + new float3(-size, -size, 0) , center+ new float3(size, size, 0) , Color.yellow, 0.1f );
        Debug.DrawLine( center + new float3(-size, size, 0) , center+ new float3(size, -size, 0) , Color.yellow, 0.1f );
        
    }

}
