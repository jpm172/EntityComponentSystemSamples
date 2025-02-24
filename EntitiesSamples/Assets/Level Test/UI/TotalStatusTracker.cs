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
    

    private void Start()
    {
        _manager = PlayerUIManager.Instance;
        UpdateText();
    }

    // Update is called once per frame
    void Update()
    {
        UpdateText();
    }

    private void UpdateText()
    {
        _healthText.text = $"{_manager.PlayerCurrentHealth}|{_manager.PlayerMaxHealth}";
        
        float bleedRate = _manager.PlayerBleedRate;
        _bleedRateText.text = $"{bleedRate:0.00}";
        _bleedCategoryText.text = GetBleedCategory( bleedRate ).ToString();
    }

    private BleedCategory GetBleedCategory( float bleedRate )
    {
        if ( bleedRate <= Mathf.Epsilon)
        {
            return BleedCategory.None;
        }
        else if ( bleedRate <= 1 )
        {
            return BleedCategory.Trickle;
        }
        else if ( bleedRate <= 5 )
        {
            return BleedCategory.SlowBleed;
        }
        else if ( bleedRate <= 15 )
        {
            return BleedCategory.HeavyBleed;
        }
        else if ( bleedRate <= 30 )
        {
            return BleedCategory.Hemorrhage;
        }

        return BleedCategory.Exodus;
    }
}




