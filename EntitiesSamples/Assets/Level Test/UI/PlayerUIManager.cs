using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Events;

public class PlayerUIManager : MonoBehaviour
{
    public static PlayerUIManager Instance;

    private int _itemKey;

    public PanelManager ActivePanel;
    
    [SerializeField]
    private GameObject _gearLayer;
    [SerializeField]
    private GameObject _healthLayer;
    [SerializeField]
    private HotbarManager _hotBar;

    [SerializeField]
    private GameObject _panelsParent;

    public BodyHealthManager _bodyManager;

    [SerializeField]
    private int _playerMaxHealth;
    [SerializeField]
    private float _playerCurrentHealth;

    [SerializeField]
    private float _playerBleedRate;
    
    [SerializeField]
    private List<WeaponItemData> _loadWeapons;
    
    [SerializeField]
    private List<HealthItemData> _loadHealthItems;
    
    //[SerializeField]
    //private List<WeaponItemInfo> _weaponItems;
    //[SerializeField]
    //private List<HealthItemInfo> _healthItems;
    [SerializeField]
    private List<ItemData> _equipmentItems;

    [SerializeField]
    private ItemInfo[] _serializedItems;
    
    private Dictionary<int, ItemInfo> _allItemsDict;
    private Dictionary<int, WeaponItemInfo> _weaponDict;
    //private Dictionary<int, HealthItemInfo> _healthItemDict;
    private List<int> _healthItemKeys;
    
    private EntityManager _entityManager;
    private Entity _playerEntity;
    
    
    //public List<WeaponItemInfo> WeaponItems => _weaponItems;
    public Dictionary<int, ItemInfo> AllItems => _allItemsDict;
    public Dictionary<int, WeaponItemInfo> WeaponItems => _weaponDict;
    //public Dictionary<int, HealthItemInfo> HealthItems => _healthItemDict;
    public List<int> HealthItemKeys => _healthItemKeys;

    public List<ItemData> EquipmentItems => _equipmentItems;
    //public List<HealthItemInfo> HealthItems => _healthItems;

    public float PlayerCurrentHealth
    {
        get => _playerCurrentHealth;
        set => _playerCurrentHealth = Math.Max(0,value);
    }

    public int PlayerMaxHealth
    {
        get => _playerMaxHealth;
        set => _playerMaxHealth = value;
    }

    public float PlayerBleedRate
    {
        get => _playerBleedRate;
        set => _playerBleedRate = Math.Max(0, value);
    }

    public UnityEvent ItemUpdateEvent;

    public void SerializeItems()
    {
        _serializedItems = new ItemInfo[_allItemsDict.Values.Count];
        _allItemsDict.Values.CopyTo( _serializedItems, 0 );
    }

    public void AddWound()
    {
        _bodyManager.AddWoundECS();
    }
    
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

        _playerMaxHealth = 300;
        _playerCurrentHealth = _playerMaxHealth;
        
        
        _allItemsDict = new Dictionary<int, ItemInfo>();
        
        _weaponDict = new Dictionary<int, WeaponItemInfo>();
        for ( int i = 0; i < _loadWeapons.Count; i++ )
        {
            WeaponItemInfo newWeapon = new WeaponItemInfo( _loadWeapons[i], _itemKey );
            newWeapon.Order = i;
            newWeapon.Weapon.CurrentAmmo = UnityEngine.Random.Range( 0, newWeapon.Weapon.MaxAmmo + 1 );
            
            _weaponDict.Add( _itemKey,  newWeapon );
            _allItemsDict.Add( _itemKey, newWeapon );
            //_weaponItems.Add( new WeaponItemInfo(_loadWeapons[i], _itemKey) );
            _itemKey++;
        }
        
