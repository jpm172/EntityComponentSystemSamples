using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerUIManager : MonoBehaviour
{
    [SerializeField]
    private GameObject _gearLayer;
    [SerializeField]
    private GameObject _healthLayer;

    [SerializeField]
    private List<WeaponItemData> _weaponItems;
    [SerializeField]
    private List<HealthItemData> _healthItems;
    [SerializeField]
    private List<ItemData> _equipmentItems;

    public List<WeaponItemData> WeaponItems => _weaponItems;
    public List<ItemData> EquipmentItems => _equipmentItems;
    public List<HealthItemData> HealthItems => _healthItems;

    private void Start()
    {
        
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
