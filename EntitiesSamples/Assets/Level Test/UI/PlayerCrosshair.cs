using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class PlayerCrosshair : MonoBehaviour
{

    private static float3 xy = new float3(1,1,0);
    private Camera _camera;

    [SerializeField]
    private RectTransform _markers;

    [SerializeField]
    private Transform _playerTransform;

    void Start()
    {
        _camera = Camera.main;
    }


    void Update()
    {
        _markers.up = _playerTransform.position - _markers.position;
        transform.position = _camera.ScreenToWorldPoint( Input.mousePosition ) * xy;
    }
}
