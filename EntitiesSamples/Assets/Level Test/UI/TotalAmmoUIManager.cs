using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Entities;
using UnityEngine;

public class TotalAmmoUIManager : MonoBehaviour
{

    private EntityManager _entityManager;

    private PlayerUIManager _manager;

    [SerializeField]
    private TextMeshProUGUI _pistolAmmo, _rifleAmmo, _shotgunAmmo, _specialAmmo;

    // Start is called before the first frame update
    void Start()
    {
        _entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        _manager = PlayerUIManager.Instance;
    }

    // Update is called once per frame
    void Update()
    {
        AmmoInfo ammo = _entityManager.GetComponentData<CharacterInventory>( _manager.PlayerEntity ).Ammo;

        _pistolAmmo.text = $"{ammo.CurrentPistolAmmo}";
        _rifleAmmo.text = $"{ammo.CurrentRifleAmmo}";
        _shotgunAmmo.text = $"{ammo.CurrentShotgunAmmo}";
        _specialAmmo.text = $"{ammo.CurrentSpecialAmmo}";

    }
}
