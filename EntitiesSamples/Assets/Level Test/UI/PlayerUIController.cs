using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerUIController : MonoBehaviour
{
    [SerializeField]
    private GameObject _gearLayer;
    [SerializeField]
    private GameObject _healthLayer;


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
