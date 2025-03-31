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
                MovementSpeed = authoring.MoveSpeed,
                Health = authoring.Health,
                MaxHealth = authoring.Health,
            });

            DynamicBuffer<InventoryElement> invBuffer =AddBuffer<InventoryElement>( entity );

            for ( int i = 0; i < 9; i++ )
            {
                invBuffer.Add( new InventoryElement() );
            }
            
            
            AddComponent(entity, new CharacterInventory { });
            
            AddBuffer<CharacterDebuff>( entity );

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
