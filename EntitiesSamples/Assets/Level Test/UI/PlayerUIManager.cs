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
    
    private Dictionary<int, ItemInfo> _allItemsDict;
    private Dictionary<int, WeaponItemInfo> _weaponDict;
    private Dictionary<int, HealthItemInfo> _healthItemDict;
    
    
    //public List<WeaponItemInfo> WeaponItems => _weaponItems;
    public Dictionary<int, ItemInfo> AllItems => _allItemsDict;
    public Dictionary<int, WeaponItemInfo> WeaponItems => _weaponDict;
    public Dictionary<int, HealthItemInfo> HealthItems => _healthItemDict;

    public List<ItemData> EquipmentItems => _equipmentItems;
    //public List<HealthItemInfo> HealthItems => _healthItems;

    
    
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
            _weaponDict.Add( _itemKey,  new WeaponItemInfo(_loadWeapons[i], _itemKey) );
            _allItemsDict.Add( _itemKey, new WeaponItemInfo(_loadWeapons[i], _itemKey) );
            //_weaponItems.Add( new WeaponItemInfo(_loadWeapons[i], _itemKey) );
            _itemKey++;
        }
        
        //_healthItems = new List<HealthItemInfo>(_loadHealthItems.Count);
        _healthItemDict = new Dictionary<int, HealthItemInfo>();
        for ( int i = 0; i < _loadHealthItems.Count; i++ )
        {
            //_healthItems.Add( new HealthItemInfo( _loadHealthItems[i], _itemKey ) );
            _healthItemDict.Add( _itemKey,  new HealthItemInfo(_loadHealthItems[i], _itemKey) );
            _allItemsDict.Add(_itemKey,  new HealthItemInfo(_loadHealthItems[i], _itemKey)  );
            _itemKey++;
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
