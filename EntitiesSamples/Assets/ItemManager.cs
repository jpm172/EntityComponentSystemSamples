using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

public class ItemManager : MonoBehaviour
{
    public static ItemManager Instance;
    private EntityManager _entityManager;

    private Entity _playerEntity;
    
    [SerializeField]
    private List<ItemData> _itemsToLoad;
    //private List<WeaponItemData> _loadWeapons;
    
    
    
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
       foreach ( ItemData data in _itemsToLoad )
       {
           Entity newItem = CreateItemEntity( data );
       }
   }


   private Entity CreateItemEntity(ItemData data)
   { 
       Entity itemEntity = _entityManager.CreateEntity();
#if UNITY_EDITOR
       _entityManager.SetName( itemEntity, data.ItemName );
#endif
        
       //_entityManager.AddComponentData(itemEntity, weaponInfo.Weapon);
       _entityManager.AddComponentData(itemEntity, new CharacterItemData(_playerEntity, data.ItemID, data.EquipTime, 1, 0));
       return itemEntity;
   }
   
}
