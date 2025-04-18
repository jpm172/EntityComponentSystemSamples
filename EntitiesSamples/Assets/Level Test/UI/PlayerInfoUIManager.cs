using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UI;

public class PlayerInfoUIManager : MonoBehaviour
{
    private PlayerUIManager _manager;
    private EntityManager _entityManager;

    [SerializeField]
    private TextMeshProUGUI _ammoText;
    
    [SerializeField]
    private TextMeshProUGUI _bleedText;

    [SerializeField]
    private Image _healthMeter;
    
    private static Color _green = new Color(0.03921569f, 0.8352941f, 0.03921569f);
    private static Color _red = new Color(0.8352941f, 0.07843138f, 0.03529412f);
    
    void Start()
    {
        _manager = PlayerUIManager.Instance;
        _entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
    }

    // Update is called once per frame
    void Update()
    {
        if ( HasWeaponEquipped( out WeaponDesc weapon, out int maxAmmo ) )
        {
            _ammoText.text = $"{weapon.CurrentAmmo}/{maxAmmo}";
        }
        else
        {
            _ammoText.text = String.Empty;
        }

        float transition = _manager.PlayerCurrentHealth / _manager.PlayerMaxHealth;
        _healthMeter.color = Color.Lerp( _green, _red, 1-transition );
        _healthMeter.fillAmount = transition;
        _bleedText.text = MyExtensionMethods.GetBleedCategory( _manager.PlayerBleedRate ).ToString();

    }


    private bool HasWeaponEquipped(out WeaponDesc weapon, out int maxAmmo)
    {
        maxAmmo = 0;
        CharacterInventory inventory = _entityManager.GetComponentData<CharacterInventory>( _manager.PlayerEntity );
        weapon = new WeaponDesc();
        if ( inventory.EquippedItem == Entity.Null )
            return false;

        if ( !_entityManager.HasComponent( inventory.EquippedItem, typeof( WeaponDesc ) ) )
            return false;
        
        weapon = _entityManager.GetComponentData<WeaponDesc>( inventory.EquippedItem );
        maxAmmo = _entityManager.GetComponentData<CharacterInventory>( _manager.PlayerEntity ).Ammo.GetAmmo( weapon.AmmoType );
        
        
        return true;

    }
}
