using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class PlayerAuthoring : MonoBehaviour
{

    public float MoveSpeed;
    public float DestroyRadius = 2;
    public class PlayerBaker : Baker<PlayerAuthoring>
    {
        public override void Bake(PlayerAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            
            AddComponent(entity, new MyCharacterComponent
            {
                MovementSpeed = authoring.MoveSpeed
            });
            
            AddComponent(entity, new WeaponInfo
            {
                DestroyRadius = authoring.DestroyRadius,
                FireRate = 1
            });
            
            AddComponent<PlayerInputs>(entity);
            
        }
    }
}
