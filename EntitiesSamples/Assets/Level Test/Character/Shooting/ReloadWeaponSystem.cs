using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public partial struct ReloadWeaponSystem : ISystem
{

    public void OnCreate( ref SystemState state )
    {
        
    }

    public void OnDestroy( ref SystemState state )
    {
        
    }

    public void OnUpdate( ref SystemState state )
    {
        foreach ( var ( input, inventory, player) in SystemAPI.Query< RefRO<PlayerInputs>, RefRW<CharacterInventory>>().WithEntityAccess() )
        {

            if ( !input.ValueRO.Reload )
                continue;
            
            Entity equippedItem = inventory.ValueRW.EquippedItem;
            if(!state.EntityManager.HasComponent( equippedItem,typeof(WeaponDesc) ))
                continue;
            
            WeaponDesc weapon = state.EntityManager.GetComponentData<WeaponDesc>( equippedItem );
            AmmoInfo ammoBox = inventory.ValueRW.Ammo;
            int reloadAmt = math.min(ammoBox.GetAmmo( weapon.AmmoType ), weapon.MaxAmmo - weapon.CurrentAmmo);
            weapon.CurrentAmmo += reloadAmt;
            ammoBox.RemoveAmmo( weapon.AmmoType, reloadAmt );
            
            state.EntityManager.SetComponentData( equippedItem, weapon );
            inventory.ValueRW.Ammo = ammoBox;
        }
    }
}
