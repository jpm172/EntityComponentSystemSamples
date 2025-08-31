using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using Random = UnityEngine.Random;

public class ItemManager : MonoBehaviour
{
    public static ItemManager Instance;
    private EntityManager _entityManager;

    private Entity _playerEntity;
    
    private int _itemKey;
    
    [SerializeField]
    private List<ItemData> _itemDatabase;
    
    [SerializeField]
    private List<ItemData> _itemsToLoad;

    private Dictionary<int, ItemData> _itemDictionary;
    
    
   private void Awake()
    {
        if ( Instance == null )
        {
            Instance = this;
        }
        else
        {
            Destroy( Instance );
        }

        
        _itemDictionary = new Dictionary<int, ItemData>();
        foreach ( ItemData data in _itemDatabase )
        {
            _itemDictionary.Add( data.ItemID, data );
        }
        
        World world = World.DefaultGameObjectInjectionWorld;
        _entityManager = world.EntityManager;
        
    }

   private void Start()
   {
       _entityManager.CreateEntityQuery( typeof( PlayerInputs ) )
           .TryGetSingletonEntity<Entity>(out _playerEntity);
       
       LoadItems();
       
   }


   private void LoadItems()
   {
       NativeArray<InventoryItem> newItems = new NativeArray<InventoryItem>(_itemsToLoad.Count, Allocator.Temp);
       for ( int i = 0; i < _itemsToLoad.Count; i++ )
       {
           ItemData data = _itemsToLoad[i];
           Entity newItem = CreateItemEntity( GetItemByID( data.ItemID ) );
           newItems[i] = new InventoryItem{Item = newItem};
       }
       
       DynamicBuffer<InventoryItem> invBuffer = _entityManager.GetBuffer<InventoryItem>( _playerEntity );
       invBuffer.AddRange( newItems );
       

   }


   private Entity CreateItemEntity(ItemData data)
   { 
       Entity itemEntity = _entityManager.CreateEntity();
#if UNITY_EDITOR
       _entityManager.SetName( itemEntity, data.ItemName );
#endif
       
       _entityManager.AddComponentData(itemEntity, new CharacterItemData(_playerEntity, data.ItemID, data.EquipTime, 1, _itemKey));
       _entityManager.AddComponentData( itemEntity, new PlayerItemData() );
       _entityManager.AddComponentData( itemEntity, new DestroyItem() );
       _entityManager.SetComponentEnabled( itemEntity, typeof(DestroyItem), false );

       if ( data.ItemType == ItemType.Weapon )
       {
           AddWeaponComponents( data, itemEntity );
       }
       else if ( data.ItemType == ItemType.Health )
       {
           AddHealthItemComponents( data, itemEntity );
       }

       _itemKey++;
       return itemEntity;
   }


   private void AddWeaponComponents(ItemData data, Entity itemEntity)
   {
       WeaponDesc weaponComponent = WeaponDataToComponent( (WeaponItemData) data );
       weaponComponent.CurrentAmmo = Random.Range( 0, weaponComponent.MaxAmmo + 1 );
       _entityManager.AddComponentData(itemEntity, weaponComponent );
   }

   private void AddHealthItemComponents( ItemData data, Entity itemEntity )
   {
       HealthItemData healthItemData = (HealthItemData) data;
       
       HealthItemDesc healthItemComponent = HealthItemDataToComponent( healthItemData);
       healthItemComponent.CurrentCharges = healthItemComponent.MaxCharges ;
       _entityManager.AddComponentData(itemEntity, healthItemComponent);
       
       if ( healthItemData.Type == HealthItemType.HealthKit )
       {
           _entityManager.AddComponentData( itemEntity, new HealthKitInfo() );
       }
   }
   
   
   private WeaponDesc WeaponDataToComponent(WeaponItemData data)
   {
       
       float fireRate = 1 / data.FireRate;
       WeaponDesc newWeapon = new WeaponDesc
       {
           Type = WeaponType.Gun,
           AmmoType = data.AmmoType,
           ReloadProfile = data.ReloadProfile,
           BulletsPerShot = data.BulletsPerShot,
           MaxAmmo = data.MaxAmmo,
           FireRate = fireRate,
           WeaponSpread = data.WeaponSpread,
           Recoil = data.Recoil,
           Penetration = data.Penetration,
           Range = data.Range,
       };
        
       return newWeapon;
   }
   
   private HealthItemDesc HealthItemDataToComponent(HealthItemData data)
   {
        
       HealthItemDesc newHealthItem = new HealthItemDesc
       {
           MaxCharges = data.MaxCharges,
           HealTime = data.HealTime,
           ChargesPerHeal = data.ChargesPerHeal,
           Type = data.Type
       };
        
       return newHealthItem;
   }

   public static ItemData GetItemByID( int id )
   {
       if(Instance._itemDictionary.TryGetValue( id, out ItemData data ))
       {
           return data;
       }

       Debug.LogError( "Item ID not found in Database!" );
       
       return null;
   }
   
}
