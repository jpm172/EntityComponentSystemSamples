using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;
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

    private Image _leftMarkerImage,
        _rightMarkerImage;
    
    [SerializeField]
    private Transform _playerTransform;

    public Vector3 RecoilOffset;

    public float Recovery;
    public float MaxMagnitude;
    public float MinMagnitude;

    public float Control;
    
    public bool fullAuto;
    public float fireRate;
    private float fireRateTimer;
    private EntityManager _entityManager;
    private Entity _playerEntity;

    public float horizontalRecoil = 2;
    public float verticalRecoil = 0.25f;
    
    void Start()
    {
        _leftMarkerImage = _leftMarker.GetComponent<Image>();
        _rightMarkerImage = _rightMarker.GetComponent<Image>();
        
        _camera = Camera.main;
        _entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        _entityManager.CreateEntityQuery( typeof( PlayerInputs ) )
            .TryGetSingletonEntity<Entity>(out _playerEntity);
    }


    void Update()
    {

        PlayerInputs inputs = _entityManager.GetComponentData<PlayerInputs>( _playerEntity );
        RecoilData recoil = _entityManager.GetComponentData<RecoilData>( _playerEntity );
        /*
        recoil.RecoveryTime = Recovery;
        _entityManager.SetComponentData( _playerEntity, recoil );

        if ( Input.GetMouseButtonDown( 0 ) || (Input.GetMouseButton( 0 ) && fullAuto && fireRateTimer <= 0) )
        {

            fireRateTimer = 1 / fireRate;
            recoil.TargetRecoilAngle += AddRecoilAngle(recoil);
            recoil.RecoilTimer = math.min(recoil.RecoilTimer + Control * Time.deltaTime, 1);
            //inputs.RecoilTimer = math.min(inputs.RecoilTimer + (7 * (10/fireRate)) * Time.deltaTime, 1);
            
            

            recoil.TimeSinceShot = 0;
            _entityManager.SetComponentData( _playerEntity, recoil );
            
        }
        //Debug.Log( inputs.RecoilTimer );

        fireRateTimer -= Time.deltaTime;
        */

        transform.position =  inputs.AimPosition;
        _crosshair.transform.position = inputs.AimPosition + recoil.RecoilOffset;
        
        _direction = (transform.position - _playerTransform.position).normalized;

        _markers.transform.up = _direction;
        
        Vector3 inverse = _markers.InverseTransformPoint( inputs.AimPosition + recoil.RecoilOffset );
        _leftMarker.transform.localPosition = new Vector3(-25 + math.min( 0, inverse.x ), 0);
        _rightMarker.transform.localPosition = new Vector3(25 + math.max( 0, inverse.x ), 0);

        
        Color lerpColor = Color.Lerp( Color.black, Color.white, recoil.RecoilTimer );
        _leftMarkerImage.color = lerpColor;
        _rightMarkerImage.color = lerpColor;
        
        //_leftMarker.transform.localPosition = new Vector3(-25 + math.min( 0, inverse.x ), _leftMarker.transform.position.y);
        //_rightMarker.transform.localPosition = new Vector3(25 + math.max( 0, inverse.x ), _rightMarker.transform.position.y,0);

    }

    private float AddRecoilAngle(RecoilData recoil)
    {
        float recoilValue = math.lerp( MaxMagnitude, MinMagnitude, recoil.RecoilTimer );
        
        //float coneAngle = math.lerp( StartCone, EndCone, inputs.RecoilTimer );
        float coneAngle = 45;
        
        Debug.Log( $"{recoil.RecoilTimer} -> {recoilValue} " );
        if ( recoil.TargetRecoilAngle + recoilValue > coneAngle )
        {
            recoilValue *= -1;
        }
        else if ( recoil.TargetRecoilAngle - recoilValue >= -coneAngle )
        {
            recoilValue *= math.@select( 1, -1, Random.Range( 0, 2 ) == 1 );
        }
        
        
        
        return recoilValue;
    }
    

    private void MonoCrosshair()
    {
        if(Input.GetMouseButtonDown( 0 ))
            RecoilOffset = new Vector3(Random.Range( -100, 100 ),0,0);
        
        _direction = (transform.position - _playerTransform.position).normalized;
        
        transform.up = _direction;
        
        transform.position =  (_camera.ScreenToWorldPoint( Input.mousePosition ) * xy);
        
        _crosshair.transform.localPosition = Vector3.MoveTowards(_crosshair.transform.localPosition, RecoilOffset,MaxMagnitude );

        _leftMarker.transform.localPosition = new Vector3(-25 + math.min( 0, RecoilOffset.x ),0,0);
        _rightMarker.transform.localPosition = new Vector3(25 + math.max( 0, RecoilOffset.x ),0,0);
        
        RecoilOffset = Vector3.MoveTowards( RecoilOffset, Vector3.zero, Recovery );
    }
}
