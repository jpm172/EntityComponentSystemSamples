using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;

public class PlayerCrosshair : MonoBehaviour
{

    private static float3 xy = new float3(1,1,0);
    private Camera _camera;

    [SerializeField]
    private Vector3 _direction;
    
    [SerializeField]
    private RectTransform _crosshair;
    
    [SerializeField]
    private RectTransform _markers;
    
    [SerializeField]
    private RectTransform _leftMarker,
        _rightMarker;

    [SerializeField]
    private Transform _playerTransform;

    public Vector3 RecoilOffset;

    public float Recovery;
    public float Magnitude;

    private EntityManager _entityManager;
    private Entity _playerEntity;
    
    void Start()
    {
        _camera = Camera.main;
        _entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        _entityManager.CreateEntityQuery( typeof( PlayerInputs ) )
            .TryGetSingletonEntity<Entity>(out _playerEntity);
    }


    void Update()
    {
        
        //_direction = (transform.position - _playerTransform.position).normalized;
        //transform.up = _direction;
        //transform.position =  (_camera.ScreenToWorldPoint( Input.mousePosition ) * xy);

        PlayerInputs inputs = _entityManager.GetComponentData<PlayerInputs>( _playerEntity );
        
        

    }

    private void MonoCrosshair()
    {
        if(Input.GetMouseButtonDown( 0 ))
            RecoilOffset = new Vector3(Random.Range( -100, 100 ),0,0);
        
        _direction = (transform.position - _playerTransform.position).normalized;
        
        transform.up = _direction;
        
        transform.position =  (_camera.ScreenToWorldPoint( Input.mousePosition ) * xy);
        
        _crosshair.transform.localPosition = Vector3.MoveTowards(_crosshair.transform.localPosition, RecoilOffset,Magnitude );

        _leftMarker.transform.localPosition = new Vector3(-25 + math.min( 0, RecoilOffset.x ),0,0);
        _rightMarker.transform.localPosition = new Vector3(25 + math.max( 0, RecoilOffset.x ),0,0);
        
        RecoilOffset = Vector3.MoveTowards( RecoilOffset, Vector3.zero, Recovery );
    }
}
