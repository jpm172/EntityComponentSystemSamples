using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class PlayerAuthoring : MonoBehaviour
{

    public float MoveSpeed;
    public float Health = 300;
    public class PlayerBaker : Baker<PlayerAuthoring>
    {
        public override void Bake(PlayerAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            
            AddComponent(entity, new MyCharacterComponent
            {
                //Health = authoring.Health,
                //MaxHealth = authoring.Health,
            });
            
            
            StatInfo baseStats = new StatInfo
            {
                Health = authoring.Health,
                MaxHealth = authoring.Health,
                Armor = 0,
                MoveSpeed = authoring.MoveSpeed,
                HeadStats = new CharacterLimb( BodyPart.Head, 50 ),
                ChestStats = new CharacterLimb( BodyPart.Chest, 0 ),
                LeftArmStats = new CharacterLimb( BodyPart.LeftArm, 50 ),
                RightArmStats = new CharacterLimb( BodyPart.RightArm, 50 ),
                LeftLegStats = new CharacterLimb( BodyPart.LeftLeg, 50 ),
                RightLegStats = new CharacterLimb( BodyPart.RightLeg, 50 ),
            };
            
            AddComponent(entity, new CharacterStats{BaseStats = baseStats, TotalStats = baseStats});

            DynamicBuffer<InventoryItem> inventoryBuffer = AddBuffer<InventoryItem>( entity );
            DynamicBuffer<HotBarItem> hotbarBuffer =AddBuffer<HotBarItem>( entity );

            for ( int i = 0; i < 9; i++ )
            {
                hotbarBuffer.Add( new HotBarItem() );
            }
            
            
            AddComponent(entity, new CharacterInventory { Ammo = new AmmoInfo(100,100,50,20)});
            
            AddBuffer<StatusEffect>(entity);
            //AddBuffer<TimedStatusEffect>( entity );

            AddComponent<PlayerInputs>(entity);
            AddComponent<RecoilData>(entity);
            AuthorHealth( entity, authoring );
        }


        private void AuthorHealth(Entity entity, PlayerAuthoring authoring)
        {

            AddBuffer<DamageInfo>( entity );
            AddBuffer<CharacterWound>( entity );
            
            /*
            DynamicBuffer<CharacterLimb> invBuffer = AddBuffer<CharacterLimb>( entity );
            invBuffer.Add( new CharacterLimb( BodyPart.Head, 50 ) );
            invBuffer.Add( new CharacterLimb( BodyPart.Chest, 0 ) );
            invBuffer.Add( new CharacterLimb( BodyPart.LeftArm, 50 ) );
            invBuffer.Add( new CharacterLimb( BodyPart.RightArm, 50 ) );
            invBuffer.Add( new CharacterLimb( BodyPart.LeftLeg, 50 ) );
            invBuffer.Add( new CharacterLimb( BodyPart.RightLeg, 50 ) );
            */
        }
        
    }
}
