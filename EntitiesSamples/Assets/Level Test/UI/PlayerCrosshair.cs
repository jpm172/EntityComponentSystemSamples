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

    public float horizontalRecoil = 2;
    public float verticalRecoil = 0.25f;
    
    void Start()
    {
        _camera = Camera.main;
        _entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        _entityManager.CreateEntityQuery( typeof( PlayerInputs ) )
            .TryGetSingletonEntity<Entity>(out _playerEntity);
    }


    void Update()
    {

        PlayerInputs inputs = _entityManager.GetComponentData<PlayerInputs>( _playerEntity );

        if ( Input.GetMouseButtonDown( 0 ) )
        {
            // inputs.RecoilValue = new Vector3(Random.Range( -2, 2 ),Random.Range( -2, 2 ),0);
           inputs.TargetRecoilValue += new float3(Random.Range( 0, horizontalRecoil),
               Random.Range( -verticalRecoil, verticalRecoil ),0);
           inputs.TimeSinceShot = 0;
           //inputs.TargetRecoilValue = new float3(1,1,0);
            _entityManager.SetComponentData( _playerEntity, inputs );
            
        }
            
        
        transform.position =  inputs.AimPosition;
        _crosshair.transform.position = inputs.AimPosition + inputs.RecoilOffset;
        
        _direction = (transform.position - _playerTransform.position).normalized;

        _markers.transform.up = _direction;
        
        Vector3 inverse = _markers.InverseTransformPoint( inputs.AimPosition + inputs.RecoilOffset );
        _leftMarker.transform.localPosition = new Vector3(-25 + math.min( 0, inverse.x ), _leftMarker.transform.position.y);
        _rightMarker.transform.localPosition = new Vector3(25 + math.max( 0, inverse.x ), _rightMarker.transform.position.y,0);

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
