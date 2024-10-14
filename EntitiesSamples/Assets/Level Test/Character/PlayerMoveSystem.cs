using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

[UpdateBefore(typeof(TransformSystemGroup))]
public partial struct PlayerMoveSystem : ISystem
{
    public void OnCreate( ref SystemState state )
    {
        
    }

    public void OnDestroy( ref SystemState state )
    {
        
    }

    public void OnUpdate( ref SystemState state )
    {
        var deltaTime = SystemAPI.Time.DeltaTime;
        new PlayerMoveJob
        {
            DeltaTime = deltaTime
        }.Schedule();
    }
}

//[BurstCompile]
public partial struct PlayerMoveJob : IJobEntity
{
    public float DeltaTime;

    private void Execute(ref LocalTransform transform, in PlayerInputs input, MyCharacterComponent attributes)
    {
        
        transform.Position.xy += input.MoveInput * attributes.MovementSpeed * DeltaTime;
        
        float3 forward = input.AimPosition - transform.Position;



        //quaternion rotation = TransformHelpers.LookAtRotation( transform.Position, transform.Position + input.Forward, input.Up );
        //quaternion rotation = quaternion.LookRotationSafe(forward, input.Up );
        
        
        //quaternion rotation = quaternion.LookRotationSafe(transform.Forward(), forward ); //works!
        quaternion rotation = quaternion.LookRotationSafe(transform.Forward(), forward ); 
        
        //quaternion rotation = quaternion.LookRotationSafe(input.Forward, input.Up );
        

        transform.Rotation = rotation;
        transform = transform.RotateZ( math.radians(90 ) );



        //transform = transform.RotateZ( 1*DeltaTime );

    }
    
    
}
