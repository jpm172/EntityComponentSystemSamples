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

            
            
            Entity equippedItem = inventory.ValueRW.EquippedItem;
            if(!state.EntityManager.HasComponent( equippedItem,typeof(WeaponDesc) ))
                continue;
            
            WeaponDesc weapon = state.EntityManager.GetComponentData<WeaponDesc>( equippedItem );


            if ( CanReload( input.ValueRO, weapon, inventory.ValueRO )   )
            {
                weapon.ReloadProfile.ReloadState = ReloadState.Reloading;
                weapon.ReloadProfile.ReloadRemaining = weapon.ReloadProfile.ReloadTimer;
            }
            
            if(weapon.ReloadProfile.ReloadState == ReloadState.Ready)
                continue;

            //cancel reload
            if ( input.ValueRO.Shoot || inventory.ValueRO.Switching )//
            {
                weapon.ReloadProfile.ReloadRemaining = 0;
                weapon.ReloadProfile.ReloadState = ReloadState.Ready;
                state.EntityManager.SetComponentData( inventory.ValueRW.EquippedItem, weapon );
                continue;
            }

            switch ( weapon.ReloadProfile.ReloadType )
            {
                case ReloadType.Magazine:
                    ReloadMaganize(weapon, inventory, ref state);
                    break;
                case ReloadType.Manual:
                    ReloadManual(weapon, inventory, ref state);
                    break;
            }
        }
    }

    ///Can initiate reload if:
    /// Pressing Reload
    /// Not already reloading
    /// gun is not fully loaded
    /// has at least 1 bullet of the gun's ammo type
    private bool CanReload(PlayerInputs input, WeaponDesc weapon, CharacterInventory inventory)
    {
        return input.Reload && weapon.ReloadProfile.ReloadState == ReloadState.Ready &&
               weapon.CurrentAmmo < weapon.MaxAmmo && inventory.Ammo.GetAmmo( weapon.AmmoType ) > 0;
    }

    private void ReloadMaganize(WeaponDesc weapon, RefRW<CharacterInventory> inventory, ref SystemState state )
    {
        weapon.ReloadProfile.ReloadRemaining -= SystemAPI.Time.DeltaTime;

        if ( weapon.ReloadProfile.ReloadRemaining <= 0 )
        {
            AmmoInfo ammo = inventory.ValueRW.Ammo;
            int reloadAmt = math.min(ammo.GetAmmo( weapon.AmmoType ), weapon.MaxAmmo - weapon.CurrentAmmo);
            weapon.CurrentAmmo += reloadAmt;
            ammo.RemoveAmmo( weapon.AmmoType, reloadAmt );
            weapon.ReloadProfile.ReloadState = ReloadState.Ready;
            
            inventory.ValueRW.Ammo = ammo;
        }
        
        state.EntityManager.SetComponentData( inventory.ValueRW.EquippedItem, weapon );
        
    }

    private void ReloadManual(WeaponDesc weapon, RefRW<CharacterInventory> inventory, ref SystemState state)
    {
        weapon.ReloadProfile.ReloadRemaining -= SystemAPI.Time.DeltaTime;

        if ( weapon.ReloadProfile.ReloadRemaining <= 0 )
        {
            AmmoInfo ammo = inventory.ValueRW.Ammo;
            if ( ammo.GetAmmo( weapon.AmmoType ) == 0 )
            {
                weapon.ReloadProfile.ReloadState = ReloadState.Ready;
                return;
            }
            weapon.CurrentAmmo++;
            ammo.RemoveAmmo( weapon.AmmoType, 1 );
            
            if ( weapon.CurrentAmmo >= weapon.MaxAmmo )
            {
                weapon.ReloadProfile.ReloadState = ReloadState.Ready;
            }
            else
            {
                weapon.ReloadProfile.ReloadRemaining = weapon.ReloadProfile.ReloadTimer;
            }
            
            inventory.ValueRW.Ammo = ammo;
            
        }
        
        state.EntityManager.SetComponentData( inventory.ValueRW.EquippedItem, weapon );
    }
    
}
