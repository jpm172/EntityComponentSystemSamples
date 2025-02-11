using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class TotalStatusTracker : MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI _healthText;
    
    [SerializeField]
    private TextMeshProUGUI _bleedRateText;
    
    [SerializeField]
    private TextMeshProUGUI _bleedCategoryText;

    private PlayerUIManager _manager;
    
    
    private int _health;

    private void Start()
    {
        _manager = PlayerUIManager.Instance;
        _health = _manager.PlayerCurrentHealth;
        UpdateText();
    }

    // Update is called once per frame
    void Update()
    {
        if ( _health != _manager.PlayerCurrentHealth )
        {
            _health = _manager.PlayerCurrentHealth;
            UpdateText();
        }
    }

    private void UpdateText()
    {
        _healthText.text = $"{_health}|{_manager.PlayerMaxHealth}";
        
        float bleedRate = _manager.PlayerBleedRate;
        _bleedRateText.text = $"{bleedRate:0.00}";
        _bleedCategoryText.text = GetBleedCategory( bleedRate ).ToString();
    }

    private BleedCategory GetBleedCategory( float bleedRate )
    {
        if ( bleedRate <= Mathf.Epsilon )
        {
            return BleedCategory.None;
        }
        else if ( bleedRate <= 5 )
        {
            return BleedCategory.Trickle;
        }
        else if ( bleedRate <= 15 )
        {
            return BleedCategory.SlowBleed;
        }
        else if ( bleedRate <= 35 )
        {
            return BleedCategory.HeavyBleed;
        }
        else if ( bleedRate <= 65 )
        {
            return BleedCategory.Hemorrhage;
        }

        return BleedCategory.Exodus;
    }
}

public enum BleedCategory : int
{
    None = 0,
    Trickle = 1,
    SlowBleed = 2,
    SteadyBleed = 3,
    HeavyBleed = 4,
    Hemorrhage = 5,
    Exodus = 6
    
}


