using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UI;

public class PlayerActionTracker : MonoBehaviour
{

    [SerializeField]
    private Image _reloadMeter;
    
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

        if ( inventory.Timer > 0 )
        {
            _reloadMeter.fillAmount = inventory.Remaining / inventory.Timer;
        }
        else
        {
            _reloadMeter.fillAmount = 0;
        }
        
    }
}
