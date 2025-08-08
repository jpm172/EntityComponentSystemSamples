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
    
    private int _itemKey;
    
    [SerializeField]
    private List<ItemData> _itemDatabase;
    
    [SerializeField]
    private List<ItemData> _itemsToLoad;
    //private List<WeaponItemData> _loadWeapons;

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
       foreach ( ItemData data in _itemsToLoad )
       {
           Entity newItem = CreateItemEntity( GetItemByID( data.ItemID ) );
       }
   }


   private Entity CreateItemEntity(ItemData data)
   { 
       Entity itemEntity = _entityManager.CreateEntity();
#if UNITY_EDITOR
       _entityManager.SetName( itemEntity, data.ItemName );
#endif
        
       //_entityManager.AddComponentData(itemEntity, weaponInfo.Weapon);
       _entityManager.AddComponentData(itemEntity, new CharacterItemData(_playerEntity, data.ItemID, data.EquipTime, 1, _itemKey));
       _itemKey++;
       return itemEntity;
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
