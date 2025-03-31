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
    public float Magnitude;

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

        inputs.RecoveryTime = Recovery;
        _entityManager.SetComponentData( _playerEntity, inputs );

        if ( Input.GetMouseButtonDown( 0 ) || (Input.GetMouseButton( 0 ) && fullAuto && fireRateTimer <= 0) )
        {

            fireRateTimer = 1 / fireRate;
            inputs.TargetRecoilAngle += AddRecoilAngle(inputs);
            inputs.RecoilTimer = math.min(inputs.RecoilTimer + (7 * (10/fireRate)) * Time.deltaTime, 1);
            
            

            inputs.TimeSinceShot = 0;
            _entityManager.SetComponentData( _playerEntity, inputs );
            
        }
        //Debug.Log( inputs.RecoilTimer );

        fireRateTimer -= Time.deltaTime;

        transform.position =  inputs.AimPosition;
        _crosshair.transform.position = inputs.AimPosition + inputs.RecoilOffset;
        
        _direction = (transform.position - _playerTransform.position).normalized;

        _markers.transform.up = _direction;
        
        Vector3 inverse = _markers.InverseTransformPoint( inputs.AimPosition + inputs.RecoilOffset );
        _leftMarker.transform.localPosition = new Vector3(-25 + math.min( 0, inverse.x ), 0);
        _rightMarker.transform.localPosition = new Vector3(25 + math.max( 0, inverse.x ), 0);

        
        Color lerpColor = Color.Lerp( Color.black, Color.white, inputs.RecoilTimer );
        _leftMarkerImage.color = lerpColor;
        _rightMarkerImage.color = lerpColor;
        
        //_leftMarker.transform.localPosition = new Vector3(-25 + math.min( 0, inverse.x ), _leftMarker.transform.position.y);
        //_rightMarker.transform.localPosition = new Vector3(25 + math.max( 0, inverse.x ), _rightMarker.transform.position.y,0);

    }

    private float AddRecoilAngle(PlayerInputs inputs)
    {
        float recoil = Magnitude;

        recoil = math.lerp( Magnitude, .01f, inputs.RecoilTimer );
        Debug.Log( $"{inputs.RecoilTimer} -> {recoil} " );
        if ( inputs.TargetRecoilAngle + recoil > 45 )
        {
            recoil *= -1;
        }
        else if ( inputs.TargetRecoilAngle - recoil >= -45 )
        {
            recoil *= math.@select( 1, -1, Random.Range( 0, 2 ) == 1 );
        }
        
        
        
        return recoil;
    }

    private float3 CalculateRecoil(PlayerInputs inputs)
    {
        float4 recoilBounds = new float4(-2, -0.5f, 2, 0.5f);
        float3 newRecoil = inputs.RecoilValue;

        float xMin = recoilBounds.x - newRecoil.x;
        float xMax = recoilBounds.z - newRecoil.x;

        float yMin = recoilBounds.y - newRecoil.y;
        float yMax = recoilBounds.w - newRecoil.y;
        //Debug.Log( $"{newRecoil} -> {xMin}, {xMax}" );


        float3 addRecoil = new float3(0,0,0);
        bool fitLeft = math.abs( xMin ) >= Magnitude;
        bool fitRight = xMax >= Magnitude;
        
        if ( fitLeft && fitRight )
        {
            addRecoil.x = Magnitude *  math.@select( 1, -1, Random.Range( 0, 2 ) == 1 );
        }
        else if ( fitLeft )
        {
            addRecoil.x = -Magnitude;
        }
        else
        {
            addRecoil.x = Magnitude;
        }
        
        float xMag = Random.Range( xMin, xMax );
        float yMag = Random.Range( yMin, yMax );


        return newRecoil + addRecoil;
        //return newRecoil + new float3(xMag, yMag,0);
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
