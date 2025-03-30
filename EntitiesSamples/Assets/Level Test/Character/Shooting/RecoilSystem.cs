using System;
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
    private static readonly float4 RecoilBounds = new float4(-2, -0.5f, 2, 0.5f);
    
    private void Execute( ref LocalTransform transform, ref PlayerInputs input )
    {
        float3 forward = math.normalizesafe( input.AimPosition - transform.Position );
        float distance = math.distance( transform.Position, input.AimPosition );
        
        //float3 targetRecoilPos = math.rotate( quaternion.RotateZ( math.radians( -input.TargetRecoilAngle ) ), forward );

        float snap =  math.min(math.sqrt( input.TimeSinceShot ), 1);

        if ( input.RecoilTimer > 0 )
        {
            snap = math.min(math.pow( input.TimeSinceShot, 0.25f ), 1);
        }
        
        input.RecoilAngle = math.lerp( input.RecoilAngle, input.TargetRecoilAngle, snap );
        
        float3 recoilPos = math.rotate( quaternion.RotateZ( math.radians( -input.RecoilAngle ) ), forward );

        //obviously, recoil angle of 90 will then cause a division by 0 
        float hypotenus = distance / math.cos( math.radians( input.RecoilAngle ) );
        float3 pos = transform.Position + ( recoilPos * hypotenus );

        input.RecoilOffset =  pos - input.AimPosition;
        float recovery = math.min( math.pow( input.TimeSinceShot, 4 ), 1 );
        
        input.TargetRecoilAngle = math.lerp( input.TargetRecoilAngle, 0, recovery );
        input.TimeSinceShot += DeltaTime;
        input.RecoilTimer -= DeltaTime;
        
        
        DrawAim( transform, input );
        DrawRecoil( pos );
       
    }


    private void DrawAim(LocalTransform transform, PlayerInputs input)
    {
        float3 forward = math.normalizesafe( input.AimPosition - transform.Position );
        float3 targetRecoilPos = math.rotate( quaternion.RotateZ( math.radians( -input.TargetRecoilAngle ) ), forward );
        float3 recoilPos = math.rotate( quaternion.RotateZ( math.radians( -input.RecoilAngle ) ), forward );
        
        
        float3 right = math.rotate( quaternion.RotateZ( math.radians( 90 ) ), forward );
        Debug.DrawLine( transform.Position, transform.Position + forward * 40, Color.green);
        Debug.DrawLine( transform.Position, transform.Position + targetRecoilPos  * 40 , Color.blue);
        Debug.DrawLine( transform.Position, transform.Position + recoilPos  * 40 , Color.red);
        Debug.DrawLine( input.AimPosition, input.AimPosition + right * 40, Color.black );
        Debug.DrawLine( input.AimPosition, input.AimPosition - right * 40, Color.black );
    }


    private void DrawBounds(PlayerInputs input, float3 forward, float4 scaledBounds)
    {
        float3 b1 = math.rotate( quaternion.RotateZ( math.radians( 90 ) ), forward ) * scaledBounds.x;
        b1 -= forward * scaledBounds.y;
        
        float3 b2 = math.rotate( quaternion.RotateZ( math.radians( 90 ) ), forward ) * scaledBounds.z;
        b2 -= forward * scaledBounds.y;
        
        Debug.DrawLine( input.AimPosition + b1, input.AimPosition + b1 - 2*forward *scaledBounds.w, Color.white, 0.1f );
        Debug.DrawLine( input.AimPosition + b2, input.AimPosition + b2 - 2*forward *scaledBounds.w, Color.white, 0.1f );
        Debug.DrawLine( input.AimPosition + b1, input.AimPosition + b2, Color.white, 0.1f );
        Debug.DrawLine( input.AimPosition + b1 - 2*forward *scaledBounds.w, input.AimPosition + b2 - 2*forward *scaledBounds.w, Color.white, 0.1f );
    }


    private void Iteration2( ref LocalTransform transform, ref PlayerInputs input )
    {
         float boundsScale = math.distance( transform.Position, input.AimPosition ) / 5;
        boundsScale = math.clamp( boundsScale, 0.2f, 1 );
        float4 scaledBounds = RecoilBounds * boundsScale;

        float speed = math.max( 0.5f - input.TimeSinceShot, input.TimeSinceShot ); // / math.max(0.5f, boundsScale);
        input.RecoilValue = MoveTowards( input.RecoilValue, input.TargetRecoilValue, speed );


        float3 forward = math.normalizesafe( transform.Position-input.AimPosition );

        float3 targetPosition = math.rotate( quaternion.RotateZ( math.radians( 90 ) ), forward ) * input.RecoilValue.x;
        targetPosition -= forward * input.RecoilValue.y;

        input.RecoilOffset = targetPosition * boundsScale;

        float3 right = math.rotate( quaternion.RotateZ( math.radians( 90 ) ), forward );
        Debug.DrawLine( input.AimPosition, input.AimPosition + forward, Color.green, 0.1f );
        Debug.DrawLine( input.AimPosition, input.AimPosition + right, Color.red, 0.1f );
        Debug.DrawLine( input.AimPosition, input.AimPosition - right, Color.blue, 0.1f );
        
        DrawBounds( input, forward, scaledBounds );
       
        
        /*
        float3 b1 = math.rotate( quaternion.RotateZ( math.radians( 90 ) ), forward ) * RecoilBounds.x;
        b1 -= forward * RecoilBounds.y;
        
        float3 b2 = math.rotate( quaternion.RotateZ( math.radians( 90 ) ), forward ) * RecoilBounds.z;
        b2 -= forward * RecoilBounds.y;
        
        Debug.DrawLine( input.AimPosition + b1, input.AimPosition + b1 - 2*forward *RecoilBounds.w, Color.white, 0.1f );
        Debug.DrawLine( input.AimPosition + b2, input.AimPosition + b2 - 2*forward *RecoilBounds.w, Color.white, 0.1f );
        Debug.DrawLine( input.AimPosition + b1, input.AimPosition + b2, Color.white, 0.1f );
        Debug.DrawLine( input.AimPosition + b1 - 2*forward *RecoilBounds.w, input.AimPosition + b2 - 2*forward *RecoilBounds.w, Color.white, 0.1f );
        */
        
        DrawRecoil( input, input.RecoilOffset);
        
        input.TargetRecoilValue = Vector3.MoveTowards( input.TargetRecoilValue, float3.zero, input.TimeSinceShot/2 );
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
        Debug.DrawLine( center + new float3(-size, -size, 0) , center+ new float3(size, size, 0) , Color.yellow );
        Debug.DrawLine( center + new float3(-size, size, 0) , center+ new float3(size, -size, 0) , Color.yellow );
        
    }
    
    private void DrawRecoil( float3 pos)
    {
        float3 center = pos;
        float size = 0.25f;
        Debug.DrawLine( center + new float3(-size, -size, 0) , center+ new float3(size, size, 0) , Color.yellow );
        Debug.DrawLine( center + new float3(-size, size, 0) , center+ new float3(size, -size, 0) , Color.yellow );
        
        
        
    }

}
