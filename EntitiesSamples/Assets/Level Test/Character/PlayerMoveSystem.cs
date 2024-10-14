using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.CharacterController;
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

[BurstCompile]
public partial struct PlayerMoveJob : IJobEntity
{
    public float DeltaTime;
    private static readonly float AngleAdjust = math.radians( 90 );

    private void Execute(ref LocalTransform transform, in PlayerInputs input, MyCharacterComponent attributes)
    {
        
        transform.Position.xy += input.MoveInput * attributes.MovementSpeed * DeltaTime;
        //float2 targetPosition = transform.Position.xy + input.MoveInput * attributes.MovementSpeed;
        //velocity = math.lerp(velocity, targetVelocity, MathUtilities.GetSharpnessInterpolant(interpolationSharpness, deltaTime));
        
        //rotate the character to look at the mouse
        float3 forward = input.AimPosition - transform.Position;
        quaternion rotation = quaternion.LookRotationSafe(transform.Forward(), forward );
        
        transform.Rotation = rotation;
        transform = transform.RotateZ( AngleAdjust );

    }
    
    
}
