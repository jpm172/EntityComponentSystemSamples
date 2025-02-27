using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class PlayerAuthoring : MonoBehaviour
{

    public float MoveSpeed;
    public float Health = 350;
    public float Blood = 5000;
    public class PlayerBaker : Baker<PlayerAuthoring>
    {
        public override void Bake(PlayerAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            
            AddComponent(entity, new MyCharacterComponent
            {
                MovementSpeed = authoring.MoveSpeed
            });

            DynamicBuffer<InventoryElement> invBuffer =AddBuffer<InventoryElement>( entity );

            for ( int i = 0; i < 9; i++ )
            {
                invBuffer.Add( new InventoryElement() );
            }
            
            
            AddComponent(entity, new CharacterInventory
            {
                
            });
            /*
            AddComponent(entity, new WeaponInfo
            {
                Type = authoring.WeaponType,
                IsExplosion = authoring.IsExplosion,
                ThrowForce = authoring.ThrowForce,
                ExplosionRadius = authoring.ExplosionRadius,
                Range = authoring.WeaponRange,
                Penetration = authoring.Penetration,
                WeaponSpread = authoring.WeaponSpread,
                BulletsPerShot = authoring.BulletsPerShot,
                FireRate = authoring.FireRate
            });
            */
            
            AddComponent<PlayerInputs>(entity);
            
        }
    }
}