        _healthItemKeys = new List<int>();
        for ( int i = 0; i < _loadHealthItems.Count; i++ )
        {
            if ( _loadHealthItems[i].Stackable )
            {
                bool foundItem = false;
                foreach ( int key in _healthItemKeys )
                {
                    if ( _allItemsDict[key].Data.ItemID == _loadHealthItems[i].ItemID )
                    {
                        _allItemsDict[key].Quantity++;
                        foundItem = true;
                        break;
                    }
                }

                if ( !foundItem )
                {
                    HealthItemInfo newHealth = new HealthItemInfo( _loadHealthItems[i], _itemKey );
                    newHealth.HealthItem.CurrentCharges =  UnityEngine.Random.Range( 0, newHealth.HealthItem.MaxCharges + 1 );
                    newHealth.Order = i;
            
                    //_healthItemDict.Add( _itemKey, newHealth );
                    _healthItemKeys.Add( _itemKey );
                    _allItemsDict.Add(_itemKey,  newHealth  );
                    _itemKey++;
                }
            }
            else
            {
                //_healthItems.Add( new HealthItemInfo( _loadHealthItems[i], _itemKey ) );
                HealthItemInfo newHealth = new HealthItemInfo( _loadHealthItems[i], _itemKey );
                //newHealth.HealthItem.CurrentCharges =  UnityEngine.Random.Range( 0, newHealth.HealthItem.MaxCharges + 1 );
                newHealth.HealthItem.CurrentCharges =  newHealth.HealthItem.MaxCharges;
                //newHealth.HealthItem.CurrentCharges =  5;
                newHealth.Order = i;
            
                //_healthItemDict.Add( _itemKey, newHealth );
                _healthItemKeys.Add( _itemKey );
                _allItemsDict.Add(_itemKey,  newHealth  );
                _itemKey++;
            }
        }
        
    }

    private void Start()
    {
        _entityManager.CreateEntityQuery( typeof( PlayerInputs ) )
            .TryGetSingletonEntity<Entity>(out _playerEntity);
    }

    public void NewWoundECS(CharacterWound newWound)
    {
        _bodyManager.AddWoundECS(newWound);
    }

    public void HealedWoundECS(int woundIndex)
    {
        _bodyManager.RemoveWoundECS( woundIndex );
    }
    
    public void EquipSlot( int equipIndex )
    {

        DynamicBuffer<InventoryElement> invBuffer = _entityManager.GetBuffer<InventoryElement>( _playerEntity );
        CharacterInventory inventory = _entityManager.GetComponentData<CharacterInventory>( _playerEntity );

        if ( invBuffer[equipIndex].Item == Entity.Null )
        {
            if ( inventory.EquippedItem != Entity.Null )
                inventory.Timer = _entityManager.GetComponentData<CharacterItemData>( inventory.EquippedItem ).EquipTime;
            
            inventory.SwitchToItem = Entity.Null;
            _entityManager.SetComponentData( _playerEntity, inventory );
            return;
        }
        
        CharacterItemData itemData = _entityManager.GetComponentData<CharacterItemData>( invBuffer[equipIndex].Item );

        bool newSwitchEquipped = inventory.Timer <= 0 && inventory.EquippedItem == invBuffer[equipIndex].Item;
        //if already equipping this item, dont reset the timer
        if ( inventory.SwitchToItem == invBuffer[equipIndex].Item || newSwitchEquipped )
            return;
        
        inventory.SwitchToItem = invBuffer[equipIndex].Item;
        inventory.Timer = itemData.EquipTime;

        if ( inventory.EquippedItem != Entity.Null )
            inventory.Timer += _entityManager.GetComponentData<CharacterItemData>( inventory.EquippedItem ).EquipTime;
        
        _entityManager.SetComponentData( _playerEntity, inventory );
    }

    public void AddItemEntity(ItemInfo item, int equipIndex, bool equip)
    {

        ItemType itemType = item.Data.ItemType;
        if ( itemType == ItemType.Weapon )
        {
            Entity itemEntity = CreateWeaponEntity( (WeaponItemInfo) item );
            DynamicBuffer<InventoryElement> invBuffer = _entityManager.GetBuffer<InventoryElement>( _playerEntity );
            invBuffer.ElementAt( equipIndex ).Item = itemEntity;

            if ( equip )
            {
                CharacterInventory playerInv = _entityManager.GetComponentData<CharacterInventory>( _playerEntity );
                //playerInv.EquippedItem = itemEntity;
                playerInv.SwitchToItem = invBuffer[equipIndex].Item;
                playerInv.Timer = item.Data.EquipTime;
                _entityManager.SetComponentData( _playerEntity, playerInv );
            }
            
        }
        else if ( itemType == ItemType.Health )
        {
            Entity itemEntity = CreateHealthItemEntity( (HealthItemInfo) item );
            DynamicBuffer<InventoryElement> invBuffer = _entityManager.GetBuffer<InventoryElement>( _playerEntity );
            invBuffer.ElementAt( equipIndex ).Item = itemEntity;
            if ( equip )
            {
                CharacterInventory playerInv = _entityManager.GetComponentData<CharacterInventory>( _playerEntity );
                //playerInv.EquippedItem = itemEntity;
                playerInv.SwitchToItem = invBuffer[equipIndex].Item;
                playerInv.Timer = item.Data.EquipTime;
                _entityManager.SetComponentData( _playerEntity, playerInv );
            }
        }
    }

    public void RemoveItemEntity( int removeIndex, bool unequip )
    {
        bool hasPlayer = _entityManager.CreateEntityQuery( typeof( PlayerInputs ) )
            .TryGetSingletonEntity<Entity>(out Entity player);
        if ( !hasPlayer )
            return;

        if ( unequip )
        {
            CharacterInventory playerInv = _entityManager.GetComponentData<CharacterInventory>( player );
            playerInv.EquippedItem = Entity.Null;
            _entityManager.SetComponentData( player, playerInv );
        }
        
        DynamicBuffer<InventoryElement> invBuffer = _entityManager.GetBuffer<InventoryElement>( player );
        Entity removedItem = invBuffer.ElementAt( removeIndex ).Item;
        invBuffer.ElementAt( removeIndex ).Item = Entity.Null;
        _entityManager.DestroyEntity( removedItem );
        
    }

    public void SwapItemEntities( int index1, int index2 )
    {
        DynamicBuffer<InventoryElement> invBuffer = _entityManager.GetBuffer<InventoryElement>( _playerEntity );
        CharacterInventory inventory = _entityManager.GetComponentData<CharacterInventory>( _playerEntity );

        float timer = 0;
        
        InventoryElement swap = invBuffer[index1];
        invBuffer.ElementAt( index1 ) = invBuffer[index2];
        invBuffer.ElementAt( index2 ) = swap;

        if ( invBuffer[index1].Item != Entity.Null )
            timer += _entityManager.GetComponentData<CharacterItemData>( invBuffer[index1].Item ).EquipTime;
        
        if ( invBuffer[index2].Item != Entity.Null )
            timer += _entityManager.GetComponentData<CharacterItemData>( invBuffer[index2].Item ).EquipTime;


        int equippedIndex = _hotBar.GetEquippedIndex();
        if ( equippedIndex >= 0 && equippedIndex == index1 )
        {
            inventory.SwitchToItem = invBuffer[index1].Item;
            inventory.Timer = timer;
            inventory.Remaining = 0;
        }
        else if (equippedIndex >= 0 && equippedIndex == index2)
        {
            
            inventory.SwitchToItem = invBuffer[index2].Item;
            inventory.Timer = timer;
            inventory.Remaining = 0;
        }

        _entityManager.SetComponentData( _playerEntity, inventory );

    }
    
    public void QuickUseItem(HealthItemInfo healthItem, BodyPart healPart)
    {
        CharacterInventory playerInv = _entityManager.GetComponentData<CharacterInventory>( _playerEntity );
        Entity itemEntity;
        bool inHotBar = _hotBar.ContainsItem( healthItem.Key, out int result );
        if ( inHotBar )
        {
            DynamicBuffer<InventoryElement> invBuffer = _entityManager.GetBuffer<InventoryElement>( _playerEntity );
            itemEntity = invBuffer[result].Item;
        }
        else
        {
            itemEntity = CreateHealthItemEntity( healthItem );
        }
        
        QuickUseData quickData = new QuickUseData
        {
            PreviousEquipped = playerInv.EquippedItem,
            Part = healPart,
            InHotBar = inHotBar
        };
        _entityManager.AddComponentData( itemEntity, quickData );
        
         
        playerInv.SwitchToItem = itemEntity;
        playerInv.Timer = healthItem.Data.EquipTime;
        _entityManager.SetComponentData( _playerEntity, playerInv );
        
    }

    private Entity CreateWeaponEntity( WeaponItemInfo weaponInfo )
    {
        
        Entity itemEntity = _entityManager.CreateEntity();
#if UNITY_EDITOR
        _entityManager.SetName( itemEntity, weaponInfo.Data.ItemName );
#endif

        _entityManager.AddComponentData(itemEntity, weaponInfo.Weapon);
        _entityManager.AddComponentData(itemEntity, new CharacterItemData(weaponInfo.Data.EquipTime, 1, weaponInfo.Key));

        return itemEntity;
    }
    
    private Entity CreateHealthItemEntity( HealthItemInfo itemInfo )
    {
        Entity itemEntity = _entityManager.CreateEntity();
#if UNITY_EDITOR
        _entityManager.SetName( itemEntity, itemInfo.Data.ItemName );
#endif
        
        
        _entityManager.AddComponentData(itemEntity, itemInfo.HealthItem);
        _entityManager.AddComponentData(itemEntity, new CharacterItemData(itemInfo.Data.EquipTime, itemInfo.Quantity, itemInfo.Key));

        return itemEntity;
    }
    
    //set update == true whenever using them internally (like the Heal All action) so that
    //all items are properly updated, but when using items directly (drag and drop), 
    //it will be handled by the DragObject, and there is no need to update all the other items
    public void RemoveItem(int key, bool update)
    {
        _hotBar.TryRemoveFromHotBar( key );
        
        ItemInfo removedItem = _allItemsDict[key];
        if ( removedItem.GetType() == typeof(HealthItemInfo) )
        {
            _healthItemKeys.Remove( removedItem.Key );
            _allItemsDict.Remove( removedItem.Key );
        }
        
        if(update)
            ItemUpdateEvent.Invoke();
    }

    public void UpdateItem( HealthItemDesc item, CharacterItemData itemData)
    {
        HealthItemInfo updatedItem = (HealthItemInfo)_allItemsDict[itemData.Key];
        updatedItem.HealthItem = item;
        updatedItem.Quantity = itemData.Quantity;
        
        ItemUpdateEvent.Invoke();
    }
    
    public void ToggleInventory()
    {
        bool value = !_panelsParent.activeInHierarchy;
        _panelsParent.SetActive( value );
         _hotBar.HoldOpen = value;
    }
    
    public void OpenGear()
    {
        ActivePanel = _gearLayer.GetComponent<PanelManager>();
        _gearLayer.SetActive( true );
        _healthLayer.SetActive( false );
    }

    public void OpenHealth()
    {
        ActivePanel = _healthLayer.GetComponent<PanelManager>();
        _healthLayer.SetActive( true );
        _gearLayer.SetActive( false );
    }
}
