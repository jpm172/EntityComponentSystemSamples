using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class PlayerAuthoring : MonoBehaviour
{

    public float MoveSpeed;
    public float ExplosionRadius = 2;
    public float WeaponRange = 20;
    public float Penetration = 10;
    public int BulletsPerShot = 1;
    public float FireRate = 0.5f;
    public float WeaponSpread;
    public bool IsExplosion;
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
                IsExplosion = authoring.IsExplosion,
                ExplosionRadius = authoring.ExplosionRadius,
                Range = authoring.WeaponRange,
                Penetration = authoring.Penetration,
                WeaponSpread = authoring.WeaponSpread,
                BulletsPerShot = authoring.BulletsPerShot,
                FireRate = authoring.FireRate
            });
            
            AddComponent<PlayerInputs>(entity);
            
        }
    }
}
