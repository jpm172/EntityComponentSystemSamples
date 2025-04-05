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
                Health = authoring.Health,
                MaxHealth = authoring.Health,
            });
            
            CharacterStats baseStats = new CharacterStats
            {
                Armor = 0,
                MoveSpeed = authoring.MoveSpeed,
                HeadStats = new LimbStats(1,1),
                ChestStats = new LimbStats(1,1),
                LeftArmStats = new LimbStats(1,1),
                RightArmStats = new LimbStats(1,1),
                LeftLegStats = new LimbStats(1,1),
                RightLegStats = new LimbStats(1,1),
            };
            
            AddComponent(entity, new BaseStats{Stats = baseStats});
            AddComponent(entity, new TotalStats());

            DynamicBuffer<InventoryElement> invBuffer =AddBuffer<InventoryElement>( entity );

            for ( int i = 0; i < 9; i++ )
            {
                invBuffer.Add( new InventoryElement() );
            }
            
            
            AddComponent(entity, new CharacterInventory { });
            
            AddBuffer<TimedStatusEffect>( entity );

            AddComponent<PlayerInputs>(entity);
            AddComponent<RecoilData>(entity);
            AuthorHealth( entity, authoring );
        }


        private void AuthorHealth(Entity entity, PlayerAuthoring authoring)
        {

            AddBuffer<DamageInfo>( entity );
            AddBuffer<CharacterWound>( entity );
            
            
            DynamicBuffer<CharacterLimb> invBuffer = AddBuffer<CharacterLimb>( entity );
            invBuffer.Add( new CharacterLimb( BodyPart.Head, 50 ) );
            invBuffer.Add( new CharacterLimb( BodyPart.Chest, 0 ) );
            invBuffer.Add( new CharacterLimb( BodyPart.LeftArm, 50 ) );
            invBuffer.Add( new CharacterLimb( BodyPart.RightArm, 50 ) );
            invBuffer.Add( new CharacterLimb( BodyPart.LeftLeg, 50 ) );
            invBuffer.Add( new CharacterLimb( BodyPart.RightLeg, 50 ) );
        }
        
    }
}
