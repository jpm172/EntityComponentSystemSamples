using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;

public class PlayerActionTracker : MonoBehaviour
{

    [SerializeField]
    private Image _equippingMeter,
        _indicator;
    

    private bool equipActive;
    private bool reloadActive;
    
    private EntityManager _entityManager;
    private Entity _playerEntity;
    
    void Start()
    {
        _entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        _entityManager.CreateEntityQuery( typeof( PlayerInputs ) )
            .TryGetSingletonEntity<Entity>(out _playerEntity);
    }

    // Update is called once per frame
    void Update()
    {
        CharacterInventory inventory = _entityManager.GetComponentData<CharacterInventory>( _playerEntity );
        
        ItemSwitchIndicator( inventory );
        ReloadIndicator( inventory );
        
    }

    private void ReloadIndicator( CharacterInventory inventory )
    {
        if ( inventory.EquippedItem == Entity.Null )
        {
            reloadActive = false;
            return;
        }


        if ( !_entityManager.HasComponent( inventory.EquippedItem, typeof( WeaponDesc ) ) )
        {
            reloadActive = false;
            return;
        }
            

        WeaponDesc weapon = _entityManager.GetComponentData<WeaponDesc>( inventory.EquippedItem );

        if ( weapon.ReloadProfile.ReloadState == ReloadState.Ready )
        {
            reloadActive = false;
            return;
        }

        reloadActive = true;
        _equippingMeter.color = Color.red;
        _equippingMeter.fillAmount = 1- (weapon.ReloadProfile.ReloadRemaining / weapon.ReloadProfile.ReloadTimer);

    }

    private void ItemSwitchIndicator(CharacterInventory inventory)
    {
        if( inventory.SwitchToItem != EquippingData.Null)
        {
            if ( !_indicator.enabled )
                _indicator.enabled = true; 
            UpdateMeterEvenSplit( inventory );
        }
        else
        {
            if ( _indicator.enabled )
                _indicator.enabled = false;
            _equippingMeter.fillAmount = 0;
        }
    }

    private void UpdateMeterEvenSplit(CharacterInventory inventory)
    {

        float holsterTime = GetItemEquipTime( inventory.EquippedItem );
        float equipTime = GetItemEquipTime( inventory.SwitchToItem.SwitchTo );
        _equippingMeter.color = Color.white;
        
        if (  inventory.Remaining < holsterTime )
        {
            if ( holsterTime <= math.EPSILON )
                _equippingMeter.fillAmount = 0.5f;
            else
                _equippingMeter.fillAmount = (inventory.Remaining / holsterTime) *0.5f;
        }
        else
        {
            if ( equipTime <= math.EPSILON )
                _equippingMeter.fillAmount = 1;
            else
                _equippingMeter.fillAmount = 0.5f + (((inventory.Remaining- holsterTime) / equipTime) *0.5f);
        }
    }
    
    private void UpdateMeterProportional(CharacterInventory inventory)
    {

        float holsterTime = GetItemEquipTime( inventory.EquippedItem );
        float equipTime = GetItemEquipTime( inventory.SwitchToItem.SwitchTo );
        float totalTime = holsterTime + equipTime;

        float frac = 0;
        if ( equipTime + holsterTime > math.EPSILON )
            frac = holsterTime / (equipTime + holsterTime );
        
        //_indicatorRect.rotation = Quaternion.Euler( 0,0,360f * frac );
        
        _equippingMeter.fillAmount = inventory.Remaining / inventory.Timer;
    }
    
    private float GetItemEquipTime( Entity entity )
    {
        if ( entity == Entity.Null )
            return 0;

        CharacterItemData itemData = _entityManager.GetComponentData<CharacterItemData>( entity );
        return itemData.EquipTime;
    }
    
    
}
