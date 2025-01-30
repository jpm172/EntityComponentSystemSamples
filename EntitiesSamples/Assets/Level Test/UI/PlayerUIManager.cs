using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerUIManager : MonoBehaviour
{
    public static PlayerUIManager Instance;

    private int _itemKey;
    
    [SerializeField]
    private GameObject _gearLayer;
    [SerializeField]
    private GameObject _healthLayer;

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
    
    
    
    //public List<WeaponItemInfo> WeaponItems => _weaponItems;
    public Dictionary<int, ItemInfo> AllItems => _allItemsDict;
    public Dictionary<int, WeaponItemInfo> WeaponItems => _weaponDict;
    //public Dictionary<int, HealthItemInfo> HealthItems => _healthItemDict;
    public List<int> HealthItemKeys => _healthItemKeys;

    public List<ItemData> EquipmentItems => _equipmentItems;
    //public List<HealthItemInfo> HealthItems => _healthItems;

    public void SerializeItems()
    {
        _serializedItems = new ItemInfo[_allItemsDict.Values.Count];
        _allItemsDict.Values.CopyTo( _serializedItems, 0 );
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
        
        _allItemsDict = new Dictionary<int, ItemInfo>();

        //_weaponItems = new List<WeaponItemInfo>(_loadWeapons.Count);
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
        
        //_healthItemDict = new Dictionary<int, HealthItemInfo>();
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
                newHealth.HealthItem.CurrentCharges =  UnityEngine.Random.Range( 0, newHealth.HealthItem.MaxCharges + 1 );
                newHealth.Order = i;
            
                //_healthItemDict.Add( _itemKey, newHealth );
                _healthItemKeys.Add( _itemKey );
                _allItemsDict.Add(_itemKey,  newHealth  );
                _itemKey++;
            }
        }
        
    }

    public void RemoveItem(int key)
    {
        ItemInfo removedItem = _allItemsDict[key];
        if ( removedItem.GetType() == typeof(HealthItemInfo) )
        {
            //_healthItemDict.Remove( removedItem.Key );
            _healthItemKeys.Remove( removedItem.Key );
            _allItemsDict.Remove( removedItem.Key );
        }
    }
    

    public void OpenGear()
    {
        _gearLayer.SetActive( true );
        _healthLayer.SetActive( false );
    }

    public void OpenHealth()
    {
        _healthLayer.SetActive( true );
        _gearLayer.SetActive( false );
    }
}
